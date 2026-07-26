"""Korean conjugator."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from morphology import conjugate  # noqa: E402


def test_hada_haeyo():
    assert conjugate("ko", "하다", {"formality": "haeyo", "tense": "present"}) == "해요"
    assert conjugate("ko", "하다", {"formality": "haeyo", "tense": "past"}) == "했어요"


def test_alda_haeyo():
    assert conjugate("ko", "알다", {"formality": "haeyo", "tense": "present"}) == "알아요"
    assert conjugate("ko", "알다", {"formality": "haeyo", "tense": "past"}) == "알았어요"
