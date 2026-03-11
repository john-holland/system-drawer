"""
Generate fungible work orders from causality tree. Linear chains get depends_on;
hub-and-spoke work orders are parallelizable.
"""

import json
import logging
import uuid
from pathlib import Path
from typing import Any

log = logging.getLogger(__name__)


def generate_work_orders(
    episode_id: str,
    narrative_types: dict[str, str],  # node_id -> "linear" | "hub_and_spoke"
    edges: list[tuple[str, str]],  # (from, to) causality edges
    prompt_descriptions: dict[str, str] | None = None,  # node_id -> description
) -> list[dict[str, Any]]:
    """
    Transform causality graph into work orders.

    - Linear nodes: chain with depends_on (topological order)
    - Hub-and-spoke nodes: no depends_on (parallelizable)
    """
    prompt_descriptions = prompt_descriptions or {}

    # Topological sort for linear chain
    in_deg: dict[str, int] = {}
    for a, b in edges:
        in_deg.setdefault(a, 0)
        in_deg[b] = in_deg.get(b, 0) + 1
    linear_nodes = [n for n, t in narrative_types.items() if t == "linear"]
    hub_nodes = [n for n, t in narrative_types.items() if t == "hub_and_spoke"]

    # Build depends_on for linear: each linear node depends on its predecessors in topo order
    from collections import deque
    adj = {a: [] for a, _ in edges}
    for a, b in edges:
        adj[a].append(b)
    topo: list[str] = []
    q = deque(n for n in in_deg if in_deg[n] == 0)
    while q:
        u = q.popleft()
        topo.append(u)
        for v in adj.get(u, []):
            in_deg[v] -= 1
            if in_deg[v] == 0:
                q.append(v)

    pred: dict[str, str] = {}
    for a, b in edges:
        pred[b] = a

    work_orders: list[dict[str, Any]] = []
    seen = set()
    node_to_wo_id: dict[str, str] = {}

    def make_wo(node: str, narrative_type: str, depends_wo_ids: list[str]) -> dict[str, Any]:
        wo_id = f"wo_{uuid.uuid4().hex[:12]}"
        node_to_wo_id[node] = wo_id
        return {
            "id": wo_id,
            "episode_id": episode_id,
            "causality_leaf_id": node,
            "asset_id": node,
            "narrative_type": narrative_type,
            "depends_on": json.dumps(depends_wo_ids),
            "prompt_description": prompt_descriptions.get(node, ""),
            "status": "pending",
            "assigned_to": None,
        }

    for node in topo:
        if node not in linear_nodes and node not in hub_nodes:
            continue
        if node in seen:
            continue
        seen.add(node)
        dep_ids = []
        if node in pred and pred[node] in node_to_wo_id:
            dep_ids = [node_to_wo_id[pred[node]]]
        wo = make_wo(node, narrative_types.get(node, "linear"), dep_ids)
        work_orders.append(wo)

    for node in hub_nodes:
        if node in seen:
            continue
        seen.add(node)
        wo = make_wo(node, "hub_and_spoke", [])
        work_orders.append(wo)

    return work_orders
