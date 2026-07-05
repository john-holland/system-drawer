"""Invoke gov-glove npm CLI and map results to SocietySnapshot."""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Any

_API_ROOT = Path(__file__).resolve().parent


def resolve_gov_glove_bin() -> Path:
    override = os.environ.get("GOV_GLOVE_BIN")
    if override:
        return Path(override)
    win = sys.platform == "win32"
    candidates = [
        _API_ROOT / "node_modules" / ".bin" / ("gov-glove.cmd" if win else "gov-glove"),
        _API_ROOT / "vendor" / "gov-glove" / "dist" / "cli.js",
    ]
    for c in candidates:
        if c.exists():
            if c.suffix == ".js":
                return c
            return c
    raise FileNotFoundError(
        "gov-glove CLI not found. Run: cd Scripts/continuuuum_api && npm install"
    )


def call_gov_glove(method: str, params: dict[str, Any] | None = None) -> dict[str, Any]:
    cli = resolve_gov_glove_bin()
    payload = json.dumps({"method": method, "params": params or {}})
    if cli.suffix == ".js":
        cmd = ["node", str(cli)]
    else:
        cmd = [str(cli)]
    proc = subprocess.run(
        cmd,
        input=payload,
        capture_output=True,
        text=True,
        timeout=30,
        check=False,
    )
    if proc.returncode != 0:
        raise RuntimeError(proc.stderr or proc.stdout or f"gov-glove exited {proc.returncode}")
    data = json.loads(proc.stdout)
    if not data.get("ok"):
        raise RuntimeError(data.get("error", "gov-glove error"))
    return data["result"]


def to_society_snapshot(city_id: str, tick_index: int, glove: dict[str, Any], zoning: dict[str, Any] | None = None) -> dict[str, Any]:
    zoning = zoning or {}
    fv = zoning.get("featureVector") or glove
    return {
        "cityId": city_id,
        "tickIndex": tick_index,
        "taxRate": fv.get("taxRate", 0.07),
        "healthcareCoverage": fv.get("healthcareCoverage", 0.85),
        "elderlyCareCoverage": fv.get("elderlyCareCoverage", 0.85),
        "welfareBenefits": fv.get("welfareBenefits", 0.7),
        "lobbyistActivity": fv.get("lobbyistActivity", 0.3),
        "congressStability": fv.get("congressStability", 0.85),
        "stateBudgetDelta": fv.get("stateBudgetDelta", 0),
        "lobbyistTaxDelta": glove.get("lobbyistDelta", 0) if "lobbyistDelta" in glove else 0,
        "supportedPopulationMin": zoning.get("supportedPopulationMin"),
        "supportedPopulationMax": zoning.get("supportedPopulationMax"),
        "zoningAllocations": zoning.get("allocations", []),
        "cityScapeProfile": zoning.get("cityScapeProfile"),
    }
