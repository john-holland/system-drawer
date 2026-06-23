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


def resolve_draft_script_id(conn: sqlite3.Connection, body: dict) -> Optional[str]:
    draft_script_id = body.get("draftScriptId") or body.get("draft_script_id")
    if draft_script_id:
        return str(draft_script_id)
    draft_episode_id = body.get("draftEpisodeId") or body.get("draftId")
    if not draft_episode_id:
        return None
    row = conn.execute(
        """SELECT id FROM draft_episode_script
           WHERE draft_episode_id = ?
           ORDER BY updated_at DESC LIMIT 1""",
        (draft_episode_id,),
    ).fetchone()
    return row["id"] if row else None


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
               JOIN draft_episode_script s ON s.episode_script_id = n.episode_script_id
               WHERE s.id = ? AND n.episode_script_id IS NOT NULL""",
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


def upsert_draft_script_text(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    script_text: str,
    language: str = "en",
) -> None:
    """Persist draft script body (used when applying edits from Script Output)."""
    cur = conn.execute(
        "SELECT id FROM draft_episode_script WHERE draft_episode_id = ? AND language = ?",
        (draft_episode_id, language),
    )
    row = cur.fetchone()
    now = _now()
    if row:
        conn.execute(
            "UPDATE draft_episode_script SET script_text = ?, updated_at = ? WHERE id = ?",
            (script_text, now, row["id"]),
        )
        return
    conn.execute(
        """INSERT INTO draft_episode_script
           (id, draft_episode_id, episode_script_id, script_text, language, created_at, updated_at)
           VALUES (?, ?, NULL, ?, ?, ?, ?)""",
        (str(uuid.uuid4()), draft_episode_id, script_text, language, now, now),
    )


def ensure_reviewer_rows_for_submission(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    change_list_id: str,
) -> None:
    """Ensure reviewer table rows exist when a change list is submitted for review."""
    draft = conn.execute(
        "SELECT created_by FROM draft_episodes WHERE id = ?",
        (draft_episode_id,),
    ).fetchone()
    reviewee = (draft["created_by"] if draft else None) or "anonymous"
    reviewer_ids: set[str] = set()
    for r in conn.execute(
        "SELECT reviewer_user_id FROM reviewer WHERE draft_episode_id = ?",
        (draft_episode_id,),
    ).fetchall():
        reviewer_ids.add(r["reviewer_user_id"])
    for r in conn.execute(
        "SELECT user_id FROM localization_change_list_reviewers WHERE change_list_id = ?",
        (change_list_id,),
    ).fetchall():
        reviewer_ids.add(r["user_id"])
    if not reviewer_ids:
        return
    now = _now()
    for reviewer_id in reviewer_ids:
        existing = conn.execute(
            "SELECT id FROM reviewer WHERE draft_episode_id = ? AND reviewer_user_id = ?",
            (draft_episode_id, reviewer_id),
        ).fetchone()
        if existing:
            conn.execute(
                "UPDATE reviewer SET status = 'pending', updated_at = ? WHERE id = ?",
                (now, existing["id"]),
            )
            continue
        conn.execute(
            """INSERT INTO reviewer
               (id, draft_episode_id, reviewer_user_id, reviewee_user_id, status, created_at, updated_at)
               VALUES (?, ?, ?, ?, 'pending', ?, ?)""",
            (str(uuid.uuid4()), draft_episode_id, reviewer_id, reviewee, now, now),
        )


def list_submitted_change_lists_for_user(
    conn: sqlite3.Connection,
    user_id: str,
) -> list[dict[str, Any]]:
    """Drafts with change lists in review, visible to author, assigned reviewer, or CL reviewer."""
    cur = conn.execute(
        """SELECT cl.id AS change_list_id, cl.workflow_status, cl.submitted_at, cl.updated_at,
                  d.id AS draft_episode_id, d.title, d.committed_at, d.created_by
           FROM localization_change_lists cl
           JOIN draft_episodes d ON d.id = cl.draft_episode_id
           WHERE cl.workflow_status IN ('in_review', 'submitted')
           AND (
               d.created_by = ?
               OR EXISTS (
                   SELECT 1 FROM reviewer r
                   WHERE r.draft_episode_id = d.id
                   AND (r.reviewer_user_id = ? OR r.reviewee_user_id = ?)
               )
               OR EXISTS (
                   SELECT 1 FROM localization_change_list_reviewers clr
                   WHERE clr.change_list_id = cl.id AND clr.user_id = ?
               )
           )
           ORDER BY COALESCE(cl.submitted_at, cl.updated_at) DESC""",
        (user_id, user_id, user_id, user_id),
    )
    return [dict(r) for r in cur.fetchall()]


def is_draft_author(conn: sqlite3.Connection, draft_episode_id: str, user_id: str) -> bool:
    row = conn.execute(
        "SELECT created_by FROM draft_episodes WHERE id = ?",
        (draft_episode_id,),
    ).fetchone()
    if not row:
        return False
    author = (row["created_by"] or "anonymous").strip()
    return author == (user_id or "anonymous").strip()


def require_draft_author(conn: sqlite3.Connection, draft_episode_id: str, user_id: str) -> Optional[str]:
    if not is_draft_author(conn, draft_episode_id, user_id):
        return "Only the draft author may perform this action"
    return None


def change_list_needs_review_ack(row: Optional[sqlite3.Row | dict]) -> bool:
    if not row:
        return False
    if isinstance(row, sqlite3.Row):
        row = dict(row)
    status = (row.get("workflow_status") or row.get("workflowStatus") or "").lower()
    if status in ("in_review", "submitted"):
        return True
    return bool(row.get("submitted_at") or row.get("submittedAt"))


def ensure_script_output_tables(conn: sqlite3.Connection) -> None:
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS script_suggestions (
            id TEXT PRIMARY KEY,
            draft_episode_id TEXT NOT NULL,
            suggested_by TEXT NOT NULL,
            base_script_text TEXT NOT NULL,
            suggested_script_text TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'pending',
            review_cycle INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            resolved_at TEXT,
            resolved_by TEXT
        );
        CREATE TABLE IF NOT EXISTS script_suggestions_archive (
            id TEXT PRIMARY KEY,
            original_suggestion_id TEXT,
            draft_episode_id TEXT NOT NULL,
            suggested_by TEXT NOT NULL,
            base_script_text TEXT NOT NULL,
            suggested_script_text TEXT NOT NULL,
            status TEXT NOT NULL,
            review_cycle INTEGER NOT NULL DEFAULT 0,
            created_at TEXT NOT NULL,
            resolved_at TEXT,
            resolved_by TEXT,
            archived_at TEXT NOT NULL,
            archived_reason TEXT NOT NULL DEFAULT 'resolved'
        );
        """
    )


