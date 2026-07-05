"""Parse {P:dream-day|aspect=...|spatial2d-slot=...} lemma spans."""

from __future__ import annotations

import re
from typing import Any

DREAM_DAY_RE = re.compile(
    r"\{P:dream-day(?:\|([^}]+))?\}",
    re.IGNORECASE,
)


def _parse_params(raw: str | None) -> dict[str, str]:
    out: dict[str, str] = {}
    if not raw:
        return out
    for part in raw.split("|"):
        if "=" in part:
            k, v = part.split("=", 1)
            out[k.strip().lower()] = v.strip().strip('"')
    return out


def parse_dream_day_spans(text: str) -> list[dict[str, Any]]:
    results: list[dict[str, Any]] = []
    for m in DREAM_DAY_RE.finditer(text or ""):
        params = _parse_params(m.group(1))
        results.append(
            {
                "span": m.group(0),
                "aspectId": params.get("aspect") or params.get("aspectid") or "",
                "spatial2dSlot": params.get("spatial2d-slot") or params.get("spatial2dslot") or "",
                "satisfiedHint": params.get("satisfied"),
            }
        )
    return results


def compile_dream_day_hints(text: str) -> dict[str, Any]:
    spans = parse_dream_day_spans(text)
    by_aspect: dict[str, dict[str, Any]] = {}
    for s in spans:
        aid = s.get("aspectId") or "unknown"
        by_aspect[aid] = s
    return {"spans": spans, "byAspect": by_aspect}
