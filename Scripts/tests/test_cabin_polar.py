"""Cabin polar VO + composite hop — no OpenCV/YOLO weights required."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from pose_detectors.cabin_polar import polar_components, summarize_flow, write_polar_track  # noqa: E402
from pose_detectors.dispatch import is_cabin_spec, run_detect_hop, set_hop_runner  # noqa: E402
from pose_detectors.yolo26_vehicle import write_vehicle_track  # noqa: E402
from pose_detectors.posetrack import sample, write_track  # noqa: E402


def test_radial_expand_is_positive_when_flow_outward():
    # Points around center flowing away from center → forward.
    cx, cy = 50.0, 50.0
    points = [(60.0, 50.0), (40.0, 50.0), (50.0, 60.0), (50.0, 40.0)]
    flows = [(4.0, 0.0), (-4.0, 0.0), (0.0, 4.0), (0.0, -4.0)]
    stats = summarize_flow(points, flows, (cx, cy))
    assert stats["radialExpand"] > 0
    assert stats["speedHint"] > 0
    r, _a = polar_components(60, 50, 4, 0, cx, cy)
    assert r > 0


def test_inward_flow_is_negative_radial():
    points = [(60.0, 50.0)]
    flows = [(-3.0, 0.0)]
    stats = summarize_flow(points, flows, (50.0, 50.0))
    assert stats["radialExpand"] < 0
    assert stats["speedHint"] == 0.0


def test_is_cabin_spec():
    assert is_cabin_spec("cabin_composite@v1")
    assert not is_cabin_spec("yolo26_vehicle@intel")


def test_composite_stub_writes_pose_polar_empty_vehicle(tmp_path):
    pose_p = tmp_path / "p.posetrack.json"
    polar_p = tmp_path / "p.polar.json"
    veh_p = tmp_path / "p.vehicletrack.json"

    def stub(_path, payload):
        write_track(pose_p, "mediapipe_holistic@v1", [sample("Human:Hips", 0.0, 0, 1, 0)])
        write_polar_track(
            polar_p,
            "cabin_polar@v1",
            [{"tMs": 0, "radialExpand": 0.4, "azimuthalYaw": 0.0, "speedHint": 5.0, "yawRateHint": 0.0}],
        )
        write_vehicle_track(veh_p, "yolo26_vehicle@intel", [], [])
        return {
            "pose_track_path": str(pose_p),
            "polar_velocity_path": str(polar_p),
            "vehicle_track_path": str(veh_p),
        }

    set_hop_runner("cabin_composite@v1", stub)
    try:
        art = run_detect_hop("clip.mp4", {"model_spec": "cabin_composite@v1", "recording_id": "c1"})
        assert art["pose_track_path"] == str(pose_p)
        assert art["polar_velocity_path"] == str(polar_p)
        assert veh_p.is_file()
    finally:
        set_hop_runner("cabin_composite@v1", None)


def test_cabin_composite_succeeds_when_yolo_weights_missing(monkeypatch, tmp_path):
    clip = tmp_path / "cabin.mp4"
    clip.write_bytes(b"not-a-real-mp4")
    pose_p = tmp_path / "c.posetrack.json"
    polar_p = tmp_path / "c.polar.json"

    def fake_pose(_path, _payload):
        write_track(pose_p, "mediapipe_holistic@v1", [sample("Human:Hips", 0.0, 0, 1, 0)])
        return {"pose_track_path": str(pose_p)}

    def fake_polar(_path, _payload):
        write_polar_track(
            polar_p,
            "cabin_polar@v1",
            [{"tMs": 0, "radialExpand": 0.2, "azimuthalYaw": 0.0, "speedHint": 4.0, "yawRateHint": 0.0}],
        )
        return {"polar_velocity_path": str(polar_p)}

    def boom(_path, _payload):
        raise RuntimeError("Intel YOLO26 vehicle weights missing")

    import pose_detectors.cabin_composite as cabin_composite
    import pose_detectors.cabin_polar as cabin_polar
    import pose_detectors.mediapipe_holistic as mediapipe_holistic
    import pose_detectors.yolo26_vehicle as yolo26_vehicle

    monkeypatch.setattr(mediapipe_holistic, "run", fake_pose)
    monkeypatch.setattr(cabin_polar, "run", fake_polar)
    monkeypatch.setattr(yolo26_vehicle, "run", boom)

    art = cabin_composite.run(str(clip), {"recording_id": "c1", "model_spec": "cabin_composite@v1"})
    assert art["pose_track_path"] == str(pose_p)
    assert art["polar_velocity_path"] == str(polar_p)
    assert "yolo_skipped" in art
    assert Path(art["vehicle_track_path"]).is_file()
