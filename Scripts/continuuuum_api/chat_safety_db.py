"""Chat entitlement, TOS, convenience fee, player profit, and warehouse."""

from __future__ import annotations

import hashlib
import json
import secrets
import sqlite3
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

try:
    from continuuuum_api.commerce_db import new_id
    from continuuuum_api.credits_db import warehouse_append
except ImportError:
    from commerce_db import new_id
    from credits_db import warehouse_append

CHAT_WAREHOUSE_LIST_ID = "chat-tos"
CHAT_HISTORY_LIST_ID = "chat-history"
CHAT_WAREHOUSE_TENANT = "chat-safety"
DEFAULT_HOT_MAX_BYTES = 50 * 1024 * 1024
DEFAULT_WAREHOUSE_MAX_BYTES = 500 * 1024 * 1024
HOT_BYTE_LADDER = (50 * 1024 * 1024, 100 * 1024 * 1024, 500 * 1024 * 1024, 2048 * 1024 * 1024)
CHAT_TOS_V1_ID = "chat-tos-v1.0.0"
CHAT_FEE_USD = 1.00
AGE_VERIFICATION_ADMIN = "admin_and_associated_legal_entities"

CHAT_TOS_V1_BODY = """CHAT STRUCTURED COMMUNICATION TERMS (v1.0.0)

By paying this convenience fee you are paying for this software.

You guarantee you are the sole user of this software for communication.

You are of legal age to communicate safely online.

There is no separate refund. The convenience fee is immediately credited to the
entitled player's withdrawable profit. Withdrawing that balance is how you
recover the fee.

If an administrator pays on your behalf, that administrator and associated
legal entities assume responsibility for age verification and legal means.
You must still sign these terms before chat is enabled.
"""

_SURFACES_PATH = Path(__file__).resolve().parent / "cave" / "chat_surfaces.yaml"
_SCHEMA_PATH = Path(__file__).resolve().parents[1] / "continuuuum_cave_saurce_schema.sql"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _tos_hash(body: str) -> str:
    return hashlib.sha256(body.encode("utf-8")).hexdigest()


def load_chat_surfaces() -> list[dict[str, Any]]:
    text = _SURFACES_PATH.read_text(encoding="utf-8")
    try:
        import yaml
    except ImportError:
        return _parse_surfaces_fallback(text)
    data = yaml.safe_load(text) or {}
    return list(data.get("surfaces") or [])


def _parse_surfaces_fallback(text: str) -> list[dict[str, Any]]:
    surfaces: list[dict[str, Any]] = []
    current: dict[str, Any] = {}
    for raw in text.splitlines():
        line = raw.rstrip()
        if line.startswith("  - id:"):
            if current:
                surfaces.append(current)
            current = {"id": line.split(":", 1)[1].strip()}
        elif current and ":" in line and line.startswith("    "):
            key, val = line.strip().split(":", 1)
            current[key.strip()] = val.strip()
    if current:
        surfaces.append(current)
    return surfaces


def warehouse_chat(
    conn: sqlite3.Connection,
    event_kind: str,
    *,
    actor_user_id: str | None = None,
    payload: dict[str, Any] | None = None,
    source: str = "chat_safety",
    list_id: str | None = None,
) -> str:
    return warehouse_append(
        conn,
        tenant_id=CHAT_WAREHOUSE_TENANT,
        list_id=list_id or CHAT_WAREHOUSE_LIST_ID,
        event_kind=event_kind,
        source=source,
        actor_user_id=actor_user_id,
        payload=payload or {},
    )


def ensure_chat_safety_tables(conn: sqlite3.Connection) -> None:
    if _SCHEMA_PATH.exists():
        conn.executescript(_SCHEMA_PATH.read_text(encoding="utf-8"))
    _seed_tos_v1(conn)
    conn.commit()


