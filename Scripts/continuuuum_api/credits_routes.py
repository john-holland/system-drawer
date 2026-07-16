"""Credits lists API: CRUD, update-list from work orders / Resaurce HR, warehouse history."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuuuum_api.credits_db import (
        ensure_credits_schema,
        entry_is_visible,
        new_id,
        warehouse_append,
        _now,
    )
except ImportError:
    from credits_db import (
        ensure_credits_schema,
        entry_is_visible,
        new_id,
        warehouse_append,
        _now,
    )

GetConn = Callable[[], sqlite3.Connection]
RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def _bool_int(v: Any, default: int = 0) -> int:
    if v is None:
        return default
    if isinstance(v, bool):
        return 1 if v else 0
    if isinstance(v, (int, float)):
        return 1 if int(v) else 0
    s = str(v).strip().lower()
    if s in ("1", "true", "yes", "on"):
        return 1
    if s in ("0", "false", "no", "off"):
        return 0
    return default


def _parse_images(v: Any) -> str:
    if v is None:
        return "[]"
    if isinstance(v, str):
        try:
            json.loads(v)
            return v
        except json.JSONDecodeError:
            return json.dumps([v] if v else [])
    if isinstance(v, list):
        return json.dumps(v)
    return "[]"


def _row_section(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    return {
        "id": d["id"],
        "listId": d["list_id"],
        "title": d["title"],
        "sortOrder": d["sort_order"],
        "scrollSpeed": d["scroll_speed"],
        "isSpecialUi": bool(d["is_special_ui"]),
        "quadrantPath": d["quadrant_path"],
        "createdAt": d.get("created_at"),
        "updatedAt": d.get("updated_at"),
    }


def _row_entry(row: sqlite3.Row, include_hidden: bool = True) -> dict[str, Any] | None:
    d = dict(row)
    show_full = bool(d["show_full_name"])
    show_nick = bool(d["show_nickname"])
    visible = entry_is_visible(show_full, show_nick)
    if not include_hidden and not visible:
        return None
    try:
        images = json.loads(d.get("images_json") or "[]")
    except json.JSONDecodeError:
        images = []
    return {
        "id": d["id"],
        "listId": d["list_id"],
        "sectionId": d["section_id"],
        "fullName": d.get("full_name") or "",
        "nickName": d.get("nick_name") or "",
        "showNickname": show_nick,
        "showFullName": show_full,
        "visible": visible,
        "sortOrder": d["sort_order"],
        "quote": d.get("quote") or "",
        "images": images,
        "company": d.get("company") or "",
        "rightsMarks": d.get("rights_marks") or "",
        "years": d.get("years") or "",
        "scrollSpeed": d.get("scroll_speed"),
        "sourceUserId": d.get("source_user_id"),
        "sourceKind": d.get("source_kind") or "manual",
        "createdAt": d.get("created_at"),
        "updatedAt": d.get("updated_at"),
    }


def _row_list(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    return {
        "id": d["id"],
        "tenantId": d["tenant_id"],
        "title": d["title"],
        "episodeId": d.get("episode_id"),
        "createdAt": d["created_at"],
        "updatedAt": d["updated_at"],
    }


def _load_full_list(conn: sqlite3.Connection, list_id: str, include_hidden: bool) -> dict[str, Any] | None:
    row = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
    if row is None:
        return None
    out = _row_list(row)
    sections = [
        _row_section(r)
        for r in conn.execute(
            "SELECT * FROM credits_sections WHERE list_id = ? ORDER BY sort_order, title",
            (list_id,),
        ).fetchall()
    ]
    entries_out: list[dict[str, Any]] = []
    for r in conn.execute(
        "SELECT * FROM credits_entries WHERE list_id = ? ORDER BY sort_order, full_name",
        (list_id,),
    ).fetchall():
        e = _row_entry(r, include_hidden=include_hidden)
        if e is not None:
            entries_out.append(e)
    out["sections"] = sections
    out["entries"] = entries_out
    return out


def _ensure_default_section(conn: sqlite3.Connection, list_id: str, title: str = "Cast & Crew") -> str:
    existing = conn.execute(
        "SELECT id FROM credits_sections WHERE list_id = ? ORDER BY sort_order LIMIT 1",
        (list_id,),
    ).fetchone()
    if existing:
        return existing["id"]
    sid = new_id("csec")
    now = _now()
    conn.execute(
        """INSERT INTO credits_sections
           (id, list_id, title, sort_order, scroll_speed, is_special_ui, quadrant_path, created_at, updated_at)
           VALUES (?,?,?,?,?,?,?,?,?)""",
        (sid, list_id, title, 0, 40.0, 0, "R.0", now, now),
    )
    return sid


def _fetch_hr_employees() -> list[dict[str, Any]]:
    trace = f"credits_{uuid.uuid4().hex[:10]}"
    body = json.dumps(
        {
            "route": "resaurce:hr/employees/available",
            "payload": {},
            "trace_id": trace,
        }
    ).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            data = json.loads(resp.read().decode())
            return list(data.get("employees") or [])
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError, TimeoutError):
        # Offline fallback matching Resaurce mock HR for local/dev.
        return [
            {"id": "hr_001", "name": "Sarah Johnson", "email": "sarah.johnson@company.com"},
            {"id": "hr_002", "name": "Michael Chen", "email": "michael.chen@company.com"},
            {"id": "hr_003", "name": "Emily Rodriguez", "email": "emily.rodriguez@company.com"},
        ]


def _hr_by_id() -> dict[str, dict[str, Any]]:
    return {str(e.get("id")): e for e in _fetch_hr_employees() if e.get("id")}


def _collect_work_order_assignees(conn: sqlite3.Connection, episode_id: str | None) -> list[str]:
    ids: set[str] = set()
    if episode_id:
        rows = conn.execute(
            """SELECT DISTINCT assigned_to FROM work_orders
               WHERE episode_id = ? AND assigned_to IS NOT NULL AND TRIM(assigned_to) != ''""",
            (episode_id,),
        ).fetchall()
    else:
        rows = conn.execute(
            """SELECT DISTINCT assigned_to FROM work_orders
               WHERE assigned_to IS NOT NULL AND TRIM(assigned_to) != ''"""
        ).fetchall()
    for r in rows:
        ids.add(str(r["assigned_to"]).strip())

    # story_assignees via stories on episode or story_work_orders
    try:
        if episode_id:
            sa = conn.execute(
                """SELECT DISTINCT sa.user_id
                   FROM story_assignees sa
                   JOIN stories s ON s.id = sa.story_id
                   WHERE s.episode_id = ? AND sa.user_id IS NOT NULL AND TRIM(sa.user_id) != ''""",
                (episode_id,),
            ).fetchall()
        else:
            sa = conn.execute(
                """SELECT DISTINCT user_id FROM story_assignees
                   WHERE user_id IS NOT NULL AND TRIM(user_id) != ''"""
            ).fetchall()
        for r in sa:
            ids.add(str(r["user_id"]).strip())
    except sqlite3.OperationalError:
        pass
    return sorted(ids)


def _upsert_entry(
    conn: sqlite3.Connection,
    *,
    list_id: str,
    section_id: str,
    source_user_id: str,
    source_kind: str,
    full_name: str,
    company: str = "",
    force_reset_flags: bool = False,
) -> str:
    """Returns 'added' | 'updated' | 'skipped'."""
    existing = conn.execute(
        "SELECT * FROM credits_entries WHERE list_id = ? AND source_user_id = ?",
        (list_id, source_user_id),
    ).fetchone()
    now = _now()
    if existing is None:
        max_ord = conn.execute(
            "SELECT COALESCE(MAX(sort_order), -1) AS m FROM credits_entries WHERE section_id = ?",
            (section_id,),
        ).fetchone()["m"]
        eid = new_id("cent")
        conn.execute(
            """INSERT INTO credits_entries
               (id, list_id, section_id, full_name, nick_name, show_nickname, show_full_name,
                sort_order, quote, images_json, company, rights_marks, years, scroll_speed,
                source_user_id, source_kind, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (
                eid,
                list_id,
                section_id,
                full_name,
                "",
                0,
                1,
                int(max_ord) + 1,
                "",
                "[]",
                company,
                "",
                "",
                None,
                source_user_id,
                source_kind,
                now,
                now,
            ),
        )
        return "added"

    if force_reset_flags:
        conn.execute(
            """UPDATE credits_entries
               SET full_name = ?, company = COALESCE(NULLIF(?, ''), company),
                   show_full_name = 1, show_nickname = 0, source_kind = ?, updated_at = ?
               WHERE id = ?""",
            (full_name, company, source_kind, now, existing["id"]),
        )
    else:
        # Refresh name/company from source; keep quote/images/flags
        conn.execute(
            """UPDATE credits_entries
               SET full_name = CASE WHEN TRIM(full_name) = '' OR full_name = source_user_id THEN ? ELSE full_name END,
                   company = CASE WHEN TRIM(company) = '' AND ? != '' THEN ? ELSE company END,
                   source_kind = ?, updated_at = ?
               WHERE id = ?""",
            (full_name, company, company, source_kind, now, existing["id"]),
        )
    return "updated"


