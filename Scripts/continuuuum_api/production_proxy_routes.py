"""Proxy resaurce production budget/schedule routes with local SQLite fallback."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone
from typing import Callable

from flask import Response, jsonify, request

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
GetConn = Callable[[], sqlite3.Connection]

try:
    from continuuuum_api.local_production_store import (
        create_schedule as local_create_schedule,
        get_schedule as local_get_schedule,
        list_schedules as local_list_schedules,
        update_milestone_stories as local_update_milestone_stories,
    )
except ImportError:
    from local_production_store import (
        create_schedule as local_create_schedule,
        get_schedule as local_get_schedule,
        list_schedules as local_list_schedules,
        update_milestone_stories as local_update_milestone_stories,
    )


def _cave(route: str, payload: dict | None = None) -> tuple[dict, int]:
    trace = f"continuuuum_{uuid.uuid4().hex[:10]}"
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


def _use_local(out: dict, status: int) -> bool:
    return status == 502 or out.get("error") == "resaurce_unavailable"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_story_id() -> str:
    return f"story_{uuid.uuid4().hex[:12]}"


def _create_story(
    conn: sqlite3.Connection,
    *,
    summary: str,
    schedule_id: str | None,
    budget_id: str | None,
    episode_id: str | None,
    calendar_start: str | None,
    calendar_end: str | None,
) -> str:
    sid = _new_story_id()
    now = _now()
    conn.execute(
        """INSERT INTO stories
           (id, tenant_id, project_id, resaurce_schedule_id, resaurce_budget_plan_id,
            external_provider, summary, description, story_value, status,
            episode_id, calendar_start_date, calendar_end_date,
            build_errors_json, created_at, updated_at)
           VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
        (
            sid,
            "default",
            None,
            schedule_id,
            budget_id,
            "none",
            summary,
            None,
            1.0,
            "new",
            episode_id,
            calendar_start,
            calendar_end,
            "[]",
            now,
            now,
        ),
    )
    conn.commit()
    return sid


