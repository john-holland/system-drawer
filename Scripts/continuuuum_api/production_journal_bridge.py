"""Mirror saurce_ledger_entries into resaurce production budget journal."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
import uuid
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")


def mirror_saurce_ledger_to_resaurce(
    ledger_entry_id: str,
    entry_type: str,
    product_id: str | None,
    *,
    gross_amount: float | None = None,
    net_amount: float | None = None,
    budget_plan_id: str | None = None,
    story_id: str | None = None,
    work_order_id: str | None = None,
) -> dict[str, Any]:
    """Post a balanced journal entry in resaurce for a Saurce ledger line."""
    payload = {
        "saurce_ledger_entry_id": ledger_entry_id,
        "entry_type": entry_type,
        "product_id": product_id,
        "gross_amount": gross_amount,
        "net_amount": net_amount,
        "budget_plan_id": budget_plan_id,
        "story_id": story_id,
        "work_order_id": work_order_id,
    }
    body = json.dumps(
        {
            "route": "resaurce:production/budget/journal/from-saurce",
            "payload": payload,
            "trace_id": f"mirror_{uuid.uuid4().hex[:12]}",
        }
    ).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode())
    except (urllib.error.URLError, urllib.error.HTTPError, json.JSONDecodeError) as e:
        return {"ok": False, "error": "resaurce_mirror_failed", "detail": str(e)}
