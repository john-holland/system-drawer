"""Loadouts CRUD + inventory lemma property-spec seeds."""

from __future__ import annotations

import uuid
from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request

GetConn = Callable[[], Any]

INVENTORY_PROPERTY_SPECS: list[dict[str, str | None]] = [
    {
        "key": "inv-op",
        "value_type": "String",
        "default_value": "have",
        "description": "inventory op: have|give|take|transfer|assert",
        "allowed_values_json": '["have","give","take","transfer","assert"]',
    },
    {
        "key": "inv-item",
        "value_type": "String",
        "default_value": "",
        "description": "Loadout item name",
        "allowed_values_json": None,
    },
    {
        "key": "inv-from",
        "value_type": "String",
        "default_value": "",
        "description": "Source actor id",
        "allowed_values_json": None,
    },
    {
        "key": "inv-to",
        "value_type": "String",
        "default_value": "",
        "description": "Target actor id",
        "allowed_values_json": None,
    },
    {
        "key": "wp-name",
        "value_type": "String",
        "default_value": "A",
        "description": "Waypoint name / id",
        "allowed_values_json": None,
    },
    {
        "key": "wp-x",
        "value_type": "Float",
        "default_value": "0",
        "description": "Waypoint X",
        "allowed_values_json": None,
    },
    {
        "key": "wp-y",
        "value_type": "Float",
        "default_value": "0",
        "description": "Waypoint Y",
        "allowed_values_json": None,
    },
    {
        "key": "wp-z",
        "value_type": "Float",
        "default_value": "0",
        "description": "Waypoint Z",
        "allowed_values_json": None,
    },
    {
        "key": "wp-formation",
        "value_type": "String",
        "default_value": "triangle",
        "description": "Formation id for leg",
        "allowed_values_json": '["triangle","pineapple","divide_and_conquer"]',
    },
]

SCHEMA_PATH = Path(__file__).resolve().parent.parent / "continuuuum_loadouts_schema.sql"


def ensure_loadouts_schema(conn) -> None:
    sql = SCHEMA_PATH.read_text(encoding="utf-8") if SCHEMA_PATH.is_file() else ""
    if not sql:
        sql = """
CREATE TABLE IF NOT EXISTS loadouts (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  icon_asset TEXT,
  prefab_id TEXT,
  use_takeout_animation INTEGER NOT NULL DEFAULT 0,
  use_putaway_animation INTEGER NOT NULL DEFAULT 0,
  ownedby_actor_id TEXT,
  heldby_actor_id TEXT,
  onground_x REAL,
  onground_y REAL,
  onground_z REAL,
  loadout_set_id TEXT NOT NULL DEFAULT 'default'
);
"""
    conn.executescript(sql)
    # Migrate older tables missing XYZ / set columns
    cols = {
        r[1] if not hasattr(r, "keys") else r["name"]
        for r in conn.execute("PRAGMA table_info(loadouts)").fetchall()
    }
    alters = []
    if "onground_x" not in cols:
        alters.append("ALTER TABLE loadouts ADD COLUMN onground_x REAL")
    if "onground_y" not in cols:
        alters.append("ALTER TABLE loadouts ADD COLUMN onground_y REAL")
    if "onground_z" not in cols:
        alters.append("ALTER TABLE loadouts ADD COLUMN onground_z REAL")
    if "loadout_set_id" not in cols:
        alters.append("ALTER TABLE loadouts ADD COLUMN loadout_set_id TEXT NOT NULL DEFAULT 'default'")
    for stmt in alters:
        conn.execute(stmt)
    conn.commit()


def _coerce_xyz(body: dict, cur: dict) -> None:
    for snake, camel in (
        ("onground_x", "ongroundX"),
        ("onground_y", "ongroundY"),
        ("onground_z", "ongroundZ"),
    ):
        if snake in body and body[snake] is not None:
            cur[snake] = float(body[snake])
        elif camel in body and body[camel] is not None:
            cur[snake] = float(body[camel])


