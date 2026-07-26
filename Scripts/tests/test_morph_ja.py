"""Japanese conjugator."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from morphology import conjugate  # noqa: E402


def test_godan_kaku():
    assert conjugate("ja", "書く", {"tense": "masu", "politeness": "polite"}) == "書きます"
    assert conjugate("ja", "書く", {"tense": "te"}) == "書いて"
    assert conjugate("ja", "書く", {"tense": "nai", "polarity": "negative"}) == "書かない"


def test_ichidan_taberu():
    assert conjugate("ja", "食べる", {"tense": "masu"}) == "食べます"
    assert conjugate("ja", "食べる", {"tense": "te"}) == "食べて"
    assert conjugate("ja", "食べる", {"tense": "nai"}) == "食べない"


def test_suru_kuru():
    assert conjugate("ja", "する", {"tense": "masu"}) == "します"
    assert conjugate("ja", "する", {"tense": "te"}) == "して"
    assert conjugate("ja", "来る", {"tense": "masu"}) == "来ます"
    assert conjugate("ja", "来る", {"tense": "nai"}) == "来ない"
