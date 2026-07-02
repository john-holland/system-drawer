"""Spatial generator (2D / 3D / 4D) property specs for lemma library display and validation."""

from __future__ import annotations

import json
import sqlite3
from typing import Any

SPATIAL_GENERATOR_DEFINITIONS_KEY = "spatial-generator-definitions"

# Property spec rows seeded into localization_property_specs
SPATIAL_PROPERTY_SPECS: list[dict[str, Any]] = [
    {
        "key": "spatial-generator-definitions",
        "value_type": "json",
        "default_value": "[]",
        "description": "JSON array of spatial generator definitions (2d, 3d, 4d). Multiple allowed per lemma.",
        "allowed_values_json": json.dumps(["2d", "3d", "4d"]),
    },
    {
        "key": "spatial-gen-2d-label",
        "value_type": "string",
        "default_value": "",
        "description": "2D spatial generator label (legacy single-def shorthand).",
    },
    {
        "key": "spatial-gen-3d-label",
        "value_type": "string",
        "default_value": "",
        "description": "3D spatial generator label (legacy single-def shorthand).",
    },
    {
        "key": "spatial-gen-4d-label",
        "value_type": "string",
        "default_value": "",
        "description": "4D spatial generator label (legacy single-def shorthand).",
    },
    {
        "key": "spatial-gen-2d-grid-x",
        "value_type": "number",
        "default_value": "32",
        "description": "2D generator grid resolution X.",
    },
    {
        "key": "spatial-gen-2d-grid-y",
        "value_type": "number",
        "default_value": "32",
        "description": "2D generator grid resolution Y.",
    },
    {
        "key": "spatial-gen-3d-grid-x",
        "value_type": "number",
        "default_value": "16",
        "description": "3D generator grid resolution X.",
    },
    {
        "key": "spatial-gen-3d-grid-y",
        "value_type": "number",
        "default_value": "16",
        "description": "3D generator grid resolution Y.",
    },
    {
        "key": "spatial-gen-3d-grid-z",
        "value_type": "number",
        "default_value": "16",
        "description": "3D generator grid resolution Z.",
    },
    {
        "key": "spatial-gen-4d-grid-x",
        "value_type": "number",
        "default_value": "8",
        "description": "4D generator grid resolution X.",
    },
    {
        "key": "spatial-gen-4d-grid-y",
        "value_type": "number",
        "default_value": "8",
        "description": "4D generator grid resolution Y.",
    },
    {
        "key": "spatial-gen-4d-grid-z",
        "value_type": "number",
        "default_value": "8",
        "description": "4D generator grid resolution Z.",
    },
    {
        "key": "spatial-gen-4d-slice-count",
        "value_type": "number",
        "default_value": "4",
        "description": "4D generator temporal slice count.",
    },
    {
        "key": "spatial-gen-4d-t-min",
        "value_type": "number",
        "default_value": "0",
        "description": "4D generator narrative time minimum (seconds).",
    },
    {
        "key": "spatial-gen-4d-t-max",
        "value_type": "number",
        "default_value": "3600",
        "description": "4D generator narrative time maximum (seconds).",
    },
]


def ensure_spatial_property_specs(conn: sqlite3.Connection) -> None:
    for spec in SPATIAL_PROPERTY_SPECS:
        row = conn.execute(
            "SELECT key FROM localization_property_specs WHERE key = ?",
            (spec["key"],),
        ).fetchone()
        if row:
            continue
        conn.execute(
            """INSERT INTO localization_property_specs
               (key, value_type, allowed_values_json, default_value, description)
               VALUES (?, ?, ?, ?, ?)""",
            (
                spec["key"],
                spec["value_type"],
                spec.get("allowed_values_json"),
                spec.get("default_value"),
                spec.get("description"),
            ),
        )
    conn.commit()


def parse_spatial_generator_definitions(raw: Any) -> list[dict[str, Any]]:
    if raw is None or raw == "":
        return []
    if isinstance(raw, list):
        return [d for d in raw if isinstance(d, dict)]
    if isinstance(raw, str):
        try:
            parsed = json.loads(raw)
            if isinstance(parsed, list):
                return [d for d in parsed if isinstance(d, dict)]
        except json.JSONDecodeError:
            pass
    return []


def definitions_from_legacy_properties(props: dict[str, Any]) -> list[dict[str, Any]]:
    """Build definition list from per-dimension shorthand property keys."""
    out: list[dict[str, Any]] = []
    for dim in ("2d", "3d", "4d"):
        label = props.get(f"spatial-gen-{dim}-label") or props.get(f"spatial-gen-{dim}")
        if not label:
            continue
        entry: dict[str, Any] = {"id": f"legacy_{dim}", "dimension": dim, "label": str(label)}
        if dim == "2d":
            if props.get("spatial-gen-2d-grid-x"):
                entry["gridResX"] = props["spatial-gen-2d-grid-x"]
            if props.get("spatial-gen-2d-grid-y"):
                entry["gridResY"] = props["spatial-gen-2d-grid-y"]
        elif dim == "3d":
            for axis in ("x", "y", "z"):
                k = f"spatial-gen-3d-grid-{axis}"
                if props.get(k):
                    entry[f"gridRes{axis.upper()}"] = props[k]
        elif dim == "4d":
            for axis in ("x", "y", "z"):
                k = f"spatial-gen-4d-grid-{axis}"
                if props.get(k):
                    entry[f"gridRes{axis.upper()}"] = props[k]
            if props.get("spatial-gen-4d-slice-count"):
                entry["sliceCount"] = props["spatial-gen-4d-slice-count"]
            if props.get("spatial-4d-id"):
                entry["spatial4dId"] = props["spatial-4d-id"]
            if props.get("spatial-t-min") is not None:
                entry["tMin"] = props["spatial-t-min"]
            if props.get("spatial-t-max") is not None:
                entry["tMax"] = props["spatial-t-max"]
        out.append(entry)
    return out


def enrich_entry_spatial_generators(view: dict[str, Any]) -> None:
    props = view.get("properties") or {}
    defs = parse_spatial_generator_definitions(props.get(SPATIAL_GENERATOR_DEFINITIONS_KEY))
    if not defs:
        defs = definitions_from_legacy_properties(props)
    if view.get("spatial4dId") and not any(d.get("spatial4dId") for d in defs):
        for d in defs:
            if d.get("dimension") == "4d":
                d.setdefault("spatial4dId", view["spatial4dId"])
                break

    view["spatialGeneratorDefinitions"] = defs
    labels_2d = [d.get("label") or d.get("id") for d in defs if d.get("dimension") == "2d"]
    labels_3d = [d.get("label") or d.get("id") for d in defs if d.get("dimension") == "3d"]
    labels_4d = [d.get("label") or d.get("id") for d in defs if d.get("dimension") == "4d"]
    view["spatialGen2d"] = ", ".join(str(x) for x in labels_2d if x) or ""
    view["spatialGen3d"] = ", ".join(str(x) for x in labels_3d if x) or ""
    view["spatialGen4d"] = ", ".join(str(x) for x in labels_4d if x) or ""
    view["spatialGenDims"] = ", ".join(
        sorted({str(d.get("dimension") or "").upper() for d in defs if d.get("dimension")})
    )
