"""Persona day / civil LOD: multiplexed persona + biorhythm request + settings."""

from __future__ import annotations

from typing import Any, Callable

from flask import Flask, jsonify, request

GetConn = Callable[[], Any]

DEFAULT_CIVIL_LOD_SETTINGS: dict[str, Any] = {
    "kindPriorityOrder": ["Kitchen", "School", "Church", "Library", "Mall", "Generic"],
    "developerMaxSpeedMps": 12.0,
    "logFalloffBase": 10.0,
    "lodFloor": 0.15,
    "maxFullSimVenues": 4,
    "maxWokenActors": 24,
    "featureBudgetId": "civil_systems",
    "featureBudgetImportanceHint": "Civil Systems / Persona Day — wake retinues under FeatureBudget Auto",
}

VENUE_CATALOG = [
    {"kind": "Kitchen", "buildingTypeId": "restaurant", "hoursCron": "* 11-22 * * *"},
    {"kind": "School", "buildingTypeId": "school", "hoursCron": "* 7-16 * * 1-5"},
    {"kind": "Mall", "buildingTypeId": "mall", "hoursCron": "* 10-21 * * *"},
    {"kind": "Library", "buildingTypeId": "library", "hoursCron": "* 9-20 * * *"},
    {"kind": "Church", "buildingTypeId": "church_small", "hoursCron": "* 8-12 * * 0"},
    {"kind": "Generic", "buildingTypeId": "", "hoursCron": "* 8-20 * * *"},
]

_SETTINGS_MEM: dict[str, Any] = dict(DEFAULT_CIVIL_LOD_SETTINGS)


def _try_pull_aspects(conn, city_id: str, actor_id: str | None) -> dict[str, Any]:
    try:
        from continuuuum_api.dream_cycle import pull_character_aspects
    except ImportError:
        try:
            from dream_cycle import pull_character_aspects
        except ImportError:
            return {
                "cityId": city_id,
                "actorId": actor_id,
                "snapshot": {"healthcareCoverage": 0.85, "taxRate": 0.07},
                "aspectVectors": [],
            }
    try:
        return pull_character_aspects(conn, city_id, actor_id)
    except Exception:
        return {
            "cityId": city_id,
            "actorId": actor_id,
            "snapshot": {"healthcareCoverage": 0.85, "taxRate": 0.07},
            "aspectVectors": [],
        }


def _need_satisfied_from_aspects(aspect_vectors: list[dict[str, Any]]) -> dict[str, float]:
    out: dict[str, float] = {}
    for av in aspect_vectors or []:
        aid = av.get("aspectId") or ""
        weights = av.get("featureWeights") or {}
        if not aid:
            continue
        if weights:
            out[aid] = sum(float(v) for v in weights.values()) / max(1, len(weights))
        else:
            out[aid] = 0.55
    return out


def _society_features_flat(snapshot: dict[str, Any]) -> dict[str, float]:
    out: dict[str, float] = {}
    for k, v in (snapshot or {}).items():
        try:
            out[str(k)] = float(v)
        except (TypeError, ValueError):
            continue
    return out


def build_persona_bundle(
    conn,
    *,
    city_id: str,
    persona_key: str,
    actor_type: str,
    civil_kind: str,
    venue_stable_id: str | None = None,
    restaurant_id: int | None = None,
) -> dict[str, Any]:
    pulled = _try_pull_aspects(conn, city_id, persona_key)
    snapshot = pulled.get("snapshot") or {}
    needs = _need_satisfied_from_aspects(pulled.get("aspectVectors") or [])
    features = _society_features_flat(snapshot)

    duty_cron = None
    pecking = 100
    if restaurant_id is not None:
        try:
            try:
                from continuuuum_api.restaurant_db import ensure_restaurant_tables, list_retinue
            except ImportError:
                from restaurant_db import ensure_restaurant_tables, list_retinue
            ensure_restaurant_tables(conn)
            for m in list_retinue(conn, restaurant_id):
                if (m.get("persona_key") or "") == persona_key:
                    duty_cron = m.get("duty_cron")
                    pecking = int(m.get("pecking_order") or 100)
                    break
        except Exception:
            pass

    # Biorhythm seed from need mean + healthcareCoverage
    need_vals = list(needs.values()) or [0.55]
    need_mean = sum(need_vals) / len(need_vals)
    hc = float(features.get("healthcareCoverage", 0.7))
    amp = max(0.0, min(1.0, 0.35 * need_mean + 0.65 * hc))

    return {
        "personaKey": persona_key,
        "actorType": actor_type or civil_kind.lower(),
        "cityId": city_id,
        "venueStableId": venue_stable_id or "",
        "civilKind": civil_kind,
        "dutyCron": duty_cron,
        "peckingOrder": pecking,
        "biorhythmAmplitudeSeed": round(amp, 4),
        "biorhythmPhase01": round((hash(persona_key) % 1000) / 1000.0, 4),
        "societyFeatures": features,
        "needSatisfied01": needs,
        "source": {"routingTreeId": pulled.get("routingTreeId"), "aspectCount": len(pulled.get("aspectVectors") or [])},
    }


def register_persona_day_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/api/persona-day/request", methods=["GET", "POST"])
    def api_persona_day_request():
        body = request.get_json(silent=True) or {}
        args = request.args
        city_id = body.get("cityId") or args.get("cityId") or "demo-city"
        persona_key = body.get("personaKey") or args.get("personaKey") or "persona"
        actor_type = body.get("actorType") or args.get("actorType") or "generic"
        civil_kind = body.get("civilKind") or args.get("civilKind") or "Generic"
        venue_stable_id = body.get("venueStableId") or args.get("venueStableId")
        restaurant_id = body.get("restaurantId") or args.get("restaurantId")
        rid = int(restaurant_id) if restaurant_id not in (None, "") else None

        conn = get_conn()
        try:
            bundle = build_persona_bundle(
                conn,
                city_id=city_id,
                persona_key=persona_key,
                actor_type=actor_type,
                civil_kind=civil_kind,
                venue_stable_id=venue_stable_id,
                restaurant_id=rid,
            )
            return jsonify({"bundle": bundle})
        finally:
            conn.close()

    @app.route("/api/persona-day/venues", methods=["GET"])
    def api_persona_day_venues():
        return jsonify({"venues": VENUE_CATALOG, "kinds": [v["kind"] for v in VENUE_CATALOG]})

    @app.route("/api/persona-day/settings", methods=["GET", "PUT"])
    def api_persona_day_settings():
        global _SETTINGS_MEM
        if request.method == "GET":
            return jsonify({"settings": dict(_SETTINGS_MEM)})
        body = request.get_json(silent=True) or {}
        incoming = body.get("settings") or body
        merged = dict(DEFAULT_CIVIL_LOD_SETTINGS)
        merged.update(_SETTINGS_MEM)
        for k, v in incoming.items():
            if k in DEFAULT_CIVIL_LOD_SETTINGS or k in merged:
                merged[k] = v
        _SETTINGS_MEM = merged
        return jsonify({"settings": dict(_SETTINGS_MEM)})

    @app.route("/api/persona-day/meta", methods=["GET"])
    def api_persona_day_meta():
        return jsonify(
            {
                "defaults": DEFAULT_CIVIL_LOD_SETTINGS,
                "featureBudgetId": "civil_systems",
                "discoveryTokens": ["persona-day", "civil-lod", "retinue-wake"],
            }
        )
