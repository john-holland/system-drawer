import json
import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from continuum_api.server import app


def test_camera_scenes_crud_and_comments():
    client = app.test_client()
    sid = str(uuid.uuid4())

    r = client.post(
        "/api/camera/scenes",
        json={
            "id": sid,
            "episodeId": "ep1",
            "shotId": "shot-a",
            "focusMode": "SceneFocus",
            "topology": {"vector": [0.1, 0.2]},
            "memorabilityMl": 0.7,
        },
    )
    assert r.status_code == 201

    r = client.post(f"/api/camera/scenes/{sid}/rate", json={"score": 4}, headers={"X-User-ID": "tester"})
    assert r.status_code == 200

    r = client.post(f"/api/camera/scenes/{sid}/vote", json={"vote": 1}, headers={"X-User-ID": "tester"})
    assert r.status_code == 200

    r = client.post(
        f"/api/camera/scenes/{sid}/comments",
        json={"bodyText": "Nice framing @operator"},
        headers={"X-User-ID": "tester"},
    )
    assert r.status_code == 201
    cid = r.get_json()["id"]
    assert "operator" in r.get_json()["mentions"]

    r = client.post(
        f"/api/camera/scenes/{sid}/comments/{cid}/reply",
        json={"bodyText": "@tester thanks"},
        headers={"X-User-ID": "operator"},
    )
    assert r.status_code == 201

    r = client.get(f"/api/camera/hints/{sid}")
    assert r.status_code == 200
    hints = r.get_json()
    assert "modeHintBias" in hints
    assert hints["userRatingMean"] == 4.0
