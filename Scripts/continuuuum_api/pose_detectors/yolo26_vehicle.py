"""Intel YOLO26 vehicle detection hop. Required for Vehicle-kind takes — no MediaPipe fallback."""

from __future__ import annotations

import json
import math
import os
from pathlib import Path
from typing import Any

# COCO ids used by Intel/vehicle-detection (YOLO26).
VEHICLE_CLASS_IDS: dict[int, str] = {
    2: "car",
    3: "motorcycle",
    5: "bus",
    7: "truck",
}
CONF_MIN = 0.4
SPEC_ID = "yolo26_vehicle@intel"
INSTALL_HINT = (
    "Intel YOLO26 vehicle weights missing. Download YOLO26n (Ultralytics) or OpenVINO IR "
    "(yolo26n.xml + yolo26n.bin) into YOLO26_CACHE "
    "(default ~/.cache/intel-yolo26) or the video parent /yolo26/. "
    "See https://huggingface.co/Intel/vehicle-detection — do not fall back to MediaPipe."
)


def filter_vehicle_classes(
    dets: list[dict[str, Any]],
    *,
    conf_min: float = CONF_MIN,
    allowed: dict[int, str] | None = None,
) -> list[dict[str, Any]]:
    names = allowed or VEHICLE_CLASS_IDS
    out: list[dict[str, Any]] = []
    for d in dets:
        try:
            cid = int(d.get("classId", d.get("class_id", -1)))
            conf = float(d.get("conf", d.get("confidence", 0.0)))
        except (TypeError, ValueError):
            continue
        if cid not in names or conf < conf_min:
            continue
        bbox = _as_bbox(d.get("bbox") or d)
        cx = float(d.get("cx", (bbox[0] + bbox[2]) * 0.5))
        cy = float(d.get("cy", (bbox[1] + bbox[3]) * 0.5))
        out.append(
            {
                "classId": cid,
                "className": str(d.get("className") or names[cid]),
                "conf": conf,
                "bbox": {"x1": bbox[0], "y1": bbox[1], "x2": bbox[2], "y2": bbox[3]},
                "cx": cx,
                "cy": cy,
            }
        )
    return out


def parse_yolo26_output(raw: Any, *, conf_min: float = CONF_MIN) -> list[dict[str, Any]]:
    """Parse NMS-free YOLO26 rows: each is x1,y1,x2,y2,conf,class_id (pixel or normalized)."""
    rows = _flatten_pred_rows(raw)
    dets: list[dict[str, Any]] = []
    for row in rows:
        if len(row) < 6:
            continue
        x1, y1, x2, y2, conf, cid = (float(row[0]), float(row[1]), float(row[2]),
                                     float(row[3]), float(row[4]), float(row[5]))
        dets.append(
            {
                "classId": int(cid),
                "conf": conf,
                "bbox": {"x1": x1, "y1": y1, "x2": x2, "y2": y2},
                "cx": (x1 + x2) * 0.5,
                "cy": (y1 + y2) * 0.5,
            }
        )
    return filter_vehicle_classes(dets, conf_min=conf_min)


def iou_bbox(a: tuple[float, float, float, float], b: tuple[float, float, float, float]) -> float:
    ax1, ay1, ax2, ay2 = a
    bx1, by1, bx2, by2 = b
    ix1, iy1 = max(ax1, bx1), max(ay1, by1)
    ix2, iy2 = min(ax2, bx2), min(ay2, by2)
    iw, ih = max(0.0, ix2 - ix1), max(0.0, iy2 - iy1)
    inter = iw * ih
    if inter <= 0.0:
        return 0.0
    area_a = max(0.0, ax2 - ax1) * max(0.0, ay2 - ay1)
    area_b = max(0.0, bx2 - bx1) * max(0.0, by2 - by1)
    union = area_a + area_b - inter
    return inter / union if union > 1e-9 else 0.0


