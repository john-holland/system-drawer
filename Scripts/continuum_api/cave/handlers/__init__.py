"""Named composite Cave handlers."""

from __future__ import annotations

from typing import Any, Callable

from cave.dispatch_registry import HandlerContext

from cave.handlers.table_read import session_open

HandlerFn = Callable[[HandlerContext], dict[str, Any]]

HANDLERS: dict[str, HandlerFn] = {
    "table_read.session_open": session_open,
}

__all__ = ["HANDLERS", "session_open"]
