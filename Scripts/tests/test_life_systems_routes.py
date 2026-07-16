"""Tests for life-systems query routes and discovery helpers."""

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

from life_systems_routes import (  # noqa: E402
    LIFE_DISCOVERY_TOKENS,
    ensure_life_property_specs,
    mood_rubric,
    organ_label,
    register_life_systems_routes,
    soft_clamp01,
)
from lemma_auto_bind import LIFE_DISCOVERY_TOKENS as BIND_TOKENS  # noqa: E402
from lemma_auto_bind import _life_systems_prompt_candidate  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "t.db"
    conn = sqlite3.connect(db)
    conn.execute(
        """CREATE TABLE localization_property_specs (
            key TEXT PRIMARY KEY,
            value_type TEXT,
            allowed_values_json TEXT,
            default_value TEXT,
            description TEXT
        )"""
    )
    conn.commit()
    conn.close()

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_life_systems_routes(app, get_conn)
    return app.test_client(), get_conn


def test_mood_rubric_stable():
    a = mood_rubric()
    b = mood_rubric()
    assert a["summary"] == b["summary"]
    assert "mood:" in a["summary"]


def test_soft_clamp_and_organ_great():
    assert 0.0 <= soft_clamp01(-0.5) <= 1.0
    assert 0.0 <= soft_clamp01(1.8) <= 1.0
    assert organ_label(soft_clamp01(1.05)) == "Great"


def test_routes_mood_and_organ(app_client):
    client, _ = app_client
    r = client.get("/api/life-systems/query/mood")
    assert r.status_code == 200
    assert "valence" in r.get_json()

    r2 = client.post("/api/life-systems/query/organ", json={"id": "heart", "raw": 1.05})
    assert r2.status_code == 200
    body = r2.get_json()
    assert body["label"] == "Great"
    assert body["id"] == "heart"


def test_ensure_specs(app_client):
    client, get_conn = app_client
    r = client.post("/api/life-systems/specs/ensure")
    assert r.status_code == 200
    assert r.get_json()["inserted"] >= 1
    conn = get_conn()
    n = conn.execute("SELECT COUNT(*) AS c FROM localization_property_specs").fetchone()["c"]
    conn.close()
    assert n >= 1


def test_prompt_hints_and_auto_bind():
    assert "mood" in LIFE_DISCOVERY_TOKENS
    assert BIND_TOKENS == LIFE_DISCOVERY_TOKENS or "mood" in BIND_TOKENS
    span = {"charStart": 0, "charEnd": 4, "selectionText": "mood"}
    cand = _life_systems_prompt_candidate(span, "mood")
    assert cand is not None
    assert cand["promptPlaceholderName"] == "life"
    assert "query" in cand["propertyValue"]
