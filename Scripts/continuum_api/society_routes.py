"""Society / political sim REST API."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable

from flask import Response, jsonify, request, send_from_directory

try:
    from continuum_api.building_prompt_interpreter import merge_building_stats, request_building_prompt, sync_building
    from continuum_api.building_type_resolver import resolve_building_type, resolve_for_zone
    from continuum_api.city_constraints_solver import solve as solve_constraints
    from continuum_api.city_network_assign import provision_building_device, provision_city_network
    from continuum_api.city_spatial_map import build_spatial_map
    from continuum_api.political_solver import tick_city
    from continuum_api.society_db import default_zone_document, ensure_society_tables, new_id
    from continuum_api.society_graph import apply_preset, compute_population_support
    from continuum_api.zoning_rules_engine import parse_zone_document
except ImportError:
    from building_prompt_interpreter import merge_building_stats, request_building_prompt, sync_building
    from building_type_resolver import resolve_building_type, resolve_for_zone
    from city_constraints_solver import solve as solve_constraints
    from city_network_assign import provision_building_device, provision_city_network
    from city_spatial_map import build_spatial_map
    from political_solver import tick_city
    from society_db import default_zone_document, ensure_society_tables, new_id
    from society_graph import apply_preset, compute_population_support
    from zoning_rules_engine import parse_zone_document

GetConn = Callable[[], sqlite3.Connection]
STATIC_CC = Path(__file__).resolve().parent / "static" / "city-config"
STATIC_SD = Path(__file__).resolve().parent / "static" / "society-dashboard"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _row_planet(r: sqlite3.Row) -> dict:
    return {
        "planetId": r["planet_id"],
        "displayName": r["display_name"],
        "galacticPrefix": json.loads(r["galactic_prefix_json"]),
        "defaultNetworkId": r["default_network_id"],
        "commodityIndices": json.loads(r["commodity_indices_json"] or "{}"),
    }


def _row_city(r: sqlite3.Row) -> dict:
    return {
        "cityId": r["city_id"],
        "planetId": r["planet_id"],
        "displayName": r["display_name"],
        "cityGrid": r["city_grid"],
        "networkId": r["network_id"],
        "ipv6CityPrefix": r["ipv6_city_prefix"],
        "solverCadenceNarrativeSeconds": r["solver_cadence_narrative_seconds"],
    }


def register_society_routes(app, get_conn: GetConn) -> None:
    @app.route("/city-config")
    @app.route("/city-config/<path:subpath>")
    def serve_city_config(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_CC, subpath)
        return send_from_directory(STATIC_CC, "index.html")

    @app.route("/society-dashboard")
    @app.route("/society-dashboard/<path:subpath>")
    def serve_society_dashboard(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_SD, subpath)
        return send_from_directory(STATIC_SD, "index.html")

    def _ensure(conn: sqlite3.Connection) -> None:
        ensure_society_tables(conn)

    @app.route("/api/society/planets", methods=["GET", "POST"])
    def society_planets():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM society_planets ORDER BY display_name")
                return jsonify({"items": [_row_planet(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            planet_id = body.get("planetId") or body.get("planet_id")
            if not planet_id:
                return jsonify({"error": "planetId required"}), 400
            now = _now()
            prefix = body.get("galacticPrefix") or {"dimensional": 0, "galactic": 1, "system": 0, "planet": 2}
            conn.execute(
                """INSERT INTO society_planets
                   (planet_id, display_name, galactic_prefix_json, default_network_id, commodity_indices_json, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?)""",
                (
                    planet_id,
                    body.get("displayName", planet_id),
                    json.dumps(prefix),
                    body.get("defaultNetworkId", f"society.planet.{planet_id}"),
                    json.dumps(body.get("commodityIndices") or {}),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM society_planets WHERE planet_id = ?", (planet_id,))
            return jsonify(_row_planet(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/society/planets/<planet_id>/cities", methods=["GET", "POST"])
    def society_planet_cities(planet_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM society_cities WHERE planet_id = ? ORDER BY display_name", (planet_id,))
                return jsonify({"items": [_row_city(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            city_id = body.get("cityId") or str(uuid.uuid4())
            display = body.get("displayName", city_id)
            net = provision_city_network(conn, planet_id, city_id, display, body.get("geohash"))
            now = _now()
            conn.execute(
                """INSERT INTO society_cities
                   (city_id, planet_id, display_name, city_grid, network_id, ipv6_city_prefix,
                    geohash, sg4d_causality_leaf_id, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    city_id,
                    planet_id,
                    display,
                    net["cityGrid"],
                    net["networkId"],
                    net["ipv6CityPrefix"],
                    body.get("geohash"),
                    body.get("sg4dCausalityLeafId"),
                    now,
                    now,
                ),
            )
            conn.execute(
                """INSERT INTO city_config (city_id, city_size_sqm, annual_budget_usd, allow_debt, commodity_indices_json, updated_at)
                   VALUES (?, ?, ?, 0, '{}', ?)""",
                (city_id, body.get("citySizeSqm", 1_000_000), body.get("annualBudgetUsd", 10_000_000), now),
            )
            zdoc = default_zone_document(city_id)
            conn.execute(
                """INSERT INTO city_zone_documents (id, city_id, version, document_json, created_at)
                   VALUES (?, ?, 1, ?, ?)""",
                (new_id(), city_id, json.dumps(zdoc), now),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM society_cities WHERE city_id = ?", (city_id,))
            return jsonify(_row_city(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/config", methods=["GET", "PATCH"])
    def society_city_config(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM city_config WHERE city_id = ?", (city_id,))
                row = cur.fetchone()
                if not row:
                    return jsonify({"error": "not found"}), 404
                return jsonify(
                    {
                        "cityId": city_id,
                        "citySizeSqm": row["city_size_sqm"],
                        "annualBudgetUsd": row["annual_budget_usd"],
                        "allowDebt": bool(row["allow_debt"]),
                        "commodityIndices": json.loads(row["commodity_indices_json"] or "{}"),
                    }
                )
            body = request.get_json(force=True) or {}
            now = _now()
            conn.execute(
                """UPDATE city_config SET
                   city_size_sqm = COALESCE(?, city_size_sqm),
                   annual_budget_usd = COALESCE(?, annual_budget_usd),
                   allow_debt = COALESCE(?, allow_debt),
                   commodity_indices_json = COALESCE(?, commodity_indices_json),
                   updated_at = ?
                   WHERE city_id = ?""",
                (
                    body.get("citySizeSqm"),
                    body.get("annualBudgetUsd"),
                    1 if body.get("allowDebt") else 0 if "allowDebt" in body else None,
                    json.dumps(body["commodityIndices"]) if "commodityIndices" in body else None,
                    now,
                    city_id,
                ),
            )
            conn.commit()
            return jsonify({"ok": True})
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/zones", methods=["GET", "PUT"])
    def society_city_zones(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute(
                    "SELECT document_json FROM city_zone_documents WHERE city_id = ? ORDER BY version DESC LIMIT 1",
                    (city_id,),
                )
                row = cur.fetchone()
                if not row:
                    return jsonify(default_zone_document(city_id))
                return jsonify(json.loads(row["document_json"]))
            body = request.get_json(force=True) or {}
            raw = body.get("document") or body.get("yaml") or body
            doc = parse_zone_document(raw, city_id)
            ver_cur = conn.execute(
                "SELECT COALESCE(MAX(version), 0) + 1 FROM city_zone_documents WHERE city_id = ?",
                (city_id,),
            )
            ver = int(ver_cur.fetchone()[0])
            conn.execute(
                """INSERT INTO city_zone_documents (id, city_id, version, document_json, created_at)
                   VALUES (?, ?, ?, ?, ?)""",
                (new_id(), city_id, ver, json.dumps(doc), _now()),
            )
            conn.commit()
            return jsonify(doc)
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/zoning/solve", methods=["POST"])
    def society_zoning_solve(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            cfg_cur = conn.execute("SELECT * FROM city_config WHERE city_id = ?", (city_id,))
            cfg = cfg_cur.fetchone()
            zone_cur = conn.execute(
                "SELECT document_json FROM city_zone_documents WHERE city_id = ? ORDER BY version DESC LIMIT 1",
                (city_id,),
            )
            zrow = zone_cur.fetchone()
            zone_doc = json.loads(zrow["document_json"]) if zrow else default_zone_document(city_id)
            bcur = conn.execute("SELECT * FROM building_registry WHERE city_id = ?", (city_id,))
            buildings = [dict(r) for r in bcur.fetchall()]
            commodities = json.loads(cfg["commodity_indices_json"] or "{}") if cfg else {}
            if body.get("commodityIndices"):
                commodities = body["commodityIndices"]
            result = solve_constraints(
                body.get("mode", "forward"),
                city_id,
                body.get("citySizeSqm") or (cfg["city_size_sqm"] if cfg else None),
                body.get("annualBudgetUsd") or (cfg["annual_budget_usd"] if cfg else None),
                body.get("zoneDocument") or zone_doc,
                buildings,
                commodities,
                bool(body.get("allowDebt") if "allowDebt" in body else (cfg["allow_debt"] if cfg else False)),
            )
            return jsonify(result)
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/cityscape", methods=["GET"])
    def society_cityscape(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute(
                "SELECT profile_json, version FROM city_scape_profiles WHERE city_id = ? ORDER BY version DESC LIMIT 1",
                (city_id,),
            )
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "no profile"}), 404
            return jsonify({"version": row["version"], "profile": json.loads(row["profile_json"])})
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/spatial-map", methods=["GET"])
    def society_spatial_map(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cfg_cur = conn.execute("SELECT city_size_sqm FROM city_config WHERE city_id = ?", (city_id,))
            cfg = cfg_cur.fetchone()
            size = float(cfg["city_size_sqm"]) if cfg else 1_000_000
            zone_cur = conn.execute(
                "SELECT document_json FROM city_zone_documents WHERE city_id = ? ORDER BY version DESC LIMIT 1",
                (city_id,),
            )
            zrow = zone_cur.fetchone()
            zone_doc = json.loads(zrow["document_json"]) if zrow else default_zone_document(city_id)
            solve_body = solve_constraints("forward", city_id, size, 10_000_000, zone_doc, [], {}, False)
            return jsonify(build_spatial_map(conn, city_id, size, zone_doc, solve_body))
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/snapshot", methods=["GET"])
    def society_snapshot(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute(
                "SELECT snapshot_json, tick_index FROM society_snapshots WHERE city_id = ? ORDER BY tick_index DESC LIMIT 1",
                (city_id,),
            )
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "no snapshot"}), 404
            return jsonify({"tickIndex": row["tick_index"], "snapshot": json.loads(row["snapshot_json"])})
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/tick", methods=["POST"])
    def society_tick(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            result = tick_city(conn, city_id, body.get("presetId"))
            return jsonify(result)
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/cadence", methods=["PATCH"])
    def society_cadence(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            sec = body.get("solverCadenceNarrativeSeconds")
            if sec is None:
                return jsonify({"error": "solverCadenceNarrativeSeconds required"}), 400
            conn.execute(
                "UPDATE society_cities SET solver_cadence_narrative_seconds = ?, updated_at = ? WHERE city_id = ?",
                (float(sec), _now(), city_id),
            )
            conn.commit()
            return jsonify({"ok": True, "solverCadenceNarrativeSeconds": sec})
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/report", methods=["GET"])
    def society_report(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            snap_cur = conn.execute(
                "SELECT snapshot_json FROM society_snapshots WHERE city_id = ? ORDER BY tick_index DESC LIMIT 1",
                (city_id,),
            )
            snap = snap_cur.fetchone()
            cfg_cur = conn.execute("SELECT * FROM city_config WHERE city_id = ?", (city_id,))
            cfg = cfg_cur.fetchone()
            pop = compute_population_support(
                set(),
                float(cfg["annual_budget_usd"]) if cfg else 10_000_000,
                json.loads(cfg["commodity_indices_json"] or "{}") if cfg else {},
            )
            snapshot = json.loads(snap["snapshot_json"]) if snap else {}
            return jsonify(
                {
                    "cityId": city_id,
                    "snapshot": snapshot,
                    "populationSupport": pop,
                    "lobbyistDelta": snapshot.get("lobbyistTaxDelta", 0),
                }
            )
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/network", methods=["GET"])
    def society_city_network(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT * FROM city_network_bindings WHERE city_id = ?", (city_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(
                {
                    "cityId": city_id,
                    "networkId": row["network_id"],
                    "ipv6CityPrefix": row["ipv6_city_prefix"],
                    "gatewayDeviceId": row["gateway_device_id"],
                    "planetNetworkId": row["planet_network_id"],
                }
            )
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/devices", methods=["GET"])
    def society_city_devices(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT network_id FROM city_network_bindings WHERE city_id = ?", (city_id,))
            binding = cur.fetchone()
            if not binding:
                return jsonify({"items": []})
            dcur = conn.execute(
                "SELECT id, display_name, ipv6_full, network_id FROM telecom_devices WHERE network_id = ?",
                (binding["network_id"],),
            )
            items = [
                {"id": r["id"], "displayName": r["display_name"], "ipv6Full": r["ipv6_full"], "networkId": r["network_id"]}
                for r in dcur.fetchall()
            ]
            return jsonify({"items": items})
        finally:
            conn.close()

    @app.route("/api/society/planets/<planet_id>/network-topology", methods=["GET"])
    def society_planet_topology(planet_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            pcur = conn.execute("SELECT default_network_id FROM society_planets WHERE planet_id = ?", (planet_id,))
            planet = pcur.fetchone()
            ccur = conn.execute("SELECT city_id, network_id, ipv6_city_prefix FROM society_cities WHERE planet_id = ?", (planet_id,))
            cities = [dict(r) for r in ccur.fetchall()]
            return jsonify({"planetId": planet_id, "planetNetworkId": planet["default_network_id"] if planet else None, "cities": cities})
        finally:
            conn.close()

    @app.route("/api/society/building-types", methods=["GET"])
    def society_building_types():
        conn = get_conn()
        try:
            _ensure(conn)
            pc = request.args.get("propertyClass")
            if pc:
                cur = conn.execute(
                    "SELECT * FROM building_type_maps WHERE property_class = ? ORDER BY priority DESC",
                    (pc,),
                )
            else:
                cur = conn.execute("SELECT * FROM building_type_maps ORDER BY display_name")
            items = []
            for r in cur.fetchall():
                items.append(
                    {
                        "buildingTypeId": r["building_type_id"],
                        "displayName": r["display_name"],
                        "propertyClass": r["property_class"],
                        "prefabId": r["prefab_id"],
                        "defaultOpexUsd": r["default_opex_usd"],
                    }
                )
            return jsonify({"items": items})
        finally:
            conn.close()

    @app.route("/api/society/building-types/<building_type_id>", methods=["GET", "PUT"])
    def society_building_type(building_type_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                resolved = resolve_building_type(conn, building_type_id)
                if not resolved:
                    return jsonify({"error": "not found"}), 404
                return jsonify(resolved)
            body = request.get_json(force=True) or {}
            now = _now()
            conn.execute(
                """INSERT INTO building_type_maps
                   (building_type_id, display_name, property_class, prefab_id, lemma_entry_id,
                    default_opex_usd, service_profile_json, priority, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, '{}', 0, ?)
                   ON CONFLICT(building_type_id) DO UPDATE SET
                     display_name=excluded.display_name,
                     property_class=excluded.property_class,
                     prefab_id=excluded.prefab_id,
                     updated_at=excluded.updated_at""",
                (
                    building_type_id,
                    body.get("displayName", building_type_id),
                    body.get("propertyClass", "public"),
                    body.get("prefabId"),
                    body.get("lemmaEntryId"),
                    body.get("defaultOpexUsd", 0),
                    now,
                ),
            )
            conn.commit()
            return jsonify(resolve_building_type(conn, building_type_id))
        finally:
            conn.close()

    @app.route("/api/society/building-types/resolve", methods=["POST"])
    def society_resolve_building_type():
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            if body.get("buildingTypeId"):
                resolved = resolve_building_type(conn, body["buildingTypeId"])
            else:
                resolved = resolve_for_zone(conn, body.get("propertyClass", "public"), body.get("zoneId"))
            if not resolved:
                return jsonify({"error": "no match"}), 404
            return jsonify(resolved)
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/building-registry", methods=["GET", "POST"])
    def society_building_registry(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM building_registry WHERE city_id = ?", (city_id,))
                items = []
                for r in cur.fetchall():
                    d = dict(r)
                    if d.get("building_type_id"):
                        d["resolved"] = resolve_building_type(conn, d["building_type_id"])
                    items.append(d)
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            sid = body.get("stableId") or new_id()
            now = _now()
            btype = body.get("buildingTypeId", "city_hall")
            resolved = resolve_building_type(conn, btype)
            conn.execute(
                """INSERT INTO building_registry
                   (stable_id, city_id, building_type_id, zone_id, property_class, display_name,
                    pin_local_x, pin_local_z, opex_usd, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    sid,
                    city_id,
                    btype,
                    body.get("zoneId"),
                    (resolved or {}).get("propertyClass", "public"),
                    body.get("displayName") or (resolved or {}).get("displayName", sid),
                    body.get("pinLocalX"),
                    body.get("pinLocalZ"),
                    body.get("opexUsd") or (resolved or {}).get("defaultOpexUsd", 0),
                    now,
                    now,
                ),
            )
            provision_building_device(conn, city_id, sid, body.get("displayName", sid), body.get("causalityLeafId"))
            conn.commit()
            return jsonify({"stableId": sid}), 201
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/buildings/<stable_id>", methods=["PATCH"])
    def society_building_patch(city_id: str, stable_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            now = _now()
            conn.execute(
                """UPDATE building_registry SET
                   pin_local_x = COALESCE(?, pin_local_x),
                   pin_local_z = COALESCE(?, pin_local_z),
                   zone_id = COALESCE(?, zone_id),
                   building_type_id = COALESCE(?, building_type_id),
                   updated_at = ?
                   WHERE stable_id = ? AND city_id = ?""",
                (
                    body.get("pinLocalX"),
                    body.get("pinLocalZ"),
                    body.get("zoneId"),
                    body.get("buildingTypeId"),
                    now,
                    stable_id,
                    city_id,
                ),
            )
            conn.commit()
            return jsonify({"ok": True})
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/buildings/<stable_id>/prompt", methods=["POST"])
    def society_building_prompt(city_id: str, stable_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            action = body.get("action", "sync")
            if action == "sync":
                return jsonify(sync_building(conn, city_id, stable_id))
            if action == "merge":
                return jsonify(
                    {
                        "merged": merge_building_stats(
                            float(body.get("local", 0)),
                            float(body.get("remote", 0)),
                            float(body.get("confidence", 0.8)),
                        )
                    }
                )
            if action == "request":
                return jsonify(request_building_prompt(conn, body.get("prompt", ""), city_id))
            return jsonify({"error": "unknown action"}), 400
        finally:
            conn.close()

    @app.route("/api/society/scenarios/<preset_id>/apply", methods=["POST"])
    def society_scenario(preset_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            city_id = body.get("cityId")
            if not city_id:
                return jsonify({"error": "cityId required"}), 400
            result = tick_city(conn, city_id, preset_id)
            return jsonify(result)
        finally:
            conn.close()

    @app.route("/api/society/cities/<city_id>/conditions/prompt", methods=["GET"])
    def society_conditions_prompt(city_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute(
                "SELECT snapshot_json FROM society_snapshots WHERE city_id = ? ORDER BY tick_index DESC LIMIT 1",
                (city_id,),
            )
            row = cur.fetchone()
            snap = json.loads(row["snapshot_json"]) if row else {}
            ccur = conn.execute("SELECT display_name FROM society_cities WHERE city_id = ?", (city_id,))
            city = ccur.fetchone()
            name = city["display_name"] if city else city_id
            text = (
                f"{name}: tax burden {snap.get('taxRate', 0):.1%}, "
                f"healthcare coverage {snap.get('healthcareCoverage', 0):.0%}, "
                f"lobbyist activity {snap.get('lobbyistActivity', 0):.2f}, "
                f"congress stability {snap.get('congressStability', 0):.0%}."
            )
            return jsonify({"cityId": city_id, "prompt": text, "snapshot": snap})
        finally:
            conn.close()

    @app.route("/api/society/reports/population-support", methods=["GET"])
    def society_report_population():
        city_id = request.args.get("cityId")
        conn = get_conn()
        try:
            _ensure(conn)
            cfg_cur = conn.execute("SELECT * FROM city_config WHERE city_id = ?", (city_id,))
            cfg = cfg_cur.fetchone()
            pop = compute_population_support(
                set(),
                float(cfg["annual_budget_usd"]) if cfg else 10_000_000,
                json.loads(cfg["commodity_indices_json"] or "{}") if cfg else {},
            )
            return jsonify(pop)
        finally:
            conn.close()

    @app.route("/api/society/merge", methods=["POST"])
    def society_merge_api():
        from society_merge import merge_routing_trees, merge_snapshots

        body = request.get_json(force=True) or {}
        kind = body.get("kind", "snapshot")
        if kind == "routing":
            return jsonify(merge_routing_trees(body.get("server", {}), body.get("client", {})))
        return jsonify(merge_snapshots(body.get("server", {}), body.get("client", {})))
