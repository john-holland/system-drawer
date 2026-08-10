"""Garbage bag Continuuuum site — credits-style create+ with default random_garbage_bag."""

from __future__ import annotations

import time
import uuid
from typing import Any, Dict, List

from flask import jsonify, request

_BAGS: Dict[str, Dict[str, Any]] = {}
_FACILITIES: Dict[str, Dict[str, Any]] = {}

RANDOM_BAG_ID = "random_garbage_bag"


def _ensure_default_bag() -> Dict[str, Any]:
    if RANDOM_BAG_ID not in _BAGS:
        _BAGS[RANDOM_BAG_ID] = {
            "id": RANDOM_BAG_ID,
            "title": "Random Garbage Bag",
            "commodities": [
                {"key": "organic", "weight": 0.5},
                {"key": "plastic", "weight": 0.3},
                {"key": "paper", "weight": 0.2},
            ],
            "defaultMassKg": 8.0,
            "isDefault": True,
            "createdAt": time.time(),
            "updatedAt": time.time(),
        }
    return _BAGS[RANDOM_BAG_ID]


def register_garbage_bag_routes(app) -> None:
    _ensure_default_bag()

    @app.route("/api/garbage-bags", methods=["GET"])
    def garbage_bags_list():
        _ensure_default_bag()
        bags = sorted(_BAGS.values(), key=lambda b: (0 if b.get("isDefault") else 1, b.get("title") or ""))
        return jsonify({"bags": bags, "defaultBagId": RANDOM_BAG_ID})

    @app.route("/api/garbage-bags", methods=["POST"])
    def garbage_bags_create():
        body = request.get_json(silent=True) or {}
        title = (body.get("title") or "Garbage Bag").strip() or "Garbage Bag"
        bid = body.get("id") or ("gbag_" + uuid.uuid4().hex[:10])
        if bid == RANDOM_BAG_ID:
            return jsonify({"error": "cannot overwrite default random_garbage_bag"}), 400
        commodities = body.get("commodities") or [{"key": "mixed", "weight": 1.0}]
        bag = {
            "id": bid,
            "title": title,
            "commodities": commodities,
            "defaultMassKg": float(body.get("defaultMassKg") or 8.0),
            "isDefault": False,
            "createdAt": time.time(),
            "updatedAt": time.time(),
        }
        _BAGS[bid] = bag
        return jsonify(bag), 201

    @app.route("/api/garbage-bags/<bag_id>", methods=["GET"])
    def garbage_bags_get(bag_id: str):
        _ensure_default_bag()
        bag = _BAGS.get(bag_id)
        if not bag:
            return jsonify({"error": "not found"}), 404
        return jsonify(bag)

    @app.route("/api/garbage-bags/<bag_id>", methods=["PATCH"])
    def garbage_bags_patch(bag_id: str):
        _ensure_default_bag()
        bag = _BAGS.get(bag_id)
        if not bag:
            return jsonify({"error": "not found"}), 404
        body = request.get_json(silent=True) or {}
        if "title" in body and bag_id != RANDOM_BAG_ID:
            bag["title"] = (body.get("title") or bag["title"]).strip() or bag["title"]
        if "commodities" in body:
            bag["commodities"] = body["commodities"] or bag["commodities"]
        if "defaultMassKg" in body:
            bag["defaultMassKg"] = float(body["defaultMassKg"])
        bag["updatedAt"] = time.time()
        return jsonify(bag)

    @app.route("/api/garbage-bags/<bag_id>", methods=["DELETE"])
    def garbage_bags_delete(bag_id: str):
        if bag_id == RANDOM_BAG_ID:
            return jsonify({"error": "cannot delete default random_garbage_bag"}), 400
        if bag_id not in _BAGS:
            return jsonify({"error": "not found"}), 404
        del _BAGS[bag_id]
        return jsonify({"ok": True})

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
