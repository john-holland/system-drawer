"""Tests for Cave, resaurce, and saurce integration routes."""

import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuum_api"))

from continuum_api.server import app
from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE


def test_cave_routes_and_config():
    client = app.test_client()
    r = client.get("/api/routes")
    assert r.status_code == 200
    data = r.get_json()
    assert "cave" in data
    assert any(t.get("tomeId") == "lemma-tome" for t in data.get("tomes", []))

    r = client.get("/api/config/overview")
    assert r.status_code == 200
    assert r.get_json().get("logViewMachine", {}).get("version") == "2.1.1"


def test_builtin_preorder_legal_gate():
    client = app.test_client()
    r = client.get("/api/legal/platform-features/preordering")
    assert r.status_code == 200
    body = r.get_json()
    assert body["featureKey"] == PLATFORM_PREORDER_FEATURE
    assert body["legalCase"]["id"] == BUILTIN_PREORDER_CASE_ID
    assert body["gate"]["status"] == "blocked"


def test_saurce_game_product_preorder_and_investment_flow():
    client = app.test_client()
    slug = f"game-{uuid.uuid4().hex[:8]}"
    r = client.post(
        "/api/saurce/products",
        json={
            "slug": slug,
            "name": "Test Star Game",
            "type": "game",
            "gameProfile": {
                "playModes": {"singlePlayer": True, "multiplayer": True},
                "multiplayerConfig": {"hostAgentRequired": True, "clientAgentRequired": True, "maxPlayers": 8},
            },
            "priceTag": {"amount": 60, "currency": "USD"},
        },
    )
    assert r.status_code == 201
    product_id = r.get_json()["id"]

    r = client.patch(
        f"/api/saurce/products/{product_id}/preorder",
        json={"enabled": True},
    )
    assert r.status_code == 409
    assert r.get_json().get("code") == "preorder_patent_blocked"

    client.patch(
        "/api/legal/platform-features/preordering",
        json={"status": "cleared"},
    )
    r = client.patch(
        f"/api/saurce/products/{product_id}/preorder",
        json={"enabled": True, "depositAmount": 10, "currency": "USD"},
    )
    assert r.status_code == 200

    r = client.patch(
        f"/api/saurce/products/{product_id}/preorder/investment",
        json={
            "customerCrowdfund": {
                "enabled": True,
                "baselineDiscountPercent": 10,
                "upsidePoolPercent": 5,
                "maxBackerReturnMultiple": 2,
            },
        },
    )
    assert r.status_code == 200

    r = client.post(
        f"/api/saurce/products/{product_id}/preorder/reserve",
        json={"investmentAmount": 25, "tier": "investor_backer", "depositPaid": 10},
        headers={"X-User-ID": "backer-1"},
    )
    assert r.status_code == 201
    assert r.get_json().get("investmentPositionId")

    r = client.post(
        "/api/saurce/foundation/safe-crypto-foundation-default/allocate",
        json={"productId": product_id, "amount": 1000},
    )
    assert r.status_code == 200

    r = client.post(
        f"/api/saurce/products/{product_id}/preorder/accrue-upside",
        json={"netAmount": 10000, "micropaymentStub": True, "micropaymentAmount": 50},
    )
    assert r.status_code == 200

    r = client.post(
        "/api/drawer-game/stub/host",
        json={"productId": product_id, "event": "HOST_START"},
    )
    assert r.status_code == 200


def test_media_rights_and_lemma_package():
    client = app.test_client()
    r = client.post(
        "/api/media-rights/publish",
        json={"assetId": "asset-1", "platform": "unity_asset_store", "territory": "US"},
    )
    assert r.status_code == 201

    r = client.post(
        "/api/lemma-packages",
        json={"name": "Premium pack", "premiumCost": 29.99, "vatRate": 10, "stateTaxJurisdiction": "CA"},
    )
    assert r.status_code == 201
    pid = r.get_json()["id"]
    r = client.get(f"/api/lemma-packages/{pid}/price-quote")
    assert r.status_code == 200
    assert r.get_json()["total"] > 0


def test_tome_message_delegation():
    client = app.test_client()
    r = client.post(
        "/api/tomes/saurce-tome/machines/productMachine/message",
        json={"event": "LIST", "data": {}},
    )
    assert r.status_code == 200
    assert r.get_json().get("ok") is True
