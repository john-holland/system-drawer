"""Tests for quest parser and routes."""

from __future__ import annotations

import os
import sqlite3
import sys
import tempfile
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))

from quest_parser import LITTLE_PRINCE_FIXTURE, compile_quest, compile_quest_to_json
from cave.manifest_loader import load_cave_manifest, message_to_structural


def test_compile_little_prince_fixture():
    result = compile_quest(LITTLE_PRINCE_FIXTURE, "little-prince-tour")
    assert result.set_id == "little-prince-tour"
    assert result.title == "Explore the asteroid belt"
    errors = [i for i in result.issues if i.level == "error"]
    assert not errors
    assert len(result.nodes) == 1
    obj = result.nodes[0].get("children") or [result.nodes[0]]
    meet_fox = obj[0]
    assert meet_fox["objectiveId"] == "meet-fox"
    assert meet_fox["spatial4dId"] == "s4d-fox-vol"
    travel_nodes = [c for c in meet_fox.get("children", []) if c.get("travelBinding")]
    assert travel_nodes and travel_nodes[0]["travelBinding"] == "fox-approach"


def test_manifest_quest_messages():
    manifest = load_cave_manifest()
    assert message_to_structural(manifest, "quest_compile") == "quest/compile"
    assert message_to_structural(manifest, "quest_session_open") == "quest/session/open"
    handlers = manifest.get("handlers") or {}
    assert "quest/spatial-nodes" in handlers


def test_quest_session_flow():
    from quest_db import ensure_quest_schema, save_compiled_set, create_session

    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    conn = sqlite3.connect(path)
    conn.row_factory = sqlite3.Row
    ensure_quest_schema(conn)
    compiled = compile_quest_to_json(LITTLE_PRINCE_FIXTURE, "little-prince-tour")
    save_compiled_set(
        conn,
        set_id="little-prince-tour",
        lemma_entry_id=None,
        title="Little Prince",
        compiled=compiled,
    )
    conn.commit()
    view = create_session(
        conn,
        set_id="little-prince-tour",
        tenant="default",
        user_id="test",
        trace_id="trace-1",
    )
    conn.commit()
    conn.close()
    assert view["ok"] is True
    assert view["activeObjective"]["objectiveId"] == "meet-fox"

    try:
        os.unlink(path)
    except OSError:
        pass