def associate_tracks(
    prev: list[dict[str, Any]],
    dets: list[dict[str, Any]],
    next_id: int,
    *,
    iou_min: float = 0.3,
) -> tuple[list[dict[str, Any]], int]:
    """Greedy IoU / centroid association. Prefer keeping the same trackId across frames."""
    assigned: list[dict[str, Any]] = []
    used_prev: set[int] = set()
    used_det: set[int] = set()
    pairs: list[tuple[float, int, int]] = []
    for i, t in enumerate(prev):
        tb = _as_bbox(t.get("bbox") or t)
        tcx, tcy = float(t.get("cx", (tb[0] + tb[2]) * 0.5)), float(t.get("cy", (tb[1] + tb[3]) * 0.5))
        for j, d in enumerate(dets):
            db = _as_bbox(d.get("bbox") or d)
            dcx, dcy = float(d.get("cx", (db[0] + db[2]) * 0.5)), float(d.get("cy", (db[1] + db[3]) * 0.5))
            iou = iou_bbox(tb, db)
            dist = math.hypot(tcx - dcx, tcy - dcy)
            score = iou if iou >= iou_min else (0.15 / (1.0 + dist) if dist < 80.0 else 0.0)
            if score > 0.0:
                pairs.append((score, i, j))
    pairs.sort(key=lambda p: p[0], reverse=True)
    for _score, i, j in pairs:
        if i in used_prev or j in used_det:
            continue
        used_prev.add(i)
        used_det.add(j)
        d = dict(dets[j])
        d["trackId"] = int(prev[i]["trackId"])
        assigned.append(d)
    for j, d in enumerate(dets):
        if j in used_det:
            continue
        d = dict(d)
        d["trackId"] = next_id
        next_id += 1
        assigned.append(d)
    return assigned, next_id


def hsv_hist_correlation(hist_a: list[float], hist_b: list[float]) -> float:
    if not hist_a or not hist_b or len(hist_a) != len(hist_b):
        return 0.0
    na = math.sqrt(sum(v * v for v in hist_a))
    nb = math.sqrt(sum(v * v for v in hist_b))
    if na < 1e-9 or nb < 1e-9:
        return 0.0
    return sum(a * b for a, b in zip(hist_a, hist_b)) / (na * nb)


def heading_from_motion(dx: float, dy: float, facing_yaw: float = 0.0) -> float:
    """Image +x = right, +y = down. World yaw = atan2(dx, -dy) + facing."""
    return math.atan2(dx, -dy) + facing_yaw


def split_scene_cuts(
    frames: list[dict[str, Any]],
    *,
    hist_corr_thresh: float = 0.55,
    bbox_iou_cut: float = 0.25,
) -> list[dict[str, Any]]:
    """Split whenever HSV correlation drops or the primary bbox jumps (camera cut / angle change)."""
    if not frames:
        return []
    cuts = [0]
    for i in range(1, len(frames)):
        prev, cur = frames[i - 1], frames[i]
        hist_a = prev.get("hsvHist") or []
        hist_b = cur.get("hsvHist") or []
        hist_cut = bool(hist_a and hist_b and hsv_hist_correlation(hist_a, hist_b) < hist_corr_thresh)
        pb = prev.get("primaryBbox")
        cb = cur.get("primaryBbox")
        bbox_cut = False
        if pb and cb:
            bbox_cut = iou_bbox(_as_bbox(pb), _as_bbox(cb)) < bbox_iou_cut
        if hist_cut or bbox_cut:
            cuts.append(i)
    cuts.append(len(frames))
    segments: list[dict[str, Any]] = []
    for a, b in zip(cuts, cuts[1:]):
        if a >= b:
            continue
        chunk = frames[a:b]
        start_ms = float(chunk[0].get("tMs", 0.0))
        end_ms = float(chunk[-1].get("tMs", start_ms))
        heading = _heading_for_chunk(chunk)
        primary = _largest_in_chunk(chunk)
        segments.append(
            {
                "startMs": start_ms,
                "endMs": end_ms,
                "headingRad": heading,
                "subjectTrackId": int(primary.get("trackId", 0)) if primary else 0,
                "subjectClassId": int(primary.get("classId", 0)) if primary else 0,
                "hasFacingYawOverride": False,
                "facingYawOverride": 0.0,
            }
        )
    return segments


