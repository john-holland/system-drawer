"""Load and merge Cave manifest + tome YAML configs."""

from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

CAVE_DIR = Path(__file__).resolve().parent


def _load_yaml(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    with path.open(encoding="utf-8") as f:
        data = yaml.safe_load(f) or {}
    return data if isinstance(data, dict) else {}


def load_cave_manifest(*, reload: bool = False) -> dict[str, Any]:
    del reload  # reserved for cache invalidation
    manifest = _load_yaml(CAVE_DIR / "cave.manifest.yaml")
    tomes_dir = CAVE_DIR / "tomes"
    tome_fragments: list[dict[str, Any]] = []
    if tomes_dir.exists():
        for path in sorted(tomes_dir.glob("*.yaml")):
            cfg = _load_yaml(path)
            if cfg.get("id"):
                tome_fragments.append(cfg)
    manifest.setdefault("tome_configs", tome_fragments)
    return manifest


def message_to_structural(manifest: dict[str, Any], message: str) -> str | None:
    messages = manifest.get("messages") or {}
    val = messages.get(message)
    if isinstance(val, str):
        return val
    return None


def resolve_structural_route(body: dict[str, Any], manifest: dict[str, Any] | None = None) -> str:
    manifest = manifest or load_cave_manifest()
    route = str(body.get("route") or "")
    if route:
        _, structural = parse_route_from_full(route)
        return structural
    message = body.get("message")
    if message:
        mapped = message_to_structural(manifest, str(message))
        if mapped:
            return mapped
    return ""


def parse_route_from_full(route: str) -> tuple[str | None, str]:
    from cave.paths import parse_route

    return parse_route(route)


def get_handler_spec(manifest: dict[str, Any], structural: str) -> dict[str, Any] | None:
    handlers = manifest.get("handlers") or {}
    spec = handlers.get(structural)
    return spec if isinstance(spec, dict) else None


def get_lvm_events(manifest: dict[str, Any], structural: str) -> list[str]:
    spec = get_handler_spec(manifest, structural) or {}
    events = spec.get("lvm_events") or []
    return [str(e) for e in events if e]


def is_mutating_route(manifest: dict[str, Any], structural: str) -> bool:
    spec = get_handler_spec(manifest, structural) or {}
    if spec.get("mutating"):
        return True
    mutating = manifest.get("mutating_routes") or []
    return structural in mutating
