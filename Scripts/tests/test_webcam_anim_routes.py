"""Webcam animation type_metadata store + SPA shell."""

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

from webcam_anim_routes import (  # noqa: E402
    KIND_VALUE,
    ensure_webcam_anim_schema,
    register_webcam_anim_routes,
)


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "webcam.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_webcam_anim_schema(conn)
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_webcam_anim_routes(app, get_conn)
    return app.test_client()


def test_post_accepts_type_metadata_keys(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        json={
            "kind": KIND_VALUE,
            "webcamAnimKind": "vehicle",
            "model_spec": "mediapipe_holistic@test",
            "subsection": "takeoff_roll_0",
            "animationListIndex": 3,
            "timelineStartMs": 1200,
            "timelineEndMs": 8400,
            "granularity": "millisecond",
            "targetHint": "magneto_bt",
        },
    )
    assert r.status_code == 201
    body = r.get_json()
    assert body["kind"] == KIND_VALUE
    assert body["model_spec"] == "mediapipe_holistic@test"
    assert body["subsection"] == "takeoff_roll_0"
    assert body["type_metadata"]["kind"] == KIND_VALUE
    assert body["type_metadata"]["model_spec"] == "mediapipe_holistic@test"


def test_list_filters_by_webcam_anim_recording_kind(app_client):
    app_client.post(
        "/api/webcam-animations",
        json={
            "kind": KIND_VALUE,
            "subsection": "keep-me",
            "model_spec": "stub",
        },
    )
    app_client.post(
        "/api/webcam-animations",
        json={
            "kind": "other_doc",
            "subsection": "skip-me",
            "model_spec": "stub",
        },
    )
    listed = app_client.get("/api/webcam-animations?kind=webcam_anim_recording").get_json()
    assert all(row["kind"] == KIND_VALUE for row in listed)
    assert any(row["subsection"] == "keep-me" for row in listed)
    assert not any(row["subsection"] == "skip-me" for row in listed)


def test_nested_type_metadata_and_spa(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        json={
            "type_metadata": {
                "kind": KIND_VALUE,
                "model_spec": "nested",
                "subsection": "reedit",
            }
        },
    )
    assert r.status_code == 201
    assert r.get_json()["model_spec"] == "nested"
    spa = app_client.get("/webcam-animations")
    assert spa.status_code == 200
    assert b"Webcam / video IK takes" in spa.data


def test_form_type_metadata_like_usc_upload(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        data={
            "document_type": "video",
            "type_metadata": '{"kind":"webcam_anim_recording","model_spec":"form-spec","subsection":"clip_a"}',
        },
    )
    assert r.status_code == 201
    body = r.get_json()
    assert body["model_spec"] == "form-spec"
    assert body["subsection"] == "clip_a"
    listed = app_client.get("/api/webcam-animations?kind=webcam_anim_recording").get_json()
    assert any(row["model_spec"] == "form-spec" for row in listed)


def test_models_seed_and_put_concurrency(app_client):
    r = app_client.get("/api/webcam-animations/models")
    assert r.status_code == 200
    data = r.get_json()
    kinds = {m["kind"] for m in data["models"]}
    assert "pose" in kinds
    assert "whisper" in kinds
    assert "music" in kinds
    assert data["totalConcurrency"] >= 1
    ids = {m["id"] for m in data["models"]}
    assert "whisper@base" in ids
    put = app_client.put(
        "/api/webcam-animations/models",
        json={"totalConcurrency": 3, "models": data["models"]},
    )
    assert put.status_code == 200
    assert put.get_json()["totalConcurrency"] == 3
    profiles = data["detectorProfiles"]
    assert {p["id"] for p in profiles} >= {"human-mediapipe-v1", "animal-mocap-v2"}
    human = next(p for p in profiles if p["id"] == "human-mediapipe-v1")
    assert human["poseEngine"] == "mediapipe"
    assert human["mediapipeSpec"] == "mediapipe_holistic@v1"


