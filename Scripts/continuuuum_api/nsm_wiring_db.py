"""Ensure NSM lemma schema and seed associations / hedges / prime wiring."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any

try:
    from continuuuum_api.lemma_completion_db import (
        builtin_urn,
        ensure_lemma_completion_schema,
        load_builtin_term_ids,
        load_prime_glosses,
        load_primes,
        _now,
        _slug,
        new_id,
    )
except ImportError:
    from lemma_completion_db import (
        builtin_urn,
        ensure_lemma_completion_schema,
        load_builtin_term_ids,
        load_prime_glosses,
        load_primes,
        _now,
        _slug,
        new_id,
    )

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent
_DATA_DIR = Path(__file__).resolve().parent / "data"
NSM_SCHEMA_PATH = _SCHEMA_ROOT / "continuuuum_nsm_lemma_schema.sql"
ASSOC_PATH = _DATA_DIR / "nsm_prime_associations_en.json"
HEDGES_PATH = _DATA_DIR / "nsm_fuzzy_hedges_en.json"
COB_ES_FR_PATH = _DATA_DIR / "nsm_cob_es_fr.json"
COB_JA_KO_ZH_PATH = _DATA_DIR / "nsm_cob_ja_ko_zh.json"
COB_SCHEMA_PATH = _SCHEMA_ROOT / "continuuuum_change_of_basis_schema.sql"

CAUSALITY_BY_TERM = {
    "because": "causal",
    "if": "conditional",
    "not": "negation",
    "when": "temporal",
    "before": "temporal",
    "after": "temporal",
    "now": "temporal",
    "maybe": "modal",
    "can": "modal",
}

TEMPORAL_BY_TERM = {
    "when": "when",
    "now": "now",
    "before": "before",
    "after": "after",
    "a-long-time": "duration",
    "a-short-time": "duration",
    "for-some-time": "duration",
    "moment": "moment",
}


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def _ensure_column(conn: sqlite3.Connection, table: str, column: str, decl: str) -> None:
    cols = {r[1] for r in conn.execute(f"PRAGMA table_info({table})").fetchall()}
    if column not in cols:
        conn.execute(f"ALTER TABLE {table} ADD COLUMN {column} {decl}")


def ensure_nsm_schema(conn: sqlite3.Connection) -> None:
    """Create NSM tables + property specs; extend CoB columns safely."""
    if not _table_exists(conn, "localization_property_specs"):
        conn.execute(
            """CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY,
                value_type TEXT,
                allowed_values_json TEXT,
                default_value TEXT,
                description TEXT
            )"""
        )
    sql = NSM_SCHEMA_PATH.read_text(encoding="utf-8")
    conn.executescript(sql)

    if not _table_exists(conn, "change_of_basis_rules"):
        if COB_SCHEMA_PATH.is_file():
            conn.executescript(COB_SCHEMA_PATH.read_text(encoding="utf-8"))

    if _table_exists(conn, "change_of_basis_rules"):
        for col, decl in (
            ("source_language_id", "TEXT"),
            ("source_pos", "TEXT"),
            ("conjugation_mood", "TEXT"),
            ("conjugation_tense", "TEXT"),
            ("conjugation_person", "TEXT"),
            ("conjugation_number", "TEXT"),
            ("activation_json", "TEXT"),
            ("replacement_json", "TEXT"),
            ("match_mode", "TEXT DEFAULT 'sequence'"),
            ("max_applications", "INTEGER"),
            ("enabled", "INTEGER NOT NULL DEFAULT 1"),
        ):
            _ensure_column(conn, "change_of_basis_rules", col, decl)

    conn.execute(
        """CREATE TABLE IF NOT EXISTS change_of_basis_conjugations (
            id TEXT PRIMARY KEY,
            target_language_id TEXT NOT NULL,
            entry_id TEXT,
            lemma_term TEXT NOT NULL,
            pos_tag TEXT NOT NULL,
            mood TEXT NOT NULL DEFAULT 'indicative',
            tense TEXT NOT NULL DEFAULT 'present',
            person TEXT NOT NULL DEFAULT '3',
            number TEXT NOT NULL DEFAULT 'singular',
            aspect TEXT NOT NULL DEFAULT 'none',
            politeness TEXT NOT NULL DEFAULT 'plain',
            polarity TEXT NOT NULL DEFAULT 'affirmative',
            voice TEXT NOT NULL DEFAULT 'active',
            formality TEXT NOT NULL DEFAULT 'plain',
            target_form TEXT NOT NULL,
            UNIQUE(target_language_id, lemma_term, pos_tag, mood, tense, person, number,
                   aspect, politeness, polarity, voice, formality)
        )"""
    )
    for col, decl in (
        ("aspect", "TEXT NOT NULL DEFAULT 'none'"),
        ("politeness", "TEXT NOT NULL DEFAULT 'plain'"),
        ("polarity", "TEXT NOT NULL DEFAULT 'affirmative'"),
        ("voice", "TEXT NOT NULL DEFAULT 'active'"),
        ("formality", "TEXT NOT NULL DEFAULT 'plain'"),
    ):
        _ensure_column(conn, "change_of_basis_conjugations", col, decl)
    conn.execute(
        """CREATE TABLE IF NOT EXISTS change_of_basis_engine_defaults (
            id TEXT PRIMARY KEY DEFAULT 'global',
            max_global_passes INTEGER NOT NULL DEFAULT 32,
            max_rule_applications INTEGER NOT NULL DEFAULT 8,
            max_clause_expansions INTEGER NOT NULL DEFAULT 64,
            warn_on_loop INTEGER NOT NULL DEFAULT 1,
            fail_on_loop INTEGER NOT NULL DEFAULT 0,
            require_validation INTEGER NOT NULL DEFAULT 0
        )"""
    )
    cur = conn.execute(
        "SELECT 1 FROM change_of_basis_engine_defaults WHERE id = 'global' LIMIT 1"
    ).fetchone()
    if not cur:
        conn.execute(
            """INSERT INTO change_of_basis_engine_defaults (
                id, max_global_passes, max_rule_applications, max_clause_expansions,
                warn_on_loop, fail_on_loop, require_validation
            ) VALUES ('global', 32, 8, 64, 1, 0, 0)"""
        )
    conn.commit()


def logical_form_for_prime(term: str) -> dict[str, Any]:
    t = term.strip()
    if t == "after":
        return {"op": "after", "args": [{"op": "prime", "term": "now"}]}
    if t in ("if", "because", "not", "and", "or", "can", "maybe", "before", "when", "like", "true"):
        return {"op": t, "args": [{"op": "var", "name": "P"}]}
    if t in ("very", "more", "little", "much"):
        return {"op": "hedge", "hedgeId": t, "args": [{"op": "var", "name": "P"}]}
    return {"op": "prime", "term": t}


def seed_nsm_associations(conn: sqlite3.Connection, language_code: str = "en") -> int:
    if not ASSOC_PATH.is_file():
        return 0
    data = json.loads(ASSOC_PATH.read_text(encoding="utf-8"))
    rows = list(data.get("associations") or [])
    n = 0
    for row in rows:
        rid = row.get("id") or new_id("nsa")
        try:
            conn.execute(
                """INSERT OR IGNORE INTO nsm_prime_associations (
                    id, language_code, source_term, target_term, relation_kind,
                    directed, math_form_json, notes
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    rid,
                    language_code,
                    row["source_term"],
                    row["target_term"],
                    row["relation_kind"],
                    int(row.get("directed", 1)),
                    json.dumps(row["math_form"]) if row.get("math_form") else row.get("math_form_json"),
                    row.get("notes"),
                ),
            )
            n += 1
        except Exception:
            continue
    conn.commit()
    return n


