"""Tests for vehicle inventory + fire/dispatch persona-day catalog."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from vehicle_inventory_routes import register_vehicle_inventory_routes  # noqa: E402
from persona_day_routes import register_persona_day_routes  # noqa: E402
from building_ragdoll_routes import register_building_ragdoll_routes  # noqa: E402
from lemma_auto_bind import STREET_LIGHT_DISCOVERY_TOKENS, _street_light_prompt_candidate  # noqa: E402


@pytest.fixture
def app_client():
    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_conn():
        raise RuntimeError("not needed")

    register_vehicle_inventory_routes(app, get_conn)
    register_persona_day_routes(app, get_conn)
    register_building_ragdoll_routes(app, get_conn)
    return app.test_client()


def test_vehicle_inventory_roundtrip(app_client):
    put = app_client.put(
        "/api/civil/vehicle-inventory",
        json={
            "vehicleId": "engine-1",
            "displayName": "Engine 1",
            "interiors": [
                {"sectionName": "cabin", "capacity": 10, "items": []},
                {"sectionName": "hose_bed", "capacity": 80, "items": []},
            ],
        },
    )
    assert put.status_code == 200
    listed = app_client.get("/api/civil/vehicle-inventory").get_json()["vehicles"]
    assert any(v["vehicleId"] == "engine-1" for v in listed)
    one = app_client.get("/api/civil/vehicle-inventory/engine-1").get_json()["vehicle"]
    assert len(one["interiors"]) == 2


def test_fire_station_in_venues_and_meta(app_client):
    kinds = app_client.get("/api/persona-day/venues").get_json()["kinds"]
    assert "FireStation" in kinds
    settings = app_client.get("/api/persona-day/settings").get_json()["settings"]
    assert "FireStation" in settings["kindPriorityOrder"]
    meta = app_client.get("/api/civil/meta").get_json()
    for tok in ("dispatch", "fire-station", "traffic-light", "vehicle-inventory", "pixel-light"):
        assert tok in meta["discoveryTokens"]
    pd_meta = app_client.get("/api/persona-day/meta").get_json()
    assert "fire-station" in pd_meta["discoveryTokens"]


def test_street_light_lemma_bind():
    assert "changed-to" in STREET_LIGHT_DISCOVERY_TOKENS
    span = {"charStart": 0, "charEnd": 3}
    cand = _street_light_prompt_candidate(span, "red")
    assert cand is not None
    assert "changed-to=red" in cand["propertyValue"]
