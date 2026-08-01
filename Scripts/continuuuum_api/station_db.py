"""Station placard DB ensure + helpers."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any

SCHEMA_PATH = Path(__file__).resolve().parents[1] / "continuuuum_stations_schema.sql"

STATION_KINDS = ("cooking", "train", "bus", "computer", "generic")
ASSIGN_TYPES = ("building", "vehicle", "persona")


def ensure_station_tables(conn: sqlite3.Connection) -> None:
    conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    conn.commit()
    _seed_defaults(conn)


def _seed_defaults(conn: sqlite3.Connection) -> None:
    cur = conn.execute("SELECT COUNT(*) AS c FROM station_placards")
    row = cur.fetchone()
    count = int(row[0] if not isinstance(row, sqlite3.Row) else row["c"])
    if count > 0:
        return
    samples = [
        ("demo-city", "stn-cook-1", "Line Grill", "cooking", "bldg-restaurant-1", None, 3.0),
        ("demo-city", "stn-desk-1", "Dispatch Desk", "computer", "city_hall", None, 1.5),
        ("demo-city", "stn-bus-1", "Bay 2", "bus", "bus_depot", "bus-01", 2.0),
        ("demo-city", "stn-train-1", "Platform A", "train", "train_station", "train-01", 4.0),
    ]
    for city, sid, name, kind, bldg, vehicle, weight in samples:
        conn.execute(
            """INSERT INTO station_placards
               (city_id, stable_id, name, kind, config_json, building_stable_id, vehicle_id, staffing_weight, level_id)
               VALUES (?, ?, ?, ?, '{}', ?, ?, ?, 'demo-level')""",
            (city, sid, name, kind, bldg, vehicle, weight),
        )
        pid = conn.execute("SELECT last_insert_rowid()").fetchone()[0]
        conn.execute(
            """INSERT INTO station_commodities
               (station_id, commodity_key, cron_expr, surge_mult, quantity, price, availability)
               VALUES (?, ?, '0 */6 * * *', 1, 5, 1, 1)""",
            (pid, "labor" if kind != "cooking" else "power"),
        )
        conn.execute(
            """INSERT INTO station_assignments
               (station_id, assign_type, ref_id, role, pecking_order)
               VALUES (?, 'building', ?, ?, 10)""",
            (pid, bldg, kind),
        )
        if vehicle:
            conn.execute(
                """INSERT INTO station_assignments
                   (station_id, assign_type, ref_id, role, pecking_order)
                   VALUES (?, 'vehicle', ?, 'operator', 20)""",
                (pid, vehicle),
            )
    conn.commit()


def row_to_dict(row: sqlite3.Row | None) -> dict[str, Any] | None:
    if row is None:
        return None
    return {k: row[k] for k in row.keys()}


def _enrich_station(conn: sqlite3.Connection, st: dict[str, Any]) -> dict[str, Any]:
    sid = st["id"]
    st["commodities"] = [
        row_to_dict(r)
        for r in conn.execute(
            "SELECT * FROM station_commodities WHERE station_id = ? ORDER BY id", (sid,)
        ).fetchall()
    ]
    st["assignments"] = [
        row_to_dict(r)
        for r in conn.execute(
            "SELECT * FROM station_assignments WHERE station_id = ? ORDER BY pecking_order, id",
            (sid,),
        ).fetchall()
    ]
    return st


def list_stations(conn: sqlite3.Connection, city_id: str | None = None) -> list[dict[str, Any]]:
    if city_id:
        cur = conn.execute(
            "SELECT * FROM station_placards WHERE city_id = ? ORDER BY kind, name",
            (city_id,),
        )
    else:
        cur = conn.execute("SELECT * FROM station_placards ORDER BY city_id, kind, name")
    return [_enrich_station(conn, row_to_dict(r)) for r in cur.fetchall()]


def replace_stations(
    conn: sqlite3.Connection, city_id: str, placards: list[dict[str, Any]]
) -> list[dict[str, Any]]:
    # Delete commodities/assignments via cascade by deleting placards for city
    ids = [
        r[0]
        for r in conn.execute(
            "SELECT id FROM station_placards WHERE city_id = ?", (city_id,)
        ).fetchall()
    ]
    for pid in ids:
        conn.execute("DELETE FROM station_commodities WHERE station_id = ?", (pid,))
        conn.execute("DELETE FROM station_assignments WHERE station_id = ?", (pid,))
    conn.execute("DELETE FROM station_placards WHERE city_id = ?", (city_id,))
    for p in placards or []:
        kind = (p.get("kind") or "generic").lower()
        if kind not in STATION_KINDS:
            kind = "generic"
        conn.execute(
            """INSERT INTO station_placards
               (city_id, stable_id, name, kind, config_json, causality_leaf_id,
                building_stable_id, vehicle_id, parent_station_id, level_id, staffing_weight, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, datetime('now'))""",
            (
                city_id,
                p.get("stableId") or p.get("stable_id") or f"stn-{kind}",
                p.get("name") or "Station",
                kind,
                json.dumps(p.get("config") or p.get("config_json") or {}),
                p.get("causalityLeafId") or p.get("causality_leaf_id"),
                p.get("buildingStableId") or p.get("building_stable_id"),
                p.get("vehicleId") or p.get("vehicle_id"),
                p.get("parentStationId") or p.get("parent_station_id"),
                p.get("levelId") or p.get("level_id") or "",
                float(p.get("staffingWeight") or p.get("staffing_weight") or 1),
            ),
        )
        pid = conn.execute("SELECT last_insert_rowid()").fetchone()[0]
        for c in p.get("commodities") or []:
            conn.execute(
                """INSERT INTO station_commodities
                   (station_id, commodity_key, cron_expr, one_shot_at, surge_mult, quantity, price, availability)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    pid,
                    c.get("commodityKey") or c.get("commodity_key") or "labor",
                    c.get("cronExpr") or c.get("cron_expr"),
                    c.get("oneShotAt") or c.get("one_shot_at"),
                    float(c.get("surgeMult") or c.get("surge_mult") or 1),
                    float(c.get("quantity") or 1),
                    float(c.get("price") or 0),
                    1 if c.get("availability", True) else 0,
                ),
            )
        for a in p.get("assignments") or []:
            at = (a.get("assignType") or a.get("assign_type") or "persona").lower()
            if at not in ASSIGN_TYPES:
                at = "persona"
            conn.execute(
                """INSERT INTO station_assignments
                   (station_id, assign_type, ref_id, role, pecking_order)
                   VALUES (?, ?, ?, ?, ?)""",
                (
                    pid,
                    at,
                    a.get("refId") or a.get("ref_id") or "",
                    a.get("role") or "",
                    int(a.get("peckingOrder") or a.get("pecking_order") or 100),
                ),
            )
    conn.commit()
    return list_stations(conn, city_id)


