"""Ensure audit log and user presence schema on continuum.db."""

from __future__ import annotations

import sqlite3
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
AUDIT_SCHEMA = REPO_ROOT / "continuum_audit_schema.sql"


def ensure_audit_schema(conn: sqlite3.Connection) -> None:
    if AUDIT_SCHEMA.is_file():
        conn.executescript(AUDIT_SCHEMA.read_text(encoding="utf-8"))
        conn.commit()
