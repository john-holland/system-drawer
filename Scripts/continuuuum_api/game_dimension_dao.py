"""DAO for games, dimensions, visibility, associations, dim property overrides, warm snapshots."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Optional

try:
    from continuuuum_api.game_dimension_db import ensure_game_dimension_schema
except ImportError:
    from game_dimension_db import ensure_game_dimension_schema

ASSOCIABLE_TABLES = frozenset({"thesaurus_entries", "localization_property_specs"})


def _utcnow() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _ensure(conn: sqlite3.Connection) -> None:
    ensure_game_dimension_schema(conn)


def _row_game(r: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": r["id"],
        "slug": r["slug"],
        "displayName": r["display_name"],
        "active": bool(r["active"]),
        "isPublic": bool(r["is_public"]),
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def _row_dim(r: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": r["id"],
        "dimIndex": int(r["dim_index"]),
        "slug": r["slug"],
        "displayName": r["display_name"],
        "isPublic": bool(r["is_public"]),
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def get_game_by_slug(conn: sqlite3.Connection, slug: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM games WHERE slug = ?", (slug,)).fetchone()
    return _row_game(r) if r else None


def get_game_by_id(conn: sqlite3.Connection, game_id: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM games WHERE id = ?", (game_id,)).fetchone()
    return _row_game(r) if r else None


def get_dimension_by_index(conn: sqlite3.Connection, dim_index: int) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM dimensions WHERE dim_index = ?", (int(dim_index),)).fetchone()
    return _row_dim(r) if r else None


def get_dimension_by_slug(conn: sqlite3.Connection, slug: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM dimensions WHERE slug = ?", (slug,)).fetchone()
    return _row_dim(r) if r else None


def get_dimension_by_id(conn: sqlite3.Connection, dimension_id: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM dimensions WHERE id = ?", (dimension_id,)).fetchone()
    return _row_dim(r) if r else None


def resolve_dimension_ref(conn: sqlite3.Connection, ref: Any) -> Optional[dict[str, Any]]:
    """Accept int dim_index, numeric string, slug, or id."""
    if ref is None or ref == "":
        return get_dimension_by_index(conn, 0)
    if isinstance(ref, int):
        return get_dimension_by_index(conn, ref)
    s = str(ref).strip()
    if s.isdigit() or (s.startswith("-") and s[1:].isdigit()):
        return get_dimension_by_index(conn, int(s))
    by_slug = get_dimension_by_slug(conn, s)
    if by_slug:
        return by_slug
    return get_dimension_by_id(conn, s)


def resolve_game_ref(conn: sqlite3.Connection, ref: Any) -> Optional[dict[str, Any]]:
    if ref is None or ref == "":
        return get_game_by_slug(conn, "main")
    s = str(ref).strip()
    by_slug = get_game_by_slug(conn, s)
    if by_slug:
        return by_slug
    return get_game_by_id(conn, s)


def _has_grant(conn: sqlite3.Connection, subject_kind: str, subject_id: str, user_id: str) -> bool:
    r = conn.execute(
        """
        SELECT 1 FROM gd_visibility_grants
        WHERE subject_kind = ? AND subject_id = ? AND user_id = ?
        """,
        (subject_kind, subject_id, user_id),
    ).fetchone()
    return r is not None


def is_game_visible(conn: sqlite3.Connection, game: dict[str, Any], user_id: str, is_admin: bool) -> bool:
    if is_admin:
        return True
    if game.get("isPublic"):
        return True
    return _has_grant(conn, "game", game["id"], user_id)


def is_dimension_visible(
    conn: sqlite3.Connection, dim: dict[str, Any], user_id: str, is_admin: bool
) -> bool:
    if is_admin:
        return True
    if int(dim.get("dimIndex", -1)) == 0:
        return True
    if dim.get("isPublic"):
        return True
    return _has_grant(conn, "dimension", dim["id"], user_id)


def assert_game_visible(
    conn: sqlite3.Connection, game: dict[str, Any], user_id: str, is_admin: bool
) -> Optional[str]:
    if is_game_visible(conn, game, user_id, is_admin):
        return None
    return "GAME_NOT_VISIBLE"


def assert_dimension_visible(
    conn: sqlite3.Connection, dim: dict[str, Any], user_id: str, is_admin: bool
) -> Optional[str]:
    if is_dimension_visible(conn, dim, user_id, is_admin):
        return None
    return "DIMENSION_NOT_VISIBLE"


def list_games_for_user(conn: sqlite3.Connection, user_id: str, is_admin: bool) -> list[dict[str, Any]]:
    _ensure(conn)
    rows = conn.execute("SELECT * FROM games WHERE active = 1 ORDER BY slug").fetchall()
    out = []
    for r in rows:
        g = _row_game(r)
        if is_game_visible(conn, g, user_id, is_admin):
            out.append(g)
    return out


def list_dimensions_for_user(
    conn: sqlite3.Connection, user_id: str, is_admin: bool
) -> list[dict[str, Any]]:
    _ensure(conn)
    rows = conn.execute("SELECT * FROM dimensions ORDER BY dim_index").fetchall()
    out = []
    for r in rows:
        d = _row_dim(r)
        if is_dimension_visible(conn, d, user_id, is_admin):
            out.append(d)
    return out


def create_game(
    conn: sqlite3.Connection, slug: str, display_name: str, is_public: bool = False
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    gid = str(uuid.uuid4())
    conn.execute(
        """
        INSERT INTO games (id, slug, display_name, active, is_public, created_at, updated_at)
        VALUES (?, ?, ?, 1, ?, ?, ?)
        """,
        (gid, slug, display_name, 1 if is_public else 0, now, now),
    )
    conn.commit()
    return get_game_by_id(conn, gid)  # type: ignore[return-value]


def create_dimension(
    conn: sqlite3.Connection,
    dim_index: int,
    display_name: str,
    slug: Optional[str] = None,
    is_public: bool = False,
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    did = str(uuid.uuid4())
    conn.execute(
        """
        INSERT INTO dimensions (id, dim_index, slug, display_name, is_public, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?)
        """,
        (did, int(dim_index), slug or f"dim-{dim_index}", display_name, 1 if is_public else 0, now, now),
    )
    conn.commit()
    return get_dimension_by_id(conn, did)  # type: ignore[return-value]


def get_user_context(conn: sqlite3.Connection, user_id: str) -> dict[str, Any]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM user_context WHERE user_id = ?", (user_id,)).fetchone()
    game = get_game_by_slug(conn, "main")
    dim = get_dimension_by_index(conn, 0)
    if r:
        if r["game_id"]:
            g = get_game_by_id(conn, r["game_id"])
            if g:
                game = g
        if r["dimension_id"]:
            d = get_dimension_by_id(conn, r["dimension_id"])
            if d:
                dim = d
    return {
        "userId": user_id,
        "game": game,
        "dimension": dim,
        "gameSlug": game["slug"] if game else "main",
        "dimIndex": dim["dimIndex"] if dim else 0,
    }


def set_user_context(
    conn: sqlite3.Connection,
    user_id: str,
    game_id: Optional[str] = None,
    dimension_id: Optional[str] = None,
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    existing = conn.execute("SELECT * FROM user_context WHERE user_id = ?", (user_id,)).fetchone()
    cur_game = existing["game_id"] if existing else None
    cur_dim = existing["dimension_id"] if existing else None
    if game_id is not None:
        cur_game = game_id
    if dimension_id is not None:
        cur_dim = dimension_id
    if not cur_game:
        g = get_game_by_slug(conn, "main")
        cur_game = g["id"] if g else None
    if not cur_dim:
        d = get_dimension_by_index(conn, 0)
        cur_dim = d["id"] if d else None
    conn.execute(
        """
        INSERT INTO user_context (user_id, game_id, dimension_id, updated_at)
        VALUES (?, ?, ?, ?)
        ON CONFLICT(user_id) DO UPDATE SET
          game_id = excluded.game_id,
          dimension_id = excluded.dimension_id,
          updated_at = excluded.updated_at
        """,
        (user_id, cur_game, cur_dim, now),
    )
    conn.commit()
    return get_user_context(conn, user_id)


def list_visibility_matrix(conn: sqlite3.Connection) -> dict[str, Any]:
    _ensure(conn)
    games = [_row_game(r) for r in conn.execute("SELECT * FROM games ORDER BY slug").fetchall()]
    dims = [_row_dim(r) for r in conn.execute("SELECT * FROM dimensions ORDER BY dim_index").fetchall()]
    grants = conn.execute(
        "SELECT subject_kind, subject_id, user_id FROM gd_visibility_grants"
    ).fetchall()
    by_subj: dict[tuple[str, str], list[str]] = {}
    for g in grants:
        key = (g["subject_kind"], g["subject_id"])
        by_subj.setdefault(key, []).append(g["user_id"])
    return {
        "games": [
            {**g, "grantedUserIds": by_subj.get(("game", g["id"]), [])} for g in games
        ],
        "dimensions": [
            {**d, "grantedUserIds": by_subj.get(("dimension", d["id"]), [])} for d in dims
        ],
    }


def put_visibility(
    conn: sqlite3.Connection,
    subject_kind: str,
    subject_id: str,
    *,
    is_public: Optional[bool] = None,
    grant_user_ids: Optional[list[str]] = None,
    granted_by: Optional[str] = None,
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    kind = subject_kind.strip().lower()
    if kind not in ("game", "dimension"):
        raise ValueError("subject_kind must be game or dimension")
    if kind == "game":
        if is_public is not None:
            conn.execute(
                "UPDATE games SET is_public = ?, updated_at = ? WHERE id = ?",
                (1 if is_public else 0, now, subject_id),
            )
    else:
        if is_public is not None:
            # dim 0 stays public
            dim = get_dimension_by_id(conn, subject_id)
            if dim and int(dim["dimIndex"]) == 0:
                is_public = True
            conn.execute(
                "UPDATE dimensions SET is_public = ?, updated_at = ? WHERE id = ?",
                (1 if is_public else 0, now, subject_id),
            )
    if grant_user_ids is not None:
        conn.execute(
            "DELETE FROM gd_visibility_grants WHERE subject_kind = ? AND subject_id = ?",
            (kind, subject_id),
        )
        for uid in grant_user_ids:
            uid = str(uid).strip()
            if not uid:
                continue
            conn.execute(
                """
                INSERT INTO gd_visibility_grants (id, subject_kind, subject_id, user_id, granted_by, created_at)
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (str(uuid.uuid4()), kind, subject_id, uid, granted_by, now),
            )
    conn.commit()
    return list_visibility_matrix(conn)


