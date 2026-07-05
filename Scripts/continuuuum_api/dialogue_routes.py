"""Flask routes for lemma dialogue compile, sessions, suggestions, and speech."""

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


def register_dialogue_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    try:
        from continuuuum_api.dialogue_parser import BOOK_CONCERT_FIXTURE, compile_dialogue_to_json
        from continuuuum_api.dialogue_db import (
            advance_session,
            choose_session,
            create_session,
            ensure_dialogue_schema,
            get_session,
            load_compiled_set,
            save_compiled_set,
            sync_goals,
        )
        from continuuuum_api.dialogue_speech import inpaint_speech, synthesize_speech
    except ImportError:
        from dialogue_parser import BOOK_CONCERT_FIXTURE, compile_dialogue_to_json
        from dialogue_db import (
            advance_session,
            choose_session,
            create_session,
            ensure_dialogue_schema,
            get_session,
            load_compiled_set,
            save_compiled_set,
            sync_goals,
        )
        from dialogue_speech import inpaint_speech, synthesize_speech

    @app.route("/api/dialogue/compile", methods=["POST"])
    def dialogue_compile():
        body = request.get_json(silent=True) or {}
        text = body.get("text") or body.get("lemmaPrompt") or ""
        default_set = body.get("setId") or "dialogue-set"
        compiled = compile_dialogue_to_json(text, default_set)
        errors = [i for i in compiled.get("issues", []) if i.get("level") == "error"]
        status = 200 if not errors else 400
        return jsonify({"ok": not errors, "compiled": compiled}), status

    @app.route("/api/thesaurus/entries/<path:entry_id>/compile-dialogue", methods=["POST"])
    def compile_dialogue_for_entry(entry_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            ensure_dialogue_schema(conn)
            text = body.get("text")
            if not text:
                cur = conn.execute(
                    "SELECT lemma_prompt FROM thesaurus_lemma_overlays WHERE target_entry_id = ?",
                    (entry_id,),
                )
                row = cur.fetchone()
                text = row[0] if row else ""
            default_set = body.get("setId") or entry_id
            compiled = compile_dialogue_to_json(text or "", default_set)
            errors = [i for i in compiled.get("issues", []) if i.get("level") == "error"]
            if not errors and body.get("persist", True):
                save_compiled_set(
                    conn,
                    set_id=compiled.get("setId") or default_set,
                    lemma_entry_id=entry_id,
                    name=body.get("name") or compiled.get("setId") or default_set,
                    compiled=compiled,
                )
                conn.commit()
            status = 200 if not errors else 400
            return jsonify({"ok": not errors, "compiled": compiled, "entryId": entry_id}), status
        finally:
            conn.close()

    @app.route("/api/dialogue/sets/<set_id>", methods=["GET"])
    def dialogue_get_set(set_id: str):
        conn = get_conn()
        try:
            compiled = load_compiled_set(conn, set_id)
            if not compiled:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"ok": True, "compiled": compiled}), 200
        finally:
            conn.close()

    @app.route("/api/dialogue/session/open", methods=["POST"])
    def dialogue_session_open():
        body = request.get_json(silent=True) or {}
        set_id = body.get("setId") or body.get("set_id")
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

    @app.route("/api/dialogue/session/<session_id>/choose", methods=["POST"])
    def dialogue_session_choose(session_id: str):
        body = request.get_json(silent=True) or {}
        answer_id = body.get("answerId") or body.get("answer")
        if not answer_id:
            return jsonify({"error": "answerId required"}), 400
        conn = get_conn()
        try:
            view = choose_session(conn, session_id, answer_id)
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/dialogue/session/<session_id>/advance", methods=["POST"])
    def dialogue_session_advance(session_id: str):
        conn = get_conn()
        try:
            view = advance_session(conn, session_id)
            conn.commit()
            return jsonify(view), 200
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/dialogue/session/<session_id>/goals/sync", methods=["POST"])
    def dialogue_session_sync_goals(session_id: str):
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

    @app.route("/api/dialogue/suggest/generate", methods=["POST"])
    def dialogue_suggest_generate():
        body = request.get_json(silent=True) or {}
        set_id = body.get("setId")
        parent_node_id = body.get("parentNodeId")
        if not set_id:
            return jsonify({"error": "setId required"}), 400
        conn = get_conn()
        try:
            ensure_dialogue_schema(conn)
            suggestion_id = str(uuid.uuid4())
            context = body.get("context") or body.get("text") or ""
            candidates = [
                {
                    "text": f"{context} (candidate A)".strip(),
                    "presentation": "text",
                    "speakerKey": body.get("speakerKey"),
                },
                {
                    "text": f"{context} (candidate B)".strip(),
                    "presentation": "text",
                    "speakerKey": body.get("speakerKey"),
                },
            ]
            suggestion_json = {"candidates": candidates, "styleNotes": "Stub LLM style notes"}
            conn.execute(
                """
                INSERT INTO dialogue_suggestions (id, set_id, parent_node_id, suggestion_json, status, source, created_at)
                VALUES (?, ?, ?, ?, 'pending', 'llm', ?)
                """,
                (suggestion_id, set_id, parent_node_id, json.dumps(suggestion_json), _now()),
            )
            conn.commit()
            return jsonify({"ok": True, "suggestionId": suggestion_id, **suggestion_json}), 200
        finally:
            conn.close()

    @app.route("/api/dialogue/suggest/accept", methods=["POST"])
    def dialogue_suggest_accept():
        body = request.get_json(silent=True) or {}
        suggestion_id = body.get("suggestionId")
        candidate_index = int(body.get("candidateIndex", 0))
        if not suggestion_id:
            return jsonify({"error": "suggestionId required"}), 400
        conn = get_conn()
        try:
            ensure_dialogue_schema(conn)
            cur = conn.execute(
                "SELECT set_id, parent_node_id, suggestion_json FROM dialogue_suggestions WHERE id = ?",
                (suggestion_id,),
            )
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not_found"}), 404
            payload = json.loads(row[2])
            candidates = payload.get("candidates") or []
            if candidate_index < 0 or candidate_index >= len(candidates):
                return jsonify({"error": "invalid_candidate_index"}), 400
            chosen = candidates[candidate_index]
            compiled = load_compiled_set(conn, row[0])
            if not compiled:
                return jsonify({"error": "set_not_found"}), 404
            new_node = {
                "id": f"n-suggest-{suggestion_id[:8]}",
                "kind": "line",
                "text": chosen.get("text", ""),
                "presentation": chosen.get("presentation", "text"),
                "speakerKey": chosen.get("speakerKey"),
                "children": [],
                "answers": [],
            }
            parent_id = row[1]
            if parent_id:
                _insert_under_parent(compiled.get("nodes") or [], parent_id, new_node)
            else:
                (compiled.setdefault("nodes", [])).append(new_node)
            save_compiled_set(
                conn,
                set_id=row[0],
                lemma_entry_id=None,
                name=row[0],
                compiled=compiled,
            )
            conn.execute(
                "UPDATE dialogue_suggestions SET status = 'accepted' WHERE id = ?",
                (suggestion_id,),
            )
            conn.commit()
            return jsonify({"ok": True, "node": new_node, "compiled": compiled}), 200
        finally:
            conn.close()

    @app.route("/api/dialogue/speech/synthesize", methods=["POST"])
    def dialogue_speech_synthesize():
        body = request.get_json(silent=True) or {}
        try:
            result = synthesize_speech(body, get_conn())
            return jsonify(result), 200 if result.get("ok") else 400
        except Exception as e:
            return jsonify({"ok": False, "error": str(e)}), 500

    @app.route("/api/dialogue/speech/inpaint", methods=["POST"])
    def dialogue_speech_inpaint():
        body = request.get_json(silent=True) or {}
        try:
            result = inpaint_speech(body)
            return jsonify(result), 200 if result.get("ok") else 400
        except Exception as e:
            return jsonify({"ok": False, "error": str(e)}), 500

    @app.route("/api/dialogue/fixture/book-concert", methods=["GET"])
    def dialogue_fixture():
        compiled = compile_dialogue_to_json(BOOK_CONCERT_FIXTURE, "book-concert")
        return jsonify({"ok": True, "compiled": compiled, "text": BOOK_CONCERT_FIXTURE}), 200


def _insert_under_parent(nodes: list[dict[str, Any]], parent_id: str, new_node: dict[str, Any]) -> bool:
    for n in nodes:
        if n.get("id") == parent_id:
            n.setdefault("children", []).append(new_node)
            return True
        if _insert_under_parent(n.get("children") or [], parent_id, new_node):
            return True
    return False
