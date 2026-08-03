"""Company + keycard Continuuuum routes and /keycards page."""

from __future__ import annotations

from pathlib import Path
from typing import Any, Callable

from flask import Flask, jsonify, request, send_from_directory

GetConn = Callable[[], Any]
STATIC_KEYCARDS = Path(__file__).resolve().parent / "static" / "keycards"

_COMPANIES: dict[str, dict[str, Any]] = {}
_KEYCARDS: dict[str, dict[str, Any]] = {}


def register_hospitality_security_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/keycards")
    @app.route("/keycards/<path:subpath>")
    def serve_keycards(subpath=None):
        if subpath and subpath != "index.html":
            return send_from_directory(STATIC_KEYCARDS, subpath)
        return send_from_directory(STATIC_KEYCARDS, "index.html")

    @app.route("/api/civil/companies", methods=["GET"])
    def api_companies_list():
        return jsonify({"companies": list(_COMPANIES.values())})

    @app.route("/api/civil/companies/<company_id>", methods=["GET", "PUT"])
    def api_company(company_id: str):
        if request.method == "GET":
            row = _COMPANIES.get(company_id)
            if not row:
                return jsonify({"error": "not_found", "companyId": company_id}), 404
            return jsonify({"company": row})
        body = request.get_json(silent=True) or {}
        company = body.get("company") or body
        row = {
            "companyId": company_id,
            "displayName": company.get("displayName") or company_id,
            "parentCompanyId": company.get("parentCompanyId") or "",
            "fundingSources": company.get("fundingSources") or [],
            "staff": company.get("staff") or [],
        }
        _COMPANIES[company_id] = row
        return jsonify({"company": row})

    @app.route("/api/civil/keycards", methods=["GET", "PUT"])
    def api_keycards():
        if request.method == "GET":
            keycard_id = request.args.get("keycardId")
            rows = list(_KEYCARDS.values())
            if keycard_id:
                rows = [r for r in rows if r.get("keycardId") == keycard_id]
            return jsonify({"keycards": rows})
        body = request.get_json(silent=True) or {}
        items = body.get("keycards")
        if isinstance(items, list):
            for item in items:
                kid = item.get("keycardId")
                if not kid:
                    continue
                _KEYCARDS[kid] = {
                    "keycardId": kid,
                    "boundNodeId": item.get("boundNodeId") or "",
                    "allowedNodeIds": item.get("allowedNodeIds") or [],
                    "actorIdsAtNode": item.get("actorIdsAtNode") or [],
                    "label": item.get("label") or kid,
                }
            return jsonify({"keycards": list(_KEYCARDS.values())})
        kid = body.get("keycardId")
        if not kid:
            return jsonify({"error": "keycardId required"}), 400
        _KEYCARDS[kid] = {
            "keycardId": kid,
            "boundNodeId": body.get("boundNodeId") or "",
            "allowedNodeIds": body.get("allowedNodeIds") or [],
            "actorIdsAtNode": body.get("actorIdsAtNode") or [],
            "label": body.get("label") or kid,
        }
        return jsonify({"keycard": _KEYCARDS[kid]})

    @app.route("/api/civil/hospitality-meta", methods=["GET"])
    def api_hospitality_meta():
        return jsonify(
            {
                "discoveryTokens": [
                    "hospitality",
                    "keycards",
                    "companies",
                    "nightclub",
                    "hotel",
                    "embassy",
                    "barber",
                ],
                "venues": [
                    "NightClub",
                    "Bar",
                    "Inn",
                    "Hotel",
                    "MilitaryCheckpoint",
                    "SpyAgency",
                    "Embassy",
                    "GovLegislative",
                    "Monarchic",
                    "Spa",
                    "PrivateIndustry",
                    "BarberShop",
                ],
            }
        )