def list_associations(
    conn: sqlite3.Connection,
    table_name: Optional[str] = None,
    entity_id: Optional[str] = None,
) -> list[dict[str, Any]]:
    _ensure(conn)
    sql = "SELECT * FROM entity_gd_assoc WHERE 1=1"
    args: list[Any] = []
    if table_name:
        sql += " AND table_name = ?"
        args.append(table_name)
    if entity_id:
        sql += " AND entity_id = ?"
        args.append(entity_id)
    sql += " ORDER BY table_name, entity_id"
    rows = conn.execute(sql, args).fetchall()
    return [
        {
            "id": r["id"],
            "tableName": r["table_name"],
            "entityId": r["entity_id"],
            "gameId": r["game_id"],
            "dimensionId": r["dimension_id"],
            "createdAt": r["created_at"],
        }
        for r in rows
    ]


def upsert_associations(conn: sqlite3.Connection, rows: list[dict[str, Any]]) -> int:
    """Replace associations for each (table, entity) group present in rows."""
    _ensure(conn)
    now = _utcnow()
    groups: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for row in rows:
        key = (row["tableName"], row["entityId"])
        groups.setdefault(key, []).append(row)
    n = 0
    for (table, eid), items in groups.items():
        conn.execute(
            "DELETE FROM entity_gd_assoc WHERE table_name = ? AND entity_id = ?",
            (table, eid),
        )
        for item in items:
            conn.execute(
                """
                INSERT INTO entity_gd_assoc (id, table_name, entity_id, game_id, dimension_id, created_at)
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (
                    str(uuid.uuid4()),
                    table,
                    eid,
                    item["gameId"],
                    item["dimensionId"],
                    now,
                ),
            )
            n += 1
    conn.commit()
    return n


def put_association_matrix(conn: sqlite3.Connection, matrix: list[dict[str, Any]]) -> int:
    """
    matrix items: { tableName, entityId, gameIds: [], dimensionIds: [] }
    Writes cartesian product of games × dimensions for each entity.
    """
    _ensure(conn)
    rows: list[dict[str, Any]] = []
    for m in matrix:
        games = m.get("gameIds") or []
        dims = m.get("dimensionIds") or []
        if not games or not dims:
            # clear entity
            conn.execute(
                "DELETE FROM entity_gd_assoc WHERE table_name = ? AND entity_id = ?",
                (m["tableName"], m["entityId"]),
            )
            continue
        for gid in games:
            for did in dims:
                rows.append(
                    {
                        "tableName": m["tableName"],
                        "entityId": m["entityId"],
                        "gameId": gid,
                        "dimensionId": did,
                    }
                )
    if rows:
        return upsert_associations(conn, rows)
    conn.commit()
    return 0


def entity_visible_for_context(
    conn: sqlite3.Connection,
    table_name: str,
    entity_id: str,
    game_id: str,
    dimension_id: str,
) -> bool:
    """Unscoped (no assoc rows) = visible to all; else must match game+dim."""
    _ensure(conn)
    any_row = conn.execute(
        "SELECT 1 FROM entity_gd_assoc WHERE table_name = ? AND entity_id = ? LIMIT 1",
        (table_name, entity_id),
    ).fetchone()
    if not any_row:
        return True
    hit = conn.execute(
        """
        SELECT 1 FROM entity_gd_assoc
        WHERE table_name = ? AND entity_id = ? AND game_id = ? AND dimension_id = ?
        LIMIT 1
        """,
        (table_name, entity_id, game_id, dimension_id),
    ).fetchone()
    return hit is not None


def resolve_entry_properties(
    conn: sqlite3.Connection, entry_id: str, dim_index: int = 0
) -> dict[str, str]:
    """Dim 0 bag from thesaurus_entry_properties, overlay thesaurus_entry_property_dims for dim."""
    _ensure(conn)
    bag: dict[str, str] = {}
    try:
        rows = conn.execute(
            """
            SELECT property_key, property_value FROM thesaurus_entry_properties
            WHERE entry_id = ?
            """,
            (entry_id,),
        ).fetchall()
        for r in rows:
            bag[r["property_key"]] = r["property_value"] if r["property_value"] is not None else ""
    except sqlite3.OperationalError:
        pass
    if int(dim_index) != 0:
        ovs = conn.execute(
            """
            SELECT property_key, property_value FROM thesaurus_entry_property_dims
            WHERE entry_id = ? AND dim_index = ?
            """,
            (entry_id, int(dim_index)),
        ).fetchall()
        for r in ovs:
            bag[r["property_key"]] = r["property_value"] if r["property_value"] is not None else ""
    return bag


def upsert_entry_property_dim(
    conn: sqlite3.Connection,
    entry_id: str,
    dim_index: int,
    property_key: str,
    property_value: str,
) -> None:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        INSERT INTO thesaurus_entry_property_dims
          (id, entry_id, dim_index, property_key, property_value, updated_at)
        VALUES (?, ?, ?, ?, ?, ?)
        ON CONFLICT(entry_id, dim_index, property_key) DO UPDATE SET
          property_value = excluded.property_value,
          updated_at = excluded.updated_at
        """,
        (str(uuid.uuid4()), entry_id, int(dim_index), property_key, property_value, now),
    )
    conn.commit()


