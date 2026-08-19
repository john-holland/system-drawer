"""PoseTrack JSON shared with Unity JsonUtility (traitId, timeMs, localPosition, localRotation)."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any


def sample(
    trait_id: str,
    time_ms: float,
    x: float = 0.0,
    y: float = 0.0,
    z: float = 0.0,
    qx: float = 0.0,
    qy: float = 0.0,
    qz: float = 0.0,
    qw: float = 1.0,
) -> dict[str, Any]:
    return {
        "traitId": trait_id,
        "timeMs": float(time_ms),
        "localPosition": {"x": float(x), "y": float(y), "z": float(z)},
        "localRotation": {"x": float(qx), "y": float(qy), "z": float(qz), "w": float(qw)},
    }


def write_track(path: Path, model_spec: str, samples: list[dict[str, Any]]) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    payload = {"modelSpec": model_spec, "samples": samples}
    path.write_text(json.dumps(payload), encoding="utf-8")
    return path
