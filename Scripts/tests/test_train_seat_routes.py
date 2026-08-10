"""Train seat ticket API smoke tests."""

from __future__ import annotations

import sqlite3

from continuuuum_api.train_seat_routes import ensure_train_seat_tables, register_train_seat_routes
from flask import Flask


def test_put_and_list_train_seats():
    app = Flask(__name__)
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_train_seat_tables(conn)

    def get_conn():
        return conn

    register_train_seat_routes(app, get_conn)
    client = app.test_client()
    r = client.put(
        "/api/train-seats",
        json={
            "consistId": "c1",
            "carNumber": 2,
            "seatTotal": 48,
            "leftGridWidth": 2,
            "rightGridWidth": 3,
            "rowGaps": [0.8, 1.0],
            "entranceRows": [{"rowIndex": 0, "side": "Door"}],
            "seatTypePrefabId": "seat_a",
        },
    )
    assert r.status_code == 200
    body = r.get_json()
    assert body["ok"] is True
    assert body["ticket"]["carNumber"] == 2
    listed = client.get("/api/train-seats").get_json()
    assert len(listed["tickets"]) == 1