def write_vehicle_track(path: Path, model_spec: str, frames: list[dict[str, Any]], segments: list[dict[str, Any]]) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "modelSpec": model_spec,
        "frames": frames,
        "segments": segments,
    }
    path.write_text(json.dumps(payload), encoding="utf-8")
    return path


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    spec = (payload.get("model_spec") or SPEC_ID).strip() or SPEC_ID
    rec_id = payload.get("recording_id") or Path(file_path).stem
    out_dir = Path(file_path).parent / "vehicletracks"
    out_path = out_dir / f"{rec_id}.vehicletrack.json"

    model = _load_model(Path(file_path))
    try:
        import cv2
    except ImportError as exc:
        raise RuntimeError("OpenCV is required for yolo26_vehicle@intel (pip install opencv-python-headless).") from exc

    cap = cv2.VideoCapture(file_path)
    if not cap.isOpened():
        raise RuntimeError(f"cannot open video: {file_path}")

    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    width = max(1.0, cap.get(cv2.CAP_PROP_FRAME_WIDTH) or 1.0)
    height = max(1.0, cap.get(cv2.CAP_PROP_FRAME_HEIGHT) or 1.0)
    frames: list[dict[str, Any]] = []
    cut_rows: list[dict[str, Any]] = []
    prev_tracks: list[dict[str, Any]] = []
    next_id = 1
    frame_i = 0
    try:
        while True:
            ok, bgr = cap.read()
            if not ok:
                break
            t_ms = frame_i * (1000.0 / fps)
            raw = _infer(model, bgr)
            dets = parse_yolo26_output(raw)
            dets = _normalize_dets(dets, width, height)
            assigned, next_id = associate_tracks(prev_tracks, dets, next_id)
            prev_tracks = assigned
            hsv_hist = _hsv_hist(cv2, bgr)
            primary = _largest_det(assigned)
            for d in assigned:
                frames.append(
                    {
                        "tMs": t_ms,
                        "trackId": int(d["trackId"]),
                        "classId": int(d["classId"]),
                        "className": d.get("className") or VEHICLE_CLASS_IDS.get(int(d["classId"]), "vehicle"),
                        "conf": float(d["conf"]),
                        "bbox": d["bbox"],
                        "cx": float(d["cx"]),
                        "cy": float(d["cy"]),
                    }
                )
            cut_rows.append(
                {
                    "tMs": t_ms,
                    "hsvHist": hsv_hist,
                    "primaryBbox": primary["bbox"] if primary else None,
                    "cx": float(primary["cx"]) if primary else 0.5,
                    "cy": float(primary["cy"]) if primary else 0.5,
                    "trackId": int(primary["trackId"]) if primary else 0,
                    "classId": int(primary["classId"]) if primary else 0,
                }
            )
            frame_i += 1
    finally:
        cap.release()

    segments = split_scene_cuts(cut_rows)
    write_vehicle_track(out_path, spec, frames, segments)
    return {"vehicle_track_path": str(out_path)}


def _as_bbox(src: Any) -> tuple[float, float, float, float]:
    if isinstance(src, dict):
        return (
            float(src.get("x1", 0.0)),
            float(src.get("y1", 0.0)),
            float(src.get("x2", src.get("x1", 0.0))),
            float(src.get("y2", src.get("y1", 0.0))),
        )
    if isinstance(src, (list, tuple)) and len(src) >= 4:
        return float(src[0]), float(src[1]), float(src[2]), float(src[3])
    return 0.0, 0.0, 0.0, 0.0


def _flatten_pred_rows(raw: Any) -> list[list[float]]:
    if raw is None:
        return []
    if hasattr(raw, "tolist"):
        raw = raw.tolist()
    if isinstance(raw, (list, tuple)) and raw and isinstance(raw[0], (int, float)):
        return [list(map(float, raw))]
    rows: list[list[float]] = []
    stack = [raw]
    while stack:
        cur = stack.pop()
        if cur is None:
            continue
        if isinstance(cur, (list, tuple)):
            if cur and isinstance(cur[0], (int, float)) and len(cur) >= 6:
                rows.append([float(x) for x in cur[:6]])
            else:
                stack.extend(cur)
    return rows


def _largest_det(dets: list[dict[str, Any]]) -> dict[str, Any] | None:
    best = None
    best_area = -1.0
    for d in dets:
        b = _as_bbox(d.get("bbox") or d)
        area = max(0.0, b[2] - b[0]) * max(0.0, b[3] - b[1])
        if area > best_area:
            best_area = area
            best = d
    return best


def _largest_in_chunk(chunk: list[dict[str, Any]]) -> dict[str, Any] | None:
    best = None
    best_area = -1.0
    for row in chunk:
        pb = row.get("primaryBbox")
        if not pb:
            continue
        b = _as_bbox(pb)
        area = max(0.0, b[2] - b[0]) * max(0.0, b[3] - b[1])
        if area > best_area:
            best_area = area
            best = row
    return best


