"""Script suggestions and draft comments API tests."""

from __future__ import annotations

import sqlite3
import sys
import uuid
from pathlib import Path

import pytest

_scripts = Path(__file__).resolve().parents[2]
_api = _scripts / "continuuuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from continuuuum_api.localization_helpers import ensure_script_output_tables, is_draft_author


@pytest.fixture
def so_db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE draft_episodes (
            id TEXT PRIMARY KEY, title TEXT, created_by TEXT, committed_at TEXT,
            created_at TEXT, updated_at TEXT, episode_id TEXT, tenant_id TEXT,
            engine TEXT, scene_path TEXT, t_start REAL, t_end REAL, plot_description TEXT
        );
        INSERT INTO draft_episodes VALUES (
            'draft-1', 'Test', 'author-1', NULL,
            '2020-01-01', '2020-01-02', NULL, 'default',
            'unity', NULL, 0, 3600, NULL
        );
        CREATE TABLE draft_episode_script (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, episode_script_id TEXT,
            script_text TEXT, language TEXT, created_at TEXT, updated_at TEXT
        );
        INSERT INTO draft_episode_script VALUES (
            'script-1', 'draft-1', NULL, 'hello world', 'en', '2020-01-01', '2020-01-01'
        );
        CREATE TABLE reviewer (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, reviewer_user_id TEXT,
            reviewee_user_id TEXT, status TEXT, created_at TEXT, updated_at TEXT
        );
        CREATE TABLE reviewer_comments (
            id TEXT PRIMARY KEY, reviewer_id TEXT NOT NULL, script_ref TEXT,
            text_selection_start INTEGER, text_selection_end INTEGER,
            comment_text TEXT NOT NULL, created_at TEXT NOT NULL, review_cycle INTEGER DEFAULT 0
        );
        CREATE TABLE localization_clause_bindings (
            id TEXT PRIMARY KEY, draft_script_id TEXT, char_start INTEGER, char_end INTEGER,
            farey_left_num INTEGER, farey_left_den INTEGER, farey_right_num INTEGER, farey_right_den INTEGER,
            selection_text TEXT, property_key TEXT, property_value TEXT, binding_kind TEXT,
            updated_at TEXT
        );
        CREATE TABLE localization_change_lists (
            id TEXT PRIMARY KEY, episode_script_id TEXT, draft_episode_id TEXT,
            comment_topic_id TEXT, workflow_status TEXT, revision INTEGER,
            review_cycle INTEGER, last_saved_at TEXT, created_at TEXT, updated_at TEXT, submitted_at TEXT
        );
        CREATE TABLE comment_topics (id TEXT PRIMARY KEY, title TEXT, created_at TEXT, updated_at TEXT);
        """
    )
    ensure_script_output_tables(conn)
    yield conn
    conn.close()


def test_is_draft_author(so_db):
    assert is_draft_author(so_db, "draft-1", "author-1")
    assert not is_draft_author(so_db, "draft-1", "bob")


def test_create_and_list_suggestion(so_db):
    from continuuuum_api.script_output_routes import _suggestion_row

    now = "2020-06-01T00:00:00Z"
    sid = str(uuid.uuid4())
    so_db.execute(
        """INSERT INTO script_suggestions
           (id, draft_episode_id, suggested_by, base_script_text, suggested_script_text,
            status, review_cycle, created_at, updated_at)
           VALUES (?, 'draft-1', 'bob', 'hello world', 'hello brave world', 'pending', 0, ?, ?)""",
        (sid, now, now),
    )
    cur = so_db.execute(
        "SELECT * FROM script_suggestions WHERE draft_episode_id = 'draft-1' AND status = 'pending'"
    )
    rows = cur.fetchall()
    assert len(rows) == 1
    assert _suggestion_row(rows[0])["suggestedBy"] == "bob"


def test_ensure_draft_comment_thread(so_db):
    from continuuuum_api.localization_helpers import ensure_draft_comment_thread

    rid = ensure_draft_comment_thread(so_db, "draft-1")
    assert rid
    rid2 = ensure_draft_comment_thread(so_db, "draft-1")
    assert rid == rid2