def register_production_proxy_routes(app, get_conn: GetConn) -> None:
    @app.route("/api/production/budget", methods=["GET", "POST"])
    def production_budget():
        if request.method == "GET":
            out, status = _cave("production/budget/list", dict(request.args))
            if _use_local(out, status):
                conn = get_conn()
                rows = conn.execute(
                    """SELECT DISTINCT resaurce_budget_plan_id AS id FROM stories
                       WHERE resaurce_budget_plan_id IS NOT NULL AND TRIM(resaurce_budget_plan_id) != ''"""
                ).fetchall()
                conn.close()
                plans = [{"id": r["id"], "name": r["id"]} for r in rows]
                return jsonify({"ok": True, "budget_plans": plans, "warning": "resaurce_unavailable"}), 200
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

    @app.route("/api/production/budget/template", methods=["GET"])
    def production_budget_template():
        try:
            from continuuuum_api.scripts.sheets_publish import budget_template_payload
        except ImportError:
            from scripts.sheets_publish import budget_template_payload
        payload = budget_template_payload()
        download = request.args.get("download", "1").lower() not in ("0", "false", "no")
        if download:
            body = json.dumps(payload, indent=2)
            return Response(
                body,
                mimetype="application/json",
                headers={
                    "Content-Disposition": 'attachment; filename="continuuuum-budget-template.json"',
                },
            )
        return jsonify(payload), 200

    @app.route("/api/production/budget/<plan_id>/publish-sheets", methods=["POST"])
    def production_budget_publish_sheets(plan_id: str):
        try:
            from continuuuum_api.scripts.sheets_publish import publish_budget_plan
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
            if _use_local(out, status):
                conn = get_conn()
                schedules = local_list_schedules(conn)
                conn.close()
                return jsonify({"ok": True, "production_schedules": schedules}), 200
            return jsonify(out), status
        body = request.get_json(silent=True) or {}
        out, status = _cave("production/schedule/create", body)
        if _use_local(out, status):
            conn = get_conn()
            schedule = local_create_schedule(conn, body)
            conn.close()
            return jsonify({"ok": True, "production_schedule": schedule}), 201
        return jsonify(out), status

    @app.route("/api/production/schedules/create-with-stories", methods=["POST"])
    def production_schedules_create_with_stories():
        body = request.get_json(silent=True) or {}
        milestones_in = body.get("milestones") or []
        schedule_payload = {
            "name": body.get("name"),
            "start_date": body.get("startDate") or body.get("start_date"),
            "end_date": body.get("endDate") or body.get("end_date"),
            "timezone": body.get("timezone") or "UTC",
            "budget_plan_id": body.get("budgetPlanId") or body.get("budget_plan_id"),
            "episode_ids": body.get("episodeIds") or body.get("episode_ids") or [],
            "draft_episode_ids": body.get("draftEpisodeIds") or body.get("draft_episode_ids") or [],
            "milestones": [],
        }
        for m in milestones_in:
            schedule_payload["milestones"].append({
                "label": m.get("label"),
                "start_date": m.get("startDate") or m.get("start_date"),
                "end_date": m.get("endDate") or m.get("end_date"),
            })

        out, status = _cave("production/schedule/create", schedule_payload)
        use_local = _use_local(out, status)
        conn = get_conn()
        try:
            from continuuuum_api.story_db import ensure_stories_schema
        except ImportError:
            from story_db import ensure_stories_schema
        ensure_stories_schema(conn)
        if use_local:
            schedule = local_create_schedule(conn, schedule_payload)
        else:
            schedule = out.get("production_schedule") or {}
            if not schedule.get("id"):
                conn.close()
                return jsonify(out), status

        schedule_id = schedule.get("id")
        budget_id = schedule.get("budget_plan_id") or schedule_payload.get("budget_plan_id")
        episode_ids = schedule_payload["episode_ids"]
        default_episode = episode_ids[0] if episode_ids else None
        created_stories: list[dict] = []

        milestones = schedule.get("milestones") or []
        for idx, m_in in enumerate(milestones_in):
            m_row = milestones[idx] if idx < len(milestones) else None
            if not m_row:
                continue
            if not m_in.get("createStory"):
                continue
            story_id = _create_story(
                conn,
                summary=m_in.get("label") or m_row.get("label") or "Milestone story",
                schedule_id=schedule_id,
                budget_id=budget_id,
                episode_id=m_in.get("episodeId") or default_episode,
                calendar_start=m_row.get("start_date") or m_in.get("startDate") or m_in.get("start_date"),
                calendar_end=m_row.get("end_date") or m_in.get("endDate") or m_in.get("end_date"),
            )
            story_ids = list(m_row.get("continuuuum_story_ids") or [])
            story_ids.append(story_id)
            if use_local:
                local_update_milestone_stories(conn, m_row["id"], story_ids)
            else:
                _cave(
                    "production/schedule/milestone/stories",
                    {
                        "schedule_id": schedule_id,
                        "milestone_id": m_row["id"],
                        "continuuuum_story_ids": story_ids,
                    },
                )
            created_stories.append({"milestoneId": m_row["id"], "storyId": story_id})

        if use_local:
            schedule = local_get_schedule(conn, schedule_id) or schedule
        conn.close()
        return jsonify({
            "ok": True,
            "production_schedule": schedule,
            "createdStories": created_stories,
        }), 201

    @app.route("/api/production/schedules/<schedule_id>", methods=["GET"])
    def production_schedule_get(schedule_id: str):
        out, status = _cave("production/schedule/get", {"schedule_id": schedule_id})
        if _use_local(out, status):
            conn = get_conn()
            schedule = local_get_schedule(conn, schedule_id)
            conn.close()
            if not schedule:
                return jsonify({"ok": False, "error": "not_found"}), 404
            return jsonify({"ok": True, "production_schedule": schedule}), 200
        return jsonify(out), status

    @app.route("/api/production/schedules/<schedule_id>/milestones/<milestone_id>/stories", methods=["POST"])
    def production_milestone_stories(schedule_id: str, milestone_id: str):
        body = request.get_json(silent=True) or {}
        story_ids = body.get("continuuuumStoryIds") or body.get("story_ids") or []
        out, status = _cave(
            "production/schedule/milestone/stories",
            {
                "schedule_id": schedule_id,
                "milestone_id": milestone_id,
                "continuuuum_story_ids": story_ids,
            },
        )
        if _use_local(out, status):
            conn = get_conn()
            ms = local_update_milestone_stories(conn, milestone_id, story_ids)
            conn.close()
            if not ms:
                return jsonify({"ok": False, "error": "not_found"}), 404
            return jsonify({"ok": True, "milestone": ms}), 200
        return jsonify(out), status
