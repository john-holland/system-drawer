"""Proxy envelope v2 routes to resaurce Cave."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
SAURCE_CAVE_URL = os.environ.get("SAURCE_CAVE_URL", "http://127.0.0.1:3457").rstrip("/")


def proxy_cave_route(
    service: str,
    structural: str,
    payload: dict[str, Any] | None,
    trace_id: str,
    *,
    schema_version: str = "2.0",
) -> dict[str, Any]:
    base = RESAURCE_CAVE_URL if service == "resaurce" else SAURCE_CAVE_URL
    body = json.dumps(
        {
            "schema_version": schema_version,
            "route": f"{service}:{structural}",
            "payload": payload or {},
            "trace_id": trace_id,
            "reply_mode": "sync_http",
        }
    ).encode()
    req = urllib.request.Request(
        f"{base}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        try:
            return json.loads(e.read().decode() or "{}")
        except json.JSONDecodeError:
            return {"ok": False, "error": "upstream_http_error", "status": e.code}
    except urllib.error.URLError as e:
        return {"ok": False, "error": "upstream_unavailable", "detail": str(e)}