def get_warm_snapshot(
    conn: sqlite3.Connection, game_id: str, dimension_id: str, sg_kind: str
) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute(
        """
        SELECT * FROM sg_dimension_warm_snapshots
        WHERE game_id = ? AND dimension_id = ? AND sg_kind = ?
        """,
        (game_id, dimension_id, sg_kind),
    ).fetchone()
    if not r:
        return None
    return {
        "id": r["id"],
        "gameId": r["game_id"],
        "dimensionId": r["dimension_id"],
        "sgKind": r["sg_kind"],
        "payload": json.loads(r["payload_json"]),
        "etag": r["etag"],
        "builtAt": r["built_at"],
        "sourceRevision": r["source_revision"],
    }


def put_warm_snapshot(
    conn: sqlite3.Connection,
    game_id: str,
    dimension_id: str,
    sg_kind: str,
    payload: dict[str, Any],
    etag: str,
    source_revision: Optional[str] = None,
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        INSERT INTO sg_dimension_warm_snapshots
          (id, game_id, dimension_id, sg_kind, payload_json, etag, built_at, source_revision)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(game_id, dimension_id, sg_kind) DO UPDATE SET
          payload_json = excluded.payload_json,
          etag = excluded.etag,
          built_at = excluded.built_at,
          source_revision = excluded.source_revision
        """,
        (
            str(uuid.uuid4()),
            game_id,
            dimension_id,
            sg_kind,
            json.dumps(payload),
            etag,
            now,
            source_revision,
        ),
    )
    conn.commit()
    return get_warm_snapshot(conn, game_id, dimension_id, sg_kind)  # type: ignore[return-value]


def invalidate_warm_snapshots(
    conn: sqlite3.Connection,
    game_id: Optional[str] = None,
    dimension_id: Optional[str] = None,
) -> int:
    _ensure(conn)
    if game_id and dimension_id:
        cur = conn.execute(
            "DELETE FROM sg_dimension_warm_snapshots WHERE game_id = ? AND dimension_id = ?",
            (game_id, dimension_id),
        )
    elif game_id:
        cur = conn.execute(
            "DELETE FROM sg_dimension_warm_snapshots WHERE game_id = ?", (game_id,)
        )
    else:
        cur = conn.execute("DELETE FROM sg_dimension_warm_snapshots")
    conn.commit()
    return cur.rowcount


# --- Change lists / reviews ---


def create_change_list(
    conn: sqlite3.Connection, owner_user_id: str, title: str, items: list[dict[str, Any]]
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    cl_id = str(uuid.uuid4())
    conn.execute(
        """
        INSERT INTO gd_change_lists (id, title, status, owner_user_id, created_at, updated_at)
        VALUES (?, ?, 'in_progress', ?, ?, ?)
        """,
        (cl_id, title or "Association changes", owner_user_id, now, now),
    )
    for it in items:
        conn.execute(
            """
            INSERT INTO gd_change_list_items
              (id, change_list_id, op, table_name, entity_id, game_id, dimension_id, ack, payload_json, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?, 0, ?, ?)
            """,
            (
                str(uuid.uuid4()),
                cl_id,
                it.get("op", "add"),
                it["tableName"],
                it["entityId"],
                it.get("gameId"),
                it.get("dimensionId"),
                json.dumps(it.get("payload") or {}),
                now,
            ),
        )
    conn.commit()
    return get_change_list(conn, cl_id)  # type: ignore[return-value]


def list_change_lists(conn: sqlite3.Connection, status: Optional[str] = None) -> list[dict[str, Any]]:
    _ensure(conn)
    if status:
        rows = conn.execute(
            "SELECT * FROM gd_change_lists WHERE status = ? ORDER BY updated_at DESC",
            (status,),
        ).fetchall()
    else:
        rows = conn.execute("SELECT * FROM gd_change_lists ORDER BY updated_at DESC").fetchall()
    return [
        {
            "id": r["id"],
            "title": r["title"],
            "status": r["status"],
            "ownerUserId": r["owner_user_id"],
            "createdAt": r["created_at"],
            "updatedAt": r["updated_at"],
        }
        for r in rows
    ]


def get_change_list(conn: sqlite3.Connection, cl_id: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    r = conn.execute("SELECT * FROM gd_change_lists WHERE id = ?", (cl_id,)).fetchone()
    if not r:
        return None
    items = conn.execute(
        "SELECT * FROM gd_change_list_items WHERE change_list_id = ?", (cl_id,)
    ).fetchall()
    reviewers = conn.execute(
        "SELECT * FROM gd_change_list_reviewers WHERE change_list_id = ?", (cl_id,)
    ).fetchall()
    comments = conn.execute(
        "SELECT * FROM gd_review_comments WHERE change_list_id = ? ORDER BY created_at",
        (cl_id,),
    ).fetchall()
    suggestions = conn.execute(
        "SELECT * FROM gd_suggestions WHERE change_list_id = ? ORDER BY created_at",
        (cl_id,),
    ).fetchall()
    return {
        "id": r["id"],
        "title": r["title"],
        "status": r["status"],
        "ownerUserId": r["owner_user_id"],
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
        "items": [
            {
                "id": i["id"],
                "op": i["op"],
                "tableName": i["table_name"],
                "entityId": i["entity_id"],
                "gameId": i["game_id"],
                "dimensionId": i["dimension_id"],
                "ack": bool(i["ack"]),
                "payload": json.loads(i["payload_json"] or "{}"),
            }
            for i in items
        ],
        "reviewers": [
            {
                "id": rv["id"],
                "reviewerUserId": rv["reviewer_user_id"],
                "status": rv["status"],
                "updatedAt": rv["updated_at"],
            }
            for rv in reviewers
        ],
        "comments": [
            {
                "id": c["id"],
                "itemId": c["item_id"],
                "authorUserId": c["author_user_id"],
                "body": c["body"],
                "createdAt": c["created_at"],
            }
            for c in comments
        ],
        "suggestions": [
            {
                "id": s["id"],
                "authorUserId": s["author_user_id"],
                "payload": json.loads(s["payload_json"] or "{}"),
                "status": s["status"],
                "createdAt": s["created_at"],
            }
            for s in suggestions
        ],
    }


def set_change_list_status(conn: sqlite3.Connection, cl_id: str, status: str) -> Optional[dict[str, Any]]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        "UPDATE gd_change_lists SET status = ?, updated_at = ? WHERE id = ?",
        (status, now, cl_id),
    )
    conn.commit()
    return get_change_list(conn, cl_id)


def add_reviewer(conn: sqlite3.Connection, cl_id: str, reviewer_user_id: str) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        INSERT INTO gd_change_list_reviewers (id, change_list_id, reviewer_user_id, status, updated_at)
        VALUES (?, ?, ?, 'pending', ?)
        ON CONFLICT(change_list_id, reviewer_user_id) DO UPDATE SET updated_at = excluded.updated_at
        """,
        (str(uuid.uuid4()), cl_id, reviewer_user_id, now),
    )
    conn.commit()
    return get_change_list(conn, cl_id)  # type: ignore[return-value]


