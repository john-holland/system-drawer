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
        sql = (_SCHEMA_ROOT / "continuum_dream_cycle_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    conn.commit()


def save_day_session(conn: sqlite3.Connection, session: dict[str, Any]) -> None:
    ensure_dream_cycle_schema(conn)
    now = _now()
    conn.execute(
        """INSERT INTO dream_day_sessions
           (session_id, city_id, day_prompt, lemma_ids_json, aspect_states_json,
            day_collapse_seed, quad_digest_json, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            session["sessionId"],
            session["cityId"],
            session.get("dayPrompt"),
            json.dumps(session.get("lemmaIds") or []),
            json.dumps(session.get("aspectStates") or []),
            int(session.get("dayCollapseSeed") or 0),
            json.dumps(session.get("quadDigest") or {}),
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
) -> str:
    ensure_dream_cycle_schema(conn)
    import uuid

    recall_id = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO dream_memory_recalls
           (recall_id, sleep_session_id, actor_id, lstm_output_json, created_at)
           VALUES (?, ?, ?, ?, ?)""",
        (recall_id, sleep_session_id, actor_id, json.dumps(output), _now()),
    )
    conn.commit()
    return recall_id
