"""Multiplayer society coefficient merge (weather-style)."""

from __future__ import annotations

from typing import Any

from building_flywheel import merge_cell, server_client_weight


def blend_cells(
    server: dict[str, float],
    client: dict[str, float],
    confidence: float = 0.9,
    timeout_order: int = 0,
) -> dict[str, float]:
    w = server_client_weight(confidence, timeout_order)
    keys = set(server) | set(client)
    return {k: merge_cell(server.get(k, 0.0), client.get(k, 0.0), w) for k in keys}


def merge_snapshots(server: dict[str, Any], client: dict[str, Any]) -> dict[str, Any]:
    out = dict(server)
    for key in ("taxRate", "healthcareCoverage", "lobbyistActivity", "congressStability"):
        if key in client:
            out[key] = merge_cell(float(server.get(key, 0)), float(client[key]), 0.7)
    return out


def merge_routing_trees(server: dict, client: dict) -> dict:
    server_order = [n.get("building", {}).get("stableId") for n in server.get("nodes", [])]
    client_order = [n.get("building", {}).get("stableId") for n in client.get("nodes", [])]
    merged = list(dict.fromkeys(server_order + client_order))
    return {
        "treeId": server.get("treeId"),
        "visitOrder": [{"stableId": sid} for sid in merged if sid],
        "nodes": [{"type": "visit", "building": {"stableId": sid}} for sid in merged if sid],
    }
