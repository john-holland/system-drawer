"""Cabin composite hop: occupant pose + polar VO + optional YOLO26 traffic (no ego bbox)."""

from __future__ import annotations

from typing import Any

SPEC_ID = "cabin_composite@v1"


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    artifacts: dict[str, Any] = {}
    pose_payload = dict(payload)
    species = (payload.get("species") or "").strip()
    if species:
        from .mocap_anything import run as mocap_run

        pose_payload["model_spec"] = (payload.get("pose_model_spec") or "mocapanything@v2").strip()
        artifacts.update(mocap_run(file_path, pose_payload) or {})
    else:
        from .mediapipe_holistic import run as pose_run

        pose_payload["model_spec"] = (payload.get("pose_model_spec") or "mediapipe_holistic@v1").strip()
        artifacts.update(pose_run(file_path, pose_payload) or {})

    yolo_payload = dict(payload)
    yolo_payload["model_spec"] = "yolo26_vehicle@intel"
    try:
        from .yolo26_vehicle import run as yolo_run

        artifacts.update(yolo_run(file_path, yolo_payload) or {})
    except Exception as exc:  # noqa: BLE001 — cabin YOLO is optional traffic
        from pathlib import Path as _Path

        from .yolo26_vehicle import write_vehicle_track

        artifacts["yolo_skipped"] = str(exc)[:500]
        rec_id = payload.get("recording_id") or _Path(file_path).stem
        empty = _Path(file_path).parent / "vehicletracks" / f"{rec_id}.vehicletrack.json"
        write_vehicle_track(empty, "yolo26_vehicle@intel", [], [])
        artifacts["vehicle_track_path"] = str(empty)

    from .cabin_polar import run as polar_run

    polar_payload = dict(payload)
    polar_payload["model_spec"] = "cabin_polar@v1"
    if artifacts.get("vehicle_track_path"):
        polar_payload["vehicle_track_path"] = artifacts["vehicle_track_path"]
    artifacts.update(polar_run(file_path, polar_payload) or {})
    return artifacts
