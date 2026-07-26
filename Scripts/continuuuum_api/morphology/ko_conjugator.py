"""Korean conjugator (ko_v1): stem + speech-level endings."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .registry import register_plugin

_DATA = Path(__file__).resolve().parent / "data" / "ko_irregulars.json"
_IRREG: dict[str, Any] | None = None


def _load() -> dict[str, Any]:
    global _IRREG
    if _IRREG is None:
        _IRREG = json.loads(_DATA.read_text(encoding="utf-8")) if _DATA.is_file() else {}
    return _IRREG


def _stem(lemma: str) -> str:
    if lemma.endswith("다"):
        return lemma[:-1]
    return lemma


def _verb_type(lemma: str) -> str:
    entry = _load().get(lemma, {})
    return entry.get("type") or "regular"


def _apply_stem_irregular(stem: str, vtype: str) -> str:
    if vtype == "d_irregular" and stem.endswith("듣"):
        # 듣다 → 들-
        return stem[:-1] + "들"
    if vtype == "b_irregular" and stem.endswith("ㅂ"):
        return stem[:-1] + "우"
    return stem


def _level(slots: dict[str, str]) -> str:
    formality = slots.get("formality", "plain")
    politeness = slots.get("politeness", "plain")
    if formality in ("hae", "haeyo", "hapnida"):
        return formality
    if politeness == "polite" or formality == "polite":
        return "haeyo"
    if politeness in ("formal", "hapnida") or formality == "formal":
        return "hapnida"
    if politeness in ("hae", "casual"):
        return "hae"
    # default haeyo for NSM CoB
    if politeness == "plain" and formality == "plain":
        return "haeyo"
    return "haeyo"


class KoConjugator:
    ref = "ko_v1"
    language_codes = ("ko",)

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None:
        lemma = lemma.strip()
        if not lemma:
            return None
        tense = slots.get("tense", "present")
        polarity = slots.get("polarity", "affirmative")
        honorific = slots.get("honorific", "0") in ("1", "true", "yes")
        level = _level(slots)
        vtype = _verb_type(lemma)
        stem = _apply_stem_irregular(_stem(lemma), vtype)

        if honorific and not stem.endswith("시"):
            # insert 시 before ending
            if vtype == "hada" and stem.endswith("하"):
                stem = stem[:-1] + "하시"
            else:
                stem = stem + "시"

        # 하다 family
        if vtype == "hada" or lemma.endswith("하다"):
            base = stem
            if polarity != "affirmative":
                if tense == "past":
                    forms = {"hae": "안 했어", "haeyo": "안 했어요", "hapnida": "하지 않았습니다"}
                elif tense == "future":
                    forms = {"hae": "안 할 거야", "haeyo": "안 할 거예요", "hapnida": "하지 않을 것입니다"}
                else:
                    forms = {"hae": "안 해", "haeyo": "안 해요", "hapnida": "하지 않습니다"}
                return forms.get(level)
            if tense == "past":
                forms = {"hae": "했어", "haeyo": "했어요", "hapnida": "했습니다"}
            elif tense == "future":
                forms = {"hae": "할 거야", "haeyo": "할 거예요", "hapnida": "할 것입니다"}
            else:
                forms = {"hae": "해", "haeyo": "해요", "hapnida": "합니다"}
            # map 하다 → 해요 from stem
            if lemma == "하다" or stem in ("하", "하시"):
                return forms.get(level)
            # X하다 → X해요
            prefix = stem[:-1] if stem.endswith("하") else (stem[:-2] if stem.endswith("하시") else stem)
            mapped = forms.get(level, "")
            if stem.endswith("하시"):
                # honorific already in stem
                if tense == "past":
                    return prefix + ("하셨어요" if level == "haeyo" else "하셨어" if level == "hae" else "하셨습니다")
                if tense == "future":
                    return prefix + ("하실 거예요" if level == "haeyo" else "하실 거야" if level == "hae" else "하실 것입니다")
                return prefix + ("하세요" if level == "haeyo" else "하셔" if level == "hae" else "하십니다")
            return prefix + mapped

        def _hae_vowel() -> str:
            # simplified: use 아 after ㅏ/ㅗ stems else 어
            if not stem:
                return "어"
            last = stem[-1]
            if last in "ㅏㅗㅑㅛ" or stem.endswith(("아", "오", "알")):
                return "아"
            return "어"

        v = _hae_vowel()
        if polarity != "affirmative":
            if tense == "past":
                forms = {"hae": f"안 {stem}{v}ㅆ어", "haeyo": f"안 {stem}{v}ㅆ어요", "hapnida": f"{stem}지 않았습니다"}
            elif tense == "future":
                forms = {"hae": f"안 {stem}을 거야", "haeyo": f"안 {stem}을 거예요", "hapnida": f"{stem}지 않을 것입니다"}
            else:
                forms = {"hae": f"안 {stem}{v}", "haeyo": f"안 {stem}{v}요", "hapnida": f"{stem}지 않습니다"}
            return forms.get(level)

        if tense == "past":
            # 알다 → 알았어요
            if lemma == "알다" or stem == "알":
                forms = {"hae": "알았어", "haeyo": "알았어요", "hapnida": "알았습니다"}
            else:
                forms = {"hae": f"{stem}{v}ㅆ어", "haeyo": f"{stem}{v}ㅆ어요", "hapnida": f"{stem}었습니다"}
        elif tense == "future":
            forms = {"hae": f"{stem}을 거야", "haeyo": f"{stem}을 거예요", "hapnida": f"{stem}을 것입니다"}
        else:
            if lemma == "알다" or stem == "알":
                forms = {"hae": "알아", "haeyo": "알아요", "hapnida": "압니다"}
            else:
                forms = {"hae": f"{stem}{v}", "haeyo": f"{stem}{v}요", "hapnida": f"{stem}습니다"}
        return forms.get(level)


register_plugin(KoConjugator())
