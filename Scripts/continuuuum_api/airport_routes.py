"""Airport airplane schedule + staff hours Continuuuum routes."""

from __future__ import annotations

import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC_AIRPLANES = Path(__file__).resolve().parent / "static" / "airplanes"
STATIC_STAFF = Path(__file__).resolve().parent / "static" / "staff_hours"
SCHEMA = Path(__file__).resolve().parent.parent / "continuuuum_airport_schema.sql"


def ensure_airport_tables(conn: sqlite3.Connection) -> None:
    sql = SCHEMA.read_text(encoding="utf-8") if SCHEMA.is_file() else ""
    if sql:
        conn.executescript(sql)
    else:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS airport_airplane_schedule (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                airplane_id TEXT NOT NULL,
                flight_id TEXT NOT NULL,
                cron_expr TEXT NOT NULL DEFAULT '* 6-22 * * *',
                schedule_kind TEXT NOT NULL DEFAULT 'service',
                enabled INTEGER NOT NULL DEFAULT 1,
                label TEXT,
                airplane_crew_json TEXT,
                gate_crew_json TEXT,
                ground_crew_json TEXT,
                notes TEXT
            )
            """
        )
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS airport_staff_hours (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                building_id TEXT NOT NULL,
                role TEXT NOT NULL,
                open_cron TEXT NOT NULL DEFAULT '* 5-23 * * *',
                close_cron TEXT NOT NULL DEFAULT '',
                enabled INTEGER NOT NULL DEFAULT 1,
                notes TEXT
            )
            """
        )
    conn.commit()


def _row_airplane(r: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": r[0],
        "airplaneId": r[1],
        "flightId": r[2],
        "cronExpr": r[3],
        "scheduleKind": r[4],
        "enabled": bool(r[5]),
        "label": r[6] or "",
        "airplaneCrewJson": r[7] or "",
        "gateCrewJson": r[8] or "",
        "groundCrewJson": r[9] or "",
        "notes": r[10] or "",
    }


def _row_staff(r: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": r[0],
        "buildingId": r[1],
        "role": r[2],
        "openCron": r[3],
        "closeCron": r[4] or "",
        "enabled": bool(r[5]),
        "notes": r[6] or "",
    }


