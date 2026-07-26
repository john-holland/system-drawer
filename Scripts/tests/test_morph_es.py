"""Spanish generative conjugator."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from morphology import conjugate  # noqa: E402


def test_regular_ar_er_ir_present():
    assert conjugate("es", "hablar", {"person": "1", "number": "singular"}) == "hablo"
    assert conjugate("es", "comer", {"person": "3", "number": "singular"}) == "come"
    assert conjugate("es", "vivir", {"person": "1", "number": "plural"}) == "vivimos"


def test_irregular_hacer_ser():
    assert conjugate("es", "hacer", {"person": "3", "number": "singular"}) == "hace"
    assert conjugate("es", "hacer", {"tense": "preterite", "person": "3", "number": "singular"}) == "hizo"
    assert conjugate("es", "ser", {"person": "1", "number": "singular"}) == "soy"
    assert conjugate("es", "ser", {"tense": "preterite", "person": "3", "number": "singular"}) == "fue"


def test_participle_gerund():
    assert conjugate("es", "hablar", {"tense": "participle"}) == "hablado"
    assert conjugate("es", "hacer", {"tense": "participle"}) == "hecho"
