"""Table read session API tests."""

from __future__ import annotations

import os
import sqlite3
import sys
import tempfile
import uuid
from pathlib import Path

import pytest

_scripts = Path(__file__).resolve().parents[2]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from continuum_api import server as srv
from continuum_api.table_read_routes import ensure_table_read_tables


def _bootstrap_db(path: str) -> None:
    conn = sqlite3.connect(path)
    conn.executescript(
        """
        CREATE TABLE draft_episodes (
            id TEXT PRIMARY KEY, title TEXT, created_by TEXT, committed_at TEXT,
            created_at TEXT, updated_at TEXT, episode_id TEXT
        );
        INSERT INTO draft_episodes VALUES (
            'draft-tr', 'Table Read Draft', 'host-1', NULL, '2020-01-01', '2020-01-01', NULL
        );
        CREATE TABLE draft_episode_script (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, script_text TEXT, language TEXT,
            created_at TEXT, updated_at TEXT
        );
        INSERT INTO draft_episode_script VALUES (
            'ds-1', 'draft-tr',
            'ALICE\nHello.\n\nBOB\nHi.', 'en', '2020-01-01', '2020-01-01'
        );
        CREATE TABLE reviewer_comments (
            id TEXT PRIMARY KEY, reviewer_id TEXT, draft_episode_id TEXT,
            comment_text TEXT, comment_type TEXT, created_at TEXT,
            text_selection_start INTEGER, text_selection_end INTEGER, script_ref TEXT,
            review_cycle INTEGER DEFAULT 0
        );
        INSERT INTO reviewer_comments VALUES (
            'c1', 'rev-1', 'draft-tr', 'Nice line.', 'general', '2020-01-01', 0, 5, NULL, 0
        );
        """
    )
    ensure_table_read_tables(conn)
    conn.commit()
    conn.close()


@pytest.fixture
def tr_client(monkeypatch):
    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    _bootstrap_db(path)
    monkeypatch.setenv("CONTINUUM_DB", path)
    srv._schema_initialized = True
    yield srv.app.test_client(), path
    try:
        os.unlink(path)
    except OSError:
        pass


def test_create_join_advance_session(tr_client):
    client, _ = tr_client
    r = client.post(
        "/api/table-read/sessions",
        json={"draftEpisodeId": "draft-tr"},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201
    sid = r.get_json()["id"]

    r = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    snap = r.get_json()
    assert snap["session"]["draftEpisodeId"] == "draft-tr"
    assert len(snap["turns"]) >= 1
    assert snap["currentTurn"]["status"] == "active"

    r = client.post(
        f"/api/table-read/sessions/{sid}/join",
        json={"displayName": "reader-2"},
        headers={"X-User-ID": "reader-2"},
    )
    assert r.status_code == 200

    r = client.post(f"/api/table-read/sessions/{sid}/advance", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
    snap2 = r.get_json()
    assert snap2["currentTurn"]["turnIndex"] >= 1


def test_comment_mode_round_robin(tr_client):
    client, _ = tr_client
    r = client.post(
        "/api/table-read/sessions",
        json={
            "draftEpisodeId": "draft-tr",
            "segmentMode": "comments",
            "commentMode": "round_robin",
        },
        headers={"X-User-ID": "host-1"},
    )
    sid = r.get_json()["id"]
    client.post(
        f"/api/table-read/sessions/{sid}/join",
        json={},
        headers={"X-User-ID": "reader-2"},
    )
    r = client.get(f"/api/table-read/sessions/{sid}", headers={"X-User-ID": "host-1"})
    snap = r.get_json()
    assert snap["session"]["segmentMode"] == "comments"
    assert any(t["segmentType"] == "comment" for t in snap["turns"])


def test_recording_finalize(tr_client):
    client, _ = tr_client
    r = client.post(
        "/api/table-read/sessions",
        json={"draftEpisodeId": "draft-tr"},
        headers={"X-User-ID": "host-1"},
    )
    sid = r.get_json()["id"]
    r = client.post(
        f"/api/table-read/sessions/{sid}/recordings",
        json={"mediaKind": "audio"},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 201
    rid = r.get_json()["id"]
    r = client.post(
        f"/api/table-read/sessions/{sid}/recordings/{rid}/parts",
        json={"libraryDocId": 42, "partIndex": 0},
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 200
    assert r.get_json()["partCount"] == 1
    r = client.post(
        f"/api/table-read/sessions/{sid}/recordings/{rid}/finalize",
        headers={"X-User-ID": "host-1"},
    )
    assert r.status_code == 200
    assert r.get_json()["status"] == "finalized"


def test_end_session_host_only(tr_client):
    client, _ = tr_client
    r = client.post(
        "/api/table-read/sessions",
        json={"draftEpisodeId": "draft-tr"},
        headers={"X-User-ID": "host-1"},
    )
    sid = r.get_json()["id"]
    r = client.post(f"/api/table-read/sessions/{sid}/end", headers={"X-User-ID": "other"})
    assert r.status_code == 403
    r = client.post(f"/api/table-read/sessions/{sid}/end", headers={"X-User-ID": "host-1"})
    assert r.status_code == 200
