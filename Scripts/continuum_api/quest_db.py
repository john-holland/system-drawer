"""Quest database helpers and session state."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_quest_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "localization_property_specs"):
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY,
                value_type TEXT NOT NULL,
                allowed_values_json TEXT,
                default_value TEXT,
                description TEXT
            )
            """
        )
    if not _table_exists(conn, "quest_sets"):
        sql = (_SCHEMA_ROOT / "continuum_quest_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    conn.commit()


def save_compiled_set(
    conn: sqlite3.Connection,
    *,
    set_id: str,
    lemma_entry_id: str | None,
    title: str,
    compiled: dict[str, Any],
    episode_id: str | None = None,
    root_spatial_4d_id: str | None = None,
    default_map_profile: dict[str, Any] | None = None,
) -> dict[str, Any]:
    ensure_quest_schema(conn)
    now = _now()
    cur = conn.execute("SELECT version FROM quest_sets WHERE id = ?", (set_id,))
    row = cur.fetchone()
    version = (int(row[0]) + 1) if row else 1
    conn.execute(
        """
        INSERT INTO quest_sets
            (id, episode_id, lemma_entry_id, title, root_spatial_4d_id, compiled_json,
             default_map_profile_json, version, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(id) DO UPDATE SET
            episode_id=excluded.episode_id,
            lemma_entry_id=excluded.lemma_entry_id,
            title=excluded.title,
            root_spatial_4d_id=excluded.root_spatial_4d_id,
            compiled_json=excluded.compiled_json,
            default_map_profile_json=excluded.default_map_profile_json,
            version=excluded.version,
            updated_at=excluded.updated_at
        """,
        (
            set_id,
            episode_id or compiled.get("episodeId"),
            lemma_entry_id,
            title or compiled.get("title") or set_id,
            root_spatial_4d_id or compiled.get("rootSpatial4dId"),
            json.dumps(compiled),
            json.dumps(default_map_profile or {}),
            version,
            now,
        ),
    )
    _sync_objectives_from_compiled(conn, set_id, compiled)
    return {"setId": set_id, "version": version, "updatedAt": now}


def _sync_objectives_from_compiled(conn: sqlite3.Connection, set_id: str, compiled: dict[str, Any]) -> None:
    conn.execute("DELETE FROM quest_objectives WHERE quest_set_id = ?", (set_id,))
    sort_order = 0

    def walk(nodes: list[dict[str, Any]], parent_objective: str | None) -> None:
        nonlocal sort_order
        for n in nodes:
            oid = n.get("objectiveId")
            if oid:
                row_id = str(uuid.uuid4())
                bounds = n.get("bounds4d") or n.get("bounds3d")
                conn.execute(
                    """
                    INSERT INTO quest_objectives
                        (id, quest_set_id, objective_id, parent_id, spatial_4d_id, bounds_json,
                         predicate4d, completion4d, sort_order, summary_text, pathing_json, behavior_trees_json)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        row_id,
                        set_id,
                        oid,
                        parent_objective,
                        n.get("spatial4dId"),
                        json.dumps(bounds) if bounds else None,
                        n.get("predicate4d"),
                        n.get("completion4d"),
                        sort_order,
                        n.get("summary") or n.get("text"),
                        json.dumps({"travelBinding": n.get("travelBinding")}) if n.get("travelBinding") else None,
                        json.dumps(
                            {
                                "uiBt": n.get("uiBt"),
                                "mapBt": n.get("mapBt"),
                                "animBt": n.get("animBt"),
                                "mapLayer": n.get("mapLayer"),
                            }
                        ),
                    ),
                )
                sort_order += 1
                walk(n.get("children") or [], oid)
            else:
                walk(n.get("children") or [], parent_objective)

    walk(compiled.get("nodes") or [], None)


def load_compiled_set(conn: sqlite3.Connection, set_id: str) -> dict[str, Any] | None:
    ensure_quest_schema(conn)
    cur = conn.execute(
        "SELECT compiled_json, version, title, root_spatial_4d_id FROM quest_sets WHERE id = ?",
        (set_id,),
    )
    row = cur.fetchone()
    if not row:
        return None
    data = json.loads(row[0])
    data["version"] = row[1]
    data["title"] = row[2]
    data["rootSpatial4dId"] = row[3]
    return data


def create_session(
    conn: sqlite3.Connection,
    *,
    set_id: str,
    tenant: str,
    user_id: str | None,
    trace_id: str | None,
) -> dict[str, Any]:
    ensure_quest_schema(conn)
    compiled = load_compiled_set(conn, set_id)
    if not compiled:
        raise ValueError(f"Unknown quest set: {set_id}")

    session_id = str(uuid.uuid4())
    now = _now()
    first_objective = _first_objective_id(compiled)
    state = {
        "questSetId": set_id,
        "activeObjectiveId": first_objective,
        "completedObjectiveIds": [],
        "goalFlags": {},
        "completions4d": [],
        "playerSpatialState": {},
        "visited": [],
    }
    conn.execute(
        """
        INSERT INTO quest_sessions
            (id, quest_set_id, tenant, user_id, active_objective_id, state_json, trace_id, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (session_id, set_id, tenant, user_id, first_objective, json.dumps(state), trace_id, now, now),
    )
    return _session_view(session_id, compiled, state)


def get_session(conn: sqlite3.Connection, session_id: str) -> tuple[dict[str, Any], dict[str, Any]] | None:
    ensure_quest_schema(conn)
    cur = conn.execute(
        "SELECT quest_set_id, state_json FROM quest_sessions WHERE id = ?",
        (session_id,),
    )
    row = cur.fetchone()
    if not row:
        return None
    compiled = load_compiled_set(conn, row[0])
    if not compiled:
        return None
    state = json.loads(row[1])
    return compiled, state


def update_session_state(conn: sqlite3.Connection, session_id: str, state: dict[str, Any]) -> None:
    conn.execute(
        """UPDATE quest_sessions SET state_json = ?, active_objective_id = ?, updated_at = ? WHERE id = ?""",
        (json.dumps(state), state.get("activeObjectiveId"), _now(), session_id),
    )


def _flatten_objectives(nodes: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    out: dict[str, dict[str, Any]] = {}

    def walk(items: list[dict[str, Any]]) -> None:
        for n in items:
            oid = n.get("objectiveId")
            if oid:
                out[oid] = n
            walk(n.get("children") or [])

    walk(nodes)
    return out


def _first_objective_id(compiled: dict[str, Any]) -> str | None:
    def walk(nodes: list[dict[str, Any]]) -> str | None:
        for n in nodes:
            oid = n.get("objectiveId")
            if oid:
                return oid
            found = walk(n.get("children") or [])
            if found:
                return found
        return None

    return walk(compiled.get("nodes") or [])


def _goal_satisfied(state: dict[str, Any], node: dict[str, Any]) -> bool:
    pred = node.get("predicate4d")
    if pred and not state.get("goalFlags", {}).get(pred):
        return False
    comp = node.get("completion4d")
    if comp and comp not in state.get("completions4d", []):
        return False
    return True


def _session_view(session_id: str, compiled: dict[str, Any], state: dict[str, Any]) -> dict[str, Any]:
    index = _flatten_objectives(compiled.get("nodes") or [])
    active_id = state.get("activeObjectiveId")
    active = index.get(active_id) if active_id else None
    pending = [
        {"objectiveId": oid, "summary": n.get("summary"), "spatial4dId": n.get("spatial4dId")}
        for oid, n in index.items()
        if oid not in state.get("completedObjectiveIds", []) and _goal_satisfied(state, n)
    ]
    return {
        "ok": True,
        "sessionId": session_id,
        "setId": compiled.get("setId"),
        "title": compiled.get("title"),
        "state": state,
        "activeObjective": active,
        "pendingObjectives": pending,
        "compiledVersion": compiled.get("version"),
    }


def activate_objective(conn: sqlite3.Connection, session_id: str, objective_id: str) -> dict[str, Any]:
    pair = get_session(conn, session_id)
    if not pair:
        raise ValueError("session_not_found")
    compiled, state = pair
    index = _flatten_objectives(compiled.get("nodes") or [])
    if objective_id not in index:
        raise ValueError("objective_not_found")
    node = index[objective_id]
    if not _goal_satisfied(state, node):
        raise ValueError("objective_locked")
    state["activeObjectiveId"] = objective_id
    visited = state.setdefault("visited", [])
    if objective_id not in visited:
        visited.append(objective_id)
    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def complete_objective(conn: sqlite3.Connection, session_id: str, objective_id: str) -> dict[str, Any]:
    pair = get_session(conn, session_id)
    if not pair:
        raise ValueError("session_not_found")
    compiled, state = pair
    index = _flatten_objectives(compiled.get("nodes") or [])
    if objective_id not in index:
        raise ValueError("objective_not_found")
    completed = state.setdefault("completedObjectiveIds", [])
    if objective_id not in completed:
        completed.append(objective_id)
    comp = index[objective_id].get("completion4d")
    if comp:
        comps = state.setdefault("completions4d", [])
        if comp not in comps:
            comps.append(comp)
    next_id = _next_objective(compiled, state, objective_id)
    state["activeObjectiveId"] = next_id
    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def _next_objective(compiled: dict[str, Any], state: dict[str, Any], after_id: str) -> str | None:
    index = _flatten_objectives(compiled.get("nodes") or [])
    completed = set(state.get("completedObjectiveIds") or [])
    for oid, node in index.items():
        if oid in completed:
            continue
        if _goal_satisfied(state, node):
            return oid
    return None


def sync_goals(conn: sqlite3.Connection, session_id: str, goals: dict[str, bool]) -> dict[str, Any]:
    pair = get_session(conn, session_id)
    if not pair:
        raise ValueError("session_not_found")
    compiled, state = pair
    flags = state.setdefault("goalFlags", {})
    for k, v in goals.items():
        flags[str(k)] = bool(v)
    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def save_summary(
    conn: sqlite3.Connection,
    *,
    objective_row_id: str,
    mode: str,
    text: str,
    style_profile: dict[str, Any] | None = None,
    suggestion_id: str | None = None,
) -> dict[str, Any]:
    ensure_quest_schema(conn)
    sid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """
        INSERT INTO quest_summaries (id, objective_id, mode, text, style_profile_json, suggestion_id, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?)
        """,
        (sid, objective_row_id, mode, text, json.dumps(style_profile or {}), suggestion_id, now),
    )
    return {"id": sid, "mode": mode, "text": text, "updatedAt": now}


def find_objective_row(conn: sqlite3.Connection, set_id: str, objective_id: str) -> sqlite3.Row | None:
    ensure_quest_schema(conn)
    return conn.execute(
        "SELECT * FROM quest_objectives WHERE quest_set_id = ? AND objective_id = ?",
        (set_id, objective_id),
    ).fetchone()
