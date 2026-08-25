"""Phone pole / street-wire association Continuuuum routes + SQLite write-through."""

from __future__ import annotations

import sqlite3
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC_PW = Path(__file__).resolve().parent / "static" / "phone-wires"
SCHEMA_PATH = Path(__file__).resolve().parents[1] / "continuuuum_phone_wire_schema.sql"

_POLES: dict[str, dict[str, Any]] = {}
_WIRES: dict[str, dict[str, Any]] = {}
_ASSOCS: list[dict[str, Any]] = []
_ASSOC_SEQ = 1


def ensure_phone_wire_schema(conn: sqlite3.Connection | None) -> None:
    if conn is None:
        return
    sql = SCHEMA_PATH.read_text(encoding="utf-8") if SCHEMA_PATH.exists() else ""
    if sql:
        conn.executescript(sql)
        conn.commit()


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _load(conn: sqlite3.Connection | None) -> None:
    if conn is None:
        return
    try:
        ensure_phone_wire_schema(conn)
        for row in conn.execute(
            "SELECT pole_id, display_name, asset_guid, city_id, world_json, updated_by, updated_at FROM phone_poles"
        ):
            _POLES[row[0]] = {
                "pole_id": row[0],
                "display_name": row[1],
                "asset_guid": row[2],
                "city_id": row[3],
                "world_json": row[4],
                "updated_by": row[5],
                "updated_at": row[6],
            }
        for row in conn.execute(
            "SELECT wire_id, from_pole_id, to_pole_id, asset_guid, rope_json, updated_by, updated_at FROM phone_wires"
        ):
            _WIRES[row[0]] = {
                "wire_id": row[0],
                "from_pole_id": row[1],
                "to_pole_id": row[2],
                "asset_guid": row[3],
                "rope_json": row[4],
                "updated_by": row[5],
                "updated_at": row[6],
            }
        for row in conn.execute(
            "SELECT id, pole_id, wire_id, intersection_lot_id, asset_guid, wire_end_kind, t01, updated_by, updated_at FROM phone_wire_associations"
        ):
            _ASSOCS.append(
                {
                    "id": row[0],
                    "pole_id": row[1],
                    "wire_id": row[2],
                    "intersection_lot_id": row[3],
                    "asset_guid": row[4],
                    "wire_end_kind": row[5],
                    "t01": row[6],
                    "updated_by": row[7],
                    "updated_at": row[8],
                }
            )
    except Exception:
        pass


def _upsert_pole(conn: sqlite3.Connection | None, row: dict[str, Any]) -> None:
    _POLES[row["pole_id"]] = row
    if conn is None:
        return
    try:
        ensure_phone_wire_schema(conn)
        conn.execute(
            """
            INSERT INTO phone_poles (pole_id, display_name, asset_guid, city_id, world_json, updated_by, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(pole_id) DO UPDATE SET
                display_name=excluded.display_name,
                asset_guid=excluded.asset_guid,
                city_id=excluded.city_id,
                world_json=excluded.world_json,
                updated_by=excluded.updated_by,
                updated_at=excluded.updated_at
            """,
            (
                row["pole_id"],
                row.get("display_name"),
                row.get("asset_guid"),
                row.get("city_id"),
                row.get("world_json"),
                row.get("updated_by"),
                row.get("updated_at"),
            ),
        )
        conn.commit()
    except Exception:
        pass


def _upsert_wire(conn: sqlite3.Connection | None, row: dict[str, Any]) -> None:
    _WIRES[row["wire_id"]] = row
    if conn is None:
        return
    try:
        ensure_phone_wire_schema(conn)
        conn.execute(
            """
            INSERT INTO phone_wires (wire_id, from_pole_id, to_pole_id, asset_guid, rope_json, updated_by, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(wire_id) DO UPDATE SET
                from_pole_id=excluded.from_pole_id,
                to_pole_id=excluded.to_pole_id,
                asset_guid=excluded.asset_guid,
                rope_json=excluded.rope_json,
                updated_by=excluded.updated_by,
                updated_at=excluded.updated_at
            """,
            (
                row["wire_id"],
                row.get("from_pole_id"),
                row.get("to_pole_id"),
                row.get("asset_guid"),
                row.get("rope_json"),
                row.get("updated_by"),
                row.get("updated_at"),
            ),
        )
        conn.commit()
    except Exception:
        pass


