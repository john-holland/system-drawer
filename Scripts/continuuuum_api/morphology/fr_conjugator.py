"""French generative conjugator (fr_v1)."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .registry import register_plugin

_DATA = Path(__file__).resolve().parent / "data" / "fr_irregulars.json"
_IRREG: dict[str, Any] | None = None
_PN = ("1|singular", "2|singular", "3|singular", "1|plural", "2|plural", "3|plural")

_ER_PRESENT = ("e", "es", "e", "ons", "ez", "ent")
_IR_PRESENT = ("is", "is", "it", "issons", "issez", "issent")
_RE_PRESENT = ("s", "s", "", "ons", "ez", "ent")
_ER_IMP = ("ais", "ais", "ait", "ions", "iez", "aient")
_FUT = ("ai", "as", "a", "ons", "ez", "ont")
_COND = ("ais", "ais", "ait", "ions", "iez", "aient")
_SUBJ_ER = ("e", "es", "e", "ions", "iez", "ent")


def _load() -> dict[str, Any]:
    global _IRREG
    if _IRREG is None:
        _IRREG = json.loads(_DATA.read_text(encoding="utf-8")) if _DATA.is_file() else {}
    return _IRREG


def _pn_key(person: str, number: str) -> str:
    return f"{person}|{number}"


def _cls(lemma: str) -> str | None:
    low = lemma.lower()
    if low.endswith("er"):
        return "er"
    if low.endswith("ir"):
        return "ir"
    if low.endswith("re"):
        return "re"
    return None


def _stem(lemma: str, c: str) -> str:
    if c == "re":
        return lemma[:-2]
    return lemma[:-2]


def _from_table(entry: dict[str, Any], mood: str, tense: str, person: str, number: str) -> str | None:
    if tense in ("participle", "past_participle") or mood == "participle":
        return entry.get("participle")
    block = entry.get(f"{mood}|{tense}")
    if isinstance(block, dict):
        return block.get(_pn_key(person, number))
    return None


def _aux_form(aux_lemma: str, person: str, number: str) -> str:
    entry = _load().get(aux_lemma, {})
    block = entry.get("indicative|present") or {}
    return block.get(_pn_key(person, number)) or aux_lemma


def _regular(lemma: str, mood: str, tense: str, person: str, number: str) -> str | None:
    c = _cls(lemma)
    if not c:
        return None
    stem = _stem(lemma, c)
    idx = _PN.index(_pn_key(person, number)) if _pn_key(person, number) in _PN else 2

    if tense in ("participle", "past_participle") or mood == "participle":
        if c == "er":
            return stem + "é"
        if c == "ir":
            return stem + "i"
        return stem + "u"

    if mood == "indicative" and tense == "present":
        if c == "er":
            return stem + _ER_PRESENT[idx]
        if c == "ir":
            return stem + _IR_PRESENT[idx]
        # -re
        end = _RE_PRESENT[idx]
        return stem + end

    if mood == "indicative" and tense == "imperfect":
        if c == "er":
            return stem + _ER_IMP[idx]
        if c == "ir":
            return stem + "iss" + _ER_IMP[idx]
        return stem + _ER_IMP[idx]

    if mood == "indicative" and tense == "future":
        base = lemma if c != "re" else lemma[:-1]
        return base + _FUT[idx]

    if mood == "indicative" and tense == "conditional":
        base = lemma if c != "re" else lemma[:-1]
        return base + _COND[idx]

    if mood == "subjunctive" and tense == "present":
        if c == "er":
            return stem + _SUBJ_ER[idx]
        if c == "ir":
            return stem + "iss" + _SUBJ_ER[idx]
        return stem + _SUBJ_ER[idx]

    return None


class FrConjugator:
    ref = "fr_v1"
    language_codes = ("fr",)

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None:
        lemma = lemma.strip()
        if not lemma:
            return None
        mood = slots.get("mood", "indicative")
        tense = slots.get("tense", "present")
        person = slots.get("person", "3")
        number = slots.get("number", "singular")
        irreg = _load().get(lemma.lower(), {})

        if tense in ("passe_compose", "passé_composé", "perfect"):
            part = (irreg.get("participle") if irreg else None) or _regular(
                lemma, "participle", "participle", person, number
            )
            if not part:
                return None
            aux = (irreg.get("aux") if irreg else None) or "avoir"
            return f"{_aux_form(aux, person, number)} {part}"

        hit = _from_table(irreg, mood, tense, person, number) if irreg else None
        if hit:
            return hit
        return _regular(lemma, mood, tense, person, number)


register_plugin(FrConjugator())