def upsert_level_stats(
    conn: sqlite3.Connection,
    level_id: str,
    city_id: str,
    payload: dict[str, Any],
    placards: list[dict[str, Any]] | None = None,
) -> dict[str, Any]:
    if placards is not None:
        replace_stations(conn, city_id, placards)
    conn.execute(
        """INSERT INTO station_level_stats (level_id, city_id, payload_json, uploaded_at)
           VALUES (?, ?, ?, datetime('now'))
           ON CONFLICT(level_id, city_id) DO UPDATE SET
             payload_json = excluded.payload_json,
             uploaded_at = datetime('now')""",
        (level_id, city_id, json.dumps(payload or {})),
    )
    conn.commit()
    row = conn.execute(
        "SELECT * FROM station_level_stats WHERE level_id = ? AND city_id = ?",
        (level_id, city_id),
    ).fetchone()
    return row_to_dict(row)


def assemblage_graph(conn: sqlite3.Connection, city_id: str) -> dict[str, Any]:
    stations = list_stations(conn, city_id)
    nodes = []
    links = []
    buildings: set[str] = set()
    vehicles: set[str] = set()
    for st in stations:
        nid = f"station-{st['stable_id']}"
        nodes.append(
            {
                "id": nid,
                "label": st["name"],
                "kind": st["kind"],
                "nodeType": "station",
                "value": float(st.get("staffing_weight") or 1),
                "parent": st.get("parent_station_id") or st.get("building_stable_id") or "city",
            }
        )
        b = st.get("building_stable_id")
        if b:
            buildings.add(b)
            links.append({"source": nid, "target": f"building-{b}"})
        v = st.get("vehicle_id")
        if v:
            vehicles.add(v)
            links.append({"source": nid, "target": f"vehicle-{v}"})
        parent = st.get("parent_station_id")
        if parent:
            links.append({"source": nid, "target": f"station-{parent}"})
    for b in buildings:
        nodes.append(
            {"id": f"building-{b}", "label": b, "kind": "building", "nodeType": "building", "value": 2, "parent": "city"}
        )
    for v in vehicles:
        nodes.append(
            {"id": f"vehicle-{v}", "label": v, "kind": "vehicle", "nodeType": "vehicle", "value": 1.5, "parent": "city"}
        )
    nodes.append({"id": "city", "label": city_id, "kind": "city", "nodeType": "city", "value": 1, "parent": None})
    return {"nodes": nodes, "links": links}


def treemap_hierarchy(conn: sqlite3.Connection, city_id: str) -> dict[str, Any]:
    """Planet → city → building → station squarified-friendly hierarchy."""
    stations = list_stations(conn, city_id)
    by_building: dict[str, list[dict[str, Any]]] = {}
    for st in stations:
        b = st.get("building_stable_id") or "unassigned"
        by_building.setdefault(b, []).append(st)

    building_children = []
    for bldg, sts in by_building.items():
        children = [
            {
                "name": s["name"],
                "stableId": s["stable_id"],
                "kind": s["kind"],
                "value": max(0.1, float(s.get("staffing_weight") or 1)),
            }
            for s in sts
        ]
        building_children.append(
            {
                "name": bldg,
                "nodeType": "building",
                "value": sum(c["value"] for c in children) or 1,
                "children": children,
            }
        )

    return {
        "name": "gov",
        "children": [
            {
                "name": "planet",
                "children": [
                    {
                        "name": city_id or "city",
                        "nodeType": "city",
                        "children": building_children,
                    }
                ],
            }
        ],
    }
