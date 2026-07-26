"""NSM wiring API routes."""

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

from nsm_routes import register_nsm_routes  # noqa: E402
from nsm_wiring_db import ensure_nsm_schema, seed_nsm_prime_wiring  # noqa: E402


@pytest.fixture
def client(tmp_path):
    db = tmp_path / "nsm.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    ensure_nsm_schema(conn)
    # minimal lemma_completion
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS lemma_completion (
            id TEXT PRIMARY KEY,
            language_code TEXT NOT NULL DEFAULT 'en',
            term TEXT NOT NULL,
            rank INTEGER,
            entry_id TEXT,
            is_prime INTEGER NOT NULL DEFAULT 0,
            is_builtin INTEGER NOT NULL DEFAULT 0,
            is_implemented INTEGER NOT NULL DEFAULT 0,
            benefits_from_asset_store INTEGER NOT NULL DEFAULT 0,
            nsm_definition TEXT,
            composition_json TEXT,
            descriptor_json TEXT,
            updated_at TEXT NOT NULL,
            UNIQUE(language_code, term)
        );
        """
    )
    conn.commit()
    conn.close()

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_nsm_routes(app, get_conn)
    return app.test_client(), get_conn


def test_seed_and_primes(client):
    c, get_conn = client
    r = c.post("/api/nsm/seed-wiring", json={"language": "en"})
    assert r.status_code == 200
    body = r.get_json()
    assert body["ok"] is True
    assert body["primesWired"] >= 65

    r = c.get("/api/nsm/primes")
    assert r.status_code == 200
    assert r.get_json()["count"] == 65

    r = c.get("/api/nsm/associations?term=when")
    assert r.status_code == 200
    kinds = {a["relation_kind"] for a in r.get_json()["associations"]}
    assert "dual_exponent" in kinds


def test_evaluate_bool_and_fuzzy(client):
    c, _ = client
    c.post("/api/nsm/seed-wiring", json={})
    r = c.post(
        "/api/nsm/evaluate",
        json={
            "mode": "bool",
            "form": {"op": "not", "args": [{"op": "var", "name": "P"}]},
            "env": {"P": False},
        },
    )
    assert r.status_code == 200
    assert r.get_json()["value"] is True

    r = c.post(
        "/api/nsm/evaluate",
        json={
            "mode": "fuzzy",
            "form": {"op": "hedge", "hedgeId": "somewhat", "args": [{"op": "grade", "value": 0.6}]},
        },
    )
    assert r.status_code == 200
    assert 0.0 <= r.get_json()["value"] <= 1.0


def test_fuzzy_vars_session(client):
    c, _ = client
    c.post("/api/nsm/seed-wiring", json={})
    r = c.put(
        "/api/nsm/fuzzy/vars/sess1",
        json={"vars": [{"var_key": "pred:skittish", "var_kind": "predicate", "grade": 0.8}]},
    )
    assert r.status_code == 200
    r = c.post(
        "/api/nsm/fuzzy/vars/sess1/adjust",
        json={"varKey": "pred:skittish", "hedgeId": "less"},
    )
    assert r.status_code == 200
    assert r.get_json()["var"]["grade"] < 0.8

    r = c.post(
        "/api/nsm/evaluate",
        json={
            "mode": "fuzzy",
            "sessionId": "sess1",
            "form": {"op": "var", "name": "pred:skittish"},
            "upsertVars": [{"var_key": "event:press_button", "var_kind": "event", "grade": 1.0}],
        },
    )
    assert r.status_code == 200


def test_patch_hedge_curve(client):
    c, _ = client
    c.post("/api/nsm/seed-wiring", json={})
    r = c.patch(
        "/api/nsm/fuzzy/hedges/somewhat",
        json={"curve": {"kind": "logistic", "k": 5.0, "x0": 0.5, "yMin": 0.1, "yMax": 0.6, "clamp": True}},
    )
    assert r.status_code == 200
