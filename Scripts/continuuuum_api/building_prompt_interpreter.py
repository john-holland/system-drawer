"""Building prompt interpreter — sync / merge / request."""

from __future__ import annotations

import sqlite3
from typing import Any

try:
    from continuuuum_api.building_flywheel import merge_cell, server_client_weight
    from continuuuum_api.building_type_resolver import resolve_building_type
    from continuuuum_api.political_solver import tick_city
except ImportError:
    from building_flywheel import merge_cell, server_client_weight
    from building_type_resolver import resolve_building_type
    from political_solver import tick_city


def sync_building(conn: sqlite3.Connection, city_id: str, stable_id: str) -> dict[str, Any]:
    cur = conn.execute("SELECT * FROM building_registry WHERE stable_id = ? AND city_id = ?", (stable_id, city_id))
    b = cur.fetchone()
    if not b:
        raise ValueError("building not found")
    snap_cur = conn.execute(
        "SELECT snapshot_json FROM society_snapshots WHERE city_id = ? ORDER BY tick_index DESC LIMIT 1",
        (city_id,),
    )
    snap_row = snap_cur.fetchone()
    snapshot = {}
    if snap_row:
        import json

        snapshot = json.loads(snap_row["snapshot_json"])
    resolved = resolve_building_type(conn, b["building_type_id"]) if b["building_type_id"] else None
    return {
        "building": dict(b),
        "snapshot": snapshot,
        "resolvedType": resolved,
    }


def merge_building_stats(local: float, remote: float, confidence: float = 0.8, timeout_order: int = 0) -> float:
    w = server_client_weight(confidence, timeout_order)
    return merge_cell(local, remote, w)


def request_building_prompt(conn: sqlite3.Connection, prompt: str, city_id: str) -> dict[str, Any]:
    prompt_l = prompt.lower()
    cur = conn.execute("SELECT building_type_id, display_name, property_class FROM building_type_maps")
    match = None
    for row in cur.fetchall():
        if row["building_type_id"] in prompt_l or (row["display_name"] or "").lower() in prompt_l:
            match = row["building_type_id"]
            break
    if not match:
        match = "city_hall"
    resolved = resolve_building_type(conn, match)
    return {"prompt": prompt, "buildingTypeId": match, "resolved": resolved}
