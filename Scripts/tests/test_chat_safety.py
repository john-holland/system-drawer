"""Chat safety docket, unrated Continuuuum, and opt-in multiplayer chat."""

from __future__ import annotations

import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuuuum_api"))

from continuuuum_api.server import app, get_conn
from commerce_db import (
    ROBLOX_AG_KENTUCKY_CASE_ID,
    ROBLOX_AG_OHIO_CASE_ID,
    ROBLOX_AG_TEXAS_CASE_ID,
    ROBLOX_MDL_3166_CASE_ID,
)
from legal_collision import check_story_legal_collisions


def _client():
    return app.test_client()


def _new_game(client, structured_chat="optional", jurisdictions=None):
    slug = f"chat-game-{uuid.uuid4().hex[:8]}"
    gp = {
        "contentRating": "unrated",
        "playModes": {"singlePlayer": True, "multiplayer": True},
        "multiplayerConfig": {
            "hostAgentRequired": True,
            "clientAgentRequired": True,
            "structuredChat": structured_chat,
            "structuredChatFeeUsd": 1.0,
            "voiceChat": "disabled",
            "composeMode": "preview",
            "lexicon": {
                "words": [
                    {"id": "hello", "text": "hello"},
                    {"id": "hi", "text": "hi"},
                    {"id": "nope", "text": "nope"},
                ]
            },
        },
    }
    if jurisdictions is not None:
        gp["jurisdictions"] = jurisdictions
    r = client.post(
        "/api/saurce/products",
        json={"slug": slug, "name": slug, "type": "game", "gameProfile": gp},
    )
    assert r.status_code == 201, r.get_data(as_text=True)
    return r.get_json()["id"]


def test_docket_seed_and_collision_skip():
    client = _client()
    r = client.get("/api/legal/cases?caseKind=external_litigation")
    assert r.status_code == 200
    items = r.get_json()["items"]
    ids = {c["id"] for c in items}
    assert ROBLOX_MDL_3166_CASE_ID in ids
    assert ROBLOX_AG_TEXAS_CASE_ID in ids
    assert ROBLOX_AG_KENTUCKY_CASE_ID in ids
    assert ROBLOX_AG_OHIO_CASE_ID in ids
    mdl = next(c for c in items if c["id"] == ROBLOX_MDL_3166_CASE_ID)
    assert mdl["caseKind"] == "external_litigation"
    assert (mdl.get("externalMetadata") or {}).get("mdlNumber") == "3166"

    r = client.get("/api/legal/watchlist")
    assert r.status_code == 200
    watch_ids = {w["id"] for w in r.get_json()["items"]}
    assert "roblox-ag-louisiana" in watch_ids
    assert "roblox-ag-florida" in watch_ids
    assert "roblox-ag-arkansas" in watch_ids
    assert "roblox-la-county" in watch_ids

    r = client.get(f"/api/legal/cases/{ROBLOX_MDL_3166_CASE_ID}/docket-entries")
    assert r.status_code == 200
    assert r.get_json()["items"]

    r = client.get(f"/api/legal/cases/{ROBLOX_MDL_3166_CASE_ID}/code-lines")
    assert r.status_code == 200
    paths = [row.get("file_path") or row.get("filePath") for row in r.get_json()["items"]]
    assert any("StructuredChatChannel.cs" in (p or "") for p in paths)
    assert any("chat_safety_routes.py" in (p or "") for p in paths)

    conn = get_conn()
    warnings = check_story_legal_collisions(
        conn,
        "lemma",
        json.dumps({"legalCaseId": ROBLOX_MDL_3166_CASE_ID, "lemmaEntryId": "x"}),
    )
    conn.close()
    assert warnings == []


