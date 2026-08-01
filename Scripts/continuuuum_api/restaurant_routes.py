"""Restaurant / kitchen sim routes + gov-glove /restaurants page."""

from __future__ import annotations

from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

try:
    from continuuuum_api.restaurant_db import (
        BIN_STRATEGIES,
        ORDER_STATUSES,
        build_retinue_treemap,
        chef_card_attachment_graph,
        create_order,
        ensure_restaurant_tables,
        get_menu,
        get_order,
        get_restaurant,
        list_commodity_schedules,
        list_ingredients,
        list_orders,
        list_restaurants,
        list_retinue,
        list_supply_links,
        patch_order,
        patch_order_status,
        replace_commodity_schedules,
        replace_menu,
        upsert_retinue,
    )
except ImportError:
    from restaurant_db import (
        BIN_STRATEGIES,
        ORDER_STATUSES,
        build_retinue_treemap,
        chef_card_attachment_graph,
        create_order,
        ensure_restaurant_tables,
        get_menu,
        get_order,
        get_restaurant,
        list_commodity_schedules,
        list_ingredients,
        list_orders,
        list_restaurants,
        list_retinue,
        list_supply_links,
        patch_order,
        patch_order_status,
        replace_commodity_schedules,
        replace_menu,
        upsert_retinue,
    )

GetConn = Callable[[], Any]

STATIC_RESTAURANTS = Path(__file__).resolve().parent / "static" / "restaurants"

CHEF_DISCOVERY_TOKENS = (
    "chef",
    "cook",
    "kitchen",
    "sear",
    "filet",
    "plating",
    "line-chef",
    "prep",
)

THREAT_DISCOVERY_TOKENS = (
    "threat",
    "on-edge",
    "all-clear",
    "under-attack",
    "potential-intruders",
    "alert",
)


def register_restaurant_routes(app: Flask, get_conn: GetConn) -> None:
    def _ensure(conn):
        ensure_restaurant_tables(conn)

    @app.route("/restaurants")
    @app.route("/restaurants/<path:subpath>")
    def serve_restaurants(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_RESTAURANTS, subpath)
        return send_from_directory(STATIC_RESTAURANTS, "index.html")

    @app.route("/api/restaurant/list", methods=["GET"])
    def api_restaurant_list():
        conn = get_conn()
        try:
            _ensure(conn)
            return jsonify({"restaurants": list_restaurants(conn)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>", methods=["GET"])
    def api_restaurant_get(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            r = get_restaurant(conn, rid)
            if not r:
                return jsonify({"error": "not found"}), 404
            return jsonify(r)
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/menu", methods=["GET", "PUT"])
    def api_restaurant_menu(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            if request.method == "GET":
                return jsonify({"menu": get_menu(conn, rid)})
            body = request.get_json(silent=True) or {}
            items = body.get("menu") or body.get("items") or []
            return jsonify({"menu": replace_menu(conn, rid, items)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/orders", methods=["GET", "POST"])
    def api_restaurant_orders(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            if request.method == "GET":
                return jsonify({"orders": list_orders(conn, rid)})
            body = request.get_json(silent=True) or {}
            order = create_order(conn, rid, body)
            # Sim call-chain event (no payment)
            return jsonify({"order": order, "event": {"type": "order.created", "orderId": order["id"], "status": order["status"]}})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/orders/<int:oid>/status", methods=["PATCH"])
    def api_restaurant_order_status(rid: int, oid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(silent=True) or {}
            if not body.get("status") and "ticketLabel" not in body and "ticket_label" not in body and "notes" not in body:
                return jsonify({"error": "status or ticket fields required", "allowed": list(ORDER_STATUSES)}), 400
            try:
                order = patch_order(conn, rid, oid, body)
            except ValueError as e:
                return jsonify({"error": str(e), "allowed": list(ORDER_STATUSES)}), 400
            if not order:
                return jsonify({"error": "not found"}), 404
            return jsonify(
                {
                    "order": order,
                    "event": {"type": "order.status", "orderId": oid, "status": order.get("status")},
                }
            )
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/retinue", methods=["GET", "PUT"])
    def api_restaurant_retinue(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            if request.method == "GET":
                return jsonify({"retinue": list_retinue(conn, rid)})
            body = request.get_json(silent=True) or {}
            members = body.get("retinue") or body.get("members") or []
            return jsonify({"retinue": upsert_retinue(conn, rid, members)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/retinue/treemap", methods=["GET"])
    def api_restaurant_retinue_treemap(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            return jsonify({"treemap": build_retinue_treemap(list_retinue(conn, rid))})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/commodities", methods=["GET", "PUT"])
    def api_restaurant_commodities(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            if request.method == "GET":
                return jsonify({"schedules": list_commodity_schedules(conn, rid)})
            body = request.get_json(silent=True) or {}
            schedules = body.get("schedules") or []
            return jsonify({"schedules": replace_commodity_schedules(conn, rid, schedules)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/ingredients", methods=["GET"])
    def api_restaurant_ingredients(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            return jsonify({"ingredients": list_ingredients(conn, rid)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/supply-links", methods=["GET"])
    def api_restaurant_supply(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            return jsonify({"supplyLinks": list_supply_links(conn, rid), "binStrategies": list(BIN_STRATEGIES)})
        finally:
            conn.close()

    @app.route("/api/restaurant/<int:rid>/chef-card-graph", methods=["GET"])
    def api_restaurant_chef_graph(rid: int):
        conn = get_conn()
        try:
            _ensure(conn)
            if not get_restaurant(conn, rid):
                return jsonify({"error": "not found"}), 404
            return jsonify(chef_card_attachment_graph(conn, rid))
        finally:
            conn.close()

    @app.route("/api/restaurant/meta", methods=["GET"])
    def api_restaurant_meta():
        return jsonify(
            {
                "orderStatuses": list(ORDER_STATUSES),
                "binStrategies": list(BIN_STRATEGIES),
                "discoveryTokens": {"chef": list(CHEF_DISCOVERY_TOKENS), "threat": list(THREAT_DISCOVERY_TOKENS)},
            }
        )
