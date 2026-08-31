"""Private servers + OAuth tables on Continuuuum vote/lobby DB."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))

from vote_routes import (  # noqa: E402
    _ensure_session_private_server,
    _flip_private_server_runtime,
    _upsert_oauth,
    ensure_vote_tables,
)
from payroll_engine import ensure_payroll_schema  # noqa: E402


@pytest.fixture
def conn(tmp_path):
    db = tmp_path / "v.db"
    c = sqlite3.connect(db)
    c.row_factory = sqlite3.Row
    ensure_vote_tables(c)
    ensure_payroll_schema(c)
    return c


def test_oauth_upsert(conn):
    row = _upsert_oauth(conn, "minecraftuuuum", "microsoft", {"clientId": "abc", "azureTenant": "common"})
    assert row["clientId"] == "abc"
    again = _upsert_oauth(conn, "minecraftuuuum", "microsoft", {"clientId": "xyz"})
    assert again["id"] == row["id"]
    assert again["clientId"] == "xyz"


def test_private_server_and_flip(conn):
    conn.execute(
        """INSERT INTO game_sessions (id, lobby_session_name, display_name, created_utc, created_narrative_time, active)
           VALUES ('sess1', 'lobby-a', 'S', '2026-01-01T00:00:00Z', 0, 0)"""
    )
    conn.execute(
        """INSERT INTO game_lobbies (name, display_name, created_utc, runtime_kind, game_port)
           VALUES ('lobby-a', 'A', '2026-01-01T00:00:00Z', 'minecraft', 25565)"""
    )
    conn.commit()
    ps = _ensure_session_private_server(conn, "sess1", "lobby-a")
    assert ps["runtimeKind"] == "minecraft"
    assert ps["gameSessionId"] == "sess1"
    flipped = _flip_private_server_runtime(conn, ps["id"], "proton_unity")
    assert flipped["runtimeKind"] == "proton_unity"
    assert flipped["retainerSplit"]["serviceUnityEnabled"] is True
    assert flipped["retainerSplit"]["serviceUnrealEnabled"] is False
