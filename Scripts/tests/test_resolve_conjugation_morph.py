"""resolve_conjugation: DB override wins; morph fills misses; NSM CoB CJK."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from change_of_basis_engine import apply_change_of_basis, resolve_conjugation  # noqa: E402
from nsm_wiring_db import seed_nsm_cob_es_fr, seed_nsm_cob_ja_ko_zh  # noqa: E402
from thesaurus.language_resolver import resolve_language_id  # noqa: E402


def _setup(tmp_path):
    db = tmp_path / "morph.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE languages (
            id TEXT PRIMARY KEY, code TEXT NOT NULL UNIQUE, name TEXT,
            script_direction TEXT, morphology_rules_ref TEXT
        );
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
    conn.commit()
    return conn


def test_morph_fills_db_miss(tmp_path):
    conn = _setup(tmp_path)
    seed_nsm_cob_es_fr(conn)
    lang_es = resolve_language_id(conn, "es", create=False)
    # "hablar" not in seed conjugations table for all slots — morph generates
    form = resolve_conjugation(conn, lang_es, "hablar", slots={"person": "1", "number": "singular"})
    assert form == "hablo"


def test_db_override_wins(tmp_path):
    conn = _setup(tmp_path)
    seed_nsm_cob_es_fr(conn)
    lang_es = resolve_language_id(conn, "es", create=False)
    # Seed has hacer 3sg → hace; override via resolve uses DB
    form = resolve_conjugation(conn, lang_es, "hacer", slots={"person": "3", "number": "singular"})
    assert form == "hace"


def test_cob_do_all_langs(tmp_path):
    conn = _setup(tmp_path)
    seed_nsm_cob_es_fr(conn)
    seed_nsm_cob_ja_ko_zh(conn)
    lang_en = resolve_language_id(conn, "en", create=False)
    for code, needle in (
        ("es", "hace"),
        ("fr", "fait"),
        ("ja", "します"),
        ("ko", "해요"),
        ("zh", "做"),
    ):
        lang = resolve_language_id(conn, code, create=False)
        result = apply_change_of_basis(conn, "do", lang_en, lang, dry_run=True)
        assert needle in result["scriptText"], (code, result["scriptText"])
        assert result["appliedRules"]