def register_credits_routes(app, get_conn: GetConn) -> None:
    @app.before_request
    def _ensure_credits():
        if getattr(app, "_credits_ready", False):
            return
        if request.path.startswith("/api/credits"):
            conn = get_conn()
            ensure_credits_schema(conn)
            conn.close()
            app._credits_ready = True

    @app.route("/api/credits/lists", methods=["GET"])
    def credits_lists_get():
        tenant = request.args.get("tenantId") or request.args.get("tenant_id") or "default"
        conn = get_conn()
        ensure_credits_schema(conn)
        rows = conn.execute(
            "SELECT * FROM credits_lists WHERE tenant_id = ? ORDER BY updated_at DESC",
            (tenant,),
        ).fetchall()
        conn.close()
        return jsonify({"lists": [_row_list(r) for r in rows]})

    @app.route("/api/credits/lists", methods=["POST"])
    def credits_lists_post():
        body = request.get_json(silent=True) or {}
        title = (body.get("title") or "Credits").strip() or "Credits"
        tenant = body.get("tenantId") or body.get("tenant_id") or "default"
        episode_id = body.get("episodeId") or body.get("episode_id")
        lid = new_id("clist")
        now = _now()
        conn = get_conn()
        ensure_credits_schema(conn)
        conn.execute(
            """INSERT INTO credits_lists (id, tenant_id, title, episode_id, created_at, updated_at)
               VALUES (?,?,?,?,?,?)""",
            (lid, tenant, title, episode_id, now, now),
        )
        _ensure_default_section(conn, lid, body.get("defaultSectionTitle") or "Cast & Crew")
        warehouse_append(
            conn,
            tenant_id=tenant,
            list_id=lid,
            event_kind="create_list",
            source=body.get("source") or "web",
            actor_user_id=body.get("actorUserId"),
            payload={"title": title, "episodeId": episode_id},
        )
        conn.commit()
        out = _load_full_list(conn, lid, include_hidden=True)
        conn.close()
        return jsonify(out), 201

    @app.route("/api/credits/lists/<list_id>", methods=["GET"])
    def credits_list_get(list_id: str):
        include_hidden = request.args.get("includeHidden", "0") in ("1", "true", "yes")
        conn = get_conn()
        ensure_credits_schema(conn)
        out = _load_full_list(conn, list_id, include_hidden=include_hidden)
        conn.close()
        if out is None:
            return jsonify({"error": "not_found"}), 404
        return jsonify(out)

    @app.route("/api/credits/lists/<list_id>", methods=["PATCH"])
    def credits_list_patch(list_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        row = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
        if row is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        title = body.get("title", row["title"])
        episode_id = body["episode_id"] if "episode_id" in body else (
            body["episodeId"] if "episodeId" in body else row["episode_id"]
        )
        now = _now()
        conn.execute(
            "UPDATE credits_lists SET title = ?, episode_id = ?, updated_at = ? WHERE id = ?",
            (title, episode_id, now, list_id),
        )
        warehouse_append(
            conn,
            tenant_id=row["tenant_id"],
            list_id=list_id,
            event_kind="patch_list",
            source=body.get("source") or "web",
            actor_user_id=body.get("actorUserId"),
            payload={"title": title, "episodeId": episode_id},
        )
        conn.commit()
        out = _load_full_list(conn, list_id, include_hidden=True)
        conn.close()
        return jsonify(out)

    @app.route("/api/credits/lists/<list_id>", methods=["DELETE"])
    def credits_list_delete(list_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        row = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
        if row is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        warehouse_append(
            conn,
            tenant_id=row["tenant_id"],
            list_id=list_id,
            event_kind="delete_list",
            source=body.get("source") or "web",
            actor_user_id=body.get("actorUserId"),
            payload={"title": row["title"]},
        )
        conn.execute("DELETE FROM credits_entries WHERE list_id = ?", (list_id,))
        conn.execute("DELETE FROM credits_sections WHERE list_id = ?", (list_id,))
        conn.execute("DELETE FROM credits_lists WHERE id = ?", (list_id,))
        conn.commit()
        conn.close()
        return jsonify({"ok": True})

    @app.route("/api/credits/lists/<list_id>/sections", methods=["POST"])
    def credits_section_post(list_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        lst = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
        if lst is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        max_ord = conn.execute(
            "SELECT COALESCE(MAX(sort_order), -1) AS m FROM credits_sections WHERE list_id = ?",
            (list_id,),
        ).fetchone()["m"]
        sid = new_id("csec")
        now = _now()
        qpath = body.get("quadrantPath") or body.get("quadrant_path") or f"R.{int(max_ord) + 1}"
        conn.execute(
            """INSERT INTO credits_sections
               (id, list_id, title, sort_order, scroll_speed, is_special_ui, quadrant_path, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?,?)""",
            (
                sid,
                list_id,
                (body.get("title") or "Section").strip() or "Section",
                int(body.get("sortOrder", int(max_ord) + 1)),
                float(body.get("scrollSpeed", 40)),
                _bool_int(body.get("isSpecialUi") or body.get("is_special_ui")),
                qpath,
                now,
                now,
            ),
        )
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"],
            list_id=list_id,
            event_kind="create_section",
            source=body.get("source") or "web",
            payload={"sectionId": sid, "title": body.get("title")},
        )
        conn.commit()
        row = conn.execute("SELECT * FROM credits_sections WHERE id = ?", (sid,)).fetchone()
        conn.close()
        return jsonify(_row_section(row)), 201

    @app.route("/api/credits/sections/<section_id>", methods=["PATCH"])
    def credits_section_patch(section_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        row = conn.execute("SELECT * FROM credits_sections WHERE id = ?", (section_id,)).fetchone()
        if row is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        title = body.get("title", row["title"])
        sort_order = int(body.get("sortOrder", row["sort_order"]))
        scroll_speed = float(body.get("scrollSpeed", row["scroll_speed"]))
        is_special = _bool_int(
            body.get("isSpecialUi") if "isSpecialUi" in body or "is_special_ui" in body else row["is_special_ui"],
            int(row["is_special_ui"]),
        )
        qpath = body.get("quadrantPath") or body.get("quadrant_path") or row["quadrant_path"]
        now = _now()
        conn.execute(
            """UPDATE credits_sections
               SET title=?, sort_order=?, scroll_speed=?, is_special_ui=?, quadrant_path=?, updated_at=?
               WHERE id=?""",
            (title, sort_order, scroll_speed, is_special, qpath, now, section_id),
        )
        lst = conn.execute("SELECT tenant_id FROM credits_lists WHERE id = ?", (row["list_id"],)).fetchone()
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"] if lst else "default",
            list_id=row["list_id"],
            event_kind="patch_section",
            source=body.get("source") or "web",
            payload={"sectionId": section_id, "scrollSpeed": scroll_speed},
        )
        conn.commit()
        updated = conn.execute("SELECT * FROM credits_sections WHERE id = ?", (section_id,)).fetchone()
        conn.close()
        return jsonify(_row_section(updated))

    @app.route("/api/credits/lists/<list_id>/entries", methods=["POST"])
    def credits_entry_post(list_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        lst = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
        if lst is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        section_id = body.get("sectionId") or body.get("section_id")
        if not section_id:
            section_id = _ensure_default_section(conn, list_id)
        max_ord = conn.execute(
            "SELECT COALESCE(MAX(sort_order), -1) AS m FROM credits_entries WHERE section_id = ?",
            (section_id,),
        ).fetchone()["m"]
        eid = new_id("cent")
        now = _now()
        show_full = _bool_int(body.get("showFullName"), 1)
        show_nick = _bool_int(body.get("showNickname"), 0)
        raw_speed = body.get("scrollSpeed", body.get("scroll_speed"))
        if raw_speed is None or raw_speed == "":
            scroll_speed = None
        else:
            try:
                scroll_speed = float(raw_speed)
            except (TypeError, ValueError):
                conn.close()
                return jsonify({"error": "invalid_scroll_speed"}), 400
        conn.execute(
            """INSERT INTO credits_entries
               (id, list_id, section_id, full_name, nick_name, show_nickname, show_full_name,
                sort_order, quote, images_json, company, rights_marks, years, scroll_speed,
                source_user_id, source_kind, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
            (
                eid,
                list_id,
                section_id,
                body.get("fullName") or body.get("full_name") or "",
                body.get("nickName") or body.get("nick_name") or "",
                show_nick,
                show_full,
                int(body.get("sortOrder", int(max_ord) + 1)),
                body.get("quote") or "",
                _parse_images(body.get("images")),
                body.get("company") or "",
                body.get("rightsMarks") or body.get("rights_marks") or "",
                body.get("years") or "",
                scroll_speed,
                body.get("sourceUserId") or body.get("source_user_id"),
                body.get("sourceKind") or "manual",
                now,
                now,
            ),
        )
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"],
            list_id=list_id,
            event_kind="create_entry",
            source=body.get("source") or "web",
            payload={"entryId": eid, "fullName": body.get("fullName")},
        )
        conn.commit()
        row = conn.execute("SELECT * FROM credits_entries WHERE id = ?", (eid,)).fetchone()
        conn.close()
        return jsonify(_row_entry(row, include_hidden=True)), 201

    @app.route("/api/credits/entries/<entry_id>", methods=["PATCH"])
    def credits_entry_patch(entry_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        row = conn.execute("SELECT * FROM credits_entries WHERE id = ?", (entry_id,)).fetchone()
        if row is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        before_vis = entry_is_visible(row["show_full_name"], row["show_nickname"])
        show_full = _bool_int(
            body.get("showFullName") if "showFullName" in body or "show_full_name" in body else row["show_full_name"],
            int(row["show_full_name"]),
        )
        show_nick = _bool_int(
            body.get("showNickname") if "showNickname" in body or "show_nickname" in body else row["show_nickname"],
            int(row["show_nickname"]),
        )
        now = _now()
        if "scrollSpeed" in body or "scroll_speed" in body:
            raw_speed = body["scrollSpeed"] if "scrollSpeed" in body else body.get("scroll_speed")
            if raw_speed is None or raw_speed == "":
                scroll = None
            else:
                try:
                    scroll = float(raw_speed)
                except (TypeError, ValueError):
                    conn.close()
                    return jsonify({"error": "invalid_scroll_speed"}), 400
        else:
            scroll = row["scroll_speed"]
        source_kind = (
            body.get("sourceKind")
            if "sourceKind" in body or "source_kind" in body
            else row["source_kind"]
        ) or "manual"
        if source_kind not in ("manual", "work_order", "hr"):
            source_kind = "manual"
        conn.execute(
            """UPDATE credits_entries SET
               full_name=?, nick_name=?, show_nickname=?, show_full_name=?,
               sort_order=?, quote=?, images_json=?, company=?, rights_marks=?, years=?,
               scroll_speed=?, section_id=?, source_kind=?, updated_at=?
               WHERE id=?""",
            (
                body.get("fullName") if "fullName" in body else row["full_name"],
                body.get("nickName") if "nickName" in body else row["nick_name"],
                show_nick,
                show_full,
                int(body.get("sortOrder", row["sort_order"])),
                body.get("quote") if "quote" in body else row["quote"],
                _parse_images(body.get("images")) if "images" in body else row["images_json"],
                body.get("company") if "company" in body else row["company"],
                body.get("rightsMarks") if "rightsMarks" in body else row["rights_marks"],
                body.get("years") if "years" in body else row["years"],
                scroll,
                body.get("sectionId") if "sectionId" in body else row["section_id"],
                source_kind,
                now,
                entry_id,
            ),
        )
        after_vis = entry_is_visible(show_full, show_nick)
        lst = conn.execute("SELECT tenant_id FROM credits_lists WHERE id = ?", (row["list_id"],)).fetchone()
        event = "visibility_change" if before_vis != after_vis else "patch_entry"
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"] if lst else "default",
            list_id=row["list_id"],
            event_kind=event,
            source=body.get("source") or "web",
            payload={
                "entryId": entry_id,
                "showFullName": bool(show_full),
                "showNickname": bool(show_nick),
                "visible": after_vis,
                "scrollSpeed": scroll,
            },
        )
        conn.commit()
        updated = conn.execute("SELECT * FROM credits_entries WHERE id = ?", (entry_id,)).fetchone()
        conn.close()
        return jsonify(_row_entry(updated, include_hidden=True))

    @app.route("/api/credits/entries/<entry_id>", methods=["DELETE"])
    def credits_entry_delete(entry_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_credits_schema(conn)
        row = conn.execute("SELECT * FROM credits_entries WHERE id = ?", (entry_id,)).fetchone()
        if row is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404
        lst = conn.execute("SELECT tenant_id FROM credits_lists WHERE id = ?", (row["list_id"],)).fetchone()
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"] if lst else "default",
            list_id=row["list_id"],
            event_kind="delete_entry",
            source=body.get("source") or "web",
            payload={"entryId": entry_id, "fullName": row["full_name"]},
        )
        conn.execute("DELETE FROM credits_entries WHERE id = ?", (entry_id,))
        conn.commit()
        conn.close()
        return jsonify({"ok": True})

    def _update_list_impl(list_id: str, body: dict[str, Any]):
        conn = get_conn()
        ensure_credits_schema(conn)
        lst = conn.execute("SELECT * FROM credits_lists WHERE id = ?", (list_id,)).fetchone()
        if lst is None:
            conn.close()
            return jsonify({"error": "not_found"}), 404

        mode = (body.get("mode") or "work_orders").strip().lower()
        if mode not in ("work_orders", "hr", "both"):
            conn.close()
            return jsonify({"error": "invalid_mode", "allowed": ["work_orders", "hr", "both"]}), 400

        episode_id = body.get("episodeId") or body.get("episode_id") or lst["episode_id"]
        section_id = body.get("sectionId") or body.get("section_id")
        if not section_id:
            section_id = _ensure_default_section(
                conn, list_id, body.get("defaultSectionTitle") or "Cast & Crew"
            )
        force = bool(body.get("forceResetFlags"))
        hr_map = _hr_by_id()
        added = updated = skipped = 0

        if mode in ("work_orders", "both"):
            for uid in _collect_work_order_assignees(conn, episode_id):
                hr = hr_map.get(uid) or {}
                name = str(hr.get("name") or uid)
                company = str(hr.get("company") or "")
                result = _upsert_entry(
                    conn,
                    list_id=list_id,
                    section_id=section_id,
                    source_user_id=uid,
                    source_kind="work_order",
                    full_name=name,
                    company=company,
                    force_reset_flags=force,
                )
                if result == "added":
                    added += 1
                elif result == "updated":
                    updated += 1
                else:
                    skipped += 1

        if mode in ("hr", "both"):
            for emp_id, emp in hr_map.items():
                result = _upsert_entry(
                    conn,
                    list_id=list_id,
                    section_id=section_id,
                    source_user_id=emp_id,
                    source_kind="hr",
                    full_name=str(emp.get("name") or emp_id),
                    company=str(emp.get("company") or ""),
                    force_reset_flags=force,
                )
                if result == "added":
                    added += 1
                elif result == "updated":
                    updated += 1
                else:
                    skipped += 1

        if episode_id and episode_id != lst["episode_id"]:
            conn.execute(
                "UPDATE credits_lists SET episode_id = ?, updated_at = ? WHERE id = ?",
                (episode_id, _now(), list_id),
            )
        else:
            conn.execute(
                "UPDATE credits_lists SET updated_at = ? WHERE id = ?",
                (_now(), list_id),
            )

        summary = {
            "mode": mode,
            "episodeId": episode_id,
            "added": added,
            "updated": updated,
            "skipped": skipped,
        }
        warehouse_append(
            conn,
            tenant_id=lst["tenant_id"],
            list_id=list_id,
            event_kind="update_list",
            source="work_orders" if mode == "work_orders" else ("hr" if mode == "hr" else "both"),
            actor_user_id=body.get("actorUserId"),
            payload=summary,
        )
        conn.commit()
        out = _load_full_list(conn, list_id, include_hidden=True)
        conn.close()
        out["updateSummary"] = summary
        return jsonify(out)

    @app.route("/api/credits/lists/<list_id>/update-list", methods=["POST"])
    def credits_update_list(list_id: str):
        return _update_list_impl(list_id, request.get_json(silent=True) or {})

    @app.route("/api/credits/lists/<list_id>/import-work-orders", methods=["POST"])
    def credits_import_work_orders(list_id: str):
        body = request.get_json(silent=True) or {}
        body["mode"] = "work_orders"
        return _update_list_impl(list_id, body)

    @app.route("/api/credits/lists/<list_id>/history", methods=["GET"])
    def credits_history(list_id: str):
        limit = min(int(request.args.get("limit") or 100), 500)
        conn = get_conn()
        ensure_credits_schema(conn)
        rows = conn.execute(
            """SELECT * FROM credits_warehouse_history
               WHERE list_id = ? ORDER BY created_at DESC LIMIT ?""",
            (list_id, limit),
        ).fetchall()
        conn.close()
        events = []
        for r in rows:
            d = dict(r)
            try:
                payload = json.loads(d.get("payload_json") or "{}")
            except json.JSONDecodeError:
                payload = {}
            events.append(
                {
                    "id": d["id"],
                    "tenantId": d["tenant_id"],
                    "listId": d["list_id"],
                    "eventKind": d["event_kind"],
                    "source": d["source"],
                    "actorUserId": d.get("actor_user_id"),
                    "payload": payload,
                    "createdAt": d["created_at"],
                }
            )
        return jsonify({"events": events})
