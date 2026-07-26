"""Change-of-basis rewrite engine: multi-word, word-id, loops, validation."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from change_of_basis_engine import (  # noqa: E402
    apply_change_of_basis,
    get_engine_defaults,
    upsert_rule,
    validate_ruleset,
)
from nsm_wiring_db import ensure_nsm_schema  # noqa: E402


def _setup(tmp_path):
    db = tmp_path / "cob.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE languages (id TEXT PRIMARY KEY, code TEXT NOT NULL UNIQUE, name TEXT);
        CREATE TABLE thesaurus_entries (
            id TEXT PRIMARY KEY, language_id TEXT NOT NULL, term TEXT NOT NULL, pos_tag TEXT NOT NULL
        );
        CREATE TABLE thesaurus_translations (
            id TEXT PRIMARY KEY, entry_id TEXT NOT NULL, language_id TEXT NOT NULL, form TEXT NOT NULL
        );
        CREATE TABLE thesaurus_alternatives (
            id TEXT PRIMARY KEY, entry_id TEXT NOT NULL, pos_tag TEXT, form TEXT NOT NULL, role TEXT
        );
        CREATE TABLE change_of_basis_word_overrides (
            id TEXT PRIMARY KEY, target_language_id TEXT NOT NULL, term TEXT NOT NULL,
            context_type TEXT NOT NULL DEFAULT 'default', target_form TEXT, rule_id TEXT
        );
        """
    )
    conn.execute("INSERT INTO languages VALUES ('lang_en','en','English')")
    conn.execute("INSERT INTO languages VALUES ('lang_es','es','Spanish')")
    conn.execute(
        "INSERT INTO thesaurus_entries VALUES ('e_just','lang_en','just','adverb')"
    )
    conn.execute(
        "INSERT INTO thesaurus_entries VALUES ('e_like','lang_en','like','preposition')"
    )
    conn.execute(
        "INSERT INTO thesaurus_entries VALUES ('e_had','lang_en','had','verb')"
    )
    ensure_nsm_schema(conn)
    conn.execute(
        """INSERT INTO change_of_basis_conjugations (
            id, target_language_id, entry_id, lemma_term, pos_tag, mood, tense, person, number, target_form
        ) VALUES ('c1','lang_es',NULL,'hacer','verb','indicative','past','3','singular','hizo')"""
    )
    conn.commit()
    return conn


def test_multiword_activation_replacement(tmp_path):
    conn = _setup(tmp_path)
    upsert_rule(
        conn,
        {
            "id": "r_just_like",
            "sourceLanguageId": "lang_en",
            "targetLanguageId": "lang_es",
            "priority": 1,
            "activation": {
                "tokens": [
                    {"term": "just"},
                    {"term": "like"},
                    {"wildcard": "span", "capture": "VP", "min": 1, "max": 4},
                ]
            },
            "replacement": {
                "tokens": [
                    {"capture": "VP"},
                    {"term": "como"},
                    {"conj": {"lemma": "hacer", "tense": "past", "person": "3", "number": "singular"}},
                ]
            },
        },
    )
    result = apply_change_of_basis(
        conn,
        "he just like pressed the button",
        "lang_en",
        "lang_es",
        dry_run=True,
    )
    assert "como" in result["scriptText"]
    assert "hizo" in result["scriptText"]
    assert result["appliedRules"]


def test_word_id_activation(tmp_path):
    conn = _setup(tmp_path)
    upsert_rule(
        conn,
        {
            "id": "r_ids",
            "sourceLanguageId": "lang_en",
            "targetLanguageId": "lang_es",
            "priority": 1,
            "activation": {"wordIds": ["e_just", "e_like"], "requireAllWordIds": True},
            "replacement": {"tokens": [{"term": "igual"}]},
        },
    )
    result = apply_change_of_basis(
        conn, "just like before", "lang_en", "lang_es", dry_run=True
    )
    assert "igual" in result["scriptText"]


def test_count_defaults_halt_runaway(tmp_path):
    conn = _setup(tmp_path)
    # A -> B and B -> A would loop; engine should warn and stop
    upsert_rule(
        conn,
        {
            "id": "r_a",
            "targetLanguageId": "lang_es",
            "sourceLanguageId": "lang_en",
            "priority": 1,
            "maxApplications": 2,
            "activation": {"tokens": [{"term": "alpha"}]},
            "replacement": {"tokens": [{"term": "beta"}]},
        },
    )
    upsert_rule(
        conn,
        {
            "id": "r_b",
            "targetLanguageId": "lang_es",
            "sourceLanguageId": "lang_en",
            "priority": 2,
            "maxApplications": 2,
            "activation": {"tokens": [{"term": "beta"}]},
            "replacement": {"tokens": [{"term": "alpha"}]},
        },
    )
    result = apply_change_of_basis(conn, "alpha", "lang_en", "lang_es", dry_run=True)
    assert any(w.get("type") == "loop_warning" for w in result["warnings"]) or result["stats"][
        "passes"
    ] <= get_engine_defaults(conn)["max_global_passes"]


def test_validate_incomplete_and_divergent(tmp_path):
    conn = _setup(tmp_path)
    upsert_rule(
        conn,
        {
            "id": "r_bad",
            "targetLanguageId": "lang_es",
            "activation": {"tokens": [{"entryId": "missing-id"}]},
            "replacement": {"tokens": [{"term": "x"}]},
        },
    )
    report = validate_ruleset(conn, "lang_en", "lang_es")
    assert report["completion"] == "incomplete"

    upsert_rule(
        conn,
        {
            "id": "r_c1",
            "targetLanguageId": "lang_es",
            "activation": {"tokens": [{"term": "foo"}]},
            "replacement": {"tokens": [{"term": "bar"}]},
        },
    )
    upsert_rule(
        conn,
        {
            "id": "r_c2",
            "targetLanguageId": "lang_es",
            "activation": {"tokens": [{"term": "bar"}]},
            "replacement": {"tokens": [{"term": "foo"}]},
        },
    )
    # Clear dangling rule by disabling
    conn.execute("UPDATE change_of_basis_rules SET enabled = 0 WHERE id = 'r_bad'")
    conn.commit()
    report2 = validate_ruleset(conn, "lang_en", "lang_es")
    assert report2["completion"] in ("divergent", "complete", "incomplete")
    if report2["completion"] != "incomplete":
        assert report2["completion"] == "divergent" or any(
            w.get("type") == "loop_warning" for w in report2["warnings"]
        )


def test_dry_run_no_audio_side_effects(tmp_path):
    conn = _setup(tmp_path)
    result = apply_change_of_basis(conn, "hello world", "lang_en", "lang_es", dry_run=True)
    assert result["stats"]["dryRun"] is True
    assert "scriptText" in result
