"""Validate work-order causality graphs; port of CausalityFamilyAudit semantics."""

from __future__ import annotations

import json
import sqlite3
from typing import Any


def is_compatible_prefix(parent_prefix: str | None, child_prefix: str | None) -> bool:
    if not parent_prefix:
        return True
    if not child_prefix:
        return False
    if child_prefix == parent_prefix:
        return True
    return child_prefix.startswith(parent_prefix + ".")


def is_bisecting_snake(a: str, b: str) -> bool:
    if a == b:
        return False
    if is_compatible_prefix(a, b) or is_compatible_prefix(b, a):
        return False
    pa, pb = a.split("."), b.split(".")
    shared = 0
    for i in range(min(len(pa), len(pb))):
        if pa[i] != pb[i]:
            break
        shared += 1
    return shared > 0 and not a.startswith(b + ".") and not b.startswith(a + ".")


def audit_leaf_prefixes(prefixes: list[str]) -> list[str]:
    violations: list[str] = []
    sorted_p = sorted(p for p in prefixes if p)
    for i in range(len(sorted_p)):
        for j in range(i + 1, len(sorted_p)):
            if is_bisecting_snake(sorted_p[i], sorted_p[j]):
                violations.append(f"{sorted_p[i]} x {sorted_p[j]}")
    return violations


def _parse_depends(raw: str | None) -> list[str]:
    if not raw:
        return []
    try:
        data = json.loads(raw)
        return [str(x) for x in data] if isinstance(data, list) else []
    except json.JSONDecodeError:
        return []


def validate_work_orders(
    conn: sqlite3.Connection,
    work_order_ids: list[str] | None = None,
    episode_id: str | None = None,
) -> dict[str, Any]:
    """Return { ok, buildErrors: [{ code, message, workOrderId? }] }."""
    errors: list[dict[str, str]] = []
    params: list[Any] = []
    where = "1=1"
    if work_order_ids:
        placeholders = ",".join("?" * len(work_order_ids))
        where += f" AND id IN ({placeholders})"
        params.extend(work_order_ids)
    elif episode_id:
        where += " AND episode_id = ?"
        params.append(episode_id)

    rows = conn.execute(
        f"SELECT id, causality_leaf_id, depends_on, status, episode_id FROM work_orders WHERE {where}",
        params,
    ).fetchall()
    if not rows:
        return {"ok": True, "buildErrors": []}

    wo_by_id = {r["id"]: dict(r) for r in rows}
    all_ids = set(wo_by_id.keys())

    for wo_id, wo in wo_by_id.items():
        deps = _parse_depends(wo.get("depends_on"))
        for dep in deps:
            if dep not in all_ids:
                cur = conn.execute("SELECT 1 FROM work_orders WHERE id = ?", (dep,))
                if not cur.fetchone():
                    errors.append(
                        {
                            "code": "missing_dependency",
                            "message": f"Work order {wo_id} depends on missing {dep}",
                            "workOrderId": wo_id,
                        }
                    )

    visiting: set[str] = set()
    visited: set[str] = set()

    def dfs(node: str, stack: set[str]) -> None:
        if node in stack:
            errors.append(
                {
                    "code": "cycle",
                    "message": f"Dependency cycle involving {node}",
                    "workOrderId": node,
                }
            )
            return
        if node in visited:
            return
        stack.add(node)
        wo = wo_by_id.get(node)
        if wo:
            for dep in _parse_depends(wo.get("depends_on")):
                if dep in wo_by_id:
                    dfs(dep, stack)
        stack.discard(node)
        visited.add(node)

    for wo_id in wo_by_id:
        dfs(wo_id, set())

    leaves = [wo.get("causality_leaf_id") or "" for wo in wo_by_id.values()]
    for v in audit_leaf_prefixes(leaves):
        errors.append({"code": "bisecting_snake", "message": v})

    ep_ids = {wo.get("episode_id") for wo in wo_by_id.values() if wo.get("episode_id")}
    for ep in ep_ids:
        cs = conn.execute(
            "SELECT id FROM causality_structure WHERE episode_id = ? LIMIT 1", (ep,)
        ).fetchone()
        if not cs and any(wo.get("causality_leaf_id") for wo in wo_by_id.values() if wo.get("episode_id") == ep):
            errors.append(
                {
                    "code": "missing_causality_structure",
                    "message": f"Episode {ep} has causality work orders but no causality_structure row",
                }
            )

    incomplete = [wo_id for wo_id, wo in wo_by_id.items() if wo.get("status") != "done"]
    if incomplete:
        errors.append(
            {
                "code": "incomplete_work_orders",
                "message": f"{len(incomplete)} work order(s) not done",
            }
        )

    return {"ok": len(errors) == 0, "buildErrors": errors}
