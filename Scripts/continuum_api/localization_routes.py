"""Localization property, change-list, and apply-edit API routes."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Callable

from flask import jsonify, request

from thesaurus.script_edit_diff import DiffItem, audit_edit

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]
CreateNotification = Callable[..., str]
IsAdmin = Callable[[], bool]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _ensure_review_columns(conn: sqlite3.Connection) -> None:
    alters = [
        "ALTER TABLE reviewer ADD COLUMN review_cycle INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE reviewer_comments ADD COLUMN review_cycle INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE reviewer_comments ADD COLUMN property_key TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN comment_topic_id TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_requested_at TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_requested_by TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_approved_at TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_approved_by TEXT",
    ]
    for sql in alters:
        try:
            conn.execute(sql)
        except sqlite3.OperationalError:
            pass


def build_previously_on(comment: dict, script_text: str = "") -> str:
    pk = comment.get("property_key") or comment.get("propertyKey")
    ts = comment.get("text_selection_start") or comment.get("textSelectionStart")
    te = comment.get("text_selection_end") or comment.get("textSelectionEnd")
    if pk:
        snippet = ""
        if script_text and ts is not None and te is not None and te > ts:
            snippet = script_text[int(ts): int(te)][:40]
        return f'property: {pk} on "{snippet}"'
    if script_text and ts is not None and te is not None and te > ts:
        return f'selection: "{script_text[int(ts): int(te)][:80]}"'
    text = comment.get("comment_text") or comment.get("commentText") or ""
    return text[:80]


def register_localization_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    create_notification: CreateNotification,
    is_admin: IsAdmin,
) -> None:
    @app.route("/api/thesaurus/property-specs", methods=["GET"])
    def list_property_specs():
        try:
            conn = get_conn()
            cur = conn.execute(
                "SELECT key, value_type, allowed_values_json, default_value, description FROM localization_property_specs ORDER BY key"
            )
            items = [
                {
                    "key": r["key"],
                    "valueType": r["value_type"],
                    "allowedValuesJson": r["allowed_values_json"],
                    "defaultValue": r["default_value"],
                    "description": r["description"],
                }
                for r in cur.fetchall()
            ]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuum_localization_schema.sql"}), 500

    @app.route("/api/thesaurus/entry-properties", methods=["GET"])
    def list_entry_properties():
        entry_id = request.args.get("entryId")
        try:
            conn = get_conn()
            if entry_id:
                cur = conn.execute(
                    "SELECT entry_id, property_key, property_value FROM thesaurus_entry_properties WHERE entry_id = ? ORDER BY property_key",
                    (entry_id,),
                )
            else:
                cur = conn.execute(
                    "SELECT entry_id, property_key, property_value FROM thesaurus_entry_properties ORDER BY entry_id, property_key"
                )
            items = [
                {
                    "entryId": r["entry_id"],
                    "propertyKey": r["property_key"],
                    "propertyValue": r["property_value"],
                }
                for r in cur.fetchall()
            ]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entry-properties", methods=["PUT"])
    def upsert_entry_property():
        body = request.get_json() or {}
        entry_id = body.get("entryId")
        property_key = body.get("propertyKey")
        property_value = body.get("propertyValue", "")
        if not entry_id or not property_key:
            return jsonify({"error": "entryId and propertyKey required"}), 400
        try:
            conn = get_conn()
            conn.execute(
                """INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value)
                   VALUES (?, ?, ?)
                   ON CONFLICT(entry_id, property_key) DO UPDATE SET property_value = excluded.property_value""",
                (entry_id, property_key, property_value),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entry-properties", methods=["DELETE"])
    def delete_entry_property():
        entry_id = request.args.get("entryId")
        property_key = request.args.get("propertyKey")
        if not entry_id or not property_key:
            return jsonify({"error": "entryId and propertyKey required"}), 400
        try:
            conn = get_conn()
            conn.execute(
                "DELETE FROM thesaurus_entry_properties WHERE entry_id = ? AND property_key = ?",
                (entry_id, property_key),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clause-bindings", methods=["GET"])
    def list_clause_bindings():
        draft_episode_id = request.args.get("draftEpisodeId")
        draft_script_id = request.args.get("draftScriptId")
        try:
            conn = get_conn()
            if draft_episode_id:
                cur = conn.execute(
                    """SELECT b.* FROM localization_clause_bindings b
                       JOIN draft_episode_script s ON s.id = b.draft_script_id
                       WHERE s.draft_episode_id = ?""",
                    (draft_episode_id,),
                )
            elif draft_script_id:
                cur = conn.execute(
                    "SELECT * FROM localization_clause_bindings WHERE draft_script_id = ?",
                    (draft_script_id,),
                )
            else:
                conn.close()
                return jsonify({"items": []}), 200
            items = [_binding_row(r) for r in cur.fetchall()]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clause-bindings", methods=["POST"])
    def create_clause_binding():
        body = request.get_json() or {}
        try:
            conn = get_conn()
            bid = str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO localization_clause_bindings (
                    id, episode_script_id, draft_script_id,
                    farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    char_start, char_end, selection_text, property_key, property_value,
                    binding_kind, ast_node_id, prompt_placeholder_name, created_at, updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    bid,
                    body.get("episodeScriptId"),
                    body.get("draftScriptId"),
                    body.get("fareyLeftNum", 0),
                    body.get("fareyLeftDen", 1),
                    body.get("fareyRightNum", 1),
                    body.get("fareyRightDen", 1),
                    body.get("charStart", 0),
                    body.get("charEnd", 0),
                    body.get("selectionText", ""),
                    body.get("propertyKey", ""),
                    body.get("propertyValue", ""),
                    body.get("bindingKind", "lemma"),
                    body.get("astNodeId"),
                    body.get("promptPlaceholderName"),
                    now,
                    now,
                ),
            )
            conn.commit()
            conn.close()
            return jsonify({"id": bid}), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/scripts/<draft_id>/apply-edit", methods=["POST"])
    def apply_script_edit(draft_id: str):
        body = request.get_json() or {}
        old_text = body.get("oldText", "")
        new_text = body.get("newText", "")
        try:
            conn = get_conn()
            _ensure_review_columns(conn)
            bindings = _load_bindings_for_draft(conn, draft_id)
            required, warnings, updated = audit_edit(old_text, new_text, bindings)
            change_list_id, revision = _merge_change_list(conn, draft_id, required, warnings)
            for b in updated:
                conn.execute(
                    "UPDATE localization_clause_bindings SET char_start = ?, char_end = ?, updated_at = ? WHERE id = ?",
                    (b["char_start"], b["char_end"], _now(), b["id"]),
                )
            conn.commit()
            conn.close()
            return jsonify(
                {
                    "changeListId": change_list_id,
                    "revision": revision,
                    "required": [_diff_item(d) for d in required],
                    "warnings": [_diff_item(d) for d in warnings],
                }
            ), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-lists/<change_list_id>", methods=["GET"])
    def get_change_list(change_list_id: str):
        try:
            conn = get_conn()
            cur = conn.execute("SELECT * FROM localization_change_lists WHERE id = ?", (change_list_id,))
            row = cur.fetchone()
            if not row:
                conn.close()
                return jsonify({"error": "not found"}), 404
            items_cur = conn.execute(
                """SELECT * FROM localization_change_list_items
                   WHERE change_list_id = ? AND superseded_at IS NULL ORDER BY sort_order""",
                (change_list_id,),
            )
            items = [_change_item_row(r) for r in items_cur.fetchall()]
            conn.close()
            return jsonify({**_change_list_row(row), "items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-lists/<change_list_id>/save", methods=["POST"])
    def save_change_list(change_list_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            now = _now()
            for item in body.get("items") or []:
                if item.get("id"):
                    conn.execute(
                        "UPDATE localization_change_list_items SET user_acknowledged = ? WHERE id = ? AND change_list_id = ?",
                        (1 if item.get("userAcknowledged") else 0, item["id"], change_list_id),
                    )
            conn.execute(
                "UPDATE localization_change_lists SET last_saved_at = ?, updated_at = ?, workflow_status = 'in_progress' WHERE id = ?",
                (now, now, change_list_id),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-lists/<change_list_id>/submit-for-review", methods=["POST"])
    def submit_change_list_for_review(change_list_id: str):
        try:
            conn = get_conn()
            pending = conn.execute(
                """SELECT COUNT(*) AS c FROM localization_change_list_items
                   WHERE change_list_id = ? AND severity = 'required' AND user_acknowledged = 0 AND superseded_at IS NULL""",
                (change_list_id,),
            ).fetchone()
            if pending and pending["c"] > 0:
                conn.close()
                return jsonify({"error": "required items not acknowledged"}), 400
            now = _now()
            conn.execute(
                "UPDATE localization_change_lists SET workflow_status = 'in_review', submitted_at = ?, updated_at = ? WHERE id = ?",
                (now, now, change_list_id),
            )
            row = conn.execute(
                "SELECT draft_episode_id FROM localization_change_lists WHERE id = ?",
                (change_list_id,),
            ).fetchone()
            if row and row["draft_episode_id"]:
                revs = conn.execute(
                    "SELECT reviewer_user_id FROM reviewer WHERE draft_episode_id = ?",
                    (row["draft_episode_id"],),
                ).fetchall()
                for r in revs:
                    create_notification(
                        conn,
                        r["reviewer_user_id"],
                        "change_list_submitted",
                        "Draft submitted for localization review",
                        draft_id=row["draft_episode_id"],
                    )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-lists/<change_list_id>/withdraw", methods=["POST"])
    def withdraw_change_list(change_list_id: str):
        try:
            conn = get_conn()
            now = _now()
            conn.execute(
                "UPDATE localization_change_lists SET workflow_status = 'in_progress', updated_at = ? WHERE id = ? AND workflow_status = 'in_review'",
                (now, change_list_id),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-list-items/<item_id>", methods=["PATCH"])
    def patch_change_list_item(item_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            if "userAcknowledged" in body:
                conn.execute(
                    "UPDATE localization_change_list_items SET user_acknowledged = ? WHERE id = ?",
                    (1 if body["userAcknowledged"] else 0, item_id),
                )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/reviews/<review_id>/comments/archive", methods=["GET"])
    def list_archived_comments(review_id: str):
        try:
            conn = get_conn()
            _ensure_review_columns(conn)
            cur = conn.execute(
                """SELECT id, reviewer_id, original_comment_id, comment_text, previously_on,
                          text_selection_start, text_selection_end, property_key, review_cycle, archived_at, archived_reason
                   FROM reviewer_comments_archive WHERE reviewer_id = ? ORDER BY archived_at DESC""",
                (review_id,),
            )
            items = [
                {
                    "id": r["id"],
                    "reviewerId": r["reviewer_id"],
                    "originalCommentId": r["original_comment_id"],
                    "commentText": r["comment_text"],
                    "previouslyOn": r["previously_on"],
                    "textSelectionStart": r["text_selection_start"],
                    "textSelectionEnd": r["text_selection_end"],
                    "propertyKey": r["property_key"],
                    "reviewCycle": r["review_cycle"],
                    "archivedAt": r["archived_at"],
                    "archivedReason": r["archived_reason"],
                }
                for r in cur.fetchall()
            ]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuum_localization_workflow_schema.sql"}), 500


def archive_review_comments_on_deny(conn, review_id: str, script_text: str = "") -> None:
    _ensure_review_columns(conn)
    now = _now()
    cur = conn.execute(
        "SELECT id, comment_text, text_selection_start, text_selection_end, property_key, review_cycle FROM reviewer_comments WHERE reviewer_id = ?",
        (review_id,),
    )
    for r in cur.fetchall():
        prev = build_previously_on(dict(r), script_text)
        conn.execute(
            """INSERT INTO reviewer_comments_archive (
                id, reviewer_id, original_comment_id, comment_text, previously_on,
                text_selection_start, text_selection_end, property_key, review_cycle, archived_at, archived_reason
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'review_cycle_reset')""",
            (
                str(uuid.uuid4()),
                review_id,
                r["id"],
                r["comment_text"],
                prev,
                r["text_selection_start"],
                r["text_selection_end"],
                r["property_key"],
                r["review_cycle"] or 0,
                now,
            ),
        )
        conn.execute("DELETE FROM reviewer_comments WHERE id = ?", (r["id"],))
    conn.execute(
        "UPDATE reviewer SET review_cycle = review_cycle + 1, updated_at = ? WHERE id = ?",
        (now, review_id),
    )
    conn.execute(
        """UPDATE localization_change_lists SET review_cycle = review_cycle + 1, workflow_status = 'in_progress', updated_at = ?
           WHERE draft_episode_id = (SELECT draft_episode_id FROM reviewer WHERE id = ?)""",
        (now, review_id),
    )


def _load_bindings_for_draft(conn, draft_id: str) -> list:
    cur = conn.execute(
        """SELECT b.* FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ?""",
        (draft_id,),
    )
    return [dict(r) for r in cur.fetchall()]


def _merge_change_list(conn, draft_id: str, required: list, warnings: list) -> tuple:
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

    sort = 0
    for item in required + warnings:
        conn.execute(
            """INSERT INTO localization_change_list_items (
                id, change_list_id, sort_order, severity, item_type, binding_id, description,
                old_char_start, old_char_end, new_char_start, new_char_end, auto_applied, user_acknowledged, created_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                str(uuid.uuid4()),
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
        sort += 1
    return cl_id, revision


def _binding_row(r) -> dict:
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
    }


