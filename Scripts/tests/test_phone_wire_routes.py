"""Phone-wire association API tests."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from phone_wire_routes import (  # noqa: E402
    _ASSOCS,
    _POLES,
    _WIRES,
    register_phone_wire_routes,
)


@pytest.fixture
def app_client():
    _ASSOCS.clear()
    _POLES.clear()
    _WIRES.clear()
    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_conn():
        raise RuntimeError("not needed")

    register_phone_wire_routes(app, get_conn)
    return app.test_client()


def test_phone_wire_put_get_auto(app_client):
    pole = app_client.put(
        "/api/civil/phone-poles/p-a",
        json={"display_name": "Pole A", "city_id": "c1"},
    )
    assert pole.status_code == 200
    wire = app_client.put(
        "/api/civil/phone-wires/w-1",
        json={"from_pole_id": "p-a", "to_pole_id": "p-b"},
    )
    assert wire.status_code == 200
    assoc = app_client.put(
        "/api/civil/phone-wire-associations",
        json={
            "pole_id": "p-a",
            "wire_id": "w-1",
            "intersection_lot_id": "lot-1",
            "wire_end_kind": "TrafficSignal",
            "t01": 0.5,
        },
    )
    assert assoc.status_code == 200
    got = app_client.get("/api/civil/phone-wire-associations?intersectionLotId=lot-1")
    assert got.status_code == 200
    rows = got.get_json()["associations"]
    assert len(rows) == 1
    assert rows[0]["wire_id"] == "w-1"
    auto = app_client.post(
        "/api/civil/phone-wire-associations/auto",
        json={"poleId": "p-a", "toPoleId": "p-b", "intersectionLotId": "lot-1"},
    )
    assert auto.status_code == 200
    assert len(auto.get_json()["associations"]) >= 1
