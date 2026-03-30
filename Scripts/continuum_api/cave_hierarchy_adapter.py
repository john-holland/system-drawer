"""
Cave hierarchy adapter: export JSON flat file of adapters, routing, and config.
Shows adapters in use (Cave, Tome, LogViewMachine, RobotCopy, CaveRobit),
hierarchical routing structure, and RobotCopy/CaveRobit configurations.
"""

from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Any


def _base_url() -> str:
    return os.environ.get("CAVE_BASE_URL", "http://localhost:3000").rstrip("/")


def _fetch_cave_routes() -> dict[str, Any] | None:
    """Fetch routes from Cave if it exposes GET /api/routes."""
    try:
        import urllib.request
        url = _base_url() + "/api/routes"
        req = urllib.request.Request(url)
        with urllib.request.urlopen(req, timeout=5) as resp:
            return json.loads(resp.read().decode())
    except Exception:
        return None


def export_hierarchy_flatfile(output_path: str | Path) -> dict[str, Any]:
    """Walk Cave routing/config and emit JSON hierarchy. Write to output_path.
    Returns the hierarchy dict. Builds from local adapter config + Cave API if available."""
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    cave_routes = _fetch_cave_routes()
    base = _base_url()

    adapters = {
        "cave": {"url": base, "used_in": ["continuum_api"], "functions": ["search_library", "geocode", "upload_document", "forward_audit", "get_tome_header", "get_tome_footer", "get_config_overview"]},
        "tome": {"used_in": ["continuum_api"], "slots": ["header", "footer"], "api": f"{base}/api/tome/container"},
        "logViewMachine": {"used_in": [], "api": None},
        "robotCopy": {"used_in": [], "config": {}},
        "caveRobit": {"used_in": [], "config": {}},
    }

    routing = {
        "cave": {"base": base, "routes": cave_routes.get("routes", []) if isinstance(cave_routes, dict) else []},
        "tome": {"container_slots": ["header", "footer"]},
        "logViewMachine": {"routes": []},
    }

    robot_config = {"robotCopy": {}, "caveRobit": {}}
    if isinstance(cave_routes, dict):
        robot_config["robotCopy"] = cave_routes.get("robotCopy", {})
        robot_config["caveRobit"] = cave_routes.get("caveRobit", {})

    hierarchy = {
        "adapters": adapters,
        "routing": routing,
        "robotCopy": robot_config["robotCopy"],
        "caveRobit": robot_config["caveRobit"],
    }

    with open(output_path, "w") as f:
        json.dump(hierarchy, f, indent=2)

    return hierarchy
