"""Spatial node listing for quest map editor."""

from __future__ import annotations

import json
import sqlite3
from typing import Any

try:
    from continuuuum_api.lemma_composition_spatial import _aabb_contains, _spatial_aabb
except ImportError:
    from lemma_composition_spatial import _aabb_contains, _spatial_aabb


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    row = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    ).fetchone()
    return bool(row)


def _table_columns(conn: sqlite3.Connection, table: str) -> set[str]:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return {row[1] for row in cur.fetchall()}


def _spatial_row_aabb(row: sqlite3.Row) -> dict[str, float]:
    cols = set(row.keys())
    if "t_min" in cols and "center_x" in cols:
        return _spatial_aabb(row)
    if "bounds4_json" in cols and row["bounds4_json"]:
        try:
            data = json.loads(row["bounds4_json"])
            return {
                "xMin": float(data.get("xMin", 0)),
                "xMax": float(data.get("xMax", 0)),
                "yMin": float(data.get("yMin", 0)),
                "yMax": float(data.get("yMax", 0)),
                "zMin": float(data.get("zMin", 0)),
                "zMax": float(data.get("zMax", 0)),
                "tMin": float(data.get("tMin", 0)),
                "tMax": float(data.get("tMax", 3600)),
            }
        except (json.JSONDecodeError, TypeError, ValueError):
            pass
    return {
        "xMin": -1.0, "xMax": 1.0, "yMin": -1.0, "yMax": 1.0, "zMin": -1.0, "zMax": 1.0,
        "tMin": 0.0, "tMax": 3600.0,
    }


def _bounds3d_from_aabb(aabb: dict[str, float]) -> dict[str, float]:
    return {k: aabb[k] for k in ("xMin", "xMax", "yMin", "yMax", "zMin", "zMax")}


def _query_bounds(spatial_id: str | None, mode: str, narrative_t: float | None) -> dict[str, float] | None:
    del spatial_id, mode, narrative_t
    return None


def list_spatial_nodes(
    conn: sqlite3.Connection,
    *,
    spatial4d_id: str | None = None,
    mode: str = "4d",
    narrative_t: float | None = None,
    project_id: str | None = None,
) -> dict[str, Any]:
    """Return merged DB composition nodes intersecting query bounds."""
    nodes: list[dict[str, Any]] = []
    query_aabb: dict[str, float] | None = None

    if spatial4d_id and _table_exists(conn, "spatial_4d"):
        row = conn.execute("SELECT * FROM spatial_4d WHERE id = ?", (spatial4d_id,)).fetchone()
        if row:
            query_aabb = _spatial_row_aabb(row)

    if _table_exists(conn, "thesaurus_entry_compositions"):
        cols = _table_columns(conn, "thesaurus_entry_compositions")
        spatial_col = "spatial_4d_id" if "spatial_4d_id" in cols else None
        if spatial_col:
            rows = conn.execute(
                f"""SELECT c.*, e.term AS entry_term
                    FROM thesaurus_entry_compositions c
                    LEFT JOIN thesaurus_entries e ON e.id = c.entry_id
                    ORDER BY c.sort_order"""
            ).fetchall()
            for row in rows:
                sid = row[spatial_col] if spatial_col in row.keys() else None
                if not sid:
                    continue
                srow = conn.execute("SELECT * FROM spatial_4d WHERE id = ?", (sid,)).fetchone() if _table_exists(conn, "spatial_4d") else None
                if not srow:
                    continue
                aabb = _spatial_row_aabb(srow)
                if query_aabb and not _aabb_contains(query_aabb, aabb):
                    continue
                if narrative_t is not None and not (aabb["tMin"] <= narrative_t <= aabb["tMax"]):
                    continue
                entry_id = row["entry_id"] if "entry_id" in row.keys() else row["child_entry_id"]
                nodes.append(
                    {
                        "id": entry_id,
                        "label": row["entry_term"] or entry_id,
                        "source": "db_composition",
                        "spatial4dId": sid,
                        "bounds3d": _bounds3d_from_aabb(aabb),
                        "bounds4d": aabb if mode == "4d" else None,
                        "gateway": None,
                        "questObjectiveIds": [],
                    }
                )

    if _table_exists(conn, "quest_objectives") and spatial4d_id:
        obj_rows = conn.execute(
            "SELECT objective_id, spatial_4d_id FROM quest_objectives WHERE spatial_4d_id = ?",
            (spatial4d_id,),
        ).fetchall()
        obj_by_spatial: dict[str, list[str]] = {}
        for r in obj_rows:
            obj_by_spatial.setdefault(r["spatial_4d_id"], []).append(r["objective_id"])
        for n in nodes:
            sid = n.get("spatial4dId")
            if sid and sid in obj_by_spatial:
                n["questObjectiveIds"] = obj_by_spatial[sid]

    return {
        "ok": True,
        "mode": mode,
        "spatial4dId": spatial4d_id,
        "narrativeT": narrative_t,
        "projectId": project_id,
        "nodes": nodes,
    }