def ensure_inventory_property_specs(conn) -> int:
    inserted = 0
    for spec in INVENTORY_PROPERTY_SPECS:
        cur = conn.execute(
            "SELECT 1 FROM localization_property_specs WHERE key = ?",
            (spec["key"],),
        )
        if cur.fetchone():
            continue
        try:
            conn.execute(
                """INSERT INTO localization_property_specs
                   (key, value_type, allowed_values_json, default_value, description)
                   VALUES (?, ?, ?, ?, ?)""",
                (
                    spec["key"],
                    spec["value_type"],
                    spec.get("allowed_values_json"),
                    spec["default_value"],
                    spec["description"],
                ),
            )
            inserted += 1
        except Exception:
            break
    conn.commit()
    return inserted


def _row_to_dict(row) -> dict[str, Any]:
    if row is None:
        return {}
    keys = row.keys() if hasattr(row, "keys") else []
    if keys:
        return {k: row[k] for k in keys}
    return {
        "id": row[0],
        "name": row[1],
        "icon_asset": row[2],
        "prefab_id": row[3],
        "use_takeout_animation": bool(row[4]),
        "use_putaway_animation": bool(row[5]),
        "ownedby_actor_id": row[6],
        "heldby_actor_id": row[7],
        "onground_x": row[8],
        "onground_y": row[9],
        "onground_z": row[10],
        "loadout_set_id": row[11] if len(row) > 11 else "default",
    }


