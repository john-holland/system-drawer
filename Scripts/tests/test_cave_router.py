"""Tests for YAML-driven Cave router (POST /cave/route)."""

from __future__ import annotations

import os
import sqlite3
import sys
import tempfile
from pathlib import Path
from unittest.mock import patch

_scripts = Path(__file__).resolve().parents[1]
_api = _scripts / "continuuuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from continuuuum_api import server as srv
from cave.manifest_loader import load_cave_manifest, message_to_structural
from cave.paths import parse_route


def test_parse_route():
    svc, structural = parse_route("continuuuum:stories/list")
    assert svc == "continuuuum"
    assert structural == "stories/list"
    svc2, structural2 = parse_route("resaurce:production/budget/list")
    assert svc2 == "resaurce"
    assert structural2 == "production/budget/list"


def test_manifest_loads_handlers():
    manifest = load_cave_manifest()
    assert manifest.get("service") == "continuuuum"
    handlers = manifest.get("handlers") or {}
    assert "stories/list" in handlers
    assert "sql-viewer/schema" in handlers
    assert "dialogue/session/open" in handlers
    assert message_to_structural(manifest, "list_stories") == "stories/list"
    assert message_to_structural(manifest, "dialogue_session_open") == "dialogue/session/open"


def test_cave_route_unknown():
    client = srv.app.test_client()
    r = client.post("/cave/route", json={"route": "continuuuum:does/not/exist", "trace_id": "t1"})
    assert r.status_code == 400
    assert r.get_json().get("error") == "unknown_route"


def _connect(path: str) -> sqlite3.Connection:
    c = sqlite3.connect(path)
    c.row_factory = sqlite3.Row
    return c


def test_cave_route_list_stories_empty_db():
    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    conn = sqlite3.connect(path)
    conn.executescript(
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
    from story_db import ensure_stories_schema

    ensure_stories_schema(conn)
    conn.close()

    with patch.object(srv, "get_conn", lambda: _connect(path)):
        srv._schema_initialized = True
        client = srv.app.test_client()
        r = client.post(
            "/cave/route",
            json={
                "schema_version": "2.0",
                "route": "continuuuum:stories/list",
                "payload": {},
                "trace_id": "trace_test_1",
            },
        )
        assert r.status_code == 200
        body = r.get_json()
        assert "stories" in body
    try:
        os.unlink(path)
    except OSError:
        pass


def test_cave_route_resaurce_proxy():
    client = srv.app.test_client()

    def mock_proxy(service, structural, payload, trace_id, **kwargs):
        assert service == "resaurce"
        assert structural == "production/budget/list"
        return {"ok": True, "budget_plans": [{"id": "bp_test"}]}

    with patch("cave.resaurce_proxy.proxy_cave_route", side_effect=mock_proxy):
        r = client.post(
            "/cave/route",
            json={
                "route": "continuuuum:production/budget/list",
                "payload": {},
                "trace_id": "trace_proxy_1",
            },
        )
    assert r.status_code == 200
    assert r.get_json().get("budget_plans")


def test_tome_message_via_cave_dispatch():
    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    conn = sqlite3.connect(path)
    conn.executescript(
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
    from story_db import ensure_stories_schema

    ensure_stories_schema(conn)
    conn.close()

    with patch.object(srv, "get_conn", lambda: _connect(path)):
        srv._schema_initialized = True
        client = srv.app.test_client()
        r = client.post(
            "/api/tomes/story-board-tome/machines/storyMachine/message",
            json={"event": "LIST", "data": {}},
        )
        assert r.status_code == 200
        result = r.get_json().get("result") or {}
        assert "stories" in result
    try:
        os.unlink(path)
    except OSError:
        pass


def test_config_overview_includes_manifest():
    client = srv.app.test_client()
    r = client.get("/api/config/overview")
    assert r.status_code == 200
    data = r.get_json()
    assert "manifest" in data
    assert "list_stories" in (data["manifest"].get("messages") or {})
