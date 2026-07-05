"""Bidirectional city size / budget constraint solver."""

from __future__ import annotations

from typing import Any

from zoning_rules_engine import run_zoning


def solve(
    mode: str,
    city_id: str,
    city_size_sqm: float | None,
    annual_budget_usd: float | None,
    zone_document: dict,
    existing_buildings: list[dict],
    commodity_indices: dict[str, float],
    allow_debt: bool,
) -> dict[str, Any]:
    mode = mode or "forward"
    size = float(city_size_sqm or 1_000_000)
    budget = float(annual_budget_usd or 10_000_000)

    if mode == "required_budget":
        result = run_zoning(city_id, size, budget, zone_document, existing_buildings, commodity_indices, allow_debt)
        required = float(result.get("requiredBudgetUsd") or budget)
        result["solvedAnnualBudgetUsd"] = required
        result["mode"] = mode
        return result

    if mode == "required_size":
        lo, hi = 100_000, budget * 2
        best = size
        for _ in range(24):
            mid = (lo + hi) / 2
            result = run_zoning(city_id, mid, budget, zone_document, existing_buildings, commodity_indices, False)
            req = float(result.get("requiredBudgetUsd") or 0)
            if req <= budget:
                best = mid
                lo = mid
            else:
                hi = mid
        result = run_zoning(city_id, best, budget, zone_document, existing_buildings, commodity_indices, allow_debt)
        result["solvedCitySizeSqm"] = best
        result["mode"] = mode
        return result

    if mode == "debt_unbounded":
        allow_debt = True

    result = run_zoning(city_id, size, budget, zone_document, existing_buildings, commodity_indices, allow_debt)
    result["mode"] = mode
    if allow_debt:
        debt_projection = max(0, float(result.get("requiredBudgetUsd") or 0) - budget)
        result["debtProjectionUsd"] = debt_projection
    return result
