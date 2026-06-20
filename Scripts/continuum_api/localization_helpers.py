"""Localization workflow helpers — merge, guards, property validation."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable, Dict, List, Optional, Tuple

from thesaurus.clause_audit import char_to_farey
from thesaurus.clause_ref import BINDING_KINDS, ClauseRef
from thesaurus.script_edit_diff import DiffItem

CreateNotification = Callable[..., str]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_clause_binding_columns(conn: sqlite3.Connection) -> None:
    try:
        conn.execute(
            "ALTER TABLE localization_clause_bindings ADD COLUMN entry_id TEXT REFERENCES thesaurus_entries(id) ON DELETE SET NULL"
        )
    except sqlite3.OperationalError:
        pass
    try:
        conn.execute("CREATE INDEX IF NOT EXISTS idx_clause_bindings_entry ON localization_clause_bindings(entry_id)")
    except sqlite3.OperationalError:
        pass


def get_active_change_list(conn: sqlite3.Connection, draft_episode_id: str) -> Optional[sqlite3.Row]:
    cur = conn.execute(
        """SELECT * FROM localization_change_lists
           WHERE draft_episode_id = ?
           AND workflow_status IN ('new', 'in_progress', 'in_review', 'submitted')
           ORDER BY created_at DESC LIMIT 1""",
        (draft_episode_id,),
    )
    return cur.fetchone()


def draft_blocks_author_edit(conn: sqlite3.Connection, draft_episode_id: str) -> Optional[str]:
    row = get_active_change_list(conn, draft_episode_id)
    if row and row["workflow_status"] in ("in_review", "submitted"):
        return row["workflow_status"]
    return None


def validate_property_value(conn: sqlite3.Connection, property_key: str, property_value: str) -> Optional[str]:
    cur = conn.execute(
        "SELECT value_type, allowed_values_json FROM localization_property_specs WHERE key = ?",
        (property_key,),
    )
    row = cur.fetchone()
    if not row:
        return None
    vtype = (row["value_type"] or "").lower()
    if vtype == "bool":
        if str(property_value).lower() not in ("true", "false", "1", "0"):
            return "bool value must be true or false"
    allowed = row["allowed_values_json"]
    if allowed:
        try:
            vals = json.loads(allowed)
            if isinstance(vals, list) and property_value not in [str(v) for v in vals]:
                return f"value must be one of {vals}"
        except json.JSONDecodeError:
            pass
    return None


def resolve_clause_ref_farey(conn: sqlite3.Connection, body: dict, script_text: str = "") -> ClauseRef:
    ref = ClauseRef.from_body(body)
    if ref.farey_left_den > 0 and ref.farey_right_den > 0 and (
        ref.farey_left_num != 0 or ref.farey_right_num != 1 or ref.farey_left_den != 1
    ):
        return ref
    ast_nodes = []
    if ref.draft_script_id:
        cur = conn.execute(
            """SELECT n.farey_left_num, n.farey_left_den, n.farey_right_num, n.farey_right_den
               FROM thesaurus_ast_nodes n
               JOIN draft_episode_script s ON s.draft_episode_id = n.draft_episode_id
               WHERE s.id = ?""",
            (ref.draft_script_id,),
        )
        ast_nodes = [dict(r) for r in cur.fetchall()]
    ln, ld, rn, rd = char_to_farey(script_text, ref.char_start, ref.char_end, ast_nodes or None)
    ref.farey_left_num, ref.farey_left_den = ln, ld
    ref.farey_right_num, ref.farey_right_den = rn, rd
    return ref


def merge_change_list(
    conn: sqlite3.Connection,
    draft_id: str,
    required: List[DiffItem],
    warnings: List[DiffItem],
) -> Tuple[str, int, List[dict], List[dict]]:
    """Merge diff into open CL; supersede replaced items; return ids for new items."""
    now = _now()
    cur = conn.execute(
        """SELECT id, revision FROM localization_change_lists
           WHERE draft_episode_id = ? AND workflow_status IN ('new', 'in_progress')
           ORDER BY created_at DESC LIMIT 1""",
        (draft_id,),
    )
    row = cur.fetchone()
    if row:
        cl_id = row["id"]
        revision = int(row["revision"] or 0) + 1
        conn.execute(
            "UPDATE localization_change_lists SET revision = ?, last_saved_at = ?, updated_at = ?, workflow_status = 'in_progress' WHERE id = ?",
            (revision, now, now, cl_id),
        )
        conn.execute(
            "UPDATE localization_change_list_items SET superseded_at = ? WHERE change_list_id = ? AND superseded_at IS NULL",
            (now, cl_id),
        )
    else:
        cl_id = str(uuid.uuid4())
        topic_id = str(uuid.uuid4())
        revision = 0
        conn.execute(
            "INSERT INTO comment_topics (id, title, created_at, updated_at) VALUES (?, ?, ?, ?)",
            (topic_id, f"Change list {draft_id}", now, now),
        )
        conn.execute(
            """INSERT INTO localization_change_lists (
                id, draft_episode_id, comment_topic_id, workflow_status, revision, created_at, updated_at, last_saved_at
            ) VALUES (?, ?, ?, 'in_progress', 0, ?, ?, ?)""",
            (cl_id, draft_id, topic_id, now, now, now),
        )

    required_out: List[dict] = []
    warnings_out: List[dict] = []
    sort = 0
    for item in required + warnings:
        item_id = str(uuid.uuid4())
        conn.execute(
            """INSERT INTO localization_change_list_items (
                id, change_list_id, sort_order, severity, item_type, binding_id, description,
                old_char_start, old_char_end, new_char_start, new_char_end, auto_applied, user_acknowledged, created_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                item_id,
                cl_id,
                sort,
                item.severity,
                item.item_type,
                item.binding_id,
                item.description,
                item.old_char_start,
                item.old_char_end,
                item.new_char_start,
                item.new_char_end,
                1 if item.auto_applied else 0,
                1 if item.auto_applied else 0,
                now,
            ),
        )
        row_dict = {
            "id": item_id,
            "severity": item.severity,
            "itemType": item.item_type,
            "description": item.description,
            "bindingId": item.binding_id,
            "oldCharStart": item.old_char_start,
            "oldCharEnd": item.old_char_end,
            "newCharStart": item.new_char_start,
            "newCharEnd": item.new_char_end,
            "autoApplied": item.auto_applied,
            "userAcknowledged": item.auto_applied,
        }
        if item.severity == "required":
            required_out.append(row_dict)
        else:
            warnings_out.append(row_dict)
        sort += 1
    return cl_id, revision, required_out, warnings_out


