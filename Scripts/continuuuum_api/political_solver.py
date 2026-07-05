"""Political solver tick loop per city."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

from building_flywheel import flywheel_tick
from city_behavior_tree import compile_routing_tree
from gov_glove_adapter import call_gov_glove, to_society_snapshot
from society_graph import apply_preset
from zoning_rules_engine import parse_zone_document, run_zoning

from society_db import _now, new_id


def _load_city_config(conn: sqlite3.Connection, city_id: str) -> dict:
    cur = conn.execute("SELECT * FROM city_config WHERE city_id = ?", (city_id,))
    row = cur.fetchone()
    if row:
        return dict(row)
    return {
        "city_size_sqm": 1_000_000,
        "annual_budget_usd": 10_000_000,
        "allow_debt": 0,
        "commodity_indices_json": "{}",
    }


def _load_zone_doc(conn: sqlite3.Connection, city_id: str) -> dict:
    cur = conn.execute(
        "SELECT document_json FROM city_zone_documents WHERE city_id = ? ORDER BY version DESC LIMIT 1",
        (city_id,),
    )
    row = cur.fetchone()
    if row:
        return json.loads(row["document_json"])
    from society_db import default_zone_document

    return default_zone_document(city_id)


def _load_buildings(conn: sqlite3.Connection, city_id: str) -> list[dict]:
    cur = conn.execute("SELECT * FROM building_registry WHERE city_id = ?", (city_id,))
    return [dict(r) for r in cur.fetchall()]


def tick_city(conn: sqlite3.Connection, city_id: str, preset_id: str | None = None) -> dict[str, Any]:
    cfg = _load_city_config(conn, city_id)
    zone_doc = _load_zone_doc(conn, city_id)
    buildings = _load_buildings(conn, city_id)
    commodities = json.loads(cfg.get("commodity_indices_json") or "{}")
    lobby_activity = 0.3
    if preset_id:
        lobby_activity = apply_preset(preset_id).get("lobbyistActivity", lobby_activity)

    zoning = run_zoning(
        city_id,
        float(cfg["city_size_sqm"]),
        float(cfg["annual_budget_usd"]),
        zone_doc,
        buildings,
        commodities,
        bool(cfg["allow_debt"]),
        lobby_activity,
    )
    glove = call_gov_glove("processLobbyistImpacts", {"lobbyistActivity": lobby_activity})
    cur = conn.execute("SELECT COALESCE(MAX(tick_index), -1) + 1 FROM society_snapshots WHERE city_id = ?", (city_id,))
    tick_index = int(cur.fetchone()[0])
    snapshot = to_society_snapshot(city_id, tick_index, glove, zoning)
    now = _now()
    conn.execute(
        """INSERT INTO society_snapshots (id, city_id, tick_index, snapshot_json, created_at)
           VALUES (?, ?, ?, ?, ?)""",
        (new_id(), city_id, tick_index, json.dumps(snapshot), now),
    )
    fly = flywheel_tick(conn, city_id, tick_index, zoning)
    routing = compile_routing_tree(conn, city_id)
    frame = {"tickIndex": tick_index, "snapshot": snapshot, "routing": routing, "flywheel": fly}
    conn.execute(
        """INSERT INTO prebaked_timelines (id, city_id, frame_index, frame_json, created_at)
           VALUES (?, ?, ?, ?, ?)
           ON CONFLICT(city_id, frame_index) DO UPDATE SET frame_json=excluded.frame_json""",
        (new_id(), city_id, tick_index % 24, json.dumps(frame), now),
    )
    conn.execute(
        """INSERT INTO political_solver_runs (id, city_id, tick_index, status, detail_json, created_at)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (new_id(), city_id, tick_index, "ok", json.dumps({"preset": preset_id}), now),
    )
    profile = zoning.get("cityScapeProfile") or {}
    ver_cur = conn.execute(
        "SELECT COALESCE(MAX(version), 0) + 1 FROM city_scape_profiles WHERE city_id = ?",
        (city_id,),
    )
    ver = int(ver_cur.fetchone()[0])
    conn.execute(
        """INSERT INTO city_scape_profiles (id, city_id, version, profile_json, created_at)
           VALUES (?, ?, ?, ?, ?)""",
        (new_id(), city_id, ver, json.dumps(profile), now),
    )
    conn.commit()
    return {"tickIndex": tick_index, "snapshot": snapshot, "routing": routing, "cityScapeProfile": profile}