def register_loadouts_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/api/loadouts/ensure", methods=["POST"])
    def loadouts_ensure():
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            n = ensure_inventory_property_specs(conn)
            return jsonify({"ok": True, "specsInserted": n}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts/sets", methods=["GET"])
    def loadouts_sets():
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            rows = conn.execute(
                "SELECT DISTINCT loadout_set_id FROM loadouts ORDER BY loadout_set_id"
            ).fetchall()
            sets = [r[0] if not hasattr(r, "keys") else r["loadout_set_id"] for r in rows]
            if "default" not in sets:
                sets.insert(0, "default")
            return jsonify({"sets": sets}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts", methods=["GET"])
    def loadouts_list():
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            set_id = request.args.get("set") or request.args.get("loadout_set_id")
            actor = request.args.get("actor") or request.args.get("ownedby_actor_id")
            q = "SELECT * FROM loadouts WHERE 1=1"
            args: list[Any] = []
            if set_id:
                q += " AND loadout_set_id = ?"
                args.append(set_id)
            if actor:
                q += " AND (ownedby_actor_id = ? OR heldby_actor_id = ?)"
                args.extend([actor, actor])
            q += " ORDER BY name"
            rows = conn.execute(q, args).fetchall()
            return jsonify({"items": [_row_to_dict(r) for r in rows]}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts", methods=["POST"])
    def loadouts_create():
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            item_id = body.get("id") or uuid.uuid4().hex
            conn.execute(
                """INSERT INTO loadouts
                   (id, name, icon_asset, prefab_id, use_takeout_animation, use_putaway_animation,
                    ownedby_actor_id, heldby_actor_id, onground_x, onground_y, onground_z, loadout_set_id)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    item_id,
                    body.get("name") or "item",
                    body.get("icon_asset") or body.get("iconAsset"),
                    body.get("prefab_id") or body.get("prefabId"),
                    1 if body.get("use_takeout_animation") or body.get("useTakeoutAnimation") else 0,
                    1 if body.get("use_putaway_animation") or body.get("usePutawayAnimation") else 0,
                    body.get("ownedby_actor_id") or body.get("ownedbyActorId"),
                    body.get("heldby_actor_id") or body.get("heldbyActorId"),
                    float(body.get("onground_x", body.get("ongroundX", 0) or 0)),
                    float(body.get("onground_y", body.get("ongroundY", 0) or 0)),
                    float(body.get("onground_z", body.get("ongroundZ", 0) or 0)),
                    body.get("loadout_set_id") or body.get("loadoutSetId") or "default",
                ),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM loadouts WHERE id = ?", (item_id,)).fetchone()
            return jsonify({"ok": True, "item": _row_to_dict(row)}), 201
        finally:
            conn.close()

    @app.route("/api/loadouts/<item_id>", methods=["PUT", "PATCH"])
    def loadouts_update(item_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            existing = conn.execute("SELECT * FROM loadouts WHERE id = ?", (item_id,)).fetchone()
            if not existing:
                return jsonify({"ok": False, "error": "not_found"}), 404
            cur = _row_to_dict(existing)
            cur.update({k: v for k, v in body.items() if v is not None})
            # normalize camelCase
            if "iconAsset" in body:
                cur["icon_asset"] = body["iconAsset"]
            if "prefabId" in body:
                cur["prefab_id"] = body["prefabId"]
            if "ownedbyActorId" in body:
                cur["ownedby_actor_id"] = body["ownedbyActorId"]
            if "heldbyActorId" in body:
                cur["heldby_actor_id"] = body["heldbyActorId"]
            if "loadoutSetId" in body:
                cur["loadout_set_id"] = body["loadoutSetId"]
            if "useTakeoutAnimation" in body:
                cur["use_takeout_animation"] = 1 if body["useTakeoutAnimation"] else 0
            if "usePutawayAnimation" in body:
                cur["use_putaway_animation"] = 1 if body["usePutawayAnimation"] else 0
            _coerce_xyz(body, cur)
            # Booleans may arrive as true/false
            if "use_takeout_animation" in body:
                cur["use_takeout_animation"] = 1 if body["use_takeout_animation"] else 0
            if "use_putaway_animation" in body:
                cur["use_putaway_animation"] = 1 if body["use_putaway_animation"] else 0
            conn.execute(
                """UPDATE loadouts SET
                   name=?, icon_asset=?, prefab_id=?, use_takeout_animation=?, use_putaway_animation=?,
                   ownedby_actor_id=?, heldby_actor_id=?, onground_x=?, onground_y=?, onground_z=?, loadout_set_id=?
                   WHERE id=?""",
                (
                    cur.get("name"),
                    cur.get("icon_asset"),
                    cur.get("prefab_id"),
                    1 if cur.get("use_takeout_animation") else 0,
                    1 if cur.get("use_putaway_animation") else 0,
                    cur.get("ownedby_actor_id"),
                    cur.get("heldby_actor_id"),
                    float(cur.get("onground_x") or 0),
                    float(cur.get("onground_y") or 0),
                    float(cur.get("onground_z") or 0),
                    cur.get("loadout_set_id") or "default",
                    item_id,
                ),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM loadouts WHERE id = ?", (item_id,)).fetchone()
            return jsonify({"ok": True, "item": _row_to_dict(row)}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts/<item_id>/transfer", methods=["POST"])
    def loadouts_transfer(item_id: str):
        body = request.get_json(silent=True) or {}
        to_actor = body.get("to") or body.get("toActorId") or body.get("heldby_actor_id")
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            existing = conn.execute("SELECT * FROM loadouts WHERE id = ?", (item_id,)).fetchone()
            if not existing:
                return jsonify({"ok": False, "error": "not_found"}), 404
            conn.execute(
                """UPDATE loadouts SET heldby_actor_id=?, ownedby_actor_id=COALESCE(?, ownedby_actor_id),
                   onground_x=NULL, onground_y=NULL, onground_z=NULL WHERE id=?""",
                (to_actor, body.get("ownedby_actor_id") or to_actor, item_id),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM loadouts WHERE id = ?", (item_id,)).fetchone()
            return jsonify({"ok": True, "item": _row_to_dict(row)}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts/<item_id>", methods=["DELETE"])
    def loadouts_delete(item_id: str):
        conn = get_conn()
        try:
            ensure_loadouts_schema(conn)
            conn.execute("DELETE FROM loadouts WHERE id = ?", (item_id,))
            conn.commit()
            return jsonify({"ok": True}), 200
        finally:
            conn.close()

    @app.route("/api/loadouts/prompt-hints", methods=["GET"])
    def loadouts_prompt_hints():
        return jsonify(
            {
                "placeholder": "have",
                "examples": [
                    "{P:have|item=drink|op=assert}",
                    "{P:have|op=give|item=radio|from=tim|to=sara}",
                    "{P:waypoint|name=A|x=1|y=2|z=3}",
                    "{P:waypoint|from=A|to=B|formation=triangle}",
                    "{P:formation|id=pineapple}",
                ],
            }
        ), 200
