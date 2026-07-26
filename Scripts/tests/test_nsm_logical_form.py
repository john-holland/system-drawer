"""Tests for NSM logical form bool + fuzzy evaluation."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from nsm_logical_form import (  # noqa: E402
    apply_curve,
    evaluate_bool,
    evaluate_fuzzy,
    normalize,
    pretty_print,
)


def test_normalize_and_pretty():
    f = normalize({"op": "after", "args": [{"op": "prime", "term": "now"}]})
    assert f["op"] == "after"
    assert "after" in pretty_print(f)


def test_bool_if_not_and():
    env = {"P": True, "Q": False}
    assert evaluate_bool({"op": "not", "args": [{"op": "var", "name": "Q"}]}, env) is True
    assert evaluate_bool(
        {"op": "and", "args": [{"op": "var", "name": "P"}, {"op": "var", "name": "Q"}]}, env
    ) is False
    assert evaluate_bool(
        {"op": "if", "args": [{"op": "var", "name": "P"}, {"op": "var", "name": "Q"}]}, env
    ) is False
    assert evaluate_bool(
        {"op": "if", "args": [{"op": "var", "name": "Q"}, {"op": "var", "name": "P"}]}, env
    ) is True


def test_later_fixture_after_now():
    form = {"op": "after", "args": [{"op": "prime", "term": "now"}]}
    assert evaluate_bool(form, {"now": True}) is True


def test_fuzzy_somewhat_less_than_mostly():
    hedges = {
        "somewhat": {
            "curve": {"kind": "logistic", "k": 8.0, "x0": 0.45, "yMin": 0.15, "yMax": 0.7, "clamp": True}
        },
        "mostly": {
            "curve": {"kind": "logistic", "k": 10.0, "x0": 0.6, "yMin": 0.55, "yMax": 0.95, "clamp": True}
        },
    }
    a = evaluate_fuzzy(
        {"op": "hedge", "hedgeId": "somewhat", "args": [{"op": "grade", "value": 0.6}]},
        {},
        hedges,
    )
    b = evaluate_fuzzy(
        {"op": "hedge", "hedgeId": "mostly", "args": [{"op": "grade", "value": 0.6}]},
        {},
        hedges,
    )
    assert a < b


def test_power_curve_concentration():
    y = apply_curve({"kind": "power", "p": 2.0, "clamp": True}, 0.5)
    assert abs(y - 0.25) < 1e-6
