"""Table read ↔ resaurce chat room sync."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from typing import Any

from flask import request

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def _trace_id() -> str:
    return f"tr_{uuid.uuid4().hex[:12]}"


def resaurce_route(route: str, payload: dict[str, Any]) -> dict[str, Any]:
    body = json.dumps({"route": f"resaurce:{route}", "payload": payload, "trace_id": _trace_id()}).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode())
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError) as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}


def share_url(session_id: str, draft_episode_id: str) -> str:
    origin = (request.host_url if request else "http://127.0.0.1:5050/").rstrip("/")
    return f"{origin}/table-read?session={session_id}&draft={draft_episode_id}"


def send_chat_message(room_id: str, sender: str, content: str, msg_type: str = "user") -> dict[str, Any]:
    return resaurce_route(
        "chat/message/send",
        {
            "chat_room_id": room_id,
            "sender": sender,
            "content": content,
            "type": msg_type,
        },
    )


def _active_participant_ids(conn: sqlite3.Connection, session_id: str) -> list[str]:
    cur = conn.execute(
        """SELECT user_id FROM table_read_participants
           WHERE session_id = ? AND left_at IS NULL
           ORDER BY join_order ASC""",
        (session_id,),
    )
    return [r[0] for r in cur.fetchall()]


def ensure_table_read_schema_column(conn: sqlite3.Connection) -> None:
    try:
        conn.execute("SELECT resaurce_chat_room_id FROM table_read_sessions LIMIT 1")
    except sqlite3.OperationalError:
        conn.execute("ALTER TABLE table_read_sessions ADD COLUMN resaurce_chat_room_id TEXT")
        conn.commit()


def sync_table_read_chat(
    conn: sqlite3.Connection,
    session_row: sqlite3.Row,
    *,
    post_welcome: bool = False,
) -> str | None:
    """Ensure resaurce chat room for session; return chat room id."""
    ensure_table_read_schema_column(conn)
    session_id = session_row["id"]
    draft_id = session_row["draft_episode_id"]
    participants = _active_participant_ids(conn, session_id)
    if not participants:
        participants = [session_row["host_user_id"]]

    had_room = bool(session_row["resaurce_chat_room_id"] if "resaurce_chat_room_id" in session_row.keys() else None)
    out = resaurce_route(
        "chat/room/ensure-for-table-read",
        {
            "session_id": session_id,
            "draft_episode_id": draft_id,
            "participants": participants,
            "summary": f"Table read {session_id[:8]}",
        },
    )
    room = out.get("chat_room") or {}
    room_id = room.get("id")
    if not room_id:
        try:
            from continuuuum_api.local_chat_store import ensure_table_read_room
        except ImportError:
            from local_chat_store import ensure_table_read_room
        room_id = ensure_table_read_room(
            conn,
            session_id,
            f"Table read {session_id[:8]}",
            participants,
        )

    conn.execute(
        "UPDATE table_read_sessions SET resaurce_chat_room_id = ? WHERE id = ?",
        (room_id, session_id),
    )
    conn.commit()

    if post_welcome and not had_room:
        url = share_url(session_id, draft_id)
        send_chat_message(
            room_id,
            "continuuuum",
            f"Table read session started — [Join room]({url})",
            "system",
        )
    return room_id
