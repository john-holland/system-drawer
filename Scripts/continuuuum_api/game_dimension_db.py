"""Ensure game/dimension schema on continuuuum.db and seed defaults."""

from __future__ import annotations

import re
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
GD_SCHEMA = REPO_ROOT / "continuuuum_game_dimension_schema.sql"

_gd_ready = False


def _utcnow() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _strip_leading_sql_comments(text: str) -> str:
    lines: list[str] = []
    for line in text.splitlines():
        stripped = line.strip()
        if not lines and (not stripped or stripped.startswith("--")):
            continue
        lines.append(line)
    return "\n".join(lines).strip()


def _split_sql(text: str) -> list[str]:
    parts: list[str] = []
    for chunk in re.split(r";\s*\n", text):
        stmt = _strip_leading_sql_comments(chunk)
        if stmt:
            parts.append(stmt)
    return parts


def _run_statements(conn: sqlite3.Connection, path: Path) -> None:
    if not path.is_file():
        return
    for stmt in _split_sql(path.read_text(encoding="utf-8")):
        try:
            conn.execute(stmt)
        except sqlite3.OperationalError as exc:
            msg = str(exc).lower()
            if "duplicate column" in msg or "already exists" in msg:
                continue
            raise


def _seed_defaults(conn: sqlite3.Connection) -> None:
    now = _utcnow()
    row = conn.execute("SELECT id FROM games WHERE slug = ?", ("main",)).fetchone()
    if not row:
        conn.execute(
            """
            INSERT INTO games (id, slug, display_name, active, is_public, created_at, updated_at)
            VALUES (?, 'main', 'Main', 1, 0, ?, ?)
            """,
            (str(uuid.uuid4()), now, now),
        )
    dim0 = conn.execute("SELECT id FROM dimensions WHERE dim_index = 0").fetchone()
    if not dim0:
        conn.execute(
            """
            INSERT INTO dimensions (id, dim_index, slug, display_name, is_public, created_at, updated_at)
            VALUES (?, 0, 'base', 'Dimension 0', 1, ?, ?)
            """,
            (str(uuid.uuid4()), now, now),
        )
    dim1 = conn.execute("SELECT id FROM dimensions WHERE dim_index = 1").fetchone()
    if not dim1:
        conn.execute(
            """
            INSERT INTO dimensions (id, dim_index, slug, display_name, is_public, created_at, updated_at)
            VALUES (?, 1, 'dim-1', 'Dimension 1', 0, ?, ?)
            """,
            (str(uuid.uuid4()), now, now),
        )


def ensure_game_dimension_schema(conn: sqlite3.Connection) -> None:
    global _gd_ready
    if _gd_ready:
        return
    _run_statements(conn, GD_SCHEMA)
    _seed_defaults(conn)
    conn.commit()
    _gd_ready = True


def ensure_game_dimension_schema_force(conn: sqlite3.Connection) -> None:
    """Re-run migrations (tests)."""
    global _gd_ready
    _gd_ready = False
    ensure_game_dimension_schema(conn)
