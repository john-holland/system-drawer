"""Flask routes for dream cycle day/night and memory recall."""

from __future__ import annotations

import sqlite3
from typing import Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def register_dream_cycle_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    try:
        from continuum_api.dream_cycle import complete_city_day, complete_city_night, pull_character_aspects
        from continuum_api.dream_cycle_db import (
            ensure_dream_cycle_schema,
            load_day_session,
            load_sleep_session,
            save_day_session,
            save_memory_recall,
            save_sleep_session,
        )
        from continuum_api.dream_day_parser import compile_dream_day_hints
        from continuum_api.needs_pyramid import registry_json
        from continuum_api.sleep_sim import run_sleep_sim
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
            session = complete_city_day(
                conn,
                city_id,
                day_prompt=body.get("dayPrompt") or body.get("prompt"),
                lemma_ids=body.get("lemmaIds") or body.get("lemma_ids"),
                actor_id=body.get("actorId"),
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
            output = {
                "mode": "dream_memory",
                "fragments": [
                    {"t": i / max(len(samples) - 1, 1), "v": samples[i]}
                    for i in range(0, len(samples), max(1, len(samples) // 8))
                ],
                "label": "dream memory (non-authoritative)",
            }
            recall_id = save_memory_recall(conn, sleep_id, body.get("actorId"), output)
            return jsonify({"ok": True, "recallId": recall_id, "output": output}), 200
        finally:
            conn.close()

    @app.route("/api/dream-cycle/lemma/compile", methods=["POST"])
    def dream_lemma_compile():
        body = request.get_json(silent=True) or {}
        text = body.get("text") or ""
        return jsonify({"ok": True, "compiled": compile_dream_day_hints(text)}), 200
