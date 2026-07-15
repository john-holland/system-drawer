"""Credits lists schema ensure + helpers."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def new_id(prefix: str = "crd") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_credits_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "credits_lists"):
        sql = (_SCHEMA_ROOT / "continuuuum_credits_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    conn.commit()


def entry_is_visible(show_full_name: int | bool, show_nickname: int | bool) -> bool:
    return bool(show_full_name) or bool(show_nickname)


def warehouse_append(
    conn: sqlite3.Connection,
    *,
    tenant_id: str,
    list_id: str | None,
    event_kind: str,
    source: str = "manual",
    actor_user_id: str | None = None,
    payload: dict[str, Any] | None = None,
) -> str:
    ensure_credits_schema(conn)
    hid = new_id("cwh")
    conn.execute(
        """INSERT INTO credits_warehouse_history
           (id, tenant_id, list_id, event_kind, source, actor_user_id, payload_json, created_at)
           VALUES (?,?,?,?,?,?,?,?)""",
        (
            hid,
            tenant_id or "default",
            list_id,
            event_kind,
            source,
            actor_user_id,
            json.dumps(payload or {}, ensure_ascii=False),
            _now(),
        ),
    )
    return hid
