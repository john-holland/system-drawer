#!/usr/bin/env python3
"""Batch cloud-bake gradient extraction from video frames for Unity import."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIDEO_TOOL = ROOT / "video_storage_tool"
if str(VIDEO_TOOL) not in sys.path:
    sys.path.insert(0, str(VIDEO_TOOL))

from video_to_script import _compute_color_gradient, _describe_video_frames  # noqa: E402


def build_frame_records(
    video_path: Path,
    *,
    max_frames: int = 10,
    allow_float_away: bool = False,
    convexion_bias: float = 0.0,
    convexion_size: float = 0.5,
) -> list[dict]:
    config = {
        "visual_max_frames": max_frames,
        "granularity_sec": 1.0,
    }
    visual, gradient, _style = _describe_video_frames(video_path, config)
    records: list[dict] = []
    gradient_lines = [ln.strip() for ln in (gradient or "").splitlines() if ln.strip()]
    for i, line in enumerate(gradient_lines[:max_frames]):
        grad = line.split(":", 1)[-1].strip() if ":" in line else line
        records.append(
            {
                "frameIndex": i,
                "allowFloatAway": allow_float_away,
                "anchorHash": 0,
                "gradient": grad,
                "advection": {
                    "mode": "WeatherSolverAdvection" if allow_float_away else "AnchorReset",
                },
                "convexion": {"bias": convexion_bias, "size": convexion_size},
            }
        )
    if not records and visual:
        records.append(
            {
                "frameIndex": 0,
                "allowFloatAway": allow_float_away,
                "gradient": "",
                "advection": {"mode": "WeatherSolverAdvection" if allow_float_away else "AnchorReset"},
                "convexion": {"bias": convexion_bias, "size": convexion_size},
            }
        )
    return records


def main() -> int:
    parser = argparse.ArgumentParser(description="Extract cloud-bake frame gradients from video.")
    parser.add_argument("video", type=Path, help="Input video file")
    parser.add_argument("-o", "--output", type=Path, default=Path("cloud_bake_timeline.json"))
    parser.add_argument("--max-frames", type=int, default=10)
    parser.add_argument(
        "--allow-float-away",
        action="store_true",
        help="Enable weather-solver advection between frames (default: anchor reset)",
    )
    parser.add_argument("--convexion-bias", type=float, default=0.0)
    parser.add_argument("--convexion-size", type=float, default=0.5)
    args = parser.parse_args()

    if not args.video.is_file():
        print(f"Video not found: {args.video}", file=sys.stderr)
        return 1

    records = build_frame_records(
        args.video,
        max_frames=args.max_frames,
        allow_float_away=args.allow_float_away,
        convexion_bias=args.convexion_bias,
        convexion_size=args.convexion_size,
    )
    payload = {"frames": records, "source": str(args.video)}
    args.output.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"Wrote {len(records)} frame records to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
