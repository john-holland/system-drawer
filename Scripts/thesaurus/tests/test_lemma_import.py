"""Tests for lemma import and property parsing."""

from __future__ import annotations

import io
import sqlite3
import sys
from pathlib import Path

import pytest

_scripts = Path(__file__).resolve().parents[2]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from continuum_api.lemma_import import parse_default_properties, parse_tabular_file, upsert_lemma_row
from continuum_api.lemma_merge import filter_entries, is_builtin_urn, load_builtin_vocabulary


def test_parse_default_properties_prompt():
    props = parse_default_properties("{P:walk|non-ik-animation=true}")
    assert props.get("non-ik-animation") == "true"


def test_parse_default_properties_kv():
    props = parse_default_properties("non-ik-animation=true; prefab-id=abc")
    assert props.get("non-ik-animation") == "true"
    assert props.get("prefab-id") == "abc"


def test_parse_tabular_single_column():
    content = "word\nhello\nworld\n"
    headers, rows = parse_tabular_file(content, fmt="csv")
    assert "word" in headers or rows[0]["word"] == "hello"
    assert len(rows) == 2


def test_parse_tabular_multi_column():
    content = "word,description,language\nladder,a thing,en\n"
    _, rows = parse_tabular_file(content, fmt="csv")
    assert rows[0]["word"] == "ladder"
    assert rows[0]["description"] == "a thing"
    assert rows[0]["language"] == "en"


def test_is_builtin_urn():
    assert is_builtin_urn("urn:unity:continuum:builtin:v1:/en/noun/test")
    assert not is_builtin_urn("550e8400-e29b-41d4-a716-446655440000")


def test_load_builtin_vocabulary():
    items = load_builtin_vocabulary()
    assert len(items) >= 90
    assert any(i["term"] == "the" for i in items)


def test_filter_entries_builtin_only():
    items = [
        {"term": "a", "isBuiltIn": True, "languageCode": "en", "posTag": "det"},
        {"term": "custom", "isBuiltIn": False, "languageCode": "en", "posTag": "noun"},
    ]
    out = filter_entries(items, source="builtin")
    assert len(out) == 1
    assert out[0]["term"] == "a"


@pytest.fixture
def mem_db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE languages (id TEXT PRIMARY KEY, code TEXT UNIQUE, name TEXT, script_direction TEXT);
        INSERT INTO languages VALUES ('lang-en', 'en', 'English', 'ltr');
        CREATE TABLE thesaurus_entries (id TEXT PRIMARY KEY, language_id TEXT, term TEXT, pos_tag TEXT, UNIQUE(language_id, term, pos_tag));
        CREATE TABLE thesaurus_alternatives (id TEXT PRIMARY KEY, entry_id TEXT, pos_tag TEXT, form TEXT, role TEXT);
        CREATE TABLE dictionary_definitions (id TEXT PRIMARY KEY, entry_id TEXT, language_id TEXT, definition TEXT, source TEXT, created_at TEXT);
        CREATE TABLE localization_property_specs (key TEXT PRIMARY KEY, value_type TEXT, allowed_values_json TEXT, default_value TEXT, description TEXT);
        INSERT INTO localization_property_specs VALUES ('non-ik-animation', 'Bool', NULL, 'false', '');
        INSERT INTO localization_property_specs VALUES ('prefab-id', 'String', NULL, '', '');
        CREATE TABLE thesaurus_entry_properties (entry_id TEXT, property_key TEXT, property_value TEXT, PRIMARY KEY(entry_id, property_key));
        """
    )
    yield conn
    conn.close()


def test_upsert_lemma_row(mem_db):
    from continuum_api.lemma_import import _valid_property_keys

    keys = _valid_property_keys(mem_db)
    status, err, eid = upsert_lemma_row(
        mem_db,
        {
            "word": "ladder",
            "description": "climbable",
            "language": "en",
            "partOfSpeech": "noun",
            "prefabId": "doc-99",
            "defaultProperties": "{P:ladder|non-ik-animation=true}",
        },
        keys,
    )
    assert err is None
    assert status == "created"
    assert eid
    cur = mem_db.execute("SELECT property_value FROM thesaurus_entry_properties WHERE entry_id = ? AND property_key = 'prefab-id'", (eid,))
    assert cur.fetchone()["property_value"] == "doc-99"
