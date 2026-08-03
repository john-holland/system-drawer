"""Tests for BuildingRagdoll / civil Continuuuum routes."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from building_ragdoll_routes import register_building_ragdoll_routes  # noqa: E402
from persona_day_routes import register_persona_day_routes  # noqa: E402


@pytest.fixture
def app_client():
    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_conn():
        raise RuntimeError("building_ragdoll routes do not need DB for these tests")

    register_building_ragdoll_routes(app, get_conn)
    register_persona_day_routes(app, get_conn)
    return app.test_client()


def test_civil_meta(app_client):
    r = app_client.get("/api/civil/meta")
    assert r.status_code == 200
    data = r.get_json()
    assert data.get("buildingBeast") == "stub_only"
    assert "building-ragdoll" in data.get("discoveryTokens", [])


def test_damaged_objects_roundtrip(app_client):
    r = app_client.post(
        "/api/civil/damaged-objects",
        json={"objectId": "door-1", "buildingId": "bldg-a", "damage01": 0.4},
    )
    assert r.status_code == 200
    listed = app_client.get("/api/civil/damaged-objects?buildingId=bldg-a").get_json()["damagedObjects"]
    assert any(x["objectId"] == "door-1" for x in listed)
    res = app_client.post("/api/civil/damaged-objects/door-1/resolve", json={"buildingId": "bldg-a"})
    assert res.status_code == 200
    open_rows = app_client.get("/api/civil/damaged-objects?buildingId=bldg-a").get_json()["damagedObjects"]
    assert all(x.get("resolved") or x["objectId"] != "door-1" for x in open_rows) or len(open_rows) == 0


def test_building_health_and_store_prebake(app_client):
    put = app_client.put(
        "/api/civil/building-health/bldg-a",
        json={"integrity01": 0.7, "memoryAggregate01": 0.2},
    )
    assert put.status_code == 200
    got = app_client.get("/api/civil/building-health/bldg-a").get_json()["health"]
    assert got["integrity01"] == 0.7
    shelves = app_client.post(
        "/api/civil/store/prebake-shelves",
        json={"storeId": "liquor-1", "storeType": "liquor", "count": 4},
    )
    assert shelves.status_code == 200
    body = shelves.get_json()
    assert len(body["shelves"]) == 4
    assert body["source"] == "fallback_catalog"


def test_persona_day_venues_expanded(app_client):
    r = app_client.get("/api/persona-day/venues")
    assert r.status_code == 200
    kinds = set(r.get_json()["kinds"])
    assert "SoupKitchen" in kinds
    assert "PoliceStation" in kinds
    assert "Gym" in kinds


def test_municipal_water_settings(app_client):
    put = app_client.put(
        "/api/civil/municipal-water",
        json={"supplyPressure01": 0.4, "pressureLemmaScale": 0.5},
    )
    assert put.status_code == 200
    got = app_client.get("/api/civil/municipal-water").get_json()["municipalWater"]
    assert got["supplyPressure01"] == 0.4
    meta = app_client.get("/api/civil/meta").get_json()
    assert "municipal-water" in meta["discoveryTokens"]
    assert "quaint" in meta["housingArchitectureSizes"]
