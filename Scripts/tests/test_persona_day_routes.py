"""Tests for persona-day request / settings API."""

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

from persona_day_routes import (  # noqa: E402
    DEFAULT_CIVIL_LOD_SETTINGS,
    build_persona_bundle,
    register_persona_day_routes,
)


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "pd.db"

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_persona_day_routes(app, get_conn)
    return app.test_client(), get_conn


def test_request_bundle(app_client):
    client, _ = app_client
    r = client.get("/api/persona-day/request?personaKey=line-chef&civilKind=Kitchen&cityId=demo")
    assert r.status_code == 200
    bundle = r.get_json()["bundle"]
    assert bundle["personaKey"] == "line-chef"
    assert bundle["civilKind"] == "Kitchen"
    assert 0.0 <= bundle["biorhythmAmplitudeSeed"] <= 1.0
    assert "societyFeatures" in bundle
    assert "needSatisfied01" in bundle


def test_venues_catalog(app_client):
    client, _ = app_client
    r = client.get("/api/persona-day/venues")
    assert r.status_code == 200
    kinds = r.get_json()["kinds"]
    assert "Kitchen" in kinds
    assert "School" in kinds


def test_settings_roundtrip(app_client):
    client, _ = app_client
    put = client.put(
        "/api/persona-day/settings",
        json={"settings": {"maxFullSimVenues": 7, "developerMaxSpeedMps": 20}},
    )
    assert put.status_code == 200
    assert put.get_json()["settings"]["maxFullSimVenues"] == 7
    got = client.get("/api/persona-day/settings")
    assert got.get_json()["settings"]["developerMaxSpeedMps"] == 20
    assert "kindPriorityOrder" in got.get_json()["settings"]


def test_meta_defaults(app_client):
    client, _ = app_client
    r = client.get("/api/persona-day/meta")
    assert r.status_code == 200
    assert r.get_json()["featureBudgetId"] == "civil_systems"
    assert DEFAULT_CIVIL_LOD_SETTINGS["lodFloor"] == 0.15


def test_build_persona_bundle_offline(app_client):
    _, get_conn = app_client
    conn = get_conn()
    try:
        b = build_persona_bundle(
            conn,
            city_id="c1",
            persona_key="p1",
            actor_type="teacher",
            civil_kind="School",
        )
        assert b["actorType"] == "teacher"
        assert b["civilKind"] == "School"
    finally:
        conn.close()
