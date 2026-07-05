"""Tests for causality work order validation."""

from __future__ import annotations

import json
import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))

from causality_work_order_validator import (  # noqa: E402
    audit_leaf_prefixes,
    is_bisecting_snake,
    is_compatible_prefix,
    validate_work_orders,
)
from story_db import ensure_stories_schema  # noqa: E402


def test_compatible_prefix():
    assert is_compatible_prefix("S3.O2", "S3.O2.1.7")
    assert not is_compatible_prefix("S3.O2.1.7", "S3.O2")


def test_bisecting_snake():
    assert is_bisecting_snake("S3.O2.1", "S3.O2.9")
    assert not is_bisecting_snake("S3.O2", "S3.O2.1.7")


def test_audit_leaf_prefixes_detects_violation():
    violations = audit_leaf_prefixes(["S3.O2.1", "S3.O2.9"])
    assert len(violations) >= 1


@pytest.fixture
def conn(tmp_path):
    db = tmp_path / "test.db"
    c = sqlite3.connect(db)
    c.row_factory = sqlite3.Row
    c.executescript(
        """
        CREATE TABLE episodes (id TEXT PRIMARY KEY, tenant_id TEXT, title TEXT, created_at TEXT,
            engine TEXT, t_start REAL, t_end REAL);
        CREATE TABLE causality_structure (
            id TEXT PRIMARY KEY, episode_id TEXT, structure_type TEXT, detection_source TEXT, description TEXT
        );
        CREATE TABLE work_orders (
            id TEXT PRIMARY KEY, episode_id TEXT, causality_leaf_id TEXT, asset_id TEXT,
            narrative_type TEXT, depends_on TEXT, prompt_description TEXT, status TEXT, assigned_to TEXT
        );
        """
    )
    c.execute("INSERT INTO episodes VALUES ('ep1','default','t','now','unity',0,1)")
    c.execute(
        "INSERT INTO causality_structure VALUES ('cs1','ep1','linear','manual','test structure')"
    )
    ensure_stories_schema(c)
    return c


def test_validate_cycle(conn):
    conn.execute(
        """INSERT INTO work_orders
           (id, episode_id, causality_leaf_id, asset_id, narrative_type, depends_on,
            prompt_description, status, assigned_to)
           VALUES ('a','ep1','Q1','Q1','linear','["b"]','', 'pending', NULL)"""
    )
    conn.execute(
        """INSERT INTO work_orders
           (id, episode_id, causality_leaf_id, asset_id, narrative_type, depends_on,
            prompt_description, status, assigned_to)
           VALUES ('b','ep1','Q2','Q2','linear','["a"]','', 'pending', NULL)"""
    )
    conn.commit()
    result = validate_work_orders(conn, work_order_ids=["a", "b"])
    assert not result["ok"]
    assert any(e["code"] == "cycle" for e in result["buildErrors"])


def test_validate_incomplete_blocks_complete(conn):
    conn.execute(
        """INSERT INTO work_orders
           (id, episode_id, causality_leaf_id, asset_id, narrative_type, depends_on,
            prompt_description, status, assigned_to)
           VALUES ('w1','ep1','Q1','Q1','linear','[]','', 'pending', NULL)"""
    )
    conn.commit()
    result = validate_work_orders(conn, work_order_ids=["w1"])
    assert not result["ok"]
    assert any(e["code"] == "incomplete_work_orders" for e in result["buildErrors"])
