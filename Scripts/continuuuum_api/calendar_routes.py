"""Calendar sync subscription CRUD."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def register_calendar_routes(app, get_conn: GetConn) -> None:
    @app.route("/api/calendar/subscriptions", methods=["GET", "POST"])
    def calendar_subscriptions():
        conn = get_conn()
        if request.method == "GET":
            rows = conn.execute("SELECT * FROM calendar_sync_subscriptions ORDER BY created_at DESC").fetchall()
            conn.close()
            return jsonify({"subscriptions": [dict(r) for r in rows]})
        body = request.get_json(silent=True) or {}
        sid = str(uuid.uuid4())
        conn.execute(
            """INSERT INTO calendar_sync_subscriptions
               (id, tenant_id, story_id, resaurce_schedule_id, provider, target_url,
                oauth_token_ref, cron_expr, created_at)
               VALUES (?,?,?,?,?,?,?,?,?)""",
            (
                sid,
                body.get("tenantId") or "default",
                body.get("storyId"),
                body.get("resaurceScheduleId"),
                body.get("provider") or "ical",
                body.get("targetUrl"),
                body.get("oauthTokenRef"),
                body.get("cronExpr") or "*/15 * * * *",
                _now(),
            ),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": sid}), 201

    @app.route("/api/calendar/sync-now", methods=["POST"])
    def calendar_sync_now():
        body = request.get_json(silent=True) or {}
        import importlib.util
        sync_path = Path(__file__).resolve().parent / "scripts" / "calendar_sync.py"
        spec = importlib.util.spec_from_file_location("calendar_sync", sync_path)
        mod = importlib.util.module_from_spec(spec)
        assert spec.loader
        spec.loader.exec_module(mod)
        result = mod.run_sync(get_conn(), subscription_id=body.get("subscriptionId"), provider=body.get("provider"))
        return jsonify(result), 200 if result.get("ok") else 500