def register_airport_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/airplanes")
    @app.route("/airplanes/<path:subpath>")
    def serve_airplanes(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_AIRPLANES, subpath)
        return send_from_directory(STATIC_AIRPLANES, "index.html")

    @app.route("/staff-hours")
    @app.route("/staff-hours/<path:subpath>")
    def serve_staff_hours(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_STAFF, subpath)
        return send_from_directory(STATIC_STAFF, "index.html")

    @app.route("/api/airport/airplane-schedules", methods=["GET", "POST"])
    def api_airplane_schedules():
        conn = get_conn()
        ensure_airport_tables(conn)
        if request.method == "GET":
            airplane_id = (request.args.get("airplaneId") or "").strip()
            q = (
                "SELECT id, airplane_id, flight_id, cron_expr, schedule_kind, enabled, "
                "label, airplane_crew_json, gate_crew_json, ground_crew_json, notes "
                "FROM airport_airplane_schedule"
            )
            args: list[Any] = []
            if airplane_id:
                q += " WHERE airplane_id = ?"
                args.append(airplane_id)
            q += " ORDER BY airplane_id, flight_id, schedule_kind"
            rows = conn.execute(q, args).fetchall()
            return jsonify({"schedules": [_row_airplane(r) for r in rows]})

        body = request.get_json(force=True, silent=True) or {}
        airplane_id = (body.get("airplaneId") or "").strip()
        flight_id = (body.get("flightId") or "").strip()
        if not airplane_id or not flight_id:
            return jsonify({"error": "airplaneId and flightId required"}), 400
        cron_expr = (body.get("cronExpr") or "* 6-22 * * *").strip()
        schedule_kind = (body.get("scheduleKind") or "service").strip()
        enabled = 1 if body.get("enabled", True) else 0
        label = body.get("label") or ""
        notes = body.get("notes") or ""
        airplane_crew = body.get("airplaneCrewJson") or body.get("airplane_crew_json") or ""
        gate_crew = body.get("gateCrewJson") or body.get("gate_crew_json") or ""
        ground_crew = body.get("groundCrewJson") or body.get("ground_crew_json") or ""
        cur = conn.execute(
            """
            INSERT INTO airport_airplane_schedule
                (airplane_id, flight_id, cron_expr, schedule_kind, enabled, label,
                 airplane_crew_json, gate_crew_json, ground_crew_json, notes)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (
                airplane_id,
                flight_id,
                cron_expr,
                schedule_kind,
                enabled,
                label,
                airplane_crew,
                gate_crew,
                ground_crew,
                notes,
            ),
        )
        conn.commit()
        return jsonify({"id": cur.lastrowid, "ok": True}), 201

    @app.route("/api/airport/airplane-schedules/<int:sched_id>", methods=["PUT", "DELETE"])
    def api_airplane_schedule_item(sched_id: int):
        conn = get_conn()
        ensure_airport_tables(conn)
        if request.method == "DELETE":
            conn.execute("DELETE FROM airport_airplane_schedule WHERE id = ?", (sched_id,))
            conn.commit()
            return jsonify({"ok": True})
        body = request.get_json(force=True, silent=True) or {}
        conn.execute(
            """
            UPDATE airport_airplane_schedule SET
                airplane_id = COALESCE(?, airplane_id),
                flight_id = COALESCE(?, flight_id),
                cron_expr = COALESCE(?, cron_expr),
                schedule_kind = COALESCE(?, schedule_kind),
                enabled = COALESCE(?, enabled),
                label = COALESCE(?, label),
                airplane_crew_json = COALESCE(?, airplane_crew_json),
                gate_crew_json = COALESCE(?, gate_crew_json),
                ground_crew_json = COALESCE(?, ground_crew_json),
                notes = COALESCE(?, notes)
            WHERE id = ?
            """,
            (
                body.get("airplaneId"),
                body.get("flightId"),
                body.get("cronExpr"),
                body.get("scheduleKind"),
                None if "enabled" not in body else (1 if body.get("enabled") else 0),
                body.get("label"),
                body.get("airplaneCrewJson"),
                body.get("gateCrewJson"),
                body.get("groundCrewJson"),
                body.get("notes"),
                sched_id,
            ),
        )
        conn.commit()
        return jsonify({"ok": True})

    @app.route("/api/airport/staff-hours", methods=["GET", "POST"])
    def api_staff_hours():
        conn = get_conn()
        ensure_airport_tables(conn)
        if request.method == "GET":
            building_id = (request.args.get("buildingId") or "").strip()
            q = (
                "SELECT id, building_id, role, open_cron, close_cron, enabled, notes "
                "FROM airport_staff_hours"
            )
            args: list[Any] = []
            if building_id:
                q += " WHERE building_id = ?"
                args.append(building_id)
            q += " ORDER BY building_id, role"
            rows = conn.execute(q, args).fetchall()
            return jsonify({"schedules": [_row_staff(r) for r in rows]})

        body = request.get_json(force=True, silent=True) or {}
        building_id = (body.get("buildingId") or "").strip()
        role = (body.get("role") or "").strip()
        if not building_id or not role:
            return jsonify({"error": "buildingId and role required"}), 400
        open_cron = (body.get("openCron") or "* 5-23 * * *").strip()
        close_cron = (body.get("closeCron") or "").strip()
        enabled = 1 if body.get("enabled", True) else 0
        notes = body.get("notes") or ""
        cur = conn.execute(
            """
            INSERT INTO airport_staff_hours
                (building_id, role, open_cron, close_cron, enabled, notes)
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            (building_id, role, open_cron, close_cron, enabled, notes),
        )
        conn.commit()
        return jsonify({"id": cur.lastrowid, "ok": True}), 201

    @app.route("/api/airport/staff-hours/<int:sched_id>", methods=["PUT", "DELETE"])
    def api_staff_hours_item(sched_id: int):
        conn = get_conn()
        ensure_airport_tables(conn)
        if request.method == "DELETE":
            conn.execute("DELETE FROM airport_staff_hours WHERE id = ?", (sched_id,))
            conn.commit()
            return jsonify({"ok": True})
        body = request.get_json(force=True, silent=True) or {}
        conn.execute(
            """
            UPDATE airport_staff_hours SET
                building_id = COALESCE(?, building_id),
                role = COALESCE(?, role),
                open_cron = COALESCE(?, open_cron),
                close_cron = COALESCE(?, close_cron),
                enabled = COALESCE(?, enabled),
                notes = COALESCE(?, notes)
            WHERE id = ?
            """,
            (
                body.get("buildingId"),
                body.get("role"),
                body.get("openCron"),
                body.get("closeCron"),
                None if "enabled" not in body else (1 if body.get("enabled") else 0),
                body.get("notes"),
                sched_id,
            ),
        )
        conn.commit()
        return jsonify({"ok": True})
