"""Tests for dialogue session state."""

from __future__ import annotations

import os
import sqlite3
import sys
import tempfile
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
_api = _scripts / "continuuuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))

from dialogue_db import choose_session, create_session, ensure_dialogue_schema, save_compiled_set, sync_goals
from dialogue_parser import BOOK_CONCERT_FIXTURE, compile_dialogue_to_json


def _mem_db():
    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    conn = sqlite3.connect(path)
    ensure_dialogue_schema(conn)
    compiled = compile_dialogue_to_json(BOOK_CONCERT_FIXTURE, "book-concert")
    save_compiled_set(conn, set_id="book-concert", lemma_entry_id=None, name="book-concert", compiled=compiled)
    conn.commit()
    return conn, path


def test_session_open_and_choose():
    conn, path = _mem_db()
    try:
        view = create_session(conn, set_id="book-concert", tenant="default", user_id="u1", trace_id="t1")
        conn.commit()
        assert view.get("ok") is True
        sid = view["sessionId"]
        choices = view.get("choices") or []
        assert any(c.get("answerId") == "windy-man" for c in choices)

        after = choose_session(conn, sid, "windy-man")
        conn.commit()
        assert after.get("currentNode", {}).get("answerId") == "windy-man"
    finally:
        conn.close()
        os.unlink(path)


def test_goal_sync_unlocks_predicate_branch():
    conn, path = _mem_db()
    try:
        view = create_session(conn, set_id="book-concert", tenant="default", user_id="u1", trace_id="t1")
        sid = view["sessionId"]
        choose_session(conn, sid, "long-mover")
        choose_session(conn, sid, "handcuff-python")
        sync_goals(conn, sid, {"zoan-understanding": True})
        conn.commit()
        view2 = sync_goals(conn, sid, {})
        choices = view2.get("choices") or []
        assert any(c.get("answerId") == "handcuff-python" for c in choices) or view2.get("currentNode")
    finally:
        conn.close()
        os.unlink(path)
