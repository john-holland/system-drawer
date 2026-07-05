"""Localization property, change-list, and apply-edit API routes."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Callable

from flask import jsonify, request

from thesaurus.clause_ref import BINDING_KINDS
from thesaurus.script_edit_diff import audit_binding_edit, audit_edit

try:
    from continuuuum_api.localization_helpers import (
        advance_change_list_on_review_approve,
        binding_row,
        draft_blocks_author_edit,
        effective_properties_for_span,
        ensure_clause_binding_columns,
        get_active_change_list,
        merge_change_list,
        require_draft_author,
        resolve_clause_ref_farey,
        resolve_draft_script_id,
        validate_property_value,
        withdraw_change_list_if_in_review,
    )
except ImportError:
    from localization_helpers import (
        advance_change_list_on_review_approve,
        binding_row,
        draft_blocks_author_edit,
        effective_properties_for_span,
        ensure_clause_binding_columns,
        get_active_change_list,
        merge_change_list,
        require_draft_author,
        resolve_clause_ref_farey,
        resolve_draft_script_id,
        validate_property_value,
        withdraw_change_list_if_in_review,
    )

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
            snippet = script_text[int(ts) : int(te)][:40]
        return f'property: {pk} on "{snippet}"'
    if script_text and ts is not None and te is not None and te > ts:
        return f'selection: "{script_text[int(ts): int(te)][:80]}"'
    text = comment.get("comment_text") or comment.get("commentText") or ""
    return text[:80]


def execute_binding_edit(conn: sqlite3.Connection, draft_id: str, body: dict) -> tuple[dict | None, str | None]:
    """Apply a binding edit within an open transaction. Returns (result_dict, error_message)."""
    binding_id = body.get("bindingId")
    script_text = body.get("scriptText", "")
    if not binding_id:
        return None, "bindingId required"
    cur = conn.execute(
        """SELECT b.* FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ? AND b.id = ?""",
        (draft_id, binding_id),
    )
    old_row = cur.fetchone()
    if not old_row:
        return None, "binding not found for draft"
    old = dict(old_row)
    new_cs = body.get("charStart", old["char_start"])
    new_ce = body.get("charEnd", old["char_end"])
    new_pk = body.get("propertyKey", old["property_key"])
    new_pv = body.get("propertyValue", old["property_value"])
    new_eid = body.get("entryId", old.get("entry_id"))
    err = validate_property_value(conn, new_pk, new_pv) if new_pk else None
    if err:
        return None, err
    selection_text = body.get("selectionText")
    if selection_text is None and script_text and new_ce > new_cs:
        selection_text = script_text[int(new_cs) : int(new_ce)]
    elif selection_text is None:
        selection_text = old["selection_text"]
    new = {
        **old,
        "char_start": int(new_cs),
        "char_end": int(new_ce),
        "property_key": new_pk,
        "property_value": new_pv,
        "entry_id": new_eid,
        "selection_text": selection_text,
    }
    required, warnings = audit_binding_edit(old, new, script_text)
    if not required and not warnings:
        return {"changeListId": None, "revision": 0, "required": [], "warnings": []}, None
    from thesaurus.clause_audit import char_to_farey

    ln, ld, rn, rd = char_to_farey(script_text or old.get("selection_text", ""), int(new_cs), int(new_ce))
    now = _now()
    conn.execute(
        """UPDATE localization_clause_bindings SET
           char_start = ?, char_end = ?, farey_left_num = ?, farey_left_den = ?,
           farey_right_num = ?, farey_right_den = ?, selection_text = ?,
           property_key = ?, property_value = ?, entry_id = ?, updated_at = ?
           WHERE id = ?""",
        (
            int(new_cs),
            int(new_ce),
            ln,
            ld,
            rn,
            rd,
            selection_text,
            new_pk,
            new_pv,
            new_eid,
            now,
            binding_id,
        ),
    )
    cl_id, revision, req_out, warn_out = merge_change_list(conn, draft_id, required, warnings)
    return {
        "changeListId": cl_id,
        "revision": revision,
        "required": req_out,
        "warnings": warn_out,
    }, None


def register_localization_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    create_notification: CreateNotification,
    is_admin: IsAdmin,
) -> None:
    @app.route("/api/thesaurus/property-specs", methods=["GET"])
    def list_property_specs():
        key = request.args.get("key")
        try:
            conn = get_conn()
            try:
                from continuuuum_api.spatial_generator_specs import ensure_spatial_property_specs
            except ImportError:
                from spatial_generator_specs import ensure_spatial_property_specs
            ensure_spatial_property_specs(conn)
            if key:
                cur = conn.execute(
                    "SELECT key, value_type, allowed_values_json, default_value, description FROM localization_property_specs WHERE key = ?",
                    (key,),
                )
            else:
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
            return jsonify({"error": str(e), "hint": "Apply continuuuum_localization_schema.sql"}), 500

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
            err = validate_property_value(conn, property_key, property_value)
            if err:
                conn.close()
                return jsonify({"error": err}), 400
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
        entry_id = request.args.get("entryId")
        binding_kind = request.args.get("bindingKind")
        selection_text = request.args.get("selectionText")
        try:
            conn = get_conn()
            ensure_clause_binding_columns(conn)
            if selection_text is not None:
                cur = conn.execute(
                    """SELECT b.*, s.script_text AS draft_script_text FROM localization_clause_bindings b
                       LEFT JOIN draft_episode_script s ON s.id = b.draft_script_id
                       WHERE b.selection_text = ?
                       ORDER BY b.updated_at DESC""",
                    (selection_text,),
                )
            elif draft_episode_id:
                cur = conn.execute(
                    """SELECT b.*, s.script_text AS draft_script_text FROM localization_clause_bindings b
                       JOIN draft_episode_script s ON s.id = b.draft_script_id
                       WHERE s.draft_episode_id = ?""",
                    (draft_episode_id,),
                )
            elif draft_script_id:
                cur = conn.execute(
                    """SELECT b.*, s.script_text AS draft_script_text FROM localization_clause_bindings b
                       LEFT JOIN draft_episode_script s ON s.id = b.draft_script_id
                       WHERE b.draft_script_id = ?""",
                    (draft_script_id,),
                )
            elif entry_id:
                cur = conn.execute(
                    "SELECT * FROM localization_clause_bindings WHERE entry_id = ?",
                    (entry_id,),
                )
            else:
                conn.close()
                return jsonify({"items": []}), 200
            items = [
                binding_row(r, r["draft_script_text"] if "draft_script_text" in r.keys() else "")
                for r in cur.fetchall()
            ]
            if binding_kind:
                items = [i for i in items if i.get("bindingKind") == binding_kind]
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clauses/effective", methods=["GET"])
    def get_effective_clause_properties():
        draft_episode_id = request.args.get("draftEpisodeId")
        char_start = request.args.get("charStart", type=int)
        char_end = request.args.get("charEnd", type=int)
        if not draft_episode_id or char_start is None or char_end is None:
            return jsonify({"error": "draftEpisodeId, charStart, charEnd required"}), 400
        try:
            conn = get_conn()
            ensure_clause_binding_columns(conn)
            props = effective_properties_for_span(conn, draft_episode_id, char_start, char_end)
            conn.close()
            return jsonify({"properties": props}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clause-bindings", methods=["POST"])
    def create_clause_binding():
        body = request.get_json() or {}
        binding_kind = body.get("bindingKind", "property")
        if binding_kind not in BINDING_KINDS:
            return jsonify({"error": f"bindingKind must be one of {sorted(BINDING_KINDS)}"}), 400
        property_key = body.get("propertyKey", "")
        property_value = body.get("propertyValue", "")
        if binding_kind == "property" and property_key:
            try:
                conn = get_conn()
                ensure_clause_binding_columns(conn)
                err = validate_property_value(conn, property_key, property_value)
                conn.close()
                if err:
                    return jsonify({"error": err}), 400
            except sqlite3.OperationalError as e:
                return jsonify({"error": str(e)}), 500
        try:
            conn = get_conn()
            ensure_clause_binding_columns(conn)
            script_text = body.get("scriptText", "")
            ref = resolve_clause_ref_farey(conn, body, script_text)
            draft_script_id = resolve_draft_script_id(conn, body) or ref.draft_script_id
            ref.draft_script_id = draft_script_id
            bid = str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO localization_clause_bindings (
                    id, episode_script_id, draft_script_id,
                    farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    char_start, char_end, selection_text, property_key, property_value,
                    binding_kind, ast_node_id, prompt_placeholder_name, entry_id, created_at, updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    bid,
                    ref.episode_script_id or body.get("episodeScriptId"),
                    draft_script_id,
                    ref.farey_left_num,
                    ref.farey_left_den,
                    ref.farey_right_num,
                    ref.farey_right_den,
                    ref.char_start,
                    ref.char_end,
                    ref.selection_text or body.get("selectionText", ""),
                    property_key,
                    property_value,
                    binding_kind,
                    ref.ast_node_id or body.get("astNodeId"),
                    body.get("promptPlaceholderName"),
                    ref.entry_id or body.get("entryId"),
                    now,
                    now,
                ),
            )
            conn.commit()
            conn.close()
            return jsonify({"id": bid, "clauseRef": ref.to_api()}), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clause-bindings/<binding_id>", methods=["PATCH"])
    def patch_clause_binding(binding_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            ensure_clause_binding_columns(conn)
            pk = body.get("propertyKey")
            pv = body.get("propertyValue")
            if pk is not None and pv is not None:
                err = validate_property_value(conn, pk, pv)
                if err:
                    conn.close()
                    return jsonify({"error": err}), 400
            sets = []
            params = []
            for field, col in (
                ("propertyKey", "property_key"),
                ("propertyValue", "property_value"),
                ("bindingKind", "binding_kind"),
                ("entryId", "entry_id"),
                ("charStart", "char_start"),
                ("charEnd", "char_end"),
                ("selectionText", "selection_text"),
            ):
                if field in body:
                    sets.append(f"{col} = ?")
                    params.append(body[field])
            if sets:
                sets.append("updated_at = ?")
                params.append(_now())
                params.append(binding_id)
                conn.execute(
                    f"UPDATE localization_clause_bindings SET {', '.join(sets)} WHERE id = ?",
                    params,
                )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/clause-bindings/<binding_id>", methods=["DELETE"])
    def delete_clause_binding(binding_id: str):
        try:
            conn = get_conn()
            conn.execute("DELETE FROM localization_clause_bindings WHERE id = ?", (binding_id,))
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/localization/change-lists", methods=["GET"])
    def list_change_lists_by_draft():
        draft_episode_id = request.args.get("draftEpisodeId")
        if not draft_episode_id:
            return jsonify({"error": "draftEpisodeId required"}), 400
        try:
            conn = get_conn()
            row = get_active_change_list(conn, draft_episode_id)
            if not row:
                conn.close()
                return jsonify({"item": None}), 200
            items_cur = conn.execute(
                """SELECT * FROM localization_change_list_items
                   WHERE change_list_id = ? AND superseded_at IS NULL ORDER BY sort_order""",
                (row["id"],),
            )
            items = [_change_item_row(r) for r in items_cur.fetchall()]
            conn.close()
            return jsonify({**_change_list_row(row), "items": items}), 200
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
            auth_err = require_draft_author(conn, draft_id, get_user())
            if auth_err:
                conn.close()
                return jsonify({"error": auth_err}), 403
            blocked = draft_blocks_author_edit(conn, draft_id)
            if blocked:
                conn.close()
                return jsonify({"error": f"draft change list is {blocked}; withdraw before editing"}), 409
            bindings = _load_bindings_for_draft(conn, draft_id)
            required, warnings, updated = audit_edit(old_text, new_text, bindings)
            try:
                from continuuuum_api.mod_db import audit_mayor_dog_mod_sections
            except ImportError:
                from mod_db import audit_mayor_dog_mod_sections
            mod_required = audit_mayor_dog_mod_sections(conn, draft_id, old_text, new_text)
            required = list(required) + mod_required
            cl_id, revision, req_out, warn_out = merge_change_list(conn, draft_id, required, warnings)
            try:
                from continuuuum_api.localization_helpers import upsert_draft_script_text
            except ImportError:
                from localization_helpers import upsert_draft_script_text
            upsert_draft_script_text(conn, draft_id, new_text)
            for b in updated:
                conn.execute(
                    "UPDATE localization_clause_bindings SET char_start = ?, char_end = ?, updated_at = ? WHERE id = ?",
                    (b["char_start"], b["char_end"], _now(), b["id"]),
                )
            conn.commit()
            conn.close()
            return jsonify(
                {
                    "changeListId": cl_id,
                    "revision": revision,
                    "required": req_out,
                    "warnings": warn_out,
                }
            ), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/drafts/episodes/<draft_id>/apply-binding-edit", methods=["POST"])
    def apply_binding_edit(draft_id: str):
        body = request.get_json() or {}
        binding_id = body.get("bindingId")
        if not binding_id:
            return jsonify({"error": "bindingId required"}), 400
        try:
            conn = get_conn()
            _ensure_review_columns(conn)
            auth_err = require_draft_author(conn, draft_id, get_user())
            if auth_err:
                conn.close()
                return jsonify({"error": auth_err}), 403
            withdraw_change_list_if_in_review(conn, draft_id)
            blocked = draft_blocks_author_edit(conn, draft_id)
            if blocked:
                conn.close()
                return jsonify({"error": f"draft change list is {blocked}; withdraw before editing"}), 409
            result, err = execute_binding_edit(conn, draft_id, body)
            if err:
                conn.close()
                status = 404 if err == "binding not found for draft" else 400
                return jsonify({"error": err}), status
            conn.commit()
            conn.close()
            return jsonify(result), 200
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
            row = conn.execute(
                "SELECT workflow_status FROM localization_change_lists WHERE id = ?",
                (change_list_id,),
            ).fetchone()
            if not row:
                conn.close()
                return jsonify({"error": "not found"}), 404
            status = row["workflow_status"]
            now = _now()
            for item in body.get("items") or []:
                if item.get("id"):
                    conn.execute(
                        "UPDATE localization_change_list_items SET user_acknowledged = ? WHERE id = ? AND change_list_id = ?",
                        (1 if item.get("userAcknowledged") else 0, item["id"], change_list_id),
                    )
            if status == "in_review":
                conn.commit()
                conn.close()
                return jsonify({"ok": True, "ackOnly": True}), 200
            conn.execute(
                "UPDATE localization_change_lists SET last_saved_at = ?, updated_at = ? WHERE id = ? AND workflow_status IN ('new', 'in_progress')",
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
            row = conn.execute(
                "SELECT workflow_status, draft_episode_id FROM localization_change_lists WHERE id = ?",
                (change_list_id,),
            ).fetchone()
            if not row:
                conn.close()
                return jsonify({"error": "not found"}), 404
            if row["workflow_status"] in ("in_review", "submitted"):
                conn.close()
                return jsonify({"ok": True, "alreadySubmitted": True, "workflowStatus": row["workflow_status"]}), 200
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
                try:
                    from continuuuum_api.localization_helpers import ensure_reviewer_rows_for_submission
                except ImportError:
                    from localization_helpers import ensure_reviewer_rows_for_submission
                ensure_reviewer_rows_for_submission(conn, row["draft_episode_id"], change_list_id)
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
                cl_revs = conn.execute(
                    "SELECT user_id FROM localization_change_list_reviewers WHERE change_list_id = ?",
                    (change_list_id,),
                ).fetchall()
                assigned = {r["reviewer_user_id"] for r in revs}
                for r in cl_revs:
                    if r["user_id"] in assigned:
                        continue
                    create_notification(
                        conn,
                        r["user_id"],
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

    @app.route("/api/localization/change-lists/<change_list_id>/reviewers", methods=["POST"])
    def assign_change_list_reviewer(change_list_id: str):
        body = request.get_json() or {}
        user_id = body.get("userId")
        if not user_id:
            return jsonify({"error": "userId required"}), 400
        try:
            conn = get_conn()
            conn.execute(
                """INSERT OR IGNORE INTO localization_change_list_reviewers (change_list_id, user_id, role)
                   VALUES (?, ?, ?)""",
                (change_list_id, user_id, body.get("role", "reviewer")),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 201
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
            return jsonify({"error": str(e), "hint": "Apply continuuuum_localization_workflow_schema.sql"}), 500


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
    ensure_clause_binding_columns(conn)
    cur = conn.execute(
        """SELECT b.* FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ?""",
        (draft_id,),
    )
    return [dict(r) for r in cur.fetchall()]


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
