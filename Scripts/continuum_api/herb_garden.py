"""
Herb Garden: causality 4D plant similarity tool.
Compares causality trees (episodes/scripts) to plant germination/development processes.
Plant script format: stage:value|condition:value|description:...
"""

from __future__ import annotations

import re
from typing import Any


def parse_plant_script(text: str) -> list[dict[str, Any]]:
    """Parse plant germination script into list of {stage, condition, description, order}."""
    nodes = []
    for i, line in enumerate(text.strip().splitlines()):
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        stage = condition = description = None
        for part in line.split("|"):
            part = part.strip()
            if part.startswith("stage:"):
                stage = part[6:].strip()
            elif part.startswith("condition:"):
                condition = part[10:].strip()
            elif part.startswith("description:"):
                description = part[12:].strip()
        if stage or condition or description:
            nodes.append({
                "stage": stage or "",
                "condition": condition or "",
                "description": description or "",
                "order": i,
            })
    return nodes


def get_causality_for_episode(conn, episode_id: str) -> list[dict[str, Any]]:
    """Get causality structure and work_orders for an episode as comparable nodes."""
    nodes = []
    cur = conn.execute(
        "SELECT id, structure_type, description FROM causality_structure WHERE episode_id = ?",
        (episode_id,),
    )
    for r in cur.fetchall():
        nodes.append({
            "stage": r["structure_type"],
            "condition": "causality",
            "description": r["description"] or "",
            "order": len(nodes),
            "source": "causality_structure",
            "id": r["id"],
        })
    cur = conn.execute(
        "SELECT id, narrative_type, prompt_description FROM work_orders WHERE episode_id = ?",
        (episode_id,),
    )
    for r in cur.fetchall():
        nodes.append({
            "stage": r["narrative_type"],
            "condition": "work_order",
            "description": r["prompt_description"] or "",
            "order": len(nodes),
            "source": "work_orders",
            "id": r["id"],
        })
    return nodes


def get_causality_for_draft(conn, draft_id: str) -> list[dict[str, Any]]:
    """Get causality for a draft via its episode_id (if linked)."""
    cur = conn.execute("SELECT episode_id FROM draft_episodes WHERE id = ?", (draft_id,))
    row = cur.fetchone()
    if not row or not row["episode_id"]:
        return []
    return get_causality_for_episode(conn, row["episode_id"])


def _norm(s: str) -> str:
    return " ".join((s or "").lower().split())


def compare_structures(
    script_causality: list[dict[str, Any]],
    plant_causality: list[dict[str, Any]],
) -> dict[str, Any]:
    """Compare script causality to plant structure. Returns similarity report."""

    ## we approach matching the causality and scores on description for the plant and script here, but we should also track length of similar exploration segments using BFS and DFS
    ##    i'd love to see the herb garden produced from a recursive depth match in addition to the spread matching algorithm
    report = {"matches": [], "scores": [], "alignment": [], "summary": {}}
    if not plant_causality:
        report["summary"] = {"message": "No plant nodes to compare", "avgScore": 0}
        return report
    scores = []
    for i, sc in enumerate(script_causality):
        best_score = 0
        best_j = -1
        for j, pc in enumerate(plant_causality):
            score = 0
            if _norm(sc.get("stage", "")) and _norm(pc.get("stage", "")):
                if _norm(sc["stage"]) == _norm(pc["stage"]):
                    score += 0.5
                elif _norm(sc["stage"]) in _norm(pc["stage"]) or _norm(pc["stage"]) in _norm(sc["stage"]):
                    score += 0.3
            if _norm(sc.get("description", "")) and _norm(pc.get("description", "")):
                w1 = set(_norm(sc["description"]).split())
                w2 = set(_norm(pc["description"]).split())
                overlap = len(w1 & w2) / max(len(w1 | w2), 1)
                score += overlap * 0.5
            if score > best_score:
                best_score = score
                best_j = j
        if best_j >= 0:
            report["matches"].append({
                "scriptIndex": i,
                "plantIndex": best_j,
                "scriptNode": sc,
                "plantNode": plant_causality[best_j],
                "score": round(best_score, 3),
            })
            report["alignment"].append((i, best_j))
            scores.append(best_score)
    report["summary"] = {
        "scriptNodes": len(script_causality),
        "plantNodes": len(plant_causality),
        "matches": len(report["matches"]),
        "avgScore": round(sum(scores) / len(scores), 3) if scores else 0,
    }
    return report
