"""Tests for credits lists API."""

from __future__ import annotations

import json
import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from credits_db import ensure_credits_schema  # noqa: E402
from credits_routes import register_credits_routes  # noqa: E402


def _init_db(db_path: Path) -> None:
    c = sqlite3.connect(db_path)
    c.executescript(
        """
        CREATE TABLE work_orders (
            id TEXT PRIMARY KEY, episode_id TEXT NOT NULL, causality_leaf_id TEXT, asset_id TEXT,
            narrative_type TEXT NOT NULL DEFAULT 'linear', depends_on TEXT, prompt_description TEXT,
            status TEXT NOT NULL DEFAULT 'pending', assigned_to TEXT
        );
        CREATE TABLE stories (
            id TEXT PRIMARY KEY, tenant_id TEXT NOT NULL DEFAULT 'default',
            episode_id TEXT, summary TEXT
        );
        CREATE TABLE story_assignees (
            story_id TEXT NOT NULL, user_id TEXT NOT NULL, role TEXT, created_at TEXT
        );
        """
    )
    ensure_credits_schema(c)
    c.execute(
        "INSERT INTO work_orders (id, episode_id, assigned_to) VALUES (?,?,?)",
        ("wo1", "ep1", "alice"),
    )
    c.execute(
        "INSERT INTO work_orders (id, episode_id, assigned_to) VALUES (?,?,?)",
        ("wo2", "ep1", "hr_001"),
    )
    c.execute(
        "INSERT INTO work_orders (id, episode_id, assigned_to) VALUES (?,?,?)",
        ("wo3", "ep2", "bob"),
    )
    c.commit()
    c.close()


@pytest.fixture
def app_client(tmp_path, monkeypatch):
    db = tmp_path / "continuuuum.db"
    _init_db(db)

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        return conn

    monkeypatch.setattr(
        "credits_routes._fetch_hr_employees",
        lambda: [
            {"id": "hr_001", "name": "Sarah Johnson", "company": "Resaurce"},
            {"id": "hr_002", "name": "Michael Chen"},
        ],
    )

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_credits_routes(app, get_conn)
    return app.test_client()


def test_create_list_and_update_work_orders(app_client):
    r = app_client.post("/api/credits/lists", json={"title": "Game Credits", "episodeId": "ep1"})
    assert r.status_code == 201
    lid = r.get_json()["id"]

    r2 = app_client.post(
        f"/api/credits/lists/{lid}/update-list",
        json={"mode": "work_orders", "episodeId": "ep1"},
    )
    assert r2.status_code == 200
    data = r2.get_json()
    assert data["updateSummary"]["added"] >= 2
    names = {e["fullName"] for e in data["entries"]}
    assert "alice" in names or "Sarah Johnson" in names
    assert "Sarah Johnson" in names  # hr_001 enriched


def test_update_list_hr(app_client):
    r = app_client.post("/api/credits/lists", json={"title": "HR Credits"})
    lid = r.get_json()["id"]
    r2 = app_client.post(f"/api/credits/lists/{lid}/update-list", json={"mode": "hr"})
    assert r2.status_code == 200
    ids = {e["sourceUserId"] for e in r2.get_json()["entries"]}
    assert "hr_001" in ids
    assert "hr_002" in ids


def test_hidden_entry_omitted_without_include_hidden(app_client):
    r = app_client.post("/api/credits/lists", json={"title": "Vis"})
    lid = r.get_json()["id"]
    section_id = r.get_json()["sections"][0]["id"]
    r_e = app_client.post(
        f"/api/credits/lists/{lid}/entries",
        json={"sectionId": section_id, "fullName": "Hidden Person", "showFullName": True},
    )
    eid = r_e.get_json()["id"]
    app_client.patch(
        f"/api/credits/entries/{eid}",
        json={"showFullName": False, "showNickname": False},
    )

    pub = app_client.get(f"/api/credits/lists/{lid}")
    assert all(e["id"] != eid for e in pub.get_json()["entries"])

    edit = app_client.get(f"/api/credits/lists/{lid}?includeHidden=1")
    hidden = [e for e in edit.get_json()["entries"] if e["id"] == eid]
    assert len(hidden) == 1
    assert hidden[0]["visible"] is False


def test_warehouse_history_on_update_and_visibility(app_client):
    r = app_client.post("/api/credits/lists", json={"title": "Hist"})
    lid = r.get_json()["id"]
    app_client.post(f"/api/credits/lists/{lid}/update-list", json={"mode": "hr"})
    section_id = app_client.get(f"/api/credits/lists/{lid}?includeHidden=1").get_json()["sections"][0]["id"]
    eid = app_client.post(
        f"/api/credits/lists/{lid}/entries",
        json={"sectionId": section_id, "fullName": "X", "showFullName": True},
    ).get_json()["id"]
    app_client.patch(
        f"/api/credits/entries/{eid}",
        json={"showFullName": False, "showNickname": False},
    )

    hist = app_client.get(f"/api/credits/lists/{lid}/history").get_json()["events"]
    kinds = {e["eventKind"] for e in hist}
    assert "update_list" in kinds
    assert "visibility_change" in kinds or "create_entry" in kinds
