"""Mayor Dog Mods API routes."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuuuum_api.mod_db import (
        build_bootstrap_manifest,
        delete_moddable_target,
        ensure_mayor_dog_mods_schema,
        list_moddable_targets,
        slugify_slot_key,
        sync_episode_mod_slots,
        sync_lemma_mod_slots,
        upsert_moddable_target,
    )
except ImportError:
    from mod_db import (
        build_bootstrap_manifest,
        delete_moddable_target,
        ensure_mayor_dog_mods_schema,
        list_moddable_targets,
        slugify_slot_key,
        sync_episode_mod_slots,
        sync_lemma_mod_slots,
        upsert_moddable_target,
    )

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id(prefix: str = "mdmod") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _require_presence(user_id: str) -> tuple[dict[str, Any], int] | None:
    if not user_id or user_id == "anonymous":
        return {"error": "user presence required", "code": "presence_required"}, 401
    return None


def _is_admin_request() -> bool:
    return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")


def _get_mod_detail(conn: sqlite3.Connection, mod_id: str) -> dict[str, Any] | None:
    ensure_mayor_dog_mods_schema(conn)
    mod = conn.execute("SELECT * FROM mayor_dog_mods WHERE id = ?", (mod_id,)).fetchone()
    if not mod:
        return None
    pkg = conn.execute(
        """SELECT * FROM mod_packages
           WHERE mod_id = ? AND status = 'published'
           ORDER BY published_at DESC LIMIT 1""",
        (mod_id,),
    ).fetchone()
    lemma_overrides: list[dict[str, Any]] = []
    episode_overrides: list[dict[str, Any]] = []
    package = None
    if pkg:
        package = {
            "id": pkg["id"],
            "version": pkg["version"],
            "status": pkg["status"],
            "publishedAt": pkg["published_at"],
        }
        for r in conn.execute(
            """SELECT lo.*, t.slot_key, t.label, t.target_kind
               FROM mod_lemma_overrides lo
               LEFT JOIN moddable_targets t ON t.id = lo.target_id
               WHERE lo.package_id = ?
               ORDER BY t.slot_key""",
            (pkg["id"],),
        ).fetchall():
            lemma_overrides.append(
                {
                    "id": r["id"],
                    "targetId": r["target_id"],
                    "slotKey": r["slot_key"],
                    "label": r["label"],
                    "overrideText": r["override_text"] or "",
                }
            )
        for r in conn.execute(
            """SELECT eo.*, t.slot_key, t.label, t.target_kind
               FROM mod_episode_overrides eo
               LEFT JOIN moddable_targets t ON t.id = eo.target_id
               WHERE eo.package_id = ?
               ORDER BY t.slot_key""",
            (pkg["id"],),
        ).fetchall():
            episode_overrides.append(
                {
                    "id": r["id"],
                    "targetId": r["target_id"],
                    "slotKey": r["slot_key"],
                    "label": r["label"],
                    "overrideText": r["override_text"] or "",
                }
            )
    return {
        "id": mod["id"],
        "slug": mod["slug"],
        "displayName": mod["display_name"],
        "authorUserId": mod["author_user_id"],
        "status": mod["status"],
        "createdAt": mod["created_at"],
        "updatedAt": mod["updated_at"],
        "latestPackage": package,
        "lemmaOverrides": lemma_overrides,
        "episodeOverrides": episode_overrides,
    }


def register_mod_routes(app, get_conn: GetConn, get_user: GetUser) -> None:
    @app.route("/api/mods/registry", methods=["GET"])
    def mods_registry():
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        rows = conn.execute(
            """SELECT m.*, (
                   SELECT p.version FROM mod_packages p
                   WHERE p.mod_id = m.id AND p.status = 'published'
                   ORDER BY p.published_at DESC LIMIT 1
               ) AS latest_version
               FROM mayor_dog_mods m
               WHERE m.status = 'published'
               ORDER BY m.updated_at DESC"""
        ).fetchall()
        items = [
            {
                "id": r["id"],
                "slug": r["slug"],
                "displayName": r["display_name"],
                "authorUserId": r["author_user_id"],
                "latestVersion": r["latest_version"],
            }
            for r in rows
        ]
        conn.close()
        return jsonify({"items": items}), 200

    @app.route("/api/mods/moddable-targets", methods=["GET"])
    def get_moddable_targets():
        entry_id = request.args.get("entryId") or request.args.get("entry_id")
        draft_id = request.args.get("draftEpisodeId") or request.args.get("draft_episode_id")
        kind = request.args.get("targetKind") or request.args.get("target_kind")
        sync = (request.args.get("sync") or "1").lower() not in ("0", "false", "no")
        conn = get_conn()
        meta: dict[str, Any] = {}
        if sync:
            if not kind or kind == "lemma_prompt":
                meta["lemmaSynced"] = sync_lemma_mod_slots(conn)
            if draft_id and (not kind or kind == "episode_section"):
                meta["episode"] = sync_episode_mod_slots(conn, draft_id)
        items = list_moddable_targets(
            conn,
            entry_id=entry_id or None,
            draft_episode_id=draft_id or None,
            target_kind=kind or None,
        )
        conn.close()
        return jsonify({"items": items, "meta": meta}), 200

    @app.route("/api/mods/moddable-targets", methods=["POST"])
    def post_moddable_target():
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        item = upsert_moddable_target(conn, body)
        conn.close()
        return jsonify({"item": item}), 201

    @app.route("/api/mods/moddable-targets/<target_id>", methods=["DELETE"])
    def delete_moddable_target_route(target_id: str):
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        conn = get_conn()
        ok = delete_moddable_target(conn, target_id)
        conn.close()
        if not ok:
            return jsonify({"error": "not found"}), 404
        return jsonify({"ok": True}), 200

    @app.route("/api/mods", methods=["POST"])
    def create_mod():
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        slug = (body.get("slug") or slugify_slot_key(body.get("displayName") or "mod", "mod")).strip()
        display_name = (body.get("displayName") or slug).strip()
        now = _now()
        mod_id = _new_id("mod")
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        conn.execute(
            """INSERT INTO mayor_dog_mods (id, slug, display_name, author_user_id, status, created_at, updated_at)
               VALUES (?, ?, ?, ?, 'draft', ?, ?)""",
            (mod_id, slug, display_name, user_id, now, now),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": mod_id, "slug": slug, "displayName": display_name}), 201

    @app.route("/api/mods/<mod_id>", methods=["GET"])
    def get_mod(mod_id: str):
        conn = get_conn()
        detail = _get_mod_detail(conn, mod_id)
        conn.close()
        if not detail:
            return jsonify({"error": "not found"}), 404
        return jsonify(detail), 200

    @app.route("/api/mods/<mod_id>", methods=["PATCH"])
    def patch_mod(mod_id: str):
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        mod = conn.execute("SELECT * FROM mayor_dog_mods WHERE id = ?", (mod_id,)).fetchone()
        if not mod:
            conn.close()
            return jsonify({"error": "not found"}), 404
        if mod["author_user_id"] != user_id and not _is_admin_request() and user_id != "admin":
            conn.close()
            return jsonify({"error": "forbidden"}), 403
        display_name = body.get("displayName") or body.get("display_name")
        status = body.get("status")
        now = _now()
        if display_name is not None:
            display_name = str(display_name).strip()
            if not display_name:
                conn.close()
                return jsonify({"error": "displayName required"}), 400
            conn.execute(
                "UPDATE mayor_dog_mods SET display_name = ?, updated_at = ? WHERE id = ?",
                (display_name, now, mod_id),
            )
        if status is not None:
            status = str(status).strip()
            if status not in ("draft", "published", "archived"):
                conn.close()
                return jsonify({"error": "invalid status"}), 400
            conn.execute(
                "UPDATE mayor_dog_mods SET status = ?, updated_at = ? WHERE id = ?",
                (status, now, mod_id),
            )
        conn.commit()
        detail = _get_mod_detail(conn, mod_id)
        conn.close()
        return jsonify(detail), 200

    @app.route("/api/mods/packages", methods=["POST"])
    def upload_mod_package():
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        mod_id = body.get("modId") or body.get("mod_id")
        if not mod_id:
            return jsonify({"error": "modId required"}), 400
        version = (body.get("version") or "1.0.0").strip()
        publish = bool(body.get("publish"))
        now = _now()
        pkg_id = _new_id("pkg")
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        mod_row = conn.execute("SELECT * FROM mayor_dog_mods WHERE id = ?", (mod_id,)).fetchone()
        if not mod_row:
            conn.close()
            return jsonify({"error": "mod not found"}), 404
        status = "published" if publish else "draft"
        conn.execute(
            """INSERT INTO mod_packages (
                id, mod_id, version, payload_json, status, uploaded_by_user_id,
                published_at, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                pkg_id,
                mod_id,
                version,
                json.dumps(body.get("payload") or {}),
                status,
                user_id,
                now if publish else None,
                now,
                now,
            ),
        )
        for lo in body.get("lemmaOverrides") or body.get("lemma_overrides") or []:
            target_id = lo.get("targetId") or lo.get("target_id")
            if not target_id:
                continue
            conn.execute(
                """INSERT INTO mod_lemma_overrides (
                    id, package_id, target_id, override_text, patch_properties_json,
                    composition_patch_json, created_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?)""",
                (
                    _new_id("lo"),
                    pkg_id,
                    target_id,
                    lo.get("overrideText") or lo.get("override_text") or "",
                    json.dumps(lo.get("patchProperties") or lo.get("patch_properties_json") or {}),
                    json.dumps(lo.get("compositionPatch") or lo.get("composition_patch_json") or {}),
                    now,
                ),
            )
        for eo in body.get("episodeOverrides") or body.get("episode_overrides") or []:
            target_id = eo.get("targetId") or eo.get("target_id")
            if not target_id:
                continue
            conn.execute(
                """INSERT INTO mod_episode_overrides (
                    id, package_id, target_id, override_text, section_metadata_json, created_at
                ) VALUES (?, ?, ?, ?, ?, ?)""",
                (
                    _new_id("eo"),
                    pkg_id,
                    target_id,
                    eo.get("overrideText") or eo.get("override_text") or "",
                    json.dumps(eo.get("sectionMetadata") or eo.get("section_metadata_json") or {}),
                    now,
                ),
            )
        if publish:
            conn.execute(
                "UPDATE mayor_dog_mods SET status = 'published', updated_at = ? WHERE id = ?",
                (now, mod_id),
            )
        conn.commit()
        conn.close()
        return jsonify({"packageId": pkg_id, "modId": mod_id, "status": status}), 201

    @app.route("/api/mods/portal-settings/<mod_id>", methods=["PUT"])
    def put_portal_settings(mod_id: str):
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        doc_ids = body.get("libraryDocumentIds") or body.get("library_document_ids") or []
        settings = body.get("settings") or body.get("settings_json") or {}
        now = _now()
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        mod_row = conn.execute("SELECT author_user_id FROM mayor_dog_mods WHERE id = ?", (mod_id,)).fetchone()
        if not mod_row:
            conn.close()
            return jsonify({"error": "mod not found"}), 404
        if mod_row["author_user_id"] != user_id and user_id != "admin":
            conn.close()
            return jsonify({"error": "forbidden"}), 403
        conn.execute(
            """INSERT OR REPLACE INTO mod_portal_usc_sets (mod_id, library_document_ids_json, settings_json, updated_at)
               VALUES (?, ?, ?, ?)""",
            (mod_id, json.dumps(doc_ids), json.dumps(settings), now),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "modId": mod_id}), 200

    @app.route("/api/mods/bootstrap", methods=["GET"])
    def mods_bootstrap():
        user_id = request.args.get("userId") or request.args.get("user_id") or get_user()
        episode_id = request.args.get("episodeId") or request.args.get("episode_id")
        conn = get_conn()
        manifest = build_bootstrap_manifest(conn, user_id=user_id, episode_id=episode_id)
        targets = list_moddable_targets(conn)
        manifest["targetsById"] = {t["id"]: t for t in targets}
        conn.close()
        return jsonify(manifest), 200

    @app.route("/api/mods/enabled", methods=["PUT"])
    def set_enabled_mods():
        user_id = get_user()
        denied = _require_presence(user_id)
        if denied:
            return jsonify(denied[0]), denied[1]
        body = request.get_json(silent=True) or {}
        package_ids = body.get("packageIds") or body.get("package_ids") or []
        now = _now()
        conn = get_conn()
        ensure_mayor_dog_mods_schema(conn)
        conn.execute("DELETE FROM user_enabled_mods WHERE user_id = ?", (user_id,))
        for idx, pkg_id in enumerate(package_ids):
            conn.execute(
                """INSERT INTO user_enabled_mods (user_id, mod_package_id, priority, enabled_at)
                   VALUES (?, ?, ?, ?)""",
                (user_id, pkg_id, idx, now),
            )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "userId": user_id, "packageIds": package_ids}), 200

    @app.route("/mayor-dog-mods", strict_slashes=False)
    @app.route("/mayor-dog-mods/", strict_slashes=False)
    @app.route("/mayor-dog-mods/<path:subpath>", strict_slashes=False)
    def serve_mayor_dog_mods_portal(subpath=None):
        from flask import send_from_directory
        from pathlib import Path

        static_dir = Path(__file__).resolve().parent / "static" / "mayor-dog-mods"
        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")
