"""Transit Authority vehicle + building schedule Continuuuum routes."""

from __future__ import annotations

import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC_TRANSIT = Path(__file__).resolve().parent / "static" / "transit"
SCHEMA = Path(__file__).resolve().parent.parent / "continuuuum_transit_schema.sql"


def ensure_transit_tables(conn: sqlite3.Connection) -> None:
    sql = SCHEMA.read_text(encoding="utf-8") if SCHEMA.is_file() else ""
    if sql:
        conn.executescript(sql)
    else:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS transit_authority_vehicle_schedule (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                vehicle_id TEXT NOT NULL,
                route_id TEXT NOT NULL,
                cron_expr TEXT NOT NULL DEFAULT '* 6-22 * * 1-5',
                schedule_kind TEXT NOT NULL DEFAULT 'service',
                enabled INTEGER NOT NULL DEFAULT 1,
                label TEXT,
                notes TEXT
            )
            """
        )
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS transit_building_schedule (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                station_id TEXT NOT NULL,
                building_id TEXT,
                cron_expr TEXT NOT NULL DEFAULT '* 5-23 * * *',
                kind TEXT NOT NULL DEFAULT 'opening',
                enabled INTEGER NOT NULL DEFAULT 1,
                notes TEXT
            )
            """
        )
    conn.commit()


def _row_vehicle(r: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": r[0],
        "vehicleId": r[1],
        "routeId": r[2],
        "cronExpr": r[3],
        "scheduleKind": r[4],
        "enabled": bool(r[5]),
        "label": r[6] or "",
        "notes": r[7] or "",
    }


def _row_building(r: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": r[0],
        "stationId": r[1],
        "buildingId": r[2] or "",
        "cronExpr": r[3],
        "kind": r[4],
        "enabled": bool(r[5]),
        "notes": r[6] or "",
    }


