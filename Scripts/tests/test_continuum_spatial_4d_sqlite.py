"""
SQLite integration tests for Continuum schema SQL (episodes + spatial_4d + spatial_4d_history).

These validate the SQL shipped under Scripts/ against SQLite (same engine as continuum_api/server.py).

Run from Drawer 2 repo (install pytest if needed):
  cd Scripts
  pytest tests/test_continuum_spatial_4d_sqlite.py -v
"""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Set

import pytest

SCRIPTS_DIR = Path(__file__).resolve().parent.parent


def _read_sql(name: str) -> str:
    path = SCRIPTS_DIR / name
    if not path.is_file():
        pytest.skip(f"Missing schema file: {path}")
    return path.read_text(encoding="utf-8")


@pytest.fixture
def conn_episodes_and_spatial(tmp_path: Path) -> sqlite3.Connection:
    """Fresh DB with continuum_episodes_schema + continuum_spatial_4d_schema applied."""
    db_path = tmp_path / "continuum_test.db"
    conn = sqlite3.connect(str(db_path))
    conn.execute("PRAGMA foreign_keys = ON")
    conn.executescript(_read_sql("continuum_episodes_schema.sql"))
    conn.executescript(_read_sql("continuum_spatial_4d_schema.sql"))
    conn.commit()
    return conn


def _table_names(conn: sqlite3.Connection) -> Set[str]:
    cur = conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
    )
    return {row[0] for row in cur.fetchall()}


