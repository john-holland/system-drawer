"""
Procedural loop detection on forward causality graph. Labels nodes as
linear (no loops), non-linear (loops), or hub_and_spoke (loop complexes).
"""

import logging
from collections import defaultdict
from typing import Literal

log = logging.getLogger(__name__)

NarrativeType = Literal["linear", "non-linear", "hub_and_spoke"]


def detect_cycles(edges: list[tuple[str, str]]) -> list[list[str]]:
    """
    Find all cycles in a directed graph using DFS + back edges.
    edges: list of (from_id, to_id)
    Returns list of cycles (each cycle as list of node ids).
    """
    adj: dict[str, list[str]] = defaultdict(list)
    for a, b in edges:
        adj[a].append(b)

    cycles: list[list[str]] = []
    WHITE, GRAY, BLACK = 0, 1, 2
    color: dict[str, int] = defaultdict(lambda: WHITE)
    parent: dict[str, str] = {}
    stack: list[str] = []

    def dfs(u: str) -> bool:
        color[u] = GRAY
        stack.append(u)
        for v in adj[u]:
            if color[v] == GRAY:
                # Back edge: cycle found
                idx = stack.index(v)
                cycle = stack[idx:]
                cycles.append(cycle)
            elif color[v] == WHITE:
                parent[v] = u
                dfs(v)
        stack.pop()
        color[u] = BLACK
        return False

    for node in adj:
        if color[node] == WHITE:
            dfs(node)

    return cycles


def classify_narrative_type(
    edges: list[tuple[str, str]],
    node_ids: list[str] | None = None,
) -> dict[str, NarrativeType]:
    """
    Classify each node as linear, non-linear, or hub_and_spoke.

    - linear: node not in any cycle
    - non-linear: node in exactly one cycle
    - hub_and_spoke: node in multiple cycles or high fan-out (hub)

    Returns:
        Dict mapping node_id -> narrative_type
    """
    if node_ids is None:
        node_ids = list(set(a for a, _ in edges) | set(b for _, b in edges))

    cycles = detect_cycles(edges)
    node_in_cycles: dict[str, int] = defaultdict(int)
    for cycle in cycles:
        for n in set(cycle):
            node_in_cycles[n] += 1

    # Fan-out for hub detection
    fan_out: dict[str, int] = defaultdict(int)
    for a, _ in edges:
        fan_out[a] += 1
    max_fan = max(fan_out.values()) if fan_out else 0
    hub_threshold = max(3, max_fan // 2)

    result: dict[str, NarrativeType] = {}
    for n in node_ids:
        cycle_count = node_in_cycles[n]
        out = fan_out[n]
        if cycle_count >= 2 or (cycle_count >= 1 and out >= hub_threshold):
            result[n] = "hub_and_spoke"
        elif cycle_count == 1:
            result[n] = "non-linear"
        else:
            result[n] = "linear"

    return result


def analyze_causality_graph(
    edges: list[tuple[str, str]],
) -> dict:
    """
    Full analysis: cycles, narrative types, and structure summary.
    """
    cycles = detect_cycles(edges)
    node_ids = list(set(a for a, _ in edges) | set(b for _, b in edges))
    narrative_types = classify_narrative_type(edges, node_ids)

    return {
        "cycles": cycles,
        "cycle_count": len(cycles),
        "narrative_types": dict(narrative_types),
        "linear_count": sum(1 for t in narrative_types.values() if t == "linear"),
        "non_linear_count": sum(1 for t in narrative_types.values() if t == "non-linear"),
        "hub_and_spoke_count": sum(1 for t in narrative_types.values() if t == "hub_and_spoke"),
    }
