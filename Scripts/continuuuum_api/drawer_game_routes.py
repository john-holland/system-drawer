"""system-drawer-game stub endpoints gated by GameProfile multiplayer config."""

from __future__ import annotations

import json
import sqlite3
from typing import Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def _game_allows(product_id: str | None, conn: sqlite3.Connection, flag: str) -> bool:
    if not product_id:
        return True
    row = conn.execute("SELECT game_profile_json FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
    if not row:
        return False
    gp = json.loads(row["game_profile_json"] or "{}")
    mc = gp.get("multiplayerConfig") or {}
    return bool(mc.get(flag))


def register_drawer_game_routes(app, get_conn: GetConn) -> None:
    @app.route("/api/drawer-game/stub/client", methods=["POST"])
    def drawer_game_client():
        body = request.get_json(silent=True) or {}
        product_id = body.get("productId")
        conn = get_conn()
        gp_ok = _game_allows(product_id, conn, "clientAgentRequired") if product_id else True
        conn.close()
        if product_id and not gp_ok:
            return jsonify({"error": "multiplayer client not enabled for product"}), 403
        return jsonify({"ok": True, "role": "multiplayerClient", "event": body.get("event"), "sessionId": "stub-session"}), 200

    @app.route("/api/drawer-game/stub/host", methods=["POST"])
    def drawer_game_host():
        body = request.get_json(silent=True) or {}
        product_id = body.get("productId")
        conn = get_conn()
        gp_ok = _game_allows(product_id, conn, "hostAgentRequired") if product_id else True
        conn.close()
        if product_id and not gp_ok:
            return jsonify({"error": "host agent not enabled for product"}), 403
        return jsonify({"ok": True, "role": "hostAgent", "event": body.get("event"), "hostId": "stub-host"}), 200

    @app.route("/api/drawer-game/stub/status")
    def drawer_game_status():
        return jsonify({"status": "stub", "tomeId": "drawer-game-tome"}), 200
