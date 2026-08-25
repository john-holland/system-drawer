"""BuildingRagdoll / damaged-objects / store shelf prebake Continuuuum routes."""

from __future__ import annotations

import random
from typing import Any, Callable

from flask import Flask, jsonify, request

GetConn = Callable[[], Any]

_DAMAGED: list[dict[str, Any]] = []
_HEALTH: dict[str, dict[str, Any]] = {}
_REQUIREMENTS: dict[str, dict[str, Any]] = {}
_SHELVES: dict[str, list[dict[str, Any]]] = {}

DEFAULT_COMMODITIES = [
    "labor",
    "power",
    "water",
    "snack",
    "beer",
    "wine",
    "spirit",
    "book",
    "tool",
    "parts",
]


def register_building_ragdoll_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/api/civil/damaged-objects", methods=["GET", "POST"])
    def api_damaged_objects():
        global _DAMAGED
        if request.method == "GET":
            building_id = request.args.get("buildingId")
            rows = [r for r in _DAMAGED if not r.get("resolved")]
            if building_id:
                rows = [r for r in rows if r.get("buildingId") == building_id]
            return jsonify({"damagedObjects": rows})
        body = request.get_json(silent=True) or {}
        rec = {
            "objectId": body.get("objectId") or "obj",
            "buildingId": body.get("buildingId") or "building",
            "damage01": float(body.get("damage01") or 0.2),
            "materialClass": body.get("materialClass") or "Generic",
            "worldPos": body.get("worldPos") or [0, 0, 0],
            "reportedAtUnix": body.get("reportedAtUnix") or 0,
            "waypointGroup": body.get("waypointGroup") or "repair",
            "resolved": False,
        }
        for existing in _DAMAGED:
            if existing.get("objectId") == rec["objectId"] and existing.get("buildingId") == rec["buildingId"]:
                existing.update(rec)
                return jsonify({"damagedObject": existing})
        _DAMAGED.append(rec)
        return jsonify({"damagedObject": rec})

    @app.route("/api/civil/damaged-objects/<object_id>/resolve", methods=["POST"])
    def api_resolve_damaged(object_id: str):
        building_id = (request.get_json(silent=True) or {}).get("buildingId") or request.args.get("buildingId")
        for r in _DAMAGED:
            if r.get("objectId") == object_id and (not building_id or r.get("buildingId") == building_id):
                r["resolved"] = True
                return jsonify({"ok": True, "damagedObject": r})
        return jsonify({"error": "not found"}), 404

    @app.route("/api/civil/building-health/<building_id>", methods=["GET", "PUT"])
    def api_building_health(building_id: str):
        if request.method == "GET":
            return jsonify({"buildingId": building_id, "health": _HEALTH.get(building_id, {
                "integrity01": 1.0,
                "occupancyLoad01": 0.0,
                "exteriorPressure01": 0.0,
                "commodityHunger01": 0.0,
                "memoryAggregate01": 0.0,
            })})
        body = request.get_json(silent=True) or {}
        health = body.get("health") or body
        _HEALTH[building_id] = {
            "integrity01": float(health.get("integrity01", 1)),
            "occupancyLoad01": float(health.get("occupancyLoad01", 0)),
            "exteriorPressure01": float(health.get("exteriorPressure01", 0)),
            "commodityHunger01": float(health.get("commodityHunger01", 0)),
            "memoryAggregate01": float(health.get("memoryAggregate01", 0)),
        }
        return jsonify({"buildingId": building_id, "health": _HEALTH[building_id]})

    @app.route("/api/civil/building-requirements/<building_type_id>", methods=["GET", "PUT"])
    def api_building_requirements(building_type_id: str):
        if request.method == "GET":
            return jsonify({
                "buildingTypeId": building_type_id,
                "requirements": _REQUIREMENTS.get(building_type_id, {"slots": []}),
            })
        body = request.get_json(silent=True) or {}
        _REQUIREMENTS[building_type_id] = body.get("requirements") or body
        return jsonify({"buildingTypeId": building_type_id, "requirements": _REQUIREMENTS[building_type_id]})

    @app.route("/api/civil/store/prebake-shelves", methods=["POST"])
    def api_store_prebake():
        body = request.get_json(silent=True) or {}
        store_id = body.get("storeId") or "store"
        store_type = (body.get("storeType") or "generic").lower()
        prompt = body.get("prompt") or ""
        count = int(body.get("count") or 8)
        keys = list(DEFAULT_COMMODITIES)
        if "liquor" in store_type or "liquor" in prompt.lower():
            keys = ["beer", "wine", "spirit", "mixer", "snack"]
        shelves = []
        for i in range(max(1, min(count, 64))):
            key = random.choice(keys)
            shelves.append({
                "shelfId": f"shelf-{i}",
                "commodityKey": key,
                "displayName": key,
                "quantity": round(random.uniform(1, 12), 2),
                "price": round(random.uniform(1, 40), 2),
            })
        _SHELVES[store_id] = shelves
        return jsonify({
            "storeId": store_id,
            "storeType": store_type,
            "prompt": prompt,
            "shelves": shelves,
            "source": "fallback_catalog",
        })

    _WATER = {
        "supplyPressure01": 1.0,
        "hotSupply01": 0.85,
        "coldSupply01": 1.0,
        "sewerCapacity01": 1.0,
        "pressureLemmaScale": 1.0,
    }

    @app.route("/api/civil/municipal-water", methods=["GET", "PUT"])
    def api_municipal_water():
        if request.method == "GET":
            return jsonify({"municipalWater": dict(_WATER)})
        body = request.get_json(silent=True) or {}
        incoming = body.get("municipalWater") or body
        for k in _WATER:
            if k in incoming:
                _WATER[k] = float(incoming[k])
        return jsonify({"municipalWater": dict(_WATER)})

    @app.route("/api/civil/meta", methods=["GET"])
    def api_civil_meta():
        return jsonify({
            "discoveryTokens": [
                "building-ragdoll",
                "damaged-objects",
                "store-prebake",
                "civic-card",
                "municipal-water",
                "housing-ragdoll",
                "power-lines",
                "dispatch",
                "fire-station",
                "traffic-light",
                "vehicle-inventory",
                "phone-wire",
                "pixel-light",
                "police-station",
                "vehicle-repair",
                "cop-cards",
            ],
            "buildingBeast": "stub_only",
            "housingArchitectureSizes": [
                "quaint",
                "good_size",
                "mc_mansion",
                "mansion",
                "cabin",
                "cottage",
                "townhome",
            ],
        })
