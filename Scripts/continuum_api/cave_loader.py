"""Load Cave / Tome / CaveRobit YAML configs for Continuum."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import yaml

CAVE_DIR = Path(__file__).resolve().parent / "cave"
BUILTIN_PREORDER_CASE_ID = "00000000-0000-4000-8000-000000000001"
PLATFORM_PREORDER_FEATURE = "platform.preordering"


def _load_yaml(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    with path.open(encoding="utf-8") as f:
        data = yaml.safe_load(f) or {}
    return data if isinstance(data, dict) else {}


def load_cave_config() -> dict[str, Any]:
    return _load_yaml(CAVE_DIR / "cave.yaml")


def load_cave_manifest() -> dict[str, Any]:
    try:
        from cave.manifest_loader import load_cave_manifest as _load

        return _load()
    except ImportError:
        return _load_yaml(CAVE_DIR / "cave.manifest.yaml")


def load_cave_robit_config() -> dict[str, Any]:
    return _load_yaml(CAVE_DIR / "cave-robit.yaml")


def load_all_tome_configs() -> list[dict[str, Any]]:
    tomes_dir = CAVE_DIR / "tomes"
    configs: list[dict[str, Any]] = []
    if not tomes_dir.exists():
        return configs
    for path in sorted(tomes_dir.glob("*.yaml")):
        cfg = _load_yaml(path)
        if cfg.get("id"):
            configs.append(cfg)
    return configs


def build_routes_overview() -> dict[str, Any]:
    cave = load_cave_config()
    robit = load_cave_robit_config()
    tomes = load_all_tome_configs()
    manifest = load_cave_manifest()
    messages = manifest.get("messages") or {}
    spelunk = cave.get("spelunk") or {}
    child_routes = []
    for key, child in (spelunk.get("childCaves") or {}).items():
        if isinstance(child, dict):
            child_routes.append(
                {
                    "key": key,
                    "route": child.get("route"),
                    "tomeId": child.get("tomeId"),
                    "container": child.get("container"),
                }
            )
    tome_routes = []
    for t in tomes:
        tome_id = t.get("id")
        for machine, spec in (t.get("machines") or {}).items():
            if isinstance(spec, dict):
                for event, msg in (spec.get("events") or {}).items():
                    structural = messages.get(str(msg))
                    tome_routes.append(
                        {
                            "tomeId": tome_id,
                            "machine": machine,
                            "event": event,
                            "message": msg,
                            "structuralRoute": structural,
                            "path": f"/api/tomes/{tome_id}/machines/{machine}/message",
                            "method": "POST",
                        }
                    )
        routing = t.get("routing") or {}
        base = routing.get("basePath", "")
        for machine, spec in (routing.get("routes") or {}).items():
            if isinstance(spec, dict):
                tome_routes.append(
                    {
                        "tomeId": tome_id,
                        "machine": machine,
                        "path": f"{base}{spec.get('path', '')}",
                        "method": spec.get("method", "POST"),
                    }
                )
    return {
        "cave": {"name": cave.get("name"), "routes": child_routes},
        "tomes": tome_routes,
        "caveRobit": robit,
        "manifest": {"messages": messages, "handlerCount": len(manifest.get("handlers") or {})},
        "robotCopy": {"transport": "POST /cave/route", "legacyShim": "/api/tomes/{tome}/machines/{machine}/message"},
    }


def build_config_overview() -> dict[str, Any]:
    manifest = load_cave_manifest()
    return {
        "cave": load_cave_config(),
        "manifest": {
            "schema_version": manifest.get("schema_version"),
            "service": manifest.get("service"),
            "messages": manifest.get("messages"),
            "structural": manifest.get("structural"),
            "handlers": list((manifest.get("handlers") or {}).keys()),
        },
        "tomes": load_all_tome_configs(),
        "caveRobit": load_cave_robit_config(),
        "logViewMachine": {"version": "2.1.1", "package": "log-view-machine"},
    }


def resolve_robit_transport(to_tome: str) -> dict[str, Any] | None:
    robit = load_cave_robit_config()
    for route in robit.get("routes") or []:
        if route.get("toTome") == to_tome:
            return route.get("transport")
    return robit.get("defaultTransport")


def export_hierarchy_json() -> str:
    return json.dumps(build_routes_overview(), indent=2)