def _seed_tos_v1(conn: sqlite3.Connection) -> None:
    now = _now()
    body = CHAT_TOS_V1_BODY.strip() + "\n"
    digest = _tos_hash(body)
    cur = conn.execute("SELECT 1 FROM chat_tos_versions WHERE id = ?", (CHAT_TOS_V1_ID,))
    if cur.fetchone():
        return
    conn.execute(
        """INSERT INTO chat_tos_versions (id, semver, content_hash, body, published_at, published_by)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (CHAT_TOS_V1_ID, "1.0.0", digest, body, now, "continuuuum-legal-seed"),
    )
    warehouse_chat(
        conn,
        "chat_tos_published",
        actor_user_id="continuuuum-legal-seed",
        payload={"tosVersionId": CHAT_TOS_V1_ID, "semver": "1.0.0", "contentHash": digest},
    )


def current_tos(conn: sqlite3.Connection) -> dict[str, Any] | None:
    row = conn.execute(
        "SELECT * FROM chat_tos_versions ORDER BY published_at DESC LIMIT 1"
    ).fetchone()
    if not row:
        return None
    return {
        "id": row["id"],
        "semver": row["semver"],
        "contentHash": row["content_hash"],
        "body": row["body"],
        "publishedAt": row["published_at"],
        "publishedBy": row["published_by"],
    }


def _wallet_balance(conn: sqlite3.Connection, holder_kind: str, holder_id: str) -> float:
    row = conn.execute(
        "SELECT balance_usd FROM chat_profit_wallets WHERE holder_kind = ? AND holder_id = ?",
        (holder_kind, holder_id),
    ).fetchone()
    return float(row["balance_usd"]) if row else 0.0


def _adjust_wallet(
    conn: sqlite3.Connection,
    holder_kind: str,
    holder_id: str,
    delta: float,
) -> float:
    now = _now()
    current = _wallet_balance(conn, holder_kind, holder_id)
    next_bal = round(current + delta, 2)
    conn.execute(
        """INSERT INTO chat_profit_wallets (holder_kind, holder_id, balance_usd, currency, updated_at)
           VALUES (?, ?, ?, 'USD', ?)
           ON CONFLICT(holder_kind, holder_id) DO UPDATE SET
             balance_usd = excluded.balance_usd,
             updated_at = excluded.updated_at""",
        (holder_kind, holder_id, next_bal, now),
    )
    return next_bal


def player_profit_balance(conn: sqlite3.Connection, user_id: str) -> float:
    return _wallet_balance(conn, "user", user_id)


def _game_profile(conn: sqlite3.Connection, product_id: str) -> dict[str, Any]:
    row = conn.execute(
        "SELECT game_profile_json FROM saurce_products WHERE id = ?",
        (product_id,),
    ).fetchone()
    if not row:
        return {}
    try:
        return json.loads(row["game_profile_json"] or "{}") or {}
    except json.JSONDecodeError:
        return {}


def structured_chat_config(gp: dict[str, Any]) -> dict[str, Any]:
    mc = gp.get("multiplayerConfig") or {}
    fee = mc.get("structuredChatFeeUsd")
    try:
        fee_usd = float(fee) if fee is not None else CHAT_FEE_USD
    except (TypeError, ValueError):
        fee_usd = CHAT_FEE_USD
    return {
        "structuredChat": (mc.get("structuredChat") or "off"),
        "voiceChat": (mc.get("voiceChat") or "disabled"),
        "feeUsd": fee_usd,
        "composeMode": (mc.get("composeMode") or "preview"),
        "lexicon": {"words": normalize_lexicon_words((mc.get("lexicon") or {}).get("words"))},
        "historyRetention": normalize_history_retention(mc.get("historyRetention")),
        "jurisdictions": list(gp.get("jurisdictions") or []),
        "contentRating": gp.get("contentRating") or "unrated",
    }


def normalize_lexicon_words(raw: Any) -> list[dict[str, Any]]:
    words: list[dict[str, Any]] = []
    seen: set[str] = set()
    if not isinstance(raw, list):
        return words
    for item in raw:
        text = ""
        lemma_id = None
        wid = ""
        if isinstance(item, str):
            text = item.strip()
        elif isinstance(item, dict):
            text = str(item.get("text") or item.get("word") or "").strip()
            lemma_id = item.get("lemmaEntryId") or item.get("lemma_entry_id")
            wid = str(item.get("id") or "").strip()
        if not text:
            continue
        if not wid:
            wid = "".join(ch if ch.isalnum() or ch in "-_" else "-" for ch in text.lower()).strip("-") or text.lower()
        else:
            wid = wid.lower()
        if wid in seen:
            continue
        seen.add(wid)
        words.append({"id": wid, "text": text, "lemmaEntryId": lemma_id})
    return words


def normalize_history_retention(raw: Any) -> dict[str, Any]:
    src = raw if isinstance(raw, dict) else {}

    def _int(key: str, default: int | None) -> int | None:
        val = src.get(key)
        if val is None or val == "" or val == "off":
            return default if key == "hotMaxBytes" else None
        try:
            n = int(val)
        except (TypeError, ValueError):
            return default
        return n if n > 0 else None

    hot = _int("hotMaxBytes", DEFAULT_HOT_MAX_BYTES) or DEFAULT_HOT_MAX_BYTES
    wh = src.get("warehouseMaxBytes")
    if wh in ("keep", 0, "0"):
        warehouse_bytes: int | None = None
    else:
        warehouse_bytes = _int("warehouseMaxBytes", DEFAULT_WAREHOUSE_MAX_BYTES)
    keep = src.get("warehouseKeepAfterHotTruncate")
    if keep is None:
        keep = True
    return {
        "hotMaxBytes": hot,
        "hotMaxMessages": _int("hotMaxMessages", None),
        "hotMaxAgeDays": _int("hotMaxAgeDays", None),
        "warehouseMaxBytes": warehouse_bytes,
        "warehouseMaxMessages": _int("warehouseMaxMessages", None),
        "warehouseMaxAgeDays": _int("warehouseMaxAgeDays", None),
        "warehouseKeepAfterHotTruncate": bool(keep),
    }


def lexicon_document(conn: sqlite3.Connection, product_id: str) -> dict[str, Any] | None:
    row = conn.execute("SELECT id FROM saurce_products WHERE id = ?", (product_id,)).fetchone()
    if not row:
        return None
    cfg = structured_chat_config(_game_profile(conn, product_id))
    return {
        "productId": product_id,
        "composeMode": cfg["composeMode"],
        "lexicon": cfg["lexicon"],
        "historyRetention": cfg["historyRetention"],
        "structuredChat": cfg["structuredChat"],
    }


def put_lexicon(conn: sqlite3.Connection, product_id: str, body: dict[str, Any]) -> dict[str, Any] | None:
    row = conn.execute(
        "SELECT game_profile_json FROM saurce_products WHERE id = ?",
        (product_id,),
    ).fetchone()
    if not row:
        return None
    try:
        gp = json.loads(row["game_profile_json"] or "{}") or {}
    except json.JSONDecodeError:
        gp = {}
    mc = gp.setdefault("multiplayerConfig", {})
    if "composeMode" in body:
        mode = str(body.get("composeMode") or "preview").strip().lower()
        mc["composeMode"] = mode if mode in ("preview", "sendbutton") else "preview"
        if mc["composeMode"] == "sendbutton":
            mc["composeMode"] = "sendButton"
    if "lexicon" in body or "words" in body:
        words = body.get("words")
        if words is None:
            words = (body.get("lexicon") or {}).get("words")
        mc["lexicon"] = {"words": normalize_lexicon_words(words)}
    if "historyRetention" in body:
        mc["historyRetention"] = normalize_history_retention(body.get("historyRetention"))
    now = _now()
    conn.execute(
        "UPDATE saurce_products SET game_profile_json = ?, updated_at = ? WHERE id = ?",
        (json.dumps(gp), now, product_id),
    )
    conn.commit()
    return lexicon_document(conn, product_id)


def _allowed_word_ids(cfg: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    for w in (cfg.get("lexicon") or {}).get("words") or []:
        ids.add(str(w.get("id") or "").lower())
        ids.add(str(w.get("text") or "").strip().lower())
    ids.discard("")
    return ids


def tokens_allowed(cfg: dict[str, Any], tokens: list[str] | None, text: str | None) -> bool:
    allowed = _allowed_word_ids(cfg)
    check: list[str] = []
    if tokens:
        check.extend(str(t).strip() for t in tokens if str(t).strip())
    elif text:
        check.extend(part.strip(".,!?") for part in text.split() if part.strip(".,!?"))
    if not check:
        return True
    if not allowed:
        return False
    for tok in check:
        if tok.lower() not in allowed:
            return False
    return True


def _jurisdiction_flags(cfg: dict[str, Any], code: str | None) -> tuple[bool, bool]:
    text_ok = cfg["structuredChat"] in ("optional", "required")
    voice_ok = cfg["voiceChat"] in ("optional", "required")
    if not code:
        return text_ok, voice_ok
    needle = str(code).upper()
    for item in cfg["jurisdictions"]:
        if not isinstance(item, dict):
            continue
        if str(item.get("code") or "").upper() != needle:
            continue
        text_rule = str(item.get("textChat") or "allowed").lower()
        voice_rule = str(item.get("voiceChat") or "disabled").lower()
        if text_rule in ("disabled", "off", "denied", "blocked"):
            text_ok = False
        if voice_rule in ("disabled", "off", "denied", "blocked"):
            voice_ok = False
        break
    return text_ok, voice_ok


def _entitlement_row(conn: sqlite3.Connection, user_id: str, product_id: str) -> sqlite3.Row | None:
    return conn.execute(
        """SELECT * FROM chat_entitlements
           WHERE user_id = ? AND product_id = ? AND status = 'active'""",
        (user_id, product_id),
    ).fetchone()


def evaluate_entitlement(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    product_id: str,
    jurisdiction: str | None = None,
    channel: str = "text",
) -> dict[str, Any]:
    gp = _game_profile(conn, product_id)
    cfg = structured_chat_config(gp)
    row = _entitlement_row(conn, user_id, product_id)
    text_ok, voice_ok = _jurisdiction_flags(cfg, jurisdiction)
    entitled = bool(row) and cfg["structuredChat"] in ("optional", "required")
    deny: str | None = None
    if cfg["structuredChat"] == "off":
        deny = "chat_entitlement_required"
        entitled = False
        text_ok = False
        voice_ok = False
    elif not row:
        deny = "tos_not_signed"
    elif channel == "voice" and not voice_ok:
        deny = "chat_disabled_jurisdiction"
    elif channel == "text" and not text_ok:
        deny = "chat_disabled_jurisdiction"
    return {
        "entitled": entitled,
        "denyCode": deny,
        "textAllowed": bool(entitled and text_ok),
        "voiceAllowed": bool(entitled and voice_ok),
        "tosSigned": bool(row),
        "tosVersionId": row["tos_version_id"] if row else None,
        "payerKind": row["payer_kind"] if row else None,
        "structuredChat": cfg["structuredChat"],
        "voiceChat": cfg["voiceChat"],
        "feeUsd": cfg["feeUsd"],
        "contentRating": cfg["contentRating"],
        "composeMode": cfg["composeMode"],
        "lexicon": cfg["lexicon"],
        "historyRetention": cfg["historyRetention"],
        "profitBalanceUsd": player_profit_balance(conn, user_id),
        "productId": product_id,
        "userId": user_id,
        "jurisdiction": jurisdiction,
    }


def evaluate_send(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    product_id: str,
    jurisdiction: str | None = None,
    channel: str = "text",
    tokens: list[str] | None = None,
    text: str | None = None,
) -> dict[str, Any]:
    snap = evaluate_entitlement(
        conn, user_id=user_id, product_id=product_id, jurisdiction=jurisdiction, channel=channel
    )
    allowed = snap["textAllowed"] if channel != "voice" else snap["voiceAllowed"]
    snap["ok"] = bool(allowed)
    if allowed:
        cfg = structured_chat_config(_game_profile(conn, product_id))
        if not tokens_allowed(cfg, tokens, text):
            snap["ok"] = False
            snap["denyCode"] = "chat_word_not_allowed"
            return snap
        snap["denyCode"] = None
        return snap
    if not snap.get("denyCode"):
        snap["denyCode"] = "chat_entitlement_required"
    return snap


def _ledger(
    conn: sqlite3.Connection,
    entry_type: str,
    product_id: str | None,
    *,
    gross: float,
    net: float,
    idempotency_key: str,
    meta: dict[str, Any],
) -> str:
    try:
        from continuuuum_api.saurce_routes import _ledger as saurce_ledger
    except ImportError:
        from saurce_routes import _ledger as saurce_ledger
    return saurce_ledger(
        conn,
        entry_type,
        product_id,
        gross_amount=gross,
        net_amount=net,
        currency="USD",
        idempotency_key=idempotency_key,
        meta=meta,
    )


def _existing_ledger(conn: sqlite3.Connection, idempotency_key: str) -> str | None:
    row = conn.execute(
        "SELECT id FROM saurce_ledger_entries WHERE idempotency_key = ?",
        (idempotency_key,),
    ).fetchone()
    return row["id"] if row else None


def activate_entitlement(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    product_id: str,
    sole_user_attested: bool,
    legal_age_attested: bool,
    tos_version_id: str | None = None,
    signer_ip: str | None = None,
    invite_token: str | None = None,
    actor_user_id: str | None = None,
) -> dict[str, Any]:
    if not sole_user_attested or not legal_age_attested:
        return {"ok": False, "error": "attestations required", "code": "tos_not_signed"}
    tos = current_tos(conn)
    if not tos:
        return {"ok": False, "error": "tos not published", "code": "tos_not_signed"}
    if tos_version_id and tos_version_id != tos["id"]:
        return {"ok": False, "error": "tos version mismatch", "code": "tos_not_signed"}
    existing = _entitlement_row(conn, user_id, product_id)
    if existing:
        return {"ok": True, "entitlement": _entitlement_dict(existing), "alreadyActive": True}

    invite = None
    if invite_token:
        invite = conn.execute(
            "SELECT * FROM chat_invites WHERE token = ?",
            (invite_token,),
        ).fetchone()
        if not invite:
            return {"ok": False, "error": "invite not found", "code": "invite_not_found"}
        if invite["accepted_at"]:
            return {"ok": False, "error": "invite already accepted", "code": "invite_used"}
        if invite["product_id"] != product_id:
            return {"ok": False, "error": "invite product mismatch", "code": "invite_mismatch"}

    cfg = structured_chat_config(_game_profile(conn, product_id))
    fee = float(cfg["feeUsd"] or CHAT_FEE_USD)
    pay_for_them = bool(invite and invite["pay_for_them"])
    payer_kind = "admin" if pay_for_them else "self"
    payer_user_id = invite["created_by_admin"] if pay_for_them else user_id
    payer_entity = (invite["payer_legal_entity"] if invite else None) or (
        "continuuuum-ops" if pay_for_them else None
    )
    now = _now()
    family = f"chat-fee:{user_id}:{product_id}:{tos['id']}"
    fee_key = f"{family}:charge"
    profit_key = f"{family}:profit"

    warehouse_chat(
        conn,
        "chat_tos_signed",
        actor_user_id=actor_user_id or user_id,
        payload={
            "userId": user_id,
            "productId": product_id,
            "tosVersionId": tos["id"],
            "tosHash": tos["contentHash"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
            "signerIp": signer_ip,
            "payerKind": payer_kind,
            "payerUserId": payer_user_id,
            "payerLegalEntity": payer_entity,
        },
    )

    fee_ledger_id = _existing_ledger(conn, fee_key)
    if not fee_ledger_id:
        fee_ledger_id = _ledger(
            conn,
            "chat_convenience_fee",
            product_id,
            gross=fee,
            net=fee,
            idempotency_key=fee_key,
            meta={
                "userId": user_id,
                "payerKind": payer_kind,
                "payerUserId": payer_user_id,
                "payerLegalEntity": payer_entity,
                "tosHash": tos["contentHash"],
            },
        )
    if pay_for_them:
        _adjust_wallet(conn, "admin", payer_user_id, -fee)
        if payer_entity:
            _adjust_wallet(conn, "legal_entity", payer_entity, -fee)
        warehouse_chat(
            conn,
            "chat_admin_paid",
            actor_user_id=payer_user_id,
            payload={
                "userId": user_id,
                "adminUserId": payer_user_id,
                "legalEntity": payer_entity,
                "productId": product_id,
                "tosHash": tos["contentHash"],
                "amountUsd": fee,
                "age_verification_responsibility": AGE_VERIFICATION_ADMIN,
                "feeLedgerId": fee_ledger_id,
            },
        )
    warehouse_chat(
        conn,
        "chat_fee_charged",
        actor_user_id=payer_user_id,
        payload={
            "userId": user_id,
            "adminUserId": payer_user_id if pay_for_them else None,
            "legalEntity": payer_entity,
            "productId": product_id,
            "tosHash": tos["contentHash"],
            "amountUsd": fee,
            "payerKind": payer_kind,
            "feeLedgerId": fee_ledger_id,
            "age_verification_responsibility": AGE_VERIFICATION_ADMIN if pay_for_them else "self_attestation",
        },
    )

    profit_ledger_id = _existing_ledger(conn, profit_key)
    if not profit_ledger_id:
        profit_ledger_id = _ledger(
            conn,
            "chat_convenience_profit_credit",
            product_id,
            gross=fee,
            net=fee,
            idempotency_key=profit_key,
            meta={
                "userId": user_id,
                "payerKind": payer_kind,
                "tosHash": tos["contentHash"],
                "withdrawable": True,
            },
        )
    new_balance = _adjust_wallet(conn, "user", user_id, fee)
    warehouse_chat(
        conn,
        "chat_profit_credited",
        actor_user_id=user_id,
        payload={
            "userId": user_id,
            "adminUserId": payer_user_id if pay_for_them else None,
            "legalEntity": payer_entity,
            "productId": product_id,
            "tosHash": tos["contentHash"],
            "amountUsd": fee,
            "profitBalanceUsd": new_balance,
            "profitLedgerId": profit_ledger_id,
            "age_verification_responsibility": AGE_VERIFICATION_ADMIN if pay_for_them else "self_attestation",
        },
    )

    eid = new_id()
    conn.execute(
        """INSERT INTO chat_entitlements
           (id, user_id, product_id, tos_version_id, signed_at, signer_ip,
            sole_user_attested, legal_age_attested, payer_kind, payer_user_id,
            payer_legal_entity, fee_ledger_id, profit_ledger_id, fee_usd, status)
           VALUES (?, ?, ?, ?, ?, ?, 1, 1, ?, ?, ?, ?, ?, ?, 'active')""",
        (
            eid,
            user_id,
            product_id,
            tos["id"],
            now,
            signer_ip,
            payer_kind,
            payer_user_id,
            payer_entity,
            fee_ledger_id,
            profit_ledger_id,
            fee,
        ),
    )
    if invite:
        conn.execute(
            "UPDATE chat_invites SET accepted_at = ?, user_id = COALESCE(user_id, ?) WHERE token = ?",
            (now, user_id, invite_token),
        )
    warehouse_chat(
        conn,
        "chat_entitlement_granted",
        actor_user_id=user_id,
        payload={
            "userId": user_id,
            "adminUserId": payer_user_id if pay_for_them else None,
            "legalEntity": payer_entity,
            "productId": product_id,
            "tosHash": tos["contentHash"],
            "tosVersionId": tos["id"],
            "amountUsd": fee,
            "entitlementId": eid,
            "feeLedgerId": fee_ledger_id,
            "profitLedgerId": profit_ledger_id,
            "payerKind": payer_kind,
            "age_verification_responsibility": AGE_VERIFICATION_ADMIN if pay_for_them else "self_attestation",
        },
    )
    conn.commit()
    row = conn.execute("SELECT * FROM chat_entitlements WHERE id = ?", (eid,)).fetchone()
    return {
        "ok": True,
        "entitlement": _entitlement_dict(row) if row else {"id": eid},
        "profitBalanceUsd": new_balance,
        "feeUsd": fee,
        "payerKind": payer_kind,
    }


def _entitlement_dict(row: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": row["id"],
        "userId": row["user_id"],
        "productId": row["product_id"],
        "tosVersionId": row["tos_version_id"],
        "signedAt": row["signed_at"],
        "signerIp": row["signer_ip"],
        "soleUserAttested": bool(row["sole_user_attested"]),
        "legalAgeAttested": bool(row["legal_age_attested"]),
        "payerKind": row["payer_kind"],
        "payerUserId": row["payer_user_id"],
        "payerLegalEntity": row["payer_legal_entity"],
        "feeLedgerId": row["fee_ledger_id"],
        "profitLedgerId": row["profit_ledger_id"],
        "feeUsd": row["fee_usd"],
        "status": row["status"],
    }


def create_invite(
    conn: sqlite3.Connection,
    *,
    email: str,
    product_id: str,
    created_by_admin: str,
    user_id: str | None = None,
    pay_for_them: bool = False,
    payer_legal_entity: str | None = None,
    expires_at: str | None = None,
    invite_base: str = "",
) -> dict[str, Any]:
    token = secrets.token_urlsafe(24)
    iid = new_id()
    now = _now()
    entity = payer_legal_entity or ("continuuuum-ops" if pay_for_them else None)
    conn.execute(
        """INSERT INTO chat_invites
           (id, token, email, user_id, product_id, created_by_admin, pay_for_them,
            payer_legal_entity, expires_at, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            iid,
            token,
            email,
            user_id,
            product_id,
            created_by_admin,
            1 if pay_for_them else 0,
            entity,
            expires_at,
            now,
        ),
    )
    path = f"/chat-tos/accept?token={token}"
    url = f"{invite_base.rstrip('/')}{path}" if invite_base else path
    payload = {
        "inviteId": iid,
        "token": token,
        "email": email,
        "userId": user_id,
        "productId": product_id,
        "adminUserId": created_by_admin,
        "legalEntity": entity,
        "payForThem": pay_for_them,
        "inviteUrl": url,
        "age_verification_responsibility": AGE_VERIFICATION_ADMIN if pay_for_them else "self_attestation",
    }
    warehouse_chat(conn, "chat_invite_created", actor_user_id=created_by_admin, payload=payload)
    warehouse_chat(
        conn,
        "email_queued",
        actor_user_id=created_by_admin,
        payload={**payload, "transport": "stub"},
    )
    warehouse_chat(
        conn,
        "chat_invite_emailed",
        actor_user_id=created_by_admin,
        payload={**payload, "transport": "stub", "email_status": "sent"},
    )
    warehouse_chat(
        conn,
        "email_sent",
        actor_user_id=created_by_admin,
        payload={**payload, "transport": "stub"},
    )
    conn.commit()
    return {"id": iid, "token": token, "inviteUrl": url, "email": email, "payForThem": pay_for_them}


