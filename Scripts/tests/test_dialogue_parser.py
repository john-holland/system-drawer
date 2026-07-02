"""Tests for dialogue_parser.py — book-concert fixture, nesting, validation."""

from __future__ import annotations

import sys
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
_api = _scripts / "continuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))

from dialogue_parser import BOOK_CONCERT_FIXTURE, compile_dialogue, compile_dialogue_to_json


def test_book_concert_compiles():
    result = compile_dialogue(BOOK_CONCERT_FIXTURE, "book-concert")
    assert result.set_id == "book-concert"
    errors = [i for i in result.issues if i.level == "error"]
    assert not errors, errors
    assert len(result.nodes) >= 1
    root = result.nodes[0]
    assert "books" in root.get("text", "").lower()
    assert len(root.get("children") or []) >= 2


def test_nested_answers_have_speaker():
    compiled = compile_dialogue_to_json(BOOK_CONCERT_FIXTURE, "book-concert")
    flat = _flatten(compiled["nodes"])

    prince = next((n for n in flat if n.get("answerId") == "handcuff-python"), None)
    assert prince is not None
    assert prince.get("speakerKey") == "prince"


def test_generate_span_captured():
    compiled = compile_dialogue_to_json(BOOK_CONCERT_FIXTURE, "book-concert")
    spans = compiled.get("generateSpans") or []
    assert len(spans) == 1
    assert spans[0].get("startNode") == "4d-node-id"
    assert spans[0].get("endNode") == "4d-node-id"


def test_unclosed_set_reports_error():
    text = '{P:dialogue|dialogue-set=orphan}"Hello"\n'
    result = compile_dialogue(text)
    errors = [i for i in result.issues if i.level == "error"]
    assert any("Unclosed" in i.message for i in errors)


def test_options_parsed():
    compiled = compile_dialogue_to_json(BOOK_CONCERT_FIXTURE, "book-concert")
    flat = _flatten(compiled["nodes"])
    node = next((n for n in flat if n.get("continueWithDialogue")), None)
    assert node is not None
    assert node.get("options") == ["long-mover"]


def _flatten(nodes):
    out = []

    def walk(items):
        for n in items:
            out.append(n)
            walk(n.get("children") or [])

    walk(nodes)
    return out
