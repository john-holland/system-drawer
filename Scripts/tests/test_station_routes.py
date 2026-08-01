"""Tests for station placard API."""

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

from station_db import ensure_station_tables, STATION_KINDS  # noqa: E402
from station_routes import register_station_routes  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "stn.db"

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    conn = get_conn()
    ensure_station_tables(conn)
    conn.close()

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_station_routes(app, get_conn)
    return app.test_client(), get_conn


def test_list_seeded(app_client):
    client, _ = app_client
    r = client.get("/api/stations?cityId=demo-city")
    assert r.status_code == 200
    stations = r.get_json()["stations"]
    assert len(stations) >= 4
    kinds = {s["kind"] for s in stations}
    assert "cooking" in kinds
    assert "computer" in kinds


def test_assemblage_shape(app_client):
    client, _ = app_client
    r = client.get("/api/stations/assemblage?cityId=demo-city")
    assert r.status_code == 200
    body = r.get_json()
    assert "nodes" in body and "links" in body
    assert any(n.get("nodeType") == "station" for n in body["nodes"])


def test_treemap_hierarchy(app_client):
    client, _ = app_client
    r = client.get("/api/stations/treemap?cityId=demo-city")
    assert r.status_code == 200
    tm = r.get_json()["treemap"]
    assert tm["name"] == "gov"
    assert "children" in tm


def test_level_stats_replace(app_client):
    client, _ = app_client
    put = client.put(
        "/api/stations/level-stats",
        json={
            "cityId": "demo-city",
            "levelId": "lvl-1",
            "stats": {"stationCount": 1, "countsByKind": {"cooking": 1}},
            "stations": [
                {
                    "stableId": "stn-new",
                    "name": "New Grill",
                    "kind": "cooking",
                    "buildingStableId": "bldg-1",
                    "staffingWeight": 2,
                    "commodities": [{"commodityKey": "power", "quantity": 3}],
                    "assignments": [{"assignType": "persona", "refId": "chef-a", "role": "line", "peckingOrder": 40}],
                }
            ],
        },
    )
    assert put.status_code == 200
    assert put.get_json()["ok"] is True
    listed = client.get("/api/stations?cityId=demo-city").get_json()["stations"]
    assert any(s["stable_id"] == "stn-new" for s in listed)
    assert len(listed) == 1  # replace city placards


def test_put_stations_and_meta(app_client):
    client, _ = app_client
    meta = client.get("/api/stations/meta")
    assert meta.status_code == 200
    assert "cooking" in meta.get_json()["kinds"]
    assert set(STATION_KINDS) <= set(meta.get_json()["kinds"])
