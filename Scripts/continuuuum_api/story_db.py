"""Ensure agile story / work-order extension schema on continuuuum.db."""

from __future__ import annotations

import sqlite3
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
EPISODES_SCHEMA = REPO_ROOT / "continuuuum_episodes_schema.sql"
STORIES_SCHEMA = REPO_ROOT / "continuuuum_stories_schema.sql"

WORK_ORDER_COLUMNS = [
    ("story_id", "TEXT REFERENCES stories(id)"),
    ("asset_kind", "TEXT"),
    ("asset_ref_json", "TEXT"),
    ("causality_test_status", "TEXT"),
    ("causality_test_log_json", "TEXT"),
    ("updated_at", "TEXT"),
]

OVERLAY_COLUMNS = [
    ("resaurce_schedule_id", "TEXT"),
    ("spatial_4d_episode_id", "TEXT"),
    ("narrative_start_offset_days", "REAL NOT NULL DEFAULT 0"),
]


def _table_columns(conn: sqlite3.Connection, table: str) -> set[str]:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return {row[1] for row in cur.fetchall()}


def _add_column_if_missing(conn: sqlite3.Connection, table: str, name: str, decl: str) -> None:
    if name in _table_columns(conn, table):
        return
    conn.execute(f"ALTER TABLE {table} ADD COLUMN {name} {decl}")


def ensure_episodes_schema(conn: sqlite3.Connection) -> None:
    """Episodes + base work_orders (required before stories schema)."""
    if EPISODES_SCHEMA.is_file():
        conn.executescript(EPISODES_SCHEMA.read_text(encoding="utf-8"))
    conn.commit()


def ensure_stories_schema(conn: sqlite3.Connection) -> None:
    ensure_episodes_schema(conn)
    if STORIES_SCHEMA.is_file():
        conn.executescript(STORIES_SCHEMA.read_text(encoding="utf-8"))
    cur = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name='work_orders'"
    )
    if cur.fetchone():
        for name, decl in WORK_ORDER_COLUMNS:
            _add_column_if_missing(conn, "work_orders", name, decl)
    cur = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name='narrative_timeline_overlay'"
    )
    if cur.fetchone():
        for name, decl in OVERLAY_COLUMNS:
            _add_column_if_missing(conn, "narrative_timeline_overlay", name, decl)
    conn.commit()
