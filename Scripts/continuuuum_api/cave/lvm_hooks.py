"""LVM2 append hooks after mutating Cave routes."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def lvm20_event(event_type: str, trace_id: str, meta: dict[str, Any] | None = None) -> dict[str, Any]:
    return {
        "schema_version": "lvm2.0",
        "id": str(uuid.uuid4()),
        "type": event_type,
        "trace_id": trace_id,
        "timestamp": _now(),
        "meta": meta or {},
    }


def append_lvm_events(get_conn: GetConn, trace_id: str, events: list[dict[str, Any]]) -> None:
    if not trace_id or not events:
        return
    try:
        conn = get_conn()
        conn.execute(
            """CREATE TABLE IF NOT EXISTS lvm_events (
                id TEXT PRIMARY KEY,
                trace_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                created_at TEXT NOT NULL
            )"""
        )
        for ev in events:
            conn.execute(
                "INSERT OR IGNORE INTO lvm_events (id, trace_id, event_type, payload_json, created_at) VALUES (?, ?, ?, ?, ?)",
                (
                    ev.get("id") or str(uuid.uuid4()),
                    trace_id,
                    ev.get("type") or "Unknown",
                    json.dumps(ev),
                    _now(),
                ),
            )
        conn.commit()
        conn.close()
    except sqlite3.Error:
        pass


def after_cave_route_mutation(
    ctx: dict[str, Any],
    out: dict[str, Any] | None,
    *,
    get_conn: GetConn | None,
    lvm_event_names: list[str],
) -> dict[str, Any] | None:
    if not out or out.get("ok") is False:
        return out
    if not lvm_event_names or not get_conn:
        return out
    trace_id = str(ctx.get("trace_id") or "")
    events = [
        lvm20_event(
            name,
            trace_id,
            {
                "route": ctx.get("structural"),
                "tenant": ctx.get("tenant") or None,
                "service": ctx.get("service") or "continuuuum",
            },
        )
        for name in lvm_event_names
    ]
    append_lvm_events(get_conn, trace_id, events)
    out = dict(out)
    out["lvm_appended"] = [e["type"] for e in events]
    return out
