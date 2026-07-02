"""Proxy resaurce Cave chat routes for Continuum UI, with local SQLite fallback."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from typing import Callable

from flask import jsonify, request

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
GetConn = Callable[[], sqlite3.Connection]

try:
    from continuum_api.local_chat_store import (
        append_message as local_append_message,
        ensure_schema as local_ensure_schema,
        ensure_story_room as local_ensure_story_room,
        ensure_table_read_room as local_ensure_table_read_room,
        find_story_room as local_find_story_room,
        list_messages as local_list_messages,
    )
except ImportError:
    from local_chat_store import (
        append_message as local_append_message,
        ensure_schema as local_ensure_schema,
        ensure_story_room as local_ensure_story_room,
        ensure_table_read_room as local_ensure_table_read_room,
        find_story_room as local_find_story_room,
        list_messages as local_list_messages,
    )


def _cave(route: str, payload: dict) -> tuple[dict, int]:
    trace = f"continuum_{uuid.uuid4().hex[:10]}"
    body = json.dumps({"route": f"resaurce:{route}", "payload": payload, "trace_id": trace}).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            return json.loads(resp.read().decode()), resp.status
    except urllib.error.HTTPError as e:
        return json.loads(e.read().decode() or "{}"), e.code
    except urllib.error.URLError as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}, 502


def _use_local(out: dict, status: int) -> bool:
    return status == 502 or out.get("error") == "resaurce_unavailable"


def register_chat_routes(app, get_conn: GetConn) -> None:
    @app.route("/api/chat/rooms/<story_id>", methods=["GET"])
    def chat_room_for_story(story_id: str):
        out, status = _cave("chat/room/get-for-story", {"story_id": story_id})
        if _use_local(out, status):
            conn = get_conn()
            room = local_find_story_room(conn, story_id)
            conn.close()
            if not room:
                return jsonify({"ok": False, "error": "not_found"}), 404
            return jsonify({"ok": True, "chat_room": room}), 200
        return jsonify(out), status if status != 200 else (200 if out.get("ok") else 404)

    @app.route("/api/chat/messages", methods=["GET", "POST"])
    def chat_messages():
        if request.method == "GET":
            room_id = request.args.get("chatRoomId") or request.args.get("chat_room_id")
            out, status = _cave("chat/messages/list", {"chat_room_id": room_id})
            if _use_local(out, status):
                conn = get_conn()
                messages = local_list_messages(conn, room_id or "")
                conn.close()
                return jsonify({"ok": True, "messages": messages, "chat_room_id": room_id}), 200
            return jsonify(out), status
        body = request.get_json(silent=True) or {}
        room_id = body.get("chatRoomId") or body.get("chat_room_id")
        out, status = _cave(
            "chat/message/send",
            {
                "chat_room_id": room_id,
                "sender": body.get("sender") or "user",
                "content": body.get("content") or "",
                "type": body.get("type") or "user",
            },
        )
        if _use_local(out, status):
            conn = get_conn()
            message = local_append_message(
                conn,
                room_id or "",
                body.get("sender") or "user",
                body.get("content") or "",
                body.get("type") or "user",
            )
            conn.close()
            if not message:
                return jsonify({"ok": False, "error": "chat_room_not_found"}), 404
            return jsonify({"ok": True, "message": message}), 200
        return jsonify(out), status

    @app.route("/api/chat/ensure-story-room", methods=["POST"])
    def chat_ensure_story():
        body = request.get_json(silent=True) or {}
        out, status = _cave("chat/room/ensure-for-story", body)
        if _use_local(out, status):
            story_id = body.get("story_id") or body.get("continuum_story_id")
            if not story_id:
                return jsonify({"ok": False, "error": "story_id required"}), 400
            conn = get_conn()
            room_id = local_ensure_story_room(
                conn,
                story_id,
                body.get("summary") or body.get("name"),
                body.get("assignees") or body.get("participants"),
                body.get("watchers"),
            )
            conn.close()
            return jsonify({"ok": True, "chat_room": {"id": room_id}}), 200
        return jsonify(out), status

    @app.route("/api/chat/ensure-table-read-room", methods=["POST"])
    def chat_ensure_table_read():
        body = request.get_json(silent=True) or {}
        out, status = _cave("chat/room/ensure-for-table-read", body)
        if _use_local(out, status):
            session_id = body.get("session_id") or body.get("continuum_table_read_session_id")
            if not session_id:
                return jsonify({"ok": False, "error": "session_id required"}), 400
            conn = get_conn()
            room_id = local_ensure_table_read_room(
                conn,
                session_id,
                body.get("summary") or body.get("name"),
                body.get("participants") or body.get("assignees"),
            )
            conn.close()
            return jsonify({"ok": True, "chat_room": {"id": room_id}}), 200
        return jsonify(out), status

    @app.route("/api/chat/rooms/table-read/<session_id>", methods=["GET"])
    def chat_room_for_table_read(session_id: str):
        out, status = _cave("chat/room/get-for-table-read", {"session_id": session_id})
        if _use_local(out, status):
            conn = get_conn()
            local_ensure_schema(conn)
            row = conn.execute(
                "SELECT * FROM chat_rooms WHERE continuum_table_read_session_id = ? LIMIT 1",
                (session_id,),
            ).fetchone()
            conn.close()
            if not row:
                return jsonify({"ok": False, "error": "not_found"}), 404
            return jsonify({"ok": True, "chat_room": {"id": row["id"]}}), 200
        return jsonify(out), status if status != 200 else (200 if out.get("ok") else 404)
