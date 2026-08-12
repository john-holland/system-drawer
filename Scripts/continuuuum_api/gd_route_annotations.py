"""@accepts_game_dimension — bind game/dimension from query/headers onto flask.g."""

from __future__ import annotations

import functools
import sqlite3
from typing import Any, Callable, Optional

from flask import request

GetConn = Callable[[], sqlite3.Connection]

_get_conn: Optional[GetConn] = None


def configure_gd_annotations(get_conn: GetConn) -> None:
    global _get_conn
    _get_conn = get_conn
    # Keep dual import paths (package vs flat) in sync for tests and server.
    for mod_name in ("continuuuum_api.gd_route_annotations", "gd_route_annotations"):
        try:
            mod = __import__(mod_name, fromlist=["_get_conn"])
            setattr(mod, "_get_conn", get_conn)
        except ImportError:
            pass


def accepts_game_dimension(fn: Callable[..., Any]) -> Callable[..., Any]:
    """Mark a Flask view as accepting game/dimension query params + headers.

    Binds flask.g.game / g.dimension via request_context; enforces visibility.
    Query/headers: game, dimension (X-Game, X-Dimension).

    Do NOT use on finance / money routes (payroll, budget, credits, legal).
    Game + dimension apply only to content and game-parameter provinces
    (lemmas, associations, mods, spatial, story, etc.).
    """

    @functools.wraps(fn)
    def wrapper(*args: Any, **kwargs: Any):
        if _get_conn is None:
            return fn(*args, **kwargs)
        try:
            from continuuuum_api.request_context import bind_game_dimension_to_g
        except ImportError:
            from request_context import bind_game_dimension_to_g  # type: ignore

        err = bind_game_dimension_to_g(_get_conn, request)
        if err is not None:
            return err
        return fn(*args, **kwargs)

    wrapper.__continuuuum_accepts_game_dimension__ = True  # type: ignore[attr-defined]
    return wrapper
