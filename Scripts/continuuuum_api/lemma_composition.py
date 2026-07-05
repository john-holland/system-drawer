"""CRUD helpers for thesaurus_entry_compositions."""

from __future__ import annotations

import json
import sqlite3
import uuid
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def _column_exists(conn: sqlite3.Connection, table: str, column: str) -> bool:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return any(r[1] == column for r in cur.fetchall())


def _ensure_localization_specs_table(conn: sqlite3.Connection) -> None:
    """Lemma composition/prompt SQL inserts property specs; ensure the table exists."""
    if _table_exists(conn, "localization_property_specs"):
        return
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS localization_property_specs (
            key TEXT PRIMARY KEY,
            value_type TEXT NOT NULL,
            allowed_values_json TEXT,
            default_value TEXT,
            description TEXT
        )
        """
    )
    conn.commit()


def ensure_lemma_composition_schema(conn: sqlite3.Connection) -> None:
    """Create composed-lemma and spatial tables when missing (idempotent)."""
    _ensure_localization_specs_table(conn)
    changed = False
    if not _table_exists(conn, "spatial_4d"):
        spatial_sql = (_SCHEMA_ROOT / "continuuuum_spatial_4d_schema.sql").read_text(encoding="utf-8")
        conn.executescript(spatial_sql)
        changed = True
    if not _table_exists(conn, "thesaurus_entry_compositions"):
        comp_sql = (_SCHEMA_ROOT / "continuuuum_lemma_composition_schema.sql").read_text(encoding="utf-8")
        conn.executescript(comp_sql)
        changed = True
    if _table_exists(conn, "thesaurus_entry_compositions"):
        for col in ("patch_properties_json", "timing_override_json"):
            if not _column_exists(conn, "thesaurus_entry_compositions", col):
                conn.execute(f"ALTER TABLE thesaurus_entry_compositions ADD COLUMN {col} TEXT")
                changed = True
    if changed:
        conn.commit()


def _parse_json_field(raw: str | None) -> dict[str, Any] | None:
    if not raw:
        return None
    try:
        data = json.loads(raw)
        return data if isinstance(data, dict) else None
    except json.JSONDecodeError:
        return None


def _row_to_child(row: sqlite3.Row, terms: dict[str, str]) -> dict[str, Any]:
    farey = None
    raw = row["anchor_farey_json"]
    if raw:
        try:
            farey = json.loads(raw)
        except json.JSONDecodeError:
            farey = None
    child_id = row["child_entry_id"]
    out: dict[str, Any] = {
        "id": row["id"],
        "entryId": child_id,
        "term": terms.get(child_id, ""),
        "sortOrder": int(row["sort_order"] or 0),
        "spatial4dId": row["spatial_4d_id"],
        "anchorText": row["anchor_text"],
        "anchorFarey": farey,
        "draftEpisodeId": row["draft_episode_id"],
    }
    keys = row.keys() if hasattr(row, "keys") else []
    if "patch_properties_json" in keys:
        patch = _parse_json_field(row["patch_properties_json"])
        if patch:
            out["patchProperties"] = patch
    if "timing_override_json" in keys:
        timing = _parse_json_field(row["timing_override_json"])
        if timing:
            out["timingOverride"] = timing
    return out


def _load_terms(conn: sqlite3.Connection, entry_ids: list[str] | None = None) -> dict[str, str]:
    terms: dict[str, str] = {}
    if entry_ids:
        if not entry_ids:
            return terms
        placeholders = ",".join("?" * len(entry_ids))
        cur = conn.execute(
            f"SELECT id, term FROM thesaurus_entries WHERE id IN ({placeholders})",
            entry_ids,
        )
        for r in cur.fetchall():
            terms[r["id"]] = r["term"] or ""
        return terms
    cur = conn.execute("SELECT id, term FROM thesaurus_entries")
    for r in cur.fetchall():
        terms[r["id"]] = r["term"] or ""
    return terms


def load_composition(conn: sqlite3.Connection, parent_entry_id: str) -> dict[str, Any]:
    ensure_lemma_composition_schema(conn)
    cols = "id, child_entry_id, sort_order, spatial_4d_id, anchor_text, anchor_farey_json, draft_episode_id"
    if _column_exists(conn, "thesaurus_entry_compositions", "patch_properties_json"):
        cols += ", patch_properties_json, timing_override_json"
    cur = conn.execute(
        f"""
        SELECT {cols}
        FROM thesaurus_entry_compositions
        WHERE parent_entry_id = ?
        ORDER BY sort_order, child_entry_id
        """,
        (parent_entry_id,),
    )
    rows = cur.fetchall()
    child_ids = [r["child_entry_id"] for r in rows]
    terms = _load_terms(conn, child_ids)
    children = [_row_to_child(r, terms) for r in rows]
    return {
        "parentEntryId": parent_entry_id,
        "children": children,
        "isComposedLemma": len(children) > 0,
    }


def _child_graph(conn: sqlite3.Connection) -> dict[str, list[str]]:
    graph: dict[str, list[str]] = {}
    cur = conn.execute("SELECT parent_entry_id, child_entry_id FROM thesaurus_entry_compositions")
    for r in cur.fetchall():
        graph.setdefault(r["parent_entry_id"], []).append(r["child_entry_id"])
    return graph


def would_create_cycle(conn: sqlite3.Connection, parent_id: str, child_id: str) -> bool:
    """True if adding parent→child would create a cycle (child is ancestor of parent)."""
    if parent_id == child_id:
        return True
    graph = _child_graph(conn)
    stack = [child_id]
    seen = {child_id}
    while stack:
        node = stack.pop()
        for nxt in graph.get(node, []):
            if nxt == parent_id:
                return True
            if nxt not in seen:
                seen.add(nxt)
                stack.append(nxt)
    return False


def validate_children(conn: sqlite3.Connection, parent_entry_id: str, children: list[dict[str, Any]]) -> str | None:
    if not children:
        return None
    seen: set[str] = set()
    for i, item in enumerate(children):
        child_id = (item.get("entryId") or item.get("childEntryId") or "").strip()
        if not child_id:
            return f"Child at index {i} is missing entryId"
        if child_id in seen:
            return f"Duplicate child entry {child_id}"
        seen.add(child_id)
        if would_create_cycle(conn, parent_entry_id, child_id):
            return f"Composition cycle detected: {child_id} is an ancestor of {parent_entry_id}"
        cur = conn.execute("SELECT id FROM thesaurus_entries WHERE id = ?", (child_id,))
        if not cur.fetchone():
            try:
                from continuuuum_api.lemma_merge import merge_vocabulary
            except ImportError:
                from lemma_merge import merge_vocabulary
            if child_id not in merge_vocabulary(conn):
                return f"Unknown child entry id: {child_id}"
    return None


def replace_composition(
    conn: sqlite3.Connection,
    parent_entry_id: str,
    children: list[dict[str, Any]],
) -> dict[str, Any]:
    ensure_lemma_composition_schema(conn)
    err = validate_children(conn, parent_entry_id, children)
    if err:
        raise ValueError(err)

    cur = conn.execute(
        "SELECT id FROM thesaurus_entries WHERE id = ?",
        (parent_entry_id,),
    )
    if not cur.fetchone():
        raise ValueError(f"Parent entry not found: {parent_entry_id}")

    conn.execute(
        "DELETE FROM thesaurus_entry_compositions WHERE parent_entry_id = ?",
        (parent_entry_id,),
    )

    for i, item in enumerate(children):
        child_id = (item.get("entryId") or item.get("childEntryId") or "").strip()
        sort_order = int(item.get("sortOrder", i))
        conn.execute(
            """
            INSERT INTO thesaurus_entry_compositions
                (id, parent_entry_id, child_entry_id, sort_order, spatial_4d_id, anchor_text, anchor_farey_json, draft_episode_id)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (
                str(uuid.uuid4()),
                parent_entry_id,
                child_id,
                sort_order,
                item.get("spatial4dId") or item.get("spatial_4d_id"),
                item.get("anchorText") or item.get("anchor_text"),
                json.dumps(item["anchorFarey"]) if item.get("anchorFarey") else item.get("anchor_farey_json"),
                item.get("draftEpisodeId") or item.get("draft_episode_id"),
            ),
        )

    summary = json.dumps([c.get("entryId") or c.get("childEntryId") for c in children])
    conn.execute(
        """
        INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value)
        VALUES (?, 'lemma-composition', ?)
        ON CONFLICT(entry_id, property_key) DO UPDATE SET property_value = excluded.property_value
        """,
        (parent_entry_id, summary),
    )
    if not children:
        conn.execute(
            "DELETE FROM thesaurus_entry_properties WHERE entry_id = ? AND property_key = 'lemma-composition'",
            (parent_entry_id,),
        )

    return load_composition(conn, parent_entry_id)


def load_all_parent_children(conn: sqlite3.Connection) -> dict[str, list[dict[str, Any]]]:
    """Map parent_entry_id -> list of child rows (minimal, for merge)."""
    ensure_lemma_composition_schema(conn)
    out: dict[str, list[dict[str, Any]]] = {}
    cur = conn.execute(
        """
        SELECT parent_entry_id, child_entry_id, sort_order
        FROM thesaurus_entry_compositions
        ORDER BY parent_entry_id, sort_order, child_entry_id
        """
    )
    rows = cur.fetchall()
    child_ids = list({r["child_entry_id"] for r in rows})
    terms = _load_terms(conn, child_ids)
    for r in rows:
        pid = r["parent_entry_id"]
        out.setdefault(pid, []).append(
            {
                "entryId": r["child_entry_id"],
                "term": terms.get(r["child_entry_id"], ""),
                "sortOrder": int(r["sort_order"] or 0),
            }
        )
    return out
