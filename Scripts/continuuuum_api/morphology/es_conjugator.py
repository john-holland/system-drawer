"""Spanish generative conjugator (es_v1)."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .registry import register_plugin

_DATA = Path(__file__).resolve().parent / "data" / "es_irregulars.json"
_IRREG: dict[str, Any] | None = None

_PN = ("1|singular", "2|singular", "3|singular", "1|plural", "2|plural", "3|plural")

_PRESENT = {
    "ar": ("o", "as", "a", "amos", "áis", "an"),
    "er": ("o", "es", "e", "emos", "éis", "en"),
    "ir": ("o", "es", "e", "imos", "ís", "en"),
}
_PRETERITE = {
    "ar": ("é", "aste", "ó", "amos", "asteis", "aron"),
    "er": ("í", "iste", "ió", "imos", "isteis", "ieron"),
    "ir": ("í", "iste", "ió", "imos", "isteis", "ieron"),
}
_IMPERFECT = {
    "ar": ("aba", "abas", "aba", "ábamos", "abais", "aban"),
    "er": ("ía", "ías", "ía", "íamos", "íais", "ían"),
    "ir": ("ía", "ías", "ía", "íamos", "íais", "ían"),
}
_FUTURE_END = ("é", "ás", "á", "emos", "éis", "án")
_COND_END = ("ía", "ías", "ía", "íamos", "íais", "ían")
_SUBJ_PRESENT = {
    "ar": ("e", "es", "e", "emos", "éis", "en"),
    "er": ("a", "as", "a", "amos", "áis", "an"),
    "ir": ("a", "as", "a", "amos", "áis", "an"),
}


def _load() -> dict[str, Any]:
    global _IRREG
    if _IRREG is None:
        _IRREG = json.loads(_DATA.read_text(encoding="utf-8")) if _DATA.is_file() else {}
    return _IRREG


def _cls(lemma: str) -> str | None:
    low = lemma.lower()
    if low.endswith("ar"):
        return "ar"
    if low.endswith("er"):
        return "er"
    if low.endswith("ir") or low.endswith("ír"):
        return "ir"
    return None


def _stem(lemma: str) -> str:
    low = lemma.lower()
    if low.endswith("ír"):
        return lemma[:-2]
    return lemma[:-2]


def _stem_change(stem: str, kind: str, person: str, number: str) -> str:
    """Apply stem change for non-nosotros/vosotros forms."""
    if number == "plural" and person in ("1", "2"):
        return stem
    if kind == "e>ie":
        # change last e in stem
        idx = stem.rfind("e")
        if idx >= 0:
            return stem[:idx] + "ie" + stem[idx + 1 :]
    if kind == "o>ue":
        idx = stem.rfind("o")
        if idx >= 0:
            return stem[:idx] + "ue" + stem[idx + 1 :]
    if kind == "e>i":
        idx = stem.rfind("e")
        if idx >= 0:
            return stem[:idx] + "i" + stem[idx + 1 :]
    return stem


def _pn_key(person: str, number: str) -> str:
    return f"{person}|{number}"


def _from_table(entry: dict[str, Any], mood: str, tense: str, person: str, number: str) -> str | None:
    if tense == "participle" or mood == "participle":
        return entry.get("participle")
    if tense == "gerund" or mood == "gerund":
        return entry.get("gerund")
    if mood == "imperative":
        block = entry.get("imperative|affirmative") or {}
        return block.get(_pn_key(person, number))
    block = entry.get(f"{mood}|{tense}")
    if isinstance(block, dict):
        return block.get(_pn_key(person, number))
    return None


def _regular(lemma: str, mood: str, tense: str, person: str, number: str, stem_change: str | None) -> str | None:
    c = _cls(lemma)
    if not c:
        return None
    stem = _stem(lemma)
    pn = _pn_key(person, number)
    idx = _PN.index(pn) if pn in _PN else 2

    if tense in ("participle",) or mood == "participle":
        return stem + ("ado" if c == "ar" else "ido")
    if tense in ("gerund",) or mood == "gerund":
        return stem + ("ando" if c == "ar" else "iendo")

    use_stem = stem
    if stem_change and mood == "indicative" and tense == "present":
        use_stem = _stem_change(stem, stem_change, person, number)
    if stem_change and mood == "subjunctive" and tense == "present":
        use_stem = _stem_change(stem, stem_change, person, number)

    if mood == "indicative" and tense == "present":
        return use_stem + _PRESENT[c][idx]
    if mood == "indicative" and tense in ("preterite", "past"):
        # pretérito uses unstressed stem (no stem-change for -ar/-er typically)
        return stem + _PRETERITE[c][idx]
    if mood == "indicative" and tense == "imperfect":
        return stem + _IMPERFECT[c][idx]
    if mood == "indicative" and tense == "future":
        return lemma.lower() + _FUTURE_END[idx]
    if mood == "indicative" and tense == "conditional":
        return lemma.lower() + _COND_END[idx]
    if mood == "subjunctive" and tense == "present":
        return use_stem + _SUBJ_PRESENT[c][idx]
    if mood == "imperative" and tense in ("present", "affirmative"):
        # tú: 3sg indicative present-like; usted: subjunctive 3sg
        if person == "2" and number == "singular":
            return use_stem + (_PRESENT[c][2] if c == "ar" else _PRESENT[c][2])
        if person == "3":
            return use_stem + _SUBJ_PRESENT[c][idx]
    return None


class EsConjugator:
    ref = "es_v1"
    language_codes = ("es",)

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None:
        lemma = lemma.strip()
        if not lemma:
            return None
        mood = slots.get("mood", "indicative")
        tense = slots.get("tense", "present")
        # aspect perfective → pretérito when tense still present
        if slots.get("aspect") == "perfective" and tense == "present" and mood == "indicative":
            tense = "preterite"
        person = slots.get("person", "3")
        number = slots.get("number", "singular")
        irreg = _load().get(lemma.lower(), {})
        hit = _from_table(irreg, mood, tense, person, number) if irreg else None
        if hit:
            return hit
        stem_change = irreg.get("stemChange") if irreg else None
        return _regular(lemma, mood, tense, person, number, stem_change)


register_plugin(EsConjugator())
