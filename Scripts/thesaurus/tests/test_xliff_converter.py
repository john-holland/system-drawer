"""Smoke test for XLIFF import (hand-crafted XML; export route unchanged)."""

import sqlite3
import sys
import tempfile
import uuid
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from thesaurus import xliff_converter


def _seed_db(conn: sqlite3.Connection):
    conn.executescript(
        """
        CREATE TABLE languages (id INTEGER PRIMARY KEY, code TEXT UNIQUE NOT NULL);
        CREATE TABLE thesaurus_entries (id TEXT PRIMARY KEY, language_id INTEGER NOT NULL, term TEXT NOT NULL, pos_tag TEXT NOT NULL);
        CREATE TABLE thesaurus_translations (id TEXT PRIMARY KEY, entry_id TEXT NOT NULL, language_id INTEGER NOT NULL, form TEXT);
        """
    )
    conn.execute("INSERT INTO languages (id, code) VALUES (1, 'en'), (2, 'fr')")
    eid = str(uuid.uuid4())
    conn.execute(
        "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES (?, 1, ?, ?)",
        (eid, "hello", "noun"),
    )
    conn.commit()
    return eid


def test_xliff_export_auto_creates_source_language():
    tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
    tmp.close()
    conn = sqlite3.connect(tmp.name)
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE languages (id TEXT PRIMARY KEY, code TEXT UNIQUE NOT NULL, name TEXT, script_direction TEXT);
        CREATE TABLE thesaurus_entries (id TEXT PRIMARY KEY, language_id TEXT NOT NULL, term TEXT NOT NULL, pos_tag TEXT NOT NULL);
        CREATE TABLE thesaurus_translations (id TEXT PRIMARY KEY, entry_id TEXT NOT NULL, language_id TEXT NOT NULL, form TEXT);
        """
    )
    eid = str(uuid.uuid4())
    from thesaurus.language_resolver import resolve_language_id

    en_id = resolve_language_id(conn, "en", create=True)
    conn.execute(
        "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES (?, ?, ?, ?)",
        (eid, en_id, "hello", "noun"),
    )
    conn.commit()
    xml = xliff_converter.export_to_xliff(conn, "en", "fr")
    conn.close()
    assert "hello" in xml
    assert 'trgLang="fr"' in xml


def test_xliff_import_inserts_translation():
    tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
    tmp.close()
    conn = sqlite3.connect(tmp.name)
    conn.row_factory = sqlite3.Row
    eid = _seed_db(conn)
    xml = f"""<?xml version="1.0"?>
<xliff version="2.0" srcLang="en" trgLang="fr" xmlns="urn:oasis:names:tc:xliff:document:2.0">
  <file id="thesaurus"><unit id="{eid}"><segment>
    <source>hello</source><target>bonjour</target>
  </segment></unit></file>
</xliff>"""
    updated, inserted = xliff_converter.import_from_xliff(conn, xml)
    row = conn.execute(
        "SELECT form FROM thesaurus_translations WHERE entry_id = ? AND language_id = 2",
        (eid,),
    ).fetchone()
    conn.close()
    assert inserted == 1
    assert row[0] == "bonjour"
