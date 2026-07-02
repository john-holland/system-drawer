"""Dream cycle day completion: gov-glove aspects + lemma 2D spatial hints."""

from __future__ import annotations

import hashlib
import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

try:
    from continuum_api.dream_day_parser import compile_dream_day_hints
    from continuum_api.needs_pyramid import all_aspects, get_aspect
    from continuum_api.sleep_sim import complete_night_for_aspect, run_sleep_sim, stable_collapse_seed
except ImportError:
    from dream_day_parser import compile_dream_day_hints
    from needs_pyramid import all_aspects, get_aspect
    from sleep_sim import complete_night_for_aspect, run_sleep_sim, stable_collapse_seed

try:
    from continuum_api.city_behavior_tree import compile_routing_tree
    from continuum_api.gov_glove_adapter import call_gov_glove, to_society_snapshot
except ImportError:
    from city_behavior_tree import compile_routing_tree
    from gov_glove_adapter import call_gov_glove, to_society_snapshot


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def pull_character_aspects(
    conn: sqlite3.Connection,
    city_id: str,
    actor_id: str | None = None,
) -> dict[str, Any]:
    tick = 0
    try:
        glove = call_gov_glove("processLobbyistImpacts", {"lobbyistActivity": 0.3})
    except (FileNotFoundError, RuntimeError, OSError):
        glove = {"lobbyistDelta": 0, "taxRate": 0.07, "healthcareCoverage": 0.85}
    snapshot = to_society_snapshot(city_id, tick, glove)
    try:
        routing = compile_routing_tree(conn, city_id)
    except sqlite3.Error:
        routing = {"visitOrder": [], "treeId": None}
    visit_order = routing.get("visitOrder") or []
    device_ids: list[str] = []
    try:
        cur = conn.execute(
            "SELECT device_id FROM telecom_devices WHERE city_id = ? LIMIT 32",
            (city_id,),
        )
        device_ids = [r[0] for r in cur.fetchall()]
    except sqlite3.Error:
        pass

    aspect_vectors = []
    for aspect in all_aspects():
        weights = {}
        for feat in aspect.society_features:
            weights[feat] = float(snapshot.get(feat, snapshot.get("healthcareCoverage", 0.5)))
        aspect_vectors.append(
            {
                "aspectId": aspect.aspect_id,
                "featureWeights": weights,
                "visitOrder": [v for v in visit_order if v.get("zoneId") in aspect.zone_ids][:8],
                "deviceIds": device_ids[:4],
                "actorId": actor_id,
            }
        )
    return {
        "cityId": city_id,
        "actorId": actor_id,
        "snapshot": snapshot,
        "routingTreeId": routing.get("treeId"),
        "aspectVectors": aspect_vectors,
    }


def complete_day_for_aspect(
    aspect_id: str,
    snapshot: dict[str, Any],
    lemma_bundle: dict[str, Any] | None = None,
) -> dict[str, Any]:
    aspect = get_aspect(aspect_id)
    if aspect is None:
        raise ValueError(f"unknown aspect: {aspect_id}")
    lemma_bundle = lemma_bundle or {}
    hint = (lemma_bundle.get("byAspect") or {}).get(aspect_id) or {}
    satisfied_hint = hint.get("satisfiedHint")
    base = 0.55
    for feat in aspect.society_features:
        if feat in snapshot:
            base = (base + float(snapshot[feat])) * 0.5
    if satisfied_hint:
        try:
            base = float(satisfied_hint)
        except ValueError:
            pass
    satisfied01 = max(0.0, min(1.0, base))
    spatial_seed = stable_collapse_seed(f"{aspect_id}:{satisfied01}:{hint.get('spatial2dSlot','')}")
    quad_digest = hashlib.sha256(json.dumps({"aspect": aspect_id, "s": satisfied01}, sort_keys=True).encode()).hexdigest()[:16]
    return {
        "aspectId": aspect_id,
        "displayName": aspect.display_name,
        "satisfied01": round(satisfied01, 4),
        "spatialSeedHint": spatial_seed,
        "spatialSlotId": hint.get("spatial2dSlot") or aspect.spatial_slot_id,
        "lemmaEntryIds": [aspect.lemma_entry_hint] if aspect.lemma_entry_hint else [],
        "quadTreeDigest": quad_digest,
    }


def compute_day_collapse_seed(aspect_states: list[dict[str, Any]]) -> int:
    digest = "|".join(
        f"{a.get('aspectId')}:{a.get('quadTreeDigest','')}" for a in sorted(aspect_states, key=lambda x: x.get("aspectId", ""))
    )
    return stable_collapse_seed(digest)


def complete_city_day(
    conn: sqlite3.Connection,
    city_id: str,
    day_prompt: str | None = None,
    lemma_ids: list[str] | None = None,
    actor_id: str | None = None,
) -> dict[str, Any]:
    pulled = pull_character_aspects(conn, city_id, actor_id)
    snapshot = pulled["snapshot"]
    lemma_bundle = compile_dream_day_hints(day_prompt or "")
    aspect_states = [
        complete_day_for_aspect(a.aspect_id, snapshot, lemma_bundle)
        for a in all_aspects()
    ]
    collapse_seed = compute_day_collapse_seed(aspect_states)
    session_id = str(uuid.uuid4())
    now = _now()
    return {
        "sessionId": session_id,
        "cityId": city_id,
        "dayPrompt": day_prompt,
        "lemmaIds": lemma_ids or [],
        "aspectStates": aspect_states,
        "dayCollapseSeed": collapse_seed,
        "quadDigest": {"aspects": [a.get("quadTreeDigest") for a in aspect_states]},
        "pulled": pulled,
        "createdAt": now,
    }


def complete_city_night(day_session: dict[str, Any]) -> dict[str, Any]:
    wave = run_sleep_sim(day_session, day_session.get("dayCollapseSeed"))
    per_aspect = [
        complete_night_for_aspect(a, day_session)
        for a in day_session.get("aspectStates") or []
    ]
    return {
        "sleepSessionId": str(uuid.uuid4()),
        "daySessionId": day_session.get("sessionId"),
        "wave": wave,
        "waveSamples": wave.get("waveSamples"),
        "phaseMarkers": wave.get("phases"),
        "perAspect": per_aspect,
        "sleepSeed": wave.get("sleepSeed"),
    }
