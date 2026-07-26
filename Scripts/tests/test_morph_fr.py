"""French generative conjugator."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from morphology import conjugate  # noqa: E402


def test_regular_er_present():
    assert conjugate("fr", "parler", {"person": "1", "number": "singular"}) == "parle"
    assert conjugate("fr", "parler", {"person": "1", "number": "plural"}) == "parlons"


def test_irregular_etre_avoir_faire():
    assert conjugate("fr", "être", {"person": "3", "number": "singular"}) == "est"
    assert conjugate("fr", "avoir", {"person": "1", "number": "singular"}) == "ai"
    assert conjugate("fr", "faire", {"person": "3", "number": "plural"}) == "font"


def test_passe_compose():
    form = conjugate("fr", "faire", {"tense": "passe_compose", "person": "3", "number": "singular"})
    assert form == "a fait"
    form2 = conjugate("fr", "parler", {"tense": "passe_compose", "person": "1", "number": "singular"})
    assert form2 == "ai parlé"
