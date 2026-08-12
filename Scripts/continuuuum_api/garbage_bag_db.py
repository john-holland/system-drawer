"""Garbage bags schema ensure + dim-aware helpers."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Optional

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent
RANDOM_BAG_ID = "random_garbage_bag"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_garbage_bag_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "garbage_bags"):
        sql = (_SCHEMA_ROOT / "continuuuum_garbage_bag_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    seed_default_bag(conn, dim=0)
    conn.commit()


def seed_default_bag(conn: sqlite3.Connection, dim: int = 0) -> None:
    now = _now()
    commodities = json.dumps(
        [
            {"key": "organic", "weight": 0.5},
            {"key": "plastic", "weight": 0.3},
            {"key": "paper", "weight": 0.2},
        ]
    )
    conn.execute(
        """
        INSERT INTO garbage_bags
          (id, dim, title, commodities_json, default_mass_kg, is_default, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, 1, ?, ?)
        ON CONFLICT(id, dim) DO NOTHING
        """,
        (RANDOM_BAG_ID, int(dim), "Random Garbage Bag", commodities, 8.0, now, now),
    )


def _row_to_bag(r: sqlite3.Row) -> dict[str, Any]:
    commodities: Any = []
    raw = r["commodities_json"]
    if raw:
        try:
            commodities = json.loads(raw)
        except (TypeError, json.JSONDecodeError):
            commodities = []
    return {
        "id": r["id"],
        "dim": int(r["dim"]),
        "title": r["title"],
        "commodities": commodities,
        "defaultMassKg": float(r["default_mass_kg"] or 8.0),
        "isDefault": bool(r["is_default"]),
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def get_bag(conn: sqlite3.Connection, bag_id: str, dim: int) -> Optional[dict[str, Any]]:
    ensure_garbage_bag_schema(conn)
    r = conn.execute(
        "SELECT * FROM garbage_bags WHERE id = ? AND dim = ?",
        (bag_id, int(dim)),
    ).fetchone()
    if r:
        return _row_to_bag(r)
    if int(dim) != 0:
        r0 = conn.execute(
            "SELECT * FROM garbage_bags WHERE id = ? AND dim = 0",
            (bag_id,),
        ).fetchone()
        if r0:
            bag = _row_to_bag(r0)
            bag["dim"] = int(dim)
            bag["resolvedFromDim0"] = True
            return bag
    return None


def list_bags(conn: sqlite3.Connection, dim: int) -> list[dict[str, Any]]:
    ensure_garbage_bag_schema(conn)
    dim = int(dim)
    # Prefer landing-dim rows; fall back to dim-0 ids not overridden at landing dim.
    rows = conn.execute(
        """
        SELECT * FROM garbage_bags
        WHERE dim = ?
           OR (dim = 0 AND id NOT IN (SELECT id FROM garbage_bags WHERE dim = ?))
        ORDER BY is_default DESC, title COLLATE NOCASE
        """,
        (dim, dim),
    ).fetchall()
    out: list[dict[str, Any]] = []
    for r in rows:
        bag = _row_to_bag(r)
        if int(r["dim"]) == 0 and dim != 0:
            bag["dim"] = dim
            bag["resolvedFromDim0"] = True
        out.append(bag)
    return out


def ensure_dim0_bag(
    conn: sqlite3.Connection,
    bag_id: str,
    *,
    title: str = "Garbage Bag",
    commodities: Any = None,
    default_mass_kg: float = 8.0,
    copy_from: Optional[dict[str, Any]] = None,
) -> None:
    """Ensure a dim-0 existence row for bag_id."""
    ensure_garbage_bag_schema(conn)
    existing = conn.execute(
        "SELECT 1 FROM garbage_bags WHERE id = ? AND dim = 0 LIMIT 1",
        (bag_id,),
    ).fetchone()
    if existing:
        return
    now = _now()
    if copy_from:
        title = copy_from.get("title") or title
        commodities = copy_from.get("commodities", commodities)
        default_mass_kg = float(copy_from.get("defaultMassKg") or default_mass_kg)
    if commodities is None:
        commodities = [{"key": "mixed", "weight": 1.0}]
    conn.execute(
        """
        INSERT INTO garbage_bags
          (id, dim, title, commodities_json, default_mass_kg, is_default, created_at, updated_at)
        VALUES (?, 0, ?, ?, ?, 0, ?, ?)
        """,
        (
            bag_id,
            title,
            json.dumps(commodities),
            float(default_mass_kg),
            now,
            now,
        ),
    )


def upsert_bag(
    conn: sqlite3.Connection,
    bag_id: str,
    dim: int,
    *,
    title: str,
    commodities: Any,
    default_mass_kg: float = 8.0,
    is_default: bool = False,
) -> dict[str, Any]:
    ensure_garbage_bag_schema(conn)
    dim = int(dim)
    now = _now()
    if dim != 0:
        ensure_dim0_bag(
            conn,
            bag_id,
            title=title,
            commodities=commodities,
            default_mass_kg=default_mass_kg,
        )
    conn.execute(
        """
        INSERT INTO garbage_bags
          (id, dim, title, commodities_json, default_mass_kg, is_default, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(id, dim) DO UPDATE SET
          title = excluded.title,
          commodities_json = excluded.commodities_json,
          default_mass_kg = excluded.default_mass_kg,
          updated_at = excluded.updated_at
        """,
        (
            bag_id,
            dim,
            title,
            json.dumps(commodities if commodities is not None else []),
            float(default_mass_kg),
            1 if is_default else 0,
            now,
            now,
        ),
    )
    conn.commit()
    bag = get_bag(conn, bag_id, dim)
    assert bag is not None
    return bag


def patch_bag(
    conn: sqlite3.Connection, bag_id: str, dim: int, body: dict[str, Any]
) -> Optional[dict[str, Any]]:
    ensure_garbage_bag_schema(conn)
    dim = int(dim)
    current = conn.execute(
        "SELECT * FROM garbage_bags WHERE id = ? AND dim = ?",
        (bag_id, dim),
    ).fetchone()
    if not current and dim != 0:
        # Clone dim-0 into landing dim then patch.
        base = get_bag(conn, bag_id, 0)
        if not base:
            return None
        ensure_dim0_bag(conn, bag_id, copy_from=base)
        upsert_bag(
            conn,
            bag_id,
            dim,
            title=base["title"],
            commodities=base["commodities"],
            default_mass_kg=base["defaultMassKg"],
            is_default=False,
        )
        current = conn.execute(
            "SELECT * FROM garbage_bags WHERE id = ? AND dim = ?",
            (bag_id, dim),
        ).fetchone()
    if not current:
        return None
    bag = _row_to_bag(current)
    if "title" in body and bag_id != RANDOM_BAG_ID:
        bag["title"] = (body.get("title") or bag["title"]).strip() or bag["title"]
    if "commodities" in body:
        bag["commodities"] = body["commodities"] or bag["commodities"]
    if "defaultMassKg" in body:
        bag["defaultMassKg"] = float(body["defaultMassKg"])
    return upsert_bag(
        conn,
        bag_id,
        dim,
        title=bag["title"],
        commodities=bag["commodities"],
        default_mass_kg=bag["defaultMassKg"],
        is_default=bag["isDefault"],
    )


def delete_bag(conn: sqlite3.Connection, bag_id: str, dim: int) -> bool:
    ensure_garbage_bag_schema(conn)
    if bag_id == RANDOM_BAG_ID and int(dim) == 0:
        return False
    cur = conn.execute(
        "DELETE FROM garbage_bags WHERE id = ? AND dim = ?",
        (bag_id, int(dim)),
    )
    conn.commit()
    return cur.rowcount > 0
