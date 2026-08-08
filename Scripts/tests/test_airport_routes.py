"""Tests for airport airplane schedule + staff hours API."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from continuuuum_api.airport_routes import ensure_airport_tables, register_airport_routes  # noqa: E402


@pytest.fixture()
def client(tmp_path):
    from flask import Flask

    db = tmp_path / "airport.db"
    conn = sqlite3.connect(db)
    ensure_airport_tables(conn)

    app = Flask(__name__)
    register_airport_routes(app, lambda: conn)
    app.config["TESTING"] = True
    with app.test_client() as c:
        yield c
    conn.close()


def test_airplane_schedule_crud(client):
    res = client.post(
        "/api/airport/airplane-schedules",
        json={
            "airplaneId": "n737-1",
            "flightId": "CUU-100",
            "cronExpr": "* 6-22 * * *",
            "scheduleKind": "service",
            "label": "Hub morning",
            "airplaneCrewJson": '{"pilot":"p1"}',
            "gateCrewJson": '{"desk":"g1"}',
            "groundCrewJson": '{"baggage":"b1"}',
        },
    )
    assert res.status_code == 201
    sid = res.get_json()["id"]

    res = client.get("/api/airport/airplane-schedules?airplaneId=n737-1")
    assert res.status_code == 200
    schedules = res.get_json()["schedules"]
    assert len(schedules) == 1
    assert schedules[0]["flightId"] == "CUU-100"
    assert "pilot" in schedules[0]["airplaneCrewJson"]

    res = client.put(
        f"/api/airport/airplane-schedules/{sid}",
        json={"label": "Updated"},
    )
    assert res.status_code == 200

    res = client.delete(f"/api/airport/airplane-schedules/{sid}")
    assert res.status_code == 200

    res = client.get("/api/airport/airplane-schedules?airplaneId=n737-1")
    assert res.get_json()["schedules"] == []


def test_staff_hours_crud(client):
    res = client.post(
        "/api/airport/staff-hours",
        json={
            "buildingId": "airport-1",
            "role": "tsa_agent",
            "openCron": "* 5-23 * * *",
            "closeCron": "",
        },
    )
    assert res.status_code == 201
    sid = res.get_json()["id"]

    res = client.post(
        "/api/airport/staff-hours",
        json={"buildingId": "airport-1", "role": ""},
    )
    assert res.status_code == 400

    res = client.get("/api/airport/staff-hours?buildingId=airport-1")
    assert len(res.get_json()["schedules"]) == 1
    assert res.get_json()["schedules"][0]["role"] == "tsa_agent"

    res = client.delete(f"/api/airport/staff-hours/{sid}")
    assert res.status_code == 200
