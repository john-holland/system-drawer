"""Script Output API — suggestions, unified draft comments, draft reviews helper."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Callable

from flask import jsonify, request

from thesaurus.script_edit_diff import audit_edit

try:
    from continuum_api.localization_helpers import (
        change_list_needs_review_ack,
        draft_blocks_author_edit,
        ensure_clause_binding_columns,
        ensure_draft_comment_thread,
        ensure_script_output_tables,
        get_active_change_list,
        is_draft_author,
        merge_change_list,
        require_draft_author,
        upsert_draft_script_text,
    )
    from continuum_api.localization_routes import _ensure_review_columns, _load_bindings_for_draft
except ImportError:
    from localization_helpers import (
        change_list_needs_review_ack,
        draft_blocks_author_edit,
        ensure_clause_binding_columns,
        ensure_draft_comment_thread,
        ensure_script_output_tables,
        get_active_change_list,
        is_draft_author,
        merge_change_list,
        require_draft_author,
        upsert_draft_script_text,
    )
    from localization_routes import _ensure_review_columns, _load_bindings_for_draft

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _ensure_comment_columns(conn: sqlite3.Connection) -> None:
    _ensure_review_columns(conn)
    alters = [
        "ALTER TABLE reviewer_comments ADD COLUMN draft_episode_id TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN source_page TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN comment_type TEXT DEFAULT 'general'",
        "ALTER TABLE reviewer_comments ADD COLUMN linked_comment_id TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN script_suggestion_id TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN author_user_id TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN draft_episode_id TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN source_page TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN comment_type TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN linked_comment_id TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN script_suggestion_id TEXT",
        "ALTER TABLE reviewer_comments_archive ADD COLUMN author_user_id TEXT",
    ]
    for sql in alters:
        try:
            conn.execute(sql)
        except sqlite3.OperationalError:
            pass


def _comment_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "reviewerId": r["reviewer_id"],
        "draftEpisodeId": r["draft_episode_id"] if "draft_episode_id" in r.keys() else None,
        "sourcePage": r["source_page"] if "source_page" in r.keys() else None,
        "commentType": r["comment_type"] if "comment_type" in r.keys() else "general",
        "linkedCommentId": r["linked_comment_id"] if "linked_comment_id" in r.keys() else None,
        "scriptSuggestionId": r["script_suggestion_id"] if "script_suggestion_id" in r.keys() else None,
        "authorUserId": r["author_user_id"] if "author_user_id" in r.keys() else None,
        "scriptRef": r["script_ref"],
        "textSelectionStart": r["text_selection_start"],
        "textSelectionEnd": r["text_selection_end"],
        "commentText": r["comment_text"],
        "reviewCycle": r["review_cycle"] if "review_cycle" in r.keys() else 0,
        "createdAt": r["created_at"],
    }


def _suggestion_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "draftEpisodeId": r["draft_episode_id"],
        "suggestedBy": r["suggested_by"],
        "baseScriptText": r["base_script_text"],
        "suggestedScriptText": r["suggested_script_text"],
        "status": r["status"],
        "reviewCycle": r["review_cycle"],
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
        "resolvedAt": r["resolved_at"],
        "resolvedBy": r["resolved_by"],
    }


def _diff_items_to_json(items) -> list:
    out = []
    for item in items:
        out.append(
            {
                "severity": item.severity,
                "itemType": item.item_type,
                "description": item.description,
                "bindingId": item.binding_id,
                "oldCharStart": item.old_char_start,
                "oldCharEnd": item.old_char_end,
                "newCharStart": item.new_char_start,
                "newCharEnd": item.new_char_end,
                "autoApplied": item.auto_applied,
            }
        )
    return out


def register_script_output_routes(app, get_conn: GetConn, get_user: GetUser) -> None:
    @app.route("/api/drafts/episodes/<draft_id>/reviews", methods=["GET"])
    def list_draft_reviews(draft_id: str):
        try:
            conn = get_conn()
            cur = conn.execute(
                """SELECT r.id, r.draft_episode_id, r.reviewer_user_id, r.reviewee_user_id,
                          r.status, r.created_at, r.updated_at
                   FROM reviewer r WHERE r.draft_episode_id = ? ORDER BY r.updated_at DESC""",
                (draft_id,),
            )
            items = [
                {
                    "id": r["id"],
                    "draftEpisodeId": r["draft_episode_id"],
                    "reviewerUserId": r["reviewer_user_id"],
                    "revieweeUserId": r["reviewee_user_id"],
                    "status": r["status"],
                    "createdAt": r["created_at"],
                    "updatedAt": r["updated_at"],
                }
                for r in cur.fetchall()
            ]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/script-suggestions", methods=["GET"])
    def list_script_suggestions(draft_id: str):
        status = (request.args.get("status") or "pending").strip().lower()
        try:
            conn = get_conn()
            ensure_script_output_tables(conn)
            if status == "archived":
                cur = conn.execute(
                    """SELECT * FROM script_suggestions_archive
                       WHERE draft_episode_id = ? ORDER BY archived_at DESC""",
                    (draft_id,),
                )
                items = [
                    {
                        "id": r["original_suggestion_id"] or r["id"],
                        "archiveId": r["id"],
                        "draftEpisodeId": r["draft_episode_id"],
                        "suggestedBy": r["suggested_by"],
                        "baseScriptText": r["base_script_text"],
                        "suggestedScriptText": r["suggested_script_text"],
                        "status": r["status"],
                        "reviewCycle": r["review_cycle"],
                        "createdAt": r["created_at"],
                        "resolvedAt": r["resolved_at"],
                        "resolvedBy": r["resolved_by"],
                        "archivedAt": r["archived_at"],
                        "archivedReason": r["archived_reason"],
                    }
                    for r in cur.fetchall()
                ]
            else:
                cur = conn.execute(
                    """SELECT * FROM script_suggestions
                       WHERE draft_episode_id = ? AND status = ?
                       ORDER BY created_at DESC""",
                    (draft_id, status),
                )
                items = [_suggestion_row(r) for r in cur.fetchall()]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/script-suggestions", methods=["POST"])
    def create_script_suggestion(draft_id: str):
        body = request.get_json() or {}
        suggested_text = (body.get("suggestedScriptText") or body.get("suggested_script_text") or "").strip()
        if not suggested_text:
            return jsonify({"error": "suggestedScriptText required"}), 400
        user_id = get_user()
        try:
            conn = get_conn()
            ensure_script_output_tables(conn)
            if is_draft_author(conn, draft_id, user_id):
                conn.close()
                return jsonify({"error": "Author should save directly, not submit suggestions"}), 403
            draft = conn.execute(
                "SELECT committed_at FROM draft_episodes WHERE id = ?",
                (draft_id,),
            ).fetchone()
            if not draft:
                conn.close()
                return jsonify({"error": "draft not found"}), 404
            if draft["committed_at"]:
                conn.close()
                return jsonify({"error": "draft is committed"}), 409
            script_row = conn.execute(
                "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY updated_at DESC LIMIT 1",
                (draft_id,),
            ).fetchone()
            base_text = (script_row["script_text"] if script_row else None) or ""
            now = _now()
            sid = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO script_suggestions
                   (id, draft_episode_id, suggested_by, base_script_text, suggested_script_text,
                    status, review_cycle, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, 'pending', 0, ?, ?)""",
                (sid, draft_id, user_id, base_text, suggested_text, now, now),
            )
            comment_text = (body.get("commentText") or body.get("comment") or "").strip()
            if comment_text:
                _ensure_comment_columns(conn)
                reviewer_id = ensure_draft_comment_thread(conn, draft_id)
                conn.execute(
                    """INSERT INTO reviewer_comments
                       (id, reviewer_id, script_ref, text_selection_start, text_selection_end,
                        comment_text, review_cycle, created_at, draft_episode_id, source_page,
                        comment_type, script_suggestion_id, author_user_id)
                       VALUES (?, ?, ?, ?, ?, ?, 0, ?, ?, 'script_output', 'suggestion', ?, ?)""",
                    (
                        str(uuid.uuid4()),
                        reviewer_id,
                        body.get("scriptRef"),
                        body.get("textSelectionStart"),
                        body.get("textSelectionEnd"),
                        comment_text,
                        now,
                        draft_id,
                        sid,
                        user_id,
                    ),
                )
            conn.commit()
            cur = conn.execute("SELECT * FROM script_suggestions WHERE id = ?", (sid,))
            item = _suggestion_row(cur.fetchone())
            conn.close()
            return jsonify({"item": item}), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/script-suggestions/<suggestion_id>/diff", methods=["GET"])
    def script_suggestion_diff(draft_id: str, suggestion_id: str):
        try:
            conn = get_conn()
            ensure_script_output_tables(conn)
            row = conn.execute(
                "SELECT * FROM script_suggestions WHERE id = ? AND draft_episode_id = ?",
                (suggestion_id, draft_id),
            ).fetchone()
            if not row:
                conn.close()
                return jsonify({"error": "suggestion not found"}), 404
            bindings = _load_bindings_for_draft(conn, draft_id)
            required, warnings, updated = audit_edit(
                row["base_script_text"] or "",
                row["suggested_script_text"] or "",
                bindings,
            )
            cl = get_active_change_list(conn, draft_id)
            conn.close()
            return jsonify(
                {
                    "required": _diff_items_to_json(required),
                    "warnings": _diff_items_to_json(warnings),
                    "updatedBindings": updated,
                    "needsReviewAck": change_list_needs_review_ack(cl),
                }
            ), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/script-suggestions/<suggestion_id>", methods=["PATCH"])
    def patch_script_suggestion(draft_id: str, suggestion_id: str):
        body = request.get_json() or {}
        action = (body.get("action") or body.get("status") or "").strip().lower()
        user_id = get_user()
        if action not in ("accept", "reject", "accepted", "rejected"):
            return jsonify({"error": "action must be accept or reject"}), 400
        if action in ("accepted",):
            action = "accept"
        if action in ("rejected",):
            action = "reject"
        try:
            conn = get_conn()
            ensure_script_output_tables(conn)
            err = require_draft_author(conn, draft_id, user_id)
            if err:
                conn.close()
                return jsonify({"error": err}), 403
            row = conn.execute(
                "SELECT * FROM script_suggestions WHERE id = ? AND draft_episode_id = ? AND status = 'pending'",
                (suggestion_id, draft_id),
            ).fetchone()
            if not row:
                conn.close()
                return jsonify({"error": "pending suggestion not found"}), 404
            now = _now()
            if action == "reject":
                conn.execute(
                    """UPDATE script_suggestions SET status = 'rejected', resolved_at = ?, resolved_by = ?, updated_at = ?
                       WHERE id = ?""",
                    (now, user_id, now, suggestion_id),
                )
                conn.execute(
                    """INSERT INTO script_suggestions_archive
                       (id, original_suggestion_id, draft_episode_id, suggested_by, base_script_text,
                        suggested_script_text, status, review_cycle, created_at, resolved_at, resolved_by,
                        archived_at, archived_reason)
                       SELECT ?, id, draft_episode_id, suggested_by, base_script_text, suggested_script_text,
                              'rejected', review_cycle, created_at, ?, ?, ?, 'rejected'
                       FROM script_suggestions WHERE id = ?""",
                    (str(uuid.uuid4()), now, user_id, now, suggestion_id),
                )
                conn.execute("DELETE FROM script_suggestions WHERE id = ?", (suggestion_id,))
                conn.commit()
                conn.close()
                return jsonify({"ok": True, "status": "rejected"}), 200

            blocked = draft_blocks_author_edit(conn, draft_id)
            if blocked:
                conn.close()
                return jsonify({"error": f"draft change list is {blocked}; withdraw before accepting"}), 409
            bindings = _load_bindings_for_draft(conn, draft_id)
            required, warnings, updated = audit_edit(
                row["base_script_text"] or "",
                row["suggested_script_text"] or "",
                bindings,
            )
            merge_change_list(conn, draft_id, required, warnings)
            for b in updated:
                conn.execute(
                    "UPDATE localization_clause_bindings SET char_start = ?, char_end = ?, updated_at = ? WHERE id = ?",
                    (b["char_start"], b["char_end"], now, b["id"]),
                )
            upsert_draft_script_text(conn, draft_id, row["suggested_script_text"] or "")
            conn.execute(
                """UPDATE script_suggestions SET status = 'accepted', resolved_at = ?, resolved_by = ?, updated_at = ?
                   WHERE id = ?""",
                (now, user_id, now, suggestion_id),
            )
            conn.execute(
                """INSERT INTO script_suggestions_archive
                   (id, original_suggestion_id, draft_episode_id, suggested_by, base_script_text,
                    suggested_script_text, status, review_cycle, created_at, resolved_at, resolved_by,
                    archived_at, archived_reason)
                   SELECT ?, id, draft_episode_id, suggested_by, base_script_text, suggested_script_text,
                          'accepted', review_cycle, created_at, ?, ?, ?, 'accepted'
                   FROM script_suggestions WHERE id = ?""",
                (str(uuid.uuid4()), now, user_id, now, suggestion_id),
            )
            conn.execute("DELETE FROM script_suggestions WHERE id = ?", (suggestion_id,))
            conn.commit()
            conn.close()
            return jsonify({"ok": True, "status": "accepted"}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/comments", methods=["GET"])
    def list_draft_comments(draft_id: str):
        source_page = request.args.get("sourcePage") or request.args.get("source_page")
        comment_type = request.args.get("commentType") or request.args.get("comment_type")
        include_archived = request.args.get("includeArchived", "false").lower() in ("1", "true", "yes")
        suggestion_id = request.args.get("scriptSuggestionId") or request.args.get("script_suggestion_id")
        try:
            conn = get_conn()
            _ensure_comment_columns(conn)
            where = ["draft_episode_id = ?"]
            params: list = [draft_id]
            if source_page:
                where.append("source_page = ?")
                params.append(source_page)
            if comment_type:
                where.append("comment_type = ?")
                params.append(comment_type)
            if suggestion_id:
                where.append("script_suggestion_id = ?")
                params.append(suggestion_id)
            sql = f"""SELECT * FROM reviewer_comments WHERE {' AND '.join(where)}
                      ORDER BY created_at"""
            cur = conn.execute(sql, params)
            items = [_comment_row(r) for r in cur.fetchall()]
            archived = []
            if include_archived:
                awhere = ["draft_episode_id = ?"]
                aparams: list = [draft_id]
                if source_page:
                    awhere.append("source_page = ?")
                    aparams.append(source_page)
                if comment_type:
                    awhere.append("comment_type = ?")
                    aparams.append(comment_type)
                acur = conn.execute(
                    f"""SELECT id, reviewer_id, original_comment_id, comment_text, previously_on,
                               text_selection_start, text_selection_end, property_key, review_cycle,
                               archived_at, archived_reason, draft_episode_id, source_page, comment_type,
                               linked_comment_id, script_suggestion_id, author_user_id
                        FROM reviewer_comments_archive WHERE {' AND '.join(awhere)}
                        ORDER BY archived_at DESC""",
                    aparams,
                )
                archived = [
                    {
                        "id": r["id"],
                        "originalCommentId": r["original_comment_id"],
                        "commentText": r["comment_text"],
                        "previouslyOn": r["previously_on"],
                        "textSelectionStart": r["text_selection_start"],
                        "textSelectionEnd": r["text_selection_end"],
                        "reviewCycle": r["review_cycle"],
                        "archivedAt": r["archived_at"],
                        "archivedReason": r["archived_reason"],
                        "draftEpisodeId": r["draft_episode_id"],
                        "sourcePage": r["source_page"],
                        "commentType": r["comment_type"],
                        "linkedCommentId": r["linked_comment_id"],
                        "scriptSuggestionId": r["script_suggestion_id"],
                        "authorUserId": r["author_user_id"],
                    }
                    for r in acur.fetchall()
                ]
            conn.close()
            return jsonify({"items": items, "archived": archived}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/comments", methods=["POST"])
    def create_draft_comment(draft_id: str):
        body = request.get_json() or {}
        text = (body.get("commentText") or body.get("comment") or "").strip()
        if not text:
            return jsonify({"error": "commentText required"}), 400
        user_id = get_user()
        source_page = (body.get("sourcePage") or body.get("source_page") or "script_output").strip()
        comment_type = (body.get("commentType") or body.get("comment_type") or "general").strip()
        try:
            conn = get_conn()
            _ensure_comment_columns(conn)
            draft = conn.execute("SELECT id FROM draft_episodes WHERE id = ?", (draft_id,)).fetchone()
            if not draft:
                conn.close()
                return jsonify({"error": "draft not found"}), 404
            reviewer_id = ensure_draft_comment_thread(conn, draft_id)
            now = _now()
            cid = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO reviewer_comments
                   (id, reviewer_id, script_ref, text_selection_start, text_selection_end,
                    comment_text, review_cycle, created_at, draft_episode_id, source_page,
                    comment_type, linked_comment_id, script_suggestion_id, author_user_id)
                   VALUES (?, ?, ?, ?, ?, ?, 0, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    cid,
                    reviewer_id,
                    body.get("scriptRef"),
                    body.get("textSelectionStart"),
                    body.get("textSelectionEnd"),
                    text,
                    now,
                    draft_id,
                    source_page,
                    comment_type,
                    body.get("linkedCommentId"),
                    body.get("scriptSuggestionId"),
                    user_id,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM reviewer_comments WHERE id = ?", (cid,))
            item = _comment_row(cur.fetchone())
            conn.close()
            return jsonify({"item": item}), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500