def seed_nsm_fuzzy_hedges(conn: sqlite3.Connection, language_code: str = "en") -> int:
    if not HEDGES_PATH.is_file():
        return 0
    data = json.loads(HEDGES_PATH.read_text(encoding="utf-8"))
    rows = list(data.get("hedges") or [])
    now = _now()
    n = 0
    for row in rows:
        hid = row.get("id") or _slug(row.get("phrase") or "hedge")
        try:
            conn.execute(
                """INSERT OR REPLACE INTO nsm_fuzzy_hedges (
                    id, language_code, phrase, aliases_json, band, curve_json,
                    linked_primes_json, updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    hid,
                    language_code,
                    row["phrase"],
                    json.dumps(row.get("aliases") or []),
                    row["band"],
                    json.dumps(row["curve"]),
                    json.dumps(row.get("linked_primes") or []),
                    now,
                ),
            )
            n += 1
        except Exception:
            continue
    conn.commit()
    return n


def seed_nsm_cob_es_fr(conn: sqlite3.Connection) -> dict[str, int]:
    """Seed en→es/fr NSM verb CoB rules + present indicative conjugations."""
    ensure_nsm_schema(conn)
    if not COB_ES_FR_PATH.is_file():
        return {"conjugations": 0, "rules": 0}
    try:
        from thesaurus.language_resolver import ensure_default_languages, resolve_language_id
    except ImportError:
        try:
            from continuuuum_api.language_resolver import (  # type: ignore
                ensure_default_languages,
                resolve_language_id,
            )
        except ImportError:
            sys_path_root = _SCHEMA_ROOT
            import sys

            if str(sys_path_root) not in sys.path:
                sys.path.insert(0, str(sys_path_root))
            from thesaurus.language_resolver import ensure_default_languages, resolve_language_id

    try:
        from continuuuum_api.change_of_basis_engine import upsert_rule
    except ImportError:
        from change_of_basis_engine import upsert_rule

    ensure_default_languages(conn)
    lang_en = resolve_language_id(conn, "en", create=True)
    lang_es = resolve_language_id(conn, "es", create=True)
    lang_fr = resolve_language_id(conn, "fr", create=True)
    data = json.loads(COB_ES_FR_PATH.read_text(encoding="utf-8"))
    conj_n = 0
    for lang_code, lemmas in (data.get("conjugations") or {}).items():
        target_id = lang_es if lang_code == "es" else lang_fr if lang_code == "fr" else None
        if not target_id:
            continue
        for lemma, slots in (lemmas or {}).items():
            for slot_key, form in (slots or {}).items():
                person, number = slot_key.split("|", 1)
                cid = f"cob_conj_{lang_code}_{_slug(lemma)}_{person}_{number[0]}"
                conn.execute(
                    """INSERT OR REPLACE INTO change_of_basis_conjugations (
                        id, target_language_id, entry_id, lemma_term, pos_tag,
                        mood, tense, person, number, aspect, politeness, polarity,
                        voice, formality, target_form
                    ) VALUES (?, ?, NULL, ?, 'verb', 'indicative', 'present', ?, ?,
                              'none', 'plain', 'affirmative', 'active', 'plain', ?)""",
                    (cid, target_id, lemma, person, number, form),
                )
                conj_n += 1

    rule_n = 0
    lemma_map = data.get("lemmaMap") or {}
    for en_term, mapping in lemma_map.items():
        for lang_code, lang_id in (("es", lang_es), ("fr", lang_fr)):
            target_lemma = mapping.get(lang_code)
            if not target_lemma:
                continue
            rid = f"cob_nsm_en_{lang_code}_{_slug(en_term)}"
            conj_tok = {
                "conj": {
                    "lemma": target_lemma,
                    "mood": "indicative",
                    "tense": "present",
                }
            }
            if lang_code == "es" and mapping.get("esNeg"):
                replacement_tokens = [{"term": "no"}, conj_tok]
            elif lang_code == "fr" and mapping.get("frNeg"):
                replacement_tokens = [{"term": "ne"}, conj_tok, {"term": "pas"}]
            else:
                replacement_tokens = [conj_tok]
            upsert_rule(
                conn,
                {
                    "id": rid,
                    "sourceLanguageId": lang_en,
                    "targetLanguageId": lang_id,
                    "priority": 50,
                    "matchMode": "sequence",
                    "enabled": 1,
                    "activation": {"tokens": [{"term": en_term}]},
                    "replacement": {"tokens": replacement_tokens},
                },
            )
            rule_n += 1
    conn.commit()
    return {"conjugations": conj_n, "rules": rule_n}


def seed_nsm_cob_ja_ko_zh(conn: sqlite3.Connection) -> dict[str, int]:
    """Seed en→ja/ko/zh NSM verb CoB rules (morph engine fills surface forms)."""
    ensure_nsm_schema(conn)
    if not COB_JA_KO_ZH_PATH.is_file():
        return {"rules": 0}
    try:
        from thesaurus.language_resolver import ensure_default_languages, resolve_language_id
    except ImportError:
        import sys

        if str(_SCHEMA_ROOT) not in sys.path:
            sys.path.insert(0, str(_SCHEMA_ROOT))
        from thesaurus.language_resolver import ensure_default_languages, resolve_language_id

    try:
        from continuuuum_api.change_of_basis_engine import upsert_rule
    except ImportError:
        from change_of_basis_engine import upsert_rule

    ensure_default_languages(conn)
    lang_en = resolve_language_id(conn, "en", create=True)
    langs = {
        "ja": resolve_language_id(conn, "ja", create=True),
        "ko": resolve_language_id(conn, "ko", create=True),
        "zh": resolve_language_id(conn, "zh", create=True),
    }
    data = json.loads(COB_JA_KO_ZH_PATH.read_text(encoding="utf-8"))
    rule_n = 0
    for en_term, mapping in (data.get("lemmaMap") or {}).items():
        for lang_code, lang_id in langs.items():
            target_lemma = mapping.get(lang_code)
            if not target_lemma or not lang_id:
                continue
            rid = f"cob_nsm_en_{lang_code}_{_slug(en_term)}"
            if lang_code == "ja":
                conj = {
                    "lemma": target_lemma,
                    "mood": "indicative",
                    "tense": "nonpast",
                    "politeness": "polite" if not mapping.get("jaNeg") else "plain",
                    "polarity": "negative" if mapping.get("jaNeg") else "affirmative",
                }
                if mapping.get("jaNeg"):
                    # plain negative nai form
                    conj["tense"] = "nai"
                    conj["politeness"] = "plain"
                replacement_tokens = [{"conj": conj}]
            elif lang_code == "ko":
                conj = {
                    "lemma": target_lemma,
                    "tense": "present",
                    "politeness": "polite",
                    "formality": "haeyo",
                    "polarity": "negative" if mapping.get("koNeg") else "affirmative",
                }
                replacement_tokens = [{"conj": conj}]
            else:  # zh
                conj = {
                    "lemma": target_lemma,
                    "aspect": "none",
                    "polarity": "bu" if mapping.get("zhNeg") else "affirmative",
                }
                replacement_tokens = [{"conj": conj}]
            upsert_rule(
                conn,
                {
                    "id": rid,
                    "sourceLanguageId": lang_en,
                    "targetLanguageId": lang_id,
                    "priority": 50,
                    "matchMode": "sequence",
                    "enabled": 1,
                    "activation": {"tokens": [{"term": en_term}]},
                    "replacement": {"tokens": replacement_tokens},
                },
            )
            rule_n += 1
    conn.commit()
    return {"rules": rule_n}


def seed_nsm_prime_wiring(conn: sqlite3.Connection, language_code: str = "en") -> dict[str, int]:
    """Idempotent: schema + associations + hedges + lemma_completion descriptors."""
    ensure_nsm_schema(conn)
    ensure_lemma_completion_schema(conn)
    assoc_n = seed_nsm_associations(conn, language_code)
    hedge_n = seed_nsm_fuzzy_hedges(conn, language_code)
    cob = seed_nsm_cob_es_fr(conn)
    cob_cjk = seed_nsm_cob_ja_ko_zh(conn)
    glosses = {str(g.get("term") or "").lower(): g for g in load_prime_glosses()}
    builtins = load_builtin_term_ids()
    now = _now()
    wired = 0
    for p in load_primes():
        term = str(p.get("term") or "").strip()
        if not term:
            continue
        group = str(p.get("group") or "")
        gloss = glosses.get(term.lower(), {})
        form = logical_form_for_prime(term)
        causality = CAUSALITY_BY_TERM.get(term, "none")
        temporal = TEMPORAL_BY_TERM.get(term, "none")
        definition = str(gloss.get("nsmDefinition") or f"NSM prime ({group}): {term}")
        mech = str(gloss.get("mechanicalRole") or "Passthrough")
        entry_id = builtins.get(term.lower()) or builtin_urn(
            str(p.get("segment") or "noun"), term, language_code
        )
        descriptor = {
            "logicalForm": form,
            "causalityRole": causality,
            "temporalRole": temporal,
            "nsmGroup": group,
            "mechanicalRole": mech,
            "nsmPrime": True,
        }
        cur = conn.execute(
            "SELECT id FROM lemma_completion WHERE language_code = ? AND term = ?",
            (language_code, term),
        ).fetchone()
        if cur:
            conn.execute(
                """UPDATE lemma_completion SET
                    entry_id = COALESCE(entry_id, ?),
                    is_prime = 1,
                    is_builtin = 1,
                    is_implemented = 1,
                    nsm_definition = ?,
                    descriptor_json = ?,
                    updated_at = ?
                WHERE id = ?""",
                (entry_id, definition, json.dumps(descriptor), now, cur["id"] if isinstance(cur, sqlite3.Row) else cur[0]),
            )
        else:
            conn.execute(
                """INSERT INTO lemma_completion (
                    id, language_code, term, rank, entry_id, is_prime, is_builtin,
                    is_implemented, benefits_from_asset_store, nsm_definition,
                    composition_json, descriptor_json, updated_at
                ) VALUES (?, ?, ?, NULL, ?, 1, 1, 1, 0, ?, NULL, ?, ?)""",
                (
                    new_id("lc"),
                    language_code,
                    term,
                    entry_id,
                    definition,
                    json.dumps(descriptor),
                    now,
                ),
            )
        wired += 1
    conn.commit()
    return {
        "associations": assoc_n,
        "hedges": hedge_n,
        "primesWired": wired,
        "cobConjugations": cob.get("conjugations", 0),
        "cobRules": cob.get("rules", 0) + cob_cjk.get("rules", 0),
        "cobCjkRules": cob_cjk.get("rules", 0),
    }
