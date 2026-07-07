"""Flask routes for dream cycle day/night and memory recall."""

from __future__ import annotations

import sqlite3
from typing import Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def register_dream_cycle_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    try:
        from continuuuum_api.dream_cycle import complete_city_day, complete_city_night, pull_character_aspects
        from continuuuum_api.dream_cycle_db import (
            ensure_dream_cycle_schema,
            load_day_session,
            load_sleep_session,
            save_day_session,
            save_memory_recall,
            save_sleep_session,
        )
        from continuuuum_api.dream_day_parser import compile_dream_day_hints
        from continuuuum_api.dream_day_horizon import horizon_config_from_body
        from continuuuum_api.needs_pyramid import registry_json
        from continuuuum_api.sleep_sim import run_sleep_sim
    except ImportError:
        from dream_cycle import complete_city_day, complete_city_night, pull_character_aspects
        from dream_cycle_db import (
            ensure_dream_cycle_schema,
            load_day_session,
            load_sleep_session,
            save_day_session,
            save_memory_recall,
            save_sleep_session,
        )
        from dream_day_parser import compile_dream_day_hints
        from dream_day_horizon import horizon_config_from_body
        from needs_pyramid import registry_json
        from sleep_sim import run_sleep_sim

    @app.route("/api/dream-cycle/needs", methods=["GET"])
    def dream_cycle_needs():
        return jsonify(registry_json())

    @app.route("/api/dream-cycle/day/complete", methods=["POST"])
    def dream_day_complete():
        body = request.get_json(silent=True) or {}
        city_id = body.get("cityId") or body.get("city_id")
        if not city_id:
            return jsonify({"error": "cityId required"}), 400
        conn = get_conn()
        try:
            ensure_dream_cycle_schema(conn)
            double_day = bool(body.get("doubleDay") or body.get("double_day"))
            dream_prompt = body.get("dreamDayPrompt") or body.get("dream_day_prompt")
            day_prompt = body.get("dayPrompt") or body.get("prompt")
            horizon_cfg = horizon_config_from_body(body)
            session = complete_city_day(
                conn,
                city_id,
                day_prompt=day_prompt,
                lemma_ids=body.get("lemmaIds") or body.get("lemma_ids"),
                actor_id=body.get("actorId"),
                double_day=double_day,
                dream_day_prompt=dream_prompt,
                good_day_horizon=horizon_cfg,
            )
            if body.get("persist", True):
                save_day_session(conn, session)
            return jsonify({"ok": True, "session": session}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/day/complete-stack", methods=["POST"])
    def dream_day_complete_stack():
        body = request.get_json(silent=True) or {}
        city_id = body.get("cityId") or body.get("city_id")
        if not city_id:
            return jsonify({"error": "cityId required"}), 400
        conn = get_conn()
        try:
            ensure_dream_cycle_schema(conn)
            horizon_cfg = horizon_config_from_body(body)
            session = complete_city_day(
                conn,
                city_id,
                dream_day_prompt=body.get("dreamDayPrompt") or body.get("dayPrompt") or body.get("prompt"),
                lemma_ids=body.get("lemmaIds") or body.get("lemma_ids"),
                actor_id=body.get("actorId"),
                double_day=True,
                good_day_horizon=horizon_cfg,
            )
            if body.get("persist", True):
                save_day_session(conn, session)
            return jsonify({"ok": True, "session": session}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/day/<session_id>", methods=["GET"])
    def dream_day_get(session_id: str):
        conn = get_conn()
        try:
            session = load_day_session(conn, session_id)
            if not session:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"ok": True, "session": session}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/day/aspects", methods=["POST"])
    def dream_day_aspects():
        body = request.get_json(silent=True) or {}
        city_id = body.get("cityId")
        if not city_id:
            return jsonify({"error": "cityId required"}), 400
        conn = get_conn()
        try:
            data = pull_character_aspects(conn, city_id, body.get("actorId"))
            return jsonify({"ok": True, "data": data}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/night/complete", methods=["POST"])
    def dream_night_complete():
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            ensure_dream_cycle_schema(conn)
            day_session = None
            if body.get("sessionId"):
                day_session = load_day_session(conn, body["sessionId"])
            if day_session is None:
                day_session = body.get("daySession")
            if not day_session:
                return jsonify({"error": "sessionId or daySession required"}), 400
            night = complete_city_night(day_session)
            if body.get("persist", True):
                save_sleep_session(conn, night)
            return jsonify({"ok": True, "night": night}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/sleep-wave/<session_id>", methods=["GET"])
    def dream_sleep_wave_get(session_id: str):
        conn = get_conn()
        try:
            sleep = load_sleep_session(conn, session_id)
            if sleep:
                return jsonify({"ok": True, "sleep": sleep}), 200
            day = load_day_session(conn, session_id)
            if day:
                wave = run_sleep_sim(day, day.get("dayCollapseSeed"))
                return jsonify({"ok": True, "wave": wave, "fromDaySession": True}), 200
            return jsonify({"error": "not_found"}), 404
        finally:
            conn.close()

    @app.route("/api/dream-cycle/memory/recall", methods=["POST"])
    def dream_memory_recall():
        body = request.get_json(silent=True) or {}
        sleep_id = body.get("sleepSessionId")
        if not sleep_id:
            return jsonify({"error": "sleepSessionId required"}), 400
        conn = get_conn()
        try:
            sleep = load_sleep_session(conn, sleep_id)
            if not sleep:
                return jsonify({"error": "sleep session not found"}), 404
            samples = sleep.get("waveSamples") or []
            safe_refrain = body.get("safeRefrain") or body.get("safe_refrain") or {}
            max_severity = float(safe_refrain.get("maxAlertSeverity", safe_refrain.get("max_alert_severity", 0.35)))
            min_bed_distance = float(
                safe_refrain.get("minNarrativeDistanceFromBed", safe_refrain.get("min_narrative_distance_from_bed", 0.6))
            )
            raw_peak = max((abs(s) for s in samples), default=0.0)
            distance_from_bed = min(1.0, min_bed_distance + raw_peak * (1.0 - min_bed_distance))
            suppressed_severity = min(max_severity, raw_peak * 0.5)
            output = {
                "mode": "dream_memory",
                "fragments": [
                    {"t": i / max(len(samples) - 1, 1), "v": samples[i]}
                    for i in range(0, len(samples), max(1, len(samples) // 8))
                ],
                "label": safe_refrain.get("refrainLabel") or "dream memory (non-authoritative)",
                "distanceFromBed": round(distance_from_bed, 4),
                "suppressedSeverity": round(suppressed_severity, 4),
                "fearProjectionMode": safe_refrain.get("fearProjectionMode") or "Distant",
            }
            recall_id = save_memory_recall(conn, sleep_id, body.get("actorId"), output, safe_refrain)
            return jsonify({"ok": True, "recallId": recall_id, "output": output}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/lemma/compile", methods=["POST"])
    def dream_lemma_compile():
        body = request.get_json(silent=True) or {}
        text = body.get("text") or ""
        return jsonify({"ok": True, "compiled": compile_dream_day_hints(text)}), 200
