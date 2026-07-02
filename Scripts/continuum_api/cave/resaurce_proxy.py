"""Proxy envelope v2 routes to resaurce Cave."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
SAURCE_CAVE_URL = os.environ.get("SAURCE_CAVE_URL", "http://127.0.0.1:3457").rstrip("/")


def _default_db_path() -> Path:
    scripts = Path(__file__).resolve().parents[2]
    drawer = Path(__file__).resolve().parents[3]
    for candidate in (
        os.environ.get("CONTINUUM_DB"),
        os.environ.get("CONTINUUM_DB_PATH"),
        str(drawer / "continuum.db"),
        str(scripts / "continuum.db"),
    ):
        if candidate:
            return Path(candidate)
    return scripts / "continuum.db"


def _local_route_fallback(structural: str, payload: dict[str, Any] | None = None) -> dict[str, Any] | None:
    """Dev-friendly local responses when resaurce Cave is offline."""
    if structural == "production/schedule/list":
        schedules: list[dict[str, Any]] = []
        try:
            conn = sqlite3.connect(_default_db_path())
            conn.row_factory = sqlite3.Row
            try:
                from continuum_api.local_production_store import list_schedules as local_list_schedules
            except ImportError:
                from local_production_store import list_schedules as local_list_schedules
            schedules = local_list_schedules(conn)
            conn.close()
        except sqlite3.Error:
            pass
        if not schedules:
            try:
                conn = sqlite3.connect(_default_db_path())
                conn.row_factory = sqlite3.Row
                rows = conn.execute(
                    """SELECT DISTINCT resaurce_schedule_id AS id FROM stories
                       WHERE resaurce_schedule_id IS NOT NULL AND TRIM(resaurce_schedule_id) != ''"""
                ).fetchall()
                schedules = [{"id": r["id"], "name": r["id"]} for r in rows]
                conn.close()
            except sqlite3.Error:
                pass
        return {
            "ok": True,
            "production_schedules": schedules,
            "warning": "resaurce_unavailable",
        }
    if structural == "production/schedule/create":
        try:
            conn = sqlite3.connect(_default_db_path())
            conn.row_factory = sqlite3.Row
            try:
                from continuum_api.local_production_store import create_schedule as local_create_schedule
            except ImportError:
                from local_production_store import create_schedule as local_create_schedule
            schedule = local_create_schedule(conn, payload or {})
            conn.close()
            return {"ok": True, "production_schedule": schedule, "warning": "resaurce_unavailable"}
        except sqlite3.Error as e:
            return {"ok": False, "error": "local_schedule_failed", "detail": str(e)}
    if structural == "production/schedule/get":
        schedule_id = (payload or {}).get("schedule_id") or (payload or {}).get("id")
        try:
            conn = sqlite3.connect(_default_db_path())
            try:
                from continuum_api.local_production_store import get_schedule as local_get_schedule
            except ImportError:
                from local_production_store import get_schedule as local_get_schedule
            schedule = local_get_schedule(conn, str(schedule_id or ""))
            conn.close()
            if schedule:
                return {"ok": True, "production_schedule": schedule, "warning": "resaurce_unavailable"}
        except sqlite3.Error:
            pass
        return {"ok": False, "error": "not_found"}
    if structural == "production/schedule/milestone/stories":
        milestone_id = (payload or {}).get("milestone_id")
        story_ids = (payload or {}).get("continuum_story_ids") or (payload or {}).get("story_ids") or []
        try:
            conn = sqlite3.connect(_default_db_path())
            try:
                from continuum_api.local_production_store import update_milestone_stories
            except ImportError:
                from local_production_store import update_milestone_stories
            ms = update_milestone_stories(conn, str(milestone_id or ""), list(story_ids))
            conn.close()
            if ms:
                return {"ok": True, "milestone": ms, "warning": "resaurce_unavailable"}
        except sqlite3.Error:
            pass
        return {"ok": False, "error": "not_found"}
    if structural == "production/budget/list":
        plans: list[dict[str, str]] = []
        try:
            conn = sqlite3.connect(_default_db_path())
            conn.row_factory = sqlite3.Row
            rows = conn.execute(
                """SELECT DISTINCT resaurce_budget_plan_id AS id FROM stories
                   WHERE resaurce_budget_plan_id IS NOT NULL AND TRIM(resaurce_budget_plan_id) != ''"""
            ).fetchall()
            plans = [{"id": r["id"], "name": r["id"]} for r in rows]
            conn.close()
        except sqlite3.Error:
            pass
        return {
            "ok": True,
            "budget_plans": plans,
            "warning": "resaurce_unavailable",
        }
    return None


def proxy_cave_route(
    service: str,
    structural: str,
    payload: dict[str, Any] | None,
    trace_id: str,
    *,
    schema_version: str = "2.0",
) -> dict[str, Any]:
    base = RESAURCE_CAVE_URL if service == "resaurce" else SAURCE_CAVE_URL
    body = json.dumps(
        {
            "schema_version": schema_version,
            "route": f"{service}:{structural}",
            "payload": payload or {},
            "trace_id": trace_id,
            "reply_mode": "sync_http",
        }
    ).encode()
    req = urllib.request.Request(
        f"{base}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        try:
            return json.loads(e.read().decode() or "{}")
        except json.JSONDecodeError:
            return {"ok": False, "error": "upstream_http_error", "status": e.code}
    except urllib.error.URLError:
        fallback = _local_route_fallback(structural, payload)
        if fallback is not None:
            return fallback
        return {"ok": False, "error": "upstream_unavailable"}