def test_queue_concurrency_one_second_stays_queued(app_client):
    app_client.put("/api/webcam-animations/models", json={"totalConcurrency": 1})
    a = app_client.post(
        "/api/webcam-animations",
        json={"kind": KIND_VALUE, "subsection": "first", "model_spec": "stub@local"},
    ).get_json()
    b = app_client.post(
        "/api/webcam-animations",
        json={"kind": KIND_VALUE, "subsection": "second", "model_spec": "stub@local"},
    ).get_json()
    statuses = {a["queueStatus"], b["queueStatus"]}
    assert "running" in statuses
    assert "queued" in statuses
    assert a.get("previewUrl") is None or b.get("previewUrl") is None

    listed = app_client.get("/api/webcam-animations?kind=webcam_anim_recording").get_json()
    by_sub = {row["subsection"]: row for row in listed}
    assert by_sub["first"]["queueStatus"] == "done"
    assert by_sub["first"]["previewUrl"]
    assert "continuuuum_editor" in by_sub["first"]["previewUrl"]
    assert by_sub["second"]["queueStatus"] == "running"
    assert by_sub["second"]["previewUrl"] is None

    listed2 = app_client.get("/api/webcam-animations?kind=webcam_anim_recording").get_json()
    by_sub2 = {row["subsection"]: row for row in listed2}
    assert by_sub2["second"]["queueStatus"] == "done"
    assert by_sub2["second"]["previewUrl"]


def test_metadata_only_becomes_viewable(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        json={"kind": KIND_VALUE, "subsection": "solo", "model_spec": "stub@local"},
    )
    assert r.status_code == 201
    assert r.get_json()["queueStatus"] == "running"
    got = app_client.get("/api/webcam-animations/" + r.get_json()["id"]).get_json()
    assert got["queueStatus"] == "done"
    assert got["previewUrl"]
    assert "View" not in got["previewUrl"]
    assert "/continuuuum_editor/index.html" in got["previewUrl"]


def test_models_include_mocapanything(app_client):
    ids = {m["id"] for m in app_client.get("/api/webcam-animations/models").get_json()["models"]}
    assert "mediapipe_holistic@v1" in ids
    assert "mocapanything@v2" in ids


def test_detector_profile_put_get_and_pinned_enqueue(app_client):
    data = app_client.get("/api/webcam-animations/models").get_json()
    profiles = data["detectorProfiles"]
    human = next(p for p in profiles if p["id"] == "human-mediapipe-v1")
    human["mediapipeSpec"] = "ghost@stale"
    put = app_client.put("/api/webcam-animations/models", json={"detectorProfiles": profiles})
    assert put.status_code == 200
    stored = next(p for p in put.get_json()["detectorProfiles"] if p["id"] == "human-mediapipe-v1")
    assert stored["mediapipeSpec"] == "ghost@stale"
    r = app_client.post(
        "/api/webcam-animations",
        json={
            "kind": KIND_VALUE,
            "detectorProfileId": "human-mediapipe-v1",
            "model_spec": "stale@old",
            "subsection": "pin-me",
        },
    )
    assert r.status_code == 201
    assert r.get_json()["model_spec"] == "mediapipe_holistic@v1"
    assert r.get_json()["type_metadata"]["model_spec"] == "mediapipe_holistic@v1"


def test_mocap_profile_fills_default_species(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        json={
            "kind": KIND_VALUE,
            "detectorProfileId": "animal-mocap-v2",
            "subsection": "wild",
        },
    )
    assert r.status_code == 201
    body = r.get_json()
    assert body["model_spec"] == "mocapanything@v2"
    assert body["species"] == "Lion"


def test_mocap_requires_species(app_client):
    r = app_client.post(
        "/api/webcam-animations",
        json={"kind": KIND_VALUE, "model_spec": "mocapanything@v2", "subsection": "wild"},
    )
    assert r.status_code == 400
    assert "species" in r.get_json()["error"]


def test_mediapipe_missing_package_fails(app_client, tmp_path):
    from pose_detectors import set_hop_runner

    def boom(_path, _payload):
        raise RuntimeError("install mediapipe in the detector env")

    set_hop_runner("mediapipe_holistic@v1", boom)
    try:
        r = app_client.post(
            "/api/webcam-animations",
            data={
                "type_metadata": json.dumps(
                    {
                        "kind": KIND_VALUE,
                        "model_spec": "mediapipe_holistic@v1",
                        "subsection": "mp",
                    }
                ),
                "file": (io.BytesIO(b"not-a-real-video"), "clip.mp4"),
            },
            content_type="multipart/form-data",
        )
        assert r.status_code == 201
        rec_id = r.get_json()["id"]
        got = app_client.get("/api/webcam-animations/" + rec_id).get_json()
        assert got["queueStatus"] == "failed"
        assert "mediapipe" in (got.get("queueError") or "")
    finally:
        set_hop_runner("mediapipe_holistic@v1", None)


