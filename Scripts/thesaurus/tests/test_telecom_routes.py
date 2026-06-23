import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from continuum_api.server import app


def test_telecom_networks_crud():
    client = app.test_client()
    dev_id = f"t-test-{uuid.uuid4().hex[:8]}"

    r = client.get("/api/telecom/networks")
    assert r.status_code == 200
    data = r.get_json()
    assert any(n["id"] == "ubiquitous" for n in data["items"])

    r = client.post("/api/telecom/devices", json={"id": dev_id, "displayName": "Test", "networkId": "ubiquitous"})
    assert r.status_code == 201

    r = client.post("/api/telecom/discover", json={"deviceId": dev_id})
    assert r.status_code == 200
    assert r.get_json()["found"] is True
