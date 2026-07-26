"""Session-scoped fuzzy variable cache for discourse grades / events."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

try:
    from continuuuum_api.nsm_logical_form import apply_curve
    from continuuuum_api.nsm_wiring_db import ensure_nsm_schema
except ImportError:
    from nsm_logical_form import apply_curve
    from nsm_wiring_db import ensure_nsm_schema


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id() -> str:
    return f"nfv_{uuid.uuid4().hex[:12]}"


def list_vars(
    conn: sqlite3.Connection, session_id: str, language_code: str = "en"
) -> list[dict[str, Any]]:
    ensure_nsm_schema(conn)
    cur = conn.execute(
        """SELECT id, session_id, language_code, var_key, var_kind, grade,
                  payload_json, source_span, updated_at
           FROM nsm_fuzzy_variable_cache
           WHERE session_id = ? AND language_code = ?
           ORDER BY var_key""",
        (session_id, language_code),
    )
    out = []
    for r in cur.fetchall():
        d = dict(r)
        if d.get("payload_json"):
            try:
                d["payload"] = json.loads(d["payload_json"])
            except json.JSONDecodeError:
                d["payload"] = None
        out.append(d)
    return out


def get_var(
    conn: sqlite3.Connection,
    session_id: str,
    var_key: str,
    language_code: str = "en",
) -> dict[str, Any] | None:
    ensure_nsm_schema(conn)
    cur = conn.execute(
        """SELECT id, session_id, language_code, var_key, var_kind, grade,
                  payload_json, source_span, updated_at
           FROM nsm_fuzzy_variable_cache
           WHERE session_id = ? AND language_code = ? AND var_key = ?
           LIMIT 1""",
        (session_id, language_code, var_key),
    ).fetchone()
    if not cur:
        return None
    d = dict(cur)
    if d.get("payload_json"):
        try:
            d["payload"] = json.loads(d["payload_json"])
        except json.JSONDecodeError:
            d["payload"] = None
    return d


def set_var(
    conn: sqlite3.Connection,
    session_id: str,
    var_key: str,
    var_kind: str,
    grade: float | None = None,
    payload: dict[str, Any] | None = None,
    source_span: str | None = None,
    language_code: str = "en",
) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    now = _now()
    existing = get_var(conn, session_id, var_key, language_code)
    payload_json = json.dumps(payload) if payload is not None else None
    g = None if grade is None else max(0.0, min(1.0, float(grade)))
    if existing:
        conn.execute(
            """UPDATE nsm_fuzzy_variable_cache
               SET var_kind = ?, grade = ?, payload_json = COALESCE(?, payload_json),
                   source_span = COALESCE(?, source_span), updated_at = ?
               WHERE id = ?""",
            (var_kind, g if g is not None else existing.get("grade"), payload_json, source_span, now, existing["id"]),
        )
        conn.commit()
        return get_var(conn, session_id, var_key, language_code) or existing
    rid = _new_id()
    conn.execute(
        """INSERT INTO nsm_fuzzy_variable_cache (
            id, session_id, language_code, var_key, var_kind, grade,
            payload_json, source_span, updated_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (rid, session_id, language_code, var_key, var_kind, g, payload_json, source_span, now),
    )
    conn.commit()
    return get_var(conn, session_id, var_key, language_code) or {
        "id": rid,
        "var_key": var_key,
        "grade": g,
    }


def replace_vars(
    conn: sqlite3.Connection,
    session_id: str,
    vars_list: list[dict[str, Any]],
    language_code: str = "en",
) -> list[dict[str, Any]]:
    ensure_nsm_schema(conn)
    conn.execute(
        "DELETE FROM nsm_fuzzy_variable_cache WHERE session_id = ? AND language_code = ?",
        (session_id, language_code),
    )
    conn.commit()
    out = []
    for v in vars_list or []:
        out.append(
            set_var(
                conn,
                session_id,
                str(v.get("var_key") or v.get("varKey") or ""),
                str(v.get("var_kind") or v.get("varKind") or "predicate"),
                v.get("grade"),
                v.get("payload"),
                v.get("source_span") or v.get("sourceSpan"),
                language_code,
            )
        )
    return out


def clear_session(conn: sqlite3.Connection, session_id: str, language_code: str | None = None) -> int:
    ensure_nsm_schema(conn)
    if language_code:
        cur = conn.execute(
            "DELETE FROM nsm_fuzzy_variable_cache WHERE session_id = ? AND language_code = ?",
            (session_id, language_code),
        )
    else:
        cur = conn.execute(
            "DELETE FROM nsm_fuzzy_variable_cache WHERE session_id = ?",
            (session_id,),
        )
    conn.commit()
    return cur.rowcount


