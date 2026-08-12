"""Parse game/dimension from query, headers, then user_context."""

from __future__ import annotations

import sqlite3
from typing import Any, Callable, Optional

from flask import Request, g

try:
    from continuuuum_api import game_dimension_dao as dao
except ImportError:
    import game_dimension_dao as dao  # type: ignore

GetConn = Callable[[], sqlite3.Connection]


def _is_admin_header(request: Request) -> bool:
    return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")


def _user_id(request: Request) -> str:
    return (request.headers.get("X-User-ID") or "anonymous").strip() or "anonymous"


def get_game_dimension(
    request: Request,
    conn: sqlite3.Connection,
    *,
    enforce_visibility: bool = True,
) -> dict[str, Any]:
    """
    Resolve game + dimension.
    Precedence: query args > X-Game / X-Dimension headers > user_context > defaults (main, 0).
    """
    user_id = _user_id(request)
    is_admin = _is_admin_header(request)
    ctx = dao.get_user_context(conn, user_id)

    game_ref = request.args.get("game")
    if game_ref is None or game_ref == "":
        game_ref = request.headers.get("X-Game")
    if game_ref is None or game_ref == "":
        game_ref = ctx.get("gameSlug") or "main"

    dim_ref: Any = request.args.get("dimension")
    if dim_ref is None or dim_ref == "":
        dim_ref = request.headers.get("X-Dimension")
    if dim_ref is None or dim_ref == "":
        dim_ref = ctx.get("dimIndex", 0)

    game = dao.resolve_game_ref(conn, game_ref)
    dim = dao.resolve_dimension_ref(conn, dim_ref)
    if not game:
        game = dao.get_game_by_slug(conn, "main")
    if not dim:
        dim = dao.get_dimension_by_index(conn, 0)

    err: Optional[str] = None
    if enforce_visibility and game:
        err = dao.assert_game_visible(conn, game, user_id, is_admin)
    if not err and enforce_visibility and dim:
        err = dao.assert_dimension_visible(conn, dim, user_id, is_admin)

    return {
        "userId": user_id,
        "isAdmin": is_admin,
        "game": game,
        "dimension": dim,
        "gameSlug": game["slug"] if game else "main",
        "dimIndex": dim["dimIndex"] if dim else 0,
        "error": err,
    }


def bind_game_dimension_to_g(get_conn: GetConn, request: Request) -> Optional[tuple[Any, int]]:
    """Bind flask.g.game / g.dimension / g.dim_index / g.game_slug. Return (jsonify, status) on visibility error."""
    conn = get_conn()
    resolved = get_game_dimension(request, conn, enforce_visibility=True)
    g.game = resolved["game"]
    g.dimension = resolved["dimension"]
    g.game_slug = resolved["gameSlug"]
    g.dim_index = resolved["dimIndex"]
    g.gd_user_id = resolved["userId"]
    g.gd_is_admin = resolved["isAdmin"]
    if resolved.get("error"):
        from flask import jsonify

        code = resolved["error"]
        return jsonify({"error": code, "code": code}), 403
    return None