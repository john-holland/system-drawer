"""Tests for vehicle inventory SQL size + police/repair discovery tokens."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from vehicle_inventory_routes import (  # noqa: E402
    ensure_vehicle_inventory_table,
    register_vehicle_inventory_routes,
)
from persona_day_routes import register_persona_day_routes  # noqa: E402
from building_ragdoll_routes import register_building_ragdoll_routes  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "vi.db"

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_vehicle_inventory_routes(app, get_conn)
    register_persona_day_routes(app, get_conn)
    register_building_ragdoll_routes(app, get_conn)
    return app.test_client(), get_conn


def test_vehicle_inventory_sql_total_size(app_client):
    client, get_conn = app_client
    put = client.put(
        "/api/civil/vehicle-inventory/cruiser-1",
        json={
            "displayName": "Cruiser 1",
            "totalSize": 55,
            "interiors": [
                {"sectionName": "cabin", "capacity": 15, "items": []},
                {"sectionName": "trunk", "capacity": 40, "items": []},
            ],
        },
    )
    assert put.status_code == 200
    assert put.get_json()["vehicle"]["totalSize"] == 55.0

    conn = get_conn()
    try:
        ensure_vehicle_inventory_table(conn)
        row = conn.execute(
            "SELECT total_size, display_name FROM vehicle_inventory WHERE vehicle_id=?",
            ("cruiser-1",),
        ).fetchone()
        assert row is not None
        assert float(row[0]) == 55.0
        assert row[1] == "Cruiser 1"
    finally:
        conn.close()

    got = client.get("/api/civil/vehicle-inventory/cruiser-1").get_json()["vehicle"]
    assert got["totalSize"] == 55.0


def test_police_repair_discovery_tokens(app_client):
    client, _ = app_client
    kinds = client.get("/api/persona-day/venues").get_json()["kinds"]
    assert "PoliceStation" in kinds
    assert "CarRepair" in kinds
    meta = client.get("/api/civil/meta").get_json()["discoveryTokens"]
    for tok in ("police-station", "vehicle-repair", "cop-cards"):
        assert tok in meta
    pd = client.get("/api/persona-day/meta").get_json()["discoveryTokens"]
    assert "police-station" in pd
