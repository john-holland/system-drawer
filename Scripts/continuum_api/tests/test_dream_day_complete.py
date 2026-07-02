"""Tests for dream day completion."""

import sqlite3

from continuum_api.dream_cycle import complete_city_day, compute_day_collapse_seed
from continuum_api.dream_cycle_db import ensure_dream_cycle_schema, save_day_session, load_day_session
from continuum_api.dream_day_parser import compile_dream_day_hints


def test_lemma_compile():
    text = '{P:dream-day|aspect=need_belonging|spatial2d-slot=need_belonging}'
    compiled = compile_dream_day_hints(text)
    assert "need_belonging" in compiled["byAspect"]


def test_collapse_seed_stable():
    states = [{"aspectId": "a", "quadTreeDigest": "abc"}, {"aspectId": "b", "quadTreeDigest": "def"}]
    s1 = compute_day_collapse_seed(states)
    s2 = compute_day_collapse_seed(states)
    assert s1 == s2


def test_complete_city_day_in_memory_db():
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    ensure_dream_cycle_schema(conn)
    session = complete_city_day(conn, "test-city", day_prompt="quiet day")
    assert session["sessionId"]
    assert len(session["aspectStates"]) == 5
    save_day_session(conn, session)
    loaded = load_day_session(conn, session["sessionId"])
    assert loaded["cityId"] == "test-city"
