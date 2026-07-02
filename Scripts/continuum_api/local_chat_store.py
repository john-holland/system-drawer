"""Local SQLite chat store when resaurce Cave is unavailable (dev / single-server)."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id(prefix: str = "chat") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def ensure_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS chat_rooms (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            room_type TEXT NOT NULL DEFAULT 'story',
            continuum_story_id TEXT,
            continuum_table_read_session_id TEXT,
            participants_json TEXT NOT NULL DEFAULT '[]',
            watchers_json TEXT NOT NULL DEFAULT '[]',
            created_at TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_chat_rooms_story ON chat_rooms(continuum_story_id);
        CREATE INDEX IF NOT EXISTS idx_chat_rooms_table_read ON chat_rooms(continuum_table_read_session_id);
        CREATE TABLE IF NOT EXISTS chat_messages (
            id TEXT PRIMARY KEY,
            room_id TEXT NOT NULL,
            sender TEXT NOT NULL,
            content TEXT NOT NULL,
            type TEXT NOT NULL DEFAULT 'user',
            timestamp TEXT NOT NULL,
            FOREIGN KEY (room_id) REFERENCES chat_rooms(id)
        );
        CREATE INDEX IF NOT EXISTS idx_chat_messages_room ON chat_messages(room_id, timestamp);
        """
    )
    conn.commit()


def _room_row_to_client(row: sqlite3.Row, messages: list[dict[str, Any]] | None = None) -> dict[str, Any]:
    return {
        "id": row["id"],
        "name": row["name"],
        "room_type": row["room_type"],
        "continuum_story_id": row["continuum_story_id"],
        "continuum_table_read_session_id": row["continuum_table_read_session_id"],
        "participants": json.loads(row["participants_json"] or "[]"),
        "watchers": json.loads(row["watchers_json"] or "[]"),
        "messages": messages or [],
        "createdAt": row["created_at"],
    }


def find_story_room(conn: sqlite3.Connection, story_id: str) -> dict[str, Any] | None:
    ensure_schema(conn)
    row = conn.execute(
        "SELECT * FROM chat_rooms WHERE continuum_story_id = ? LIMIT 1",
        (story_id,),
    ).fetchone()
    return _room_row_to_client(row) if row else None


def ensure_story_room(
    conn: sqlite3.Connection,
    story_id: str,
    summary: str | None = None,
    assignees: list[str] | None = None,
    watchers: list[str] | None = None,
) -> str:
    ensure_schema(conn)
    existing = conn.execute(
        "SELECT id FROM chat_rooms WHERE continuum_story_id = ? LIMIT 1",
        (story_id,),
    ).fetchone()
    participants = sorted(
        {str(u) for u in (assignees or []) + (watchers or []) if u}
    )
    watchers_list = [str(u) for u in (watchers or []) if u]
    now = _now()
    if existing:
        conn.execute(
            """UPDATE chat_rooms SET participants_json = ?, watchers_json = ?, name = COALESCE(?, name)
               WHERE id = ?""",
            (json.dumps(participants), json.dumps(watchers_list), summary, existing["id"]),
        )
        conn.commit()
        return existing["id"]

    room_id = _new_id()
    conn.execute(
        """INSERT INTO chat_rooms
           (id, name, room_type, continuum_story_id, participants_json, watchers_json, created_at)
           VALUES (?, ?, 'story', ?, ?, ?, ?)""",
        (room_id, summary or f"Story {story_id}", story_id, json.dumps(participants), json.dumps(watchers_list), now),
    )
    conn.commit()
    return room_id


def ensure_table_read_room(
    conn: sqlite3.Connection,
    session_id: str,
    summary: str | None = None,
    participants: list[str] | None = None,
) -> str:
    ensure_schema(conn)
    existing = conn.execute(
        "SELECT id FROM chat_rooms WHERE continuum_table_read_session_id = ? LIMIT 1",
        (session_id,),
    ).fetchone()
    ids = sorted({str(u) for u in (participants or []) if u})
    now = _now()
    if existing:
        conn.execute(
            "UPDATE chat_rooms SET participants_json = ? WHERE id = ?",
            (json.dumps(ids), existing["id"]),
        )
        conn.commit()
        return existing["id"]

    room_id = _new_id()
    conn.execute(
        """INSERT INTO chat_rooms
           (id, name, room_type, continuum_table_read_session_id, participants_json, watchers_json, created_at)
           VALUES (?, ?, 'table_read', ?, ?, '[]', ?)""",
        (room_id, summary or f"Table read {session_id[:8]}", session_id, json.dumps(ids), now),
    )
    conn.commit()
    return room_id


def list_messages(conn: sqlite3.Connection, room_id: str) -> list[dict[str, Any]]:
    ensure_schema(conn)
    rows = conn.execute(
        """SELECT id, room_id, sender, content, type, timestamp
           FROM chat_messages WHERE room_id = ? ORDER BY timestamp ASC""",
        (room_id,),
    ).fetchall()
    return [
        {
            "id": r["id"],
            "room_id": r["room_id"],
            "sender": r["sender"],
            "content": r["content"],
            "type": r["type"],
            "timestamp": r["timestamp"],
        }
        for r in rows
    ]


def append_message(
    conn: sqlite3.Connection,
    room_id: str,
    sender: str,
    content: str,
    msg_type: str = "user",
) -> dict[str, Any] | None:
    ensure_schema(conn)
    room = conn.execute("SELECT id FROM chat_rooms WHERE id = ?", (room_id,)).fetchone()
    if not room:
        return None
    msg_id = _new_id("msg")
    now = _now()
    conn.execute(
        """INSERT INTO chat_messages (id, room_id, sender, content, type, timestamp)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (msg_id, room_id, sender, content, msg_type, now),
    )
    conn.commit()
    return {
        "id": msg_id,
        "room_id": room_id,
        "sender": sender,
        "content": content,
        "type": msg_type,
        "timestamp": now,
    }
