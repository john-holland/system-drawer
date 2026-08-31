"""Webcam / video IK animation recordings: SPA + type_metadata store."""

from __future__ import annotations

import json
import sqlite3
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable
from urllib.parse import urlencode

from flask import jsonify, request, send_from_directory

try:
    from pose_detectors import (
        DetectPending,
        is_cabin_spec,
        is_gpu_spec,
        is_vehicle_spec,
        is_whisper_spec,
        run_detect_hop,
    )
except ImportError:
    from continuuuum_api.pose_detectors import (
        DetectPending,
        is_cabin_spec,
        is_gpu_spec,
        is_vehicle_spec,
        is_whisper_spec,
        run_detect_hop,
    )

GetConn = Callable[[], sqlite3.Connection]

KIND_VALUE = "webcam_anim_recording"


def _as_bool(value: Any) -> bool:
    if isinstance(value, bool):
        return value
    if value is None:
        return False
    if isinstance(value, (int, float)):
        return value != 0
    return str(value).strip().lower() in ("1", "true", "yes")

SEED_MODELS = [
    {
        "id": "mediapipe_holistic@v1",
        "kind": "pose",
        "label": "MediaPipe Holistic v1",
        "enabled": True,
        "defaultForKind": "pose",
        "detectorId": "continuuuum-remote",
        "notes": "Pose / webcam IK. Weights via pip install mediapipe (Python 3.12). Do not vendor .tflite.",
    },
    {
        "id": "yolo26_vehicle@intel",
        "kind": "vehicle",
        "label": "Intel YOLO26 vehicle detection",
        "enabled": True,
        "defaultForKind": "vehicle",
        "detectorId": "continuuuum-remote",
        "notes": "Required for exterior Vehicle takes. Intel/vehicle-detection YOLO26 (car/motorcycle/bus/truck). OpenVINO IR in YOLO26_CACHE; no MediaPipe fallback.",
    },
    {
        "id": "cabin_composite@v1",
        "kind": "vehicle",
        "label": "Cabin composite (pose + polar VO + optional YOLO)",
        "enabled": True,
        "defaultForKind": "",
        "detectorId": "continuuuum-remote",
        "notes": "In-cabin Vehicle takes: MediaPipe/MoCapAnything + polar windshield VO. YOLO26 is optional traffic through glass.",
    },
    {
        "id": "whisper@base",
        "kind": "whisper",
        "label": "Whisper base",
        "enabled": True,
        "defaultForKind": "whisper",
        "detectorId": "whisper-dialog",
        "notes": "Dialog ASR via USC /api/media (whisper@base). Continuuuum matches only; no local openai-whisper.",
    },
    {
        "id": "music_analysis@stub",
        "kind": "music",
        "label": "Music analysis stub",
        "enabled": True,
        "defaultForKind": "music",
        "detectorId": "music-analysis",
        "notes": "Song spans; stub detector",
    },
    {
        "id": "stub@local",
        "kind": "pose",
        "label": "Local stub",
        "enabled": True,
        "defaultForKind": "",
        "detectorId": "local-stub",
        "notes": "Fast Unity / server stub",
    },
    {
        "id": "mocapanything@v2",
        "kind": "pose",
        "label": "MoCapAnything V2",
        "enabled": True,
        "defaultForKind": "",
        "detectorId": "continuuuum-remote",
        "notes": "Multi-species BVH; requires type_metadata.species. pip/venv at MOCAPANYTHING_ROOT.",
    },
]

SEED_PROFILES = [
    {
        "id": "human-mediapipe-v1",
        "label": "Human (MediaPipe v1)",
        "enabled": True,
        "poseEngine": "mediapipe",
        "mediapipeSpec": "mediapipe_holistic@v1",
        "mocapSpec": "mocapanything@v2",
        "defaultSpecies": "",
        "mocapRoot": "",
        "mocapTimeoutSec": 10,
    },
    {
        "id": "animal-mocap-v2",
        "label": "Animal (MoCapAnything v2)",
        "enabled": True,
        "poseEngine": "mocapanything",
        "mediapipeSpec": "mediapipe_holistic@v1",
        "mocapSpec": "mocapanything@v2",
        "defaultSpecies": "Lion",
        "mocapRoot": "",
        "mocapTimeoutSec": 10,
    },
]

