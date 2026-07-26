"""Shared conjugation / morph transform slot bag."""

from __future__ import annotations

from typing import Any


SLOT_KEYS = (
    "mood",
    "tense",
    "person",
    "number",
    "aspect",
    "politeness",
    "polarity",
    "voice",
    "formality",
    "honorific",
)

DEFAULTS = {
    "mood": "indicative",
    "tense": "present",
    "person": "3",
    "number": "singular",
    "aspect": "none",
    "politeness": "plain",
    "polarity": "affirmative",
    "voice": "active",
    "formality": "plain",
    "honorific": "0",
}


def normalize_slots(raw: dict[str, Any] | None = None, **kwargs: Any) -> dict[str, str]:
    """Merge kwargs + raw into a normalized string slot bag with defaults."""
    src: dict[str, Any] = {}
    if raw:
        src.update(raw)
    src.update({k: v for k, v in kwargs.items() if v is not None})
    out: dict[str, str] = {}
    for key in SLOT_KEYS:
        camel = "".join(w.capitalize() if i else w for i, w in enumerate(key.split("_")))
        val = src.get(key)
        if val is None:
            val = src.get(camel)
        if val is None:
            val = DEFAULTS[key]
        if isinstance(val, bool):
            val = "1" if val else "0"
        out[key] = str(val).strip() or DEFAULTS[key]
    return out


def slot_key_tuple(slots: dict[str, str]) -> tuple[str, ...]:
    return tuple(slots.get(k, DEFAULTS[k]) for k in SLOT_KEYS)
