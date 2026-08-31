"""Image-to-model media store + Modly stub."""

from __future__ import annotations

import io
import json
import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from image_to_model_routes import (  # noqa: E402
    CONTINUUUUM_GRAN,
    MINECRAFT_GRAN,
    ensure_artwork_media_schema,
    normalize_granularity,
    register_image_to_model_routes,
)

PNG = (
    b"\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01"
    b"\x08\x02\x00\x00\x00\x90wS\xde\x00\x00\x00\x0cIDATx\x9cc```\x00\x00"
    b"\x00\x04\x00\x01\x0d\n-\xb4\x00\x00\x00\x00IEND\xaeB`\x82"
)


@pytest.fixture
def app_client(tmp_path, monkeypatch):
    db = tmp_path / "itm.db"
    monkeypatch.setenv("CONTINUUUUM_LIBRARY_UPLOADS", str(tmp_path / "uploads"))
    monkeypatch.delenv("MODLY_ROOT", raising=False)
    monkeypatch.setenv("MODLY_ROOT", str(tmp_path / "no-modly"))

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_artwork_media_schema(conn)
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_image_to_model_routes(app, get_conn)
    return app.test_client()


def test_normalize_blocks_per_meter():
    g = normalize_granularity({"blocksPerMeter": 2, "pixelGrid": 16})
    assert abs(g["blockMeters"] - 0.5) < 1e-6
    assert g["preset"] == "custom"


def test_features_and_presets(app_client):
    r = app_client.get("/api/image-to-model/features")
    assert r.status_code == 200
    body = r.get_json()
    ids = [f["id"] for f in body["features"]]
    assert "modly@local" in ids
    assert "voxel-ragdoll" in ids
    assert body["granularityMinecraft"]["blockMeters"] == 1.0
    assert body["granularityContinuuuum"]["maxBones"] == 256
    alias = app_client.get("/api/video-generation/features")
    assert alias.status_code == 200
    mc = app_client.get("/api/image-to-model/granularity/minecraft")
    assert mc.get_json()["pixelGrid"] == MINECRAFT_GRAN["pixelGrid"]
    cc = app_client.get("/api/image-to-model/granularity/continuuuum")
    assert cc.get_json()["skinLayout"] == CONTINUUUUM_GRAN["skinLayout"]


def test_media_store_and_blob(app_client):
    gran = json.dumps({"preset": "minecraft", "pixelGrid": 16, "blockMeters": 1})
    r = app_client.post(
        "/api/image-to-model/media",
        data={"granularity": gran, "image": (io.BytesIO(PNG), "dot.png")},
        content_type="multipart/form-data",
    )
    assert r.status_code == 200
    body = r.get_json()
    assert body["artworkId"].startswith("art_")
    assert body["granularity"]["preset"] in ("minecraft", "custom")
    kinds = [m["kind"] for m in body["media"]]
    assert "source_image" in kinds
    blob = app_client.get(f"/api/image-to-model/media/{body['artworkId']}/source_image?t=-1")
    assert blob.status_code == 200
    assert len(blob.data) > 0
    listed = app_client.get(f"/api/video-generation/media/{body['artworkId']}")
    assert listed.status_code == 200
    assert listed.get_json()["media"]


def test_modly_unavailable(app_client):
    gran = json.dumps({"preset": "minecraft"})
    stored = app_client.post(
        "/api/image-to-model/media",
        data={"artworkId": "art_test1", "granularity": gran, "image": (io.BytesIO(PNG), "dot.png")},
        content_type="multipart/form-data",
    )
    assert stored.status_code == 200
    r = app_client.post("/api/video-generation/modly", json={"artworkId": "art_test1"})
    assert r.status_code == 200
    body = r.get_json()
    assert body["ok"] is False
    assert body["available"] is False


def test_spa_index(app_client):
    r = app_client.get("/image-to-model")
    assert r.status_code == 200
    assert b"Image to model" in r.data
