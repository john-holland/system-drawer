"""Zoning rules engine — gov-glove computeZoning + zone documents."""

from __future__ import annotations

import json
from typing import Any

import yaml

from gov_glove_adapter import call_gov_glove
from society_graph import compute_population_support


def parse_zone_document(raw: str | dict | None, city_id: str) -> dict[str, Any]:
    if raw is None:
        from society_db import default_zone_document

        return default_zone_document(city_id)
    if isinstance(raw, dict):
        return raw
    text = raw.strip()
    if text.startswith("{"):
        return json.loads(text)
    return yaml.safe_load(text)


def run_zoning(
    city_id: str,
    city_size_sqm: float,
    annual_budget_usd: float,
    zone_document: dict[str, Any],
    existing_buildings: list[dict],
    commodity_indices: dict[str, float],
    allow_debt: bool,
    lobbyist_activity: float = 0.3,
) -> dict[str, Any]:
    glove = call_gov_glove(
        "computeZoning",
        {
            "citySizeSqm": city_size_sqm,
            "annualBudgetUsd": annual_budget_usd,
            "zoneDocument": zone_document,
            "existingBuildings": existing_buildings,
            "commodityIndices": commodity_indices,
            "allowDebt": allow_debt,
            "lobbyistActivity": lobbyist_activity,
        },
    )
    lobby = call_gov_glove("processLobbyistImpacts", {"lobbyistActivity": lobbyist_activity})
    pop = compute_population_support(
        set(z.get("propertyClass", "") for z in zone_document.get("zones", [])),
        annual_budget_usd,
        commodity_indices,
        lobby.get("healthcareCoverage", 0.85),
    )
    glove["supportedPopulationMin"] = pop["supportedPopulationMin"]
    glove["supportedPopulationMax"] = pop["supportedPopulationMax"]
    glove["featureVector"] = {**lobby, **(glove.get("featureVector") or {})}
    return glove