def patch_reviewer_status(
    conn: sqlite3.Connection, cl_id: str, reviewer_user_id: str, status: str
) -> Optional[dict[str, Any]]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        UPDATE gd_change_list_reviewers SET status = ?, updated_at = ?
        WHERE change_list_id = ? AND reviewer_user_id = ?
        """,
        (status, now, cl_id, reviewer_user_id),
    )
    conn.commit()
    return get_change_list(conn, cl_id)


def add_comment(
    conn: sqlite3.Connection, cl_id: str, author_user_id: str, body: str, item_id: Optional[str] = None
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        INSERT INTO gd_review_comments (id, change_list_id, item_id, author_user_id, body, created_at)
        VALUES (?, ?, ?, ?, ?, ?)
        """,
        (str(uuid.uuid4()), cl_id, item_id, author_user_id, body, now),
    )
    conn.commit()
    return get_change_list(conn, cl_id)  # type: ignore[return-value]


def add_suggestion(
    conn: sqlite3.Connection, cl_id: str, author_user_id: str, payload: dict[str, Any]
) -> dict[str, Any]:
    _ensure(conn)
    now = _utcnow()
    conn.execute(
        """
        INSERT INTO gd_suggestions (id, change_list_id, author_user_id, payload_json, status, created_at)
        VALUES (?, ?, ?, ?, 'open', ?)
        """,
        (str(uuid.uuid4()), cl_id, author_user_id, json.dumps(payload), now),
    )
    conn.commit()
    return get_change_list(conn, cl_id)  # type: ignore[return-value]