def invite_by_token(conn: sqlite3.Connection, token: str) -> dict[str, Any] | None:
    row = conn.execute("SELECT * FROM chat_invites WHERE token = ?", (token,)).fetchone()
    if not row:
        return None
    return {
        "id": row["id"],
        "token": row["token"],
        "email": row["email"],
        "userId": row["user_id"],
        "productId": row["product_id"],
        "createdByAdmin": row["created_by_admin"],
        "payForThem": bool(row["pay_for_them"]),
        "payerLegalEntity": row["payer_legal_entity"],
        "expiresAt": row["expires_at"],
        "acceptedAt": row["accepted_at"],
        "createdAt": row["created_at"],
        "inviteUrl": f"/chat-tos/accept?token={row['token']}",
    }


def list_invites(conn: sqlite3.Connection, limit: int = 200) -> list[dict[str, Any]]:
    rows = conn.execute(
        "SELECT * FROM chat_invites ORDER BY created_at DESC LIMIT ?",
        (limit,),
    ).fetchall()
    return [invite_by_token(conn, r["token"]) for r in rows if r]


def withdraw_profit(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    amount_usd: float,
    rail: str = "stub",
) -> dict[str, Any]:
    amount = round(float(amount_usd), 2)
    if amount <= 0:
        return {"ok": False, "error": "amount must be positive"}
    bal = player_profit_balance(conn, user_id)
    if amount > bal + 1e-9:
        return {"ok": False, "error": "insufficient profit balance", "profitBalanceUsd": bal}
    new_bal = _adjust_wallet(conn, "user", user_id, -amount)
    wid = new_id()
    now = _now()
    conn.execute(
        """INSERT INTO chat_profit_withdrawals (id, user_id, amount_usd, rail, status, created_at)
           VALUES (?, ?, ?, ?, 'posted', ?)""",
        (wid, user_id, amount, rail, now),
    )
    warehouse_chat(
        conn,
        "chat_profit_withdrawn",
        actor_user_id=user_id,
        payload={
            "userId": user_id,
            "amountUsd": amount,
            "profitBalanceUsd": new_bal,
            "rail": rail,
            "withdrawalId": wid,
        },
    )
    conn.commit()
    return {"ok": True, "withdrawalId": wid, "profitBalanceUsd": new_bal, "amountUsd": amount}


