"""MoCapAnything video2pose2rot subprocess hop. GPU jobs may raise DetectPending."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path
from typing import Any

from .dispatch import DetectPending

DEFAULT_REPO = Path(os.environ.get("MOCAPANYTHING_ROOT", r"D:\Development\MocapAnything"))
DEFAULT_PYTHON = Path(
    os.environ.get(
        "MOCAPANYTHING_PYTHON",
        str(DEFAULT_REPO / ".venv" / "Scripts" / "python.exe"),
    )
)


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    species = (payload.get("species") or "").strip()
    if not species:
        raise RuntimeError("mocapanything@v2 requires type_metadata.species")

    repo = Path(payload.get("mocap_root") or DEFAULT_REPO)
    py = Path(payload.get("mocap_python") or DEFAULT_PYTHON)
    wrapper = Path(__file__).resolve().parent / "run_mocap_anything.py"
    out_dir = Path(file_path).parent / "mocap_out" / (payload.get("recording_id") or "rec")
    out_dir.mkdir(parents=True, exist_ok=True)
    stamp = out_dir / ".started"
    done = out_dir / ".done"

    if done.is_file():
        return _collect(out_dir)

    if stamp.is_file() and not done.is_file():
        raise DetectPending("mocapanything subprocess still running")

    if not py.is_file():
        raise DetectPending(f"mocapanything python missing: {py}")
    if not repo.is_dir():
        raise DetectPending(f"mocapanything repo missing: {repo}")

    stamp.write_text("1", encoding="utf-8")
    cmd = [
        str(py),
        str(wrapper),
        "--video",
        file_path,
        "--species",
        species,
        "--out",
        str(out_dir),
        "--repo",
        str(repo),
    ]
    timeout = int(payload.get("mocap_timeout_sec") or 10)
    try:
        proc = subprocess.run(
            cmd,
            cwd=str(repo),
            capture_output=True,
            text=True,
            timeout=timeout,
            check=False,
        )
    except subprocess.TimeoutExpired as exc:
        raise DetectPending(f"mocapanything still running: {exc}") from exc

    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "mocapanything failed").strip()
        raise RuntimeError(err[:2000])

    done.write_text("1", encoding="utf-8")
    return _collect(out_dir)


def _collect(out_dir: Path) -> dict[str, Any]:
    bvh = next(out_dir.rglob("*.bvh"), None)
    npy = next(out_dir.rglob("*_pose_pred.npy"), None)
    track = next(out_dir.rglob("*.posetrack.json"), None)
    return {
        "bvh_path": str(bvh) if bvh else "",
        "npy_path": str(npy) if npy else "",
        "pose_track_path": str(track) if track else "",
    }
