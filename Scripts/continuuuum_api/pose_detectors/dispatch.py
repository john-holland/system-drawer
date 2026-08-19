"""Dispatch detect hops by model_spec. GPU specs may raise DetectPending to stay running."""

from __future__ import annotations

from typing import Any, Callable

HopFn = Callable[[str, dict[str, Any]], dict[str, Any]]

GPU_SPECS = ("mediapipe_holistic@v1", "mocapanything@v2")

_HOPS: dict[str, HopFn] = {}


class DetectPending(Exception):
    """Hop started but not finished — leave the queue row running."""


def is_gpu_spec(spec: str) -> bool:
    s = (spec or "").strip()
    return any(s == g or s.startswith(g.split("@")[0] + "@") for g in GPU_SPECS) or s.startswith(
        "mocapanything@"
    ) or s.startswith("mediapipe_holistic@")


def is_whisper_spec(spec: str) -> bool:
    return (spec or "").strip().startswith("whisper@")


def set_hop_runner(spec: str, fn: HopFn | None) -> None:
    if fn is None:
        _HOPS.pop(spec, None)
    else:
        _HOPS[spec] = fn


def _default_runner(spec: str) -> HopFn | None:
    if spec.startswith("mediapipe_holistic@"):
        from .mediapipe_holistic import run

        return run
    if spec.startswith("mocapanything@"):
        from .mocap_anything import run

        return run
    if spec.startswith("whisper@"):
        from .whisper_usc import run

        return run
    return None


def run_detect_hop(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    spec = (payload.get("model_spec") or "").strip()
    runner = _HOPS.get(spec) or _default_runner(spec)
    if runner is None:
        return {}
    return runner(file_path, payload) or {}
