"""Top-down spatial map payload for city-config UI."""

from __future__ import annotations

import hashlib
import json
import math
import sqlite3
from typing import Any


ZONE_COLORS = {
    "private": "#4a90d9",
    "commercial": "#e6a23c",
    "public": "#67c23a",
    "religious": "#9b59b6",
    "hobby_venue": "#1abc9c",
    "addiction_venue": "#e74c3c",
}


def _side_from_size(city_size_sqm: float) -> float:
    return math.sqrt(max(city_size_sqm, 1))


def layout_zones_striped(
    zones: list[dict],
    allocations: list[dict],
    width_m: float,
    depth_m: float,
) -> list[dict]:
    if not allocations:
        return []
    total_share = sum(a.get("areaSqm", 1) for a in allocations) or 1
    x0 = -width_m / 2
    out = []
    for alloc in allocations:
        share = alloc.get("areaSqm", 1) / total_share
        strip_w = width_m * share
        zone = next((z for z in zones if z.get("id") == alloc.get("zoneId")), {})
        poly = zone.get("layoutPolygon")
        if poly:
            polygon = [[p[0], p[1]] for p in poly]
        else:
            polygon = [
                [x0, -depth_m / 2],
                [x0 + strip_w, -depth_m / 2],
                [x0 + strip_w, depth_m / 2],
                [x0, depth_m / 2],
            ]
        pc = alloc.get("propertyClass") or zone.get("propertyClass", "public")
        out.append(
            {
                "zoneId": alloc.get("zoneId"),
                "propertyClass": pc,
                "color": ZONE_COLORS.get(pc, "#888"),
                "polygon": polygon,
                "areaSqm": alloc.get("areaSqm"),
                "label": zone.get("id", alloc.get("zoneId")),
            }
        )
        x0 += strip_w
    return out


def _jitter_pin(stable_id: str, zone_poly: list) -> tuple[float, float]:
    if not zone_poly:
        return 0.0, 0.0
    xs = [p[0] for p in zone_poly]
    zs = [p[1] for p in zone_poly]
    cx = sum(xs) / len(xs)
    cz = sum(zs) / len(zs)
    h = int(hashlib.md5(stable_id.encode()).hexdigest()[:8], 16)
    return cx + (h % 100 - 50), cz + ((h >> 8) % 100 - 50)


def build_spatial_map(
    conn: sqlite3.Connection,
    city_id: str,
    city_size_sqm: float,
    zone_document: dict,
    zoning_result: dict | None = None,
) -> dict[str, Any]:
    width = _side_from_size(city_size_sqm)
    depth = width
    bounds = {"centerX": 0, "centerZ": 0, "widthM": width, "depthM": depth}
    allocations = (zoning_result or {}).get("allocations") or []
    zones = layout_zones_striped(zone_document.get("zones", []), allocations, width, depth)
    zone_by_id = {z["zoneId"]: z for z in zones}

    cur = conn.execute("SELECT * FROM building_registry WHERE city_id = ?", (city_id,))
    buildings = []
    for row in cur.fetchall():
        zx = row["pin_local_x"]
        zz = row["pin_local_z"]
        if zx is None or zz is None:
            zp = zone_by_id.get(row["zone_id"] or "", {})
            zx, zz = _jitter_pin(row["stable_id"], zp.get("polygon", []))
        buildings.append(
            {
                "stableId": row["stable_id"],
                "buildingTypeId": row["building_type_id"],
                "displayName": row["display_name"] or row["stable_id"],
                "zoneId": row["zone_id"],
                "pinLocalX": zx,
                "pinLocalZ": zz,
                "propertyClass": row["property_class"],
            }
        )

    profile = (zoning_result or {}).get("cityScapeProfile") or {}
    planned = profile.get("plannedBuildings") or []

    return {
        "bounds": bounds,
        "zones": zones,
        "buildings": buildings,
        "plannedBuildings": planned,
    }
