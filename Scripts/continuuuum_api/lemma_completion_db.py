"""Lemma completion schema helpers + seed from NSM primes and common words."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent
_DATA_DIR = Path(__file__).resolve().parent / "data"
PRIMES_PATH = _DATA_DIR / "nsm_semantic_primes_en.json"
GLOSSES_PATH = _DATA_DIR / "nsm_semantic_prime_glosses_en.json"
COMMON_WORDS_PATH = _DATA_DIR / "common_words_en_5000.json"
BUILTIN_PATH = _DATA_DIR / "builtin_vocabulary.json"
SCHEMA_PATH = _SCHEMA_ROOT / "continuuuum_lemma_completion_schema.sql"

BUILTIN_URN_PREFIX = "urn:unity:continuuuum:builtin:v1:"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def new_id(prefix: str = "lc") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_lemma_completion_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "lemma_completion"):
        sql = SCHEMA_PATH.read_text(encoding="utf-8")
        conn.executescript(sql)
    cols = {
        r[1]
        for r in conn.execute("PRAGMA table_info(lemma_completion)").fetchall()
    }
    if "is_prime" not in cols:
        conn.execute(
            "ALTER TABLE lemma_completion ADD COLUMN is_prime INTEGER NOT NULL DEFAULT 0"
        )
    conn.commit()


def _slug(s: str) -> str:
    out: list[str] = []
    last_us = False
    for c in s:
        if c.isalnum():
            out.append(c.lower())
            last_us = False
        elif c in "_- " and out and not last_us:
            out.append("_")
            last_us = True
    r = "".join(out).strip("_")
    return r or "_"


def builtin_urn(segment: str, term: str, lang: str = "en") -> str:
    return f"{BUILTIN_URN_PREFIX}/{lang}/{_slug(segment)}/{_slug(term)}"


def load_primes() -> list[dict[str, Any]]:
    data = json.loads(PRIMES_PATH.read_text(encoding="utf-8"))
    return list(data.get("primes") or [])


def load_prime_glosses() -> list[dict[str, Any]]:
    if not GLOSSES_PATH.is_file():
        return []
    data = json.loads(GLOSSES_PATH.read_text(encoding="utf-8"))
    return list(data.get("primes") or [])


def load_common_words() -> list[dict[str, Any]]:
    data = json.loads(COMMON_WORDS_PATH.read_text(encoding="utf-8"))
    return list(data.get("words") or [])


def load_builtin_items() -> list[dict[str, Any]]:
    """Raw items from Unity-exported builtin_vocabulary.json."""
    if not BUILTIN_PATH.is_file():
        return []
    data = json.loads(BUILTIN_PATH.read_text(encoding="utf-8"))
    return list(data.get("items") or [])


def load_builtin_term_ids() -> dict[str, str]:
    """Map lowercase term -> first builtin id."""
    out: dict[str, str] = {}
    for item in load_builtin_items():
        term = str(item.get("term") or "").lower()
        if term and term not in out:
            out[term] = str(item["id"])
    return out


def _term_aliases(term: str) -> list[str]:
    t = (term or "").strip().lower()
    if not t:
        return []
    variants = {
        t,
        t.replace("-", "_"),
        t.replace("_", "-"),
        t.replace("-", " "),
        t.replace("_", " "),
        t.replace(" ", "-"),
        t.replace(" ", "_"),
    }
    return [v for v in variants if v]


def sync_builtin_implementation(
    conn: sqlite3.Connection, language_code: str = "en"
) -> dict[str, int]:
    """Mark Unity builtin vocabulary terms as builtin + implemented in lemma_completion.

    Source of truth: ``builtin_vocabulary.json`` (export of VocabularyBuiltInRegistry).
    Inserts missing compound terms (e.g. gas-station) that are not in the common-5000 list.
    """
    ensure_lemma_completion_schema(conn)
    now = _now()
    updated = 0
    inserted = 0
    seen_terms: set[str] = set()

    for item in load_builtin_items():
        term = str(item.get("term") or "").strip()
        if not term:
            continue
        term_key = term.lower()
        if term_key in seen_terms:
            continue
        seen_terms.add(term_key)
        entry_id = str(item.get("id") or "") or None
        pos_tag = str(item.get("posTag") or "").strip() or None
        aliases = _term_aliases(term)
        placeholders = ",".join("?" for _ in aliases)
        row = conn.execute(
            f"""SELECT id, term FROM lemma_completion
                WHERE language_code = ? AND lower(term) IN ({placeholders})
                ORDER BY CASE WHEN lower(term) = ? THEN 0 ELSE 1 END
                LIMIT 1""",
            (language_code, *aliases, term_key),
        ).fetchone()
        if row:
            conn.execute(
                """UPDATE lemma_completion SET
                     entry_id = COALESCE(?, entry_id),
                     is_builtin = 1,
                     is_implemented = 1,
                     updated_at = ?
                   WHERE id = ?""",
                (entry_id, now, row["id"]),
            )
            updated += 1
        else:
            segment = "noun"
            if pos_tag:
                pt = pos_tag.lower()
                if pt in ("verb",):
                    segment = "verb"
                elif pt in ("adjective", "adj"):
                    segment = "adj"
                elif pt in ("adverb", "adv"):
                    segment = "adv"
                elif pt in ("preposition", "prep"):
                    segment = "prep"
                elif pt in ("conjunction", "conj"):
                    segment = "conj"
                elif pt in ("determiner", "article"):
                    segment = "det"
                elif pt in ("type_name",):
                    segment = "literal"
                elif pt in ("pronoun",):
                    segment = "pron"
                elif pt in ("numeral",):
                    segment = "num"
            desc = None
            if pos_tag or item.get("builtInCategory") or item.get("tags"):
                desc = json.dumps(
                    {
                        "posTag": pos_tag,
                        "builtInCategory": item.get("builtInCategory"),
                        "tags": item.get("tags") or [],
                        "unityBuiltin": True,
                    },
                    ensure_ascii=False,
                )
            conn.execute(
                """INSERT INTO lemma_completion
                   (id, language_code, term, rank, entry_id, is_prime, is_builtin, is_implemented,
                    benefits_from_asset_store, nsm_definition, composition_json, descriptor_json, updated_at)
                   VALUES (?,?,?,?,?,0,1,1,0,NULL,NULL,?,?)""",
                (
                    new_id(),
                    language_code,
                    term,
                    None,
                    entry_id or builtin_urn(segment, term, language_code),
                    desc,
                    now,
                ),
            )
            inserted += 1

    conn.commit()
    return {"builtinSynced": updated, "builtinInserted": inserted, "builtinTerms": len(seen_terms)}


def seed_lemma_completion(conn: sqlite3.Connection, language_code: str = "en") -> dict[str, int]:
    """Idempotent seed of primes + common words. Returns insert/skip counts."""
    ensure_lemma_completion_schema(conn)
    builtins = load_builtin_term_ids()
    now = _now()
    inserted = 0
    skipped = 0

    for p in load_primes():
        term = str(p.get("term") or "").strip()
        if not term:
            continue
        entry_id = builtin_urn(str(p.get("segment") or "noun"), term, language_code)
        # Prefer existing registry URN if term already present under another segment
        entry_id = builtins.get(term.lower(), entry_id)
        cur = conn.execute(
            "SELECT id FROM lemma_completion WHERE language_code = ? AND term = ?",
            (language_code, term),
        ).fetchone()
        if cur:
            skipped += 1
            continue
        conn.execute(
            """INSERT INTO lemma_completion
               (id, language_code, term, rank, entry_id, is_prime, is_builtin, is_implemented,
                benefits_from_asset_store, nsm_definition, composition_json, descriptor_json, updated_at)
               VALUES (?,?,?,?,?,1,1,0,0,NULL,NULL,NULL,?)""",
            (new_id(), language_code, term, None, entry_id, now),
        )
        inserted += 1

    for w in load_common_words():
        term = str(w.get("word") or "").strip()
        if not term:
            continue
        rank = w.get("rank")
        try:
            rank_i = int(rank) if rank is not None else None
        except (TypeError, ValueError):
            rank_i = None
        cur = conn.execute(
            "SELECT id FROM lemma_completion WHERE language_code = ? AND term = ?",
            (language_code, term),
        ).fetchone()
        if cur:
            # Fill rank if missing (primes that also appear in the common list)
            if rank_i is not None:
                conn.execute(
                    """UPDATE lemma_completion
                       SET rank = COALESCE(rank, ?),
                           entry_id = COALESCE(entry_id, ?),
                           updated_at = ?
                       WHERE language_code = ? AND term = ?""",
                    (rank_i, builtins.get(term.lower()), now, language_code, term),
                )
            skipped += 1
            continue
        is_builtin = 1 if term.lower() in builtins else 0
        conn.execute(
            """INSERT INTO lemma_completion
               (id, language_code, term, rank, entry_id, is_prime, is_builtin, is_implemented,
                benefits_from_asset_store, nsm_definition, composition_json, descriptor_json, updated_at)
               VALUES (?,?,?,?,?,0,?,0,0,NULL,NULL,NULL,?)""",
            (
                new_id(),
                language_code,
                term,
                rank_i,
                builtins.get(term.lower()),
                is_builtin,
                now,
            ),
        )
        inserted += 1

    glossed = seed_prime_glosses(conn, language_code=language_code)
    conn.commit()
    sync = sync_builtin_implementation(conn, language_code=language_code)
    return {
        "inserted": inserted,
        "skipped": skipped,
        "glossesUpdated": glossed,
        **sync,
    }


def seed_prime_glosses(conn: sqlite3.Connection, language_code: str = "en") -> int:
    """Upsert hand-authored NSM glosses; mark primes defined/builtin/implemented."""
    ensure_lemma_completion_schema(conn)
    primes_by_term = {
        str(p.get("term") or "").strip(): p for p in load_primes() if p.get("term")
    }
    builtins = load_builtin_term_ids()
    now = _now()
    updated = 0

    for g in load_prime_glosses():
        term = str(g.get("term") or "").strip()
        if not term:
            continue
        meta = primes_by_term.get(term) or {}
        pos = str(g.get("posTag") or meta.get("posTag") or "unknown")
        role = str(g.get("mechanicalRole") or "AtomicSubject")
        definition = str(g.get("nsmDefinition") or "").strip()
        if not definition:
            continue
        entry_id = builtins.get(term.lower()) or builtin_urn(
            str(meta.get("segment") or "noun"), term, language_code
        )
        descriptor = {
            "lemma": term,
            "posTag": pos,
            "mechanicalRole": role,
            "outputTier": 0,
            "nsmDefinition": definition,
            "functionalDescription": definition,
            "compositionChildren": [],
            "properties": [],
        }
        desc_s = json.dumps(descriptor, ensure_ascii=False)
        existing = conn.execute(
            "SELECT id FROM lemma_completion WHERE language_code = ? AND term = ?",
            (language_code, term),
        ).fetchone()
        if existing:
            conn.execute(
                """UPDATE lemma_completion SET
                     entry_id = COALESCE(?, entry_id),
                     is_prime = 1,
                     is_builtin = 1,
                     is_implemented = 1,
                     nsm_definition = ?,
                     descriptor_json = ?,
                     updated_at = ?
                   WHERE id = ?""",
                (entry_id, definition, desc_s, now, existing["id"]),
            )
        else:
            conn.execute(
                """INSERT INTO lemma_completion
                   (id, language_code, term, rank, entry_id, is_prime, is_builtin, is_implemented,
                    benefits_from_asset_store, nsm_definition, composition_json, descriptor_json, updated_at)
                   VALUES (?,?,?,?,?,1,1,1,0,?,NULL,?,?)""",
                (new_id(), language_code, term, None, entry_id, definition, desc_s, now),
            )
        updated += 1

    conn.commit()
    return updated


def row_to_entry(row: sqlite3.Row | dict) -> dict[str, Any]:
    r = dict(row)
    pos_tag = None
    descriptor = None
    raw_desc = r.get("descriptor_json")
    if raw_desc:
        try:
            descriptor = json.loads(raw_desc)
            if isinstance(descriptor, dict):
                pos_tag = descriptor.get("posTag") or descriptor.get("pos_tag")
        except (TypeError, json.JSONDecodeError):
            descriptor = None
    composition = None
    raw_comp = r.get("composition_json")
    if raw_comp:
        try:
            composition = json.loads(raw_comp)
        except (TypeError, json.JSONDecodeError):
            composition = raw_comp
    return {
        "id": r["id"],
        "languageCode": r.get("language_code") or "en",
        "term": r["term"],
        "rank": r.get("rank"),
        "entryId": r.get("entry_id"),
        "isPrime": bool(r.get("is_prime")),
        "isBuiltin": bool(r.get("is_builtin")),
        "isImplemented": bool(r.get("is_implemented")),
        "benefitsFromAssetStore": bool(r.get("benefits_from_asset_store")),
        "nsmDefinition": r.get("nsm_definition"),
        "composition": composition,
        "descriptor": descriptor,
        "posTag": pos_tag,
        "updatedAt": r.get("updated_at"),
        "isDefined": bool((r.get("nsm_definition") or "").strip()),
    }


def summary(conn: sqlite3.Connection, scope: str = "all", language_code: str = "en") -> dict[str, Any]:
    ensure_lemma_completion_schema(conn)
    where = "language_code = ?"
    params: list[Any] = [language_code]
    if scope == "common5000":
        where += " AND rank IS NOT NULL"
    elif scope == "primes":
        where += " AND is_prime = 1"

    total = conn.execute(
        f"SELECT COUNT(*) AS c FROM lemma_completion WHERE {where}", params
    ).fetchone()["c"]
    defined = conn.execute(
        f"""SELECT COUNT(*) AS c FROM lemma_completion
            WHERE {where} AND nsm_definition IS NOT NULL AND TRIM(nsm_definition) != ''""",
        params,
    ).fetchone()["c"]
    builtin = conn.execute(
        f"SELECT COUNT(*) AS c FROM lemma_completion WHERE {where} AND is_builtin = 1",
        params,
    ).fetchone()["c"]
    implemented = conn.execute(
        f"SELECT COUNT(*) AS c FROM lemma_completion WHERE {where} AND is_implemented = 1",
        params,
    ).fetchone()["c"]
    asset_store = conn.execute(
        f"""SELECT COUNT(*) AS c FROM lemma_completion
            WHERE {where} AND benefits_from_asset_store = 1""",
        params,
    ).fetchone()["c"]

    def pct(n: int) -> float:
        return round(100.0 * n / total, 2) if total else 0.0

    # Inventory progress: builtin or implemented counts as "done" for overall %.
    progressed = conn.execute(
        f"""SELECT COUNT(*) AS c FROM lemma_completion
            WHERE {where} AND (is_builtin = 1 OR is_implemented = 1)""",
        params,
    ).fetchone()["c"]

    return {
        "scope": scope,
        "languageCode": language_code,
        "total": total,
        "defined": defined,
        "builtin": builtin,
        "implemented": implemented,
        "assetStore": asset_store,
        "progressed": progressed,
        "percentDefined": pct(defined),
        "percentBuiltin": pct(builtin),
        "percentImplemented": pct(implemented),
        "percentAssetStore": pct(asset_store),
        "percentOverall": pct(progressed),
    }


def list_entries(
    conn: sqlite3.Connection,
    *,
    language_code: str = "en",
    q: str | None = None,
    missing_definition: bool = False,
    not_implemented: bool = False,
    asset_store: bool | None = None,
    is_builtin: bool | None = None,
    is_prime: bool | None = None,
    has_rank: bool | None = None,
    rank_min: int | None = None,
    rank_max: int | None = None,
    limit: int = 50,
    offset: int = 0,
) -> tuple[list[dict[str, Any]], int]:
    ensure_lemma_completion_schema(conn)
    clauses = ["language_code = ?"]
    params: list[Any] = [language_code]
    if q:
        clauses.append("term LIKE ?")
        params.append(f"%{q}%")
    if missing_definition:
        clauses.append("(nsm_definition IS NULL OR TRIM(nsm_definition) = '')")
    if not_implemented:
        clauses.append("is_implemented = 0")
    if asset_store is True:
        clauses.append("benefits_from_asset_store = 1")
    elif asset_store is False:
        clauses.append("benefits_from_asset_store = 0")
    if is_builtin is True:
        clauses.append("is_builtin = 1")
    elif is_builtin is False:
        clauses.append("is_builtin = 0")
    if is_prime is True:
        clauses.append("is_prime = 1")
    elif is_prime is False:
        clauses.append("is_prime = 0")
    if has_rank is True:
        clauses.append("rank IS NOT NULL")
    elif has_rank is False:
        clauses.append("rank IS NULL")
    if rank_min is not None:
        clauses.append("rank >= ?")
        params.append(rank_min)
    if rank_max is not None:
        clauses.append("rank <= ?")
        params.append(rank_max)

    where = " AND ".join(clauses)
    total = conn.execute(
        f"SELECT COUNT(*) AS c FROM lemma_completion WHERE {where}", params
    ).fetchone()["c"]
    rows = conn.execute(
        f"""SELECT * FROM lemma_completion WHERE {where}
            ORDER BY CASE WHEN rank IS NULL THEN 0 ELSE 1 END, rank ASC, term ASC
            LIMIT ? OFFSET ?""",
        [*params, limit, offset],
    ).fetchall()
    return [row_to_entry(r) for r in rows], int(total)


def patch_entry(conn: sqlite3.Connection, entry_id: str, body: dict[str, Any]) -> dict[str, Any] | None:
    ensure_lemma_completion_schema(conn)
    row = conn.execute(
        "SELECT * FROM lemma_completion WHERE id = ?", (entry_id,)
    ).fetchone()
    if row is None:
        return None

    fields: list[str] = []
    params: list[Any] = []
    mapping = {
        "isBuiltin": "is_builtin",
        "isImplemented": "is_implemented",
        "benefitsFromAssetStore": "benefits_from_asset_store",
        "entryId": "entry_id",
        "nsmDefinition": "nsm_definition",
        "compositionJson": "composition_json",
        "descriptorJson": "descriptor_json",
        "rank": "rank",
    }
    for camel, col in mapping.items():
        if camel not in body:
            continue
        val = body[camel]
        if col in ("is_builtin", "is_implemented", "benefits_from_asset_store"):
            val = 1 if val else 0
        elif col in ("composition_json", "descriptor_json") and val is not None and not isinstance(val, str):
            val = json.dumps(val)
        fields.append(f"{col} = ?")
        params.append(val)

    if not fields:
        return row_to_entry(row)

    fields.append("updated_at = ?")
    params.append(_now())
    params.append(entry_id)
    conn.execute(
        f"UPDATE lemma_completion SET {', '.join(fields)} WHERE id = ?",
        params,
    )
    conn.commit()
    row = conn.execute(
        "SELECT * FROM lemma_completion WHERE id = ?", (entry_id,)
    ).fetchone()
    return row_to_entry(row)


def upsert_definition(
    conn: sqlite3.Connection,
    *,
    term: str,
    language_code: str = "en",
    rank: int | None = None,
    nsm_definition: str | None = None,
    composition: Any = None,
    descriptor: Any = None,
    entry_id: str | None = None,
) -> dict[str, Any]:
    ensure_lemma_completion_schema(conn)
    now = _now()
    comp_s = json.dumps(composition) if composition is not None and not isinstance(composition, str) else composition
    desc_s = json.dumps(descriptor) if descriptor is not None and not isinstance(descriptor, str) else descriptor
    existing = conn.execute(
        "SELECT * FROM lemma_completion WHERE language_code = ? AND term = ?",
        (language_code, term),
    ).fetchone()
    if existing:
        conn.execute(
            """UPDATE lemma_completion SET
                 rank = COALESCE(?, rank),
                 entry_id = COALESCE(?, entry_id),
                 nsm_definition = COALESCE(?, nsm_definition),
                 composition_json = COALESCE(?, composition_json),
                 descriptor_json = COALESCE(?, descriptor_json),
                 updated_at = ?
               WHERE id = ?""",
            (rank, entry_id, nsm_definition, comp_s, desc_s, now, existing["id"]),
        )
        conn.commit()
        row = conn.execute(
            "SELECT * FROM lemma_completion WHERE id = ?", (existing["id"],)
        ).fetchone()
        return row_to_entry(row)

    eid = new_id()
    builtins = load_builtin_term_ids()
    conn.execute(
        """INSERT INTO lemma_completion
           (id, language_code, term, rank, entry_id, is_prime, is_builtin, is_implemented,
            benefits_from_asset_store, nsm_definition, composition_json, descriptor_json, updated_at)
           VALUES (?,?,?,?,?,0,?,0,0,?,?,?,?)""",
        (
            eid,
            language_code,
            term,
            rank,
            entry_id or builtins.get(term.lower()),
            1 if term.lower() in builtins else 0,
            nsm_definition,
            comp_s,
            desc_s,
            now,
        ),
    )
    conn.commit()
    row = conn.execute("SELECT * FROM lemma_completion WHERE id = ?", (eid,)).fetchone()
    return row_to_entry(row)
