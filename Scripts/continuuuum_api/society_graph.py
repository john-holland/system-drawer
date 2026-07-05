"""Society feature graph — population support and feature attribution."""

from __future__ import annotations

import csv
from pathlib import Path
from typing import Any

ACTUARIAL_DIR = Path(__file__).resolve().parent / "data" / "actuarial"
DEFAULT_FEATURES = {
    "psychological": ["stress", "addiction_proclivity", "spirituality_index"],
    "social": ["civic_trust", "religious_attendance", "hobby_participation"],
    "societal": ["tax_burden", "lobby_pressure", "congress_stability"],
    "property_class": [
        "public",
        "religious",
        "commercial",
        "private",
        "hobby_venue",
        "addiction_venue",
    ],
}

LOBBY_PRESETS = {
    "lobbyists_congress_on_crack": {
        "lobbyistActivity": 2.5,
        "congressStability": 0.2,
    },
}


def load_actuarial_bands() -> list[dict[str, str]]:
    path = ACTUARIAL_DIR / "population_bands.csv"
    if not path.exists():
        return [
            {"age_band": "0-17", "mortality": "0.001", "morbidity": "0.05"},
            {"age_band": "18-64", "mortality": "0.002", "morbidity": "0.12"},
            {"age_band": "65+", "mortality": "0.04", "morbidity": "0.35"},
        ]
    with path.open(encoding="utf-8") as f:
        return list(csv.DictReader(f))


def compute_population_support(
    enabled_features: set[str],
    annual_budget_usd: float,
    commodity_indices: dict[str, float] | None = None,
    healthcare_coverage: float = 0.85,
) -> dict[str, Any]:
    commodity_indices = commodity_indices or {}
    bands = load_actuarial_bands()
    morbidity_sum = sum(float(b.get("morbidity", 0.1)) for b in bands)
    cost_index = sum(commodity_indices.values()) / max(len(commodity_indices), 1)
    cost_per_capita = 8000 * (1 + cost_index * 0.1) * (2 - healthcare_coverage)
    if cost_per_capita <= 0:
        cost_per_capita = 8000
    supported_max = int(annual_budget_usd / cost_per_capita)
    supported_min = int(supported_max * 0.6 * (1 - morbidity_sum * 0.1))
    attribution = []
    for layer, nodes in DEFAULT_FEATURES.items():
        for node in nodes:
            if node in enabled_features or layer == "property_class":
                attribution.append({"layer": layer, "node": node, "weight": 1.0 / len(nodes)})
    return {
        "supportedPopulationMin": max(supported_min, 0),
        "supportedPopulationMax": max(supported_max, supported_min),
        "costPerCapitaUsd": round(cost_per_capita, 2),
        "featureAttribution": attribution,
    }


def apply_preset(preset_id: str) -> dict[str, float]:
    return dict(LOBBY_PRESETS.get(preset_id, {}))
