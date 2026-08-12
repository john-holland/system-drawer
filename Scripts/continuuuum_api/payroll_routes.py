"""Continuuuum company income payroll API + SPA."""

from __future__ import annotations

import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]
IsAdmin = Callable[[], bool]

ADMIN_COMPANY_FIELDS = frozenset(
    {"highWaterMarkUsd", "high_water_mark_usd", "hwmRetainerPct", "hwm_retainer_pct"}
)
ADMIN_RETAINER_AMOUNT_FIELDS = frozenset(
    {"amountUsd", "amount_usd", "percent", "amountLocked", "amount_locked"}
)


def _default_is_admin() -> bool:
    return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")


def _require_admin_for_keys(body: dict[str, Any], keys: frozenset[str], is_admin: IsAdmin):
    """Return 403 response tuple if body touches admin-only keys without admin."""
    if any(k in body for k in keys) and not is_admin():
        return jsonify({"error": "admin required to change HWM or retainer amounts"}), 403
    return None

try:
    from continuuuum_api.payroll_engine import (
        add_member,
        company_summary,
        compute_service_budget,
        create_company,
        create_retainer,
        delete_member,
        delete_retainer,
        draw_retainer,
        ensure_payroll_schema,
        get_company,
        list_companies,
        list_events,
        list_members,
        list_retainers,
        patch_company,
        patch_member,
        patch_retainer,
        post_income,
    )
except ImportError:
    from payroll_engine import (
        add_member,
        company_summary,
        compute_service_budget,
        create_company,
        create_retainer,
        delete_member,
        delete_retainer,
        draw_retainer,
        ensure_payroll_schema,
        get_company,
        list_companies,
        list_events,
        list_members,
        list_retainers,
        patch_company,
        patch_member,
        patch_retainer,
        post_income,
    )

