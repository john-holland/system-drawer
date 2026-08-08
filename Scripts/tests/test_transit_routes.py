"""Tests for transit authority schedule API."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from continuuuum_api.transit_routes import ensure_transit_tables, register_transit_routes  # noqa: E402


@pytest.fixture()
def client(tmp_path):
    from flask import Flask

    db = tmp_path / "transit.db"
    conn = sqlite3.connect(db)
    ensure_transit_tables(conn)

    app = Flask(__name__)
    register_transit_routes(app, lambda: conn)
    app.config["TESTING"] = True
    with app.test_client() as c:
        yield c
    conn.close()


def test_vehicle_schedule_crud(client):
    res = client.post(
        "/api/transit/vehicle-schedules",
        json={
            "vehicleId": "bus-1",
            "routeId": "downtown",
            "cronExpr": "* 6-22 * * 1-5",
            "scheduleKind": "service",
            "label": "Downtown",
        },
    )
    assert res.status_code == 201
    sid = res.get_json()["id"]

    res = client.get("/api/transit/vehicle-schedules?vehicleId=bus-1")
    assert res.status_code == 200
    schedules = res.get_json()["schedules"]
    assert len(schedules) == 1
    assert schedules[0]["routeId"] == "downtown"

    res = client.get("/api/transit/routes")
    assert "bus-1" in res.get_json()["vehicleRoutes"]

    res = client.delete(f"/api/transit/vehicle-schedules/{sid}")
    assert res.status_code == 200


def test_building_schedule_kinds(client):
    res = client.post(
        "/api/transit/building-schedules",
        json={
            "stationId": "depot-a",
            "kind": "opening",
            "cronExpr": "* 5-23 * * *",
        },
    )
    assert res.status_code == 201

    res = client.post(
        "/api/transit/building-schedules",
        json={"stationId": "depot-a", "kind": "bogus", "cronExpr": "* * * * *"},
    )
    assert res.status_code == 400

    res = client.get("/api/transit/building-schedules?stationId=depot-a")
    assert len(res.get_json()["schedules"]) == 1
