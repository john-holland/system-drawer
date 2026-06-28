"""Tests for spatial 4D timeline origin."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuum_api"
sys.path.insert(0, str(API))

from spatial_timeline import (  # noqa: E402
    compute_narrative_calendar_anchor,
    get_spatial_4d_timeline_origin,
)


def _db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    conn.executescript(
        """
        CREATE TABLE episodes (id TEXT PRIMARY KEY, t_start REAL NOT NULL, t_end REAL NOT NULL);
        CREATE TABLE spatial_4d (
            id TEXT PRIMARY KEY, episode_id TEXT, t_min REAL NOT NULL, t_max REAL NOT NULL,
            center_x REAL, center_y REAL, center_z REAL, size_x REAL, size_y REAL, size_z REAL,
            tenant_id TEXT, created_at TEXT
        );
        CREATE TABLE stories (id TEXT PRIMARY KEY, resaurce_schedule_id TEXT, episode_id TEXT);
        """
    )
    conn.execute("INSERT INTO episodes VALUES ('ep1', 0, 100)")
    conn.execute(
        "INSERT INTO spatial_4d (id, episode_id, t_min, t_max, center_x, center_y, center_z, size_x, size_y, size_z, tenant_id, created_at) VALUES ('s1','ep1', 0, 50, 0,0,0, 1,1,1, 'default', 'now')"
    )
    conn.execute("INSERT INTO stories VALUES ('st1', 'sched_a', 'ep1')")
    conn.commit()
    return conn


def test_spatial_origin_from_schedule():
    conn = _db()
    origin = get_spatial_4d_timeline_origin(conn, schedule_id="sched_a")
    assert origin["narrativeTOrigin"] == 0.0
    assert origin["spatial4dVolumeCount"] == 1
    assert "ep1" in origin["episodeIds"]


def test_narrative_anchor_with_offset():
    assert compute_narrative_calendar_anchor("2026-01-01", 7) == "2026-01-08"
