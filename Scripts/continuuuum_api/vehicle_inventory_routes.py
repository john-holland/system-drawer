"""VehicleRagdoll inventory Continuuuum routes + SQLite write-through."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC_VI = Path(__file__).resolve().parent / "static" / "vehicle-inventory"

_VEHICLES: dict[str, dict[str, Any]] = {}


def ensure_vehicle_inventory_table(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS vehicle_inventory (
            vehicle_id TEXT PRIMARY KEY,
            display_name TEXT,
            interiors_json TEXT,
            total_size REAL,
            integrity01 REAL
        )
        """
    )
    conn.commit()


def _load_from_db(conn: sqlite3.Connection | None) -> None:
    if conn is None:
        return
    try:
        ensure_vehicle_inventory_table(conn)
        rows = conn.execute(
            "SELECT vehicle_id, display_name, interiors_json, total_size, integrity01 FROM vehicle_inventory"
        ).fetchall()
        for row in rows:
            interiors = json.loads(row[2] or "[]")
            _VEHICLES[row[0]] = {
                "vehicleId": row[0],
                "displayName": row[1] or row[0],
                "interiors": interiors,
                "totalSize": float(row[3] or 0),
                "integrity01": float(row[4] if row[4] is not None else 1),
            }
    except Exception:
        pass


def _persist(conn: sqlite3.Connection | None, row: dict[str, Any]) -> None:
    if conn is None:
        return
    try:
        ensure_vehicle_inventory_table(conn)
        conn.execute(
            """
            INSERT INTO vehicle_inventory (vehicle_id, display_name, interiors_json, total_size, integrity01)
            VALUES (?, ?, ?, ?, ?)
            ON CONFLICT(vehicle_id) DO UPDATE SET
                display_name=excluded.display_name,
                interiors_json=excluded.interiors_json,
                total_size=excluded.total_size,
                integrity01=excluded.integrity01
            """,
            (
                row["vehicleId"],
                row.get("displayName") or row["vehicleId"],
                json.dumps(row.get("interiors") or []),
                float(row.get("totalSize") or 0),
                float(row.get("integrity01") if row.get("integrity01") is not None else 1),
            ),
        )
        conn.commit()
    except Exception:
        pass


def register_vehicle_inventory_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/vehicle-inventory")
    @app.route("/vehicle-inventory/<path:subpath>")
    def serve_vehicle_inventory(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_VI, subpath)
        return send_from_directory(STATIC_VI, "index.html")

    @app.route("/api/civil/vehicle-inventory", methods=["GET", "PUT"])
    def api_vehicle_inventory():
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        try:
            if conn is not None:
                _load_from_db(conn)
            if request.method == "GET":
                vid = request.args.get("vehicleId")
                rows = list(_VEHICLES.values())
                if vid:
                    rows = [r for r in rows if r.get("vehicleId") == vid]
                return jsonify({"vehicles": rows})
            body = request.get_json(silent=True) or {}
            items = body.get("vehicles")
            if isinstance(items, list):
                for item in items:
                    vid = item.get("vehicleId")
                    if not vid:
                        continue
                    row = _normalize(item)
                    _VEHICLES[vid] = row
                    _persist(conn, row)
                return jsonify({"vehicles": list(_VEHICLES.values())})
            vid = body.get("vehicleId")
            if not vid:
                return jsonify({"error": "vehicleId required"}), 400
            row = _normalize(body)
            _VEHICLES[vid] = row
            _persist(conn, row)
            return jsonify({"vehicle": row})
        finally:
            if conn is not None:
                try:
                    conn.close()
                except Exception:
                    pass

    @app.route("/api/civil/vehicle-inventory/<vehicle_id>", methods=["GET", "PUT"])
    def api_vehicle_one(vehicle_id: str):
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        try:
            if conn is not None:
                _load_from_db(conn)
            if request.method == "GET":
                row = _VEHICLES.get(vehicle_id)
                if not row:
                    return jsonify({"error": "not_found"}), 404
                return jsonify({"vehicle": row})
            body = request.get_json(silent=True) or {}
            body["vehicleId"] = vehicle_id
            row = _normalize(body.get("vehicle") or body)
            _VEHICLES[vehicle_id] = row
            _persist(conn, row)
            return jsonify({"vehicle": row})
        finally:
            if conn is not None:
                try:
                    conn.close()
                except Exception:
                    pass


def _normalize(raw: dict[str, Any]) -> dict[str, Any]:
    sections = []
    for s in raw.get("interiors") or []:
        sections.append(
            {
                "sectionName": s.get("sectionName") or "cabin",
                "capacity": float(s.get("capacity") or 10),
                "items": s.get("items") or [],
            }
        )
    if not sections:
        sections = [
            {"sectionName": "cabin", "capacity": 10, "items": []},
            {"sectionName": "cargo", "capacity": 40, "items": []},
        ]
    total = raw.get("totalSize")
    if total is None:
        total = sum(float(s["capacity"]) for s in sections)
    return {
        "vehicleId": raw.get("vehicleId") or "",
        "displayName": raw.get("displayName") or raw.get("vehicleId") or "",
        "integrity01": float(raw.get("integrity01") if raw.get("integrity01") is not None else 1),
        "totalSize": float(total),
        "interiors": sections,
    }
