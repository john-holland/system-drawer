"""Mandarin aspect/polarity transforms."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from morphology import conjugate  # noqa: E402


def test_le_guo_mei():
    assert conjugate("zh", "做", {"aspect": "le"}) == "做 了"
    assert conjugate("zh", "做", {"aspect": "guo"}) == "做 过"
    assert conjugate("zh", "做", {"polarity": "mei"}) == "没 做"
    assert conjugate("zh", "做", {"polarity": "mei", "aspect": "le"}) == "没 做"
