"""Comment delete request queue and reviewee approval."""



from __future__ import annotations



import os

import sqlite3

import sys

import tempfile

from pathlib import Path



import pytest



_scripts = Path(__file__).resolve().parents[2]

_api = _scripts / "continuuuum_api"

if str(_api) not in sys.path:

    sys.path.insert(0, str(_api))

if str(_scripts) not in sys.path:

    sys.path.insert(0, str(_scripts))



from continuuuum_api import server as srv





def _bootstrap_db(path: str) -> None:

    conn = sqlite3.connect(path)

    conn.executescript(

        """

        CREATE TABLE draft_episodes (

            id TEXT PRIMARY KEY, title TEXT, created_by TEXT, committed_at TEXT,

            created_at TEXT, updated_at TEXT

        );

        INSERT INTO draft_episodes VALUES (

            'draft-1', 'Test draft', 'author-1', NULL, '2020-01-01', '2020-01-01'

        );

        CREATE TABLE reviewer (

            id TEXT PRIMARY KEY, draft_episode_id TEXT, reviewer_user_id TEXT,

            reviewee_user_id TEXT, status TEXT, review_cycle INTEGER DEFAULT 0,

            created_at TEXT, updated_at TEXT

        );

        INSERT INTO reviewer VALUES (

            'rev-1', 'draft-1', 'reviewer-1', 'author-1', 'pending', 0, '2020-01-01', '2020-01-01'

        );

        CREATE TABLE reviewer_comments (

            id TEXT PRIMARY KEY, reviewer_id TEXT, script_ref TEXT,

            text_selection_start INTEGER, text_selection_end INTEGER,

            comment_text TEXT, review_cycle INTEGER DEFAULT 0, created_at TEXT,

            delete_requested_at TEXT, delete_requested_by TEXT

        );

        INSERT INTO reviewer_comments VALUES (

            'cmt-1', 'rev-1', NULL, 0, 5, 'Fix intro', 0, '2020-01-01', NULL, NULL

        );

        CREATE TABLE reviewer_comments_archive (

            id TEXT PRIMARY KEY, reviewer_id TEXT, original_comment_id TEXT,

            comment_text TEXT, previously_on TEXT, text_selection_start INTEGER,

            text_selection_end INTEGER, property_key TEXT, review_cycle INTEGER,

            archived_at TEXT, archived_reason TEXT

        );

        CREATE TABLE notifications (

            id TEXT PRIMARY KEY, user_id TEXT, type TEXT, message TEXT,

            draft_id TEXT, review_id TEXT, read_at TEXT, created_at TEXT

        );

        """

    )

    conn.commit()

    conn.close()





@pytest.fixture

def review_client(monkeypatch):

    fd, path = tempfile.mkstemp(suffix=".db")

    os.close(fd)

    _bootstrap_db(path)

    monkeypatch.setenv("CONTINUUUUM_DB", path)

    srv._schema_initialized = True

    yield srv.app.test_client(), path

    try:

        os.unlink(path)

    except OSError:

        pass





def test_reviewee_can_approve_delete_request(review_client):

    client, _ = review_client

    r = client.patch(

        "/api/reviews/rev-1/comments/cmt-1",

        json={"requestDelete": True},

        headers={"X-User-ID": "reviewer-1"},

    )

    assert r.status_code == 200

    r = client.patch(

        "/api/reviews/rev-1/comments/cmt-1",

        json={"approveDelete": True},

        headers={"X-User-ID": "author-1"},

    )

    assert r.status_code == 200

    r = client.get(

        "/api/reviews/rev-1/comments",

        headers={"X-User-ID": "author-1"},

    )

    assert r.status_code == 200

    assert r.get_json()["items"] == []





def test_admin_delete_request_queue(review_client):

    client, _ = review_client

    client.patch(

        "/api/reviews/rev-1/comments/cmt-1",

        json={"requestDelete": True},

        headers={"X-User-ID": "reviewer-1"},

    )

    r = client.get(

        "/api/reviews/comment-delete-requests",

        headers={"X-User-ID": "admin-1", "X-Admin": "1"},

    )

    assert r.status_code == 200

    data = r.get_json()

    assert data["total"] == 1

    assert data["items"][0]["commentId"] == "cmt-1"

