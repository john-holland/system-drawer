"""Compile city routing behavior trees from zoning + building registry."""

from __future__ import annotations

import sqlite3
from typing import Any

PROPERTY_ROUTE_ORDER = [
    "public",
    "religious",
    "commercial",
    "private",
    "hobby_venue",
    "addiction_venue",
]


def compile_routing_tree(conn: sqlite3.Connection, city_id: str) -> dict[str, Any]:
    cur = conn.execute(
        """SELECT stable_id, property_class, zone_id, building_type_id, display_name
           FROM building_registry WHERE city_id = ? ORDER BY property_class""",
        (city_id,),
    )
    by_class: dict[str, list] = {pc: [] for pc in PROPERTY_ROUTE_ORDER}
    for row in cur.fetchall():
        pc = row["property_class"] or "public"
        by_class.setdefault(pc, []).append(
            {
                "stableId": row["stable_id"],
                "zoneId": row["zone_id"],
                "buildingTypeId": row["building_type_id"],
                "displayName": row["display_name"],
            }
        )
    visit_order = []
    for pc in PROPERTY_ROUTE_ORDER:
        visit_order.extend(by_class.get(pc, []))
    return {
        "cityId": city_id,
        "treeId": f"society.routing.{city_id}",
        "visitOrder": visit_order,
        "nodes": [{"type": "visit", "building": b} for b in visit_order],
    }
