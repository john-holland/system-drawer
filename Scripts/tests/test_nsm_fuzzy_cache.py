"""Fuzzy variable cache phrase fixtures."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuuuum_api"))
sys.path.insert(0, str(ROOT))

from nsm_fuzzy_cache import (  # noqa: E402
    adjust_grade,
    find_prior_similar,
    get_var,
    remember_event,
    set_var,
)
from nsm_wiring_db import ensure_nsm_schema, seed_nsm_fuzzy_hedges  # noqa: E402


def _conn(tmp_path):
    db = tmp_path / "f.db"
    c = sqlite3.connect(db)
    c.row_factory = sqlite3.Row
    ensure_nsm_schema(c)
    seed_nsm_fuzzy_hedges(c)
    return c


def test_less_skittish_lowers_grade(tmp_path):
    conn = _conn(tmp_path)
    set_var(conn, "s1", "pred:skittish", "predicate", 0.8)
    less_curve = {
        "kind": "power",
        "p": 1.5,
        "yScale": 0.7,
        "clamp": True,
    }
    row = adjust_grade(conn, "s1", "pred:skittish", hedge_id="less", curve=less_curve)
    assert row["grade"] < 0.8


def test_like_before_prior_brush(tmp_path):
    conn = _conn(tmp_path)
    set_var(conn, "s1", "referent:cat", "referent", 1.0)
    remember_event(conn, "s1", "brush_nose", {"actor": "cat"}, 0.7)
    remember_event(conn, "s1", "brush_nose", {"actor": "cat"}, 1.0)
    prior = find_prior_similar(conn, "s1", "brush_nose")
    assert prior is not None
    assert prior["grade"] == 0.7


def test_press_button_just_like_before(tmp_path):
    conn = _conn(tmp_path)
    set_var(conn, "s2", "referent:he", "referent", 1.0)
    remember_event(conn, "s2", "press_button", {"actor": "he"}, 0.85)
    remember_event(conn, "s2", "press_button", {"actor": "he"}, 1.0)
    prior = find_prior_similar(conn, "s2", "press_button")
    assert prior is not None
    assert float(prior["grade"]) >= 0.8
    cur = get_var(conn, "s2", "event:press_button")
    assert cur is not None
    assert float(cur["grade"]) == 1.0
