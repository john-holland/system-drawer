"""Cave routing, Tome message delegation, login, and presence."""

from __future__ import annotations

import secrets
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request, session

from cave_loader import build_config_overview, build_routes_overview, load_all_tome_configs, resolve_robit_transport
from cave.manifest_loader import load_cave_manifest, message_to_structural
from cave.router import handle_cave_route
from cave.tome_dispatch import resolve_tome_event_route
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

    @app.route("/cave/route", methods=["POST"])
    def cave_route():
        body = request.get_json(silent=True) or {}
        out = handle_cave_route(body, get_conn, get_current_user, app.test_client())
        status = 200 if out.get("ok") is not False else 400
        if out.get("error") == "upstream_unavailable":
            status = 502
        return jsonify(out), status

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
        manifest = load_cave_manifest()
        structural = resolve_tome_event_route(tome_id, machine_id, event, manifest)
        if structural:
            service = manifest.get("service") or "continuuuum"
            route = f"{service}:{structural}"
            out = handle_cave_route(
                {
                    "schema_version": "2.0",
                    "route": route,
                    "payload": data,
                    "trace_id": body.get("trace_id") or f"tome_{uuid.uuid4().hex[:12]}",
                    "reply_mode": "sync_http",
                },
                get_conn,
                get_current_user,
                app.test_client(),
            )
            result = out
        else:
            result = {"ack": True, "event": event, "user": get_current_user()}
        return jsonify(
            {
                "ok": True,
                "tomeId": tome_id,
                "machineId": machine_id,
                "event": event,
                "transport": transport,
                "result": result,
            }
        ), 200

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
        cave_or_tome_id = request.args.get("caveOrTomeId") or (request.get_json(silent=True) or {}).get("caveOrTomeId") or "continuuuum"
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


def cave_message(message: str, payload: dict[str, Any] | None = None, **kwargs: Any) -> dict[str, Any]:
    """Helper for scripts: resolve message alias via manifest."""
    manifest = load_cave_manifest()
    structural = message_to_structural(manifest, message)
    if not structural:
        return {"ok": False, "error": "unknown_message", "message": message}
    service = manifest.get("service") or "continuuuum"
    return handle_cave_route(
        {
            "schema_version": "2.0",
            "route": f"{service}:{structural}",
            "payload": payload or {},
            "trace_id": kwargs.get("trace_id") or f"script_{uuid.uuid4().hex[:12]}",
        },
        kwargs["get_conn"],
        kwargs.get("get_current_user") or (lambda: "system"),
        kwargs["client"],
    )
