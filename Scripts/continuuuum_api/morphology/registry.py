"""Map language codes / morphology_rules_ref → conjugator plugins."""

from __future__ import annotations

from typing import Any, Callable, Protocol

from .slots import normalize_slots

ConjugateFn = Callable[[str, dict[str, str]], str | None]


class MorphPlugin(Protocol):
    ref: str
    language_codes: tuple[str, ...]

    def conjugate(self, lemma: str, slots: dict[str, str]) -> str | None: ...


_PLUGINS_BY_REF: dict[str, MorphPlugin] = {}
_PLUGINS_BY_CODE: dict[str, MorphPlugin] = {}

# language code → default morphology_rules_ref
DEFAULT_RULES_REF = {
    "es": "es_v1",
    "fr": "fr_v1",
    "ja": "ja_v1",
    "ko": "ko_v1",
    "zh": "zh_v1",
}


def register_plugin(plugin: MorphPlugin) -> None:
    _PLUGINS_BY_REF[plugin.ref] = plugin
    for code in plugin.language_codes:
        _PLUGINS_BY_CODE[code.lower()] = plugin


def get_plugin(language_code: str | None = None, rules_ref: str | None = None) -> MorphPlugin | None:
    if rules_ref and rules_ref in _PLUGINS_BY_REF:
        return _PLUGINS_BY_REF[rules_ref]
    if language_code:
        code = language_code.strip().lower()
        if code in _PLUGINS_BY_CODE:
            return _PLUGINS_BY_CODE[code]
        # zh-Hant / zh-Hans → zh
        if code.startswith("zh"):
            return _PLUGINS_BY_CODE.get("zh")
    return None


def conjugate(
    language_code: str,
    lemma: str,
    slots: dict[str, Any] | None = None,
    *,
    rules_ref: str | None = None,
) -> str | None:
    plugin = get_plugin(language_code, rules_ref)
    if not plugin:
        return None
    norm = normalize_slots(slots)
    return plugin.conjugate(lemma.strip(), norm)


def ensure_plugins_loaded() -> None:
    """Import language modules so they self-register."""
    from . import es_conjugator  # noqa: F401
    from . import fr_conjugator  # noqa: F401
    from . import ja_conjugator  # noqa: F401
    from . import ko_conjugator  # noqa: F401
    from . import zh_transforms  # noqa: F401
