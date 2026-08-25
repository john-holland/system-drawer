"""YOLO26 vehicle hop: class filter, tracking, scene cuts — no weights in CI."""

from __future__ import annotations

import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from pose_detectors.dispatch import is_vehicle_spec, run_detect_hop, set_hop_runner  # noqa: E402
from pose_detectors.yolo26_vehicle import (  # noqa: E402
    SPEC_ID,
    associate_tracks,
    filter_vehicle_classes,
    heading_from_motion,
    hsv_hist_correlation,
    parse_yolo26_output,
    split_scene_cuts,
    write_vehicle_track,
)


def test_filter_keeps_intel_vehicle_classes_only():
    dets = [
        {"classId": 2, "conf": 0.9, "bbox": {"x1": 0, "y1": 0, "x2": 10, "y2": 10}},
        {"classId": 0, "conf": 0.99, "bbox": {"x1": 0, "y1": 0, "x2": 20, "y2": 20}},  # person
        {"classId": 7, "conf": 0.35, "bbox": {"x1": 1, "y1": 1, "x2": 8, "y2": 8}},  # truck below conf
        {"classId": 5, "conf": 0.41, "bbox": {"x1": 2, "y1": 2, "x2": 9, "y2": 9}},
        {"classId": 3, "conf": 0.8, "bbox": {"x1": 3, "y1": 3, "x2": 6, "y2": 6}},
    ]
    kept = filter_vehicle_classes(dets)
    ids = {d["classId"] for d in kept}
    assert ids == {2, 5, 3}
    assert all(d["conf"] >= 0.4 for d in kept)
    names = {d["className"] for d in kept}
    assert names == {"car", "bus", "motorcycle"}


def test_parse_yolo26_nms_free_shape():
    raw = [[[10, 20, 40, 60, 0.8, 2], [1, 1, 2, 2, 0.9, 0], [5, 5, 15, 25, 0.5, 7]]]
    dets = parse_yolo26_output(raw)
    assert {d["classId"] for d in dets} == {2, 7}


def test_associate_tracks_keeps_id():
    prev = [
        {
            "trackId": 4,
            "classId": 2,
            "bbox": {"x1": 0.1, "y1": 0.1, "x2": 0.4, "y2": 0.4},
            "cx": 0.25,
            "cy": 0.25,
        }
    ]
    dets = [
        {
            "classId": 2,
            "className": "car",
            "conf": 0.9,
            "bbox": {"x1": 0.12, "y1": 0.12, "x2": 0.42, "y2": 0.42},
            "cx": 0.27,
            "cy": 0.27,
        }
    ]
    assigned, next_id = associate_tracks(prev, dets, 5)
    assert len(assigned) == 1
    assert assigned[0]["trackId"] == 4
    assert next_id == 5


def test_scene_cut_on_hist_or_bbox_jump():
    hist_a = [1.0] + [0.0] * 7
    hist_b = [0.0] * 7 + [1.0]
    assert hsv_hist_correlation(hist_a, hist_b) < 0.2
    frames = [
        {
            "tMs": 0,
            "hsvHist": hist_a,
            "primaryBbox": {"x1": 0.1, "y1": 0.1, "x2": 0.3, "y2": 0.3},
            "cx": 0.2,
            "cy": 0.2,
            "trackId": 1,
            "classId": 2,
        },
        {
            "tMs": 33,
            "hsvHist": hist_a,
            "primaryBbox": {"x1": 0.12, "y1": 0.12, "x2": 0.32, "y2": 0.32},
            "cx": 0.22,
            "cy": 0.22,
            "trackId": 1,
            "classId": 2,
        },
        {
            "tMs": 66,
            "hsvHist": hist_b,
            "primaryBbox": {"x1": 0.7, "y1": 0.7, "x2": 0.9, "y2": 0.9},
            "cx": 0.8,
            "cy": 0.8,
            "trackId": 2,
            "classId": 2,
        },
    ]
    segs = split_scene_cuts(frames)
    assert len(segs) == 2
    assert segs[0]["startMs"] == 0
    assert segs[1]["startMs"] == 66
    assert segs[0]["subjectTrackId"] == 1


def test_heading_image_axes():
    # Move right and slightly up in image (dy negative in image coords means up).
    h = heading_from_motion(1.0, 0.0, facing_yaw=0.0)
    assert abs(h - math.atan2(1.0, 0.0)) < 1e-9
    h2 = heading_from_motion(0.0, -1.0)
    assert abs(h2 - math.atan2(0.0, 1.0)) < 1e-9


def test_is_vehicle_spec():
    assert is_vehicle_spec("yolo26_vehicle@intel")
    assert is_vehicle_spec("yolo26_vehicle@other")
    assert not is_vehicle_spec("mediapipe_holistic@v1")
    assert not is_vehicle_spec("mocap_anything@v2")

def test_dispatch_uses_stub_runner(tmp_path):
    out = tmp_path / "t.vehicletrack.json"

    def stub(path, payload):
        write_vehicle_track(
            out,
            payload.get("model_spec") or SPEC_ID,
            [
                {
                    "tMs": 0,
                    "trackId": 1,
                    "classId": 2,
                    "className": "car",
                    "conf": 0.9,
                    "bbox": {"x1": 0.1, "y1": 0.1, "x2": 0.4, "y2": 0.4},
                    "cx": 0.25,
                    "cy": 0.25,
                }
            ],
            [{"startMs": 0, "endMs": 100, "headingRad": 0.0, "subjectTrackId": 1, "subjectClassId": 2}],
        )
        return {"vehicle_track_path": str(out)}

    set_hop_runner(SPEC_ID, stub)
    try:
        artifacts = run_detect_hop("clip.mp4", {"model_spec": SPEC_ID, "recording_id": "r1"})
        assert artifacts["vehicle_track_path"] == str(out)
        assert out.is_file()
    finally:
        set_hop_runner(SPEC_ID, None)
