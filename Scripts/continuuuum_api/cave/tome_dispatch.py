"""Map tome machine events to structural Cave routes."""

from __future__ import annotations

from typing import Any

from cave.manifest_loader import load_cave_manifest, message_to_structural


def resolve_tome_event_route(
    tome_id: str,
    machine_id: str,
    event: str,
    manifest: dict[str, Any] | None = None,
) -> str | None:
    manifest = manifest or load_cave_manifest()
    for tome in manifest.get("tome_configs") or []:
        if tome.get("id") != tome_id:
            continue
        machines = tome.get("machines") or {}
        machine = machines.get(machine_id) if isinstance(machines, dict) else None
        if isinstance(machine, dict):
            events = machine.get("events") or {}
            message_name = events.get(event) or events.get(event.upper()) or events.get(event.lower())
            if message_name:
                mapped = message_to_structural(manifest, str(message_name))
                if mapped:
                    return mapped
        robot = tome.get("robotCopy") or {}
        flows = robot.get("flows") or {}
        flow = flows.get(event) or flows.get(event.lower())
        if isinstance(flow, dict) and flow.get("message"):
            mapped = message_to_structural(manifest, str(flow["message"]))
            if mapped:
                return mapped
    return None
