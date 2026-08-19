"""Tests for thesaurus schema bootstrap."""

from __future__ import annotations

import sqlite3
import tempfile
from pathlib import Path

from continuuuum_api.thesaurus_db import ensure_thesaurus_schema
from continuuuum_api.lemma_merge import merge_vocabulary


def test_ensure_thesaurus_schema_creates_entries_table() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        db = Path(tmp) / "test.db"
        conn = sqlite3.connect(db)
        ensure_thesaurus_schema(conn)
        cur = conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='thesaurus_entries'"
        )
        assert cur.fetchone() is not None
        conn.close()


def test_merge_vocabulary_on_fresh_db() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        db = Path(tmp) / "test.db"
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_thesaurus_schema(conn)
        merged = merge_vocabulary(conn)
        assert isinstance(merged, dict)
        assert len(merged) >= 1  # built-in vocabulary
        conn.close()


def test_chat_open_close_lemma_property_specs_seeded() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        db = Path(tmp) / "test.db"
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_thesaurus_schema(conn)
        keys = {
            r["key"]
            for r in conn.execute(
                "SELECT key FROM localization_property_specs WHERE key IN (?,?,?,?,?,?,?)",
                (
                    "chat-op",
                    "product-id",
                    "session-id",
                    "compose-mode",
                    "chat-surface",
                    "auto-close-on-exit",
                    "require-entitlement",
                ),
            )
        }
        assert keys == {
            "chat-op",
            "product-id",
            "session-id",
            "compose-mode",
            "chat-surface",
            "auto-close-on-exit",
            "require-entitlement",
        }
        conn.close()
