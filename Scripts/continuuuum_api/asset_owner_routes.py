"""Admin-only asset owner reassignment with history + warehouse event."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable
from urllib.parse import urljoin

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]
IsAdmin = Callable[[], bool]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_asset_owner_tables(conn: sqlite3.Connection) -> None:
    conn.execute(
        """CREATE TABLE IF NOT EXISTS asset_owner_history (
            id TEXT PRIMARY KEY,
            asset_kind TEXT NOT NULL,
            asset_id TEXT NOT NULL,
            from_owner TEXT,
            to_owner TEXT NOT NULL,
            admin_user_id TEXT NOT NULL,
            created_at TEXT NOT NULL,
            reason TEXT
        )"""
    )
    conn.execute(
        "CREATE INDEX IF NOT EXISTS idx_asset_owner_history_asset ON asset_owner_history(asset_kind, asset_id, created_at)"
    )
    conn.commit()


def _current_owner(conn: sqlite3.Connection, kind: str, asset_id: str) -> str | None:
    if kind == "usc":
        return None
    if kind == "continuuuum":
        for table, col in (
            ("webcam_anim_recordings", "created_by"),
            ("table_read_sessions", "host_user_id"),
            ("table_read_recordings", "user_id"),
            ("dialogue_sets", None),
        ):
            try:
                if table == "dialogue_sets":
                    cur = conn.execute("SELECT lemma_entry_id FROM dialogue_sets WHERE id = ?", (asset_id,))
                    row = cur.fetchone()
                    if row:
                        return None
                    continue
                cur = conn.execute(f"SELECT {col} FROM {table} WHERE id = ?", (asset_id,))
                row = cur.fetchone()
                if row:
                    return row[0]
            except sqlite3.OperationalError:
                continue
    return None


def _apply_continuuuum_owner(conn: sqlite3.Connection, asset_id: str, to_owner: str) -> str | None:
    updated = None
    try:
        cur = conn.execute("SELECT created_by FROM webcam_anim_recordings WHERE id = ?", (asset_id,))
        row = cur.fetchone()
        if row:
            updated = row[0]
            conn.execute("UPDATE webcam_anim_recordings SET created_by = ? WHERE id = ?", (to_owner, asset_id))
            return updated
    except sqlite3.OperationalError:
        pass
    try:
        cur = conn.execute("SELECT host_user_id FROM table_read_sessions WHERE id = ?", (asset_id,))
        row = cur.fetchone()
        if row:
            updated = row[0]
            conn.execute("UPDATE table_read_sessions SET host_user_id = ? WHERE id = ?", (to_owner, asset_id))
            return updated
    except sqlite3.OperationalError:
        pass
    try:
        cur = conn.execute("SELECT user_id FROM table_read_recordings WHERE id = ?", (asset_id,))
        row = cur.fetchone()
        if row:
            updated = row[0]
            conn.execute("UPDATE table_read_recordings SET user_id = ? WHERE id = ?", (to_owner, asset_id))
            return updated
    except sqlite3.OperationalError:
        pass
    return updated


def reassign_owner(
    conn: sqlite3.Connection,
    *,
    kind: str,
    asset_id: str,
    to_owner: str,
    admin_user_id: str,
    reason: str = "",
    library_base: str = "http://127.0.0.1:5050",
    from_owner: str | None = None,
) -> dict[str, Any]:
    kind = (kind or "").strip().lower()
    if kind not in ("usc", "continuuuum"):
        raise ValueError("asset_kind must be usc or continuuuum")
    if not asset_id or not to_owner:
        raise ValueError("asset_id and to_owner required")

    prev = from_owner
    usc_payload = None
    if kind == "usc":
        try:
            import requests
        except ImportError as exc:
            raise RuntimeError("requests required to PATCH USC owner_id") from exc
        url = urljoin(library_base.rstrip("/") + "/", f"api/library/documents/{asset_id}")
        resp = requests.patch(url, json={"owner_id": to_owner}, timeout=20)
        if resp.status_code >= 400:
            raise RuntimeError(f"USC owner PATCH failed: {resp.status_code}")
        try:
            usc_payload = resp.json()
        except json.JSONDecodeError:
            usc_payload = {}
        prev = usc_payload.get("owner_id_previous") or usc_payload.get("from_owner") or prev
    else:
        applied = _apply_continuuuum_owner(conn, asset_id, to_owner)
        if applied is None and prev is None:
            prev = _current_owner(conn, kind, asset_id)
        elif applied is not None:
            prev = applied

    hid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO asset_owner_history
           (id, asset_kind, asset_id, from_owner, to_owner, admin_user_id, created_at, reason)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
        (hid, kind, str(asset_id), prev, to_owner, admin_user_id, now, reason or ""),
    )
    warehouse_id = None
    try:
        from continuuuum_api.credits_db import warehouse_append
    except ImportError:
        from credits_db import warehouse_append
    warehouse_id = warehouse_append(
        conn,
        tenant_id="default",
        list_id=None,
        event_kind="asset_owner_reassigned",
        source="admin",
        actor_user_id=admin_user_id,
        payload={
            "asset_kind": kind,
            "asset_id": str(asset_id),
            "from_owner": prev,
            "to_owner": to_owner,
            "history_id": hid,
            "reason": reason or "",
        },
    )
    conn.commit()
    return {
        "ok": True,
        "historyId": hid,
        "warehouseId": warehouse_id,
        "assetKind": kind,
        "assetId": str(asset_id),
        "fromOwner": prev,
        "toOwner": to_owner,
        "usc": usc_payload,
    }


def register_asset_owner_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    is_admin: IsAdmin,
    library_base: str = "http://127.0.0.1:5050",
) -> None:
    @app.route("/api/admin/asset-owners", methods=["GET", "POST"])
    def admin_asset_owners():
        if not is_admin():
            return jsonify({"error": "admin only"}), 403
        conn = get_conn()
        try:
            ensure_asset_owner_tables(conn)
            if request.method == "GET":
                cur = conn.execute(
                    "SELECT * FROM asset_owner_history ORDER BY created_at DESC LIMIT 200"
                )
                items = []
                for r in cur.fetchall():
                    items.append(
                        {
                            "id": r["id"],
                            "assetKind": r["asset_kind"],
                            "assetId": r["asset_id"],
                            "fromOwner": r["from_owner"],
                            "toOwner": r["to_owner"],
                            "adminUserId": r["admin_user_id"],
                            "createdAt": r["created_at"],
                            "reason": r["reason"],
                        }
                    )
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            kind = body.get("assetKind") or body.get("kind") or ""
            asset_id = str(body.get("assetId") or body.get("asset_id") or "").strip()
            to_owner = (body.get("toOwner") or body.get("ownerId") or body.get("to_owner") or "").strip()
            reason = body.get("reason") or ""
            try:
                out = reassign_owner(
                    conn,
                    kind=kind,
                    asset_id=asset_id,
                    to_owner=to_owner,
                    admin_user_id=get_user(),
                    reason=reason,
                    library_base=library_base,
                    from_owner=body.get("fromOwner"),
                )
            except ValueError as exc:
                return jsonify({"error": str(exc)}), 400
            except RuntimeError as exc:
                return jsonify({"error": str(exc)}), 502
            return jsonify(out)
        finally:
            conn.close()
