"""Galactic body registry and night-sky cache REST API."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from typing import Callable

from flask import jsonify, request

try:
    from continuuuum_api.galactic_db import ensure_galactic_tables
    from continuuuum_api.society_db import new_id
except ImportError:
    from galactic_db import ensure_galactic_tables
    from society_db import new_id

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _row_body(r: sqlite3.Row) -> dict:
    return {
        "bodyId": r["body_id"],
        "kind": r["kind"],
        "displayName": r["display_name"],
        "galacticX": r["galactic_x"],
        "galacticY": r["galactic_y"],
        "galacticZ": r["galactic_z"],
        "massKg": r["mass_kg"],
        "radiusM": r["radius_m"],
        "radiationLevel": r["radiation_level"],
        "gravityWellStrength": r["gravity_well_strength"],
        "societyPlanetId": r["society_planet_id"],
        "uscAssetId": r["usc_asset_id"],
        "scenePrefabRef": r["scene_prefab_ref"],
        "lemmaColorId": r["lemma_color_id"],
        "lemmaVisibilityId": r["lemma_visibility_id"],
        "immovable": bool(r["immovable"]),
    }


def _row_cache(r: sqlite3.Row) -> dict:
    return {
        "cacheId": r["cache_id"],
        "observerBodyId": r["observer_body_id"],
        "anchorLat": r["anchor_lat"],
        "anchorLon": r["anchor_lon"],
        "anchorAltM": r["anchor_alt_m"],
        "cubemapUscId": r["cubemap_usc_id"],
        "localPath": r["local_path"],
        "starCount": r["star_count"],
        "bakeVersion": r["bake_version"],
    }


def _row_lattice(r: sqlite3.Row) -> dict:
    return {
        "cellId": r["cell_id"],
        "centroidX": r["centroid_x"],
        "centroidY": r["centroid_y"],
        "centroidZ": r["centroid_z"],
        "eggRadii": json.loads(r["egg_radii_json"] or "{}"),
        "blendedCacheIds": json.loads(r["blended_cache_ids_json"] or "[]"),
        "weights": json.loads(r["weights_json"] or "[]"),
    }


def register_galactic_routes(app, get_conn: GetConn) -> None:
    def _ensure(conn: sqlite3.Connection) -> None:
        ensure_galactic_tables(conn)

    @app.route("/api/galactic/bodies", methods=["GET", "POST"])
    def galactic_bodies():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                kind = request.args.get("kind")
                if kind:
                    cur = conn.execute(
                        "SELECT * FROM galactic_bodies WHERE kind = ? ORDER BY display_name",
                        (kind,),
                    )
                else:
                    cur = conn.execute("SELECT * FROM galactic_bodies ORDER BY display_name")
                return jsonify({"items": [_row_body(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            body_id = body.get("bodyId") or body.get("body_id")
            if not body_id:
                return jsonify({"error": "bodyId required"}), 400
            now = _now()
            conn.execute(
                """INSERT INTO galactic_bodies
                   (body_id, kind, display_name, galactic_x, galactic_y, galactic_z,
                    mass_kg, radius_m, radiation_level, gravity_well_strength,
                    society_planet_id, usc_asset_id, scene_prefab_ref,
                    lemma_color_id, lemma_visibility_id, immovable, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    body_id,
                    body.get("kind", "planetoid"),
                    body.get("displayName", body_id),
                    float(body.get("galacticX", 0)),
                    float(body.get("galacticY", 0)),
                    float(body.get("galacticZ", 0)),
                    float(body.get("massKg", 0)),
                    float(body.get("radiusM", 0)),
                    float(body.get("radiationLevel", 0)),
                    float(body.get("gravityWellStrength", 1)),
                    body.get("societyPlanetId"),
                    body.get("uscAssetId"),
                    body.get("scenePrefabRef"),
                    body.get("lemmaColorId"),
                    body.get("lemmaVisibilityId"),
                    1 if body.get("immovable") else 0,
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM galactic_bodies WHERE body_id = ?", (body_id,))
            return jsonify(_row_body(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/galactic/bodies/<body_id>", methods=["GET"])
    def galactic_body_one(body_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT * FROM galactic_bodies WHERE body_id = ?", (body_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(_row_body(row))
        finally:
            conn.close()

    @app.route("/api/galactic/night-sky/caches", methods=["GET", "POST"])
    def galactic_night_sky_caches():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                observer = request.args.get("observerBodyId")
                if observer:
                    cur = conn.execute(
                        "SELECT * FROM galactic_night_sky_caches WHERE observer_body_id = ? ORDER BY anchor_lat, anchor_lon",
                        (observer,),
                    )
                else:
                    cur = conn.execute("SELECT * FROM galactic_night_sky_caches ORDER BY observer_body_id")
                return jsonify({"items": [_row_cache(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            cache_id = body.get("cacheId") or new_id()
            observer_body_id = body.get("observerBodyId")
            if not observer_body_id:
                return jsonify({"error": "observerBodyId required"}), 400
            now = _now()
            conn.execute(
                """INSERT INTO galactic_night_sky_caches
                   (cache_id, observer_body_id, anchor_lat, anchor_lon, anchor_alt_m,
                    cubemap_usc_id, local_path, star_count, bake_version, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    cache_id,
                    observer_body_id,
                    float(body.get("anchorLat", 0)),
                    float(body.get("anchorLon", 0)),
                    float(body.get("anchorAltM", 0)),
                    body.get("cubemapUscId"),
                    body.get("localPath"),
                    int(body.get("starCount", 0)),
                    int(body.get("bakeVersion", 1)),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM galactic_night_sky_caches WHERE cache_id = ?", (cache_id,))
            return jsonify(_row_cache(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/galactic/sky-lattice", methods=["GET", "POST"])
    def galactic_sky_lattice():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM galactic_sky_lattice_cells ORDER BY cell_id")
                return jsonify({"items": [_row_lattice(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            cell_id = body.get("cellId") or new_id()
            now = _now()
            conn.execute(
                """INSERT INTO galactic_sky_lattice_cells
                   (cell_id, centroid_x, centroid_y, centroid_z,
                    egg_radii_json, blended_cache_ids_json, weights_json, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    cell_id,
                    float(body.get("centroidX", 0)),
                    float(body.get("centroidY", 0)),
                    float(body.get("centroidZ", 0)),
                    json.dumps(body.get("eggRadii") or {}),
                    json.dumps(body.get("blendedCacheIds") or []),
                    json.dumps(body.get("weights") or []),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM galactic_sky_lattice_cells WHERE cell_id = ?", (cell_id,))
            return jsonify(_row_lattice(cur.fetchone())), 201
        finally:
            conn.close()
