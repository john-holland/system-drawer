"""Tests for double-day dream stack."""

import sqlite3

from continuuuum_api.dream_cycle import (
    complete_city_day,
    complete_double_day_stack,
    complete_good_day_horizon,
    complete_dream_day_layer,
    compute_day_collapse_seed,
)
from continuuuum_api.dream_cycle_db import ensure_dream_cycle_schema, save_day_session, load_day_session
from continuuuum_api.dream_day_horizon import GoodDayHorizonConfig


def test_good_day_horizon_respects_floor():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    config = GoodDayHorizonConfig(min_satisfied=0.75, max_satisfied=0.9)
    outer = complete_good_day_horizon(conn, "test-city", config=config)
    for state in outer["aspectStates"]:
        assert state["satisfied01"] >= 0.75
        assert state["satisfied01"] <= 0.9
    assert outer["lemmaHintsApplied"] is False


def test_dream_layer_cannot_undercut_outer_floor():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    outer = complete_good_day_horizon(conn, "test-city")
    floors = {s["aspectId"]: s["satisfied01"] for s in outer["aspectStates"]}
    inner = complete_dream_day_layer(
        outer,
        '{P:dream-day|aspect=need_belonging|spatial2d-slot=need_belonging|satisfied=0.1}',
    )
    for state in inner["aspectStates"]:
        aid = state["aspectId"]
        assert state["satisfied01"] >= floors[aid] - 0.001


def test_double_day_seed_chain_stable():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    s1 = complete_double_day_stack(conn, "city-a", "calm dream")
    s2 = complete_double_day_stack(conn, "city-a", "calm dream")
    assert s1["goodDayCollapseSeed"] == s2["goodDayCollapseSeed"]
    assert s1["dreamDayCollapseSeed"] == s2["dreamDayCollapseSeed"]


def test_double_day_persist_roundtrip():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    session = complete_city_day(conn, "test-city", double_day=True, dream_day_prompt="dream")
    assert session["doubleDay"] is True
    assert session.get("goodDayCollapseSeed")
    assert session.get("dreamDayCollapseSeed")
    save_day_session(conn, session)
    loaded = load_day_session(conn, session["sessionId"])
    assert loaded["doubleDay"] is True
    assert loaded["goodDayCollapseSeed"] == session["goodDayCollapseSeed"]


def test_single_day_backward_compat():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    session = complete_city_day(conn, "test-city", day_prompt="quiet day")
    assert session.get("doubleDay") is False
    assert len(session["aspectStates"]) == 5
    states = [{"aspectId": "a", "quadTreeDigest": "abc"}]
    assert compute_day_collapse_seed(states) == compute_day_collapse_seed(states)
