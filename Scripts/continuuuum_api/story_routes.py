"""Story, work-order, narrative overlay, and resaurce sync routes."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuuuum_api.causality_work_order_validator import validate_work_orders
    from continuuuum_api.legal_collision import check_story_legal_collisions, has_critical_collision
    from continuuuum_api.production_validation import validate_budget_plan_id, validate_schedule_id
    from continuuuum_api.spatial_timeline import (
        compute_narrative_calendar_anchor,
        get_spatial_4d_timeline_origin,
    )
    from continuuuum_api.story_db import ensure_stories_schema
except ImportError:
    from causality_work_order_validator import validate_work_orders
    from legal_collision import check_story_legal_collisions, has_critical_collision
    from production_validation import validate_budget_plan_id, validate_schedule_id
    from spatial_timeline import compute_narrative_calendar_anchor, get_spatial_4d_timeline_origin
    from story_db import ensure_stories_schema

import sys
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))
import continuuuum_work_orders as causality_wo_gen

GetConn = Callable[[], sqlite3.Connection]

STORY_STATUSES = ["new", "grooming", "in_progress", "in_review", "submitted", "completed"]
STATUS_ORDER = {s: i for i, s in enumerate(STORY_STATUSES)}

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id(prefix: str = "story") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _row_story(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    d["buildErrors"] = json.loads(d.pop("build_errors_json") or "[]")
    return d


def _resaurce_route(route: str, payload: dict) -> dict:
    body = json.dumps({"route": f"resaurce:{route}", "payload": payload, "trace_id": _new_id("trace")}).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode())
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError) as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}


def _sync_story_chat(conn: sqlite3.Connection, story_id: str) -> str | None:
    row = conn.execute("SELECT summary FROM stories WHERE id = ?", (story_id,)).fetchone()
    if not row:
        return None
    assignees = [
        r["user_id"]
        for r in conn.execute("SELECT user_id FROM story_assignees WHERE story_id = ?", (story_id,)).fetchall()
    ]
    watchers = [
        r["user_id"]
        for r in conn.execute("SELECT user_id FROM story_watchers WHERE story_id = ?", (story_id,)).fetchall()
    ]
    out = _resaurce_route(
        "chat/room/ensure-for-story",
        {
            "story_id": story_id,
            "summary": row["summary"],
            "assignees": assignees,
            "watchers": watchers,
        },
    )
    room = out.get("chat_room") or {}
    room_id = room.get("id")
    if not room_id:
        try:
            from continuuuum_api.local_chat_store import ensure_story_room
        except ImportError:
            from local_chat_store import ensure_story_room
        room_id = ensure_story_room(conn, story_id, row["summary"], assignees, watchers)
    if room_id:
        conn.execute(
            "UPDATE stories SET resaurce_chat_room_id = ?, updated_at = ? WHERE id = ?",
            (room_id, _now(), story_id),
        )
        conn.commit()
    return room_id


def _append_story_comment(
    description: str | None,
    kind: str,
    text: str,
    *,
    user_id: str | None = None,
) -> str:
    stamp = _now()
    actor = (user_id or "system").strip() or "system"
    block = f"\n\n--- Story comment ({stamp}) — {kind} — {actor} ---\n{text.strip()}\n"
    return (description or "").rstrip() + block


def _user_id_from_request() -> str:
    return (request.headers.get("X-User-ID") or "anonymous").strip() or "anonymous"


def _story_members(conn: sqlite3.Connection, story_id: str) -> dict:
    assignees = [
        dict(r) for r in conn.execute("SELECT * FROM story_assignees WHERE story_id = ?", (story_id,)).fetchall()
    ]
    watchers = [
        dict(r) for r in conn.execute("SELECT * FROM story_watchers WHERE story_id = ?", (story_id,)).fetchall()
    ]
    wo_ids = [
        r["work_order_id"]
        for r in conn.execute("SELECT work_order_id FROM story_work_orders WHERE story_id = ?", (story_id,)).fetchall()
    ]
    return {"assignees": assignees, "watchers": watchers, "workOrderIds": wo_ids}


def _row_overlay(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    d["events"] = json.loads(d.pop("events_json") or "[]")
    d["narrativeStartOffsetDays"] = float(d.get("narrative_start_offset_days") or 0)
    d["resaurceScheduleId"] = d.get("resaurce_schedule_id")
    d["spatial4dEpisodeId"] = d.get("spatial_4d_episode_id")
    return d


def _schedule_start_from_resaurce(schedule_id: str | None) -> str | None:
    if not schedule_id:
        return None
    out = _resaurce_route("production/schedule/get", {"schedule_id": schedule_id})
    sched = out.get("production_schedule") or {}
    return sched.get("start_date") or sched.get("startDate")


def _overlay_context(conn: sqlite3.Connection, overlay: dict | None, schedule_id: str | None) -> dict[str, Any]:
    sid = schedule_id or (overlay or {}).get("resaurce_schedule_id") or (overlay or {}).get("resaurceScheduleId")
    ep_id = (overlay or {}).get("spatial_4d_episode_id") or (overlay or {}).get("spatial4dEpisodeId")
    spatial = get_spatial_4d_timeline_origin(conn, episode_id=ep_id, schedule_id=sid)
    sched_start = _schedule_start_from_resaurce(sid)
    offset = float((overlay or {}).get("narrative_start_offset_days") or (overlay or {}).get("narrativeStartOffsetDays") or 0)
    effective = compute_narrative_calendar_anchor(sched_start, offset)
    return {
        "spatial4d": spatial,
        "scheduleStartDate": sched_start,
        "narrativeStartOffsetDays": offset,
        "effectiveNarrativeStartDate": effective or (overlay or {}).get("custom_start_date"),
    }


def register_story_routes(app, get_conn: GetConn) -> None:
    @app.before_request
    def _ensure_story_schema():
        if not getattr(app, "_stories_ready", False):
            ensure_stories_schema(get_conn())
            app._stories_ready = True

    @app.route("/api/stories", methods=["GET", "POST"])
    def stories_collection():
        if request.method == "GET":
            conn = get_conn()
            where, params = ["1=1"], []
            for key, col in (
                ("tenant_id", "tenant_id"),
                ("status", "status"),
                ("scheduleId", "resaurce_schedule_id"),
                ("resaurce_schedule_id", "resaurce_schedule_id"),
                ("project_id", "project_id"),
            ):
                val = request.args.get(key)
                if val:
                    where.append(f"{col} = ?")
                    params.append(val)
            sql = f"SELECT * FROM stories WHERE {' AND '.join(where)} ORDER BY updated_at DESC LIMIT 200"
            rows = conn.execute(sql, params).fetchall()
            conn.close()
            return jsonify({"stories": [_row_story(r) for r in rows]})

        body = request.get_json(silent=True) or {}
        sched_id = body.get("resaurceScheduleId")
        budget_id = body.get("resaurceBudgetPlanId")
        sched_val = validate_schedule_id(sched_id)
        if not sched_val.get("ok"):
            return jsonify(sched_val), 400
        budget_val = validate_budget_plan_id(budget_id)
        if not budget_val.get("ok"):
            return jsonify(budget_val), 400
        sid = _new_id()
        now = _now()
        warnings = check_story_legal_collisions(conn := get_conn(), body.get("assetKind"), body.get("assetRefJson"))
        conn.execute(
            """INSERT INTO stories
               (id, tenant_id, project_id, resaurce_schedule_id, resaurce_budget_plan_id,
                external_provider, external_key, external_url, github_project_number,
                jira_project_key, jira_issue_type, summary, description, story_value, status,
                episode_id, narrative_t_start, narrative_t_end, calendar_start_date, calendar_end_date,
                build_errors_json, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (
                sid,
                body.get("tenantId") or "default",
                body.get("projectId"),
                body.get("resaurceScheduleId"),
                body.get("resaurceBudgetPlanId"),
                body.get("externalProvider") or "none",
                body.get("externalKey"),
                body.get("externalUrl"),
                body.get("githubProjectNumber"),
                body.get("jiraProjectKey"),
                body.get("jiraIssueType"),
                body.get("summary") or "Untitled story",
                body.get("description"),
                float(body.get("storyValue") or 0),
                "new",
                body.get("episodeId"),
                body.get("narrativeTStart"),
                body.get("narrativeTEnd"),
                body.get("calendarStartDate"),
                body.get("calendarEndDate"),
                "[]",
                now,
                now,
            ),
        )
        conn.commit()
        _sync_story_chat(conn, sid)
        conn.close()
        return jsonify({"id": sid, "legalCollisionWarnings": warnings}), 201

    @app.route("/api/stories/<story_id>", methods=["GET", "PATCH"])
    def story_detail(story_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM stories WHERE id = ?", (story_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404

        if request.method == "GET":
            data = _row_story(row)
            data.update(_story_members(conn, story_id))
            conn.close()
            return jsonify(data)

        body = request.get_json(silent=True) or {}
        if "resaurceScheduleId" in body:
            sched_val = validate_schedule_id(body.get("resaurceScheduleId"))
            if not sched_val.get("ok"):
                conn.close()
                return jsonify(sched_val), 400
        if "resaurceBudgetPlanId" in body:
            budget_val = validate_budget_plan_id(body.get("resaurceBudgetPlanId"))
            if not budget_val.get("ok"):
                conn.close()
                return jsonify(budget_val), 400
        current = dict(row)
        if current["status"] == "completed":
            conn.close()
            return jsonify({"error": "story_completed_no_reopen"}), 409

        new_status = body.get("status")
        if new_status:
            if STATUS_ORDER.get(new_status, -1) < STATUS_ORDER.get(current["status"], 0):
                conn.close()
                return jsonify({"error": "status_regression_not_allowed"}), 409
            if new_status in ("submitted", "completed"):
                wo_ids = [
                    r["work_order_id"]
                    for r in conn.execute(
                        "SELECT work_order_id FROM story_work_orders WHERE story_id = ?", (story_id,)
                    ).fetchall()
                ]
                validation = validate_work_orders(conn, work_order_ids=wo_ids)
                if not validation["ok"]:
                    conn.execute(
                        "UPDATE stories SET build_errors_json = ?, updated_at = ? WHERE id = ?",
                        (json.dumps(validation["buildErrors"]), _now(), story_id),
                    )
                    conn.commit()
                    conn.close()
                    return jsonify({"error": "causality_validation_failed", "buildErrors": validation["buildErrors"]}), 422
            if new_status == "in_progress":
                warnings = check_story_legal_collisions(
                    conn, body.get("assetKind"), body.get("assetRefJson")
                )
                if has_critical_collision(warnings):
                    conn.close()
                    return jsonify({"error": "legal_collision", "legalCollisionWarnings": warnings}), 422

        fields, params = [], []
        for key, col, transform in (
            ("summary", "summary", str),
            ("description", "description", str),
            ("storyValue", "story_value", float),
            ("status", "status", str),
            ("resaurceScheduleId", "resaurce_schedule_id", str),
            ("resaurceBudgetPlanId", "resaurce_budget_plan_id", str),
            ("calendarStartDate", "calendar_start_date", str),
            ("calendarEndDate", "calendar_end_date", str),
            ("narrativeTStart", "narrative_t_start", float),
            ("narrativeTEnd", "narrative_t_end", float),
            ("externalProvider", "external_provider", str),
            ("externalKey", "external_key", str),
            ("externalUrl", "external_url", str),
            ("jiraProjectKey", "jira_project_key", str),
            ("jiraIssueType", "jira_issue_type", str),
        ):
            if key in body and body[key] is not None:
                fields.append(f"{col} = ?")
                params.append(transform(body[key]))
        if "githubProjectNumber" in body and body["githubProjectNumber"] is not None:
            fields.append("github_project_number = ?")
            params.append(int(body["githubProjectNumber"]))

        if new_status == "completed":
            fields.extend(["completed_at = ?", "build_errors_json = ?"])
            params.extend([_now(), "[]"])

        if fields:
            fields.append("updated_at = ?")
            params.append(_now())
            params.append(story_id)
            conn.execute(f"UPDATE stories SET {', '.join(fields)} WHERE id = ?", params)
            conn.commit()

        updated = conn.execute("SELECT * FROM stories WHERE id = ?", (story_id,)).fetchone()
        conn.close()
        return jsonify(_row_story(updated))

    @app.route("/api/stories/<story_id>/assignees", methods=["POST", "DELETE"])
    def story_assignees(story_id: str):
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id")
        if not user_id:
            return jsonify({"error": "userId required"}), 400
        conn = get_conn()
        if request.method == "POST":
            conn.execute(
                "INSERT OR IGNORE INTO story_assignees (story_id, user_id, role, created_at) VALUES (?,?,?,?)",
                (story_id, user_id, body.get("role") or "assignee", _now()),
            )
            conn.commit()
        else:
            conn.execute("DELETE FROM story_assignees WHERE story_id = ? AND user_id = ?", (story_id, user_id))
            conn.commit()
        _sync_story_chat(conn, story_id)
        conn.close()
        return jsonify({"ok": True})

    @app.route("/api/stories/<story_id>/watchers", methods=["POST", "DELETE"])
    def story_watchers(story_id: str):
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id")
        if not user_id:
            return jsonify({"error": "userId required"}), 400
        conn = get_conn()
        if request.method == "POST":
            conn.execute(
                "INSERT OR IGNORE INTO story_watchers (story_id, user_id, created_at) VALUES (?,?,?)",
                (story_id, user_id, _now()),
            )
            conn.commit()
        else:
            conn.execute("DELETE FROM story_watchers WHERE story_id = ? AND user_id = ?", (story_id, user_id))
            conn.commit()
        _sync_story_chat(conn, story_id)
        conn.close()
        return jsonify({"ok": True})

    @app.route("/api/stories/<story_id>/work-orders", methods=["POST", "DELETE"])
    def story_link_work_orders(story_id: str):
        body = request.get_json(silent=True) or {}
        wo_id = body.get("workOrderId") or body.get("work_order_id")
        if not wo_id:
            return jsonify({"error": "workOrderId required"}), 400
        conn = get_conn()
        if request.method == "POST":
            conn.execute(
                "INSERT OR IGNORE INTO story_work_orders (story_id, work_order_id, created_at) VALUES (?,?,?)",
                (story_id, wo_id, _now()),
            )
            conn.execute("UPDATE work_orders SET story_id = ? WHERE id = ?", (story_id, wo_id))
            conn.commit()
        else:
            conn.execute(
                "DELETE FROM story_work_orders WHERE story_id = ? AND work_order_id = ?",
                (story_id, wo_id),
            )
            conn.commit()
        conn.close()
        return jsonify({"ok": True})

    @app.route("/api/stories/<story_id>/ensure-chat", methods=["POST"])
    def story_ensure_chat(story_id: str):
        conn = get_conn()
        row = conn.execute("SELECT id FROM stories WHERE id = ?", (story_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404
        room_id = _sync_story_chat(conn, story_id)
        conn.close()
        if not room_id:
            return jsonify({"ok": False, "error": "chat_room_create_failed"}), 500
        return jsonify({"ok": True, "chatRoomId": room_id})

    @app.route("/api/stories/<story_id>/validate-causality", methods=["POST"])
    def story_validate_causality(story_id: str):
        conn = get_conn()
        wo_ids = [
            r["work_order_id"]
            for r in conn.execute("SELECT work_order_id FROM story_work_orders WHERE story_id = ?", (story_id,)).fetchall()
        ]
        result = validate_work_orders(conn, work_order_ids=wo_ids)
        conn.execute(
            "UPDATE stories SET build_errors_json = ?, updated_at = ? WHERE id = ?",
            (json.dumps(result["buildErrors"]), _now(), story_id),
        )
        conn.commit()
        conn.close()
        status = 200 if result["ok"] else 422
        return jsonify(result), status

    @app.route("/api/stories/<story_id>/clone", methods=["POST"])
    def story_clone(story_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM stories WHERE id = ?", (story_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        source = dict(row)
        body = request.get_json(silent=True) or {}
        sched_id = body.get("resaurceScheduleId") if "resaurceScheduleId" in body else source.get("resaurce_schedule_id")
        budget_id = body.get("resaurceBudgetPlanId") if "resaurceBudgetPlanId" in body else source.get("resaurce_budget_plan_id")
        sched_val = validate_schedule_id(sched_id)
        if not sched_val.get("ok"):
            conn.close()
            return jsonify(sched_val), 400
        budget_val = validate_budget_plan_id(budget_id)
        if not budget_val.get("ok"):
            conn.close()
            return jsonify(budget_val), 400
        new_id = _new_id()
        now = _now()
        user_id = _user_id_from_request()
        summary = body.get("summary") or f"{source.get('summary') or source['id']} (copy)"
        comment = (
            f"Cloned from {source['id']}"
            f"{(' — ' + source['summary']) if source.get('summary') else ''}."
        )
        description = _append_story_comment(source.get("description"), "clone", comment, user_id=user_id)
        conn.execute(
            """INSERT INTO stories
               (id, tenant_id, project_id, resaurce_schedule_id, resaurce_budget_plan_id,
                external_provider, external_key, external_url, github_project_number,
                jira_project_key, jira_issue_type, summary, description, story_value, status,
                episode_id, narrative_t_start, narrative_t_end, calendar_start_date, calendar_end_date,
                build_errors_json, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (
                new_id,
                source.get("tenant_id") or "default",
                source.get("project_id"),
                sched_id,
                budget_id,
                source.get("external_provider") or "none",
                None,
                None,
                source.get("github_project_number"),
                source.get("jira_project_key"),
                source.get("jira_issue_type"),
                summary,
                description,
                float(source.get("story_value") or 0),
                "new",
                source.get("episode_id"),
                source.get("narrative_t_start"),
                source.get("narrative_t_end"),
                source.get("calendar_start_date"),
                source.get("calendar_end_date"),
                "[]",
                now,
                now,
            ),
        )
        conn.commit()
        _sync_story_chat(conn, new_id)
        created = conn.execute("SELECT * FROM stories WHERE id = ?", (new_id,)).fetchone()
        conn.close()
        return jsonify(_row_story(created)), 201

    @app.route("/api/stories/<story_id>/reopen", methods=["POST"])
    def story_reopen(story_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM stories WHERE id = ?", (story_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        current = dict(row)
        if current["status"] == "completed":
            conn.close()
            return jsonify({"error": "story_completed_no_reopen"}), 409
        if current["status"] != "submitted":
            conn.close()
            return jsonify({"error": "story_reopen_requires_submitted", "status": current["status"]}), 409
        body = request.get_json(silent=True) or {}
        user_id = _user_id_from_request()
        reason = (body.get("reason") or body.get("comment") or "").strip()
        comment = reason or f"Reopened from status `{current['status']}`."
        description = _append_story_comment(current.get("description"), "reopen", comment, user_id=user_id)
        now = _now()
        conn.execute(
            """UPDATE stories SET status = ?, description = ?, build_errors_json = ?,
               completed_at = NULL, updated_at = ? WHERE id = ?""",
            ("new", description, "[]", now, story_id),
        )
        conn.commit()
        updated = conn.execute("SELECT * FROM stories WHERE id = ?", (story_id,)).fetchone()
        conn.close()
        return jsonify(_row_story(updated))

    @app.route("/api/work-orders", methods=["GET", "POST"])
    @app.route("/api/work-orders/<wo_id>", methods=["GET", "PATCH"])
    def work_orders_api(wo_id: str | None = None):
        conn = get_conn()
        if request.method == "GET" and wo_id:
            row = conn.execute("SELECT * FROM work_orders WHERE id = ?", (wo_id,)).fetchone()
            conn.close()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(dict(row))

        if request.method == "GET":
            where, params = ["1=1"], []
            for arg, col in (
                ("episode_id", "episode_id"),
                ("story_id", "story_id"),
                ("status", "status"),
            ):
                v = request.args.get(arg)
                if v:
                    where.append(f"{col} = ?")
                    params.append(v)
            rows = conn.execute(
                f"SELECT * FROM work_orders WHERE {' AND '.join(where)} ORDER BY id LIMIT 500", params
            ).fetchall()
            conn.close()
            return jsonify({"workOrders": [dict(r) for r in rows]})

        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            wid = _new_id("wo")
            warnings = check_story_legal_collisions(
                conn, body.get("assetKind"), json.dumps(body.get("assetRef") or {}) if body.get("assetRef") else None
            )
            conn.execute(
                """INSERT INTO work_orders
                   (id, episode_id, story_id, causality_leaf_id, asset_id, narrative_type, depends_on,
                    prompt_description, status, assigned_to, work_order_source,
                    asset_kind, asset_ref_json, updated_at)
                   VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (
                    wid,
                    body.get("episodeId"),
                    body.get("storyId"),
                    body.get("causalityLeafId"),
                    body.get("assetId"),
                    body.get("narrativeType") or "linear",
                    json.dumps(body.get("dependsOn") or []),
                    body.get("promptDescription"),
                    body.get("status") or "pending",
                    body.get("assignedTo"),
                    body.get("workOrderSource") or "causality",
                    body.get("assetKind"),
                    json.dumps(body.get("assetRef") or {}) if body.get("assetRef") else None,
                    _now(),
                ),
            )
            if body.get("storyId"):
                conn.execute(
                    "INSERT OR IGNORE INTO story_work_orders (story_id, work_order_id, created_at) VALUES (?,?,?)",
                    (body["storyId"], wid, _now()),
                )
            conn.commit()
            conn.close()
            return jsonify({"id": wid, "legalCollisionWarnings": warnings}), 201

        body = request.get_json(silent=True) or {}
        fields, params = [], []
        for key, col in (
            ("status", "status"),
            ("assignedTo", "assigned_to"),
            ("promptDescription", "prompt_description"),
            ("assetKind", "asset_kind"),
        ):
            if key in body:
                fields.append(f"{col} = ?")
                params.append(body[key])
        if "assetRef" in body:
            fields.append("asset_ref_json = ?")
            params.append(json.dumps(body["assetRef"]))
        if fields:
            fields.append("updated_at = ?")
            params.append(_now())
            params.append(wo_id)
            conn.execute(f"UPDATE work_orders SET {', '.join(fields)} WHERE id = ?", params)
            conn.commit()
        row = conn.execute("SELECT * FROM work_orders WHERE id = ?", (wo_id,)).fetchone()
        conn.close()
        return jsonify(dict(row) if row else {})

    @app.route("/api/work-orders/<wo_id>/run-causality-test", methods=["POST"])
    def work_order_run_causality_test(wo_id: str):
        conn = get_conn()
        row = conn.execute("SELECT id, episode_id FROM work_orders WHERE id = ?", (wo_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404
        story_link = conn.execute(
            "SELECT story_id FROM story_work_orders WHERE work_order_id = ?", (wo_id,)
        ).fetchone()
        if story_link:
            wo_ids = [
                r["work_order_id"]
                for r in conn.execute(
                    "SELECT work_order_id FROM story_work_orders WHERE story_id = ?",
                    (story_link["story_id"],),
                ).fetchall()
            ]
        else:
            wo_ids = [wo_id]
        result = validate_work_orders(conn, work_order_ids=wo_ids)
        status_str = "pass" if result["ok"] else "fail"
        conn.execute(
            "UPDATE work_orders SET causality_test_status = ?, causality_test_log_json = ?, updated_at = ? WHERE id = ?",
            (status_str, json.dumps(result), _now(), wo_id),
        )
        conn.commit()
        conn.close()
        return jsonify({**result, "causalityTestStatus": status_str}), 200 if result["ok"] else 422

    @app.route("/api/episodes/<episode_id>/generate-causality-work-orders", methods=["POST"])
    def generate_causality_work_orders(episode_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ntd_rows = conn.execute(
            "SELECT asset_id, narrative_type FROM narrative_type_detections WHERE episode_id = ?",
            (episode_id,),
        ).fetchall()
        narrative_types = {r["asset_id"]: r["narrative_type"] for r in ntd_rows}
        cs = conn.execute(
            "SELECT procedural_cycle_ids FROM causality_structure WHERE episode_id = ? LIMIT 1",
            (episode_id,),
        ).fetchone()
        edges: list[tuple[str, str]] = []
        if cs and cs["procedural_cycle_ids"]:
            try:
                cycles = json.loads(cs["procedural_cycle_ids"])
                for c in cycles:
                    if isinstance(c, (list, tuple)) and len(c) >= 2:
                        edges.append((str(c[0]), str(c[1])))
            except json.JSONDecodeError:
                pass
        orders = causality_wo_gen.generate_work_orders(episode_id, narrative_types, edges)
        created = []
        for wo in orders:
            conn.execute(
                """INSERT OR REPLACE INTO work_orders
                   (id, episode_id, causality_leaf_id, asset_id, narrative_type, depends_on,
                    prompt_description, status, work_order_source, updated_at)
                   VALUES (?,?,?,?,?,?,?,?,?,?)""",
                (
                    wo["id"],
                    wo["episode_id"],
                    wo.get("causality_leaf_id"),
                    wo.get("asset_id"),
                    wo["narrative_type"],
                    wo.get("depends_on"),
                    wo.get("prompt_description"),
                    wo.get("status", "pending"),
                    "causality",
                    _now(),
                ),
            )
            created.append(wo["id"])
        if body.get("storyId"):
            for wid in created:
                conn.execute(
                    "INSERT OR IGNORE INTO story_work_orders (story_id, work_order_id, created_at) VALUES (?,?,?)",
                    (body["storyId"], wid, _now()),
                )
        conn.commit()
        conn.close()
        return jsonify({"workOrderIds": created, "count": len(created)})

    @app.route("/api/spatial-4d/timeline-origin", methods=["GET"])
    def spatial_4d_timeline_origin():
        conn = get_conn()
        episode_id = request.args.get("episode_id") or request.args.get("episodeId")
        schedule_id = request.args.get("schedule_id") or request.args.get("scheduleId")
        origin = get_spatial_4d_timeline_origin(conn, episode_id=episode_id, schedule_id=schedule_id)
        sched_start = _schedule_start_from_resaurce(schedule_id)
        origin["scheduleStartDate"] = sched_start
        conn.close()
        return jsonify(origin)

    @app.route("/api/narrative-timeline-overlay", methods=["GET", "POST"])
    @app.route("/api/narrative-timeline-overlay/<overlay_id>", methods=["GET", "PATCH"])
    def narrative_timeline_overlay(overlay_id: str | None = None):
        conn = get_conn()
        if request.method == "GET" and overlay_id:
            row = conn.execute("SELECT * FROM narrative_timeline_overlay WHERE id = ?", (overlay_id,)).fetchone()
            conn.close()
            if not row:
                return jsonify({"error": "not found"}), 404
            overlay = _row_overlay(row)
            return jsonify(overlay)

        if request.method == "GET":
            pid = request.args.get("project_id")
            sched_q = request.args.get("schedule_id") or request.args.get("resaurce_schedule_id")
            if sched_q:
                row = conn.execute(
                    """SELECT * FROM narrative_timeline_overlay
                       WHERE resaurce_schedule_id = ? ORDER BY updated_at DESC LIMIT 1""",
                    (sched_q,),
                ).fetchone()
                if not row:
                    row = conn.execute(
                        "SELECT * FROM narrative_timeline_overlay ORDER BY updated_at DESC LIMIT 1"
                    ).fetchone()
            elif pid:
                row = conn.execute(
                    "SELECT * FROM narrative_timeline_overlay WHERE project_id = ? ORDER BY updated_at DESC LIMIT 1",
                    (pid,),
                ).fetchone()
            else:
                row = conn.execute(
                    "SELECT * FROM narrative_timeline_overlay ORDER BY updated_at DESC LIMIT 1"
                ).fetchone()
            if not row:
                ctx = _overlay_context(conn, None, sched_q)
                conn.close()
                return jsonify({"overlay": None, "context": ctx})
            overlay = _row_overlay(row)
            ctx = _overlay_context(conn, overlay, sched_q or overlay.get("resaurce_schedule_id"))
            conn.close()
            return jsonify({"overlay": overlay, "context": ctx})

        body = request.get_json(silent=True) or {}
        now = _now()
        schedule_id = body.get("resaurceScheduleId") or body.get("resaurce_schedule_id")
        episode_id = body.get("spatial4dEpisodeId") or body.get("spatial_4d_episode_id")
        offset = float(body.get("narrativeStartOffsetDays") if body.get("narrativeStartOffsetDays") is not None else body.get("narrative_start_offset_days") or 0)
        sched_start = _schedule_start_from_resaurce(schedule_id)
        custom_start = compute_narrative_calendar_anchor(sched_start, offset) or body.get("customStartDate") or now[:10]

        if request.method == "POST":
            oid = _new_id("ntl")
            conn.execute(
                """INSERT INTO narrative_timeline_overlay
                   (id, project_id, resaurce_schedule_id, spatial_4d_episode_id,
                    custom_start_date, narrative_start_offset_days, scale_label, source, events_json, created_at, updated_at)
                   VALUES (?,?,?,?,?,?,?,?,?,?,?)""",
                (
                    oid,
                    body.get("projectId"),
                    schedule_id,
                    episode_id,
                    custom_start,
                    offset,
                    body.get("scaleLabel"),
                    body.get("source") or "spatial_4d_offset",
                    json.dumps(body.get("events") or []),
                    now,
                    now,
                ),
            )
            conn.commit()
            conn.close()
            return jsonify({"id": oid, "customStartDate": custom_start, "narrativeStartOffsetDays": offset}), 201

        conn.execute(
            """UPDATE narrative_timeline_overlay SET
               custom_start_date = COALESCE(?, custom_start_date),
               narrative_start_offset_days = COALESCE(?, narrative_start_offset_days),
               resaurce_schedule_id = COALESCE(?, resaurce_schedule_id),
               spatial_4d_episode_id = COALESCE(?, spatial_4d_episode_id),
               scale_label = COALESCE(?, scale_label),
               events_json = COALESCE(?, events_json),
               updated_at = ?
               WHERE id = ?""",
            (
                custom_start if schedule_id or body.get("narrativeStartOffsetDays") is not None else body.get("customStartDate"),
                offset if body.get("narrativeStartOffsetDays") is not None else None,
                schedule_id,
                episode_id,
                body.get("scaleLabel"),
                json.dumps(body["events"]) if "events" in body else None,
                now,
                overlay_id,
            ),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "customStartDate": custom_start})
