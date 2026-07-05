"""Galactic body registry schema bootstrap and seeds."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
SCHEMA_PATH = REPO_ROOT / "continuuuum_galactic_schema.sql"

try:
    from continuuuum_api.society_db import ensure_society_tables
except ImportError:
    from society_db import ensure_society_tables


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_galactic_tables(conn: sqlite3.Connection) -> None:
    ensure_society_tables(conn)
    if SCHEMA_PATH.exists():
        conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    conn.commit()
    _seed_defaults(conn)


def _seed_defaults(conn: sqlite3.Connection) -> None:
    now = _now()
    seeds = [
        (
            "sol",
            "star",
            "Sol",
            0.0,
            0.0,
            0.0,
            1.989e30,
            696340000.0,
            1.0,
            50.0,
            None,
            None,
            None,
            None,
            None,
            1,
        ),
        (
            "earth",
            "planet",
            "Earth",
            1.496e11,
            0.0,
            0.0,
            5.972e24,
            6371000.0,
            0.01,
            1.0,
            "earth",
            None,
            None,
            None,
            None,
            1,
        ),
        (
            "little-prince",
            "planetoid",
            "Asteroid B-612",
            1.5e11,
            2.0e9,
            0.0,
            1.0e15,
            500.0,
            0.001,
            0.2,
            "little-prince",
            None,
            None,
            None,
            None,
            0,
        ),
        (
            "mars",
            "planet",
            "Mars",
            2.279e11,
            0.0,
            0.0,
            6.39e23,
            3389500.0,
            0.02,
            0.8,
            None,
            None,
            None,
            "celestial.mars.color",
            "celestial.mars.visibility",
            1,
        ),
        (
            "pluto",
            "planetoid",
            "Pluto",
            5.906e12,
            0.0,
            0.0,
            1.303e22,
            1188300.0,
            0.005,
            0.3,
            None,
            None,
            None,
            None,
            None,
            1,
        ),
    ]
    for row in seeds:
        cur = conn.execute("SELECT 1 FROM galactic_bodies WHERE body_id = ?", (row[0],))
        if cur.fetchone():
            continue
        conn.execute(
            """INSERT INTO galactic_bodies
               (body_id, kind, display_name, galactic_x, galactic_y, galactic_z,
                mass_kg, radius_m, radiation_level, gravity_well_strength,
                society_planet_id, usc_asset_id, scene_prefab_ref,
                lemma_color_id, lemma_visibility_id, immovable, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (*row, now, now),
        )
    conn.commit()