def adjust_grade(
    conn: sqlite3.Connection,
    session_id: str,
    var_key: str,
    *,
    delta: float | None = None,
    hedge_id: str | None = None,
    curve: dict[str, Any] | None = None,
    language_code: str = "en",
    create_kind: str = "predicate",
) -> dict[str, Any]:
    row = get_var(conn, session_id, var_key, language_code)
    base = float(row["grade"]) if row and row.get("grade") is not None else 0.5
    if hedge_id or curve:
        g = apply_curve(curve, base)
        if hedge_id == "less" and curve is None:
            g = apply_curve({"kind": "power", "p": 1.5, "yScale": 0.7, "clamp": True}, base)
    elif delta is not None:
        g = max(0.0, min(1.0, base + float(delta)))
    else:
        g = base
    return set_var(
        conn,
        session_id,
        var_key,
        (row or {}).get("var_kind") or create_kind,
        g,
        (row or {}).get("payload") if isinstance((row or {}).get("payload"), dict) else None,
        language_code=language_code,
    )


def remember_event(
    conn: sqlite3.Connection,
    session_id: str,
    event_key: str,
    payload: dict[str, Any] | None = None,
    grade: float | None = 1.0,
    language_code: str = "en",
    source_span: str | None = None,
) -> dict[str, Any]:
    key = event_key if event_key.startswith("event:") else f"event:{event_key}"
    pl = dict(payload or {})
    pl.setdefault("remembered_at", _now())
    # Keep prior snapshot under prior: key for "before" lookups
    prior = get_var(conn, session_id, key, language_code)
    if prior:
        set_var(
            conn,
            session_id,
            f"prior:{key}",
            "similarity_anchor",
            prior.get("grade"),
            prior.get("payload") if isinstance(prior.get("payload"), dict) else {"raw": prior.get("payload_json")},
            prior.get("source_span"),
            language_code,
        )
    return set_var(conn, session_id, key, "event", grade, pl, source_span, language_code)


def find_prior_similar(
    conn: sqlite3.Connection,
    session_id: str,
    event_key: str,
    language_code: str = "en",
) -> dict[str, Any] | None:
    key = event_key if event_key.startswith("event:") else f"event:{event_key}"
    prior = get_var(conn, session_id, f"prior:{key}", language_code)
    if prior:
        return prior
    # Fallback: any prior:* with same suffix
    for v in list_vars(conn, session_id, language_code):
        if str(v.get("var_key") or "").startswith("prior:") and key in str(v.get("var_key")):
            return v
    return None


def upsert_vars_batch(
    conn: sqlite3.Connection,
    session_id: str,
    upserts: list[dict[str, Any]],
    language_code: str = "en",
) -> list[dict[str, Any]]:
    out = []
    for u in upserts or []:
        kind = str(u.get("var_kind") or u.get("varKind") or "predicate")
        key = str(u.get("var_key") or u.get("varKey") or "")
        if not key:
            continue
        if kind == "event" or key.startswith("event:"):
            out.append(
                remember_event(
                    conn,
                    session_id,
                    key,
                    u.get("payload"),
                    u.get("grade", 1.0),
                    language_code,
                    u.get("source_span") or u.get("sourceSpan"),
                )
            )
        elif u.get("hedgeId") or u.get("hedge_id") or u.get("delta") is not None:
            curve = u.get("curve")
            out.append(
                adjust_grade(
                    conn,
                    session_id,
                    key,
                    delta=u.get("delta"),
                    hedge_id=u.get("hedgeId") or u.get("hedge_id"),
                    curve=curve,
                    language_code=language_code,
                    create_kind=kind,
                )
            )
        else:
            out.append(
                set_var(
                    conn,
                    session_id,
                    key,
                    kind,
                    u.get("grade"),
                    u.get("payload"),
                    u.get("source_span") or u.get("sourceSpan"),
                    language_code,
                )
            )
    return out


def env_from_session(
    conn: sqlite3.Connection, session_id: str, language_code: str = "en"
) -> dict[str, Any]:
    env: dict[str, Any] = {}
    for v in list_vars(conn, session_id, language_code):
        key = str(v.get("var_key") or "")
        if v.get("grade") is not None:
            env[key] = float(v["grade"])
            short = key.split(":", 1)[-1]
            env[f"grade:{short}"] = float(v["grade"])
            env[short] = float(v["grade"])
    return env
