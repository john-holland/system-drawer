"""Mandarin aspect / polarity / voice particle transforms (zh_v1)."""

from __future__ import annotations

from .registry import register_plugin


class ZhTransforms:
    ref = "zh_v1"
    language_codes = ("zh", "zh-hans", "zh-hant", "zh-cn", "zh-tw")

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None:
        lemma = lemma.strip()
        if not lemma:
            return None
        aspect = slots.get("aspect", "none")
        polarity = slots.get("polarity", "affirmative")
        voice = slots.get("voice", "active")

        # polarity particles
        if polarity in ("bu", "不", "negative_bu"):
            core = f"不 {lemma}"
        elif polarity in ("mei", "没", "negative_mei", "negative"):
            # 没 preferred with perfective
            core = f"没 {lemma}"
        else:
            core = lemma

        # aspect — skip 了 after 没
        if polarity in ("mei", "没", "negative_mei", "negative") and aspect in ("le", "perfective"):
            aspect = "none"

        if aspect in ("le", "perfective"):
            core = f"{core} 了"
        elif aspect in ("guo", "experiential"):
            core = f"{core} 过"
        elif aspect in ("zhe", "durative"):
            core = f"{core} 着"

        if voice in ("ba", "把"):
            # 把 + object placeholder omitted; mark construction
            core = f"把 … {core}"
        elif voice in ("bei", "被"):
            core = f"被 … {core}"

        return core


register_plugin(ZhTransforms())
