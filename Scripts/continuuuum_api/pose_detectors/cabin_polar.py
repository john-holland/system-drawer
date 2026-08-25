"""Cabin polar visual odometry from windshield optical flow. No extra ML weights."""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Any

SPEC_ID = "cabin_polar@v1"
DASHBOARD_BAND = 0.55  # ignore y >= this fraction (lower dashboard)


def unit(dx: float, dy: float) -> tuple[float, float]:
    n = math.hypot(dx, dy)
    if n < 1e-9:
        return 0.0, 0.0
    return dx / n, dy / n


def polar_components(
    px: float,
    py: float,
    flow_x: float,
    flow_y: float,
    cx: float,
    cy: float,
) -> tuple[float, float]:
    """Return (radial, azimuthal) flow at a point relative to image center."""
    rx, ry = unit(px - cx, py - cy)
    if rx == 0.0 and ry == 0.0:
        return 0.0, 0.0
    tx, ty = -ry, rx
    radial = flow_x * rx + flow_y * ry
    azimuthal = flow_x * tx + flow_y * ty
    return radial, azimuthal


def summarize_flow(
    points: list[tuple[float, float]],
    flows: list[tuple[float, float]],
    center: tuple[float, float],
    *,
    speed_scale: float = 12.0,
    yaw_scale: float = 0.08,
) -> dict[str, float]:
    """Mean radial expansion (forward) and azimuthal flow (yaw)."""
    if not points or not flows or len(points) != len(flows):
        return {"radialExpand": 0.0, "azimuthalYaw": 0.0, "speedHint": 0.0, "yawRateHint": 0.0}
    rad = 0.0
    az = 0.0
    n = 0
    cx, cy = center
    for (px, py), (fx, fy) in zip(points, flows):
        r, a = polar_components(px, py, fx, fy, cx, cy)
        rad += r
        az += a
        n += 1
    rad /= n
    az /= n
    return {
        "radialExpand": rad,
        "azimuthalYaw": az,
        "speedHint": max(0.0, rad * speed_scale),
        "yawRateHint": az * yaw_scale,
    }


def write_polar_track(
    path: Path,
    model_spec: str,
    frames: list[dict[str, Any]],
    segments: list[dict[str, Any]] | None = None,
) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    payload: dict[str, Any] = {"modelSpec": model_spec, "frames": frames}
    if segments:
        payload["segments"] = segments
    path.write_text(json.dumps(payload), encoding="utf-8")
    return path


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    try:
        import cv2
        import numpy as np
    except ImportError as exc:
        raise RuntimeError("OpenCV is required for cabin polar VO (pip install opencv-python-headless).") from exc

    spec = (payload.get("model_spec") or SPEC_ID).strip() or SPEC_ID
    rec_id = payload.get("recording_id") or Path(file_path).stem
    out_dir = Path(file_path).parent / "polartracks"
    out_path = out_dir / f"{rec_id}.polar.json"

    cap = cv2.VideoCapture(file_path)
    if not cap.isOpened():
        raise RuntimeError(f"cannot open video: {file_path}")

    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    ok, prev_bgr = cap.read()
    if not ok:
        cap.release()
        raise RuntimeError(f"empty video: {file_path}")

    prev_gray = cv2.cvtColor(prev_bgr, cv2.COLOR_BGR2GRAY)
    h, w = prev_gray.shape
    cy = h * 0.5
    cx = w * 0.5
    mask_y = int(h * DASHBOARD_BAND)
    grid = []
    for y in range(8, mask_y, max(8, mask_y // 12)):
        for x in range(8, w - 8, max(8, w // 16)):
            grid.append([[float(x), float(y)]])
    prev_pts = np.array(grid, dtype=np.float32) if grid else None

    from .yolo26_vehicle import _hsv_hist, split_scene_cuts

    frames: list[dict[str, Any]] = []
    cut_rows: list[dict[str, Any]] = []
    frame_i = 0
    try:
        while True:
            ok, bgr = cap.read()
            if not ok:
                break
            frame_i += 1
            gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)
            t_ms = frame_i * (1000.0 / fps)
            stats = {"radialExpand": 0.0, "azimuthalYaw": 0.0, "speedHint": 0.0, "yawRateHint": 0.0}
            if prev_pts is not None and len(prev_pts) > 0:
                nxt, st, _err = cv2.calcOpticalFlowPyrLK(prev_gray, gray, prev_pts, None)
                points: list[tuple[float, float]] = []
                flows: list[tuple[float, float]] = []
                if nxt is not None and st is not None:
                    for i, flag in enumerate(st):
                        if int(flag[0]) != 1:
                            continue
                        x0, y0 = float(prev_pts[i][0][0]), float(prev_pts[i][0][1])
                        x1, y1 = float(nxt[i][0][0]), float(nxt[i][0][1])
                        if y0 >= mask_y:
                            continue
                        points.append((x0, y0))
                        flows.append((x1 - x0, y1 - y0))
                if points:
                    stats = summarize_flow(points, flows, (cx, cy))
                good = nxt[st.flatten() == 1] if nxt is not None and st is not None else None
                prev_pts = good.reshape(-1, 1, 2) if good is not None and len(good) >= 8 else prev_pts
            frames.append({"tMs": t_ms, **stats})
            cut_rows.append({"tMs": t_ms, "hsvHist": _hsv_hist(cv2, bgr)})
            prev_gray = gray
    finally:
        cap.release()

    segments = split_scene_cuts(cut_rows)
    write_polar_track(out_path, spec, frames, segments)
    return {"polar_velocity_path": str(out_path), "frame_count": len(frames)}
