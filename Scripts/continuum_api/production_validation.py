"""Validate resaurce production schedule/budget references."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
import uuid
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


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


def validate_budget_plan_id(plan_id: str | None) -> dict[str, Any]:
    if not plan_id:
        return {"ok": True}
    out = _cave("production/budget/get", {"budget_plan_id": plan_id})
    if not out.get("ok") or not out.get("budget_plan"):
        return {"ok": False, "error": "invalid_budget_plan_id", "planId": plan_id}
    return {"ok": True, "budget_plan": out["budget_plan"]}


def validate_schedule_id(schedule_id: str | None) -> dict[str, Any]:
    if not schedule_id:
        return {"ok": True}
    out = _cave("production/schedule/get", {"schedule_id": schedule_id})
    if not out.get("ok") or not out.get("production_schedule"):
        return {"ok": False, "error": "invalid_schedule_id", "scheduleId": schedule_id}
    return {"ok": True, "production_schedule": out["production_schedule"]}
