"""Review list includes drafts with submitted change lists."""

from __future__ import annotations

import sqlite3
import sys
import uuid
from pathlib import Path

import pytest

_scripts = Path(__file__).resolve().parents[2]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from continuum_api.localization_helpers import (
    ensure_reviewer_rows_for_submission,
    list_submitted_change_lists_for_user,
    upsert_draft_script_text,
)


@pytest.fixture
def review_db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE draft_episodes (id TEXT PRIMARY KEY, title TEXT, created_by TEXT, committed_at TEXT);
        INSERT INTO draft_episodes VALUES ('draft-1', 'Episode draft', 'author-1', NULL);
        CREATE TABLE comment_topics (id TEXT PRIMARY KEY, title TEXT, created_at TEXT, updated_at TEXT);
        INSERT INTO comment_topics VALUES ('topic-1', 't', 'now', 'now');
        CREATE TABLE localization_change_lists (
            id TEXT PRIMARY KEY, episode_script_id TEXT, draft_episode_id TEXT,
            comment_topic_id TEXT, workflow_status TEXT, revision INTEGER,
            review_cycle INTEGER, last_saved_at TEXT, created_at TEXT, updated_at TEXT, submitted_at TEXT
        );
        INSERT INTO localization_change_lists VALUES (
            'cl-1', NULL, 'draft-1', 'topic-1', 'in_review', 1, 0, NULL, 'now', 'now', 'now'
        );
        CREATE TABLE reviewer (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, reviewer_user_id TEXT,
            reviewee_user_id TEXT, status TEXT, created_at TEXT, updated_at TEXT
        );
        CREATE TABLE localization_change_list_reviewers (
            change_list_id TEXT, user_id TEXT, role TEXT, approved_at TEXT, rejected_at TEXT,
            PRIMARY KEY (change_list_id, user_id)
        );
        CREATE TABLE draft_episode_script (
            id TEXT PRIMARY KEY, draft_episode_id TEXT, episode_script_id TEXT,
            script_text TEXT, language TEXT, created_at TEXT, updated_at TEXT
        );
        """
    )
    yield conn
    conn.close()


def test_list_submitted_change_lists_for_author(review_db):
    hits = list_submitted_change_lists_for_user(review_db, "author-1")
    assert len(hits) == 1
    assert hits[0]["draft_episode_id"] == "draft-1"
    assert hits[0]["workflow_status"] == "in_review"


def test_list_submitted_change_lists_for_reviewer(review_db):
    review_db.execute(
        """INSERT INTO reviewer VALUES ('rev-1', 'draft-1', 'rev-user', 'author-1', 'pending', 'now', 'now')"""
    )
    hits = list_submitted_change_lists_for_user(review_db, "rev-user")
    assert len(hits) == 1


def test_ensure_reviewer_rows_from_change_list_reviewer(review_db):
    review_db.execute(
        "INSERT INTO localization_change_list_reviewers VALUES ('cl-1', 'rev-2', 'reviewer', NULL, NULL)"
    )
    ensure_reviewer_rows_for_submission(review_db, "draft-1", "cl-1")
    row = review_db.execute(
        "SELECT reviewer_user_id FROM reviewer WHERE draft_episode_id = 'draft-1'"
    ).fetchone()
    assert row["reviewer_user_id"] == "rev-2"


def test_upsert_draft_script_text(review_db):
    upsert_draft_script_text(review_db, "draft-1", "hello script")
    row = review_db.execute(
        "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = 'draft-1'"
    ).fetchone()
    assert row["script_text"] == "hello script"
    upsert_draft_script_text(review_db, "draft-1", "updated")
    row = review_db.execute(
        "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = 'draft-1'"
    ).fetchone()
    assert row["script_text"] == "updated"
