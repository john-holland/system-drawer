"""Structured multiplayer chat: surfaces, entitlement, TOS, fee, invites."""

from __future__ import annotations

import sqlite3
from typing import Callable

from flask import jsonify, request

try:
    from continuuuum_api.chat_safety_db import (
        CHAT_HISTORY_LIST_ID,
        activate_entitlement,
        create_invite,
        current_tos,
        ensure_chat_safety_tables,
        evaluate_entitlement,
        evaluate_send,
        invite_by_token,
        lexicon_document,
        list_invites,
        list_session_messages,
        list_warehouse,
        load_chat_surfaces,
        player_profit_balance,
        put_lexicon,
        record_chargeback,
        record_committed_message,
        record_jurisdiction_denied,
        truncate_chat_history,
        withdraw_profit,  # todo review: right placement?
    )
    from continuuuum_api.commerce_db import ensure_cave_commerce_tables
except ImportError:
    from chat_safety_db import (
        CHAT_HISTORY_LIST_ID,
        activate_entitlement,
        create_invite,
        current_tos,
        ensure_chat_safety_tables,
        evaluate_entitlement,
        evaluate_send,
        invite_by_token,
        lexicon_document,
        list_invites,
        list_session_messages,
        list_warehouse,
        load_chat_surfaces,
        player_profit_balance,
        put_lexicon,
        record_chargeback,
        record_committed_message,
        record_jurisdiction_denied,
        truncate_chat_history,
        withdraw_profit,
    )
    from commerce_db import ensure_cave_commerce_tables

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]
IsAdmin = Callable[[], bool]