def commit_change_list(conn: sqlite3.Connection, cl_id: str) -> Optional[dict[str, Any]]:
    """Apply assoc add/remove items then mark merged; invalidate warm for touched game/dims."""
    cl = get_change_list(conn, cl_id)
    if not cl:
        return None
    now = _utcnow()
    touched: set[tuple[str, str]] = set()
    for it in cl["items"]:
        if it["op"] == "add" and it.get("gameId") and it.get("dimensionId"):
            conn.execute(
                """
                INSERT OR IGNORE INTO entity_gd_assoc
                  (id, table_name, entity_id, game_id, dimension_id, created_at)
                VALUES (?, ?, ?, ?, ?, ?)
                """,
                (
                    str(uuid.uuid4()),
                    it["tableName"],
                    it["entityId"],
                    it["gameId"],
                    it["dimensionId"],
                    now,
                ),
            )
            touched.add((it["gameId"], it["dimensionId"]))
        elif it["op"] == "remove" and it.get("gameId") and it.get("dimensionId"):
            conn.execute(
                """
                DELETE FROM entity_gd_assoc
                WHERE table_name = ? AND entity_id = ? AND game_id = ? AND dimension_id = ?
                """,
                (it["tableName"], it["entityId"], it["gameId"], it["dimensionId"]),
            )
            touched.add((it["gameId"], it["dimensionId"]))
    for gid, did in touched:
        invalidate_warm_snapshots(conn, gid, did)
    conn.execute(
        "UPDATE gd_change_lists SET status = 'merged', updated_at = ? WHERE id = ?",
        (now, cl_id),
    )
    conn.commit()
    return get_change_list(conn, cl_id)


