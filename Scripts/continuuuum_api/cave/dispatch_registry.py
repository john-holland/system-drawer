"""Execute manifest handler specs against the Flask app."""

from __future__ import annotations

import json
from dataclasses import dataclass
from typing import Any, Callable
from urllib.parse import quote, urlencode

import sqlite3

GetConn = Callable[[], sqlite3.Connection]
GetCurrentUser = Callable[[], str]


@dataclass
class HandlerContext:
    structural: str
    payload: dict[str, Any]
    trace_id: str
    tenant: str
    get_conn: GetConn
    get_current_user: GetCurrentUser
    client: Any


def _format_path(template: str, payload: dict[str, Any]) -> str:
    out = template
    for key, val in payload.items():
        if val is None:
            continue
        snake = key
        camel = "".join(
            word.capitalize() if i else word
            for i, word in enumerate(key.split("_"))
        )
        for k in (key, snake, camel):
            out = out.replace("{" + k + "}", quote(str(val), safe=""))
    return out


def _query_from_payload(payload: dict[str, Any], keys: list[str] | None) -> dict[str, str]:
    if not keys:
        return {k: str(v) for k, v in payload.items() if v is not None and not isinstance(v, (dict, list))}
    out: dict[str, str] = {}
    for key in keys:
        if key in payload and payload[key] is not None:
            out[key] = str(payload[key])
    return out


def execute_internal(spec: dict[str, Any], ctx: HandlerContext) -> dict[str, Any]:
    method = str(spec.get("method", "GET")).upper()
    path = _format_path(str(spec.get("path", "/")), ctx.payload)
    headers = {"Content-Type": "application/json", "X-User-ID": ctx.get_current_user()}
    extra_headers = spec.get("headers") or []
    for h in extra_headers:
        if h == "X-User-ID":
            headers["X-User-ID"] = ctx.get_current_user()

    query_keys = spec.get("query_from_payload")
    query = _query_from_payload(ctx.payload, query_keys if isinstance(query_keys, list) else None)

    if method == "GET":
        if query:
            path = path + ("&" if "?" in path else "?") + urlencode(query)
        resp = ctx.client.get(path, headers=headers)
    elif method == "DELETE":
        resp = ctx.client.delete(path, headers=headers)
    elif method == "PATCH":
        resp = ctx.client.patch(path, json=ctx.payload, headers=headers)
    else:
        resp = ctx.client.post(path, json=ctx.payload, headers=headers)

    if resp.is_json:
        body = resp.get_json()
        if isinstance(body, dict) and "ok" not in body:
            body = dict(body)
            body.setdefault("ok", resp.status_code < 400)
        return body if isinstance(body, dict) else {"ok": resp.status_code < 400, "data": body}
    try:
        return json.loads(resp.data.decode())
    except (json.JSONDecodeError, AttributeError):
        return {"ok": resp.status_code < 400, "status": resp.status_code}


def dispatch_handler(spec: dict[str, Any], ctx: HandlerContext) -> dict[str, Any]:
    handler_key = spec.get("handler")
    if handler_key:
        from cave.handlers import HANDLERS

        fn = HANDLERS.get(str(handler_key))
        if fn:
            return fn(ctx)
        return {"ok": False, "error": "unknown_handler", "handler": handler_key}
    if spec.get("proxy"):
        from cave.resaurce_proxy import proxy_cave_route

        service = str(spec.get("proxy"))
        return proxy_cave_route(service, ctx.structural, ctx.payload, ctx.trace_id)
    internal = spec.get("internal")
    if isinstance(internal, dict):
        return execute_internal(internal, ctx)
    return {"ok": False, "error": "invalid_handler_spec", "route": ctx.structural}
