"""Unified draft comments column helpers."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest

_scripts = Path(__file__).resolve().parents[2]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))

from continuum_api.script_output_routes import _ensure_comment_columns, _comment_row


@pytest.fixture
def comment_db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE reviewer (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, reviewer_user_id TEXT,
            reviewee_user_id TEXT, status TEXT, created_at TEXT, updated_at TEXT
        );
        INSERT INTO reviewer VALUES ('rev-1', 'draft-1', 'author', 'author', 'pending', 't', 't');
        CREATE TABLE reviewer_comments (
            id TEXT PRIMARY KEY, reviewer_id TEXT NOT NULL, script_ref TEXT,
            text_selection_start INTEGER, text_selection_end INTEGER,
            comment_text TEXT NOT NULL, created_at TEXT NOT NULL, review_cycle INTEGER DEFAULT 0
        );
        CREATE TABLE reviewer_comments_archive (
            id TEXT PRIMARY KEY, reviewer_id TEXT, original_comment_id TEXT,
            comment_text TEXT, previously_on TEXT, text_selection_start INTEGER,
            text_selection_end INTEGER, property_key TEXT, review_cycle INTEGER,
            archived_at TEXT, archived_reason TEXT
        );
        """
    )
    _ensure_comment_columns(conn)
    conn.execute(
        """INSERT INTO reviewer_comments
           (id, reviewer_id, comment_text, created_at, draft_episode_id, source_page, comment_type, author_user_id)
           VALUES ('c1', 'rev-1', 'hello', 't', 'draft-1', 'script_output', 'general', 'bob')"""
    )
    yield conn
    conn.close()


def test_comment_row_includes_source_page(comment_db):
    row = comment_db.execute("SELECT * FROM reviewer_comments WHERE id = 'c1'").fetchone()
    out = _comment_row(row)
    assert out["sourcePage"] == "script_output"
    assert out["commentType"] == "general"
    assert out["authorUserId"] == "bob"