def register_transit_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/transit")
    @app.route("/transit/<path:subpath>")
    def serve_transit(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_TRANSIT, subpath)
        return send_from_directory(STATIC_TRANSIT, "index.html")

    @app.route("/api/transit/vehicle-schedules", methods=["GET", "POST"])
    def api_vehicle_schedules():
        conn = get_conn()
        ensure_transit_tables(conn)
        if request.method == "GET":
            vehicle_id = (request.args.get("vehicleId") or "").strip()
            q = "SELECT id, vehicle_id, route_id, cron_expr, schedule_kind, enabled, label, notes FROM transit_authority_vehicle_schedule"
            args: list[Any] = []
            if vehicle_id:
                q += " WHERE vehicle_id = ?"
                args.append(vehicle_id)
            q += " ORDER BY vehicle_id, route_id, schedule_kind"
            rows = conn.execute(q, args).fetchall()
            return jsonify({"schedules": [_row_vehicle(r) for r in rows]})

        body = request.get_json(force=True, silent=True) or {}
        vehicle_id = (body.get("vehicleId") or "").strip()
        route_id = (body.get("routeId") or "").strip()
        if not vehicle_id or not route_id:
            return jsonify({"error": "vehicleId and routeId required"}), 400
        cron_expr = (body.get("cronExpr") or "* 6-22 * * 1-5").strip()
        schedule_kind = (body.get("scheduleKind") or "service").strip()
        enabled = 1 if body.get("enabled", True) else 0
        label = body.get("label") or ""
        notes = body.get("notes") or ""
        cur = conn.execute(
            """
            INSERT INTO transit_authority_vehicle_schedule
                (vehicle_id, route_id, cron_expr, schedule_kind, enabled, label, notes)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            (vehicle_id, route_id, cron_expr, schedule_kind, enabled, label, notes),
        )
        conn.commit()
        return jsonify({"id": cur.lastrowid, "ok": True}), 201

    @app.route("/api/transit/vehicle-schedules/<int:sched_id>", methods=["PUT", "DELETE"])
    def api_vehicle_schedule_item(sched_id: int):
        conn = get_conn()
        ensure_transit_tables(conn)
        if request.method == "DELETE":
            conn.execute("DELETE FROM transit_authority_vehicle_schedule WHERE id = ?", (sched_id,))
            conn.commit()
            return jsonify({"ok": True})
        body = request.get_json(force=True, silent=True) or {}
        conn.execute(
            """
            UPDATE transit_authority_vehicle_schedule SET
                vehicle_id = COALESCE(?, vehicle_id),
                route_id = COALESCE(?, route_id),
                cron_expr = COALESCE(?, cron_expr),
                schedule_kind = COALESCE(?, schedule_kind),
                enabled = COALESCE(?, enabled),
                label = COALESCE(?, label),
                notes = COALESCE(?, notes)
            WHERE id = ?
            """,
            (
                body.get("vehicleId"),
                body.get("routeId"),
                body.get("cronExpr"),
                body.get("scheduleKind"),
                None if "enabled" not in body else (1 if body.get("enabled") else 0),
                body.get("label"),
                body.get("notes"),
                sched_id,
            ),
        )
        conn.commit()
        return jsonify({"ok": True})

    @app.route("/api/transit/building-schedules", methods=["GET", "POST"])
    def api_building_schedules():
        conn = get_conn()
        ensure_transit_tables(conn)
        if request.method == "GET":
            station_id = (request.args.get("stationId") or "").strip()
            q = "SELECT id, station_id, building_id, cron_expr, kind, enabled, notes FROM transit_building_schedule"
            args: list[Any] = []
            if station_id:
                q += " WHERE station_id = ?"
                args.append(station_id)
            q += " ORDER BY station_id, kind"
            rows = conn.execute(q, args).fetchall()
            return jsonify({"schedules": [_row_building(r) for r in rows]})

        body = request.get_json(force=True, silent=True) or {}
        station_id = (body.get("stationId") or "").strip()
        if not station_id:
            return jsonify({"error": "stationId required"}), 400
        cron_expr = (body.get("cronExpr") or "* 5-23 * * *").strip()
        kind = (body.get("kind") or "opening").strip()
        if kind not in ("maintenance", "opening", "closing"):
            return jsonify({"error": "kind must be maintenance|opening|closing"}), 400
        enabled = 1 if body.get("enabled", True) else 0
        building_id = body.get("buildingId") or ""
        notes = body.get("notes") or ""
        cur = conn.execute(
            """
            INSERT INTO transit_building_schedule
                (station_id, building_id, cron_expr, kind, enabled, notes)
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            (station_id, building_id, cron_expr, kind, enabled, notes),
        )
        conn.commit()
        return jsonify({"id": cur.lastrowid, "ok": True}), 201

    @app.route("/api/transit/building-schedules/<int:sched_id>", methods=["PUT", "DELETE"])
    def api_building_schedule_item(sched_id: int):
        conn = get_conn()
        ensure_transit_tables(conn)
        if request.method == "DELETE":
            conn.execute("DELETE FROM transit_building_schedule WHERE id = ?", (sched_id,))
            conn.commit()
            return jsonify({"ok": True})
        body = request.get_json(force=True, silent=True) or {}
        conn.execute(
            """
            UPDATE transit_building_schedule SET
                station_id = COALESCE(?, station_id),
                building_id = COALESCE(?, building_id),
                cron_expr = COALESCE(?, cron_expr),
                kind = COALESCE(?, kind),
                enabled = COALESCE(?, enabled),
                notes = COALESCE(?, notes)
            WHERE id = ?
            """,
            (
                body.get("stationId"),
                body.get("buildingId"),
                body.get("cronExpr"),
                body.get("kind"),
                None if "enabled" not in body else (1 if body.get("enabled") else 0),
                body.get("notes"),
                sched_id,
            ),
        )
        conn.commit()
        return jsonify({"ok": True})

    @app.route("/api/transit/routes", methods=["GET"])
    def api_transit_routes_map():
        """Client-side map of vehicle→routes derived from vehicle schedules."""
        conn = get_conn()
        ensure_transit_tables(conn)
        rows = conn.execute(
            """
            SELECT vehicle_id, route_id, cron_expr, schedule_kind, enabled, label
            FROM transit_authority_vehicle_schedule
            ORDER BY vehicle_id, route_id
            """
        ).fetchall()
        routes: dict[str, list[dict[str, Any]]] = {}
        for r in rows:
            vid = r[0]
            routes.setdefault(vid, []).append(
                {
                    "routeId": r[1],
                    "cronExpr": r[2],
                    "scheduleKind": r[3],
                    "enabled": bool(r[4]),
                    "label": r[5] or r[1],
                }
            )
        return jsonify({"vehicleRoutes": routes})
