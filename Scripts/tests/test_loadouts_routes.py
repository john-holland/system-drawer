"""Tests for loadouts routes."""

from __future__ import annotations

import os
import sqlite3
import tempfile

import pytest

from continuuuum_api.loadouts_routes import (
    ensure_loadouts_schema,
    register_loadouts_routes,
)


@pytest.fixture()
def client(tmp_path):
    from flask import Flask

    db_path = tmp_path / "t.db"
    # seed property specs table so ensure_inventory_property_specs can insert
    conn = sqlite3.connect(db_path)
    conn.execute(
        """CREATE TABLE IF NOT EXISTS localization_property_specs (
           key TEXT PRIMARY KEY, value_type TEXT, allowed_values_json TEXT,
           default_value TEXT, description TEXT)"""
    )
    conn.commit()
    conn.close()

    app = Flask(__name__)

    def get_conn():
        c = sqlite3.connect(db_path)
        c.row_factory = sqlite3.Row
        return c

    register_loadouts_routes(app, get_conn)
    app.config["TESTING"] = True
    return app.test_client()


def test_ensure_and_crud(client):
    r = client.post("/api/loadouts/ensure")
    assert r.status_code == 200
    r = client.post(
        "/api/loadouts",
        json={
            "name": "radio",
            "ownedby_actor_id": "tim",
            "loadout_set_id": "raid",
            "onground_x": 1.5,
            "onground_y": 0.25,
            "onground_z": -3,
        },
    )
    assert r.status_code == 201
    item = r.get_json()["item"]
    item_id = item["id"]
    assert item["onground_x"] == 1.5
    assert item["onground_y"] == 0.25
    assert item["onground_z"] == -3
    r = client.get("/api/loadouts?set=raid")
    assert r.status_code == 200
    assert len(r.get_json()["items"]) == 1
    r = client.put(
        f"/api/loadouts/{item_id}",
        json={"onground_x": 9, "onground_y": 8, "onground_z": 7, "name": "radio"},
    )
    assert r.status_code == 200
    assert r.get_json()["item"]["onground_x"] == 9
    r = client.post(f"/api/loadouts/{item_id}/transfer", json={"to": "sara"})
    assert r.status_code == 200
    assert r.get_json()["item"]["heldby_actor_id"] == "sara"
    r = client.get("/api/loadouts/sets")
    assert "raid" in r.get_json()["sets"]
