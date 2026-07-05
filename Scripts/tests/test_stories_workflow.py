"""Tests for story workflow API."""

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

from story_db import ensure_stories_schema  # noqa: E402
from story_routes import register_story_routes  # noqa: E402


def _init_db(db_path: Path) -> None:
    c = sqlite3.connect(db_path)
    c.executescript(
        """
        CREATE TABLE episodes (id TEXT PRIMARY KEY, tenant_id TEXT NOT NULL DEFAULT 'default',
            title TEXT NOT NULL, created_at TEXT NOT NULL, engine TEXT NOT NULL DEFAULT 'unity',
            scene_path TEXT, t_start REAL NOT NULL, t_end REAL NOT NULL,
            tokenized_script_ref TEXT, plot_description TEXT);
        CREATE TABLE work_orders (
            id TEXT PRIMARY KEY, episode_id TEXT NOT NULL, causality_leaf_id TEXT, asset_id TEXT,
            narrative_type TEXT NOT NULL, depends_on TEXT, prompt_description TEXT,
            status TEXT NOT NULL DEFAULT 'pending', assigned_to TEXT
        );
        """
    )
    ensure_stories_schema(c)
    c.close()


@pytest.fixture
def app_client(tmp_path, monkeypatch):
    db = tmp_path / "continuuuum.db"
    _init_db(db)

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        return conn

    monkeypatch.setattr("story_routes._resaurce_route", lambda route, payload: {"ok": True, "chat_room": {"id": "room_test"}})

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_story_routes(app, get_conn)
    return app.test_client()


def test_create_and_list_story(app_client):
    r = app_client.post("/api/stories", json={"summary": "Test story", "storyValue": 5})
    assert r.status_code == 201
    sid = r.get_json()["id"]
    r2 = app_client.get("/api/stories")
    assert r2.status_code == 200
    ids = [s["id"] for s in r2.get_json()["stories"]]
    assert sid in ids


def test_completed_story_cannot_reopen(app_client):
    r = app_client.post("/api/stories", json={"summary": "Done story"})
    sid = r.get_json()["id"]
    app_client.patch(f"/api/stories/{sid}", json={"status": "completed"})
    r3 = app_client.patch(f"/api/stories/{sid}", json={"status": "in_progress"})
    assert r3.status_code == 409


def test_status_regression_blocked(app_client):
    r = app_client.post("/api/stories", json={"summary": "Regress"})
    sid = r.get_json()["id"]
    app_client.patch(f"/api/stories/{sid}", json={"status": "grooming"})
    r2 = app_client.patch(f"/api/stories/{sid}", json={"status": "new"})
    assert r2.status_code == 409


def test_story_create_syncs_chat_room(app_client):
    r = app_client.post("/api/stories", json={"summary": "Chat story"})
    assert r.status_code == 201
    sid = r.get_json()["id"]
    detail = app_client.get(f"/api/stories/{sid}").get_json()
    assert detail.get("resaurce_chat_room_id") == "room_test"


def test_patch_external_jira_fields(app_client):
    r = app_client.post("/api/stories", json={"summary": "Jira story"})
    sid = r.get_json()["id"]
    app_client.patch(
        f"/api/stories/{sid}",
        json={
            "jiraProjectKey": "CONT",
            "jiraIssueType": "Story",
            "externalProvider": "jira",
            "externalKey": "CONT-42",
        },
    )
    detail = app_client.get(f"/api/stories/{sid}").get_json()
    assert detail["jira_project_key"] == "CONT"
    assert detail["external_key"] == "CONT-42"