def record_chargeback(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    product_id: str,
    actor_user_id: str,
    amount_usd: float | None = None,
) -> dict[str, Any]:
    row = _entitlement_row(conn, user_id, product_id)
    amount = float(amount_usd if amount_usd is not None else (row["fee_usd"] if row else CHAT_FEE_USD))
    warehouse_chat(
        conn,
        "chat_fee_chargeback",
        actor_user_id=actor_user_id,
        payload={
            "userId": user_id,
            "productId": product_id,
            "amountUsd": amount,
            "entitlementId": row["id"] if row else None,
            "note": "Card-network chargeback warehouse event; not a product refund API.",
        },
    )
    conn.commit()
    return {"ok": True, "event": "chat_fee_chargeback"}


def list_warehouse(
    conn: sqlite3.Connection,
    limit: int = 200,
    list_id: str | None = None,
) -> list[dict[str, Any]]:
    lid = list_id or CHAT_WAREHOUSE_LIST_ID
    if lid == "all":
        rows = conn.execute(
            """SELECT * FROM credits_warehouse_history
               WHERE tenant_id = ?
               ORDER BY created_at DESC LIMIT ?""",
            (CHAT_WAREHOUSE_TENANT, limit),
        ).fetchall()
    else:
        rows = conn.execute(
            """SELECT * FROM credits_warehouse_history
               WHERE tenant_id = ? AND list_id = ?
               ORDER BY created_at DESC LIMIT ?""",
            (CHAT_WAREHOUSE_TENANT, lid, limit),
        ).fetchall()
    items = []
    for r in rows:
        payload = {}
        try:
            payload = json.loads(r["payload_json"] or "{}")
        except json.JSONDecodeError:
            payload = {}
        items.append(
            {
                "id": r["id"],
                "eventKind": r["event_kind"],
                "source": r["source"],
                "actorUserId": r["actor_user_id"],
                "listId": r["list_id"],
                "payload": payload,
                "createdAt": r["created_at"],
            }
        )
    return items


