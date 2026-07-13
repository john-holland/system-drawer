"""Tests for play-within-dream improbability and script reproduction coeff bake."""

from __future__ import annotations

import sqlite3

from continuuuum_api.dream_improbability import (
    UNWRAP_ESCAPISM_PREVIEW,
    UNWRAP_PLAY_THOUGHT,
    bake_dream_reproduction_coeff,
    ensure_script_reproduction_columns,
    mean_logistic_coeffs,
    score_play_improbability,
)


def _conn() -> sqlite3.Connection:
    conn = sqlite3.connect(":memory:")
    conn.row_factory = sqlite3.Row
    return conn


def _install_script_tables(conn: sqlite3.Connection) -> None:
    conn.executescript(
        """
        CREATE TABLE draft_episodes (
            id TEXT PRIMARY KEY,
            title TEXT,
            created_at TEXT,
            updated_at TEXT
        );
        CREATE TABLE draft_episode_script (
            id TEXT PRIMARY KEY,
            draft_episode_id TEXT NOT NULL,
            episode_script_id TEXT,
            script_text TEXT,
            language TEXT DEFAULT 'en',
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );
        CREATE TABLE episode_script (
            id TEXT PRIMARY KEY,
            episode_id TEXT,
            script_text TEXT,
            language TEXT,
            created_at TEXT NOT NULL
        );
        CREATE TABLE localization_clause_bindings (
            id TEXT PRIMARY KEY,
            draft_script_id TEXT,
            entry_id TEXT,
            binding_kind TEXT,
            property_key TEXT,
            property_value TEXT,
            prompt_placeholder_name TEXT
        );
        """
    )
    ensure_script_reproduction_columns(conn)


def test_mean_logistic_empty_defaults():
    assert mean_logistic_coeffs([]) == 0.5


def test_bake_writes_float_to_script_row():
    conn = _conn()
    _install_script_tables(conn)
    conn.execute(
        "INSERT INTO draft_episodes (id, title, created_at, updated_at) VALUES ('d1', 'Play', 't', 't')"
    )
    conn.execute(
        """INSERT INTO draft_episode_script
           (id, draft_episode_id, script_text, created_at, updated_at)
           VALUES ('s1', 'd1', 'curtain rises', 't', 't')"""
    )
    conn.execute(
        """INSERT INTO localization_clause_bindings
           (id, draft_script_id, entry_id, binding_kind, property_key, property_value)
           VALUES ('b1', 's1', 'e1', 'lemma', 'logistic-coeff', '0.2'),
                  ('b2', 's1', 'e2', 'prompt_placeholder', 'logistic-coeff', '0.4')"""
    )
    conn.commit()
    value = bake_dream_reproduction_coeff(conn, "d1")
    assert abs(value - 0.3) < 1e-6
    row = conn.execute(
        "SELECT dream_reproduction_coeff FROM draft_episode_script WHERE id = 's1'"
    ).fetchone()
    assert abs(float(row["dream_reproduction_coeff"]) - 0.3) < 1e-6


def test_escapism_foundation_clamps_improbability():
    score = score_play_improbability(
        layer_stack=["good_day_horizon", "developer_dream"],
        inducing_play={"inductionPrior": 0.1, "draftEpisodeId": "d1", "consumedPhrases": ["0:a"]},
        nested_play_event={"draftEpisodeId": "d1", "phrase": "a"},
        reproduction_coeff=0.2,
    )
    assert score["unwrapMode"] == UNWRAP_ESCAPISM_PREVIEW
    assert score["improbability01"] == 1.0
    assert score["audit"]["foundationClamped"] is True
    assert "escapism" in score["refrainLabel"]
    assert isinstance(score["inductionPrior"], float)
    assert isinstance(score["success01"], float)


def test_play_thought_unpack_when_foundation_strong():
    score = score_play_improbability(
        layer_stack=["good_day_horizon", "developer_dream", "play_echo"],
        inducing_play={
            "inductionPrior": 0.9,
            "draftEpisodeId": "d1",
            "consumedPhrases": ["0:curtain", "1:act1", "2:act2"],
        },
        nested_play_event={"draftEpisodeId": "d1", "phrase": "curtain"},
        reproduction_coeff=0.95,
        rem_entropy_norm=1.0,
    )
    assert score["unwrapMode"] == UNWRAP_PLAY_THOUGHT
    assert score["improbability01"] < 1.0 or score["audit"]["foundationClamped"] is False
    assert score["audit"]["foundationClamped"] is False
    assert "play thought" in score["refrainLabel"]


def test_always_scores_near_zero_success():
    score = score_play_improbability(
        layer_stack=["good_day_horizon", "developer_dream"],
        inducing_play={"inductionPrior": 0.0},
        nested_play_event={},
        reproduction_coeff=1.0,
    )
    assert "success01" in score
    assert score["success01"] >= 0.0
    assert score["unwrapMode"] == UNWRAP_ESCAPISM_PREVIEW
    assert score["improbability01"] == 1.0


def test_intense_staged_acts_still_score():
    """Narrative is never gated — staged intensity does not refuse a score."""
    score = score_play_improbability(
        layer_stack=["good_day_horizon", "developer_dream"],
        inducing_play={
            "inductionPrior": 0.8,
            "draftEpisodeId": "horror-draft",
            "consumedPhrases": ["0:preview", "1:jump-scare-stage-direction"],
        },
        nested_play_event={"draftEpisodeId": "horror-draft", "phrase": "preview"},
        reproduction_coeff=0.99,
    )
    assert score["success01"] is not None
    assert score["refrainLabel"]
    assert score["memoryMode"] in ("preview_only", "partial", "full_reproduction")


def test_coeff_poles_memory_mode():
    low = score_play_improbability(
        inducing_play={"inductionPrior": 0.9},
        reproduction_coeff=0.0,
        layer_stack=["good_day_horizon", "developer_dream", "play_echo"],
        nested_play_event={"draftEpisodeId": "x"},
    )
    high = score_play_improbability(
        inducing_play={"inductionPrior": 0.9},
        reproduction_coeff=1.0,
        layer_stack=["good_day_horizon", "developer_dream", "play_echo"],
        nested_play_event={"draftEpisodeId": "x"},
    )
    assert low["memoryMode"] == "full_reproduction"
    assert high["memoryMode"] == "preview_only"
    assert low["fidelity01"] == 1.0
    assert high["fidelity01"] == 0.0
