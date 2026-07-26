"""Resolve buildingTypeId → prefabId / lemma."""

from __future__ import annotations

import json
import sqlite3
from typing import Any


def resolve_building_type(conn: sqlite3.Connection, building_type_id: str) -> dict[str, Any] | None:
    cur = conn.execute("SELECT * FROM building_type_maps WHERE building_type_id = ?", (building_type_id,))
    row = cur.fetchone()
    if not row:
        return None
    prefab_id = row["prefab_id"]
    lemma_id = row["lemma_entry_id"]
    if not prefab_id and lemma_id:
        cur2 = conn.execute(
            "SELECT property_value FROM thesaurus_entry_properties WHERE entry_id = ? AND property_key = ?",
            (lemma_id, "prefab-id"),
        )
        p = cur2.fetchone()
        if p:
            prefab_id = p["property_value"]
    return {
        "buildingTypeId": row["building_type_id"],
        "displayName": row["display_name"],
        "propertyClass": row["property_class"],
        "prefabId": prefab_id,
        "lemmaEntryId": lemma_id,
        "defaultOpexUsd": row["default_opex_usd"],
        "serviceProfile": json.loads(row["service_profile_json"] or "{}"),
    }


def resolve_for_zone(
    conn: sqlite3.Connection,
    property_class: str,
    zone_id: str | None = None,
) -> dict[str, Any] | None:
    cur = conn.execute(
        "SELECT * FROM building_type_maps WHERE property_class = ? ORDER BY priority DESC",
        (property_class,),
    )
    for row in cur.fetchall():
        allowed = json.loads(row["allowed_zone_ids_json"] or "null")
        if allowed and zone_id and zone_id not in allowed:
            continue
        return resolve_building_type(conn, row["building_type_id"])
    return None