def _change_list_row(r) -> dict:
    return {
        "id": r["id"],
        "episodeScriptId": r["episode_script_id"],
        "draftEpisodeId": r["draft_episode_id"],
        "commentTopicId": r["comment_topic_id"],
        "workflowStatus": r["workflow_status"],
        "revision": r["revision"],
        "reviewCycle": r["review_cycle"],
        "lastSavedAt": r["last_saved_at"],
    }


def _change_item_row(r) -> dict:
    return {
        "id": r["id"],
        "changeListId": r["change_list_id"],
        "sortOrder": r["sort_order"],
        "severity": r["severity"],
        "itemType": r["item_type"],
        "bindingId": r["binding_id"],
        "description": r["description"],
        "oldCharStart": r["old_char_start"],
        "oldCharEnd": r["old_char_end"],
        "newCharStart": r["new_char_start"],
        "newCharEnd": r["new_char_end"],
        "autoApplied": bool(r["auto_applied"]),
        "userAcknowledged": bool(r["user_acknowledged"]),
    }


def _diff_item(d: DiffItem) -> dict:
    return {
        "severity": d.severity,
        "itemType": d.item_type,
        "description": d.description,
        "bindingId": d.binding_id,
        "oldCharStart": d.old_char_start,
        "oldCharEnd": d.old_char_end,
        "newCharStart": d.new_char_start,
        "newCharEnd": d.new_char_end,
        "autoApplied": d.auto_applied,
        "userAcknowledged": d.auto_applied,
    }
