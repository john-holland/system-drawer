"""Cave routing, Tome message delegation, login, and presence."""

from __future__ import annotations

import json
import secrets
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable
from urllib.parse import quote

from flask import jsonify, request, session

from cave_loader import build_config_overview, build_routes_overview, load_all_tome_configs, resolve_robit_transport
from commerce_db import ensure_cave_commerce_tables

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def register_cave_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    @app.before_request
    def _ensure_cave_tables():
        if not getattr(app, "_cave_commerce_ready", False):
            conn = get_conn()
            ensure_cave_commerce_tables(conn)
            conn.close()
            app._cave_commerce_ready = True

    @app.route("/api/routes")
    def api_routes():
        return jsonify(build_routes_overview()), 200

    @app.route("/api/config/overview")
    def api_config_overview():
        return jsonify(build_config_overview()), 200

    @app.route("/api/cave/hierarchy")
    def api_cave_hierarchy():
        return jsonify(build_routes_overview()), 200

    @app.route("/api/cave/config-overview")
    def api_cave_config_overview():
        return jsonify(build_config_overview()), 200

    @app.route("/api/tomes")
    def list_tomes():
        return jsonify({"items": load_all_tome_configs()}), 200

    @app.route("/api/tomes/<tome_id>")
    def get_tome(tome_id: str):
        for t in load_all_tome_configs():
            if t.get("id") == tome_id:
                return jsonify(t), 200
        return jsonify({"error": "not found"}), 404

    @app.route("/api/tomes/<tome_id>/machines/<machine_id>/message", methods=["POST"])
    def tome_machine_message(tome_id: str, machine_id: str):
        body = request.get_json(silent=True) or {}
        event = body.get("event") or body.get("action") or "PING"
        data = body.get("data") or {}
        transport = resolve_robit_transport(tome_id)
        result = _dispatch_tome_message(tome_id, machine_id, event, data, get_conn, get_current_user, app.test_client())
        return jsonify({"ok": True, "tomeId": tome_id, "machineId": machine_id, "event": event, "transport": transport, "result": result}), 200

    @app.route("/api/login", methods=["POST"])
    def api_login():
        body = request.get_json(silent=True) or {}
        username = (body.get("username") or body.get("user") or "anonymous").strip()
        password = body.get("password") or ""
        if password and password not in ("admin", "password") and username != "admin":
            return jsonify({"error": "invalid credentials"}), 401
        sid = secrets.token_hex(16)
        session["cave_sid"] = sid
        session["cave_user"] = username
        permission = "admin" if username == "admin" else "user"
        conn = get_conn()
        conn.execute(
            "INSERT OR REPLACE INTO cave_sessions (session_id, user_id, permission_level, created_at) VALUES (?, ?, ?, ?)",
            (sid, username, permission, _now()),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "user": username, "permissionLevel": permission}), 200

    @app.route("/api/editor/presence", methods=["GET", "POST"])
    def editor_presence():
        cave_or_tome_id = request.args.get("caveOrTomeId") or (request.get_json(silent=True) or {}).get("caveOrTomeId") or "continuum"
        conn = get_conn()
        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            user = body.get("user") or get_current_user()
            location = body.get("location") or request.path
            conn.execute(
                """INSERT OR REPLACE INTO editor_presence (cave_or_tome_id, user_id, location, updated_at)
                   VALUES (?, ?, ?, ?)""",
                (cave_or_tome_id, user, location, _now()),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        rows = conn.execute(
            "SELECT user_id, location, updated_at FROM editor_presence WHERE cave_or_tome_id = ?",
            (cave_or_tome_id,),
        ).fetchall()
        conn.close()
        return jsonify({"caveOrTomeId": cave_or_tome_id, "items": [dict(r) for r in rows]}), 200


def _dispatch_tome_message(
    tome_id: str,
    machine_id: str,
    event: str,
    data: dict[str, Any],
    get_conn: GetConn,
    get_current_user: Callable[[], str],
    client,
) -> dict[str, Any]:
    if tome_id == "lemma-tome" and machine_id == "browseMachine":
        q = data.get("q", "")
        r = client.get(f"/api/thesaurus/entries?q={q}&limit=12")
        return r.get_json() if r.is_json else {"status": r.status_code}
    if tome_id == "saurce-tome":
        path_map = {
            "productMachine": "/api/saurce/products",
            "investmentMachine": "/api/saurce/investments/ledger",
            "disbursementMachine": "/api/saurce/crypto/disburse",
        }
        path = path_map.get(machine_id)
        if path:
            if event in ("LIST", "GET", "LEDGER_QUERY", "QUERY"):
                r = client.get(path)
            else:
                r = client.post(path, json=data)
            return r.get_json() if r.is_json else {"status": r.status_code}
    if tome_id == "resaurce-tome" and machine_id == "legalMachine":
        if event == "QUERY":
            r = client.get("/api/legal/cases")
            return r.get_json() if r.is_json else {"status": r.status_code}
    if tome_id == "drawer-game-tome":
        if machine_id == "multiplayerClient":
            r = client.post("/api/drawer-game/stub/client", json={"event": event, **data})
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "hostAgent":
            r = client.post("/api/drawer-game/stub/host", json={"event": event, **data})
            return r.get_json() if r.is_json else {"status": r.status_code}
    if tome_id == "sql-viewer-tome":
        headers = {"X-User-ID": get_current_user()}
        if machine_id == "schemaMachine" and event in ("LOAD", "GET", "MESSAGE"):
            r = client.get("/api/sql-viewer/schema", headers=headers)
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "previewMachine":
            name = quote(data.get("tableName") or data.get("name") or "", safe="")
            limit = data.get("limit", 100)
            offset = data.get("offset", 0)
            r = client.get(
                f"/api/sql-viewer/tables/{name}/preview?limit={limit}&offset={offset}",
                headers=headers,
            )
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "queryMachine":
            r = client.post("/api/sql-viewer/query", json=data, headers=headers)
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "validateMachine":
            r = client.post("/api/sql-viewer/validate", json=data, headers=headers)
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "recipesMachine" and event in ("LOAD", "GET", "MESSAGE"):
            r = client.get("/api/sql-viewer/recipes", headers=headers)
            return r.get_json() if r.is_json else {"status": r.status_code}
    if tome_id == "table-read-tome":
        headers = {"X-User-ID": get_current_user()}
        session_id = data.get("sessionId") or data.get("session_id") or ""
        if machine_id == "sessionMachine":
            if event == "SESSION_OPEN":
                r = client.post(
                    f"/api/table-read/sessions/{quote(session_id, safe='')}/join",
                    json={"displayName": data.get("displayName") or get_current_user()},
                    headers=headers,
                )
                snap = r.get_json() if r.is_json else {"status": r.status_code}
                if r.status_code == 200 and session_id:
                    r2 = client.post(
                        f"/api/table-read/sessions/{quote(session_id, safe='')}/ensure-chat",
                        json={},
                        headers=headers,
                    )
                    if r2.is_json and r2.status_code == 200:
                        chat = r2.get_json()
                        if isinstance(snap, dict) and isinstance(chat, dict):
                            snap.setdefault("session", {})
                            if chat.get("chatRoomId"):
                                snap["session"]["chatRoomId"] = chat["chatRoomId"]
                            if chat.get("shareUrl"):
                                snap["session"]["shareUrl"] = chat["shareUrl"]
                return snap
            if event == "SESSION_END":
                r = client.post(
                    f"/api/table-read/sessions/{quote(session_id, safe='')}/end",
                    json={},
                    headers=headers,
                )
                return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "chatEnsureMachine" and event == "ENSURE_CHAT":
            r = client.post(
                f"/api/table-read/sessions/{quote(session_id, safe='')}/ensure-chat",
                json=data,
                headers=headers,
            )
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "inviteMachine" and event == "INVITE_USER":
            r = client.post(
                f"/api/table-read/sessions/{quote(session_id, safe='')}/invite",
                json={"userId": data.get("userId") or data.get("user_id")},
                headers=headers,
            )
            return r.get_json() if r.is_json else {"status": r.status_code}
        if machine_id == "messagesMachine":
            if event == "LIST_MESSAGES":
                room_id = data.get("chatRoomId") or data.get("chat_room_id") or ""
                r = client.get(
                    f"/api/chat/messages?chatRoomId={quote(room_id, safe='')}",
                    headers=headers,
                )
                return r.get_json() if r.is_json else {"status": r.status_code}
            if event == "SEND_MESSAGE":
                r = client.post(
                    "/api/chat/messages",
                    json={
                        "chatRoomId": data.get("chatRoomId") or data.get("chat_room_id"),
                        "content": data.get("content") or "",
                        "sender": data.get("sender") or get_current_user(),
                        "type": data.get("type") or "user",
                    },
                    headers=headers,
                )
                return r.get_json() if r.is_json else {"status": r.status_code}
    return {"ack": True, "event": event, "user": get_current_user()}