def register_payroll_routes(
    app: Any, get_conn: GetConn, is_admin: IsAdmin | None = None
) -> None:
    admin_fn = is_admin or _default_is_admin
    static_dir = Path(__file__).resolve().parent / "static" / "payroll"

    @app.route("/payroll")
    @app.route("/payroll/")
    @app.route("/payroll/<path:subpath>")
    def payroll_spa(subpath: str = ""):
        from flask import send_from_directory

        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")

    @app.before_request
    def _ensure_payroll():
        if not getattr(app, "_payroll_ready", False):
            conn = get_conn()
            try:
                ensure_payroll_schema(conn)
            finally:
                conn.close()
            app._payroll_ready = True

    @app.route("/api/payroll/companies", methods=["GET", "POST"])
    def payroll_companies():
        conn = get_conn()
        try:
            if request.method == "POST":
                body = request.get_json(silent=True) or {}
                name = (body.get("name") or "").strip()
                if not name:
                    return jsonify({"error": "name required"}), 400
                try:
                    from continuuuum_api.payroll_engine import (
                        DEFAULT_HWM_RETAINER_PCT,
                        DEFAULT_HWM_USD,
                    )
                except ImportError:
                    from payroll_engine import (  # type: ignore
                        DEFAULT_HWM_RETAINER_PCT,
                        DEFAULT_HWM_USD,
                    )
                try:
                    company = create_company(
                        conn,
                        name=name,
                        saurce_product_id=body.get("saurceProductId"),
                        high_water_mark_usd=float(
                            body.get("highWaterMarkUsd")
                            if body.get("highWaterMarkUsd") is not None
                            else DEFAULT_HWM_USD
                        ),
                        hwm_retainer_pct=float(
                            body.get("hwmRetainerPct")
                            if body.get("hwmRetainerPct") is not None
                            else DEFAULT_HWM_RETAINER_PCT
                        ),
                    )
                except ValueError as e:
                    return jsonify({"error": str(e)}), 400
                return jsonify(company), 201
            return jsonify({"companies": list_companies(conn)})
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>", methods=["GET", "PATCH"])
    def payroll_company(company_id: str):
        conn = get_conn()
        try:
            if request.method == "PATCH":
                body = request.get_json(silent=True) or {}
                denied = _require_admin_for_keys(body, ADMIN_COMPANY_FIELDS, admin_fn)
                if denied is not None:
                    return denied
                try:
                    updated = patch_company(conn, company_id, body)
                except ValueError as e:
                    return jsonify({"error": str(e)}), 400
                if updated is None:
                    return jsonify({"error": "not_found"}), 404
                return jsonify(updated)
            company = get_company(conn, company_id)
            if company is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify(company)
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/summary", methods=["GET"])
    def payroll_summary(company_id: str):
        conn = get_conn()
        try:
            summary = company_summary(conn, company_id)
            if summary is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify(summary)
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/service-budget", methods=["GET"])
    def payroll_service_budget(company_id: str):
        conn = get_conn()
        try:
            try:
                return jsonify(compute_service_budget(conn, company_id))
            except KeyError:
                return jsonify({"error": "not_found"}), 404
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/members", methods=["GET", "POST"])
    def payroll_members(company_id: str):
        conn = get_conn()
        try:
            if request.method == "POST":
                body = request.get_json(silent=True) or {}
                try:
                    member = add_member(conn, company_id, body)
                except KeyError:
                    return jsonify({"error": "not_found"}), 404
                except ValueError as e:
                    return jsonify({"error": str(e)}), 400
                return jsonify(member), 201
            if get_company(conn, company_id) is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"members": list_members(conn, company_id)})
        finally:
            conn.close()

    @app.route(
        "/api/payroll/companies/<company_id>/members/<member_id>",
        methods=["PATCH", "DELETE"],
    )
    def payroll_member(company_id: str, member_id: str):
        conn = get_conn()
        try:
            if request.method == "DELETE":
                ok = delete_member(conn, company_id, member_id)
                if not ok:
                    return jsonify({"error": "not_found"}), 404
                return jsonify({"ok": True})
            body = request.get_json(silent=True) or {}
            updated = patch_member(conn, company_id, member_id, body)
            if updated is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify(updated)
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/retainers", methods=["GET", "POST"])
    def payroll_retainers(company_id: str):
        conn = get_conn()
        try:
            if request.method == "POST":
                body = request.get_json(silent=True) or {}
                try:
                    ret = create_retainer(conn, company_id, body)
                except KeyError:
                    return jsonify({"error": "not_found"}), 404
                except ValueError as e:
                    return jsonify({"error": str(e)}), 400
                return jsonify(ret), 201
            if get_company(conn, company_id) is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"retainers": list_retainers(conn, company_id)})
        finally:
            conn.close()

    @app.route(
        "/api/payroll/companies/<company_id>/retainers/<retainer_id>",
        methods=["PATCH", "DELETE"],
    )
    def payroll_retainer(company_id: str, retainer_id: str):
        conn = get_conn()
        try:
            if request.method == "DELETE":
                try:
                    ok = delete_retainer(conn, company_id, retainer_id)
                except ValueError as e:
                    return jsonify({"error": str(e)}), 400
                if not ok:
                    return jsonify({"error": "not_found"}), 404
                return jsonify({"ok": True})
            body = request.get_json(silent=True) or {}
            denied = _require_admin_for_keys(body, ADMIN_RETAINER_AMOUNT_FIELDS, admin_fn)
            if denied is not None:
                return denied
            try:
                updated = patch_retainer(conn, company_id, retainer_id, body)
            except ValueError as e:
                return jsonify({"error": str(e)}), 400
            if updated is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify(updated)
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/income", methods=["POST"])
    def payroll_income(company_id: str):
        conn = get_conn()
        try:
            body = request.get_json(silent=True) or {}
            if body.get("netUsd") is None and body.get("net_amount") is None:
                return jsonify({"error": "netUsd required"}), 400
            net = float(body.get("netUsd", body.get("net_amount")))
            gross = body.get("grossUsd", body.get("gross_amount"))
            meta = dict(body.get("meta") or {})
            note = body.get("postNote") or body.get("note") or meta.get("postNote")
            if note is not None:
                note = str(note).strip()
                if note:
                    meta["postNote"] = note
                else:
                    meta.pop("postNote", None)
            try:
                event = post_income(
                    conn,
                    company_id,
                    net,
                    gross_amount=float(gross) if gross is not None else None,
                    source=body.get("source") or "manual",
                    idempotency_key=body.get("idempotencyKey"),
                    meta=meta or None,
                )
            except KeyError:
                return jsonify({"error": "not_found"}), 404
            except ValueError as e:
                return jsonify({"error": str(e)}), 400
            return jsonify(event), 201
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/events", methods=["GET"])
    def payroll_events(company_id: str):
        conn = get_conn()
        try:
            if get_company(conn, company_id) is None:
                return jsonify({"error": "not_found"}), 404
            try:
                limit = min(500, max(1, int(request.args.get("limit") or 50)))
                offset = max(0, int(request.args.get("offset") or 0))
            except (TypeError, ValueError):
                return jsonify({"error": "bad limit/offset"}), 400
            items, total = list_events(conn, company_id, limit=limit, offset=offset)
            return jsonify({"items": items, "total": total, "limit": limit, "offset": offset})
        finally:
            conn.close()

    @app.route("/api/payroll/companies/<company_id>/retainer/draw", methods=["POST"])
    def payroll_retainer_draw(company_id: str):
        conn = get_conn()
        try:
            body = request.get_json(silent=True) or {}
            amount = body.get("amountUsd", body.get("amount"))
            if amount is None:
                return jsonify({"error": "amountUsd required"}), 400
            try:
                result = draw_retainer(
                    conn,
                    company_id,
                    amount_usd=float(amount),
                    reason=body.get("reason"),
                )
            except KeyError:
                return jsonify({"error": "not_found"}), 404
            except ValueError as e:
                return jsonify({"error": str(e)}), 400
            return jsonify(result), 201
        finally:
            conn.close()
