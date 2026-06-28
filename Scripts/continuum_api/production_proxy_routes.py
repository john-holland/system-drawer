"""Proxy resaurce production budget/schedule routes."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
import uuid

from flask import jsonify, request

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def _cave(route: str, payload: dict | None = None) -> tuple[dict, int]:
    trace = f"continuum_{uuid.uuid4().hex[:10]}"
    body = json.dumps({"route": f"resaurce:{route}", "payload": payload or {}, "trace_id": trace}).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            return json.loads(resp.read().decode()), resp.status
    except urllib.error.HTTPError as e:
        return json.loads(e.read().decode() or "{}"), e.code
    except urllib.error.URLError as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}, 502


def register_production_proxy_routes(app) -> None:
    @app.route("/api/production/budget", methods=["GET", "POST"])
    def production_budget():
        if request.method == "GET":
            out, status = _cave("production/budget/list", dict(request.args))
            return jsonify(out), status
        out, status = _cave("production/budget/create", request.get_json(silent=True) or {})
        return jsonify(out), status

    @app.route("/api/production/budget/<plan_id>", methods=["GET"])
    def production_budget_get(plan_id: str):
        out, status = _cave("production/budget/get", {"budget_plan_id": plan_id})
        return jsonify(out), status

    @app.route("/api/production/budget/<plan_id>/journal", methods=["GET", "POST"])
    def production_budget_journal(plan_id: str):
        if request.method == "GET":
            out, status = _cave("production/budget/journal/list", {"budget_plan_id": plan_id})
            return jsonify(out), status
        body = request.get_json(silent=True) or {}
        body["budget_plan_id"] = plan_id
        out, status = _cave("production/budget/journal/post", body)
        return jsonify(out), status

    @app.route("/api/production/budget/<plan_id>/water-level", methods=["GET"])
    def production_budget_water_level(plan_id: str):
        out, status = _cave("production/budget/water-level", {"budget_plan_id": plan_id})
        return jsonify(out), status

    @app.route("/api/production/budget/<plan_id>/allocate-story", methods=["POST"])
    def production_budget_allocate_story(plan_id: str):
        body = request.get_json(silent=True) or {}
        body["budget_plan_id"] = plan_id
        out, status = _cave("production/budget/allocate-story", body)
        return jsonify(out), status

    @app.route("/api/production/budget/<plan_id>/publish-sheets", methods=["POST"])
    def production_budget_publish_sheets(plan_id: str):
        try:
            from continuum_api.scripts.sheets_publish import publish_budget_plan
        except ImportError:
            from scripts.sheets_publish import publish_budget_plan
        result = publish_budget_plan(plan_id, request.get_json(silent=True) or {})
        status = 200 if result.get("ok") else 502
        _cave(
            "production/budget/sheets-publish-status",
            {"budget_plan_id": plan_id, "status": "ok" if result.get("ok") else "error"},
        )
        return jsonify(result), status

    @app.route("/api/production/schedules", methods=["GET", "POST"])
    def production_schedules():
        if request.method == "GET":
            out, status = _cave("production/schedule/list", {})
            return jsonify(out), status
        out, status = _cave("production/schedule/create", request.get_json(silent=True) or {})
        return jsonify(out), status

    @app.route("/api/production/schedules/<schedule_id>", methods=["GET"])
    def production_schedule_get(schedule_id: str):
        out, status = _cave("production/schedule/get", {"schedule_id": schedule_id})
        return jsonify(out), status

    @app.route("/api/production/schedules/<schedule_id>/milestones/<milestone_id>/stories", methods=["POST"])
    def production_milestone_stories(schedule_id: str, milestone_id: str):
        body = request.get_json(silent=True) or {}
        out, status = _cave(
            "production/schedule/milestone/stories",
            {
                "schedule_id": schedule_id,
                "milestone_id": milestone_id,
                "continuum_story_ids": body.get("continuumStoryIds") or body.get("story_ids") or [],
            },
        )
        return jsonify(out), status
