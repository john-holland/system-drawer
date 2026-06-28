"""Recombobulate spatial graph — audit and repair composed lemma spatial bindings."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

try:
    from continuum_api.lemma_composition import load_composition
except ImportError:
    from lemma_composition import load_composition

try:
    from thesaurus.clause_audit import farey_contains, farey_to_char, resolve_binding_char_span
except ImportError:
    from clause_audit import farey_contains, farey_to_char, resolve_binding_char_span


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _farey_tuple_from_json(raw: str | dict | None) -> tuple[int, int, int, int] | None:
    if not raw:
        return None
    data = raw if isinstance(raw, dict) else json.loads(raw)
    ln = int(data.get("ln") or data.get("fareyLeftNum") or data.get("farey_left_num") or 0)
    ld = int(data.get("ld") or data.get("fareyLeftDen") or data.get("farey_left_den") or 1)
    rn = int(data.get("rn") or data.get("fareyRightNum") or data.get("farey_right_num") or 1)
    rd = int(data.get("rd") or data.get("fareyRightDen") or data.get("farey_right_den") or 1)
    return ln, ld, rn, rd


def _farey_to_json(ft: tuple[int, int, int, int]) -> str:
    return json.dumps({"ln": ft[0], "ld": ft[1], "rn": ft[2], "rd": ft[3]})


def _spatial_aabb(row: sqlite3.Row) -> dict[str, float]:
    cx = float(row["center_x"])
    cy = float(row["center_y"])
    cz = float(row["center_z"])
    sx = float(row["size_x"])
    sy = float(row["size_y"])
    sz = float(row["size_z"])
    return {
        "xMin": cx - sx / 2,
        "xMax": cx + sx / 2,
        "yMin": cy - sy / 2,
        "yMax": cy + sy / 2,
        "zMin": cz - sz / 2,
        "zMax": cz + sz / 2,
        "tMin": float(row["t_min"]),
        "tMax": float(row["t_max"]),
    }


def _aabb_contains(outer: dict[str, float], inner: dict[str, float]) -> bool:
    return (
        inner["xMin"] >= outer["xMin"] - 1e-6
        and inner["xMax"] <= outer["xMax"] + 1e-6
        and inner["yMin"] >= outer["yMin"] - 1e-6
        and inner["yMax"] <= outer["yMax"] + 1e-6
        and inner["zMin"] >= outer["zMin"] - 1e-6
        and inner["zMax"] <= outer["zMax"] + 1e-6
        and inner["tMin"] >= outer["tMin"] - 1e-6
        and inner["tMax"] <= outer["tMax"] + 1e-6
    )


def _load_spatial(conn: sqlite3.Connection, spatial_id: str | None) -> sqlite3.Row | None:
    if not spatial_id:
        return None
    cur = conn.execute("SELECT * FROM spatial_4d WHERE id = ?", (spatial_id,))
    return cur.fetchone()


def _create_default_spatial(
    conn: sqlite3.Connection,
    entry_id: str,
    sort_order: int,
    episode_id: str | None,
) -> str:
    sid = str(uuid.uuid4())
    offset = sort_order * 2.0
    conn.execute(
        """
        INSERT INTO spatial_4d
            (id, tenant_id, episode_id, created_at,
             center_x, center_y, center_z, size_x, size_y, size_z, t_min, t_max, payload_label)
        VALUES (?, 'default', ?, ?, ?, ?, ?, 1.0, 1.0, 1.0, 0.0, 3600.0, ?)
        """,
        (sid, episode_id, _now(), offset, 0.0, 0.0, f"composition:{entry_id}"),
    )
    return sid


def upsert_entry_spatial(
    conn: sqlite3.Connection,
    entry_id: str,
    *,
    bounds: dict[str, Any] | None = None,
    timing: dict[str, Any] | None = None,
    spatial_id: str | None = None,
    episode_id: str | None = None,
) -> str | None:
    """Create or update a spatial_4d row for a lemma entry. Returns spatial id or None."""
    bounds = bounds or {}
    timing = timing or {}
    if not bounds and not timing and not spatial_id:
        return spatial_id

    cx = float(bounds.get("centerX", bounds.get("center_x", 0)))
    cy = float(bounds.get("centerY", bounds.get("center_y", 0)))
    cz = float(bounds.get("centerZ", bounds.get("center_z", 0)))
    sx = float(bounds.get("sizeX", bounds.get("size_x", 1)))
    sy = float(bounds.get("sizeY", bounds.get("size_y", 1)))
    sz = float(bounds.get("sizeZ", bounds.get("size_z", 1)))
    t_min = float(timing.get("tMin", timing.get("t_min", 0)))
    t_max = float(timing.get("tMax", timing.get("t_max", 3600)))

    sid = spatial_id
    if sid:
        row = _load_spatial(conn, sid)
        if row:
            conn.execute(
                """
                UPDATE spatial_4d
                SET center_x = ?, center_y = ?, center_z = ?,
                    size_x = ?, size_y = ?, size_z = ?,
                    t_min = ?, t_max = ?,
                    episode_id = COALESCE(?, episode_id)
                WHERE id = ?
                """,
                (cx, cy, cz, sx, sy, sz, t_min, t_max, episode_id, sid),
            )
            return sid

    sid = str(uuid.uuid4())
    conn.execute(
        """
        INSERT INTO spatial_4d
            (id, tenant_id, episode_id, created_at,
             center_x, center_y, center_z, size_x, size_y, size_z, t_min, t_max, payload_label)
        VALUES (?, 'default', ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (sid, episode_id, _now(), cx, cy, cz, sx, sy, sz, t_min, t_max, f"lemma:{entry_id}"),
    )
    return sid


