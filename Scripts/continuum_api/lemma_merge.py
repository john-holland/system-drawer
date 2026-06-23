"""Merge built-in vocabulary JSON with continuum DB thesaurus rows."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any

BUILTIN_URN_PREFIX = "urn:unity:continuum:builtin:v1:"
BUILTIN_JSON = Path(__file__).resolve().parent / "data" / "builtin_vocabulary.json"


def is_builtin_urn(entry_id: str | None) -> bool:
    return bool(entry_id) and entry_id.startswith(BUILTIN_URN_PREFIX)


def load_builtin_vocabulary() -> list[dict[str, Any]]:
    if not BUILTIN_JSON.is_file():
        return []
    data = json.loads(BUILTIN_JSON.read_text(encoding="utf-8"))
    return list(data.get("items") or [])


def find_builtin_entries_for_term(
    term: str,
    language_code: str = "en",
) -> list[dict[str, Any]]:
    """Return all built-in rows matching term and language (any POS)."""
    term_lc = (term or "").strip().lower()
    if not term_lc:
        return []
    lang = (language_code or "en").strip().lower()
    out: list[dict[str, Any]] = []
    for item in load_builtin_vocabulary():
        if (item.get("term") or "").lower() != term_lc:
            continue
        if (item.get("languageCode") or "en").lower() != lang:
            continue
        out.append(item)
    return out


def find_builtin_entry(
    term: str,
    language_code: str = "en",
    pos_tag: str | None = None,
) -> dict[str, Any] | None:
    """Return built-in vocabulary row matching term, language, and optional POS."""
    term_lc = (term or "").strip().lower()
    if not term_lc:
        return None
    lang = (language_code or "en").strip().lower()
    pos = (pos_tag or "").strip().lower()
    for item in load_builtin_vocabulary():
        if (item.get("term") or "").lower() != term_lc:
            continue
        if (item.get("languageCode") or "en").lower() != lang:
            continue
        item_pos = (item.get("posTag") or "").strip().lower()
        if pos and item_pos and item_pos != pos:
            continue
        return item
    return None


def _entry_view_from_builtin(row: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": row["id"],
        "term": row["term"],
        "posTag": row.get("posTag", ""),
        "languageCode": row.get("languageCode", "en"),
        "languageId": None,
        "definition": None,
        "synonyms": [],
        "tags": list(row.get("tags") or []),
        "isBuiltIn": True,
        "builtInCategory": row.get("builtInCategory"),
        "properties": {},
        "clauseCount": 0,
        "linkedAssetIds": [],
        "components": [],
        "componentCreation": None,
    }


def _load_db_custom_entries(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    cur = conn.execute(
        """
        SELECT e.id, e.term, e.pos_tag, e.language_id, l.code AS language_code
        FROM thesaurus_entries e
        LEFT JOIN languages l ON l.id = e.language_id
        WHERE e.id NOT LIKE ?
        ORDER BY e.term
        """,
        (BUILTIN_URN_PREFIX + "%",),
    )
    items: list[dict[str, Any]] = []
    for r in cur.fetchall():
        items.append(
            {
                "id": r["id"],
                "term": r["term"],
                "posTag": r["pos_tag"],
                "languageCode": (r["language_code"] or "en").lower(),
                "languageId": r["language_id"],
                "definition": None,
                "synonyms": [],
                "tags": [],
                "isBuiltIn": False,
                "builtInCategory": None,
                "properties": {},
                "clauseCount": 0,
                "linkedAssetIds": [],
                "components": [],
                "componentCreation": None,
            }
        )
    return items


def _load_enrichment(conn: sqlite3.Connection) -> tuple[dict, dict, dict, dict, dict]:
    """definitions, alternatives, properties, clause_counts keyed by entry_id."""
    definitions: dict[str, str] = {}
    try:
        cur = conn.execute(
            """
            SELECT entry_id, definition FROM dictionary_definitions
            WHERE id IN (
                SELECT MAX(id) FROM dictionary_definitions GROUP BY entry_id
            )
            """
        )
        for r in cur.fetchall():
            definitions[r["entry_id"]] = r["definition"]
    except sqlite3.OperationalError:
        pass

    alternatives: dict[str, list[str]] = {}
    try:
        cur = conn.execute(
            "SELECT entry_id, form FROM thesaurus_alternatives WHERE role IS NULL OR role = 'synonym'"
        )
        for r in cur.fetchall():
            alternatives.setdefault(r["entry_id"], []).append(r["form"])
    except sqlite3.OperationalError:
        pass

    properties: dict[str, dict[str, str]] = {}
    try:
        cur = conn.execute("SELECT entry_id, property_key, property_value FROM thesaurus_entry_properties")
        for r in cur.fetchall():
            eid = r["entry_id"]
            properties.setdefault(eid, {})[r["property_key"]] = r["property_value"]
    except sqlite3.OperationalError:
        pass

    clause_counts: dict[str, int] = {}
    try:
        cur = conn.execute(
            """
            SELECT selection_text, COUNT(*) AS c FROM localization_clause_bindings
            WHERE binding_kind = 'lemma'
            GROUP BY LOWER(TRIM(selection_text))
            """
        )
        for r in cur.fetchall():
            clause_counts[(r["selection_text"] or "").lower().strip()] = int(r["c"])
    except sqlite3.OperationalError:
        pass

    # Map entry_id -> clause count via properties / selection text match on term
    entry_clause: dict[str, int] = {}
    try:
        cur = conn.execute(
            """
            SELECT b.selection_text, COUNT(*) AS c
            FROM localization_clause_bindings b
            WHERE b.binding_kind = 'lemma'
            GROUP BY b.selection_text
            """
        )
        text_counts = { (r["selection_text"] or "").lower(): int(r["c"]) for r in cur.fetchall() }
        cur = conn.execute("SELECT id, term FROM thesaurus_entries")
        for r in cur.fetchall():
            c = text_counts.get((r["term"] or "").lower(), 0)
            if c:
                entry_clause[r["id"]] = c
        for item in load_builtin_vocabulary():
            c = text_counts.get((item.get("term") or "").lower(), 0)
            if c:
                entry_clause[item["id"]] = c
    except sqlite3.OperationalError:
        pass

    return definitions, alternatives, properties, clause_counts, entry_clause


def merge_vocabulary(conn: sqlite3.Connection) -> dict[str, dict[str, Any]]:
    """Return merged entry map keyed by id."""
    try:
        from continuum_api.lemma_component_metadata import component_creation_view, load_all_cache_maps
    except ImportError:
        from lemma_component_metadata import component_creation_view, load_all_cache_maps

    merged: dict[str, dict[str, Any]] = {}
    for row in load_builtin_vocabulary():
        merged[row["id"]] = _entry_view_from_builtin(row)

    for row in _load_db_custom_entries(conn):
        merged[row["id"]] = row

    definitions, alternatives, properties, _, entry_clause = _load_enrichment(conn)

    for eid, view in merged.items():
        if eid in definitions and definitions[eid]:
            view["definition"] = definitions[eid]
        if eid in alternatives:
            syns = alternatives[eid]
            if view["isBuiltIn"]:
                existing = set(view.get("synonyms") or [])
                for s in syns:
                    if s not in existing:
                        view.setdefault("synonyms", []).append(s)
            else:
                view["synonyms"] = syns
        if eid in properties:
            view["properties"] = dict(properties[eid])
        elif not view.get("properties"):
            view["properties"] = {}

        # Also match enrichment by (language, term, pos) for built-ins
        if view["isBuiltIn"]:
            cur = conn.execute(
                """
                SELECT e.id FROM thesaurus_entries e
                JOIN languages l ON l.id = e.language_id
                WHERE e.term = ? AND e.pos_tag = ? AND l.code = ?
                LIMIT 1
                """,
                (view["term"], view["posTag"], view["languageCode"]),
            )
            match = cur.fetchone()
            if match:
                mid = match["id"]
                if mid in definitions and not view.get("definition"):
                    view["definition"] = definitions[mid]
                if mid in alternatives:
                    for s in alternatives[mid]:
                        if s not in view.setdefault("synonyms", []):
                            view["synonyms"].append(s)
                if mid in properties:
                    view["properties"].update(properties[mid])

        comps = list(view["properties"].keys())
        view["components"] = comps
        prefab = view["properties"].get("prefab-id")
        if prefab:
            view["linkedAssetIds"] = [prefab]
        view["clauseCount"] = entry_clause.get(eid, 0)
        if not view["clauseCount"]:
            view["clauseCount"] = entry_clause.get(
                next((k for k, v in merged.items() if v is view), ""), 0
            )
        term_lc = (view.get("term") or "").lower()
        for eid2, c in entry_clause.items():
            if eid2 == eid:
                view["clauseCount"] = c
                break
        # fallback: count by term in clause selection
        try:
            cur = conn.execute(
                """
                SELECT COUNT(*) AS c FROM localization_clause_bindings
                WHERE binding_kind = 'lemma' AND LOWER(selection_text) = ?
                """,
                (term_lc,),
            )
            row = cur.fetchone()
            if row and int(row["c"]) > 0:
                view["clauseCount"] = int(row["c"])
        except sqlite3.OperationalError:
            pass

    cache_by_entry = load_all_cache_maps(conn)

    for eid, view in merged.items():
        cache = cache_by_entry.get(eid)
        view["componentCreation"] = component_creation_view(cache)

    return merged


def filter_entries(
    entries: list[dict[str, Any]],
    *,
    q: str | None = None,
    language: str | None = None,
    pos: str | None = None,
    source: str | None = None,
    property_key: str | None = None,
    has_clause: bool | None = None,
    component: str | None = None,
    component_type: str | None = None,
    bucket_id: str | None = None,
    causality_leaf: str | None = None,
    has_component_metadata: bool | None = None,
) -> list[dict[str, Any]]:
    q_lc = (q or "").strip().lower()
    lang = (language or "").strip().lower()
    pos_f = (pos or "").strip().lower()
    src = (source or "all").strip().lower()

    out: list[dict[str, Any]] = []
    for e in entries:
        if src == "builtin" and not e.get("isBuiltIn"):
            continue
        if src == "custom" and e.get("isBuiltIn"):
            continue
        if lang and (e.get("languageCode") or "").lower() != lang:
            continue
        if pos_f and pos_f not in (e.get("posTag") or "").lower():
            continue
        if property_key and property_key not in (e.get("properties") or {}):
            continue
        if component:
            props = e.get("properties") or {}
            if component not in props and component not in (e.get("components") or []):
                continue
        cc = e.get("componentCreation") or {}
        if component_type:
            types = cc.get("componentTypes") or []
            if not any(component_type.lower() in (t or "").lower() for t in types):
                continue
        if bucket_id:
            buckets = cc.get("bucketIds") or []
            if not any((b or "").startswith(bucket_id) for b in buckets):
                continue
        if causality_leaf:
            leaves = cc.get("causalityLeafIds") or []
            if causality_leaf not in leaves:
                continue
        if has_component_metadata is True and not cc:
            continue
        if has_component_metadata is False and cc:
            continue
        if has_clause is True and int(e.get("clauseCount") or 0) <= 0:
            continue
        if has_clause is False and int(e.get("clauseCount") or 0) > 0:
            continue
        if q_lc:
            hay = " ".join(
                [
                    e.get("term") or "",
                    e.get("posTag") or "",
                    e.get("definition") or "",
                    " ".join(e.get("synonyms") or []),
                    " ".join(e.get("tags") or []),
                    e.get("id") or "",
                ]
            ).lower()
            if q_lc not in hay:
                continue
        out.append(e)
    return out
