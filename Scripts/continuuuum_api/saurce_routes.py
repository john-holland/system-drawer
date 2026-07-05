"""Saurce: products, game profiles, preorders, investment ledger, crypto & ACH stubs."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE
from commerce_db import DEFAULT_FOUNDATION_ID, ensure_cave_commerce_tables, new_id

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]

PRODUCT_TYPES = frozenset(
    {"game", "app", "software", "utility", "media", "toy", "vehicle", "robot", "misc"}
)
CLEARED_GATE_STATUSES = frozenset(
    {"cleared", "cleared_with_license", "cleared_via_design_around", "waived"}
)


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _jload(raw: str | None) -> Any:
    if not raw:
        return None
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return None


def _product_row(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    return {
        "id": d["id"],
        "slug": d["slug"],
        "name": d["name"],
        "type": d["type"],
        "description": d.get("description"),
        "continuuuumAssetId": d.get("continuuuum_asset_id"),
        "lemmaPackageId": d.get("lemma_package_id"),
        "mediaRightsAssetId": d.get("media_rights_asset_id"),
        "primaryLegalCaseId": d.get("primary_legal_case_id"),
        "priceTag": _jload(d.get("price_tag_json")),
        "subscription": _jload(d.get("subscription_json")),
        "preorder": _jload(d.get("preorder_json")),
        "investment": _jload(d.get("investment_json")),
        "gameProfile": _jload(d.get("game_profile_json")),
        "publishStatus": d.get("publish_status"),
        "createdAt": d.get("created_at"),
        "updatedAt": d.get("updated_at"),
    }


def _preorder_gate_ok(conn: sqlite3.Connection) -> bool:
    row = conn.execute(
        "SELECT status FROM platform_feature_gates WHERE feature_key = ?",
        (PLATFORM_PREORDER_FEATURE,),
    ).fetchone()
    return bool(row and row["status"] in CLEARED_GATE_STATUSES)


def _ledger(conn: sqlite3.Connection, entry_type: str, product_id: str | None, **kw: Any) -> str:
    lid = new_id()
    gross = kw.get("gross_amount")
    net = kw.get("net_amount")
    conn.execute(
        """INSERT INTO saurce_ledger_entries
           (id, entry_type, product_id, position_id, gross_amount, net_amount, investor_pool_amount,
            currency, idempotency_key, meta_json, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            lid,
            entry_type,
            product_id,
            kw.get("position_id"),
            gross,
            net,
            kw.get("investor_pool_amount"),
            kw.get("currency") or "USD",
            kw.get("idempotency_key"),
            json.dumps(kw.get("meta") or {}),
            _now(),
        ),
    )
    try:
        from continuuuum_api.production_journal_bridge import mirror_saurce_ledger_to_resaurce
    except ImportError:
        from production_journal_bridge import mirror_saurce_ledger_to_resaurce
    mirror_saurce_ledger_to_resaurce(
        lid,
        entry_type,
        product_id,
        gross_amount=gross,
        net_amount=net,
        budget_plan_id=kw.get("budget_plan_id"),
        story_id=kw.get("story_id"),
        work_order_id=kw.get("work_order_id"),
    )
    return lid


