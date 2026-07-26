"""NSM en→es/fr CoB conjugation seed."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from change_of_basis_engine import apply_change_of_basis  # noqa: E402
from nsm_wiring_db import seed_nsm_cob_es_fr, seed_nsm_prime_wiring  # noqa: E402
from thesaurus.language_resolver import resolve_language_id  # noqa: E402


def _setup(tmp_path):
    db = tmp_path / "nsm_cob.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE languages (
            id TEXT PRIMARY KEY, code TEXT NOT NULL UNIQUE, name TEXT, script_direction TEXT
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


def test_seed_do_want_spanish_french(tmp_path):
    conn = _setup(tmp_path)
    counts = seed_nsm_cob_es_fr(conn)
    assert counts["conjugations"] > 0
    assert counts["rules"] > 0

    lang_en = resolve_language_id(conn, "en", create=False)
    lang_es = resolve_language_id(conn, "es", create=False)
    lang_fr = resolve_language_id(conn, "fr", create=False)

    es_do = apply_change_of_basis(conn, "do", lang_en, lang_es, dry_run=True)
    assert "hace" in es_do["scriptText"].split()
    assert es_do["appliedRules"]

    es_want = apply_change_of_basis(conn, "want", lang_en, lang_es, dry_run=True)
    assert "quiere" in es_want["scriptText"].split()

    fr_do = apply_change_of_basis(conn, "do", lang_en, lang_fr, dry_run=True)
    assert "fait" in fr_do["scriptText"].split()

    fr_want = apply_change_of_basis(conn, "want", lang_en, lang_fr, dry_run=True)
    assert "veut" in fr_want["scriptText"].split()

    es_neg = apply_change_of_basis(conn, "dont-want", lang_en, lang_es, dry_run=True)
    assert "no" in es_neg["scriptText"].split()
    assert "quiere" in es_neg["scriptText"].split()


def test_seed_via_prime_wiring_idempotent(tmp_path):
    conn = _setup(tmp_path)
    a = seed_nsm_prime_wiring(conn)
    b = seed_nsm_prime_wiring(conn)
    assert a["cobRules"] == b["cobRules"]
    assert a["cobConjugations"] == b["cobConjugations"]
    n = conn.execute("SELECT COUNT(*) AS c FROM change_of_basis_rules WHERE id LIKE 'cob_nsm_%'").fetchone()["c"]
    assert n == a["cobRules"]
