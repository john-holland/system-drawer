"""Validate resaurce production schedule/budget references."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from pathlib import Path
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def _default_db_path() -> Path:
    scripts = Path(__file__).resolve().parents[1]
    drawer = Path(__file__).resolve().parents[2]
    for candidate in (
        os.environ.get("CONTINUUUUM_DB"),
        os.environ.get("CONTINUUUUM_DB_PATH"),
        str(drawer / "continuuuum.db"),
        str(scripts / "continuuuum.db"),
    ):
        if candidate:
            return Path(candidate)
    return scripts / "continuuuum.db"


def _cave(route: str, payload: dict) -> dict[str, Any]:
    body = json.dumps(
        {"route": f"resaurce:{route}", "payload": payload, "trace_id": f"val_{uuid.uuid4().hex[:10]}"}
    ).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return json.loads(resp.read().decode())
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError) as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}


def _local_schedule_exists(schedule_id: str) -> bool:
    try:
        conn = sqlite3.connect(_default_db_path())
        try:
            from continuuuum_api.local_production_store import get_schedule as local_get_schedule
        except ImportError:
            from local_production_store import get_schedule as local_get_schedule
        if local_get_schedule(conn, schedule_id):
            conn.close()
            return True
        row = conn.execute(
            "SELECT 1 FROM stories WHERE resaurce_schedule_id = ? LIMIT 1", (schedule_id,)
        ).fetchone()
        conn.close()
        return row is not None
    except sqlite3.Error:
        return False


def _local_budget_exists(plan_id: str) -> bool:
    try:
        conn = sqlite3.connect(_default_db_path())
        row = conn.execute(
            "SELECT 1 FROM stories WHERE resaurce_budget_plan_id = ? LIMIT 1", (plan_id,)
        ).fetchone()
        conn.close()
        return row is not None
    except sqlite3.Error:
        return False


def validate_budget_plan_id(plan_id: str | None) -> dict[str, Any]:
    if not plan_id:
        return {"ok": True}
    out = _cave("production/budget/get", {"budget_plan_id": plan_id})
    if out.get("ok") and out.get("budget_plan"):
        return {"ok": True, "budget_plan": out["budget_plan"]}
    if out.get("error") == "resaurce_unavailable" and _local_budget_exists(plan_id):
        return {"ok": True, "budget_plan": {"id": plan_id}}
    return {"ok": False, "error": "invalid_budget_plan_id", "planId": plan_id}


def validate_schedule_id(schedule_id: str | None) -> dict[str, Any]:
    if not schedule_id:
        return {"ok": True}
    out = _cave("production/schedule/get", {"schedule_id": schedule_id})
    if out.get("ok") and out.get("production_schedule"):
        return {"ok": True, "production_schedule": out["production_schedule"]}
    if out.get("error") == "resaurce_unavailable" and _local_schedule_exists(schedule_id):
        return {"ok": True, "production_schedule": {"id": schedule_id}}
    return {"ok": False, "error": "invalid_schedule_id", "scheduleId": schedule_id}
