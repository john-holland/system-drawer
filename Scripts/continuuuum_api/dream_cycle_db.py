"""Dream cycle database helpers."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_dream_cycle_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "dream_day_sessions"):
        sql = (_SCHEMA_ROOT / "continuuuum_dream_cycle_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    _migrate_dream_cycle_columns(conn)
    conn.commit()


def _column_exists(conn: sqlite3.Connection, table: str, column: str) -> bool:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return any(row[1] == column for row in cur.fetchall())


def _migrate_dream_cycle_columns(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "dream_day_sessions"):
        return
    migrations = [
        ("dream_day_sessions", "outer_session_id", "TEXT"),
        ("dream_day_sessions", "good_day_collapse_seed", "INTEGER DEFAULT 0"),
        ("dream_day_sessions", "dream_day_collapse_seed", "INTEGER DEFAULT 0"),
        ("dream_day_sessions", "layer_json", "TEXT DEFAULT '{}'"),
        ("dream_day_sessions", "double_day", "INTEGER DEFAULT 0"),
        ("dream_memory_recalls", "safe_refrain_json", "TEXT DEFAULT '{}'"),
    ]
    for table, col, col_type in migrations:
        if not _column_exists(conn, table, col):
            conn.execute(f"ALTER TABLE {table} ADD COLUMN {col} {col_type}")


def save_day_session(conn: sqlite3.Connection, session: dict[str, Any]) -> None:
    ensure_dream_cycle_schema(conn)
    now = _now()
    conn.execute(
        """INSERT INTO dream_day_sessions
           (session_id, city_id, day_prompt, lemma_ids_json, aspect_states_json,
            day_collapse_seed, quad_digest_json, outer_session_id, good_day_collapse_seed,
            dream_day_collapse_seed, layer_json, double_day, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            session["sessionId"],
            session["cityId"],
            session.get("dayPrompt") or session.get("dreamDayPrompt"),
            json.dumps(session.get("lemmaIds") or []),
            json.dumps(session.get("aspectStates") or []),
            int(session.get("dayCollapseSeed") or session.get("dreamDayCollapseSeed") or 0),
            json.dumps(session.get("quadDigest") or {}),
            session.get("outerSessionId"),
            int(session.get("goodDayCollapseSeed") or 0),
            int(session.get("dreamDayCollapseSeed") or session.get("dayCollapseSeed") or 0),
            json.dumps(session.get("layer") or session.get("layerJson") or {}),
            1 if session.get("doubleDay") else 0,
            session.get("createdAt") or now,
            now,
        ),
    )
    conn.commit()


def load_day_session(conn: sqlite3.Connection, session_id: str) -> dict[str, Any] | None:
    ensure_dream_cycle_schema(conn)
    cur = conn.execute("SELECT * FROM dream_day_sessions WHERE session_id = ?", (session_id,))
    row = cur.fetchone()
    if not row:
        return None
    return {
        "sessionId": row["session_id"],
        "cityId": row["city_id"],
        "dayPrompt": row["day_prompt"],
        "lemmaIds": json.loads(row["lemma_ids_json"] or "[]"),
        "aspectStates": json.loads(row["aspect_states_json"] or "[]"),
        "dayCollapseSeed": row["day_collapse_seed"],
        "quadDigest": json.loads(row["quad_digest_json"] or "{}"),
        "outerSessionId": row["outer_session_id"] if "outer_session_id" in row.keys() else None,
        "goodDayCollapseSeed": row["good_day_collapse_seed"] if "good_day_collapse_seed" in row.keys() else 0,
        "dreamDayCollapseSeed": row["dream_day_collapse_seed"] if "dream_day_collapse_seed" in row.keys() else row["day_collapse_seed"],
        "layer": json.loads(row["layer_json"] or "{}") if "layer_json" in row.keys() else {},
        "doubleDay": bool(row["double_day"]) if "double_day" in row.keys() else False,
        "createdAt": row["created_at"],
    }


def save_sleep_session(conn: sqlite3.Connection, night: dict[str, Any]) -> None:
    ensure_dream_cycle_schema(conn)
    wave = night.get("wave") or {}
    conn.execute(
        """INSERT INTO dream_sleep_sessions
           (sleep_session_id, day_session_id, wave_json, phase_markers_json, sleep_seed, created_at)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (
            night["sleepSessionId"],
            night["daySessionId"],
            json.dumps(wave.get("waveSamples") or []),
            json.dumps(wave.get("phases") or []),
            int(night.get("sleepSeed") or 0),
            _now(),
        ),
    )
    conn.commit()


def load_sleep_session(conn: sqlite3.Connection, sleep_session_id: str) -> dict[str, Any] | None:
    ensure_dream_cycle_schema(conn)
    cur = conn.execute("SELECT * FROM dream_sleep_sessions WHERE sleep_session_id = ?", (sleep_session_id,))
    row = cur.fetchone()
    if not row:
        return None
    return {
        "sleepSessionId": row["sleep_session_id"],
        "daySessionId": row["day_session_id"],
        "waveSamples": json.loads(row["wave_json"] or "[]"),
        "phases": json.loads(row["phase_markers_json"] or "[]"),
        "sleepSeed": row["sleep_seed"],
    }


def save_memory_recall(
    conn: sqlite3.Connection,
    sleep_session_id: str,
    actor_id: str | None,
    output: dict[str, Any],
    safe_refrain: dict[str, Any] | None = None,
) -> str:
    ensure_dream_cycle_schema(conn)
    import uuid

    recall_id = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO dream_memory_recalls
           (recall_id, sleep_session_id, actor_id, lstm_output_json, safe_refrain_json, created_at)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (recall_id, sleep_session_id, actor_id, json.dumps(output), json.dumps(safe_refrain or {}), _now()),
    )
    conn.commit()
    return recall_id
