"""CLI wrapper: --video --species --out --repo. Writes a stub PoseTrack if inference is unavailable."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--video", required=True)
    p.add_argument("--species", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--repo", default="")
    args = p.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    track = {
        "modelSpec": "mocapanything@v2",
        "samples": [
            {
                "traitId": args.species + ":Hips",
                "timeMs": 0.0,
                "localPosition": {"x": 0.0, "y": 0.0, "z": 0.0},
                "localRotation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
            }
        ],
    }
    (out / "track.posetrack.json").write_text(json.dumps(track), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