def register_chat_safety_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    is_admin: IsAdmin,
) -> None:
    @app.before_request
    def _ensure_chat_safety():
        if not getattr(app, "_chat_safety_ready", False):
            conn = get_conn()
            ensure_cave_commerce_tables(conn)
            ensure_chat_safety_tables(conn)
            conn.close()
            app._chat_safety_ready = True

    @app.route("/api/chat/surfaces")
    def chat_surfaces():
        surfaces = load_chat_surfaces()
        return jsonify(
            {
                "items": surfaces,
                "structural": [s for s in surfaces if s.get("class") == "structural_product"],
                "unratedTool": [s for s in surfaces if s.get("class") == "unrated_tool"],
            }
        ), 200

    @app.route("/api/chat/tos/current")
    def chat_tos_current():
        conn = get_conn()
        tos = current_tos(conn)
        conn.close()
        if not tos:
            return jsonify({"error": "tos not published"}), 404
        return jsonify(tos), 200

    @app.route("/api/chat/entitlement")
    def chat_entitlement_get():
        product_id = request.args.get("productId") or request.args.get("product_id")
        user_id = request.args.get("userId") or request.args.get("user_id") or get_user()
        if not product_id or not user_id:
            return jsonify({"error": "productId and userId required"}), 400
        conn = get_conn()
        snap = evaluate_entitlement(
            conn,
            user_id=user_id,
            product_id=product_id,
            jurisdiction=request.args.get("jurisdiction"),
            channel=request.args.get("channel") or "text",
        )
        conn.close()
        return jsonify(snap), 200

    @app.route("/api/chat/entitlement/activate", methods=["POST"])
    def chat_entitlement_activate():
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id") or get_user()
        product_id = body.get("productId") or body.get("product_id")
        if not user_id or not product_id:
            return jsonify({"error": "userId and productId required"}), 400
        conn = get_conn()
        out = activate_entitlement(
            conn,
            user_id=user_id,
            product_id=product_id,
            sole_user_attested=bool(body.get("soleUserAttested") or body.get("sole_user_attested")),
            legal_age_attested=bool(body.get("legalAgeAttested") or body.get("legal_age_attested")),
            tos_version_id=body.get("tosVersionId") or body.get("tos_version_id"),
            signer_ip=request.headers.get("X-Forwarded-For") or request.remote_addr,
            invite_token=body.get("inviteToken") or body.get("token") or request.args.get("token"),
            actor_user_id=get_user(),
        )
        conn.close()
        if not out.get("ok"):
            code = out.get("code") or "tos_not_signed"
            return jsonify({"error": out.get("error"), "code": code}), 403
        return jsonify(out), 200

    @app.route("/api/chat/send", methods=["POST"])
    def chat_send():
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id") or get_user()
        product_id = body.get("productId") or body.get("product_id")
        channel = body.get("channel") or "text"
        jurisdiction = body.get("jurisdiction")
        if not user_id or not product_id:
            return jsonify({"error": "userId and productId required"}), 400
        conn = get_conn()
        snap = evaluate_send(
            conn,
            user_id=user_id,
            product_id=product_id,
            jurisdiction=jurisdiction,
            channel=channel,
            tokens=body.get("tokens"),
            text=body.get("text") or body.get("content"),
        )
        if not snap.get("ok"):
            if snap.get("denyCode") == "chat_disabled_jurisdiction":
                record_jurisdiction_denied(
                    conn,
                    user_id=user_id,
                    product_id=product_id,
                    jurisdiction=jurisdiction,
                    channel=channel,
                )
            conn.close()
            return jsonify({"error": snap.get("denyCode"), "code": snap.get("denyCode"), **snap}), 403
        stored = record_committed_message(
            conn,
            product_id=product_id,
            user_id=user_id,
            session_id=body.get("sessionId") or body.get("session_id"),
            text=body.get("text") or body.get("content") or " ".join(body.get("tokens") or []),
            tokens=body.get("tokens"),
        )
        conn.close()
        return jsonify({"ok": True, "channel": channel, "queued": True, **stored}), 200

    @app.route("/api/chat/lexicon", methods=["GET", "PUT"])
    def chat_lexicon():
        product_id = request.args.get("productId") or request.args.get("product_id")
        if request.method == "PUT":
            body = request.get_json(silent=True) or {}
            product_id = product_id or body.get("productId") or body.get("product_id")
        if not product_id:
            return jsonify({"error": "productId required"}), 400
        conn = get_conn()
        if request.method == "PUT":
            out = put_lexicon(conn, product_id, body)
            conn.close()
            if not out:
                return jsonify({"error": "product not found"}), 404
            return jsonify(out), 200
        doc = lexicon_document(conn, product_id)
        conn.close()
        if not doc:
            return jsonify({"error": "product not found"}), 404
        return jsonify(doc), 200

    @app.route("/api/chat/history")
    def chat_history():
        product_id = request.args.get("productId") or request.args.get("product_id")
        if not product_id:
            return jsonify({"error": "productId required"}), 400
        conn = get_conn()
        items = list_session_messages(
            conn,
            product_id,
            session_id=request.args.get("sessionId") or request.args.get("session_id"),
        )
        conn.close()
        return jsonify({"items": items}), 200

    @app.route("/api/chat/profit")
    def chat_profit():
        user_id = request.args.get("userId") or request.args.get("user_id") or get_user()
        conn = get_conn()
        bal = player_profit_balance(conn, user_id)
        conn.close()
        return jsonify({"userId": user_id, "profitBalanceUsd": bal, "withdrawable": True}), 200

    @app.route("/api/chat/profit/withdraw", methods=["POST"])
    def chat_profit_withdraw():
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id") or get_user()
        amount = body.get("amountUsd") if "amountUsd" in body else body.get("amount")
        if amount is None:
            return jsonify({"error": "amountUsd required"}), 400
        conn = get_conn()
        out = withdraw_profit(conn, user_id=user_id, amount_usd=float(amount), rail=body.get("rail") or "stub")
        conn.close()
        if not out.get("ok"):
            return jsonify(out), 400
        return jsonify(out), 200

    @app.route("/api/chat/invites/<token>")
    def chat_invite_public(token: str):
        conn = get_conn()
        inv = invite_by_token(conn, token)
        tos = current_tos(conn)
        conn.close()
        if not inv:
            return jsonify({"error": "not found"}), 404
        return jsonify({"invite": inv, "tos": tos}), 200

    @app.route("/api/admin/chat/invites", methods=["GET", "POST"])
    def admin_chat_invites():
        if not is_admin():
            return jsonify({"error": "admin only"}), 403
        conn = get_conn()
        if request.method == "GET":
            items = list_invites(conn)
            conn.close()
            return jsonify({"items": items}), 200
        body = request.get_json(silent=True) or {}
        email = (body.get("email") or "").strip()
        product_id = body.get("productId") or body.get("product_id")
        if not email or not product_id:
            conn.close()
            return jsonify({"error": "email and productId required"}), 400
        out = create_invite(
            conn,
            email=email,
            product_id=product_id,
            created_by_admin=get_user(),
            user_id=body.get("userId") or body.get("user_id"),
            pay_for_them=bool(body.get("payForThem") or body.get("pay_for_them")),
            payer_legal_entity=body.get("payerLegalEntity") or body.get("legalEntity"),
            invite_base=body.get("inviteBase") or request.host_url.rstrip("/"),
        )
        conn.close()
        return jsonify(out), 201

    @app.route("/api/admin/chat/warehouse")
    def admin_chat_warehouse():
        if not is_admin():
            return jsonify({"error": "admin only"}), 403
        conn = get_conn()
        list_id = request.args.get("listId") or request.args.get("list_id")
        if request.args.get("kind") == "history":
            list_id = list_id or CHAT_HISTORY_LIST_ID
        items = list_warehouse(conn, list_id=list_id)
        conn.close()
        return jsonify({"items": items}), 200

    @app.route("/api/admin/chat/chargeback", methods=["POST"])
    def admin_chat_chargeback():
        if not is_admin():
            return jsonify({"error": "admin only"}), 403
        body = request.get_json(silent=True) or {}
        user_id = body.get("userId") or body.get("user_id")
        product_id = body.get("productId") or body.get("product_id")
        if not user_id or not product_id:
            return jsonify({"error": "userId and productId required"}), 400
        conn = get_conn()
        out = record_chargeback(
            conn,
            user_id=user_id,
            product_id=product_id,
            actor_user_id=get_user(),
            amount_usd=body.get("amountUsd"),
        )
        conn.close()
        return jsonify(out), 200

    @app.route("/api/admin/chat/history/truncate", methods=["POST"])
    def admin_chat_history_truncate():
        if not is_admin():
            return jsonify({"error": "admin only"}), 403
        body = request.get_json(silent=True) or {}
        product_id = body.get("productId") or body.get("product_id")
        if not product_id:
            return jsonify({"error": "productId required"}), 400
        conn = get_conn()
        out = truncate_chat_history(conn, product_id)
        conn.commit()
        conn.close()
        return jsonify({"ok": True, **out}), 200