def record_jurisdiction_denied(
    conn: sqlite3.Connection,
    *,
    user_id: str,
    product_id: str,
    jurisdiction: str | None,
    channel: str,
) -> None:
    warehouse_chat(
        conn,
        "chat_jurisdiction_denied",
        actor_user_id=user_id,
        payload={
            "userId": user_id,
            "productId": product_id,
            "jurisdiction": jurisdiction,
            "channel": channel,
        },
    )
    conn.commit()


def record_committed_message(
    conn: sqlite3.Connection,
    *,
    product_id: str,
    user_id: str,
    session_id: str | None,
    text: str,
    tokens: list[str] | None = None,
) -> dict[str, Any]:
    sid = session_id or "default"
    body = text or " ".join(tokens or [])
    token_json = json.dumps(tokens or [])
    byte_len = len(body.encode("utf-8"))
    mid = new_id()
    now = _now()
    conn.execute(
        """INSERT INTO chat_session_messages
           (id, session_id, product_id, user_id, tokens_json, text, byte_len, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
        (mid, sid, product_id, user_id, token_json, body, byte_len, now),
    )
    warehouse_chat(
        conn,
        "chat_message_committed",
        actor_user_id=user_id,
        list_id=CHAT_HISTORY_LIST_ID,
        payload={
            "userId": user_id,
            "productId": product_id,
            "sessionId": sid,
            "text": body,
            "tokens": tokens or [],
            "bytes": byte_len,
            "messageId": mid,
        },
    )
    trunc = truncate_chat_history(conn, product_id)
    conn.commit()
    return {"ok": True, "messageId": mid, "bytes": byte_len, "truncate": trunc}


def truncate_chat_history(conn: sqlite3.Connection, product_id: str) -> dict[str, Any]:
    cfg = structured_chat_config(_game_profile(conn, product_id))
    ret = cfg["historyRetention"]
    hot_removed = _truncate_hot(conn, product_id, ret)
    wh_removed = _truncate_warehouse(conn, product_id, ret)
    if hot_removed["messages"] or wh_removed["messages"]:
        warehouse_chat(
            conn,
            "chat_history_truncated",
            actor_user_id="system",
            list_id=CHAT_HISTORY_LIST_ID,
            payload={
                "productId": product_id,
                "hot": hot_removed,
                "warehouse": wh_removed,
                "warehouseKeepAfterHotTruncate": ret["warehouseKeepAfterHotTruncate"],
            },
        )
    return {"hot": hot_removed, "warehouse": wh_removed}


def _truncate_hot(conn: sqlite3.Connection, product_id: str, ret: dict[str, Any]) -> dict[str, int]:
    removed_n = 0
    removed_b = 0
    max_age = ret.get("hotMaxAgeDays")
    if max_age:
        cutoff = datetime.now(timezone.utc).timestamp() - int(max_age) * 86400
        cutoff_iso = datetime.fromtimestamp(cutoff, timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        rows = conn.execute(
            "SELECT id, byte_len FROM chat_session_messages WHERE product_id = ? AND created_at < ?",
            (product_id, cutoff_iso),
        ).fetchall()
        ids = [r["id"] for r in rows]
        removed_n += len(ids)
        removed_b += sum(int(r["byte_len"] or 0) for r in rows)
        if ids:
            conn.execute(
                f"DELETE FROM chat_session_messages WHERE id IN ({','.join('?' * len(ids))})",
                ids,
            )
            if not ret.get("warehouseKeepAfterHotTruncate"):
                _drop_warehouse_for_message_ids(conn, ids)
    while True:
        stats = conn.execute(
            """SELECT COUNT(*) AS n, COALESCE(SUM(byte_len), 0) AS b
               FROM chat_session_messages WHERE product_id = ?""",
            (product_id,),
        ).fetchone()
        n = int(stats["n"] or 0)
        b = int(stats["b"] or 0)
        over_bytes = b > int(ret.get("hotMaxBytes") or DEFAULT_HOT_MAX_BYTES)
        over_count = ret.get("hotMaxMessages") is not None and n > int(ret["hotMaxMessages"])
        if not over_bytes and not over_count:
            break
        oldest = conn.execute(
            """SELECT id, byte_len FROM chat_session_messages
               WHERE product_id = ? ORDER BY created_at ASC LIMIT 1""",
            (product_id,),
        ).fetchone()
        if not oldest:
            break
        conn.execute("DELETE FROM chat_session_messages WHERE id = ?", (oldest["id"],))
        removed_n += 1
        removed_b += int(oldest["byte_len"] or 0)
        if not ret.get("warehouseKeepAfterHotTruncate"):
            _drop_warehouse_for_message_ids(conn, [oldest["id"]])
    return {"messages": removed_n, "bytes": removed_b}


def _drop_warehouse_for_message_ids(conn: sqlite3.Connection, ids: list[str]) -> None:
    for mid in ids:
        conn.execute(
            """DELETE FROM credits_warehouse_history
               WHERE tenant_id = ? AND list_id = ? AND event_kind = 'chat_message_committed'
                 AND payload_json LIKE ?""",
            (CHAT_WAREHOUSE_TENANT, CHAT_HISTORY_LIST_ID, f'%"messageId": "{mid}"%'),
        )


def _truncate_warehouse(conn: sqlite3.Connection, product_id: str, ret: dict[str, Any]) -> dict[str, int]:
    rows = conn.execute(
        """SELECT id, payload_json, created_at FROM credits_warehouse_history
           WHERE tenant_id = ? AND list_id = ? AND event_kind = 'chat_message_committed'
           ORDER BY created_at ASC""",
        (CHAT_WAREHOUSE_TENANT, CHAT_HISTORY_LIST_ID),
    ).fetchall()
    owned: list[tuple[str, int, str]] = []
    total_b = 0
    for r in rows:
        try:
            payload = json.loads(r["payload_json"] or "{}")
        except json.JSONDecodeError:
            payload = {}
        if payload.get("productId") != product_id:
            continue
        blen = int(payload.get("bytes") or 0)
        owned.append((r["id"], blen, r["created_at"]))
        total_b += blen
    removed_n = 0
    removed_b = 0
    max_age = ret.get("warehouseMaxAgeDays")
    if max_age:
        cutoff = datetime.now(timezone.utc).timestamp() - int(max_age) * 86400
        cutoff_iso = datetime.fromtimestamp(cutoff, timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        keep: list[tuple[str, int, str]] = []
        for hid, blen, created in owned:
            if created < cutoff_iso:
                conn.execute("DELETE FROM credits_warehouse_history WHERE id = ?", (hid,))
                removed_n += 1
                removed_b += blen
                total_b -= blen
            else:
                keep.append((hid, blen, created))
        owned = keep
    max_bytes = ret.get("warehouseMaxBytes")
    max_n = ret.get("warehouseMaxMessages")
    while owned and (
        (max_bytes is not None and total_b > int(max_bytes))
        or (max_n is not None and len(owned) > int(max_n))
    ):
        hid, blen, _ = owned[0]
        conn.execute("DELETE FROM credits_warehouse_history WHERE id = ?", (hid,))
        removed_n += 1
        removed_b += blen
        total_b -= blen
        owned.pop(0)
    return {"messages": removed_n, "bytes": removed_b}


def list_session_messages(
    conn: sqlite3.Connection,
    product_id: str,
    session_id: str | None = None,
    limit: int = 200,
) -> list[dict[str, Any]]:
    if session_id:
        rows = conn.execute(
            """SELECT * FROM chat_session_messages
               WHERE product_id = ? AND session_id = ?
               ORDER BY created_at DESC LIMIT ?""",
            (product_id, session_id, limit),
        ).fetchall()
    else:
        rows = conn.execute(
            """SELECT * FROM chat_session_messages
               WHERE product_id = ?
               ORDER BY created_at DESC LIMIT ?""",
            (product_id, limit),
        ).fetchall()
    items = []
    for r in rows:
        try:
            tokens = json.loads(r["tokens_json"] or "[]")
        except json.JSONDecodeError:
            tokens = []
        items.append(
            {
                "id": r["id"],
                "sessionId": r["session_id"],
                "productId": r["product_id"],
                "userId": r["user_id"],
                "tokens": tokens,
                "text": r["text"],
                "bytes": r["byte_len"],
                "createdAt": r["created_at"],
            }
        )
    return items
