"""Table read block parser tests."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "continuuuum_api"))

from table_read_blocks import assign_round_robin, parse_reading_blocks


def test_parse_dialogue_blocks():
    script = """INT. KITCHEN - DAY

ALICE
Hello there.

BOB
Hi Alice.

ALICE
How are you?"""
    blocks = parse_reading_blocks(script)
    assert len(blocks) >= 3
    assert any("ALICE" in b["text"] for b in blocks)
    assert blocks[0]["kind"] in ("scene", "action", "dialogue")


def test_assign_round_robin():
    blocks = [{"index": 0, "text": "a"}, {"index": 1, "text": "b"}, {"index": 2, "text": "c"}]
    out = assign_round_robin(blocks, ["u1", "u2"])
    assert out[0]["assignedUserId"] == "u1"
    assert out[1]["assignedUserId"] == "u2"
    assert out[2]["assignedUserId"] == "u1"


def test_empty_script():
    assert parse_reading_blocks("") == []
    assert parse_reading_blocks("   ") == []
