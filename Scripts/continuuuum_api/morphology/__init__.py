"""Generative morphology for CoB conjugation (es/fr/ja/ko/zh)."""

from __future__ import annotations

from .registry import (
    DEFAULT_RULES_REF,
    conjugate,
    ensure_plugins_loaded,
    get_plugin,
    register_plugin,
)
from .slots import DEFAULTS, SLOT_KEYS, normalize_slots

__all__ = [
    "DEFAULT_RULES_REF",
    "DEFAULTS",
    "SLOT_KEYS",
    "conjugate",
    "ensure_plugins_loaded",
    "get_plugin",
    "normalize_slots",
    "register_plugin",
]

# Eager-load plugins on import
ensure_plugins_loaded()