def register_saurce_routes(app, get_conn: GetConn, get_current_user: GetUser) -> None:
    @app.before_request
    def _ensure():
        if not getattr(app, "_saurce_ready", False):
            ensure_cave_commerce_tables(get_conn())
            app._saurce_ready = True

    @app.route("/api/saurce/products", methods=["GET", "POST"])
    def saurce_products():
        conn = get_conn()
        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            ptype = body.get("type") or "misc"
            if ptype not in PRODUCT_TYPES:
                return jsonify({"error": "invalid type"}), 400
            pid = new_id()
            now = _now()
            slug = (body.get("slug") or body.get("name") or pid).lower().replace(" ", "-")[:64]
            game_profile = body.get("gameProfile")
            if ptype == "game" and not game_profile:
                game_profile = {
                    "playModes": {"singlePlayer": True, "multiplayer": False},
                    "singlePlayerConfig": {"offlineCapable": True},
                }
            conn.execute(
                """INSERT INTO saurce_products
                   (id, slug, name, type, description, continuuuum_asset_id, lemma_package_id,
                    price_tag_json, subscription_json, game_profile_json, publish_status, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    pid,
                    slug,
                    body.get("name") or slug,
                    ptype,
                    body.get("description"),
                    body.get("continuuuumAssetId"),
                    body.get("lemmaPackageId"),
                    json.dumps(body.get("priceTag")) if body.get("priceTag") else None,
                    json.dumps(body.get("subscription")) if body.get("subscription") else None,
                    json.dumps(game_profile) if game_profile else None,
                    "draft",
                    now,
                    now,
                ),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM saurce_products WHERE id = ?", (pid,)).fetchone()
            conn.close()
            return jsonify(_product_row(row)), 201
        ptype = request.args.get("type")
        q = "SELECT * FROM saurce_products"
        params: list[Any] = []
        if ptype:
            q += " WHERE type = ?"
            params.append(ptype)
        rows = conn.execute(q + " ORDER BY created_at DESC", params).fetchall()
        conn.close()
        return jsonify({"items": [_product_row(r) for r in rows]}), 200

    @app.route("/api/saurce/products/<product_id>", methods=["GET", "PATCH"])
    def saurce_product_detail(product_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404
        if request.method == "PATCH":
            body = request.get_json(silent=True) or {}
            updates = []
            params: list[Any] = []
            mapping = {
                "priceTag": "price_tag_json",
                "subscription": "subscription_json",
                "preorder": "preorder_json",
                "investment": "investment_json",
                "gameProfile": "game_profile_json",
            }
            for key, col in mapping.items():
                if key in body:
                    updates.append(f"{col} = ?")
                    params.append(json.dumps(body[key]) if body[key] is not None else None)
            if updates:
                params.extend([_now(), product_id])
                conn.execute(
                    f"UPDATE saurce_products SET {', '.join(updates)}, updated_at = ? WHERE id = ?",
                    params,
                )
                conn.commit()
            row = conn.execute("SELECT * FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        conn.close()
        return jsonify(_product_row(row)), 200

    @app.route("/api/saurce/products/<product_id>/game-profile", methods=["PATCH"])
    def saurce_game_profile(product_id: str):
        body = request.get_json(silent=True) or {}
        pm = body.get("playModes") or {}
        if not pm.get("singlePlayer") and not pm.get("multiplayer"):
            return jsonify({"error": "at least one play mode required"}), 400
        conn = get_conn()
        conn.execute(
            "UPDATE saurce_products SET game_profile_json = ?, updated_at = ? WHERE id = ?",
            (json.dumps(body), _now(), product_id),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200

    @app.route("/api/saurce/products/<product_id>/compliance-status")
    def saurce_compliance(product_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        gp = _jload(row["game_profile_json"]) if row else None
        cases = conn.execute(
            """SELECT * FROM legal_cases WHERE saurce_product_id = ? AND status IN ('open', 'investigating')
               AND severity IN ('high', 'critical')""",
            (product_id,),
        ).fetchall()
        blocked = len(cases) > 0
        conn.close()
        return jsonify({"productId": product_id, "blocked": blocked, "openLegalCases": len(cases), "gameProfile": gp}), 200

    @app.route("/api/saurce/products/<product_id>/preorder", methods=["PATCH"])
    def saurce_preorder_config(product_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        if body.get("enabled") and not _preorder_gate_ok(conn):
            conn.close()
            return jsonify(
                {"error": "preorder patent blocked", "code": "preorder_patent_blocked", "legalCaseId": BUILTIN_PREORDER_CASE_ID}
            ), 409
        pre = body.copy()
        pre["legalCaseId"] = BUILTIN_PREORDER_CASE_ID
        pre["status"] = "active" if body.get("enabled") and _preorder_gate_ok(conn) else body.get("status", "draft")
        conn.execute(
            "UPDATE saurce_products SET preorder_json = ?, updated_at = ? WHERE id = ?",
            (json.dumps(pre), _now(), product_id),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "preorder": pre}), 200

    @app.route("/api/saurce/products/<product_id>/preorder/status")
    def saurce_preorder_status(product_id: str):
        conn = get_conn()
        row = conn.execute("SELECT preorder_json FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        count = conn.execute(
            "SELECT COUNT(*) AS c FROM saurce_preorder_reservations WHERE product_id = ? AND status = 'reserved'",
            (product_id,),
        ).fetchone()
        gate = conn.execute(
            "SELECT status FROM platform_feature_gates WHERE feature_key = ?",
            (PLATFORM_PREORDER_FEATURE,),
        ).fetchone()
        conn.close()
        return jsonify(
            {
                "preorder": _jload(row["preorder_json"]) if row else None,
                "unitsReserved": count["c"] if count else 0,
                "legalGateStatus": gate["status"] if gate else "blocked",
            }
        ), 200

    @app.route("/api/saurce/products/<product_id>/preorder/investment", methods=["PATCH"])
    def saurce_preorder_investment(product_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        row = conn.execute("SELECT preorder_json FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        pre = (_jload(row["preorder_json"]) if row else None) or {}
        pre["investment"] = body
        conn.execute(
            "UPDATE saurce_products SET preorder_json = ?, updated_at = ? WHERE id = ?",
            (json.dumps(pre), _now(), product_id),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "investment": body}), 200

    @app.route("/api/saurce/products/<product_id>/preorder/reserve", methods=["POST"])
    def saurce_preorder_reserve(product_id: str):
        body = request.get_json(silent=True) or {}
        user = get_current_user()
        conn = get_conn()
        row = conn.execute("SELECT * FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404
        pre = _jload(row["preorder_json"]) or {}
        if not pre.get("enabled"):
            conn.close()
            return jsonify({"error": "preorder not enabled"}), 400
        deposit = float(body.get("depositPaid") or pre.get("depositAmount") or 0)
        inv_amt = float(body.get("investmentAmount") or 0)
        tier = body.get("tier") or ("investor_backer" if inv_amt > 0 else "standard")
        rid = new_id()
        discount = 0.0
        inv = pre.get("investment") or {}
        cc = inv.get("customerCrowdfund") or {}
        if cc.get("enabled"):
            pct = float(cc.get("baselineDiscountPercent") or 0)
            price = _jload(row["price_tag_json"]) or {}
            discount = float(price.get("amount") or 0) * pct / 100.0
        pos_id = None
        if inv_amt > 0:
            pos_id = new_id()
            conn.execute(
                """INSERT INTO saurce_investment_positions
                   (id, product_id, investor_account_id, position_type, committed_amount, currency, status, created_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                (pos_id, product_id, user, "preorder_backer", inv_amt, "USD", "active", _now()),
            )
            _ledger(conn, "preorder_backer_stake", product_id, gross_amount=inv_amt, position_id=pos_id)
        conn.execute(
            """INSERT INTO saurce_preorder_reservations
               (id, product_id, user_id, tier, deposit_paid, investment_amount, discount_applied, investment_position_id, status, created_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (rid, product_id, user, tier, deposit, inv_amt or None, discount, pos_id, "reserved", _now()),
        )
        if deposit > 0:
            _ledger(conn, "preorder_deposit", product_id, gross_amount=deposit, meta={"reservationId": rid})
        conn.commit()
        conn.close()
        return jsonify(
            {
                "id": rid,
                "tier": tier,
                "discountApplied": discount,
                "investmentPositionId": pos_id,
                "expectedReturnSummary": {
                    "guaranteedDiscount": discount,
                    "upsideEligible": bool(cc.get("upsidePoolPercent")),
                },
            }
        ), 201

    @app.route("/api/saurce/products/<product_id>/preorder/fulfill", methods=["POST"])
    def saurce_preorder_fulfill(product_id: str):
        conn = get_conn()
        conn.execute(
            "UPDATE saurce_preorder_reservations SET status = 'fulfilled' WHERE product_id = ? AND status = 'reserved'",
            (product_id,),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200

    @app.route("/api/saurce/products/<product_id>/preorder/accrue-upside", methods=["POST"])
    def saurce_preorder_accrue(product_id: str):
        body = request.get_json(silent=True) or {}
        net = float(body.get("netAmount") or 0)
        conn = get_conn()
        row = conn.execute("SELECT preorder_json FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
        inv = (_jload(row["preorder_json"]) or {}).get("investment") or {}
        cc = inv.get("customerCrowdfund") or {}
        pool_pct = float(cc.get("upsidePoolPercent") or 0)
        pool = net * pool_pct / 100.0
        positions = conn.execute(
            """SELECT * FROM saurce_investment_positions
               WHERE product_id = ? AND position_type = 'preorder_backer' AND status = 'active'""",
            (product_id,),
        ).fetchall()
        total = sum(float(p["committed_amount"]) for p in positions) or 1.0
        max_mult = float(cc.get("maxBackerReturnMultiple") or 2.0)
        for p in positions:
            share = pool * float(p["committed_amount"]) / total
            capped = min(share, float(p["committed_amount"]) * max_mult)
            _ledger(
                conn,
                "preorder_backer_upside_accrual",
                product_id,
                net_amount=capped,
                position_id=p["id"],
                investor_pool_amount=pool,
            )
        if body.get("micropaymentStub"):
            _ledger(
                conn,
                "preorder_micropayment_accrual",
                product_id,
                net_amount=float(body.get("micropaymentAmount") or 0),
                meta={"attribution": "lanier-stub"},
            )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "poolAmount": pool}), 200

    @app.route("/api/saurce/products/<product_id>/investment-terms", methods=["POST"])
    def saurce_investment_terms(product_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        conn.execute(
            "UPDATE saurce_products SET investment_json = ?, updated_at = ? WHERE id = ?",
            (json.dumps(body), _now(), product_id),
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200

    @app.route("/api/saurce/investments/positions", methods=["GET", "POST"])
    def saurce_positions():
        conn = get_conn()
        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            pid = new_id()
            conn.execute(
                """INSERT INTO saurce_investment_positions
                   (id, product_id, investor_account_id, position_type, ownership_percent, committed_amount, currency, status, created_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    pid,
                    body["productId"],
                    body.get("investorAccountId") or get_current_user(),
                    body.get("positionType") or "standard",
                    float(body.get("ownershipPercent") or 0),
                    float(body.get("committedAmount") or 0),
                    body.get("currency") or "USD",
                    "active",
                    _now(),
                ),
            )
            conn.commit()
            conn.close()
            return jsonify({"id": pid}), 201
        product_id = request.args.get("productId")
        ptype = request.args.get("type")
        q = "SELECT * FROM saurce_investment_positions WHERE 1=1"
        params: list[Any] = []
        if product_id:
            q += " AND product_id = ?"
            params.append(product_id)
        if ptype:
            q += " AND position_type = ?"
            params.append(ptype)
        rows = conn.execute(q, params).fetchall()
        conn.close()
        return jsonify({"items": [dict(r) for r in rows]}), 200

    @app.route("/api/saurce/investments/ledger")
    def saurce_ledger():
        conn = get_conn()
        product_id = request.args.get("productId")
        q = "SELECT * FROM saurce_ledger_entries"
        params: list[Any] = []
        if product_id:
            q += " WHERE product_id = ?"
            params.append(product_id)
        rows = conn.execute(q + " ORDER BY created_at DESC", params).fetchall()
        conn.close()
        return jsonify({"items": [dict(r) for r in rows]}), 200

    @app.route("/api/saurce/investments/disburse", methods=["POST"])
    def saurce_disburse():
        body = request.get_json(silent=True) or {}
        rail = body.get("rail") or "crypto"
        conn = get_conn()
        if rail == "ach":
            payout_id = f"ach-stub-{new_id()[:8]}"
            _ledger(conn, "disbursement", body.get("productId"), meta={"payoutId": payout_id, "rail": "ach"})
            conn.commit()
            conn.close()
            return jsonify({"payoutId": payout_id, "status": "scheduled", "rail": "ach"}), 200
        tx = f"0x{new_id().replace('-', '')[:40]}"
        _ledger(conn, "disbursement", body.get("productId"), meta={"txHash": tx, "rail": "crypto"})
        conn.commit()
        conn.close()
        return jsonify({"txHash": tx, "status": "pending", "rail": "crypto"}), 200

    @app.route("/api/saurce/foundation/<foundation_id>/allocate", methods=["POST"])
    def saurce_foundation_allocate(foundation_id: str):
        body = request.get_json(silent=True) or {}
        amount = float(body.get("amount") or 0)
        product_id = body.get("productId")
        conn = get_conn()
        _ledger(
            conn,
            "foundation_allocation",
            product_id,
            gross_amount=amount,
            meta={"foundationId": foundation_id, "asset": body.get("asset") or "USDC"},
        )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "amount": amount, "foundationId": foundation_id}), 200

    @app.route("/api/saurce/crypto/disburse", methods=["POST"])
    def saurce_crypto_disburse():
        body = request.get_json(silent=True) or {}
        tx = f"0x{new_id().replace('-', '')[:40]}"
        conn = get_conn()
        _ledger(
            conn,
            "disbursement",
            None,
            meta={"txHash": tx, "wallet": body.get("walletAddress"), "asset": body.get("asset")},
        )
        conn.commit()
        conn.close()
        return jsonify({"txHash": tx, "status": "pending"}), 200

    @app.route("/api/saurce/ach/disburse", methods=["POST"])
    def saurce_ach_disburse():
        payout_id = f"square-stub-{new_id()[:8]}"
        return jsonify({"payoutId": payout_id, "status": "scheduled", "provider": "square-stub"}), 200

    @app.route("/api/saurce/ach/payouts/<payout_id>")
    def saurce_ach_payout_status(payout_id: str):
        return jsonify({"payoutId": payout_id, "status": "completed", "provider": "square-stub"}), 200

    @app.route("/api/saurce/foundation")
    def saurce_list_foundation():
        conn = get_conn()
        rows = conn.execute("SELECT * FROM saurce_safe_crypto_foundations").fetchall()
        conn.close()
        return jsonify({"items": [dict(r) for r in rows], "defaultId": DEFAULT_FOUNDATION_ID}), 200
