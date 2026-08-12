"""Garbage bags SQL keyed by (id, dim); seed dim 0."""

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

from garbage_bag_db import (  # noqa: E402
    RANDOM_BAG_ID,
    ensure_garbage_bag_schema,
    get_bag,
    list_bags,
    upsert_bag,
)
from garbage_bag_routes import register_garbage_bag_routes  # noqa: E402
from game_dimension_db import ensure_game_dimension_schema_force  # noqa: E402
from gd_route_annotations import configure_gd_annotations  # noqa: E402


@pytest.fixture
def bag_client(tmp_path):
    db = tmp_path / "bags.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_game_dimension_schema_force(conn)
        ensure_garbage_bag_schema(conn)
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    configure_gd_annotations(get_conn)
    register_garbage_bag_routes(app, get_conn)
    return app.test_client(), get_conn


def test_schema_pk_id_dim(bag_client):
    _, get_conn = bag_client
    conn = get_conn()
    info = conn.execute("PRAGMA table_info(garbage_bags)").fetchall()
    cols = {r["name"] for r in info}
    assert "id" in cols and "dim" in cols
    pk = conn.execute("PRAGMA table_info(garbage_bags)").fetchall()
    pk_cols = [r["name"] for r in pk if r["pk"]]
    assert pk_cols == ["id", "dim"] or set(pk_cols) == {"id", "dim"}


def test_seed_default_at_dim_0(bag_client):
    _, get_conn = bag_client
    conn = get_conn()
    bag = get_bag(conn, RANDOM_BAG_ID, 0)
    assert bag is not None
    assert bag["id"] == RANDOM_BAG_ID
    assert bag["dim"] == 0
    assert bag["isDefault"] is True


def test_list_and_create_filtered_by_dimension(bag_client):
    client, get_conn = bag_client
    hdrs = {"X-User-ID": "admin", "X-Admin": "1", "X-Dimension": "0"}
    r = client.get("/api/garbage-bags", headers=hdrs)
    assert r.status_code == 200
    data = r.get_json()
    assert data["defaultBagId"] == RANDOM_BAG_ID
    assert data["dim"] == 0
    assert any(b["id"] == RANDOM_BAG_ID for b in data["bags"])

    created = client.post(
        "/api/garbage-bags",
        json={"title": "Organic Mix", "commodities": [{"key": "organic", "weight": 1.0}]},
        headers=hdrs,
    )
    assert created.status_code == 201
    body = created.get_json()
    assert body["title"] == "Organic Mix"
    assert body["dim"] == 0
    bid = body["id"]

    # Create overlay at dim 1 — ensures dim-0 existence then landing row
    hdrs1 = {"X-User-ID": "admin", "X-Admin": "1", "X-Dimension": "1"}
    created1 = client.post(
        "/api/garbage-bags",
        json={
            "id": "gbag_dim_test",
            "title": "Dim1 Bag",
            "commodities": [{"key": "metal", "weight": 1.0}],
        },
        headers=hdrs1,
    )
    assert created1.status_code == 201
    assert created1.get_json()["dim"] == 1
    conn = get_conn()
    assert get_bag(conn, "gbag_dim_test", 0) is not None
    assert get_bag(conn, "gbag_dim_test", 1)["title"] == "Dim1 Bag"

    listed1 = client.get("/api/garbage-bags", headers=hdrs1)
    assert listed1.status_code == 200
    titles = {b["id"]: b["title"] for b in listed1.get_json()["bags"]}
    assert titles.get("gbag_dim_test") == "Dim1 Bag"
    # dim-0-only bag still resolves into dim-1 list
    assert bid in titles

    deny = client.delete(f"/api/garbage-bags/{RANDOM_BAG_ID}", headers=hdrs)
    assert deny.status_code == 400


def test_upsert_helper_ensures_dim0():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_garbage_bag_schema(conn)
    bag = upsert_bag(
        conn,
        "x1",
        2,
        title="At Two",
        commodities=[{"key": "mixed", "weight": 1.0}],
    )
    assert bag["dim"] == 2
    assert get_bag(conn, "x1", 0) is not None
    assert len(list_bags(conn, 2)) >= 2  # default + x1
