"""Composite table-read Cave handlers."""

from __future__ import annotations

from typing import Any
from urllib.parse import quote

from cave.dispatch_registry import HandlerContext, execute_internal


def session_open(ctx: HandlerContext) -> dict[str, Any]:
    join_spec = {
        "method": "POST",
        "path": "/api/table-read/sessions/{sessionId}/join",
        "headers": ["X-User-ID"],
    }
    snap = execute_internal(join_spec, ctx)
    session_id = ctx.payload.get("sessionId") or ctx.payload.get("session_id") or ""
    if session_id and isinstance(snap, dict):
        ensure_ctx = HandlerContext(
            structural=ctx.structural,
            payload={"sessionId": session_id, **ctx.payload},
            trace_id=ctx.trace_id,
            tenant=ctx.tenant,
            get_conn=ctx.get_conn,
            get_current_user=ctx.get_current_user,
            client=ctx.client,
        )
        ensure_spec = {
            "method": "POST",
            "path": f"/api/table-read/sessions/{quote(str(session_id), safe='')}/ensure-chat",
            "headers": ["X-User-ID"],
        }
        chat = execute_internal(ensure_spec, ensure_ctx)
        if isinstance(chat, dict) and chat.get("chatRoomId"):
            snap.setdefault("session", {})
            if isinstance(snap.get("session"), dict):
                snap["session"]["chatRoomId"] = chat["chatRoomId"]
            if chat.get("shareUrl"):
                snap["session"]["shareUrl"] = chat["shareUrl"]
    if isinstance(snap, dict):
        snap.setdefault("ok", True)
    return snap