SCHEMA = """
CREATE TABLE IF NOT EXISTS webcam_anim_recordings (
  id TEXT PRIMARY KEY,
  kind TEXT NOT NULL DEFAULT 'webcam_anim_recording',
  webcam_anim_kind TEXT,
  model_spec TEXT,
  subsection TEXT,
  animation_list_index INTEGER DEFAULT 0,
  timeline_start_ms REAL DEFAULT 0,
  timeline_end_ms REAL DEFAULT 0,
  granularity TEXT,
  target_hint TEXT,
  type_metadata_json TEXT NOT NULL,
  library_doc_id TEXT,
  game TEXT,
  dimension TEXT,
  created_at TEXT NOT NULL,
  created_by TEXT
);
CREATE TABLE IF NOT EXISTS webcam_anim_models (
  id TEXT PRIMARY KEY,
  kind TEXT NOT NULL,
  label TEXT,
  enabled INTEGER NOT NULL DEFAULT 1,
  default_for_kind TEXT,
  detector_id TEXT,
  notes TEXT
);
CREATE TABLE IF NOT EXISTS webcam_anim_settings (
  key TEXT PRIMARY KEY,
  value TEXT
);
CREATE TABLE IF NOT EXISTS webcam_anim_upload_queue (
  id TEXT PRIMARY KEY,
  recording_id TEXT NOT NULL,
  status TEXT NOT NULL,
  created_at TEXT NOT NULL,
  started_at TEXT,
  finished_at TEXT,
  error TEXT,
  payload_json TEXT
);
"""


def ensure_webcam_anim_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(SCHEMA)
    try:
        conn.execute("ALTER TABLE webcam_anim_recordings ADD COLUMN created_by TEXT")
    except sqlite3.OperationalError:
        pass
    conn.commit()
    _seed_models_if_empty(conn)
    _ensure_concurrency(conn)
    _ensure_profiles(conn)


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _seed_models_if_empty(conn: sqlite3.Connection) -> None:
    for m in SEED_MODELS:
        exists = conn.execute(
            "SELECT 1 FROM webcam_anim_models WHERE id = ?", (m["id"],)
        ).fetchone()
        if exists:
            continue
        conn.execute(
            """
            INSERT INTO webcam_anim_models
              (id, kind, label, enabled, default_for_kind, detector_id, notes)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            (
                m["id"],
                m["kind"],
                m["label"],
                1 if m.get("enabled", True) else 0,
                m.get("defaultForKind") or "",
                m.get("detectorId") or "",
                m.get("notes") or "",
            ),
        )
    conn.commit()


def _ensure_concurrency(conn: sqlite3.Connection) -> None:
    row = conn.execute(
        "SELECT value FROM webcam_anim_settings WHERE key = 'totalConcurrency'"
    ).fetchone()
    if row is None:
        conn.execute(
            "INSERT INTO webcam_anim_settings (key, value) VALUES ('totalConcurrency', '1')"
        )
        conn.commit()


def get_total_concurrency(conn: sqlite3.Connection) -> int:
    _ensure_concurrency(conn)
    row = conn.execute(
        "SELECT value FROM webcam_anim_settings WHERE key = 'totalConcurrency'"
    ).fetchone()
    try:
        n = int(row["value"]) if row else 1
    except (TypeError, ValueError):
        n = 1
    return max(1, n)


def set_total_concurrency(conn: sqlite3.Connection, n: int) -> int:
    n = max(1, int(n))
    conn.execute(
        """
        INSERT INTO webcam_anim_settings (key, value) VALUES ('totalConcurrency', ?)
        ON CONFLICT(key) DO UPDATE SET value = excluded.value
        """,
        (str(n),),
    )
    conn.commit()
    return n


def _model_row(row: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": row["id"],
        "kind": row["kind"],
        "label": row["label"],
        "enabled": bool(row["enabled"]),
        "defaultForKind": row["default_for_kind"] or "",
        "detectorId": row["detector_id"] or "",
        "notes": row["notes"] or "",
    }


def list_models(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    _seed_models_if_empty(conn)
    rows = conn.execute(
        "SELECT * FROM webcam_anim_models ORDER BY kind, id"
    ).fetchall()
    return [_model_row(r) for r in rows]


def replace_models(conn: sqlite3.Connection, models: list[dict[str, Any]]) -> list[dict[str, Any]]:
    conn.execute("DELETE FROM webcam_anim_models")
    for m in models:
        mid = (m.get("id") or "").strip()
        if not mid:
            continue
        conn.execute(
            """
            INSERT INTO webcam_anim_models
              (id, kind, label, enabled, default_for_kind, detector_id, notes)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            (
                mid,
                (m.get("kind") or "pose").strip() or "pose",
                m.get("label") or mid,
                1 if m.get("enabled", True) else 0,
                m.get("defaultForKind") or m.get("default_for_kind") or "",
                m.get("detectorId") or m.get("detector_id") or "",
                m.get("notes") or "",
            ),
        )
    conn.commit()
    return list_models(conn)


