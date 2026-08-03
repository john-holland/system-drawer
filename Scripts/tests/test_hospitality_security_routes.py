"""Tests for hospitality/security Continuuuum routes (companies + keycards)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from hospitality_security_routes import register_hospitality_security_routes  # noqa: E402
from persona_day_routes import register_persona_day_routes  # noqa: E402


@pytest.fixture
def app_client():
    app = Flask(__name__)
    app.config["TESTING"] = True

    def get_conn():
        raise RuntimeError("not needed")

    register_hospitality_security_routes(app, get_conn)
    register_persona_day_routes(app, get_conn)
    return app.test_client()


def test_keycards_roundtrip(app_client):
    put = app_client.put(
        "/api/civil/keycards",
        json={
            "keycardId": "kc-1",
            "boundNodeId": "room-1",
            "allowedNodeIds": ["room-1"],
            "actorIdsAtNode": ["guest-a"],
            "label": "Room 1",
        },
    )
    assert put.status_code == 200
    listed = app_client.get("/api/civil/keycards").get_json()["keycards"]
    assert any(k["keycardId"] == "kc-1" for k in listed)
    one = app_client.get("/api/civil/keycards?keycardId=kc-1").get_json()["keycards"]
    assert one[0]["boundNodeId"] == "room-1"


def test_companies_crud(app_client):
    put = app_client.put(
        "/api/civil/companies/inn-1",
        json={"displayName": "Copper Inn", "parentCompanyId": "", "staff": []},
    )
    assert put.status_code == 200
    got = app_client.get("/api/civil/companies/inn-1").get_json()["company"]
    assert got["displayName"] == "Copper Inn"
    all_co = app_client.get("/api/civil/companies").get_json()["companies"]
    assert any(c["companyId"] == "inn-1" for c in all_co)


def test_hospitality_meta_and_venues(app_client):
    meta = app_client.get("/api/civil/hospitality-meta").get_json()
    assert "keycards" in meta["discoveryTokens"]
    assert "Hotel" in meta["venues"]
    venues = app_client.get("/api/persona-day/venues").get_json()["kinds"]
    for kind in ("NightClub", "Bar", "Inn", "Hotel", "SpyAgency", "BarberShop"):
        assert kind in venues
    settings = app_client.get("/api/persona-day/settings").get_json()["settings"]
    assert settings.get("musicQuantizeEnabled") is True
    assert settings.get("keycardLateCheckoutTelecomPolicy") == "room_then_cell"
