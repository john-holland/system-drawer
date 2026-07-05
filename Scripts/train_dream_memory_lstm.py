"""Train dream-memory LSTM from wave IO + day labels (v1 deterministic replay stub)."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

try:
    from continuuuum_api.sleep_sim import run_sleep_sim
except ImportError:
    from sleep_sim import run_sleep_sim


def load_day_sessions(path: Path) -> list[dict[str, Any]]:
    if not path.exists():
        return []
    data = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(data, list):
        return data
    return data.get("sessions") or []


def train_stub(sessions: list[dict[str, Any]], out_path: Path) -> dict[str, Any]:
    """v1: export replay manifest instead of ONNX weights."""
    manifest: list[dict[str, Any]] = []
    for session in sessions:
        wave = run_sleep_sim(session, session.get("dayCollapseSeed"))
        manifest.append(
            {
                "sessionId": session.get("sessionId"),
                "dayCollapseSeed": session.get("dayCollapseSeed"),
                "waveSampleCount": len(wave.get("waveSamples") or []),
                "mode": "deterministic_replay",
            }
        )
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps({"manifest": manifest, "onnx": None}, indent=2), encoding="utf-8")
    return {"trained": len(manifest), "output": str(out_path)}


def main() -> None:
    parser = argparse.ArgumentParser(description="Dream memory LSTM train stub")
    parser.add_argument("--input", type=Path, default=Path("data/dream_day_sessions.json"))
    parser.add_argument("--output", type=Path, default=Path("models/dream_memory_manifest.json"))
    args = parser.parse_args()
    sessions = load_day_sessions(args.input)
    if not sessions:
        demo = {"sessionId": "demo", "dayCollapseSeed": 42, "aspectStates": [{"satisfied01": 0.6}]}
        sessions = [demo]
    result = train_stub(sessions, args.output)
    print(json.dumps(result))


if __name__ == "__main__":
    main()
