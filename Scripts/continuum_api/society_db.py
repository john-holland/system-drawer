"""Society SQLite schema bootstrap and seeds."""

from __future__ import annotations

import csv
import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
SCHEMA_PATH = REPO_ROOT / "continuum_society_schema.sql"
SEED_BUILDING_TYPES = Path(__file__).resolve().parent / "data" / "building_types_seed.csv"
EARTH_PREFIX = {"dimensional": 0, "galactic": 1, "system": 0, "planet": 1}


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_society_tables(conn: sqlite3.Connection) -> None:
    if SCHEMA_PATH.exists():
        conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    conn.commit()
    _seed_defaults(conn)


def _seed_defaults(conn: sqlite3.Connection) -> None:
    now = _now()
    cur = conn.execute("SELECT 1 FROM society_planets WHERE planet_id = ?", ("earth",))
    if not cur.fetchone():
        conn.execute(
            """INSERT INTO society_planets
               (planet_id, display_name, galactic_prefix_json, default_network_id, commodity_indices_json, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?)""",
            (
                "earth",
                "Earth",
                json.dumps(EARTH_PREFIX),
                "society.planet.earth",
                json.dumps({"water": 1.0, "power": 1.0, "steel": 1.0, "labor": 1.0}),
                now,
                now,
            ),
        )
    if SEED_BUILDING_TYPES.exists():
        with SEED_BUILDING_TYPES.open(encoding="utf-8") as f:
            reader = csv.DictReader(f)
            for row in reader:
                conn.execute(
                    """INSERT INTO building_type_maps
                       (building_type_id, display_name, property_class, lemma_entry_id, prefab_id,
                        default_opex_usd, service_profile_json, priority, updated_at)
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                       ON CONFLICT(building_type_id) DO UPDATE SET
                         display_name=excluded.display_name,
                         property_class=excluded.property_class,
                         prefab_id=excluded.prefab_id,
                         default_opex_usd=excluded.default_opex_usd,
                         updated_at=excluded.updated_at""",
                    (
                        row["building_type_id"],
                        row["display_name"],
                        row["property_class"],
                        row.get("lemma_term") or None,
                        row.get("prefab_id") or None,
                        float(row.get("default_opex_usd") or 0),
                        "{}",
                        0,
                        now,
                    ),
                )
    conn.commit()


def default_zone_document(city_id: str) -> dict:
    return {
        "cityId": city_id,
        "version": 1,
        "zones": [
            {
                "id": "residential_low",
                "propertyClass": "private",
                "maxFAR": 1.2,
                "minAreaShare": 0.35,
                "budgetLineShare": 0.12,
                "allowedUses": ["housing"],
                "defaultBuildingTypes": ["residential_duplex"],
            },
            {
                "id": "commercial_core",
                "propertyClass": "commercial",
                "maxFAR": 4.0,
                "minAreaShare": 0.15,
                "budgetLineShare": 0.22,
                "defaultBuildingTypes": ["corner_store"],
            },
            {
                "id": "public_services",
                "propertyClass": "public",
                "maxFAR": 0.8,
                "minAreaShare": 0.10,
                "budgetLineShare": 0.28,
                "defaultBuildingTypes": ["city_hall"],
            },
        ],
        "commodityRules": {
            "water": {"budgetMultiplier": 1.0, "zoningStiffness": 0.5},
            "power": {"budgetMultiplier": 1.0, "zoningStiffness": 0.5},
            "steel": {"budgetMultiplier": 0.8, "zoningStiffness": 0.3},
            "labor": {"budgetMultiplier": 1.2, "zoningStiffness": 0.4},
        },
        "debtPolicy": {"allowDebt": False, "maxDebtToBudgetRatio": 2.0},
    }


def new_id() -> str:
    return str(uuid.uuid4())
