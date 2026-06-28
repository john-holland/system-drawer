"""Tests for political society sim."""

from __future__ import annotations

import json
import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuum_api"
if str(API) not in sys.path:
    sys.path.insert(0, str(API))
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from gov_glove_adapter import call_gov_glove, to_society_snapshot
from society_db import ensure_society_tables
from city_network_assign import provision_city_network
from city_constraints_solver import solve
from political_solver import tick_city
from society_merge import merge_snapshots


@pytest.fixture
def conn(tmp_path):
    db = tmp_path / "society.db"
    c = sqlite3.connect(db)
    c.row_factory = sqlite3.Row
    ensure_society_tables(c)
    return c


def test_gov_glove_compute_zoning():
    result = call_gov_glove("computeZoning", {"citySizeSqm": 1_000_000, "annualBudgetUsd": 10_000_000})
    assert "allocations" in result
    assert result["cityScapeProfile"]["spatialBounds"]["widthM"] > 0


def test_society_snapshot_mapping():
    glove = call_gov_glove("processLobbyistImpacts", {"lobbyistActivity": 0.5})
    zoning = call_gov_glove("computeZoning", {"citySizeSqm": 500_000, "annualBudgetUsd": 5_000_000})
    snap = to_society_snapshot("city-1", 0, glove, zoning)
    assert snap["cityId"] == "city-1"
    assert "taxRate" in snap or "lobbyistActivity" in snap


def test_create_city_and_network(conn):
    city_id = "alpha-city"
    net = provision_city_network(conn, "earth", city_id, "Alpha")
    assert net["networkId"] == f"society.city.{city_id}"
    assert ":" in net["ipv6CityPrefix"]
    conn.execute(
        """INSERT INTO society_cities
           (city_id, planet_id, display_name, city_grid, network_id, ipv6_city_prefix, created_at, updated_at)
           VALUES (?, 'earth', 'Alpha', ?, ?, ?, datetime('now'), datetime('now'))""",
        (city_id, net["cityGrid"], net["networkId"], net["ipv6CityPrefix"]),
    )
    conn.execute(
        """INSERT INTO city_config (city_id, city_size_sqm, annual_budget_usd, allow_debt, commodity_indices_json, updated_at)
           VALUES (?, 1000000, 10000000, 0, '{}', datetime('now'))""",
        (city_id,),
    )
    conn.commit()
    cur = conn.execute("SELECT network_id FROM city_network_bindings WHERE city_id = ?", (city_id,))
    assert cur.fetchone() is not None


def test_zoning_solve_forward(conn):
    city_id = "beta"
    conn.execute(
        """INSERT INTO society_cities
           (city_id, planet_id, display_name, city_grid, network_id, ipv6_city_prefix, created_at, updated_at)
           VALUES (?, 'earth', 'Beta', 2, 'society.city.beta', '0100::1', datetime('now'), datetime('now'))""",
        (city_id,),
    )
    conn.execute(
        """INSERT INTO city_config (city_id, city_size_sqm, annual_budget_usd, allow_debt, commodity_indices_json, updated_at)
           VALUES (?, 1000000, 20000000, 0, '{"water":1}', datetime('now'))""",
        (city_id,),
    )
    from society_db import default_zone_document

    doc = default_zone_document(city_id)
    result = solve("forward", city_id, 1_000_000, 20_000_000, doc, [], {"water": 1.0}, False)
    assert result.get("allocations")
    assert result["cityScapeProfile"]


def test_political_tick(conn):
    city_id = "gamma"
    conn.execute(
        """INSERT INTO society_cities
           (city_id, planet_id, display_name, city_grid, network_id, ipv6_city_prefix, created_at, updated_at)
           VALUES (?, 'earth', 'Gamma', 3, 'society.city.gamma', '0100::1', datetime('now'), datetime('now'))""",
        (city_id,),
    )
    conn.execute(
        """INSERT INTO city_config (city_id, city_size_sqm, annual_budget_usd, allow_debt, commodity_indices_json, updated_at)
           VALUES (?, 1000000, 10000000, 0, '{}', datetime('now'))""",
        (city_id,),
    )
    conn.commit()
    out = tick_city(conn, city_id)
    assert out["tickIndex"] == 0
    assert out["snapshot"]["cityId"] == city_id


def test_merge_snapshots():
    merged = merge_snapshots({"taxRate": 0.1, "lobbyistActivity": 0.2}, {"taxRate": 0.2, "lobbyistActivity": 0.4})
    assert 0.1 < merged["taxRate"] < 0.2


def test_lobbyists_congress_on_crack_preset():
    result = call_gov_glove("generateScenario", {"presetId": "lobbyists_congress_on_crack"})
    assert result["lobbyistActivity"] > 1.0
