"""Garbage bag Continuuuum site — SQL keyed by (id, dim); dim 0 defines existence."""

from __future__ import annotations

import time
import uuid
from typing import Any, Dict

from flask import g, jsonify, request

try:
    from continuuuum_api.gd_route_annotations import accepts_game_dimension
except ImportError:
    from gd_route_annotations import accepts_game_dimension  # type: ignore

try:
    from continuuuum_api import garbage_bag_db as bag_db
except ImportError:
    import garbage_bag_db as bag_db  # type: ignore

RANDOM_BAG_ID = bag_db.RANDOM_BAG_ID

# Legacy in-memory maps kept for facilities only (not yet dim-PK migrated).
_FACILITIES: Dict[str, Dict[str, Any]] = {}

# Test helpers re-export (tests may clear via bag_db seed).
_BAGS: Dict[str, Dict[str, Any]] = {}


def _ensure_default_bag() -> Dict[str, Any]:
    """Legacy shim for tests that call the old in-memory helper."""
    return {
        "id": RANDOM_BAG_ID,
        "title": "Random Garbage Bag",
        "commodities": [
            {"key": "organic", "weight": 0.5},
            {"key": "plastic", "weight": 0.3},
            {"key": "paper", "weight": 0.2},
        ],
        "defaultMassKg": 8.0,
        "isDefault": True,
        "dim": 0,
        "createdAt": time.time(),
        "updatedAt": time.time(),
    }


def _default_get_conn():
    try:
        from continuuuum_api.server import get_conn
    except ImportError:
        from server import get_conn  # type: ignore
    return get_conn()


def _dim() -> int:
    return int(getattr(g, "dim_index", 0) or 0)


def register_garbage_bag_routes(app, get_conn=None) -> None:
    _conn = get_conn or _default_get_conn

    @app.route("/api/garbage-bags", methods=["GET"])
    @accepts_game_dimension
    def garbage_bags_list():
        conn = _conn()
        bags = bag_db.list_bags(conn, _dim())
        return jsonify({"bags": bags, "defaultBagId": RANDOM_BAG_ID, "dim": _dim()})

    @app.route("/api/garbage-bags", methods=["POST"])
    @accepts_game_dimension
    def garbage_bags_create():
        body = request.get_json(silent=True) or {}
        title = (body.get("title") or "Garbage Bag").strip() or "Garbage Bag"
        bid = body.get("id") or ("gbag_" + uuid.uuid4().hex[:10])
        if bid == RANDOM_BAG_ID:
            return jsonify({"error": "cannot overwrite default random_garbage_bag"}), 400
        commodities = body.get("commodities") or [{"key": "mixed", "weight": 1.0}]
        dim = _dim()
        conn = _conn()
        bag = bag_db.upsert_bag(
            conn,
            bid,
            dim,
            title=title,
            commodities=commodities,
            default_mass_kg=float(body.get("defaultMassKg") or 8.0),
            is_default=False,
        )
        return jsonify(bag), 201

    @app.route("/api/garbage-bags/<bag_id>", methods=["GET"])
    @accepts_game_dimension
    def garbage_bags_get(bag_id: str):
        conn = _conn()
        bag = bag_db.get_bag(conn, bag_id, _dim())
        if not bag:
            return jsonify({"error": "not found"}), 404
        return jsonify(bag)

    @app.route("/api/garbage-bags/<bag_id>", methods=["PATCH"])
    @accepts_game_dimension
    def garbage_bags_patch(bag_id: str):
        body = request.get_json(silent=True) or {}
        conn = _conn()
        bag = bag_db.patch_bag(conn, bag_id, _dim(), body)
        if not bag:
            return jsonify({"error": "not found"}), 404
        return jsonify(bag)

    @app.route("/api/garbage-bags/<bag_id>", methods=["DELETE"])
    @accepts_game_dimension
    def garbage_bags_delete(bag_id: str):
        dim = _dim()
        if bag_id == RANDOM_BAG_ID and dim == 0:
            return jsonify({"error": "cannot delete default random_garbage_bag"}), 400
        conn = _conn()
        if not bag_db.delete_bag(conn, bag_id, dim):
            return jsonify({"error": "not found"}), 404
        return jsonify({"ok": True, "dim": dim})
    @app.route("/api/sanitation/facilities", methods=["GET"])
    def sanitation_facilities_list():
        return jsonify({"facilities": list(_FACILITIES.values())})

    @app.route("/api/sanitation/facilities", methods=["POST"])
    def sanitation_facilities_upsert():
        body = request.get_json(silent=True) or {}
        fid = body.get("facilityId") or body.get("id") or ("san_" + uuid.uuid4().hex[:8])
        facility = {
            "id": fid,
            "displayName": body.get("displayName") or fid,
            "companyId": body.get("companyId") or "public_sanitation_auth",
            "parentCompanyId": body.get("parentCompanyId") or "government",
            "ipv6CityPrefix": body.get("ipv6CityPrefix") or "",
            "companyIpConfigId": body.get("companyIpConfigId") or "",
            "govStats": body.get("govStats") or {"coverage01": 0.7, "budget01": 0.5},
            "updatedAt": time.time(),
        }
        _FACILITIES[fid] = facility
        return jsonify(facility), 201
