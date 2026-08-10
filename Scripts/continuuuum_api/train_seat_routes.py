"""Train seat ticket Continuuuum routes — car grid + seat prefab-id config."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC = Path(__file__).resolve().parent / "static" / "train_seats"


def ensure_train_seat_tables(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS train_seat_tickets (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            consist_id TEXT NOT NULL DEFAULT '',
            car_number INTEGER NOT NULL DEFAULT 1,
            seat_total INTEGER NOT NULL DEFAULT 40,
            left_grid_width INTEGER NOT NULL DEFAULT 2,
            right_grid_width INTEGER NOT NULL DEFAULT 2,
            row_gaps_json TEXT NOT NULL DEFAULT '[]',
            entrance_rows_json TEXT NOT NULL DEFAULT '[]',
            seat_type_prefab_id TEXT NOT NULL DEFAULT 'train_seat_default',
            seated_animation_bt_prefab_id TEXT NOT NULL DEFAULT '',
            updated_at TEXT DEFAULT CURRENT_TIMESTAMP
        )
        """
    )
    conn.commit()


def _row(r: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": r[0],
        "consistId": r[1] or "",
        "carNumber": r[2],
        "seatTotal": r[3],
        "leftGridWidth": r[4],
        "rightGridWidth": r[5],
        "rowGaps": json.loads(r[6] or "[]"),
        "entranceRows": json.loads(r[7] or "[]"),
        "seatTypePrefabId": r[8] or "train_seat_default",
        "seatedAnimationBtPrefabId": r[9] or "",
    }


def register_train_seat_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/train-seats")
    @app.route("/train-seats/<path:subpath>")
    def serve_train_seats(subpath=None):
        STATIC.mkdir(parents=True, exist_ok=True)
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC, subpath)
        index = STATIC / "index.html"
        if index.is_file():
            return send_from_directory(STATIC, "index.html")
        return jsonify({"ok": True, "page": "train-seats", "hint": "PUT /api/train-seats"})

    @app.get("/api/train-seats")
    def list_train_seats():
        conn = get_conn()
        ensure_train_seat_tables(conn)
        rows = conn.execute(
            "SELECT id, consist_id, car_number, seat_total, left_grid_width, right_grid_width, "
            "row_gaps_json, entrance_rows_json, seat_type_prefab_id, seated_animation_bt_prefab_id "
            "FROM train_seat_tickets ORDER BY car_number"
        ).fetchall()
        return jsonify({"ok": True, "tickets": [_row(r) for r in rows]})

    @app.put("/api/train-seats")
    def put_train_seat():
        data = request.get_json(force=True, silent=True) or {}
        conn = get_conn()
        ensure_train_seat_tables(conn)
        ticket_id = data.get("id")
        fields = (
            data.get("consistId") or "",
            int(data.get("carNumber") or 1),
            int(data.get("seatTotal") or 40),
            int(data.get("leftGridWidth") or 2),
            int(data.get("rightGridWidth") or 2),
            json.dumps(data.get("rowGaps") or []),
            json.dumps(data.get("entranceRows") or []),
            data.get("seatTypePrefabId") or "train_seat_default",
            data.get("seatedAnimationBtPrefabId") or "",
        )
        if ticket_id:
            conn.execute(
                """
                UPDATE train_seat_tickets SET
                  consist_id=?, car_number=?, seat_total=?, left_grid_width=?, right_grid_width=?,
                  row_gaps_json=?, entrance_rows_json=?, seat_type_prefab_id=?, seated_animation_bt_prefab_id=?,
                  updated_at=CURRENT_TIMESTAMP
                WHERE id=?
                """,
                (*fields, int(ticket_id)),
            )
        else:
            cur = conn.execute(
                """
                INSERT INTO train_seat_tickets (
                  consist_id, car_number, seat_total, left_grid_width, right_grid_width,
                  row_gaps_json, entrance_rows_json, seat_type_prefab_id, seated_animation_bt_prefab_id
                ) VALUES (?,?,?,?,?,?,?,?,?)
                """,
                fields,
            )
            ticket_id = cur.lastrowid
        conn.commit()
        row = conn.execute(
            "SELECT id, consist_id, car_number, seat_total, left_grid_width, right_grid_width, "
            "row_gaps_json, entrance_rows_json, seat_type_prefab_id, seated_animation_bt_prefab_id "
            "FROM train_seat_tickets WHERE id=?",
            (ticket_id,),
        ).fetchone()
        return jsonify({"ok": True, "ticket": _row(row)})