def test_continuuuum_compliance_does_not_require_chat_or_e_rating():
    client = _client()
    slug = f"continuuuum-{uuid.uuid4().hex[:8]}"
    r = client.post(
        "/api/saurce/products",
        json={"slug": slug, "name": "Continuuuum", "type": "game"},
    )
    assert r.status_code == 201
    pid = r.get_json()["id"]
    gp = r.get_json().get("gameProfile") or {}
    assert (gp.get("contentRating") or "unrated") == "unrated"
    assert (gp.get("multiplayerConfig") or {}).get("structuredChat") == "off"
    r = client.get(f"/api/saurce/products/{pid}/compliance-status")
    assert r.status_code == 200
    body = r.get_json()
    assert body["blocked"] is False
    assert body["chatRequired"] is False
    assert body["everyoneRatingRequired"] is False
    assert body["contentRating"] == "unrated"


def test_optional_structured_chat_rejects_without_entitlement():
    client = _client()
    pid = _new_game(client, "optional")
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": "player-unsigned", "text": "hi"},
    )
    assert r.status_code == 403
    assert r.get_json()["code"] in ("tos_not_signed", "chat_entitlement_required")
    r = client.get(f"/api/chat/entitlement?productId={pid}&userId=player-unsigned")
    assert r.status_code == 200
    assert r.get_json()["entitled"] is False


def test_self_pay_fee_credits_profit_and_warehouses():
    client = _client()
    pid = _new_game(client, "optional")
    user = f"player-{uuid.uuid4().hex[:8]}"
    tos = client.get("/api/chat/tos/current").get_json()
    r = client.post(
        "/api/chat/entitlement/activate",
        json={
            "userId": user,
            "productId": pid,
            "tosVersionId": tos["id"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
        },
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    body = r.get_json()
    assert body["ok"] is True
    assert body["feeUsd"] == 1.0
    assert body["profitBalanceUsd"] == 1.0
    assert body["payerKind"] == "self"
    r = client.get(f"/api/chat/profit?userId={user}")
    assert r.get_json()["profitBalanceUsd"] == 1.0
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "text": "hello"},
    )
    assert r.status_code == 200
    r = client.get("/api/admin/chat/warehouse", headers={"X-Admin": "1", "X-User-ID": "admin"})
    assert r.status_code == 200
    kinds = {e["eventKind"] for e in r.get_json()["items"] if (e.get("payload") or {}).get("userId") == user}
    for needed in (
        "chat_tos_signed",
        "chat_fee_charged",
        "chat_profit_credited",
        "chat_entitlement_granted",
    ):
        assert needed in kinds
    assert "chat_admin_paid" not in kinds