def advance_change_list_on_review_approve(conn: sqlite3.Connection, draft_episode_id: str) -> None:
    row = get_active_change_list(conn, draft_episode_id)
    if not row or row["workflow_status"] != "in_review":
        return
    pending = conn.execute(
        """SELECT COUNT(*) AS c FROM localization_change_list_items
           WHERE change_list_id = ? AND severity = 'required' AND user_acknowledged = 0 AND superseded_at IS NULL""",
        (row["id"],),
    ).fetchone()
    if pending and pending["c"] > 0:
        return
    now = _now()
    conn.execute(
        "UPDATE localization_change_lists SET workflow_status = 'submitted', updated_at = ? WHERE id = ?",
        (now, row["id"]),
    )


def binding_row(r) -> dict:
    return {
        "id": r["id"],
        "episodeScriptId": r["episode_script_id"],
        "draftScriptId": r["draft_script_id"],
        "fareyLeftNum": r["farey_left_num"],
        "fareyLeftDen": r["farey_left_den"],
        "fareyRightNum": r["farey_right_num"],
        "fareyRightDen": r["farey_right_den"],
        "charStart": r["char_start"],
        "charEnd": r["char_end"],
        "selectionText": r["selection_text"],
        "propertyKey": r["property_key"],
        "propertyValue": r["property_value"],
        "bindingKind": r["binding_kind"],
        "astNodeId": r["ast_node_id"] if "ast_node_id" in r.keys() else None,
        "entryId": r["entry_id"] if "entry_id" in r.keys() else None,
        "promptPlaceholderName": r["prompt_placeholder_name"],
    }


def effective_properties_for_span(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    char_start: int,
    char_end: int,
) -> dict:
    from thesaurus.clause_audit import resolve_effective_properties

    bindings = conn.execute(
        """SELECT b.* FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ?""",
        (draft_episode_id,),
    ).fetchall()
    bindings = [dict(b) for b in bindings]

    specs = {}
    for r in conn.execute("SELECT key, default_value FROM localization_property_specs").fetchall():
        specs[r["key"]] = r["default_value"]

    entry_props: Dict[str, str] = {}
    for b in bindings:
        eid = b.get("entry_id")
        if not eid:
            continue
        for r in conn.execute(
            "SELECT property_key, property_value FROM thesaurus_entry_properties WHERE entry_id = ?",
            (eid,),
        ).fetchall():
            entry_props[r["property_key"]] = r["property_value"]

    effective = {}
    for key in specs:
        val = resolve_effective_properties(
            key, bindings, entry_props, specs, char_start, char_end, None
        )
        if val is not None:
            effective[key] = val
    return effective