def _upsert_assoc(conn: sqlite3.Connection | None, row: dict[str, Any]) -> dict[str, Any]:
    global _ASSOC_SEQ
    existing = None
    for a in _ASSOCS:
        if (
            a.get("pole_id") == row.get("pole_id")
            and a.get("wire_id") == row.get("wire_id")
            and a.get("intersection_lot_id") == row.get("intersection_lot_id")
            and a.get("wire_end_kind") == row.get("wire_end_kind")
        ):
            existing = a
            break
    if existing is None:
        row["id"] = _ASSOC_SEQ
        _ASSOC_SEQ += 1
        _ASSOCS.append(row)
    else:
        existing.update(row)
        row = existing
    if conn is None:
        return row
    try:
        ensure_phone_wire_schema(conn)
        conn.execute(
            """
            INSERT INTO phone_wire_associations
                (pole_id, wire_id, intersection_lot_id, asset_guid, wire_end_kind, t01, updated_by, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(pole_id, wire_id, intersection_lot_id, wire_end_kind) DO UPDATE SET
                asset_guid=excluded.asset_guid,
                t01=excluded.t01,
                updated_by=excluded.updated_by,
                updated_at=excluded.updated_at
            """,
            (
                row.get("pole_id"),
                row.get("wire_id"),
                row.get("intersection_lot_id"),
                row.get("asset_guid"),
                row.get("wire_end_kind"),
                row.get("t01"),
                row.get("updated_by"),
                row.get("updated_at"),
            ),
        )
        conn.commit()
    except Exception:
        pass
    return row