def _load_clause_bindings(conn: sqlite3.Connection, entry_id: str, draft_episode_id: str | None) -> list[dict]:
    where = ["entry_id = ?"]
    params: list[Any] = [entry_id]
    if draft_episode_id:
        where.append(
            "(draft_script_id IN (SELECT id FROM draft_episode_script WHERE draft_episode_id = ?) OR draft_script_id IS NULL)"
        )
        params.append(draft_episode_id)
    cur = conn.execute(
        f"SELECT * FROM localization_clause_bindings WHERE {' AND '.join(where)}",
        params,
    )
    return [dict(r) for r in cur.fetchall()]


def audit_composition_spatial(
    conn: sqlite3.Connection,
    parent_entry_id: str,
    *,
    script_text: str = "",
    draft_episode_id: str | None = None,
) -> list[dict[str, Any]]:
    issues: list[dict[str, Any]] = []
    comp = load_composition(conn, parent_entry_id)
    script = script_text or ""

    def add_issue(
        code: str,
        entry_id: str,
        message: str,
        *,
        severity: str = "warning",
        proposed_fix: dict | None = None,
        requires_ack: bool = False,
        stored_text: str | None = None,
        current_text: str | None = None,
        composition_row_id: str | None = None,
    ) -> None:
        issues.append(
            {
                "id": str(uuid.uuid4()),
                "code": code,
                "severity": severity,
                "entryId": entry_id,
                "compositionRowId": composition_row_id,
                "message": message,
                "proposedFix": proposed_fix or {},
                "requiresAck": requires_ack,
                "storedText": stored_text,
                "currentText": current_text,
            }
        )

    parent_spatial_ids: list[str] = []
    for child in comp.get("children") or []:
        row_id = child.get("id")
        child_id = child.get("entryId")
        anchor = child.get("anchorText") or ""
        farey = child.get("anchorFarey")
        ft = _farey_tuple_from_json(farey) if farey else None

        if ft and script:
            cs, ce = farey_to_char(script, *ft)
            current_slice = script[cs:ce]
            if anchor and anchor != current_slice:
                add_issue(
                    "anchor_drift",
                    child_id,
                    f"Anchor text drift for child lemma {child.get('term') or child_id}",
                    severity="error",
                    requires_ack=True,
                    stored_text=anchor,
                    current_text=current_slice,
                    composition_row_id=row_id,
                    proposed_fix={"action": "update_anchor", "compositionRowId": row_id, "charStart": cs, "charEnd": ce},
                )
        elif not anchor and script and child.get("term"):
            term = child.get("term") or ""
            idx = script.lower().find(term.lower())
            if idx >= 0:
                add_issue(
                    "missing_anchor",
                    child_id,
                    f"No anchor stored for child {term}; can bind to first script occurrence",
                    severity="info",
                    requires_ack=True,
                    proposed_fix={
                        "action": "set_anchor",
                        "compositionRowId": row_id,
                        "charStart": idx,
                        "charEnd": idx + len(term),
                    },
                )

        sid = child.get("spatial4dId")
        if not sid:
            add_issue(
                "missing_spatial",
                child_id,
                f"Child {child.get('term') or child_id} has no spatial_4d row",
                severity="warning",
                requires_ack=False,
                composition_row_id=row_id,
                proposed_fix={"action": "create_spatial", "compositionRowId": row_id, "childEntryId": child_id},
            )
        else:
            parent_spatial_ids.append(sid)

    parent_spatial = None
    for sid in parent_spatial_ids[:1]:
        parent_spatial = _load_spatial(conn, sid)

    for child in comp.get("children") or []:
        child_sid = child.get("spatial4dId")
        if not child_sid or not parent_spatial:
            continue
        child_row = _load_spatial(conn, child_sid)
        if not child_row:
            continue
        outer = _spatial_aabb(parent_spatial)
        inner = _spatial_aabb(child_row)
        if not _aabb_contains(outer, inner):
            add_issue(
                "spatial_not_contained",
                child.get("entryId"),
                f"Child spatial volume is not contained in parent composition bounds",
                severity="warning",
                requires_ack=True,
                composition_row_id=child.get("id"),
                proposed_fix={"action": "expand_parent_spatial", "parentSpatialId": parent_spatial["id"]},
            )

        child_comp = load_composition(conn, child.get("entryId"))
        for grandchild in child_comp.get("children") or []:
            gc_sid = grandchild.get("spatial4dId")
            if not gc_sid:
                continue
            gc_row = _load_spatial(conn, gc_sid)
            if gc_row and not _aabb_contains(inner, _spatial_aabb(gc_row)):
                add_issue(
                    "spatial_not_contained",
                    grandchild.get("entryId"),
                    f"Grandchild spatial not contained in child {child.get('term')}",
                    severity="warning",
                    requires_ack=True,
                    composition_row_id=grandchild.get("id"),
                    proposed_fix={"action": "expand_child_spatial", "childSpatialId": child_sid},
                )

    bindings = _load_clause_bindings(conn, parent_entry_id, draft_episode_id)
    for b in bindings:
        ft = (
            int(b.get("farey_left_num") or 0),
            int(b.get("farey_left_den") or 1),
            int(b.get("farey_right_num") or 1),
            int(b.get("farey_right_den") or 1),
        )
        cs, ce = resolve_binding_char_span(b, script)
        if script and ce > cs:
            char_ft = (
                int(cs),
                max(len(script), 1),
                int(ce),
                max(len(script), 1),
            )
            if not farey_contains(ft, char_ft):
                add_issue(
                    "clause_misaligned",
                    parent_entry_id,
                    f"Clause binding Farey interval no longer contains char range [{cs}, {ce})",
                    severity="error",
                    requires_ack=True,
                    proposed_fix={"action": "realign_clause", "bindingId": b.get("id"), "charStart": cs, "charEnd": ce},
                )

    return issues


