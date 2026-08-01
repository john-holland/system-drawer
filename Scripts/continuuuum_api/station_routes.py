"""Station placard routes + /stations D3 assemblage page."""

from __future__ import annotations

from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

try:
    from continuuuum_api.station_db import (
        ASSIGN_TYPES,
        STATION_KINDS,
        assemblage_graph,
        ensure_station_tables,
        list_stations,
        replace_stations,
        treemap_hierarchy,
        upsert_level_stats,
    )
except ImportError:
    from station_db import (
        ASSIGN_TYPES,
        STATION_KINDS,
        assemblage_graph,
        ensure_station_tables,
        list_stations,
        replace_stations,
        treemap_hierarchy,
        upsert_level_stats,
    )

GetConn = Callable[[], Any]
STATIC_STATIONS = Path(__file__).resolve().parent / "static" / "stations"


def register_station_routes(app: Flask, get_conn: GetConn) -> None:
    def _ensure(conn):
        ensure_station_tables(conn)

    @app.route("/stations")
    @app.route("/stations/<path:subpath>")
    def serve_stations(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_STATIONS, subpath)
        return send_from_directory(STATIC_STATIONS, "index.html")

    @app.route("/api/stations", methods=["GET", "PUT"])
    def api_stations():
        conn = get_conn()
        try:
            _ensure(conn)
            city_id = request.args.get("cityId") or (request.get_json(silent=True) or {}).get("cityId") or "demo-city"
            if request.method == "GET":
                return jsonify({"stations": list_stations(conn, city_id), "kinds": list(STATION_KINDS)})
            body = request.get_json(silent=True) or {}
            city_id = body.get("cityId") or city_id
            placards = body.get("stations") or body.get("placards") or []
            return jsonify({"stations": replace_stations(conn, city_id, placards)})
        finally:
            conn.close()

    @app.route("/api/stations/assemblage", methods=["GET"])
    def api_stations_assemblage():
        conn = get_conn()
        try:
            _ensure(conn)
            city_id = request.args.get("cityId") or "demo-city"
            return jsonify(assemblage_graph(conn, city_id))
        finally:
            conn.close()

    @app.route("/api/stations/treemap", methods=["GET"])
    def api_stations_treemap():
        conn = get_conn()
        try:
            _ensure(conn)
            city_id = request.args.get("cityId") or "demo-city"
            return jsonify({"treemap": treemap_hierarchy(conn, city_id)})
        finally:
            conn.close()

    @app.route("/api/stations/level-stats", methods=["PUT"])
    def api_stations_level_stats():
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(silent=True) or {}
            level_id = body.get("levelId") or body.get("level_id") or "default"
            city_id = body.get("cityId") or body.get("city_id") or "demo-city"
            payload = body.get("stats") or body.get("payload") or {}
            placards = body.get("stations") or body.get("placards")
            row = upsert_level_stats(conn, level_id, city_id, payload, placards)
            return jsonify({"ok": True, "levelStats": row, "stations": list_stations(conn, city_id)})
        finally:
            conn.close()

    @app.route("/api/stations/meta", methods=["GET"])
    def api_stations_meta():
        return jsonify(
            {
                "kinds": list(STATION_KINDS),
                "assignTypes": list(ASSIGN_TYPES),
            }
        )
