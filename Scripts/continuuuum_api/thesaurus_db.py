"""Ensure core thesaurus / lemma library schema on continuuuum.db."""

from __future__ import annotations

import sqlite3
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]

SCHEMA_FILES = (
    "continuuuum_thesaurus_schema.sql",
    "continuuuum_localization_schema.sql",
    "continuuuum_lemma_library_schema.sql",
    "continuuuum_lemma_component_metadata_schema.sql",
    "continuuuum_chat_lemma_schema.sql",
)

VERSION_COLUMNS = (
    ("thesaurus_entries", "version", "TEXT DEFAULT '1.0'"),
    ("thesaurus_alternatives", "version", "TEXT"),
    ("episode_script", "min_thesaurus_version", "TEXT"),
)


def _table_exists(conn: sqlite3.Connection, table: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (table,),
    )
    return cur.fetchone() is not None


def _table_columns(conn: sqlite3.Connection, table: str) -> set[str]:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return {row[1] for row in cur.fetchall()}


def _add_column_if_missing(
    conn: sqlite3.Connection, table: str, name: str, decl: str
) -> None:
    if not _table_exists(conn, table):
        return
    if name in _table_columns(conn, table):
        return
    conn.execute(f"ALTER TABLE {table} ADD COLUMN {name} {decl}")


def _seed_default_language(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "languages"):
        return
    conn.execute(
        """
        INSERT OR IGNORE INTO languages (id, code, name, script_direction)
        VALUES ('en', 'en', 'English', 'ltr')
        """
    )


def ensure_thesaurus_schema(conn: sqlite3.Connection) -> None:
    """Apply lemma library base tables if missing (thesaurus_entries, languages, etc.)."""
    for name in SCHEMA_FILES:
        path = REPO_ROOT / name
        if path.is_file():
            conn.executescript(path.read_text(encoding="utf-8"))
    for table, col, decl in VERSION_COLUMNS:
        _add_column_if_missing(conn, table, col, decl)
    _seed_default_language(conn)
    conn.commit()
