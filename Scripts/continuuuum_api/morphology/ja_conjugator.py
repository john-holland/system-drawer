"""Japanese conjugator (ja_v1): godan / ichidan / する・来る."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .registry import register_plugin

_DATA = Path(__file__).resolve().parent / "data" / "ja_verb_classes.json"
_CLASSES: dict[str, str] | None = None

# godan row: u, a, i, e, o stems by ending kana
_GODAN = {
    "う": ("う", "わ", "い", "え", "お"),
    "く": ("く", "か", "き", "け", "こ"),
    "ぐ": ("ぐ", "が", "ぎ", "げ", "ご"),
    "す": ("す", "さ", "し", "せ", "そ"),
    "つ": ("つ", "た", "ち", "て", "と"),
    "ぬ": ("ぬ", "な", "に", "ね", "の"),
    "ぶ": ("ぶ", "ば", "び", "べ", "ぼ"),
    "む": ("む", "ま", "み", "め", "も"),
    "る": ("る", "ら", "り", "れ", "ろ"),
}

_TE_MAP = {
    "う": "って",
    "つ": "って",
    "る": "って",
    "む": "んで",
    "ぶ": "んで",
    "ぬ": "んで",
    "く": "いて",
    "ぐ": "いで",
    "す": "して",
}


def _load() -> dict[str, str]:
    global _CLASSES
    if _CLASSES is None:
        _CLASSES = json.loads(_DATA.read_text(encoding="utf-8")) if _DATA.is_file() else {}
    return _CLASSES


def _classify(lemma: str) -> str:
    known = _load().get(lemma)
    if known:
        return known
    if lemma in ("する",):
        return "suru"
    if lemma in ("来る", "くる"):
        return "kuru"
    if lemma.endswith(("いる", "える", "きる", "げる", "ける", "せる", "てる", "でる", "ねる", "べる", "める", "れる")):
        # heuristic ichidan; common exceptions handled via JSON
        if lemma in ("帰る", "切る", "知る", "走る", "入る", "要る", "減る"):
            return "godan"
        return "ichidan"
    if lemma.endswith(tuple(_GODAN.keys())):
        return "godan"
    return "unknown"


def _godan_stems(lemma: str) -> dict[str, str] | None:
    if not lemma:
        return None
    end = lemma[-1]
    row = _GODAN.get(end)
    if not row:
        return None
    base = lemma[:-1]
    return {"u": base + row[0], "a": base + row[1], "i": base + row[2], "e": base + row[3], "o": base + row[4], "end": end}


def _form_family(slots: dict[str, str]) -> str:
    """Map slots → form: dictionary/masu/te/ta/nai/potential/passive/causative/volitional/conditional."""
    mood = slots.get("mood", "indicative")
    tense = slots.get("tense", "present")
    politeness = slots.get("politeness", "plain")
    polarity = slots.get("polarity", "affirmative")
    # tense field may carry form family for ja
    if tense in (
        "dictionary",
        "masu",
        "te",
        "ta",
        "nai",
        "potential",
        "passive",
        "causative",
        "volitional",
        "conditional",
        "nonpast",
        "past",
    ):
        family = tense
    elif mood in ("potential", "passive", "causative", "volitional", "conditional"):
        family = mood
    else:
        family = "nonpast"

    if family == "present":
        family = "nonpast"
    if family == "dictionary":
        return "dictionary"
    if family == "te":
        return "te"
    if family in ("ta", "past"):
        return "ta" if polarity == "affirmative" else "nakatta"
    if family == "nai" or (family == "nonpast" and polarity != "affirmative" and politeness == "plain"):
        return "nai"
    if family == "potential":
        return "potential"
    if family == "passive":
        return "passive"
    if family == "causative":
        return "causative"
    if family == "volitional":
        return "volitional"
    if family == "conditional":
        return "conditional"
    if family == "masu" or (family == "nonpast" and politeness == "polite"):
        if polarity != "affirmative":
            return "masen"
        return "masu"
    if family == "nonpast" and politeness == "polite" and polarity != "affirmative":
        return "masen"
    # polite past
    if family == "ta" and politeness == "polite":
        return "mashita" if polarity == "affirmative" else "masen_deshita"
    if politeness == "polite" and family in ("past",):
        return "mashita"
    return "dictionary" if family == "nonpast" and politeness == "plain" else family


def _conjugate_classified(lemma: str, kind: str, family: str) -> str | None:
    if kind == "suru":
        table = {
            "dictionary": "する",
            "masu": "します",
            "masen": "しません",
            "te": "して",
            "ta": "した",
            "nai": "しない",
            "nakatta": "しなかった",
            "mashita": "しました",
            "potential": "できる",
            "passive": "される",
            "causative": "させる",
            "volitional": "しよう",
            "conditional": "すれば",
        }
        return table.get(family)

    if kind == "kuru":
        table = {
            "dictionary": "来る",
            "masu": "来ます",
            "masen": "来ません",
            "te": "来て",
            "ta": "来た",
            "nai": "来ない",
            "nakatta": "来なかった",
            "mashita": "来ました",
            "potential": "来られる",
            "passive": "来られる",
            "causative": "来させる",
            "volitional": "来よう",
            "conditional": "来れば",
        }
        return table.get(family)

    if kind == "ichidan":
        stem = lemma[:-1] if lemma.endswith("る") else lemma
        table = {
            "dictionary": lemma,
            "masu": stem + "ます",
            "masen": stem + "ません",
            "te": stem + "て",
            "ta": stem + "た",
            "nai": stem + "ない",
            "nakatta": stem + "なかった",
            "mashita": stem + "ました",
            "potential": stem + "られる",
            "passive": stem + "られる",
            "causative": stem + "させる",
            "volitional": stem + "よう",
            "conditional": stem + "れば",
        }
        return table.get(family)

    if kind in ("godan", "godan_iku"):
        stems = _godan_stems(lemma)
        if not stems:
            return None
        end = stems["end"]
        if kind == "godan_iku" and family in ("te", "ta"):
            te = "行って" if family == "te" else None
            ta = "行った"
            if family == "te":
                return te
            if family == "ta":
                return ta
        te = lemma[:-1] + _TE_MAP.get(end, "って")
        ta = te.replace("て", "た").replace("で", "だ")
        table = {
            "dictionary": lemma,
            "masu": stems["i"] + "ます",
            "masen": stems["i"] + "ません",
            "te": te,
            "ta": ta,
            "nai": stems["a"] + "ない",
            "nakatta": stems["a"] + "なかった",
            "mashita": stems["i"] + "ました",
            "potential": stems["e"] + "る",
            "passive": stems["a"] + "れる",
            "causative": stems["a"] + "せる",
            "volitional": stems["o"] + "う",
            "conditional": stems["e"] + "ば",
        }
        return table.get(family)

    return None


class JaConjugator:
    ref = "ja_v1"
    language_codes = ("ja",)

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None:
        lemma = lemma.strip()
        if not lemma:
            return None
        kind = _classify(lemma)
        if kind == "unknown":
            return None
        family = _form_family(slots)
        # polite past from past+polite
        if slots.get("tense") in ("past", "ta") and slots.get("politeness") == "polite":
            if slots.get("polarity", "affirmative") != "affirmative":
                family = "masen"  # simplify: ませんでした via mashita negation not full
                # produce ませんでした
                base = _conjugate_classified(lemma, kind, "masen")
                return (base + "でした") if base else None
            family = "mashita"
        return _conjugate_classified(lemma, kind, family)


register_plugin(JaConjugator())
