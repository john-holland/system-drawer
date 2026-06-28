"""Building flywheel — budget ledger and lattice merge weights."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def server_client_weight(confidence: float, timeout_order: int) -> float:
    return max(0.05, min(1.0, confidence)) * (1.0 / (1 + timeout_order * 0.1))


def merge_cell(local: float, remote: float, weight: float) -> float:
    return local * (1 - weight) + remote * weight


def record_budget_line(
    conn: sqlite3.Connection,
    city_id: str,
    stable_id: str | None,
    line_item: str,
    amount_usd: float,
    tick_index: int | None = None,
) -> None:
    conn.execute(
        """INSERT INTO building_budget_ledger (id, city_id, stable_id, line_item, amount_usd, tick_index, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?)""",
        (str(uuid.uuid4()), city_id, stable_id, line_item, amount_usd, tick_index, _now()),
    )


def flywheel_tick(conn: sqlite3.Connection, city_id: str, tick_index: int, zoning: dict) -> dict[str, Any]:
    total_opex = 0.0
    cur = conn.execute("SELECT stable_id, opex_usd, property_class FROM building_registry WHERE city_id = ?", (city_id,))
    rows = cur.fetchall()
    for row in rows:
        opex = float(row["opex_usd"] or 0)
        total_opex += opex
        tax = opex * 0.02
        record_budget_line(conn, city_id, row["stable_id"], "property_tax", tax, tick_index)
    for alloc in zoning.get("allocations") or []:
        record_budget_line(
            conn,
            city_id,
            None,
            f"zone_{alloc.get('zoneId')}_services",
            float(alloc.get("budgetShareUsd") or 0),
            tick_index,
        )
    return {"cityId": city_id, "tickIndex": tick_index, "totalOpexUsd": total_opex}
