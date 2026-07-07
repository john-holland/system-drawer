"""Statistical good-day horizon (no lemma hints) for double-day dream stack."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any


@dataclass
class GoodDayHorizonConfig:
    min_satisfied: float = 0.72
    max_satisfied: float = 0.92
    blend_society_weight: float = 0.85


def horizon_config_from_body(body: dict[str, Any] | None) -> GoodDayHorizonConfig:
    body = body or {}
    horizon = body.get("goodDayHorizon") or body.get("good_day_horizon") or {}
    if not isinstance(horizon, dict):
        horizon = {}
    return GoodDayHorizonConfig(
        min_satisfied=float(horizon.get("minSatisfied", horizon.get("min_satisfied", 0.72))),
        max_satisfied=float(horizon.get("maxSatisfied", horizon.get("max_satisfied", 0.92))),
        blend_society_weight=float(
            horizon.get("blendSocietyWeight", horizon.get("blend_society_weight", 0.85))
        ),
    )


def clamp_satisfied(value: float, config: GoodDayHorizonConfig) -> float:
    lo = max(0.0, min(1.0, config.min_satisfied))
    hi = max(lo, min(1.0, config.max_satisfied))
    return max(lo, min(hi, value))


def apply_horizon_to_aspect_state(
    aspect_state: dict[str, Any],
    config: GoodDayHorizonConfig,
) -> dict[str, Any]:
    """Clamp aspect satisfaction into the good-day statistical band."""
    raw = float(aspect_state.get("satisfied01", 0.55))
    weight = max(0.0, min(1.0, config.blend_society_weight))
    blended = raw * weight + config.min_satisfied * (1.0 - weight)
    satisfied01 = clamp_satisfied(blended, config)
    out = dict(aspect_state)
    out["satisfied01"] = round(satisfied01, 4)
    out["horizonClamped"] = True
    return out
