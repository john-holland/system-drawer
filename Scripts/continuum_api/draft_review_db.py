"""Ensure draft episode and review workflow schema on continuum.db."""

from __future__ import annotations

import re
import sqlite3
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
DRAFT_SCHEMA = REPO_ROOT / "continuum_draft_schema.sql"
REVIEW_SCHEMA = REPO_ROOT / "continuum_review_schema.sql"
LOCALIZATION_WORKFLOW_SCHEMA = REPO_ROOT / "continuum_localization_workflow_schema.sql"
SCRIPT_OUTPUT_SCHEMA = REPO_ROOT / "continuum_script_output_schema.sql"

_draft_review_ready = False


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


def _ensure_review_comment_columns(conn: sqlite3.Connection) -> None:
    alters = [
        "ALTER TABLE reviewer ADD COLUMN review_cycle INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE reviewer_comments ADD COLUMN review_cycle INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE reviewer_comments ADD COLUMN property_key TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN comment_topic_id TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_requested_at TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_requested_by TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_approved_at TEXT",
        "ALTER TABLE reviewer_comments ADD COLUMN delete_approved_by TEXT",
    ]
    for sql in alters:
        try:
            conn.execute(sql)
        except sqlite3.OperationalError:
            pass


def ensure_draft_review_schema(conn: sqlite3.Connection) -> None:
    global _draft_review_ready
    if _draft_review_ready:
        return
    _run_statements(conn, DRAFT_SCHEMA)
    _run_statements(conn, REVIEW_SCHEMA)
    _ensure_review_comment_columns(conn)
    try:
        _run_statements(conn, LOCALIZATION_WORKFLOW_SCHEMA)
    except sqlite3.OperationalError:
        pass
    try:
        _run_statements(conn, SCRIPT_OUTPUT_SCHEMA)
    except sqlite3.OperationalError:
        pass
    conn.commit()
    _draft_review_ready = True


def ensure_draft_review_schema_force(conn: sqlite3.Connection) -> None:
    """Re-run migrations (tests)."""
    global _draft_review_ready
    _draft_review_ready = False
    ensure_draft_review_schema(conn)
