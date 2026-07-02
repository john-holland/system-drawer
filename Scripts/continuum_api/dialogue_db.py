"""Dialogue database helpers and session state."""

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


def ensure_dialogue_schema(conn: sqlite3.Connection) -> None:
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
    if not _table_exists(conn, "dialogue_sets"):
        sql = (_SCHEMA_ROOT / "continuum_dialogue_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    conn.commit()


def save_compiled_set(
    conn: sqlite3.Connection,
    *,
    set_id: str,
    lemma_entry_id: str | None,
    name: str,
    compiled: dict[str, Any],
) -> dict[str, Any]:
    ensure_dialogue_schema(conn)
    now = _now()
    cur = conn.execute("SELECT version FROM dialogue_sets WHERE id = ?", (set_id,))
    row = cur.fetchone()
    version = (int(row[0]) + 1) if row else 1
    conn.execute(
        """
        INSERT INTO dialogue_sets (id, lemma_entry_id, name, compiled_json, version, updated_at)
        VALUES (?, ?, ?, ?, ?, ?)
        ON CONFLICT(id) DO UPDATE SET
            lemma_entry_id=excluded.lemma_entry_id,
            name=excluded.name,
            compiled_json=excluded.compiled_json,
            version=excluded.version,
            updated_at=excluded.updated_at
        """,
        (set_id, lemma_entry_id, name, json.dumps(compiled), version, now),
    )
    return {"setId": set_id, "version": version, "updatedAt": now}


def load_compiled_set(conn: sqlite3.Connection, set_id: str) -> dict[str, Any] | None:
    ensure_dialogue_schema(conn)
    cur = conn.execute(
        "SELECT compiled_json, version FROM dialogue_sets WHERE id = ?",
        (set_id,),
    )
    row = cur.fetchone()
    if not row:
        return None
    data = json.loads(row[0])
    data["version"] = row[1]
    return data


def create_session(
    conn: sqlite3.Connection,
    *,
    set_id: str,
    tenant: str,
    user_id: str | None,
    trace_id: str | None,
) -> dict[str, Any]:
    ensure_dialogue_schema(conn)
    compiled = load_compiled_set(conn, set_id)
    if not compiled:
        raise ValueError(f"Unknown dialogue set: {set_id}")

    session_id = str(uuid.uuid4())
    now = _now()
    state = {
        "currentSet": set_id,
        "currentNodeId": _first_node_id(compiled),
        "chosenAnswers": [],
        "goalFlags": {},
        "completions4d": [],
        "visited": [],
    }
    conn.execute(
        """
        INSERT INTO dialogue_sessions (id, set_id, tenant, user_id, state_json, trace_id, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (session_id, set_id, tenant, user_id, json.dumps(state), trace_id, now, now),
    )
    return _session_view(session_id, compiled, state)


def get_session(conn: sqlite3.Connection, session_id: str) -> tuple[dict[str, Any], dict[str, Any]] | None:
    ensure_dialogue_schema(conn)
    cur = conn.execute(
        "SELECT set_id, state_json FROM dialogue_sessions WHERE id = ?",
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
        "UPDATE dialogue_sessions SET state_json = ?, updated_at = ? WHERE id = ?",
        (json.dumps(state), _now(), session_id),
    )


def _first_node_id(compiled: dict[str, Any]) -> str | None:
    nodes = compiled.get("nodes") or []
    if not nodes:
        return None
    return nodes[0].get("id")


def _flatten_nodes(nodes: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    out: dict[str, dict[str, Any]] = {}

    def walk(items: list[dict[str, Any]]) -> None:
        for n in items:
            out[n["id"]] = n
            walk(n.get("children") or [])

    walk(nodes)
    return out


def _session_view(session_id: str, compiled: dict[str, Any], state: dict[str, Any]) -> dict[str, Any]:
    index = _flatten_nodes(compiled.get("nodes") or [])
    current_id = state.get("currentNodeId")
    current = index.get(current_id) if current_id else None
    visible = _visible_choices(compiled, state, index)
    return {
        "ok": True,
        "sessionId": session_id,
        "setId": compiled.get("setId"),
        "state": state,
        "currentNode": current,
        "choices": visible,
        "compiledVersion": compiled.get("version"),
    }


def _goal_satisfied(state: dict[str, Any], node: dict[str, Any]) -> bool:
    pred = node.get("predicate4d")
    if pred and not state.get("goalFlags", {}).get(pred):
        return False
    comp = node.get("completion4d")
    if comp and comp not in state.get("completions4d", []):
        return False
    return True


def _visible_choices(compiled: dict[str, Any], state: dict[str, Any], index: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    current_id = state.get("currentNodeId")
    if not current_id or current_id not in index:
        nodes = compiled.get("nodes") or []
        if not nodes:
            return []
        current_id = nodes[0]["id"]
        state["currentNodeId"] = current_id

    current = index[current_id]
    children = current.get("children") or []
    opts = current.get("options")
    choices: list[dict[str, Any]] = []
    for child in children:
        if not _goal_satisfied(state, child):
            continue
        aid = child.get("answerId")
        if opts and aid and aid not in opts:
            continue
        if aid:
            choices.append(
                {
                    "answerId": aid,
                    "text": child.get("text"),
                    "nodeId": child.get("id"),
                    "speakerKey": child.get("speakerKey"),
                    "presentation": child.get("presentation"),
                    "audioRef": child.get("audioRef"),
                    "visMode": child.get("visMode"),
                }
            )
    return choices


def advance_session(conn: sqlite3.Connection, session_id: str) -> dict[str, Any]:
    data = get_session(conn, session_id)
    if not data:
        raise ValueError("session_not_found")
    compiled, state = data
    index = _flatten_nodes(compiled.get("nodes") or [])
    current_id = state.get("currentNodeId")
    if not current_id:
        raise ValueError("no_current_node")
    current = index[current_id]
    nxt = _next_linear_node(compiled, current_id)
    if nxt:
        state["currentNodeId"] = nxt
        visited = state.setdefault("visited", [])
        if current_id not in visited:
            visited.append(current_id)
    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def choose_session(conn: sqlite3.Connection, session_id: str, answer_id: str) -> dict[str, Any]:
    data = get_session(conn, session_id)
    if not data:
        raise ValueError("session_not_found")
    compiled, state = data
    index = _flatten_nodes(compiled.get("nodes") or [])
    current_id = state.get("currentNodeId")
    if not current_id or current_id not in index:
        raise ValueError("no_current_node")

    current = index[current_id]
    target = None
    for child in current.get("children") or []:
        if child.get("answerId") == answer_id and _goal_satisfied(state, child):
            target = child
            break
    if not target:
        raise ValueError(f"invalid_answer:{answer_id}")

    state.setdefault("chosenAnswers", []).append(answer_id)
    state["currentNodeId"] = target["id"]
    goal = target.get("goal")
    if goal:
        state.setdefault("goalFlags", {})[goal] = True
    comp = target.get("completion4d")
    if comp:
        comps = state.setdefault("completions4d", [])
        if comp not in comps:
            comps.append(comp)
    jump = target.get("continueWithDialogue")
    if jump:
        state["currentSet"] = jump

    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def sync_goals(conn: sqlite3.Connection, session_id: str, goals: dict[str, bool]) -> dict[str, Any]:
    data = get_session(conn, session_id)
    if not data:
        raise ValueError("session_not_found")
    compiled, state = data
    flags = state.setdefault("goalFlags", {})
    for k, v in (goals or {}).items():
        flags[k] = bool(v)
    update_session_state(conn, session_id, state)
    return _session_view(session_id, compiled, state)


def _next_linear_node(compiled: dict[str, Any], current_id: str) -> str | None:
    nodes = compiled.get("nodes") or []
    flat: list[str] = []

    def walk(items: list[dict[str, Any]]) -> None:
        for n in items:
            flat.append(n["id"])
            walk(n.get("children") or [])

    walk(nodes)
    try:
        idx = flat.index(current_id)
    except ValueError:
        return None
    if idx + 1 < len(flat):
        return flat[idx + 1]
    return None