def repair_composition_spatial(
    conn: sqlite3.Connection,
    parent_entry_id: str,
    *,
    script_text: str = "",
    draft_episode_id: str | None = None,
    acknowledged_issue_ids: list[str],
) -> dict[str, Any]:
    issues = audit_composition_spatial(
        conn,
        parent_entry_id,
        script_text=script_text,
        draft_episode_id=draft_episode_id,
    )
    ack_set = set(acknowledged_issue_ids or [])
    applied: list[str] = []
    script = script_text or ""

    episode_id = None
    if draft_episode_id:
        cur = conn.execute("SELECT episode_id FROM draft_episodes WHERE id = ?", (draft_episode_id,))
        row = cur.fetchone()
        if row:
            episode_id = row["episode_id"]

    for issue in issues:
        iid = issue["id"]
        fix = issue.get("proposedFix") or {}
        action = fix.get("action")
        requires_ack = bool(issue.get("requiresAck"))
        if requires_ack and iid not in ack_set:
            continue
        if not requires_ack or iid in ack_set:
            pass
        else:
            continue

        if action == "create_spatial":
            row_id = fix.get("compositionRowId")
            cur = conn.execute(
                "SELECT sort_order, child_entry_id FROM thesaurus_entry_compositions WHERE id = ?",
                (row_id,),
            )
            comp_row = cur.fetchone()
            if comp_row:
                sid = _create_default_spatial(conn, comp_row["child_entry_id"], int(comp_row["sort_order"]), episode_id)
                conn.execute(
                    "UPDATE thesaurus_entry_compositions SET spatial_4d_id = ? WHERE id = ?",
                    (sid, row_id),
                )
                applied.append(iid)

        elif action in ("update_anchor", "set_anchor"):
            row_id = fix.get("compositionRowId")
            cs = int(fix.get("charStart") or 0)
            ce = int(fix.get("charEnd") or cs)
            slice_text = script[cs:ce] if script else ""
            n = max(len(script), 1)
            farey_json = _farey_to_json((cs, n, ce, n))
            conn.execute(
                """
                UPDATE thesaurus_entry_compositions
                SET anchor_text = ?, anchor_farey_json = ?, draft_episode_id = COALESCE(?, draft_episode_id)
                WHERE id = ?
                """,
                (slice_text, farey_json, draft_episode_id, row_id),
            )
            applied.append(iid)

        elif action == "realign_clause":
            binding_id = fix.get("bindingId")
            cs = int(fix.get("charStart") or 0)
            ce = int(fix.get("charEnd") or cs)
            n = max(len(script), 1)
            conn.execute(
                """
                UPDATE localization_clause_bindings
                SET char_start = ?, char_end = ?,
                    farey_left_num = ?, farey_left_den = ?,
                    farey_right_num = ?, farey_right_den = ?,
                    updated_at = ?
                WHERE id = ?
                """,
                (cs, ce, cs, n, ce, n, _now(), binding_id),
            )
            applied.append(iid)

        elif action in ("expand_parent_spatial", "expand_child_spatial"):
            if iid in ack_set:
                applied.append(iid)

    remaining = audit_composition_spatial(
        conn,
        parent_entry_id,
        script_text=script_text,
        draft_episode_id=draft_episode_id,
    )
    return {
        "parentEntryId": parent_entry_id,
        "appliedIssueIds": applied,
        "issues": remaining,
        "composition": load_composition(conn, parent_entry_id),
    }


def recombobulate_spatial(
    conn: sqlite3.Connection,
    parent_entry_id: str,
    body: dict[str, Any],
) -> dict[str, Any]:
    script_text = body.get("scriptText") or body.get("script_text") or ""
    draft_episode_id = body.get("draftEpisodeId") or body.get("draft_episode_id")
    ack_ids = body.get("acknowledgedIssueIds") or body.get("acknowledged_issue_ids") or []
    apply = bool(body.get("apply") or ack_ids)

    if apply:
        return repair_composition_spatial(
            conn,
            parent_entry_id,
            script_text=script_text,
            draft_episode_id=draft_episode_id,
            acknowledged_issue_ids=list(ack_ids),
        )

    issues = audit_composition_spatial(
        conn,
        parent_entry_id,
        script_text=script_text,
        draft_episode_id=draft_episode_id,
    )
    return {
        "parentEntryId": parent_entry_id,
        "issues": issues,
        "composition": load_composition(conn, parent_entry_id),
    }