def ensure_dim0_row(
    conn: sqlite3.Connection,
    table: str,
    id_col: str,
    id_val: str,
    *,
    dim_col: str = "dim",
    copy_from_landing: bool = False,
    landing_dim: int = 0,
) -> bool:
    """
    Ensure a dim=0 existence row for (table, id).
    Returns True if a dim-0 row was inserted, False if it already existed.
    When copy_from_landing and landing_dim != 0, clone columns from that dim row.
    """
    if not table.isidentifier() or not id_col.isidentifier() or not dim_col.isidentifier():
        raise ValueError("invalid table/column name")
    existing = conn.execute(
        f"SELECT 1 FROM {table} WHERE {id_col} = ? AND {dim_col} = 0 LIMIT 1",
        (id_val,),
    ).fetchone()
    if existing:
        return False
    if copy_from_landing and int(landing_dim) != 0:
        src = conn.execute(
            f"SELECT * FROM {table} WHERE {id_col} = ? AND {dim_col} = ?",
            (id_val, int(landing_dim)),
        ).fetchone()
        if src:
            cols = [k for k in src.keys()]
            vals = []
            for c in cols:
                if c == dim_col:
                    vals.append(0)
                else:
                    vals.append(src[c])
            placeholders = ", ".join("?" for _ in cols)
            col_sql = ", ".join(cols)
            conn.execute(
                f"INSERT INTO {table} ({col_sql}) VALUES ({placeholders})",
                vals,
            )
            return True
    raise LookupError(f"no source row to seed dim 0 for {table}.{id_col}={id_val}")
