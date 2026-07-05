"""Draft/review schema bootstrap."""

import sqlite3
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from continuuuum_api.draft_review_db import ensure_draft_review_schema_force


def test_draft_review_schema_creates_core_tables():
    conn = sqlite3.connect(":memory:")
    ensure_draft_review_schema_force(conn)
    tables = {r[0] for r in conn.execute("SELECT name FROM sqlite_master WHERE type='table'")}
    assert "draft_episodes" in tables
    assert "notifications" in tables
    assert "reviewer" in tables
    assert "reviewer_comments" in tables
    conn.close()