def _normalize_profile(p: dict[str, Any]) -> dict[str, Any]:
    pid = (p.get("id") or "").strip()
    engine = (p.get("poseEngine") or p.get("pose_engine") or "mediapipe").strip()
    if engine not in ("mediapipe", "mocapanything"):
        engine = "mediapipe"
    timeout = p.get("mocapTimeoutSec")
    if timeout is None:
        timeout = p.get("mocap_timeout_sec")
    try:
        timeout = int(timeout) if timeout not in (None, "") else 10
    except (TypeError, ValueError):
        timeout = 10
    return {
        "id": pid,
        "label": p.get("label") or pid,
        "enabled": bool(p.get("enabled", True)),
        "poseEngine": engine,
        "mediapipeSpec": (p.get("mediapipeSpec") or p.get("mediapipe_spec") or "mediapipe_holistic@v1").strip()
        or "mediapipe_holistic@v1",
        "mocapSpec": (p.get("mocapSpec") or p.get("mocap_spec") or "mocapanything@v2").strip() or "mocapanything@v2",
        "defaultSpecies": p.get("defaultSpecies") or p.get("default_species") or "",
        "mocapRoot": p.get("mocapRoot") or p.get("mocap_root") or "",
        "mocapTimeoutSec": max(1, timeout),
    }


def _ensure_profiles(conn: sqlite3.Connection) -> None:
    row = conn.execute(
        "SELECT value FROM webcam_anim_settings WHERE key = 'detectorProfiles'"
    ).fetchone()
    raw = (row["value"] if row else "") or ""
    empty = not raw.strip() or raw.strip() == "[]"
    if empty:
        conn.execute(
            """
            INSERT INTO webcam_anim_settings (key, value) VALUES ('detectorProfiles', ?)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value
            """,
            (json.dumps(SEED_PROFILES),),
        )
        conn.commit()


