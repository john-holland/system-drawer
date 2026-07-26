"""Parity tests for the 65 NSM semantic primes."""

from __future__ import annotations

import json
import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
DATA = API / "data"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from lemma_completion_db import (  # noqa: E402
    ensure_lemma_completion_schema,
    seed_lemma_completion,
    summary,
)

PRIMES_PATH = DATA / "nsm_semantic_primes_en.json"
GLOSSES_PATH = DATA / "nsm_semantic_prime_glosses_en.json"
BUILTIN_PATH = DATA / "builtin_vocabulary.json"

OVERLAP_TERMS = {
    "this",
    "move",
    "when",
    "here",
    "near",
    "inside",
    "not",
    "because",
    "if",
}


def _load_primes():
    data = json.loads(PRIMES_PATH.read_text(encoding="utf-8"))
    return list(data.get("primes") or [])


def _load_glosses():
    data = json.loads(GLOSSES_PATH.read_text(encoding="utf-8"))
    return list(data.get("primes") or [])


def _load_builtin():
    data = json.loads(BUILTIN_PATH.read_text(encoding="utf-8"))
    return list(data.get("items") or [])


def test_canonical_prime_list_is_65_unique():
    primes = _load_primes()
    assert len(primes) == 65
    terms = [p["term"] for p in primes]
    assert len(terms) == len(set(terms))


def test_glosses_cover_exactly_same_65_terms():
    prime_terms = {p["term"] for p in _load_primes()}
    glosses = _load_glosses()
    assert len(glosses) == 65
    gloss_terms = {g["term"] for g in glosses}
    assert gloss_terms == prime_terms
    for g in glosses:
        assert g.get("nsmDefinition")
        assert g.get("mechanicalRole")


def test_every_prime_tagged_nsm_prime_in_builtin_json():
    primes = _load_primes()
    by_term: dict[str, list] = {}
    for item in _load_builtin():
        by_term.setdefault(str(item["term"]).lower(), []).append(item)

    missing = []
    untagged = []
    for p in primes:
        term = p["term"]
        hits = by_term.get(term.lower()) or []
        if not hits:
            missing.append(term)
            continue
        if not any(
            "nsm" in (h.get("tags") or []) and "prime" in (h.get("tags") or [])
            for h in hits
        ):
            untagged.append(term)
    assert not missing, f"missing from builtin: {missing}"
    assert not untagged, f"missing nsm+prime tags: {untagged}"


def test_overlap_primes_still_tagged():
    by_term = {}
    for item in _load_builtin():
        by_term.setdefault(str(item["term"]).lower(), []).append(item)
    for term in OVERLAP_TERMS:
        hits = by_term.get(term) or []
        assert hits, term
        assert any("prime" in (h.get("tags") or []) for h in hits), term


def test_semantic_prime_category_count():
    items = _load_builtin()
    sem = [i for i in items if i.get("builtInCategory") == "SemanticPrime"]
    assert len(sem) >= 56


def test_seed_marks_all_primes_defined_and_implemented(tmp_path):
    db = tmp_path / "t.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    ensure_lemma_completion_schema(conn)
    result = seed_lemma_completion(conn)
    assert result.get("glossesUpdated", 0) == 65

    rows = conn.execute(
        """SELECT term, nsm_definition, is_prime, is_builtin, is_implemented
           FROM lemma_completion WHERE is_prime = 1"""
    ).fetchall()
    assert len(rows) == 65
    for r in rows:
        assert (r["nsm_definition"] or "").strip()
        assert r["is_builtin"] == 1
        assert r["is_implemented"] == 1

    s = summary(conn, scope="primes")
    conn.close()
    assert s["total"] == 65
    assert s["defined"] == 65
    assert s["builtin"] == 65
    assert s["implemented"] == 65
    assert s["percentOverall"] == 100.0