def ensure_draft_comment_thread(conn: sqlite3.Connection, draft_episode_id: str) -> str:
    """Return reviewer.id for storing draft-scoped comments (creates thread row if needed)."""
    cur = conn.execute(
        "SELECT id FROM reviewer WHERE draft_episode_id = ? ORDER BY created_at LIMIT 1",
        (draft_episode_id,),
    )
    row = cur.fetchone()
    if row:
        return row["id"]
    draft = conn.execute(
        "SELECT created_by FROM draft_episodes WHERE id = ?",
        (draft_episode_id,),
    ).fetchone()
    author = (draft["created_by"] if draft else None) or "anonymous"
    now = _now()
    rid = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO reviewer
           (id, draft_episode_id, reviewer_user_id, reviewee_user_id, status, created_at, updated_at)
           VALUES (?, ?, ?, ?, 'pending', ?, ?)""",
        (rid, draft_episode_id, author, author, now, now),
    )
    return rid


def binding_row(r, script_text: str = "") -> dict:
    char_start = r["char_start"]
    char_end = r["char_end"]
    if char_end <= char_start and script_text:
        from thesaurus.clause_audit import farey_to_char

        char_start, char_end = farey_to_char(
            script_text,
            r["farey_left_num"],
            r["farey_left_den"],
            r["farey_right_num"],
            r["farey_right_den"],
        )
    return {
        "id": r["id"],
        "episodeScriptId": r["episode_script_id"],
        "draftScriptId": r["draft_script_id"],
        "fareyLeftNum": r["farey_left_num"],
        "fareyLeftDen": r["farey_left_den"],
        "fareyRightNum": r["farey_right_num"],
        "fareyRightDen": r["farey_right_den"],
        "charStart": char_start,
        "charEnd": char_end,
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