def test_spatial_4d_and_history_tables_exist(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    names = _table_names(conn_episodes_and_spatial)
    assert "spatial_4d" in names
    assert "spatial_4d_history" in names
    assert "episodes" in names


def test_spatial_4d_insert_with_gateway_leaves_and_json(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    history_blob = {
        "rows": [
            {
                "leafBack": "S0.O1",
                "leafPause": "S1.O1",
                "leafForward": "S2.O1",
                "flags": 0,
                "narrativeT": 10.5,
                "eventType": "volume_enter",
            }
        ]
    }
    conn.execute(
        """
        INSERT INTO spatial_4d (
            id, tenant_id, created_at,
            center_x, center_y, center_z, size_x, size_y, size_z,
            t_min, t_max, payload_label,
            causality_leaf_base, causality_leaf_back, causality_leaf_pause, causality_leaf_forward,
            causality_history_json
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (
            "s4d-1",
            "default",
            "2026-01-01T00:00:00Z",
            1.0,
            2.0,
            3.0,
            4.0,
            4.0,
            4.0,
            0.0,
            3600.0,
            "Start",
            "S1.O0",
            "S0.O0",
            "S1.O0",
            "S2.O0",
            json.dumps(history_blob),
        ),
    )
    conn.commit()
    cur = conn.execute("SELECT * FROM spatial_4d WHERE id = ?", ("s4d-1",))
    row = cur.fetchone()
    assert row is not None
    cols = [d[0] for d in cur.description]
    data = dict(zip(cols, row))
    assert data["causality_leaf_back"] == "S0.O0"
    assert data["causality_leaf_pause"] == "S1.O0"
    assert data["causality_leaf_forward"] == "S2.O0"
    assert data["causality_leaf_base"] == "S1.O0"
    parsed = json.loads(data["causality_history_json"])
    assert parsed["rows"][0]["leafBack"] == "S0.O1"


def test_spatial_4d_episode_id_nullable(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    conn.execute(
        """
        INSERT INTO spatial_4d (
            id, tenant_id, created_at,
            center_x, center_y, center_z, size_x, size_y, size_z,
            t_min, t_max
        ) VALUES (?, ?, ?, 0, 0, 0, 1, 1, 1, 0, 1)
        """,
        ("s4d-orphan", "default", "2026-01-01T00:00:00Z"),
    )
    conn.commit()
    r = conn.execute("SELECT episode_id FROM spatial_4d WHERE id = ?", ("s4d-orphan",)).fetchone()
    assert r[0] is None


def test_spatial_4d_rejects_bad_episode_fk(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    with pytest.raises(sqlite3.IntegrityError):
        conn.execute(
            """
            INSERT INTO spatial_4d (
                id, tenant_id, episode_id, created_at,
                center_x, center_y, center_z, size_x, size_y, size_z,
                t_min, t_max
            ) VALUES (?, ?, ?, ?, 0, 0, 0, 1, 1, 1, 0, 1)
            """,
            ("s4d-bad-ep", "default", "no-such-episode", "2026-01-01T00:00:00Z"),
        )


def test_spatial_4d_accepts_valid_episode_fk(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    conn.execute(
        """
        INSERT INTO episodes (id, tenant_id, title, created_at, engine, t_start, t_end)
        VALUES (?, 'default', 'T', '2026-01-01T00:00:00Z', 'unity', 0, 100)
        """,
        ("ep-test-1",),
    )
    conn.execute(
        """
        INSERT INTO spatial_4d (
            id, tenant_id, episode_id, created_at,
            center_x, center_y, center_z, size_x, size_y, size_z,
            t_min, t_max
        ) VALUES (?, 'default', ?, '2026-01-01T00:00:00Z', 0, 0, 0, 1, 1, 1, 0, 1)
        """,
        ("s4d-with-ep", "ep-test-1"),
    )
    conn.commit()
    r = conn.execute("SELECT episode_id FROM spatial_4d WHERE id = ?", ("s4d-with-ep",)).fetchone()
    assert r[0] == "ep-test-1"


def test_spatial_4d_history_cascade_delete(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    conn.execute(
        """
        INSERT INTO spatial_4d (
            id, tenant_id, created_at,
            center_x, center_y, center_z, size_x, size_y, size_z,
            t_min, t_max
        ) VALUES (?, 'default', '2026-01-01T00:00:00Z', 0, 0, 0, 1, 1, 1, 0, 1)
        """,
        ("s4d-parent",),
    )
    conn.execute(
        """
        INSERT INTO spatial_4d_history (
            id, spatial_4d_id, step_index, leaf_back, leaf_pause, leaf_forward, flags, narrative_t, event_type
        ) VALUES (?, ?, 0, 'B', 'P', 'F', 1, 10.0, 'volume_enter')
        """,
        ("h1", "s4d-parent"),
    )
    conn.commit()
    assert conn.execute("SELECT COUNT(*) FROM spatial_4d_history").fetchone()[0] == 1
    conn.execute("DELETE FROM spatial_4d WHERE id = ?", ("s4d-parent",))
    conn.commit()
    assert conn.execute("SELECT COUNT(*) FROM spatial_4d_history").fetchone()[0] == 0


def test_spatial_4d_history_flags_json_roundtrip(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    conn.execute(
        """
        INSERT INTO spatial_4d (
            id, tenant_id, created_at,
            center_x, center_y, center_z, size_x, size_y, size_z,
            t_min, t_max
        ) VALUES ('p', 'default', '2026-01-01T00:00:00Z', 0, 0, 0, 1, 1, 1, 0, 1)
        """
    )
    extra = json.dumps({"door": 1, "revisit": 2})
    conn.execute(
        """
        INSERT INTO spatial_4d_history (
            id, spatial_4d_id, step_index, flags, flags_json, px, py, pz
        ) VALUES ('h2', 'p', 0, 0, ?, 1.0, 2.0, 3.0)
        """,
        (extra,),
    )
    conn.commit()
    raw = conn.execute("SELECT flags_json FROM spatial_4d_history WHERE id = 'h2'").fetchone()[0]
    assert json.loads(raw)["door"] == 1


def test_index_names_exist(conn_episodes_and_spatial: sqlite3.Connection) -> None:
    conn = conn_episodes_and_spatial
    indexes = {
        row[0]
        for row in conn.execute(
            "SELECT name FROM sqlite_master WHERE type='index' AND name IS NOT NULL"
        )
    }
    assert "idx_spatial_4d_episode" in indexes
    assert "idx_spatial_4d_tenant" in indexes
    assert "idx_s4dh_spatial" in indexes
