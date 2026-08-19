import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuuuum_api"))

from continuuuum_api.server import app


def test_prison_retinue_request_sync_merge():
    client = app.test_client()
    city_name = f"PrisonCity-{uuid.uuid4().hex[:6]}"
    r = client.post(
        "/api/society/planets/earth/cities",
        json={"displayName": city_name, "annualBudgetUsd": 8_000_000},
    )
    assert r.status_code == 201, r.get_data(as_text=True)
    city_id = r.get_json()["cityId"]
    sid = "prison-1"

    r = client.post(
        f"/api/society/cities/{city_id}/prisons/{sid}/retinue",
        json={"action": "request", "prompt": "more yard time", "govAgencyId": "corrections"},
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    body = r.get_json()
    assert body["ok"] is True
    assert body["action"] == "request"
    assert body["civilKind"] == "Prison"
    assert body["govAgencyId"] == "corrections"

    r = client.post(
        f"/api/society/cities/{city_id}/prisons/{sid}/retinue",
        json={"action": "sync"},
    )
    assert r.status_code == 200
    assert r.get_json()["action"] == "sync"

    r = client.post(
        f"/api/society/cities/{city_id}/prisons/{sid}/retinue",
        json={"action": "merge", "local": 0.2, "remote": 0.8, "confidence": 0.9},
    )
    assert r.status_code == 200
    merged = r.get_json()["payload"]["merged"]
    assert merged is not None
    assert 0.2 < float(merged) < 0.8 or float(merged) == float(merged)
