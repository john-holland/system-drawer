"""Cave route handler — envelope v2 in, YAML dispatch out."""

from __future__ import annotations

import uuid
from typing import Any, Callable

import sqlite3

from cave.dispatch_registry import HandlerContext, dispatch_handler
from cave.lvm_hooks import after_cave_route_mutation
from cave.manifest_loader import (
    get_handler_spec,
    get_lvm_events,
    is_mutating_route,
    load_cave_manifest,
    message_to_structural,
    resolve_structural_route,
)
from cave.paths import parse_route
from cave.resaurce_proxy import proxy_cave_route

GetConn = Callable[[], sqlite3.Connection]
GetCurrentUser = Callable[[], str]


def _trace_id(body: dict[str, Any]) -> str:
    tid = body.get("trace_id") or body.get("traceId")
    if tid:
        return str(tid)
    return f"continuuuum_{uuid.uuid4().hex[:12]}"


def handle_cave_route(
    body: dict[str, Any],
    get_conn: GetConn,
    get_current_user: GetCurrentUser,
    client: Any,
) -> dict[str, Any]:
    manifest = load_cave_manifest()
    trace_id = _trace_id(body)
    tenant = str(body.get("tenant") or "")
    payload = body.get("payload") if isinstance(body.get("payload"), dict) else {}

    route_raw = str(body.get("route") or "")
    message = body.get("message")
    if message and not route_raw:
        structural = message_to_structural(manifest, str(message))
        if not structural:
            return {"ok": False, "error": "unknown_message", "message": message}
        explicit_service = manifest.get("service") or "continuuuum"
    elif route_raw:
        explicit_service, structural = parse_route(route_raw)
    else:
        structural = resolve_structural_route(body, manifest)
        explicit_service = manifest.get("service") or "continuuuum"

    if not structural:
        return {"ok": False, "error": "missing_route"}

    ctx_base = {
        "structural": structural,
        "trace_id": trace_id,
        "tenant": tenant,
        "service": explicit_service,
        "route": route_raw or f"{explicit_service}:{structural}",
    }

    if explicit_service in ("resaurce", "saurce"):
        out = proxy_cave_route(explicit_service, structural, payload, trace_id)
        if is_mutating_route(manifest, structural):
            lvm_names = get_lvm_events(manifest, structural)
            out = after_cave_route_mutation(ctx_base, out, get_conn=get_conn, lvm_event_names=lvm_names)
        return out if isinstance(out, dict) else {"ok": False, "error": "invalid_upstream"}

    spec = get_handler_spec(manifest, structural)
    if not spec:
        return {"ok": False, "error": "unknown_route", "route": f"continuuuum:{structural}"}

    hctx = HandlerContext(
        structural=structural,
        payload=payload,
        trace_id=trace_id,
        tenant=tenant,
        get_conn=get_conn,
        get_current_user=get_current_user,
        client=client,
    )
    out = dispatch_handler(spec, hctx)
    if not isinstance(out, dict):
        out = {"ok": True, "data": out}

    if is_mutating_route(manifest, structural):
        lvm_names = get_lvm_events(manifest, structural)
        out = after_cave_route_mutation(ctx_base, out, get_conn=get_conn, lvm_event_names=lvm_names)

    return out
