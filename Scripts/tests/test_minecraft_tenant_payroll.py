"""Minecraftuuuum tenant payroll seed: Marketplace 70/30 + HWM 10%."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from payroll_engine import (  # noqa: E402
    PLATFORM_MICROSOFT_KIND,
    PLATFORM_MICROSOFT_PCT,
    ensure_minecraftuuuum_tenant_payroll,
    ensure_payroll_schema,
    list_retainers,
    tenant_retainer_split,
)


def test_ensure_minecraftuuuum_tenant_payroll_idempotent(tmp_path):
    db = tmp_path / "p.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    ensure_payroll_schema(conn)
    first = ensure_minecraftuuuum_tenant_payroll(conn)
    second = ensure_minecraftuuuum_tenant_payroll(conn)
    assert first["id"] == second["id"]
    assert second["tenantId"] == "minecraftuuuum"
    assert abs(second["hwmRetainerPct"] - 0.10) < 1e-9
    kinds = {r["kind"]: r for r in list_retainers(conn, second["id"])}
    assert kinds[PLATFORM_MICROSOFT_KIND]["enabled"] is True
    assert abs(float(kinds[PLATFORM_MICROSOFT_KIND]["percent"]) - PLATFORM_MICROSOFT_PCT) < 1e-9
    assert kinds["service_unity"]["enabled"] is False
    assert kinds["service_cursor"]["enabled"] is False
    split = tenant_retainer_split(conn, "minecraftuuuum")
    assert abs(split["creatorPct"] - 0.70) < 1e-9
    assert abs(split["platformPct"] - 0.30) < 1e-9
    conn.close()