def list_detector_profiles(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    _ensure_profiles(conn)
    row = conn.execute(
        "SELECT value FROM webcam_anim_settings WHERE key = 'detectorProfiles'"
    ).fetchone()
    try:
        data = json.loads((row["value"] if row else "") or "[]")
    except json.JSONDecodeError:
        data = []
    if not isinstance(data, list) or not data:
        return [_normalize_profile(dict(p)) for p in SEED_PROFILES]
    out = [_normalize_profile(p) for p in data if isinstance(p, dict) and (p.get("id") or "").strip()]
    return out or [_normalize_profile(dict(p)) for p in SEED_PROFILES]


def replace_detector_profiles(conn: sqlite3.Connection, profiles: list[dict[str, Any]]) -> list[dict[str, Any]]:
    out = [_normalize_profile(p) for p in profiles if isinstance(p, dict) and (p.get("id") or "").strip()]
    if not out:
        out = [_normalize_profile(dict(p)) for p in SEED_PROFILES]
    conn.execute(
        """
        INSERT INTO webcam_anim_settings (key, value) VALUES ('detectorProfiles', ?)
        ON CONFLICT(key) DO UPDATE SET value = excluded.value
        """,
        (json.dumps(out),),
    )
    conn.commit()
    return list_detector_profiles(conn)


def _pin_spec_to_catalog(conn: sqlite3.Connection, spec: str, engine: str) -> str:
    models = list_models(conn)
    enabled = {m["id"] for m in models if m.get("enabled") and (m.get("kind") or "pose") == "pose"}
    if spec in enabled:
        return spec
    fallback = "mocapanything@v2" if engine == "mocapanything" else "mediapipe_holistic@v1"
    ids = {m["id"] for m in models}
    if fallback in enabled or fallback in ids:
        return fallback
    return spec


def resolve_detector_profile(conn: sqlite3.Connection, profile_id: str | None) -> dict[str, Any]:
    """Return pinned model_spec / engine for a named settings profile."""
    ensure_webcam_anim_schema(conn)
    profiles = list_detector_profiles(conn)
    wanted = (profile_id or "").strip()
    if wanted:
        p = next((x for x in profiles if x["id"] == wanted), None)
        if p is None:
            raise KeyError(f"unknown detector profile: {wanted}")
        if not p.get("enabled"):
            raise KeyError(f"detector profile disabled: {wanted}")
    else:
        p = next((x for x in profiles if x.get("enabled")), None)
        if p is None:
            p = profiles[0] if profiles else _normalize_profile(dict(SEED_PROFILES[0]))
    engine = (p.get("poseEngine") or "mediapipe").strip() or "mediapipe"
    if engine == "mocapanything":
        spec = (p.get("mocapSpec") or "mocapanything@v2").strip()
    else:
        engine = "mediapipe"
        spec = (p.get("mediapipeSpec") or "mediapipe_holistic@v1").strip()
    spec = _pin_spec_to_catalog(conn, spec, engine)
    return {
        "model_spec": spec,
        "engine": engine,
        "speciesDefault": p.get("defaultSpecies") or "",
        "profile": p,
        "mocapRoot": p.get("mocapRoot") or "",
        "mocapTimeoutSec": p.get("mocapTimeoutSec") if p.get("mocapTimeoutSec") not in (None, "") else 10,
    }


def apply_detector_profile(conn: sqlite3.Connection, meta: dict[str, Any], profile_id: str) -> dict[str, Any]:
    resolved = resolve_detector_profile(conn, profile_id)
    out = dict(meta)
    out["model_spec"] = resolved["model_spec"]
    out["detectorProfileId"] = (resolved.get("profile") or {}).get("id") or profile_id
    out["poseEngine"] = resolved["engine"]
    if resolved.get("speciesDefault") and not (out.get("species") or "").strip():
        out["species"] = resolved["speciesDefault"]
    if resolved.get("mocapRoot"):
        out["mocapRoot"] = resolved["mocapRoot"]
    if resolved.get("mocapTimeoutSec") is not None:
        out["mocapTimeoutSec"] = resolved["mocapTimeoutSec"]
    return out


def models_payload(conn: sqlite3.Connection) -> dict[str, Any]:
    return {
        "models": list_models(conn),
        "totalConcurrency": get_total_concurrency(conn),
        "detectorProfiles": list_detector_profiles(conn),
    }


def insert_and_enqueue_recording(
    conn: sqlite3.Connection,
    meta: dict[str, Any],
    *,
    library_doc_id: str | None = None,
    file_path: str | None = None,
    created_by: str | None = None,
    rec_id: str | None = None,
    game: str | None = None,
    dimension: str | None = None,
    drain_complete: bool = False,
) -> dict[str, Any]:
    ensure_webcam_anim_schema(conn)
    rec_id = rec_id or ("wrec_" + uuid.uuid4().hex[:12])
    conn.execute(
        """
        INSERT INTO webcam_anim_recordings (
          id, kind, webcam_anim_kind, model_spec, subsection,
          animation_list_index, timeline_start_ms, timeline_end_ms,
          granularity, target_hint, type_metadata_json, library_doc_id,
          game, dimension, created_at, created_by
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (
            rec_id,
            meta.get("kind") or KIND_VALUE,
            meta.get("webcamAnimKind") or "ambulatory",
            meta.get("model_spec") or "",
            meta.get("subsection") or "",
            int(meta.get("animationListIndex") or 0),
            float(meta.get("timelineStartMs") or 0),
            float(meta.get("timelineEndMs") or 0),
            meta.get("granularity") or "millisecond",
            meta.get("targetHint") or "ragdoll",
            json.dumps(meta),
            library_doc_id or None,
            game or None,
            dimension or None,
            _now(),
            created_by,
        ),
    )
    conn.commit()
    payload = dict(meta)
    if library_doc_id:
        payload["libraryDocId"] = library_doc_id
    if file_path:
        payload["file_path"] = file_path
    enqueue_upload(conn, rec_id, payload)
    drain_queue(conn, complete=drain_complete)
    row = conn.execute(
        "SELECT * FROM webcam_anim_recordings WHERE id = ?", (rec_id,)
    ).fetchone()
    return _row_to_dict(conn, row)


def _queue_info_for(conn: sqlite3.Connection, rec_id: str) -> tuple[str, str | None]:
    row = conn.execute(
        """
        SELECT status, error FROM webcam_anim_upload_queue
        WHERE recording_id = ?
        ORDER BY created_at DESC
        LIMIT 1
        """,
        (rec_id,),
    ).fetchone()
    if row is None:
        return "none", None
    return row["status"], row["error"]


def _preview_url(doc: dict[str, Any]) -> str | None:
    status = doc.get("queueStatus") or "none"
    if status in ("queued", "running", "failed"):
        return None
    api_base = ""
    try:
        api_base = (request.host_url or "").rstrip("/")
    except RuntimeError:
        api_base = ""
    params = urlencode(
        {
            "docId": doc.get("libraryDocId") or doc.get("id") or "",
            "apiBase": api_base,
            "subsection": doc.get("subsection") or "",
            "startMs": str(int(doc.get("timelineStartMs") or 0)),
            "endMs": str(int(doc.get("timelineEndMs") or 0)),
        }
    )
    return f"/continuuuum_editor/index.html?{params}"


def _row_to_dict(conn: sqlite3.Connection, row: sqlite3.Row) -> dict[str, Any]:
    meta = {}
    raw = row["type_metadata_json"] if "type_metadata_json" in row.keys() else "{}"
    try:
        meta = json.loads(raw or "{}")
    except json.JSONDecodeError:
        meta = {}
    if not isinstance(meta, dict):
        meta = {}
    rec_id = row["id"]
    status, queue_error = _queue_info_for(conn, rec_id)
    if status == "none":
        status = "done"
    out = {
        "id": rec_id,
        "kind": row["kind"],
        "webcamAnimKind": row["webcam_anim_kind"],
        "model_spec": row["model_spec"],
        "subsection": row["subsection"],
        "animationListIndex": row["animation_list_index"],
        "timelineStartMs": row["timeline_start_ms"],
        "timelineEndMs": row["timeline_end_ms"],
        "granularity": row["granularity"],
        "targetHint": row["target_hint"],
        "libraryDocId": row["library_doc_id"],
        "type_metadata": meta,
        "createdAt": row["created_at"],
        "queueStatus": status,
        "queueError": queue_error,
        "species": meta.get("species") or "",
        "poseTrackPath": meta.get("poseTrackPath") or meta.get("pose_track_path") or "",
        "vehicleTrackPath": meta.get("vehicleTrackPath") or meta.get("vehicle_track_path") or "",
        "polarVelocityPath": meta.get("polarVelocityPath") or meta.get("polar_velocity_path") or "",
        "cabinCamera": _as_bool(meta.get("cabinCamera") if meta.get("cabinCamera") is not None else meta.get("cabin_camera")),
        "inferShoulderShifts": _as_bool(
            meta.get("inferShoulderShifts") if meta.get("inferShoulderShifts") is not None else meta.get("infer_shoulder_shifts")
        ),
        "facingYawDegrees": float(meta.get("facingYawDegrees") or meta.get("facing_yaw_degrees") or 0),
    }
    out["previewUrl"] = _preview_url(out)
    return out


def _extract_meta(body: dict[str, Any]) -> dict[str, Any]:
    nested = body.get("type_metadata")
    if isinstance(nested, str):
        try:
            nested = json.loads(nested)
        except json.JSONDecodeError:
            nested = {}
    if isinstance(nested, dict) and nested:
        src = dict(nested)
    else:
        src = dict(body)
    kind = (src.get("kind") or KIND_VALUE).strip() or KIND_VALUE
    return {
        "kind": kind,
        "webcamAnimKind": src.get("webcamAnimKind") or src.get("webcam_anim_kind") or "ambulatory",
        "model_spec": src.get("model_spec") or src.get("modelSpec") or "",
        "subsection": src.get("subsection") or src.get("subsectionId") or "",
        "animationListIndex": int(src.get("animationListIndex") or 0),
        "timelineStartMs": float(src.get("timelineStartMs") or src.get("timeline_start_ms") or 0),
        "timelineEndMs": float(src.get("timelineEndMs") or src.get("timeline_end_ms") or 0),
        "granularity": src.get("granularity") or "millisecond",
        "targetHint": src.get("targetHint") or src.get("target_hint") or "ragdoll",
        "species": src.get("species") or "",
        "poseTrackPath": src.get("poseTrackPath") or src.get("pose_track_path") or "",
        "vehicleTrackPath": src.get("vehicleTrackPath") or src.get("vehicle_track_path") or "",
        "polarVelocityPath": src.get("polarVelocityPath") or src.get("polar_velocity_path") or "",
        "cabinCamera": _as_bool(src.get("cabinCamera") if "cabinCamera" in src else src.get("cabin_camera")),
        "inferShoulderShifts": _as_bool(
            src.get("inferShoulderShifts") if "inferShoulderShifts" in src else src.get("infer_shoulder_shifts")
        ),
        "facingYawDegrees": float(src.get("facingYawDegrees") or src.get("facing_yaw_degrees") or 0),
        "detectorProfileId": src.get("detectorProfileId") or src.get("detector_profile_id") or "",
        "mocapRoot": src.get("mocapRoot") or src.get("mocap_root") or "",
        "mocapTimeoutSec": src.get("mocapTimeoutSec") or src.get("mocap_timeout_sec") or "",
        "voxelRagdoll": src.get("voxelRagdoll") if "voxelRagdoll" in src else src.get("voxel_ragdoll"),
        "spatialGranularity": src.get("spatialGranularity") or src.get("spatial_granularity"),
        "axisArt": src.get("axisArt") or src.get("axis_art"),
    }


def enqueue_upload(conn: sqlite3.Connection, recording_id: str, payload: dict[str, Any]) -> str:
    qid = "q_" + uuid.uuid4().hex[:12]
    conn.execute(
        """
        INSERT INTO webcam_anim_upload_queue
          (id, recording_id, status, created_at, payload_json)
        VALUES (?, ?, 'queued', ?, ?)
        """,
        (qid, recording_id, _now(), json.dumps(payload)),
    )
    conn.commit()
    return qid


def _running_count(conn: sqlite3.Connection) -> int:
    return conn.execute(
        "SELECT COUNT(*) AS c FROM webcam_anim_upload_queue WHERE status = 'running'"
    ).fetchone()["c"]


def _promote_queued(conn: sqlite3.Connection) -> None:
    cap = get_total_concurrency(conn)
    while _running_count(conn) < cap:
        row = conn.execute(
            """
            SELECT id FROM webcam_anim_upload_queue
            WHERE status = 'queued'
            ORDER BY created_at ASC
            LIMIT 1
            """
        ).fetchone()
        if row is None:
            break
        conn.execute(
            """
            UPDATE webcam_anim_upload_queue
            SET status = 'running', started_at = ?
            WHERE id = ?
            """,
            (_now(), row["id"]),
        )
        conn.commit()


def _merge_recording_meta(conn: sqlite3.Connection, rec_id: str, extra: dict[str, Any]) -> None:
    row = conn.execute(
        "SELECT type_metadata_json FROM webcam_anim_recordings WHERE id = ?", (rec_id,)
    ).fetchone()
    meta: dict[str, Any] = {}
    if row is not None:
        try:
            meta = json.loads(row["type_metadata_json"] or "{}")
        except json.JSONDecodeError:
            meta = {}
    if not isinstance(meta, dict):
        meta = {}
    if extra.get("pose_track_path"):
        meta["poseTrackPath"] = extra["pose_track_path"]
    if extra.get("vehicle_track_path"):
        meta["vehicleTrackPath"] = extra["vehicle_track_path"]
    if extra.get("polar_velocity_path"):
        meta["polarVelocityPath"] = extra["polar_velocity_path"]
    if extra.get("bvh_path"):
        meta["bvhPath"] = extra["bvh_path"]
    if extra.get("npy_path"):
        meta["npyPath"] = extra["npy_path"]
    if extra.get("dialogSpans") is not None:
        meta["dialogSpans"] = extra["dialogSpans"]
    if extra.get("dialogue_set") is not None:
        meta["dialogueSet"] = extra["dialogue_set"]
    if extra.get("dialogue_set_id"):
        meta["dialogueSetId"] = extra["dialogue_set_id"]
    if extra.get("whisper_json") is not None:
        meta["whisperJson"] = extra["whisper_json"]
    conn.execute(
        "UPDATE webcam_anim_recordings SET type_metadata_json = ? WHERE id = ?",
        (json.dumps(meta), rec_id),
    )


def _complete_running(conn: sqlite3.Connection) -> None:
    rows = conn.execute(
        "SELECT * FROM webcam_anim_upload_queue WHERE status = 'running' ORDER BY started_at ASC"
    ).fetchall()
    for row in rows:
        payload = {}
        try:
            payload = json.loads(row["payload_json"] or "{}")
        except json.JSONDecodeError:
            payload = {}
        file_path = payload.get("file_path") or ""
        rec_id = row["recording_id"]
        payload["recording_id"] = rec_id
        spec = (payload.get("model_spec") or "").strip()
        err = None
        pending = False
        artifacts: dict[str, Any] = {}
        if file_path and Path(file_path).is_file() and (
            is_gpu_spec(spec) or is_whisper_spec(spec) or is_vehicle_spec(spec) or is_cabin_spec(spec)
        ):
            try:
                artifacts = run_detect_hop(file_path, payload)
            except DetectPending:
                pending = True
            except Exception as exc:  # noqa: BLE001
                err = str(exc)
        now = _now()
        if pending:
            conn.execute(
                "UPDATE webcam_anim_upload_queue SET payload_json = ? WHERE id = ?",
                (json.dumps(payload), row["id"]),
            )
            conn.commit()
            continue
        if err:
            conn.execute(
                """
                UPDATE webcam_anim_upload_queue
                SET status = 'failed', finished_at = ?, error = ?
                WHERE id = ?
                """,
                (now, err, row["id"]),
            )
        else:
            conn.execute(
                """
                UPDATE webcam_anim_upload_queue
                SET status = 'done', finished_at = ?, error = NULL
                WHERE id = ?
                """,
                (now, row["id"]),
            )
            lib_id = payload.get("libraryDocId") or rec_id
            conn.execute(
                "UPDATE webcam_anim_recordings SET library_doc_id = COALESCE(library_doc_id, ?) WHERE id = ?",
                (lib_id, rec_id),
            )
            if artifacts:
                _merge_recording_meta(conn, rec_id, artifacts)
                dset = artifacts.get("dialogue_set")
                if isinstance(dset, dict) and dset.get("nodes") is not None:
                    try:
                        from continuuuum_api.dialogue_db import ensure_dialogue_schema, save_compiled_set
                    except ImportError:
                        from dialogue_db import ensure_dialogue_schema, save_compiled_set
                    try:
                        ensure_dialogue_schema(conn)
                        save_compiled_set(
                            conn,
                            set_id=str(dset.get("setId") or f"webcam-whisper-{rec_id}"),
                            lemma_entry_id=None,
                            name=f"Webcam whisper {rec_id[:8]}",
                            compiled=dset,
                        )
                    except Exception:  # noqa: BLE001
                        pass
        conn.commit()


def drain_queue(conn: sqlite3.Connection, *, complete: bool) -> None:
    """Promote queued jobs up to totalConcurrency. If complete, finish running jobs first."""
    if complete:
        _complete_running(conn)
    _promote_queued(conn)


def register_webcam_anim_routes(app: Any, get_conn: GetConn) -> None:
    static_dir = Path(__file__).resolve().parent / "static" / "webcam-animations"

    def _conn() -> sqlite3.Connection:
        conn = get_conn()
        ensure_webcam_anim_schema(conn)
        return conn

    @app.route("/webcam-animations")
    @app.route("/webcam-animations/")
    @app.route("/webcam-animations/<path:subpath>")
    def webcam_animations_spa(subpath: str = ""):
        if not static_dir.is_dir():
            return jsonify({"error": "webcam-animations SPA missing"}), 404
        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")

    @app.route("/api/webcam-animations/models", methods=["GET"])
    def webcam_anim_models_get():
        conn = _conn()
        return jsonify(models_payload(conn))

    @app.route("/api/webcam-animations/models", methods=["PUT"])
    def webcam_anim_models_put():
        body = request.get_json(silent=True) or {}
        conn = _conn()
        models = body.get("models")
        if isinstance(models, list):
            replace_models(conn, models)
        if body.get("totalConcurrency") is not None:
            set_total_concurrency(conn, int(body.get("totalConcurrency") or 1))
        if isinstance(body.get("detectorProfiles"), list):
            replace_detector_profiles(conn, body.get("detectorProfiles") or [])
        return jsonify(models_payload(conn))

    @app.route("/api/webcam-animations", methods=["GET"])
    def webcam_anim_list():
        kind = (request.args.get("kind") or KIND_VALUE).strip() or KIND_VALUE
        conn = _conn()
        drain_queue(conn, complete=True)
        rows = conn.execute(
            """
            SELECT * FROM webcam_anim_recordings
            WHERE kind = ?
            ORDER BY created_at DESC
            """,
            (kind,),
        ).fetchall()
        return jsonify([_row_to_dict(conn, r) for r in rows])

    @app.route("/api/webcam-animations", methods=["POST"])
    def webcam_anim_create():
        body = request.get_json(silent=True)
        if body is None:
            if request.form:
                raw_meta = request.form.get("type_metadata") or "{}"
                try:
                    body = json.loads(raw_meta)
                except json.JSONDecodeError:
                    body = {"type_metadata": raw_meta}
                if request.form.get("document_type"):
                    body.setdefault("document_type", request.form.get("document_type"))
            else:
                body = {}
        if not isinstance(body, dict):
            return jsonify({"error": "json object required"}), 400
        conn = _conn()
        meta = _extract_meta(body)
        profile_id = (
            body.get("detectorProfileId")
            or body.get("detector_profile_id")
            or meta.get("detectorProfileId")
            or ""
        ).strip()
        if profile_id:
            try:
                meta = apply_detector_profile(conn, meta, profile_id)
            except KeyError as exc:
                return jsonify({"error": str(exc)}), 400
        spec = (meta.get("model_spec") or "").strip()
        kind_l = (meta.get("webcamAnimKind") or "").strip().lower()
        cabin = _as_bool(meta.get("cabinCamera"))
        if kind_l == "vehicle" and cabin and not spec:
            meta["model_spec"] = "cabin_composite@v1"
            spec = meta["model_spec"]
        elif kind_l == "vehicle" and cabin and spec.startswith("yolo26_vehicle@"):
            meta["model_spec"] = "cabin_composite@v1"
            spec = meta["model_spec"]
        elif kind_l == "vehicle" and not spec:
            meta["model_spec"] = "yolo26_vehicle@intel"
            spec = meta["model_spec"]
        if spec.startswith("mocapanything@") and not (meta.get("species") or "").strip():
            return jsonify({"error": "mocapanything@v2 requires species"}), 400
        rec_id = (body.get("id") or "").strip() or ("wrec_" + uuid.uuid4().hex[:12])
        library_doc_id = (body.get("libraryDocId") or body.get("library_doc_id") or "").strip()
        game = (request.headers.get("X-Game") or body.get("game") or "").strip()
        dimension = (request.headers.get("X-Dimension") or body.get("dimension") or "").strip()
        created_by = (request.headers.get("X-User-ID") or body.get("createdBy") or "").strip() or None
        file_path = None
        upload = request.files.get("file") if request.files else None
        if upload and upload.filename:
            dest_dir = Path(tempfile.gettempdir()) / "webcam_anim_uploads"
            dest_dir.mkdir(parents=True, exist_ok=True)
            dest = dest_dir / (rec_id + "_" + Path(upload.filename).name)
            upload.save(dest)
            file_path = str(dest)
        doc = insert_and_enqueue_recording(
            conn,
            meta,
            library_doc_id=library_doc_id or None,
            file_path=file_path,
            created_by=created_by,
            rec_id=rec_id,
            game=game or None,
            dimension=dimension or None,
            drain_complete=False,
        )
        return jsonify(doc), 201

    @app.route("/api/webcam-animations/<rec_id>", methods=["GET"])
    def webcam_anim_get(rec_id: str):
        conn = _conn()
        drain_queue(conn, complete=True)
        row = conn.execute(
            "SELECT * FROM webcam_anim_recordings WHERE id = ?", (rec_id,)
        ).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        return jsonify(_row_to_dict(conn, row))

    @app.route("/api/webcam-animations/<rec_id>/posetrack", methods=["GET"])
    def webcam_anim_posetrack(rec_id: str):
        conn = _conn()
        row = conn.execute(
            "SELECT * FROM webcam_anim_recordings WHERE id = ?", (rec_id,)
        ).fetchone()
        if row is None:
            return jsonify({"error": "not found"}), 404
        doc = _row_to_dict(conn, row)
        path = (doc.get("poseTrackPath") or "").strip()
        if not path or not Path(path).is_file():
            return jsonify({"error": "pose track not ready", "queueStatus": doc.get("queueStatus")}), 404
        try:
            data = json.loads(Path(path).read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            return jsonify({"error": str(exc)}), 500
        return jsonify(data)

    @app.route("/api/webcam-animations/live-pose", methods=["POST"])
    def webcam_anim_live_pose():
        upload = request.files.get("file") if request.files else None
        if upload is None or not upload.filename:
            return jsonify({"error": "image file required"}), 400
        spec = (request.form.get("model_spec") or request.args.get("model_spec") or "mediapipe_holistic@v1").strip()
        t_ms = request.form.get("tMs") or request.form.get("t_ms") or "0"
        dest_dir = Path(tempfile.gettempdir()) / "webcam_anim_live_pose"
        dest_dir.mkdir(parents=True, exist_ok=True)
        dest = dest_dir / (uuid.uuid4().hex[:12] + (Path(upload.filename).suffix or ".jpg"))
        upload.save(dest)
        try:
            try:
                from pose_detectors.mediapipe_holistic import INSTALL_HINT, run_image
            except ImportError:
                from continuuuum_api.pose_detectors.mediapipe_holistic import INSTALL_HINT, run_image
            try:
                body = run_image(str(dest), {"model_spec": spec, "tMs": float(t_ms)})
            except RuntimeError as exc:
                msg = str(exc)
                code = 503 if "mediapipe" in msg.lower() or "install mediapipe" in msg.lower() else 400
                return jsonify({"error": msg}), code
            return jsonify(body)
        finally:
            try:
                dest.unlink(missing_ok=True)
            except OSError:
                pass
