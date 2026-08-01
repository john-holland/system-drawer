"""Tests for restaurant menu/order/retinue API."""

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

from restaurant_db import (  # noqa: E402
    ORDER_STATUSES,
    build_retinue_treemap,
    ensure_restaurant_tables,
)
from restaurant_routes import register_restaurant_routes, CHEF_DISCOVERY_TOKENS  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "rest.db"

    def get_conn():
        c = sqlite3.connect(db)
        c.row_factory = sqlite3.Row
        return c

    conn = get_conn()
    ensure_restaurant_tables(conn)
    conn.close()

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_restaurant_routes(app, get_conn)
    return app.test_client(), get_conn


def test_list_and_menu(app_client):
    client, _ = app_client
    r = client.get("/api/restaurant/list")
    assert r.status_code == 200
    restaurants = r.get_json()["restaurants"]
    assert len(restaurants) >= 1
    rid = restaurants[0]["id"]
    m = client.get(f"/api/restaurant/{rid}/menu")
    assert m.status_code == 200
    assert len(m.get_json()["menu"]) >= 1


def test_order_lifecycle(app_client):
    client, _ = app_client
    rid = client.get("/api/restaurant/list").get_json()["restaurants"][0]["id"]
    created = client.post(
        f"/api/restaurant/{rid}/orders",
        json={"ticketLabel": "t1", "lines": [{"name": "Burger", "qty": 1}]},
    )
    assert created.status_code == 200
    body = created.get_json()
    assert body["order"]["status"] == "queued"
    assert body["event"]["type"] == "order.created"
    oid = body["order"]["id"]
    patched = client.patch(
        f"/api/restaurant/{rid}/orders/{oid}/status",
        json={"status": "prep"},
    )
    assert patched.status_code == 200
    assert patched.get_json()["order"]["status"] == "prep"
    bad = client.patch(
        f"/api/restaurant/{rid}/orders/{oid}/status",
        json={"status": "paid"},
    )
    assert bad.status_code == 400
    assert "queued" in ORDER_STATUSES


def test_retinue_and_chef_graph(app_client):
    client, _ = app_client
    rid = client.get("/api/restaurant/list").get_json()["restaurants"][0]["id"]
    ret = client.get(f"/api/restaurant/{rid}/retinue")
    assert ret.status_code == 200
    assert len(ret.get_json()["retinue"]) >= 1
    g = client.get(f"/api/restaurant/{rid}/chef-card-graph")
    assert g.status_code == 200
    graph = g.get_json()
    assert "nodes" in graph and "links" in graph
    assert "chef" in CHEF_DISCOVERY_TOKENS


def test_build_retinue_treemap_sous_and_line_siblings():
    members = [
        {
            "persona_key": "sous-a",
            "role": "sous-chef",
            "pecking_order": 15,
            "pay_rate": 24,
            "waypoint_group": "kitchen-line",
        },
        {
            "persona_key": "line-1",
            "role": "line-chef",
            "pecking_order": 40,
            "pay_rate": 18,
            "waypoint_group": "kitchen-line",
        },
        {
            "persona_key": "line-2",
            "role": "line-chef",
            "pecking_order": 45,
            "pay_rate": 17,
            "waypoint_group": "kitchen-line",
        },
    ]
    tree = build_retinue_treemap(members)
    assert tree["kind"] == "root"
    managers = [c for c in tree["children"] if c.get("kind") == "manager"]
    assert len(managers) == 1
    sous = managers[0]
    assert sous["name"] == "sous-a"
    staff_names = {c["name"] for c in sous["children"] if c.get("kind") == "staff"}
    assert staff_names == {"line-1", "line-2"}
    assert any(c.get("kind") == "manager-self" for c in sous["children"])
    assert "parent_id" not in sous


def test_retinue_treemap_endpoint(app_client):
    client, _ = app_client
    rid = client.get("/api/restaurant/list").get_json()["restaurants"][0]["id"]
    r = client.get(f"/api/restaurant/{rid}/retinue/treemap")
    assert r.status_code == 200
    tm = r.get_json()["treemap"]
    assert tm["name"] == "retinue"
    assert tm["kind"] == "root"
    assert isinstance(tm.get("children"), list)
    assert len(tm["children"]) >= 1
    kinds = {c.get("kind") for c in tm["children"]}
    assert "manager" in kinds


def test_commodities_put(app_client):
    client, _ = app_client
    rid = client.get("/api/restaurant/list").get_json()["restaurants"][0]["id"]
    put = client.put(
        f"/api/restaurant/{rid}/commodities",
        json={"schedules": [{"commodityKey": "water", "cronExpr": "0 * * * *", "quantity": 5}]},
    )
    assert put.status_code == 200
    assert put.get_json()["schedules"][0]["commodity_key"] == "water"


def test_meta(app_client):
    client, _ = app_client
    r = client.get("/api/restaurant/meta")
    assert r.status_code == 200
    assert "orderStatuses" in r.get_json()
