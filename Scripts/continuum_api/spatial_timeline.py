"""Spatial 4D timeline origin helpers for project calendar narrative overlay."""

from __future__ import annotations

import sqlite3
from datetime import datetime, timedelta
from typing import Any


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    row = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name = ?",
        (name,),
    ).fetchone()
    return bool(row)


def _episode_ids_for_scope(
    conn: sqlite3.Connection,
    episode_id: str | None,
    schedule_id: str | None,
) -> list[str]:
    if episode_id:
        return [episode_id]
    if schedule_id:
        rows = conn.execute(
            """SELECT DISTINCT episode_id FROM stories
               WHERE resaurce_schedule_id = ? AND episode_id IS NOT NULL""",
            (schedule_id,),
        ).fetchall()
        return [r["episode_id"] for r in rows if r["episode_id"]]
    return []


def get_spatial_4d_timeline_origin(
    conn: sqlite3.Connection,
    episode_id: str | None = None,
    schedule_id: str | None = None,
) -> dict[str, Any]:
    """Return spatial generator 4D t-origin (min t_min) for episode/schedule scope."""
    episode_ids = _episode_ids_for_scope(conn, episode_id, schedule_id)
    volume_count = 0
    t_min: float | None = None
    t_max: float | None = None

    if _table_exists(conn, "spatial_4d"):
        if episode_ids:
            placeholders = ",".join("?" * len(episode_ids))
            row = conn.execute(
                f"""SELECT MIN(t_min) AS t_min, MAX(t_max) AS t_max, COUNT(*) AS n
                    FROM spatial_4d WHERE episode_id IN ({placeholders})""",
                episode_ids,
            ).fetchone()
        else:
            row = conn.execute(
                "SELECT MIN(t_min) AS t_min, MAX(t_max) AS t_max, COUNT(*) AS n FROM spatial_4d"
            ).fetchone()
        if row and row["n"]:
            volume_count = int(row["n"])
            t_min = float(row["t_min"]) if row["t_min"] is not None else None
            t_max = float(row["t_max"]) if row["t_max"] is not None else None

    episode_t_start: float | None = None
    if episode_ids and _table_exists(conn, "episodes"):
        placeholders = ",".join("?" * len(episode_ids))
        row = conn.execute(
            f"SELECT MIN(t_start) AS t_start FROM episodes WHERE id IN ({placeholders})",
            episode_ids,
        ).fetchone()
        if row and row["t_start"] is not None:
            episode_t_start = float(row["t_start"])

    # Spatial generator narrative t=0 aligns with min volume t_min (or episode.t_start fallback).
    narrative_t_origin = t_min if t_min is not None else episode_t_start if episode_t_start is not None else 0.0

    return {
        "episodeIds": episode_ids,
        "spatial4dVolumeCount": volume_count,
        "spatial4dTMin": t_min,
        "spatial4dTMax": t_max,
        "episodeTStart": episode_t_start,
        "narrativeTOrigin": narrative_t_origin,
        "source": "spatial_4d" if volume_count else ("episode" if episode_t_start is not None else "default"),
    }


def add_calendar_days(iso_date: str | None, offset_days: float) -> str | None:
    if not iso_date:
        return None
    try:
        base = datetime.fromisoformat(iso_date[:10])
    except ValueError:
        return None
    return (base + timedelta(days=float(offset_days or 0))).strftime("%Y-%m-%d")


def compute_narrative_calendar_anchor(
    schedule_start_date: str | None,
    offset_days: float,
) -> str | None:
    """Calendar date for narrative t=0 = production schedule start + offset from spatial 4D origin."""
    return add_calendar_days(schedule_start_date, offset_days)