def test_gpu_spec_stays_running_until_hop_finishes(app_client):
    from pose_detectors import DetectPending, set_hop_runner

    def pending(_path, _payload):
        raise DetectPending("still going")

    set_hop_runner("mocapanything@v2", pending)
    try:
        r = app_client.post(
            "/api/webcam-animations",
            data={
                "type_metadata": json.dumps(
                    {
                        "kind": KIND_VALUE,
                        "model_spec": "mocapanything@v2",
                        "species": "Lion",
                        "subsection": "lion1",
                    }
                ),
                "file": (io.BytesIO(b"clip"), "lion.mp4"),
            },
            content_type="multipart/form-data",
        )
        rec_id = r.get_json()["id"]
        got = app_client.get("/api/webcam-animations/" + rec_id).get_json()
        assert got["queueStatus"] == "running"
        assert got.get("previewUrl") is None
    finally:
        set_hop_runner("mocapanything@v2", None)


def test_gpu_hop_writes_posetrack(app_client, tmp_path):
    from pose_detectors import set_hop_runner
    from pose_detectors.posetrack import sample, write_track

    track_path = tmp_path / "out.posetrack.json"

    def ok(_path, payload):
        write_track(
            track_path,
            payload.get("model_spec") or "mocapanything@v2",
            [sample("Lion:Hips", 0.0, 0, 1, 0)],
        )
        return {"pose_track_path": str(track_path), "bvh_path": str(tmp_path / "x.bvh")}

    set_hop_runner("mocapanything@v2", ok)
    try:
        r = app_client.post(
            "/api/webcam-animations",
            data={
                "type_metadata": json.dumps(
                    {
                        "kind": KIND_VALUE,
                        "model_spec": "mocapanything@v2",
                        "species": "Lion",
                        "subsection": "lion2",
                    }
                ),
                "file": (io.BytesIO(b"clip"), "lion.mp4"),
            },
            content_type="multipart/form-data",
        )
        rec_id = r.get_json()["id"]
        got = app_client.get("/api/webcam-animations/" + rec_id).get_json()
        assert got["queueStatus"] == "done"
        assert got["poseTrackPath"] == str(track_path)
        pt = app_client.get(f"/api/webcam-animations/{rec_id}/posetrack")
        assert pt.status_code == 200
        body = pt.get_json()
        assert body["samples"][0]["traitId"] == "Lion:Hips"
        assert "timeMs" in body["samples"][0]
        assert "localPosition" in body["samples"][0]
        assert "localRotation" in body["samples"][0]
    finally:
        set_hop_runner("mocapanything@v2", None)


def test_whisper_usc_stub_writes_dialog_spans(app_client):
    from usc_whisper import set_transcribe_impl

    set_transcribe_impl(lambda path, base: {"text": "Hello there.", "words": []})
    try:
        r = app_client.post(
            "/api/webcam-animations",
            data={
                "type_metadata": json.dumps(
                    {
                        "kind": KIND_VALUE,
                        "model_spec": "whisper@base",
                        "subsection": "dialog",
                    }
                ),
                "file": (io.BytesIO(b"audio"), "line.wav"),
            },
            content_type="multipart/form-data",
        )
        assert r.status_code == 201
        rec_id = r.get_json()["id"]
        got = app_client.get("/api/webcam-animations/" + rec_id).get_json()
        assert got["queueStatus"] == "done"
        spans = (got.get("type_metadata") or {}).get("dialogSpans") or []
        assert spans
        assert "Hello" in spans[0]["label"]
        tree = (got.get("type_metadata") or {}).get("dialogueSet") or {}
        assert tree.get("nodes")
        assert tree["nodes"][0]["kind"] == "voice_actor_line"
    finally:
        set_transcribe_impl(None)


def test_whisper_missing_usc_fails_queue(app_client):
    from usc_whisper import UscUnavailable, set_transcribe_impl

    def boom(_path, _base):
        raise UscUnavailable("USC media unreachable")

    set_transcribe_impl(boom)
    try:
        r = app_client.post(
            "/api/webcam-animations",
            data={
                "type_metadata": json.dumps(
                    {
                        "kind": KIND_VALUE,
                        "model_spec": "whisper@base",
                        "subsection": "dialog-fail",
                    }
                ),
                "file": (io.BytesIO(b"audio"), "line.wav"),
            },
            content_type="multipart/form-data",
        )
        rec_id = r.get_json()["id"]
        got = app_client.get("/api/webcam-animations/" + rec_id).get_json()
        assert got["queueStatus"] == "failed"
        assert "USC" in (got.get("queueError") or "")
    finally:
        set_transcribe_impl(None)