def test_admin_pay_credits_invitee_requires_signature_and_liability():
    client = _client()
    pid = _new_game(client, "optional")
    player = f"invitee-{uuid.uuid4().hex[:8]}"
    r = client.post(
        "/api/admin/chat/invites",
        json={
            "email": f"{player}@example.com",
            "userId": player,
            "productId": pid,
            "payForThem": True,
            "payerLegalEntity": "acme-holdings",
        },
        headers={"X-Admin": "1", "X-User-ID": "admin-ops"},
    )
    assert r.status_code == 201, r.get_data(as_text=True)
    invite = r.get_json()
    token = invite["token"]
    r = client.get(f"/api/chat/entitlement?productId={pid}&userId={player}")
    assert r.get_json()["entitled"] is False
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": player, "text": "nope"},
    )
    assert r.status_code == 403
    tos = client.get("/api/chat/tos/current").get_json()
    r = client.post(
        "/api/chat/entitlement/activate",
        json={
            "userId": player,
            "productId": pid,
            "inviteToken": token,
            "tosVersionId": tos["id"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
        },
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    body = r.get_json()
    assert body["payerKind"] == "admin"
    assert body["profitBalanceUsd"] == 1.0
    r = client.get("/api/admin/chat/warehouse", headers={"X-Admin": "1"})
    events = [
        e
        for e in r.get_json()["items"]
        if (e.get("payload") or {}).get("userId") == player
    ]
    kinds = {e["eventKind"] for e in events}
    assert "chat_invite_created" in kinds
    assert "chat_invite_emailed" in kinds
    assert "email_queued" in kinds
    assert "email_sent" in kinds
    assert "chat_admin_paid" in kinds
    assert "chat_entitlement_granted" in kinds
    admin_paid = next(e for e in events if e["eventKind"] == "chat_admin_paid")
    assert admin_paid["payload"]["age_verification_responsibility"] == "admin_and_associated_legal_entities"
    assert admin_paid["payload"]["legalEntity"] == "acme-holdings"


def test_no_refund_route_withdraw_reduces_profit():
    client = _client()
    pid = _new_game(client, "optional")
    user = f"wd-{uuid.uuid4().hex[:8]}"
    tos = client.get("/api/chat/tos/current").get_json()
    client.post(
        "/api/chat/entitlement/activate",
        json={
            "userId": user,
            "productId": pid,
            "tosVersionId": tos["id"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
        },
    )
    r = client.post("/api/chat/refund", json={"userId": user, "productId": pid})
    assert r.status_code == 404
    r = client.post("/api/chat/profit/withdraw", json={"userId": user, "amountUsd": 1.0})
    assert r.status_code == 200, r.get_data(as_text=True)
    assert r.get_json()["profitBalanceUsd"] == 0.0
    r = client.get(f"/api/chat/profit?userId={user}")
    assert r.get_json()["profitBalanceUsd"] == 0.0
    r = client.get("/api/admin/chat/warehouse", headers={"X-Admin": "1"})
    kinds = {e["eventKind"] for e in r.get_json()["items"] if (e.get("payload") or {}).get("userId") == user}
    assert "chat_profit_withdrawn" in kinds


def test_jurisdiction_disable_blocks_send_when_entitled():
    client = _client()
    pid = _new_game(
        client,
        "optional",
        jurisdictions=[{"code": "US-TX", "textChat": "disabled", "voiceChat": "disabled"}],
    )
    user = f"tx-{uuid.uuid4().hex[:8]}"
    tos = client.get("/api/chat/tos/current").get_json()
    client.post(
        "/api/chat/entitlement/activate",
        json={
            "userId": user,
            "productId": pid,
            "tosVersionId": tos["id"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
        },
    )
    r = client.get(f"/api/chat/entitlement?productId={pid}&userId={user}")
    assert r.get_json()["entitled"] is True
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "text": "hi", "jurisdiction": "US-TX"},
    )
    assert r.status_code == 403
    assert r.get_json()["code"] == "chat_disabled_jurisdiction"
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "channel": "voice"},
    )
    assert r.status_code == 403
    assert r.get_json()["code"] == "chat_disabled_jurisdiction"


def test_chat_surfaces_split_structural_and_unrated_tools():
    client = _client()
    r = client.get("/api/chat/surfaces")
    assert r.status_code == 200
    body = r.get_json()
    unrated = {s["id"] for s in body["unratedTool"]}
    structural = {s["id"] for s in body["structural"]}
    for sid in (
        "story-board",
        "table-read",
        "nav-global",
        "lemma-build",
        "table-read-recorder",
        "unity-dialogue",
    ):
        assert sid in unrated
    assert "unity-mp-text" in structural
    assert "unity-mp-voice" in structural
    assert "story-board" not in structural


def test_docket_watch_and_chat_entitlements_pages():
    client = _client()
    assert client.get("/docket-watch/").status_code == 200
    assert client.get("/chat-entitlements/").status_code == 200
    assert client.get("/chat-tos/accept").status_code == 200
    r = client.post("/api/admin/chat/invites", json={"email": "x@y.z", "productId": "p"})
    assert r.status_code == 403