def _heading_for_chunk(chunk: list[dict[str, Any]]) -> float:
    if len(chunk) < 2:
        return 0.0
    first, last = chunk[0], chunk[-1]
    dx = float(last.get("cx", 0.5)) - float(first.get("cx", 0.5))
    dy = float(last.get("cy", 0.5)) - float(first.get("cy", 0.5))
    if abs(dx) < 1e-6 and abs(dy) < 1e-6:
        return 0.0
    return heading_from_motion(dx, dy)


def _normalize_dets(dets: list[dict[str, Any]], width: float, height: float) -> list[dict[str, Any]]:
    out = []
    for d in dets:
        b = _as_bbox(d["bbox"])
        if max(b) > 1.5:
            b = (b[0] / width, b[1] / height, b[2] / width, b[3] / height)
        d = dict(d)
        d["bbox"] = {"x1": b[0], "y1": b[1], "x2": b[2], "y2": b[3]}
        d["cx"] = (b[0] + b[2]) * 0.5
        d["cy"] = (b[1] + b[3]) * 0.5
        out.append(d)
    return out


def _hsv_hist(cv2: Any, bgr: Any) -> list[float]:
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    hist = cv2.calcHist([hsv], [0, 1], None, [16, 8], [0, 180, 0, 256])
    flat = hist.flatten().astype(float).tolist()
    s = sum(flat) or 1.0
    return [v / s for v in flat]


def _cache_dirs(video_path: Path) -> list[Path]:
    env = os.environ.get("YOLO26_CACHE", "").strip()
    dirs = []
    if env:
        dirs.append(Path(env))
    dirs.append(Path.home() / ".cache" / "intel-yolo26")
    dirs.append(video_path.parent / "yolo26")
    return dirs


def _find_ir(video_path: Path) -> Path | None:
    for d in _cache_dirs(video_path):
        xml = d / "yolo26n.xml"
        if xml.is_file():
            return xml
    return None


def _load_model(video_path: Path) -> Any:
    xml = _find_ir(video_path)
    if xml is not None:
        try:
            from ultralytics import YOLO  # type: ignore

            return ("ultralytics", YOLO(str(xml)))
        except Exception:
            try:
                import openvino as ov  # type: ignore

                core = ov.Core()
                compiled = core.compile_model(str(xml), "CPU")
                return ("openvino", compiled)
            except Exception as exc:
                raise RuntimeError(INSTALL_HINT) from exc
    try:
        from ultralytics import YOLO  # type: ignore
    except ImportError as exc:
        raise RuntimeError(INSTALL_HINT) from exc
    try:
        model = YOLO("yolo26n.pt")
    except Exception as exc:
        raise RuntimeError(INSTALL_HINT) from exc
    return ("ultralytics", model)


def _infer(model: Any, bgr: Any) -> Any:
    kind, handle = model
    if kind == "ultralytics":
        results = handle.predict(bgr, verbose=False)
        if not results:
            return []
        r0 = results[0]
        boxes = getattr(r0, "boxes", None)
        if boxes is None:
            data = getattr(r0, "data", None)
            return data
        rows = []
        xyxy = boxes.xyxy.tolist() if hasattr(boxes.xyxy, "tolist") else list(boxes.xyxy)
        confs = boxes.conf.tolist() if hasattr(boxes.conf, "tolist") else list(boxes.conf)
        clss = boxes.cls.tolist() if hasattr(boxes.cls, "tolist") else list(boxes.cls)
        for i, box in enumerate(xyxy):
            rows.append([box[0], box[1], box[2], box[3], confs[i], clss[i]])
        return rows
    # OpenVINO raw [1, 300, 6]
    import numpy as np  # type: ignore

    inp = handle.inputs[0]
    shape = list(inp.shape)
    h, w = int(shape[2]), int(shape[3]) if len(shape) == 4 else (640, 640)
    import cv2

    img = cv2.resize(bgr, (w, h))
    blob = img.transpose(2, 0, 1)[None].astype("float32") / 255.0
    out = handle(blob)
    tensor = next(iter(out.values())) if isinstance(out, dict) else out[0]
    return np.asarray(tensor)
