"""Local SQLite production schedules when resaurce Cave is unavailable."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id(prefix: str) -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def ensure_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(
        """
        CREATE TABLE IF NOT EXISTS production_schedules (
            id TEXT PRIMARY KEY,
            budget_plan_id TEXT,
            name TEXT NOT NULL,
            start_date TEXT,
            end_date TEXT,
            timezone TEXT NOT NULL DEFAULT 'UTC',
            episode_ids_json TEXT NOT NULL DEFAULT '[]',
            draft_episode_ids_json TEXT NOT NULL DEFAULT '[]',
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS schedule_milestones (
            id TEXT PRIMARY KEY,
            schedule_id TEXT NOT NULL,
            label TEXT NOT NULL,
            start_date TEXT,
            end_date TEXT,
            continuuuum_story_ids_json TEXT NOT NULL DEFAULT '[]',
            FOREIGN KEY (schedule_id) REFERENCES production_schedules(id)
        );
        """
    )
    conn.commit()


def _milestone_row(row: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": row["id"],
        "schedule_id": row["schedule_id"],
        "label": row["label"],
        "start_date": row["start_date"],
        "end_date": row["end_date"],
        "continuuuum_story_ids": json.loads(row["continuuuum_story_ids_json"] or "[]"),
    }


def _schedule_row(conn: sqlite3.Connection, schedule_id: str) -> dict[str, Any] | None:
    row = conn.execute("SELECT * FROM production_schedules WHERE id = ?", (schedule_id,)).fetchone()
    if not row:
        return None
    milestones = [
        _milestone_row(m)
        for m in conn.execute(
            "SELECT * FROM schedule_milestones WHERE schedule_id = ? ORDER BY start_date, label",
            (schedule_id,),
        ).fetchall()
    ]
    return {
        "id": row["id"],
        "budget_plan_id": row["budget_plan_id"],
        "name": row["name"],
        "start_date": row["start_date"],
        "end_date": row["end_date"],
        "timezone": row["timezone"],
        "episode_ids": json.loads(row["episode_ids_json"] or "[]"),
        "draft_episode_ids": json.loads(row["draft_episode_ids_json"] or "[]"),
        "milestones": milestones,
        "created_at": row["created_at"],
        "updated_at": row["updated_at"],
    }


def list_schedules(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    ensure_schema(conn)
    ids = [r["id"] for r in conn.execute("SELECT id FROM production_schedules ORDER BY updated_at DESC").fetchall()]
    return [_schedule_row(conn, sid) for sid in ids if _schedule_row(conn, sid)]


def create_schedule(conn: sqlite3.Connection, payload: dict[str, Any]) -> dict[str, Any]:
    ensure_schema(conn)
    now = _now()
    schedule_id = _new_id("sched")
    episode_ids = payload.get("episode_ids") or payload.get("episodeIds") or []
    draft_ids = payload.get("draft_episode_ids") or payload.get("draftEpisodeIds") or []
    conn.execute(
        """INSERT INTO production_schedules
           (id, budget_plan_id, name, start_date, end_date, timezone,
            episode_ids_json, draft_episode_ids_json, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            schedule_id,
            payload.get("budget_plan_id") or payload.get("budgetPlanId"),
            str(payload.get("name") or "Production schedule"),
            payload.get("start_date") or payload.get("startDate"),
            payload.get("end_date") or payload.get("endDate"),
            payload.get("timezone") or "UTC",
            json.dumps(list(episode_ids)),
            json.dumps(list(draft_ids)),
            now,
            now,
        ),
    )
    milestones = payload.get("milestones") or []
    for m in milestones:
        conn.execute(
            """INSERT INTO schedule_milestones
               (id, schedule_id, label, start_date, end_date, continuuuum_story_ids_json)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (
                _new_id("ms"),
                schedule_id,
                str(m.get("label") or "Milestone"),
                m.get("start_date") or m.get("startDate"),
                m.get("end_date") or m.get("endDate"),
                json.dumps(m.get("continuuuum_story_ids") or m.get("continuuuumStoryIds") or []),
            ),
        )
    conn.commit()
    return _schedule_row(conn, schedule_id) or {}


def get_schedule(conn: sqlite3.Connection, schedule_id: str) -> dict[str, Any] | None:
    ensure_schema(conn)
    return _schedule_row(conn, schedule_id)


def update_milestone_stories(
    conn: sqlite3.Connection,
    milestone_id: str,
    story_ids: list[str],
) -> dict[str, Any] | None:
    ensure_schema(conn)
    row = conn.execute("SELECT * FROM schedule_milestones WHERE id = ?", (milestone_id,)).fetchone()
    if not row:
        return None
    conn.execute(
        "UPDATE schedule_milestones SET continuuuum_story_ids_json = ? WHERE id = ?",
        (json.dumps(list(story_ids or [])), milestone_id),
    )
    conn.commit()
    updated = conn.execute("SELECT * FROM schedule_milestones WHERE id = ?", (milestone_id,)).fetchone()
    return _milestone_row(updated) if updated else None
