"""Canonical part-of-speech tags aligned with Unity built-in lemma path segments."""

from __future__ import annotations

from typing import Any

# posTag values stored in thesaurus_entries.pos_tag; segment matches URN path (en/{segment}/{term}).
POS_TAG_CATALOG: list[dict[str, Any]] = [
    {"posTag": "noun", "segment": "noun", "label": "Noun", "category": "Subject"},
    {"posTag": "verb", "segment": "verb", "label": "Verb", "category": "Action"},
    {"posTag": "determiner", "segment": "det", "label": "Determiner", "category": "Article"},
    {"posTag": "preposition", "segment": "prep", "label": "Preposition", "category": "Preposition"},
    {"posTag": "conjunction", "segment": "conj", "label": "Conjunction", "category": "DiscourseCausality"},
    {"posTag": "adverb", "segment": "adv", "label": "Adverb", "category": "DiscourseCausality"},
    {"posTag": "type_name", "segment": "literal", "label": "Literal type", "category": "LiteralType"},
    {"posTag": "adjective", "segment": "adj", "label": "Adjective", "category": None},
    {"posTag": "pronoun", "segment": "pron", "label": "Pronoun", "category": None},
    {"posTag": "interjection", "segment": "intj", "label": "Interjection", "category": None},
    {"posTag": "unknown", "segment": "unknown", "label": "Unknown", "category": None},
]

_KNOWN = {row["posTag"]: row for row in POS_TAG_CATALOG}


def normalize_pos_tag(raw: str | None) -> str:
    return (raw or "unknown").strip().lower() or "unknown"


def pos_segment(pos_tag: str | None) -> str:
    tag = normalize_pos_tag(pos_tag)
    row = _KNOWN.get(tag)
    if row:
        return str(row["segment"])
    return tag


def list_pos_tags() -> list[dict[str, Any]]:
    return [dict(row) for row in POS_TAG_CATALOG]
