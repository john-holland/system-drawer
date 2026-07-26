"""Unit tests for define_common_words_nsm (mocked LM Studio)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

SCRIPTS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPTS))

import define_common_words_nsm as dcn  # noqa: E402


def test_allow_list_grows_with_rank():
    primes = [{"term": "I", "posTag": "pronoun"}, {"term": "do", "posTag": "verb"}]
    builtins = [{"term": "go", "id": "urn:x/go", "posTag": "verb"}]
    prior = [
        {"rank": 1, "word": "the"},
        {"rank": 2, "word": "to"},
        {"rank": 3, "word": "and"},
    ]
    a1 = dcn.allow_list_for_rank(1, primes=primes, builtins=builtins, prior_words=prior)
    a3 = dcn.allow_list_for_rank(3, primes=primes, builtins=builtins, prior_words=prior)
    terms1 = {x["term"] for x in a1}
    terms3 = {x["term"] for x in a3}
    assert "I" in terms1 and "go" in terms1
    assert "the" not in terms1  # rank 1 cannot use itself/priors
    assert "the" in terms3 and "to" in terms3
    assert "and" not in terms3
    assert len(a3) > len(a1)


def test_parse_nsm_response():
    text = """
Here you go:
```json lemma-mechanism-descriptor
{
  "lemma": "the",
  "posTag": "determiner",
  "mechanicalRole": "LiteralPrimitive",
  "outputTier": 0,
  "nsmDefinition": "someone can say this word about something",
  "compositionChildren": []
}
```
"""
    d = dcn.parse_nsm_response(text)
    assert d is not None
    assert d["lemma"] == "the"
    assert "someone" in d["nsmDefinition"]


def test_process_word_uses_chat(monkeypatch, tmp_path):
    def fake_chat(base_url, model, messages, temperature):
        assert messages[0]["role"] == "system"
        assert "the" in messages[1]["content"]
        return """```json lemma-mechanism-descriptor
{"lemma":"the","posTag":"determiner","mechanicalRole":"LiteralPrimitive","nsmDefinition":"this word","compositionChildren":[]}
```"""

    allow = dcn.allow_list_for_rank(
        1,
        primes=[{"term": "this", "posTag": "determiner"}],
        builtins=[],
        prior_words=[],
    )
    rec = dcn.process_word(
        "the",
        1,
        preface="sys",
        allow=allow,
        chat=fake_chat,
        base_url="http://localhost:1234/v1",
        model="x",
        temperature=0.2,
    )
    assert rec["word"] == "the"
    assert rec["allowListSize"] == len(allow)
    assert rec["descriptor"]["posTag"] == "determiner"


def test_main_dry_run(tmp_path):
    out = tmp_path / "out.jsonl"
    rc = dcn.main(
        [
            "--dry-run",
            "--limit",
            "3",
            "--start-rank",
            "1",
            "--end-rank",
            "10",
            "--output",
            str(out),
        ]
    )
    assert rc == 0
    lines = out.read_text(encoding="utf-8").strip().splitlines()
    assert len(lines) == 3
    row = json.loads(lines[0])
    assert row["dryRun"] is True
    assert "allowListSize" in row
