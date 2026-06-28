import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuum_api"))

from continuum_api.server import app


def test_society_planets_and_city_flow():
    client = app.test_client()
    r = client.get("/api/society/planets")
    assert r.status_code == 200
    planets = r.get_json()["items"]
    assert any(p["planetId"] == "earth" for p in planets)

    city_name = f"Test-{uuid.uuid4().hex[:6]}"
    r = client.post(
        "/api/society/planets/earth/cities",
        json={"displayName": city_name, "annualBudgetUsd": 15_000_000},
    )
    assert r.status_code == 201
    city_id = r.get_json()["cityId"]

    r = client.post(f"/api/society/cities/{city_id}/zoning/solve", json={"mode": "forward"})
    assert r.status_code == 200
    assert r.get_json().get("allocations")

    r = client.post(f"/api/society/cities/{city_id}/tick", json={})
    assert r.status_code == 200
    assert "snapshot" in r.get_json()

    r = client.get(f"/api/society/cities/{city_id}/network")
    assert r.status_code == 200
    assert "ipv6CityPrefix" in r.get_json()

    r = client.get("/city-config")
    assert r.status_code == 200