def _entitle(client, pid, user):
    tos = client.get("/api/chat/tos/current").get_json()
    r = client.post(
        "/api/chat/entitlement/activate",
        json={
            "userId": user,
            "productId": pid,
            "tosVersionId": tos["id"],
            "soleUserAttested": True,
            "legalAgeAttested": True,
        },
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    return r.get_json()


def test_lexicon_get_put_and_entitlement_words():
    client = _client()
    pid = _new_game(client, "optional")
    r = client.get(f"/api/chat/lexicon?productId={pid}")
    assert r.status_code == 200
    body = r.get_json()
    assert body["composeMode"] == "preview"
    ids = {w["id"] for w in body["lexicon"]["words"]}
    assert "hello" in ids
    r = client.put(
        f"/api/chat/lexicon?productId={pid}",
        json={
            "composeMode": "sendButton",
            "lexicon": {"words": [{"id": "yes", "text": "Yes"}, {"id": "no", "text": "No"}]},
        },
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    saved = r.get_json()
    assert saved["composeMode"] == "sendButton"
    assert [w["id"] for w in saved["lexicon"]["words"]] == ["yes", "no"]
    user = f"lex-{uuid.uuid4().hex[:8]}"
    _entitle(client, pid, user)
    snap = client.get(f"/api/chat/entitlement?productId={pid}&userId={user}").get_json()
    assert snap["entitled"] is True
    assert snap["composeMode"] == "sendButton"
    assert [w["id"] for w in snap["lexicon"]["words"]] == ["yes", "no"]


def test_unknown_word_is_forbidden():
    client = _client()
    pid = _new_game(client, "optional")
    user = f"word-{uuid.uuid4().hex[:8]}"
    _entitle(client, pid, user)
    client.put(
        f"/api/chat/lexicon?productId={pid}",
        json={"lexicon": {"words": [{"id": "yes", "text": "Yes"}]}},
    )
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "text": "nope", "tokens": ["nope"]},
    )
    assert r.status_code == 403
    assert r.get_json()["code"] == "chat_word_not_allowed"
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "text": "Yes", "tokens": ["yes"]},
    )
    assert r.status_code == 200, r.get_data(as_text=True)


def test_send_writes_hot_and_warehouse():
    client = _client()
    pid = _new_game(client, "optional")
    user = f"hist-{uuid.uuid4().hex[:8]}"
    _entitle(client, pid, user)
    r = client.post(
        "/api/chat/send",
        json={"productId": pid, "userId": user, "sessionId": "s1", "text": "hello", "tokens": ["hello"]},
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    hot = client.get(f"/api/chat/history?productId={pid}&sessionId=s1").get_json()["items"]
    assert any(row["text"] == "hello" and row["userId"] == user for row in hot)
    warehouse = client.get(
        "/api/admin/chat/warehouse?kind=history",
        headers={"X-Admin": "1"},
    ).get_json()["items"]
    kinds = {
        e["eventKind"]
        for e in warehouse
        if (e.get("payload") or {}).get("userId") == user
    }
    assert "chat_message_committed" in kinds


def test_hot_truncate_keeps_warehouse():
    client = _client()
    pid = _new_game(client, "optional")
    user = f"trunc-{uuid.uuid4().hex[:8]}"
    _entitle(client, pid, user)
    r = client.put(
        f"/api/chat/lexicon?productId={pid}",
        json={
            "lexicon": {"words": [{"id": "hello", "text": "hello"}]},
            "historyRetention": {
                "hotMaxBytes": 8,
                "warehouseMaxBytes": "keep",
                "warehouseKeepAfterHotTruncate": True,
            },
        },
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    for _ in range(3):
        sent = client.post(
            "/api/chat/send",
            json={"productId": pid, "userId": user, "sessionId": "s1", "text": "hello", "tokens": ["hello"]},
        )
        assert sent.status_code == 200, sent.get_data(as_text=True)
    hot = client.get(f"/api/chat/history?productId={pid}&sessionId=s1").get_json()["items"]
    assert len(hot) == 1
    warehouse = client.get(
        "/api/admin/chat/warehouse?kind=history",
        headers={"X-Admin": "1"},
    ).get_json()["items"]
    committed = [
        e
        for e in warehouse
        if e["eventKind"] == "chat_message_committed"
        and (e.get("payload") or {}).get("userId") == user
    ]
    assert len(committed) == 3


def test_chat_lexicon_page():
    client = _client()
    assert client.get("/chat-lexicon/").status_code == 200
    assert client.get("/chat-lexicon").status_code in (200, 302)