def register_phone_wire_routes(app: Flask, get_conn: GetConn) -> None:
    try:
        _load(get_conn())
    except Exception:
        pass

    @app.route("/phone-wires")
    @app.route("/phone-wires/<path:subpath>")
    def phone_wires_page(subpath: str = "index.html"):
        folder = STATIC_PW
        if not folder.exists():
            return jsonify({"error": "missing"}), 404
        name = subpath if subpath else "index.html"
        return send_from_directory(folder, name)

    @app.route("/api/civil/phone-poles", methods=["GET", "PUT"])
    def api_phone_poles():
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        if request.method == "GET":
            return jsonify({"poles": list(_POLES.values())})
        body = request.get_json(silent=True) or {}
        pid = body.get("pole_id") or body.get("poleId")
        if not pid:
            return jsonify({"error": "pole_id required"}), 400
        row = {
            "pole_id": pid,
            "display_name": body.get("display_name") or body.get("displayName") or pid,
            "asset_guid": body.get("asset_guid") or body.get("assetGuid"),
            "city_id": body.get("city_id") or body.get("cityId"),
            "world_json": body.get("world_json") or body.get("worldJson"),
            "updated_by": body.get("updated_by") or body.get("updatedBy") or "unity",
            "updated_at": _now(),
        }
        _upsert_pole(conn, row)
        return jsonify(row)

    @app.route("/api/civil/phone-poles/<pole_id>", methods=["GET", "PUT"])
    def api_phone_pole(pole_id: str):
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        if request.method == "GET":
            row = _POLES.get(pole_id)
            if row is None:
                return jsonify({"error": "not found"}), 404
            return jsonify(row)
        body = request.get_json(silent=True) or {}
        body["pole_id"] = pole_id
        body["updated_at"] = _now()
        row = {
            "pole_id": pole_id,
            "display_name": body.get("display_name") or body.get("displayName") or pole_id,
            "asset_guid": body.get("asset_guid") or body.get("assetGuid"),
            "city_id": body.get("city_id") or body.get("cityId"),
            "world_json": body.get("world_json") or body.get("worldJson"),
            "updated_by": body.get("updated_by") or "unity",
            "updated_at": body["updated_at"],
        }
        _upsert_pole(conn, row)
        return jsonify(row)

    @app.route("/api/civil/phone-wires", methods=["GET", "PUT"])
    def api_phone_wires():
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        if request.method == "GET":
            return jsonify({"wires": list(_WIRES.values())})
        body = request.get_json(silent=True) or {}
        wid = body.get("wire_id") or body.get("wireId")
        if not wid:
            return jsonify({"error": "wire_id required"}), 400
        row = {
            "wire_id": wid,
            "from_pole_id": body.get("from_pole_id") or body.get("fromPoleId"),
            "to_pole_id": body.get("to_pole_id") or body.get("toPoleId"),
            "asset_guid": body.get("asset_guid"),
            "rope_json": body.get("rope_json"),
            "updated_by": body.get("updated_by") or "unity",
            "updated_at": _now(),
        }
        _upsert_wire(conn, row)
        return jsonify(row)

    @app.route("/api/civil/phone-wires/<wire_id>", methods=["GET", "PUT"])
    def api_phone_wire(wire_id: str):
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        if request.method == "GET":
            row = _WIRES.get(wire_id)
            if row is None:
                return jsonify({"error": "not found"}), 404
            return jsonify(row)
        body = request.get_json(silent=True) or {}
        row = {
            "wire_id": wire_id,
            "from_pole_id": body.get("from_pole_id") or body.get("fromPoleId"),
            "to_pole_id": body.get("to_pole_id") or body.get("toPoleId"),
            "asset_guid": body.get("asset_guid"),
            "rope_json": body.get("rope_json"),
            "updated_by": body.get("updated_by") or "unity",
            "updated_at": _now(),
        }
        _upsert_wire(conn, row)
        return jsonify(row)

    @app.route("/api/civil/phone-wire-associations", methods=["GET", "PUT"])
    def api_assocs():
        conn = None
        try:
            conn = get_conn()
        except Exception:
            conn = None
        if request.method == "GET":
            pole = request.args.get("poleId") or request.args.get("pole_id")
            wire = request.args.get("wireId") or request.args.get("wire_id")
            lot = request.args.get("intersectionLotId") or request.args.get("intersection_lot_id")
            guid = request.args.get("assetGuid") or request.args.get("asset_guid")
            rows = _ASSOCS
            if pole:
                rows = [r for r in rows if r.get("pole_id") == pole]
            if wire:
                rows = [r for r in rows if r.get("wire_id") == wire]
            if lot:
                rows = [r for r in rows if r.get("intersection_lot_id") == lot]
            if guid:
                rows = [r for r in rows if r.get("asset_guid") == guid]
            return jsonify({"associations": rows})
        body = request.get_json(silent=True) or {}
        row = {
            "pole_id": body.get("pole_id") or body.get("poleId"),
            "wire_id": body.get("wire_id") or body.get("wireId"),
            "intersection_lot_id": body.get("intersection_lot_id") or body.get("intersectionLotId") or "",
            "asset_guid": body.get("asset_guid") or body.get("assetGuid"),
            "wire_end_kind": body.get("wire_end_kind") or body.get("wireEndKind") or "TrafficSignal",
            "t01": float(body.get("t01") if body.get("t01") is not None else 0.5),
            "updated_by": body.get("updated_by") or "unity",
            "updated_at": _now(),
        }
        saved = _upsert_assoc(conn, row)
        return jsonify(saved)

    @app.route("/api/civil/phone-wire-associations/auto", methods=["POST"])
    def api_assocs_auto():
        body = request.get_json(silent=True) or {}
        pole = body.get("pole_id") or body.get("poleId") or request.args.get("poleId")
        to_pole = body.get("to_pole_id") or body.get("toPoleId") or request.args.get("toPoleId")
        lot = body.get("intersection_lot_id") or body.get("intersectionLotId") or request.args.get("intersectionLotId")
        hits = []
        for a in _ASSOCS:
            if lot and a.get("intersection_lot_id") == lot:
                hits.append(a)
                continue
            if pole and to_pole:
                w = _WIRES.get(a.get("wire_id") or "")
                if w and (
                    (w.get("from_pole_id") == pole and w.get("to_pole_id") == to_pole)
                    or (w.get("from_pole_id") == to_pole and w.get("to_pole_id") == pole)
                    or a.get("pole_id") == pole
                ):
                    hits.append(a)
            elif pole and a.get("pole_id") == pole:
                hits.append(a)
        return jsonify({"associations": hits})
