"""Flask routes for lemma quest compile, sessions, spatial nodes, summaries, and art."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def register_quest_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    try:
        from continuum_api.quest_parser import LITTLE_PRINCE_FIXTURE, compile_quest_to_json
        from continuum_api.quest_db import (
            activate_objective,
            complete_objective,
            create_session,
            ensure_quest_schema,
            find_objective_row,
            load_compiled_set,
            save_compiled_set,
            save_summary,
            sync_goals,
        )
        from continuum_api.quest_art import generate_art, generate_summary, inpaint_art, suggest_style
        from continuum_api.quest_spatial import list_spatial_nodes
    except ImportError:
        from quest_parser import LITTLE_PRINCE_FIXTURE, compile_quest_to_json
        from quest_db import (
            activate_objective,
            complete_objective,
            create_session,
            ensure_quest_schema,
            find_objective_row,
            load_compiled_set,
            save_compiled_set,
            save_summary,
            sync_goals,
        )
        from quest_art import generate_art, generate_summary, inpaint_art, suggest_style
        from quest_spatial import list_spatial_nodes

    @app.route("/api/quest/compile", methods=["POST"])
    def quest_compile():
        body = request.get_json(silent=True) or {}
        text = body.get("text") or body.get("lemmaPrompt") or ""
        default_set = body.get("setId") or "quest-set"
        compiled = compile_quest_to_json(text, default_set)
        errors = [i for i in compiled.get("issues", []) if i.get("level") == "error"]
        status = 200 if not errors else 400
        return jsonify({"ok": not errors, "compiled": compiled}), status

    @app.route("/api/thesaurus/entries/<path:entry_id>/compile-quest", methods=["POST"])
    def compile_quest_for_entry(entry_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            ensure_quest_schema(conn)
            text = body.get("text")
            if not text:
                cur = conn.execute(
                    "SELECT lemma_prompt FROM thesaurus_lemma_overlays WHERE target_entry_id = ?",
                    (entry_id,),
                )
                row = cur.fetchone()
                text = row[0] if row else ""
            default_set = body.get("setId") or entry_id
            compiled = compile_quest_to_json(text or "", default_set)
            errors = [i for i in compiled.get("issues", []) if i.get("level") == "error"]
            if not errors and body.get("persist", True):
                save_compiled_set(
                    conn,
                    set_id=compiled.get("setId") or default_set,
                    lemma_entry_id=entry_id,
                    title=body.get("title") or compiled.get("title") or default_set,
                    compiled=compiled,
                )
                conn.commit()
            status = 200 if not errors else 400
            return jsonify({"ok": not errors, "compiled": compiled, "entryId": entry_id}), status
        finally:
            conn.close()

    @app.route("/api/quest/sets/<set_id>", methods=["GET"])
    def quest_get_set(set_id: str):
        conn = get_conn()
        try:
            compiled = load_compiled_set(conn, set_id)
            if not compiled:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"ok": True, "compiled": compiled}), 200
        finally:
            conn.close()

    @app.route("/api/quest/spatial-nodes", methods=["GET"])
    def quest_spatial_nodes():
        conn = get_conn()
        try:
            spatial_id = request.args.get("spatial4dId") or request.args.get("spatial_4d_id")
            mode = request.args.get("mode") or "4d"
            narrative_t_raw = request.args.get("narrativeT") or request.args.get("narrative_t")
            narrative_t = float(narrative_t_raw) if narrative_t_raw not in (None, "") else None
            project_id = request.args.get("project_id")
            return jsonify(
                list_spatial_nodes(
                    conn,
                    spatial4d_id=spatial_id,
                    mode=mode,
                    narrative_t=narrative_t,
                    project_id=project_id,
                )
            ), 200
        finally:
            conn.close()

    @app.route("/api/quest/session/open", methods=["POST"])
    def quest_session_open():
        body = request.get_json(silent=True) or {}
        set_id = body.get("setId") or body.get("set_id") or body.get("questSetId")
        if not set_id:
            return jsonify({"error": "setId required"}), 400
        conn = get_conn()
        try:
            tenant = request.headers.get("X-Tenant-ID") or body.get("tenant") or "default"
            view = create_session(
                conn,
                set_id=set_id,
                tenant=tenant,
                user_id=get_current_user(),
                trace_id=body.get("trace_id") or body.get("traceId"),
            )
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 404
        finally:
            conn.close()

    @app.route("/api/quest/session/<session_id>/objective/activate", methods=["POST"])
    def quest_objective_activate(session_id: str):
        body = request.get_json(silent=True) or {}
        objective_id = body.get("objectiveId") or body.get("objective_id")
        if not objective_id:
            return jsonify({"error": "objectiveId required"}), 400
        conn = get_conn()
        try:
            view = activate_objective(conn, session_id, objective_id)
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/quest/session/<session_id>/objective/complete", methods=["POST"])
    def quest_objective_complete(session_id: str):
        body = request.get_json(silent=True) or {}
        objective_id = body.get("objectiveId") or body.get("objective_id")
        if not objective_id:
            return jsonify({"error": "objectiveId required"}), 400
        conn = get_conn()
        try:
            view = complete_objective(conn, session_id, objective_id)
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/quest/session/<session_id>/goals/sync", methods=["POST"])
    def quest_session_sync_goals(session_id: str):
        body = request.get_json(silent=True) or {}
        goals = body.get("goals") or body.get("goalFlags") or {}
        if isinstance(goals, list):
            merged: dict[str, bool] = {}
            for item in goals:
                if isinstance(item, dict) and item.get("key"):
                    merged[str(item["key"])] = bool(item.get("value"))
            goals = merged
        conn = get_conn()
        try:
            view = sync_goals(conn, session_id, goals)
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/quest/map/refresh", methods=["POST"])
    def quest_map_refresh():
        body = request.get_json(silent=True) or {}
        return jsonify(
            {
                "ok": True,
                "spatial4dId": body.get("spatial4dId"),
                "narrativeT": body.get("narrativeT"),
                "refreshedAt": _now(),
            }
        ), 200

    @app.route("/api/quest/summary/style-suggest", methods=["POST"])
    def quest_summary_style_suggest():
        body = request.get_json(silent=True) or {}
        return jsonify(suggest_style(body)), 200

    @app.route("/api/quest/summary/generate", methods=["POST"])
    def quest_summary_generate():
        body = request.get_json(silent=True) or {}
        result = generate_summary(body)
        set_id = body.get("setId")
        objective_id = body.get("objectiveId")
        if set_id and objective_id:
            conn = get_conn()
            try:
                ensure_quest_schema(conn)
                suggestion_id = str(uuid.uuid4())
                conn.execute(
                    """
                    INSERT INTO quest_suggestions
                        (id, quest_set_id, objective_id, kind, prompt, style_hint, suggestion_json, status, created_at)
                    VALUES (?, ?, ?, 'summary', ?, ?, ?, 'pending', ?)
                    """,
                    (
                        suggestion_id,
                        set_id,
                        objective_id,
                        body.get("prompt") or "",
                        body.get("styleHint") or body.get("style"),
                        json.dumps(result),
                        _now(),
                    ),
                )
                conn.commit()
                result["suggestionId"] = suggestion_id
            finally:
                conn.close()
        return jsonify(result), 200

    @app.route("/api/quest/summary/accept", methods=["POST"])
    def quest_summary_accept():
        body = request.get_json(silent=True) or {}
        suggestion_id = body.get("suggestionId")
        set_id = body.get("setId")
        objective_id = body.get("objectiveId")
        text = body.get("text")
        conn = get_conn()
        try:
            ensure_quest_schema(conn)
            if suggestion_id:
                row = conn.execute(
                    "SELECT suggestion_json, quest_set_id, objective_id FROM quest_suggestions WHERE id = ?",
                    (suggestion_id,),
                ).fetchone()
                if row:
                    payload = json.loads(row[0])
                    text = text or payload.get("text")
                    set_id = set_id or row[1]
                    objective_id = objective_id or row[2]
                    conn.execute(
                        "UPDATE quest_suggestions SET status = 'accepted', accepted_at = ? WHERE id = ?",
                        (_now(), suggestion_id),
                    )
            if not set_id or not objective_id or not text:
                return jsonify({"error": "setId, objectiveId, and text required"}), 400
            obj_row = find_objective_row(conn, set_id, objective_id)
            if not obj_row:
                return jsonify({"error": "objective_not_found"}), 404
            summary = save_summary(
                conn,
                objective_row_id=obj_row["id"],
                mode="generated",
                text=text,
                style_profile=body.get("styleProfile"),
                suggestion_id=suggestion_id,
            )
            conn.commit()
            return jsonify({"ok": True, "summary": summary}), 200
        finally:
            conn.close()

    @app.route("/api/quest/art/generate", methods=["POST"])
    def quest_art_generate():
        body = request.get_json(silent=True) or {}
        return jsonify(generate_art(body)), 200

    @app.route("/api/quest/art/inpaint", methods=["POST"])
    def quest_art_inpaint():
        body = request.get_json(silent=True) or {}
        return jsonify(inpaint_art(body)), 200

    @app.route("/api/quest/fixture/little-prince", methods=["GET"])
    def quest_fixture():
        compiled = compile_quest_to_json(LITTLE_PRINCE_FIXTURE, "little-prince-tour")
        return jsonify({"ok": True, "compiled": compiled, "text": LITTLE_PRINCE_FIXTURE}), 200
