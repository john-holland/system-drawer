"""Proxy resaurce Cave chat routes for Continuum UI."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
import uuid

from flask import jsonify, request

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


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


def register_chat_routes(app) -> None:
    @app.route("/api/chat/rooms/<story_id>", methods=["GET"])
    def chat_room_for_story(story_id: str):
        out, status = _cave("chat/room/get-for-story", {"story_id": story_id})
        return jsonify(out), status if status != 200 else (200 if out.get("ok") else 404)

    @app.route("/api/chat/messages", methods=["GET", "POST"])
    def chat_messages():
        if request.method == "GET":
            room_id = request.args.get("chatRoomId") or request.args.get("chat_room_id")
            out, status = _cave("chat/messages/list", {"chat_room_id": room_id})
            return jsonify(out), status
        body = request.get_json(silent=True) or {}
        out, status = _cave(
            "chat/message/send",
            {
                "chat_room_id": body.get("chatRoomId") or body.get("chat_room_id"),
                "sender": body.get("sender") or "user",
                "content": body.get("content") or "",
                "type": body.get("type") or "user",
            },
        )
        return jsonify(out), status

    @app.route("/api/chat/ensure-story-room", methods=["POST"])
    def chat_ensure_story():
        body = request.get_json(silent=True) or {}
        out, status = _cave("chat/room/ensure-for-story", body)
        return jsonify(out), status

    @app.route("/api/chat/ensure-table-read-room", methods=["POST"])
    def chat_ensure_table_read():
        body = request.get_json(silent=True) or {}
        out, status = _cave("chat/room/ensure-for-table-read", body)
        return jsonify(out), status

    @app.route("/api/chat/rooms/table-read/<session_id>", methods=["GET"])
    def chat_room_for_table_read(session_id: str):
        out, status = _cave("chat/room/get-for-table-read", {"session_id": session_id})
        return jsonify(out), status if status != 200 else (200 if out.get("ok") else 404)
