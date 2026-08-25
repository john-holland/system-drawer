"""MediaPipe Holistic → PoseTrack JSON. Weights download via MediaPipe; do not vendor .tflite."""

from __future__ import annotations

from pathlib import Path
from typing import Any

from .posetrack import sample, write_track

# MediaPipe pose landmark index → Unity HumanBodyBones trait id
LANDMARK_TRAITS = {
    0: "Human:Head",
    11: "Human:LeftShoulder",
    12: "Human:RightShoulder",
    13: "Human:LeftLowerArm",
    14: "Human:RightLowerArm",
    15: "Human:LeftHand",
    16: "Human:RightHand",
    23: "Human:LeftUpperLeg",
    24: "Human:RightUpperLeg",
    25: "Human:LeftLowerLeg",
    26: "Human:RightLowerLeg",
    27: "Human:LeftFoot",
    28: "Human:RightFoot",
}

INSTALL_HINT = (
    "install mediapipe in the detector env "
    "(Python 3.12: pip install mediapipe). "
    "Do not use the default 3.14 interpreter."
)


_HOLISTIC = None
_LIVE_RUNNER = None


def set_live_pose_runner(fn):
    global _LIVE_RUNNER
    _LIVE_RUNNER = fn


def samples_from_pose(pose: Any, time_ms: float) -> list[dict[str, Any]]:
    out: list[dict[str, Any]] = []
    if pose is None:
        return out
    hips = _hips_from(pose)
    out.append(sample("Human:Hips", time_ms, *hips))
    for idx, trait in LANDMARK_TRAITS.items():
        if idx >= len(pose.landmark):
            continue
        lm = pose.landmark[idx]
        out.append(sample(trait, time_ms, lm.x, lm.y, lm.z))
    return out


def _get_holistic():
    global _HOLISTIC
    try:
        import mediapipe as mp  # type: ignore
    except ImportError as exc:
        raise RuntimeError(INSTALL_HINT) from exc
    if _HOLISTIC is None:
        _HOLISTIC = mp.solutions.holistic.Holistic(
            static_image_mode=False,
            model_complexity=1,
            refine_face_landmarks=False,
        )
    return _HOLISTIC


def run_image(file_path: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
    """One RGB/BGR image → PoseTrack samples. Used by live-pose (no SQL)."""
    payload = payload or {}
    spec = (payload.get("model_spec") or "mediapipe_holistic@v1").strip()
    t_ms = float(payload.get("tMs") or payload.get("t_ms") or 0.0)
    if _LIVE_RUNNER is not None:
        return _LIVE_RUNNER(file_path, payload) or {}
    try:
        import cv2
    except ImportError as exc:
        raise RuntimeError("OpenCV is required for live pose (pip install opencv-python-headless).") from exc
    bgr = cv2.imread(file_path)
    if bgr is None:
        raise RuntimeError(f"cannot read image: {file_path}")
    rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
    holistic = _get_holistic()
    result = holistic.process(rgb)
    pose = result.pose_world_landmarks or result.pose_landmarks
    samples = samples_from_pose(pose, t_ms)
    return {"modelSpec": spec, "tMs": t_ms, "samples": samples}


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    try:
        import mediapipe as mp  # type: ignore
    except ImportError as exc:
        raise RuntimeError(INSTALL_HINT) from exc

    import cv2

    spec = (payload.get("model_spec") or "mediapipe_holistic@v1").strip()
    out_dir = Path(file_path).parent / "posetracks"
    rec_id = payload.get("recording_id") or Path(file_path).stem
    out_path = out_dir / f"{rec_id}.posetrack.json"

    cap = cv2.VideoCapture(file_path)
    if not cap.isOpened():
        raise RuntimeError(f"cannot open video: {file_path}")

    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    samples: list[dict[str, Any]] = []
    holistic = mp.solutions.holistic.Holistic(
        static_image_mode=False,
        model_complexity=1,
        refine_face_landmarks=False,
    )
    try:
        frame_i = 0
        while True:
            ok, frame = cap.read()
            if not ok:
                break
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            result = holistic.process(rgb)
            pose = result.pose_world_landmarks or result.pose_landmarks
            time_ms = frame_i * (1000.0 / fps)
            samples.extend(samples_from_pose(pose, time_ms))
            frame_i += 1
    finally:
        holistic.close()
        cap.release()

    write_track(out_path, spec, samples)
    return {"pose_track_path": str(out_path), "sample_count": len(samples)}


def _hips_from(pose: Any) -> tuple[float, float, float]:
    l = pose.landmark[23] if len(pose.landmark) > 24 else None
    r = pose.landmark[24] if len(pose.landmark) > 24 else None
    if l is None or r is None:
        return 0.0, 0.0, 0.0
    return ((l.x + r.x) * 0.5, (l.y + r.y) * 0.5, (l.z + r.z) * 0.5)
