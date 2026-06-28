"""Tests for cloud bake gradient CLI helpers."""

from __future__ import annotations

import json
import sys
import tempfile
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "video_storage_tool"))

from cloud_bake_from_video import build_frame_records  # noqa: E402
from video_to_script import _compute_color_gradient  # noqa: E402


def test_compute_color_gradient_nonempty():
    try:
        from PIL import Image
    except ImportError:
        pytest.skip("Pillow not installed")
    img = Image.new("RGB", (64, 64), color=(100, 150, 200))
    grad = _compute_color_gradient(img)
    assert "top=" in grad
    assert "mid=" in grad
    assert "bottom=" in grad


def test_build_frame_records_allow_float_away_flag():
    records_false = build_frame_records(Path("missing.ogv"), max_frames=1, allow_float_away=False)
    assert records_false == [] or records_false[0]["allowFloatAway"] is False

    rec = {
        "frameIndex": 0,
        "allowFloatAway": True,
        "advection": {"mode": "WeatherSolverAdvection"},
    }
    assert rec["allowFloatAway"] is True


def test_mock_loss_decreases():
    losses = [1.0, 0.8, 0.6, 0.45, 0.3]
    for i in range(1, len(losses)):
        assert losses[i] <= losses[i - 1]


def test_schema_roundtrip_allow_float_away():
    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp) / "timeline.json"
        payload = {
            "frames": [
                {
                    "frameIndex": 0,
                    "allowFloatAway": False,
                    "gradient": "top=#aabbcc mid=#ddeeff bottom=#001122",
                    "convexion": {"bias": 0.25, "size": 0.5},
                }
            ]
        }
        path.write_text(json.dumps(payload), encoding="utf-8")
        loaded = json.loads(path.read_text(encoding="utf-8"))
        assert loaded["frames"][0]["allowFloatAway"] is False
        assert loaded["frames"][0]["convexion"]["bias"] == 0.25
        assert loaded["frames"][0]["convexion"]["size"] == 0.5


def test_build_frame_records_convexion_fields():
    records = build_frame_records(
        Path("missing.ogv"),
        max_frames=0,
        convexion_bias=-0.5,
        convexion_size=0.75,
    )
    assert records == []
