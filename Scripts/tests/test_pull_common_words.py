"""Unit tests for pull_common_words (mocks wordfreq)."""

from __future__ import annotations

import json
import sys
import types
from pathlib import Path

import pytest

SCRIPTS = Path(__file__).resolve().parents[1]
if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

import pull_common_words as pcw


def _stub_wordfreq(monkeypatch, words=None, zipf=None):
    words = words if words is not None else ["the", "to", "and"]
    zipf = zipf if zipf is not None else {"the": 7.73, "to": 7.43, "and": 7.41}
    mod = types.ModuleType("wordfreq")
    mod.top_n_list = lambda lang, n: words[:n]
    mod.zipf_frequency = lambda w, lang: zipf[w]
    monkeypatch.setitem(sys.modules, "wordfreq", mod)


def test_build_payload_shape():
    rows = [{"rank": 1, "word": "the", "zipf": 7.73}]
    payload = pcw.build_payload("en", 5000, rows)
    assert payload["source"] == "wordfreq"
    assert payload["language"] == "en"
    assert payload["n"] == 1
    assert payload["requested_n"] == 5000
    assert payload["words"] == rows


def test_pull_common_words_uses_wordfreq(monkeypatch):
    _stub_wordfreq(monkeypatch)
    rows = pcw.pull_common_words("en", 2)
    assert rows == [
        {"rank": 1, "word": "the", "zipf": 7.73},
        {"rank": 2, "word": "to", "zipf": 7.43},
    ]


def test_write_json_and_txt(tmp_path):
    rows = [
        {"rank": 1, "word": "the", "zipf": 7.73},
        {"rank": 2, "word": "to", "zipf": 7.43},
    ]
    payload = pcw.build_payload("en", 2, rows)

    json_path = tmp_path / "words.json"
    pcw.write_output(json_path, rows, payload, "json")
    loaded = json.loads(json_path.read_text(encoding="utf-8"))
    assert loaded["words"][0]["word"] == "the"

    txt_path = tmp_path / "words.txt"
    pcw.write_output(txt_path, rows, payload, "txt")
    assert txt_path.read_text(encoding="utf-8") == "the\nto\n"


def test_main_writes_output(tmp_path, monkeypatch):
    _stub_wordfreq(monkeypatch, words=["the"])
    out = tmp_path / "out.json"
    assert pcw.main(["--n", "1", "--output", str(out)]) == 0
    data = json.loads(out.read_text(encoding="utf-8"))
    assert data["n"] == 1
    assert data["words"][0]["word"] == "the"


def test_n_must_be_positive():
    with pytest.raises(ValueError):
        pcw.pull_common_words("en", 0)
