"""Company income payroll: single HWM %, Unity/Cursor seats, flexible retainers."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

SCHEMA_PATH = Path(__file__).resolve().parent.parent / "continuuuum_payroll_schema.sql"

DEFAULT_HWM_USD = 100_000.0
DEFAULT_HWM_RETAINER_PCT = 0.10
MINECRAFTUUUUM_TENANT = "minecraftuuuum"
PLATFORM_MICROSOFT_PCT = 0.30
PLATFORM_MICROSOFT_KIND = "platform_microsoft"
DEFAULT_CURSOR_MONTHLY = 40.0
MONTHLY_CRON = "0 0 1 * *"
UNITY_ENTERPRISE_THRESHOLD_USD = 25_000_000.0
UNITY_ENTERPRISE_CONTACT_LABEL = (
    "Lifetime at or above $25,000,000 — contact Unity Finance and Business departments "
    "for custom pricing."
)

DEFAULT_UNITY_BANDS = [
    {"name": "free", "minLifetime": 0, "maxLifetime": 200_000, "seatUsd": 0},
    {"name": "pro", "minLifetime": 200_000, "maxLifetime": 25_000_000, "seatUsd": 210},
    {"name": "enterprise", "minLifetime": 25_000_000, "maxLifetime": None, "seatUsd": None, "custom": True},
]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def new_id(prefix: str = "pay") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    return (
        conn.execute(
            "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
            (name,),
        ).fetchone()
        is not None
    )


def _cols(conn: sqlite3.Connection, table: str) -> set[str]:
    return {r[1] for r in conn.execute(f"PRAGMA table_info({table})").fetchall()}


def ensure_payroll_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    if _table_exists(conn, "payroll_companies"):
        cols = _cols(conn, "payroll_companies")
        if "hwm_retainer_pct" not in cols:
            conn.execute(
                "ALTER TABLE payroll_companies ADD COLUMN hwm_retainer_pct REAL NOT NULL DEFAULT 0.10"
            )
        if "unity_enterprise_override_usd" not in cols:
            conn.execute(
                "ALTER TABLE payroll_companies ADD COLUMN unity_enterprise_override_usd REAL"
            )
        if "tenant_id" not in cols:
            conn.execute("ALTER TABLE payroll_companies ADD COLUMN tenant_id TEXT")
    if _table_exists(conn, "payroll_retainers"):
        rcols = _cols(conn, "payroll_retainers")
        if "amount_locked" not in rcols:
            conn.execute(
                "ALTER TABLE payroll_retainers ADD COLUMN amount_locked INTEGER NOT NULL DEFAULT 0"
            )
    if _table_exists(conn, "payroll_allocations"):
        acols = _cols(conn, "payroll_allocations")
        if "retainer_id" not in acols:
            conn.execute("ALTER TABLE payroll_allocations ADD COLUMN retainer_id TEXT")
    # Global pricing row
    if _table_exists(conn, "payroll_service_pricing"):
        row = conn.execute(
            "SELECT id FROM payroll_service_pricing WHERE company_id IS NULL LIMIT 1"
        ).fetchone()
        if not row:
            conn.execute(
                """INSERT INTO payroll_service_pricing
                   (id, company_id, cursor_monthly_usd, unity_bands_json, updated_at)
                   VALUES (?, NULL, ?, ?, ?)""",
                (
                    new_id("psp"),
                    DEFAULT_CURSOR_MONTHLY,
                    json.dumps(DEFAULT_UNITY_BANDS),
                    _now(),
                ),
            )
    _migrate_free_until_100k_v1(conn)
    conn.commit()


def ensure_payroll_schema_force(conn: sqlite3.Connection) -> None:
    """Re-run schema + one-shot migrations (tests / pre-prod)."""
    ensure_payroll_schema(conn)


HWM_MODEL_MIGRATION_KEY = "hwm_model"
HWM_MODEL_MIGRATION_VALUE = "free_until_100k_v1"


def _migrate_free_until_100k_v1(conn: sqlite3.Connection) -> None:
    """
    Pre-prod one-shot: free until $100k HWM then 10% retainer.
    Resets income/allocation history booked under the old pre-HWM skim model.
    Keeps companies, team members, and retainer definitions.
    """
    conn.execute(
        """CREATE TABLE IF NOT EXISTS payroll_meta (
             key TEXT PRIMARY KEY,
             value TEXT NOT NULL,
             updated_at TEXT NOT NULL
           )"""
    )
    done = conn.execute(
        "SELECT value FROM payroll_meta WHERE key = ?",
        (HWM_MODEL_MIGRATION_KEY,),
    ).fetchone()
    if done and str(done["value"] if isinstance(done, sqlite3.Row) else done[0]) == HWM_MODEL_MIGRATION_VALUE:
        # Still drop legacy tables if a partial run left them.
        conn.execute("DROP TABLE IF EXISTS payroll_saving_indexes")
        conn.execute("DROP TABLE IF EXISTS payroll_post_hwm_shares")
        return

    now = _now()
    if _table_exists(conn, "payroll_companies"):
        conn.execute(
            """UPDATE payroll_companies
               SET high_water_mark_usd = ?,
                   hwm_retainer_pct = ?,
                   lifetime_net_usd = 0,
                   phase = 'pre_hwm',
                   updated_at = ?""",
            (DEFAULT_HWM_USD, DEFAULT_HWM_RETAINER_PCT, now),
        )

    # Wipe ledger rows computed under the inverted (12% pre-HWM) model.
    for table in (
        "payroll_allocations",
        "payroll_income_events",
        "payroll_retainer_draws",
        "payroll_retainer_runs",
    ):
        if _table_exists(conn, table):
            conn.execute(f"DELETE FROM {table}")

    if _table_exists(conn, "payroll_beneficiary_balances"):
        conn.execute(
            """UPDATE payroll_beneficiary_balances
               SET ops_usd = 0, retainer_usd = 0, distributed_usd = 0"""
        )

    conn.execute("DROP TABLE IF EXISTS payroll_saving_indexes")
    conn.execute("DROP TABLE IF EXISTS payroll_post_hwm_shares")

    conn.execute(
        """INSERT INTO payroll_meta (key, value, updated_at) VALUES (?, ?, ?)
           ON CONFLICT(key) DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at""",
        (HWM_MODEL_MIGRATION_KEY, HWM_MODEL_MIGRATION_VALUE, now),
    )


def _ensure_company_balance(conn: sqlite3.Connection, company_id: str) -> None:
    conn.execute(
        """INSERT OR IGNORE INTO payroll_beneficiary_balances
           (id, company_id, beneficiary, ops_usd, retainer_usd, distributed_usd)
           VALUES (?, ?, 'company', 0, 0, 0)""",
        (new_id("pbb"), company_id),
    )


def _seed_service_retainers(conn: sqlite3.Connection, company_id: str, *, enabled: int = 1) -> None:
    now = _now()
    for kind, name, track in (
        ("service_unity", "Unity (gameplay)", "gameplay"),
        ("service_cursor", "Cursor (technical)", "technical"),
        ("service_unreal", "Unreal (gameplay)", "gameplay"),
    ):
        existing = conn.execute(
            """SELECT id FROM payroll_retainers
               WHERE company_id = ? AND kind = ? LIMIT 1""",
            (company_id, kind),
        ).fetchone()
        if existing:
            continue
        seed_enabled = 0 if kind == "service_unreal" else enabled
        conn.execute(
            """INSERT INTO payroll_retainers
               (id, company_id, name, kind, mode, percent, amount_usd, cron_expr,
                forward_company_id, forward_label, user_ids_json, auto_track, enabled,
                created_at, updated_at)
               VALUES (?, ?, ?, ?, 'fixed_cron', NULL, 0, ?, NULL, NULL, '[]', ?, ?, ?, ?)""",
            (new_id("prt"), company_id, name, kind, MONTHLY_CRON, track, seed_enabled, now, now),
        )
    _update_service_retainer_amounts(conn, company_id)


def create_company(
    conn: sqlite3.Connection,
    *,
    name: str,
    saurce_product_id: str | None = None,
    high_water_mark_usd: float = DEFAULT_HWM_USD,
    hwm_retainer_pct: float = DEFAULT_HWM_RETAINER_PCT,
) -> dict[str, Any]:
    ensure_payroll_schema(conn)
    cid = new_id("pc")
    now = _now()
    pct = float(hwm_retainer_pct)
    if pct < 0 or pct > 1:
        raise ValueError("hwmRetainerPct must be between 0 and 1")
    conn.execute(
        """INSERT INTO payroll_companies
           (id, name, saurce_product_id, high_water_mark_usd, hwm_retainer_pct,
            lifetime_net_usd, phase, currency, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, 0, 'pre_hwm', 'USD', ?, ?)""",
        (cid, name, saurce_product_id, float(high_water_mark_usd), pct, now, now),
    )
    _ensure_company_balance(conn, cid)
    _seed_service_retainers(conn, cid)
    conn.commit()
    return get_company(conn, cid)


def get_company(conn: sqlite3.Connection, company_id: str) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    row = conn.execute(
        "SELECT * FROM payroll_companies WHERE id = ?", (company_id,)
    ).fetchone()
    if not row:
        return None
    return _company_dict(row)


def find_company_by_tenant(conn: sqlite3.Connection, tenant_id: str) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    if not tenant_id:
        return None
    row = conn.execute(
        "SELECT * FROM payroll_companies WHERE tenant_id = ? LIMIT 1",
        (tenant_id,),
    ).fetchone()
    if row:
        return _company_dict(row)
    row = conn.execute(
        "SELECT * FROM payroll_companies WHERE lower(name) = lower(?) LIMIT 1",
        (tenant_id,),
    ).fetchone()
    return _company_dict(row) if row else None


def set_service_retainer_enabled(
    conn: sqlite3.Connection, company_id: str, kind: str, enabled: bool
) -> None:
    conn.execute(
        """UPDATE payroll_retainers SET enabled = ?, updated_at = ?
           WHERE company_id = ? AND kind = ?""",
        (1 if enabled else 0, _now(), company_id, kind),
    )


def tenant_retainer_split(conn: sqlite3.Connection, tenant_id: str | None = None) -> dict[str, Any]:
    """Marketplace 70/30 + Continuuuum HWM + optional Unity/Cursor/Unreal flags."""
    company = ensure_minecraftuuuum_tenant_payroll(conn, tenant_id or MINECRAFTUUUUM_TENANT)
    cid = company["id"]
    retainers = list_retainers(conn, cid)
    by_kind = {r["kind"]: r for r in retainers}
    platform = by_kind.get(PLATFORM_MICROSOFT_KIND) or {}
    platform_pct = float(platform.get("percent") or PLATFORM_MICROSOFT_PCT)
    return {
        "tenantId": company.get("tenantId") or MINECRAFTUUUUM_TENANT,
        "companyId": cid,
        "creatorPct": round(1.0 - platform_pct, 4),
        "platformPct": platform_pct,
        "continuuuumHwmPct": float(company.get("hwmRetainerPct") or DEFAULT_HWM_RETAINER_PCT),
        "platformKind": PLATFORM_MICROSOFT_KIND,
        "platformEnabled": bool(platform.get("enabled", True)),
        "serviceUnityEnabled": bool((by_kind.get("service_unity") or {}).get("enabled")),
        "serviceCursorEnabled": bool((by_kind.get("service_cursor") or {}).get("enabled")),
        "serviceUnrealEnabled": bool((by_kind.get("service_unreal") or {}).get("enabled")),
        "retainer": True,
    }


def ensure_minecraftuuuum_tenant_payroll(
    conn: sqlite3.Connection, tenant_id: str = MINECRAFTUUUUM_TENANT
) -> dict[str, Any]:
    """Idempotent company + Mojang/Microsoft 30% + HWM 10%; Unity/Cursor/Unreal disabled."""
    ensure_payroll_schema(conn)
    slug = (tenant_id or MINECRAFTUUUUM_TENANT).strip() or MINECRAFTUUUUM_TENANT
    existing = find_company_by_tenant(conn, slug)
    if existing is None:
        company = create_company(conn, name="Minecraftuuuum")
        cid = company["id"]
        conn.execute(
            "UPDATE payroll_companies SET tenant_id = ?, hwm_retainer_pct = ?, updated_at = ? WHERE id = ?",
            (slug, DEFAULT_HWM_RETAINER_PCT, _now(), cid),
        )
    else:
        cid = existing["id"]
        conn.execute(
            """UPDATE payroll_companies
               SET tenant_id = ?, hwm_retainer_pct = ?, updated_at = ?
               WHERE id = ?""",
            (slug, DEFAULT_HWM_RETAINER_PCT, _now(), cid),
        )
        _seed_service_retainers(conn, cid, enabled=0)

    now = _now()
    plat = conn.execute(
        """SELECT id FROM payroll_retainers
           WHERE company_id = ? AND kind = ? LIMIT 1""",
        (cid, PLATFORM_MICROSOFT_KIND),
    ).fetchone()
    first_platform = plat is None
    if first_platform:
        conn.execute(
            """INSERT INTO payroll_retainers
               (id, company_id, name, kind, mode, percent, amount_usd, cron_expr,
                forward_company_id, forward_label, user_ids_json, auto_track, enabled,
                created_at, updated_at)
               VALUES (?, ?, ?, ?, 'percent', ?, NULL, NULL, NULL, ?, '[]', NULL, 1, ?, ?)""",
            (
                new_id("prt"),
                cid,
                "Mojang / Microsoft (Marketplace)",
                PLATFORM_MICROSOFT_KIND,
                PLATFORM_MICROSOFT_PCT,
                "Mojang/Microsoft",
                now,
                now,
            ),
        )
        for kind in ("service_unity", "service_cursor", "service_unreal"):
            conn.execute(
                """UPDATE payroll_retainers SET enabled = 0, updated_at = ?
                   WHERE company_id = ? AND kind = ?""",
                (now, cid, kind),
            )
    conn.commit()
    company = get_company(conn, cid)
    assert company is not None
    return company


def find_company_by_product(conn: sqlite3.Connection, product_id: str) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    if not product_id:
        return None
    row = conn.execute(
        "SELECT * FROM payroll_companies WHERE saurce_product_id = ? LIMIT 1",
        (product_id,),
    ).fetchone()
    return _company_dict(row) if row else None


def list_companies(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    ensure_payroll_schema(conn)
    rows = conn.execute(
        "SELECT * FROM payroll_companies ORDER BY created_at DESC"
    ).fetchall()
    return [_company_dict(r) for r in rows]


def _company_dict(row: sqlite3.Row | dict) -> dict[str, Any]:
    r = dict(row)
    return {
        "id": r["id"],
        "name": r["name"],
        "saurceProductId": r.get("saurce_product_id"),
        "highWaterMarkUsd": float(r["high_water_mark_usd"]),
        "hwmRetainerPct": float(r.get("hwm_retainer_pct") or DEFAULT_HWM_RETAINER_PCT),
        "lifetimeNetUsd": float(r["lifetime_net_usd"]),
        "phase": r["phase"],
        "currency": r.get("currency") or "USD",
        "unityEnterpriseOverrideUsd": r.get("unity_enterprise_override_usd"),
        "tenantId": r.get("tenant_id"),
        "createdAt": r.get("created_at"),
        "updatedAt": r.get("updated_at"),
    }


def patch_company(conn: sqlite3.Connection, company_id: str, body: dict[str, Any]) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    row = conn.execute(
        "SELECT * FROM payroll_companies WHERE id = ?", (company_id,)
    ).fetchone()
    if not row:
        return None
    name = body.get("name", row["name"])
    product = body.get("saurceProductId", row["saurce_product_id"])
    if "saurce_product_id" in body:
        product = body["saurce_product_id"]
    hwm = float(body.get("highWaterMarkUsd", row["high_water_mark_usd"]))
    if hwm <= 0:
        raise ValueError("highWaterMarkUsd must be positive")
    pct = float(body.get("hwmRetainerPct", row["hwm_retainer_pct"]))
    if pct < 0 or pct > 1:
        raise ValueError("hwmRetainerPct must be between 0 and 1")
    override = body.get("unityEnterpriseOverrideUsd", row["unity_enterprise_override_usd"])
    if override is not None and override != "":
        override = float(override)
    else:
        override = None
    conn.execute(
        """UPDATE payroll_companies
           SET name = ?, saurce_product_id = ?, high_water_mark_usd = ?,
               hwm_retainer_pct = ?, unity_enterprise_override_usd = ?, updated_at = ?
           WHERE id = ?""",
        (name, product, hwm, pct, override, _now(), company_id),
    )
    _sync_service_retainers(conn, company_id)
    conn.commit()
    return get_company(conn, company_id)


def _bump_company(
    conn: sqlite3.Connection,
    company_id: str,
    *,
    ops: float = 0.0,
    retainer: float = 0.0,
) -> None:
    _ensure_company_balance(conn, company_id)
    conn.execute(
        """UPDATE payroll_beneficiary_balances
           SET ops_usd = ops_usd + ?, retainer_usd = retainer_usd + ?
           WHERE company_id = ? AND beneficiary = 'company'""",
        (ops, retainer, company_id),
    )


def _add_allocation(
    conn: sqlite3.Connection,
    *,
    event_id: str,
    company_id: str,
    beneficiary: str,
    bucket: str,
    amount: float,
    rate: float,
    phase: str,
    allocations: list[dict[str, Any]],
    retainer_id: str | None = None,
    retainer_name: str | None = None,
) -> None:
    if abs(amount) < 1e-12:
        return
    aid = new_id("pa")
    conn.execute(
        """INSERT INTO payroll_allocations
           (id, event_id, company_id, beneficiary, bucket, amount_usd, rate, phase, retainer_id)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (aid, event_id, company_id, beneficiary, bucket, amount, rate, phase, retainer_id),
    )
    row = {
        "id": aid,
        "beneficiary": beneficiary,
        "bucket": bucket,
        "amountUsd": amount,
        "rate": rate,
        "phase": phase,
    }
    if retainer_id:
        row["retainerId"] = retainer_id
    if retainer_name:
        row["retainerName"] = retainer_name
    allocations.append(row)


def _try_saurce_ledger(
    conn: sqlite3.Connection,
    entry_type: str,
    product_id: str | None,
    *,
    net_amount: float | None = None,
    gross_amount: float | None = None,
    idempotency_key: str | None = None,
    meta: dict[str, Any] | None = None,
) -> str | None:
    if not _table_exists(conn, "saurce_ledger_entries"):
        return None
    try:
        try:
            from continuuuum_api.saurce_routes import _ledger
        except ImportError:
            from saurce_routes import _ledger
        return _ledger(
            conn,
            entry_type,
            product_id,
            net_amount=net_amount,
            gross_amount=gross_amount,
            idempotency_key=idempotency_key,
            meta=meta or {},
        )
    except Exception:
        return None


def post_income(
    conn: sqlite3.Connection,
    company_id: str,
    net_amount: float,
    *,
    gross_amount: float | None = None,
    source: str = "manual",
    idempotency_key: str | None = None,
    meta: dict[str, Any] | None = None,
) -> dict[str, Any]:
    """Post net income; single HWM % skim on under-mark portion; rest to ops."""
    ensure_payroll_schema(conn)
    net = float(net_amount)
    if net < 0:
        raise ValueError("net_amount must be >= 0")
    key = idempotency_key or new_id("pik")
    existing = conn.execute(
        """SELECT id FROM payroll_income_events
           WHERE company_id = ? AND idempotency_key = ?""",
        (company_id, key),
    ).fetchone()
    if existing:
        return get_event(conn, existing["id"])

    company = conn.execute(
        "SELECT * FROM payroll_companies WHERE id = ?", (company_id,)
    ).fetchone()
    if not company:
        raise KeyError("company_not_found")

    lifetime = float(company["lifetime_net_usd"])
    hwm = float(company["high_water_mark_usd"])
    pct = float(company["hwm_retainer_pct"] or DEFAULT_HWM_RETAINER_PCT)

    room = max(0.0, hwm - lifetime)
    pre_portion = min(net, room)
    post_portion = max(0.0, net - pre_portion)
    if pre_portion > 0 and post_portion > 0:
        phase_applied = "crossing"
    elif post_portion > 0:
        phase_applied = "post_hwm"
    else:
        phase_applied = "pre_hwm"

    event_id = new_id("pie")
    now = _now()
    meta_obj = dict(meta or {})
    conn.execute(
        """INSERT INTO payroll_income_events
           (id, company_id, idempotency_key, source, gross_usd, net_usd, phase_applied,
            pre_hwm_portion_usd, post_hwm_portion_usd, meta_json, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            event_id,
            company_id,
            key,
            source,
            gross_amount,
            net,
            phase_applied,
            pre_portion,
            post_portion,
            json.dumps(meta_obj),
            now,
        ),
    )

    allocations: list[dict[str, Any]] = []
    hwm_name = _hwm_retainer_name(conn, company_id)
    # Free until HWM: skim only the over-HWM (post) portion.
    skim = post_portion * pct if post_portion > 0 else 0.0

    # Percent-of-income retainers (e.g. eshielda 5%) take from remaining after HWM skim.
    ops_pool = net - skim
    percent_rows = conn.execute(
        """SELECT id, name, percent, forward_company_id FROM payroll_retainers
           WHERE company_id = ? AND enabled = 1 AND mode = 'percent'
             AND kind NOT IN ('service_unity', 'service_cursor', 'hwm_pct')
           ORDER BY name COLLATE NOCASE""",
        (company_id,),
    ).fetchall()
    percent_cuts: list[tuple[Any, float, float]] = []
    for prow in percent_rows:
        rate = float(prow["percent"] or 0)
        if rate <= 0:
            continue
        cut = min(net * rate, ops_pool)
        if cut <= 1e-12:
            continue
        ops_pool -= cut
        percent_cuts.append((prow, cut, rate))

    if skim > 0:
        _add_allocation(
            conn,
            event_id=event_id,
            company_id=company_id,
            beneficiary="company",
            bucket="retainer",
            amount=skim,
            rate=pct,
            phase="post_hwm",
            allocations=allocations,
            retainer_name=hwm_name,
        )
        _bump_company(conn, company_id, retainer=skim)

    for prow, cut, rate in percent_cuts:
        target = prow["forward_company_id"] or company_id
        _add_allocation(
            conn,
            event_id=event_id,
            company_id=company_id,
            beneficiary="company" if target == company_id else f"forward:{target}",
            bucket="retainer",
            amount=cut,
            rate=rate,
            phase=phase_applied,
            allocations=allocations,
            retainer_id=prow["id"],
            retainer_name=prow["name"],
        )
        _ensure_company_balance(conn, target)
        _bump_company(conn, target, retainer=cut)

    # Remaining ops: fill free (pre-HWM) band first, then post-HWM remainder.
    ops_pre = min(pre_portion, ops_pool) if pre_portion > 0 else 0.0
    ops_post = max(0.0, ops_pool - ops_pre)
    if ops_pre > 0:
        _add_allocation(
            conn,
            event_id=event_id,
            company_id=company_id,
            beneficiary="company",
            bucket="ops",
            amount=ops_pre,
            rate=(ops_pre / pre_portion) if pre_portion > 0 else 0.0,
            phase="pre_hwm",
            allocations=allocations,
        )
        _bump_company(conn, company_id, ops=ops_pre)
    if ops_post > 0:
        _add_allocation(
            conn,
            event_id=event_id,
            company_id=company_id,
            beneficiary="company",
            bucket="ops",
            amount=ops_post,
            rate=(ops_post / post_portion) if post_portion > 0 else 0.0,
            phase="post_hwm",
            allocations=allocations,
        )
        _bump_company(conn, company_id, ops=ops_post)

    new_lifetime = lifetime + net
    new_phase = "post_hwm" if new_lifetime >= hwm else "pre_hwm"
    conn.execute(
        """UPDATE payroll_companies
           SET lifetime_net_usd = ?, phase = ?, updated_at = ?
           WHERE id = ?""",
        (new_lifetime, new_phase, now, company_id),
    )
    _sync_service_retainers(conn, company_id)

    product_id = company["saurce_product_id"]
    retainer_total = skim + sum(c for _, c, _ in percent_cuts)
    if retainer_total > 0:
        _try_saurce_ledger(
            conn,
            "payroll_retainer_accrual",
            product_id,
            net_amount=retainer_total,
            idempotency_key=f"{key}:retainer",
            meta={
                "eventId": event_id,
                "hwmSkimUsd": skim,
                "percentRetainers": [
                    {"retainerId": p["id"], "retainerName": p["name"], "amountUsd": c}
                    for p, c, _ in percent_cuts
                ],
            },
        )
    ops_total = ops_pool
    if ops_total > 0:
        _try_saurce_ledger(
            conn,
            "payroll_ops_retain",
            product_id,
            net_amount=ops_total,
            idempotency_key=f"{key}:ops",
            meta={"eventId": event_id},
        )

    conn.commit()
    return get_event(conn, event_id)


def _hwm_retainer_name(conn: sqlite3.Connection, company_id: str) -> str:
    row = conn.execute(
        """SELECT name FROM payroll_retainers
           WHERE company_id = ? AND kind = 'hwm_pct' LIMIT 1""",
        (company_id,),
    ).fetchone()
    if row and row["name"]:
        return str(row["name"])
    return "HWM retainer"


def get_event(conn: sqlite3.Connection, event_id: str) -> dict[str, Any]:
    row = conn.execute(
        "SELECT * FROM payroll_income_events WHERE id = ?", (event_id,)
    ).fetchone()
    if not row:
        raise KeyError("event_not_found")
    company_id = row["company_id"]
    hwm_name = _hwm_retainer_name(conn, company_id)
    name_by_id: dict[str, str] = {
        r["id"]: r["name"]
        for r in conn.execute(
            "SELECT id, name FROM payroll_retainers WHERE company_id = ?",
            (company_id,),
        ).fetchall()
    }
    allocs = []
    for a in conn.execute(
        """SELECT * FROM payroll_allocations
           WHERE event_id = ? ORDER BY bucket DESC, beneficiary, id""",
        (event_id,),
    ).fetchall():
        item = {
            "id": a["id"],
            "beneficiary": a["beneficiary"],
            "bucket": a["bucket"],
            "amountUsd": float(a["amount_usd"]),
            "rate": float(a["rate"]),
            "phase": a["phase"],
        }
        rid = a["retainer_id"] if "retainer_id" in a.keys() else None
        if rid:
            item["retainerId"] = rid
            item["retainerName"] = name_by_id.get(rid, rid)
        elif a["bucket"] == "retainer":
            item["retainerName"] = hwm_name
        allocs.append(item)
    r = dict(row)
    meta = json.loads(r["meta_json"]) if r.get("meta_json") else {}
    post_note = meta.get("postNote") or meta.get("note") or None
    return {
        "type": "income",
        "id": r["id"],
        "companyId": r["company_id"],
        "idempotencyKey": r["idempotency_key"],
        "source": r.get("source"),
        "grossUsd": r.get("gross_usd"),
        "netUsd": float(r["net_usd"]),
        "phaseApplied": r["phase_applied"],
        "preHwmPortionUsd": float(r["pre_hwm_portion_usd"]),
        "postHwmPortionUsd": float(r["post_hwm_portion_usd"]),
        "postNote": post_note,
        "meta": meta,
        "createdAt": r["created_at"],
        "allocations": allocs,
    }


def list_retainer_run_events(
    conn: sqlite3.Connection, company_id: str, *, limit: int = 50
) -> list[dict[str, Any]]:
    """Cron retainer accruals with retainer names for the events feed."""
    ensure_payroll_schema(conn)
    rows = conn.execute(
        """SELECT rr.id, rr.retainer_id, rr.company_id, rr.fire_key, rr.amount_usd,
                  rr.created_at, r.name AS retainer_name, r.kind AS retainer_kind
           FROM payroll_retainer_runs rr
           LEFT JOIN payroll_retainers r ON r.id = rr.retainer_id
           WHERE rr.company_id = ?
           ORDER BY rr.created_at DESC
           LIMIT ?""",
        (company_id, limit),
    ).fetchall()
    return [
        {
            "type": "retainer_accrual",
            "id": row["id"],
            "companyId": row["company_id"],
            "retainerId": row["retainer_id"],
            "retainerName": row["retainer_name"] or row["retainer_id"],
            "retainerKind": row["retainer_kind"],
            "amountUsd": float(row["amount_usd"]),
            "fireKey": row["fire_key"],
            "createdAt": row["created_at"],
            "allocations": [
                {
                    "beneficiary": "company",
                    "bucket": "retainer",
                    "amountUsd": float(row["amount_usd"]),
                    "retainerName": row["retainer_name"] or row["retainer_id"],
                    "retainerId": row["retainer_id"],
                }
            ],
        }
        for row in rows
    ]


def list_retainer_draw_events(
    conn: sqlite3.Connection, company_id: str, *, limit: int = 50
) -> list[dict[str, Any]]:
    """Retainer → ops draws (with reason) for the events feed."""
    ensure_payroll_schema(conn)
    if not _table_exists(conn, "payroll_retainer_draws"):
        return []
    rows = conn.execute(
        """SELECT id, company_id, beneficiary, amount_usd, reason, created_at
           FROM payroll_retainer_draws
           WHERE company_id = ?
           ORDER BY created_at DESC
           LIMIT ?""",
        (company_id, limit),
    ).fetchall()
    return [
        {
            "type": "retainer_draw",
            "id": row["id"],
            "companyId": row["company_id"],
            "beneficiary": row["beneficiary"],
            "amountUsd": float(row["amount_usd"]),
            "reason": row["reason"],
            "postNote": row["reason"],
            "createdAt": row["created_at"],
            "allocations": [
                {
                    "beneficiary": row["beneficiary"] or "company",
                    "bucket": "ops",
                    "amountUsd": float(row["amount_usd"]),
                    "fromBucket": "retainer",
                    "reason": row["reason"],
                }
            ],
        }
        for row in rows
    ]


def list_events(
    conn: sqlite3.Connection, company_id: str, *, limit: int = 50, offset: int = 0
) -> tuple[list[dict[str, Any]], int]:
    ensure_payroll_schema(conn)
    income_total = conn.execute(
        "SELECT COUNT(*) AS c FROM payroll_income_events WHERE company_id = ?",
        (company_id,),
    ).fetchone()["c"]
    run_total = 0
    if _table_exists(conn, "payroll_retainer_runs"):
        run_total = conn.execute(
            "SELECT COUNT(*) AS c FROM payroll_retainer_runs WHERE company_id = ?",
            (company_id,),
        ).fetchone()["c"]
    draw_total = 0
    if _table_exists(conn, "payroll_retainer_draws"):
        draw_total = conn.execute(
            "SELECT COUNT(*) AS c FROM payroll_retainer_draws WHERE company_id = ?",
            (company_id,),
        ).fetchone()["c"]
    # Fetch enough of each stream to fill the merged window after offset.
    fetch_n = max(limit + offset, limit)
    income_rows = conn.execute(
        """SELECT id FROM payroll_income_events
           WHERE company_id = ? ORDER BY created_at DESC LIMIT ?""",
        (company_id, fetch_n),
    ).fetchall()
    income = [get_event(conn, r["id"]) for r in income_rows]
    runs = list_retainer_run_events(conn, company_id, limit=fetch_n)
    draws = list_retainer_draw_events(conn, company_id, limit=fetch_n)
    merged = sorted(
        income + runs + draws,
        key=lambda e: e.get("createdAt") or "",
        reverse=True,
    )
    total = int(income_total) + int(run_total) + int(draw_total)
    return merged[offset : offset + limit], total


# --- Team members ---

def _member_dict(row: sqlite3.Row | dict) -> dict[str, Any]:
    r = dict(row)
    return {
        "id": r["id"],
        "companyId": r["company_id"],
        "displayName": r["display_name"],
        "email": r.get("email"),
        "resaurceEmployeeId": r.get("resaurce_employee_id"),
        "role": r.get("role") or "other",
        "isDesigner": bool(r.get("is_designer")),
        "isEngineer": bool(r.get("is_engineer")),
        "gameplay": bool(r.get("gameplay")),
        "technical": bool(r.get("technical")),
        "active": bool(r.get("active", 1)),
        "createdAt": r.get("created_at"),
        "updatedAt": r.get("updated_at"),
    }


def list_members(conn: sqlite3.Connection, company_id: str) -> list[dict[str, Any]]:
    ensure_payroll_schema(conn)
    rows = conn.execute(
        """SELECT * FROM payroll_team_members
           WHERE company_id = ? ORDER BY display_name COLLATE NOCASE""",
        (company_id,),
    ).fetchall()
    return [_member_dict(r) for r in rows]


def add_member(conn: sqlite3.Connection, company_id: str, body: dict[str, Any]) -> dict[str, Any]:
    ensure_payroll_schema(conn)
    if get_company(conn, company_id) is None:
        raise KeyError("company_not_found")
    name = (body.get("displayName") or body.get("name") or "").strip()
    if not name:
        raise ValueError("displayName required")
    role = (body.get("role") or "other").strip().lower()
    is_designer = bool(body.get("isDesigner", role == "designer"))
    is_engineer = bool(body.get("isEngineer", role == "engineer"))
    if "gameplay" in body:
        gameplay = bool(body["gameplay"])
    else:
        gameplay = is_designer
    if "technical" in body:
        technical = bool(body["technical"])
    else:
        technical = is_engineer
    mid = new_id("ptm")
    now = _now()
    conn.execute(
        """INSERT INTO payroll_team_members
           (id, company_id, display_name, email, resaurce_employee_id, role,
            is_designer, is_engineer, gameplay, technical, active, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            mid,
            company_id,
            name,
            body.get("email"),
            body.get("resaurceEmployeeId"),
            role,
            1 if is_designer else 0,
            1 if is_engineer else 0,
            1 if gameplay else 0,
            1 if technical else 0,
            0 if body.get("active") is False else 1,
            now,
            now,
        ),
    )
    _sync_service_retainers(conn, company_id)
    conn.commit()
    row = conn.execute("SELECT * FROM payroll_team_members WHERE id = ?", (mid,)).fetchone()
    return _member_dict(row)


def patch_member(
    conn: sqlite3.Connection, company_id: str, member_id: str, body: dict[str, Any]
) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    row = conn.execute(
        "SELECT * FROM payroll_team_members WHERE id = ? AND company_id = ?",
        (member_id, company_id),
    ).fetchone()
    if not row:
        return None
    r = dict(row)
    name = body.get("displayName", r["display_name"])
    email = body.get("email", r["email"])
    role = body.get("role", r["role"])
    is_designer = int(bool(body.get("isDesigner", r["is_designer"])))
    is_engineer = int(bool(body.get("isEngineer", r["is_engineer"])))
    gameplay = int(bool(body.get("gameplay", r["gameplay"])))
    technical = int(bool(body.get("technical", r["technical"])))
    active = int(bool(body.get("active", r["active"])))
    conn.execute(
        """UPDATE payroll_team_members SET
             display_name = ?, email = ?, role = ?, is_designer = ?, is_engineer = ?,
             gameplay = ?, technical = ?, active = ?, updated_at = ?
           WHERE id = ?""",
        (name, email, role, is_designer, is_engineer, gameplay, technical, active, _now(), member_id),
    )
    _sync_service_retainers(conn, company_id)
    conn.commit()
    return _member_dict(
        conn.execute("SELECT * FROM payroll_team_members WHERE id = ?", (member_id,)).fetchone()
    )


def delete_member(conn: sqlite3.Connection, company_id: str, member_id: str) -> bool:
    ensure_payroll_schema(conn)
    cur = conn.execute(
        "DELETE FROM payroll_team_members WHERE id = ? AND company_id = ?",
        (member_id, company_id),
    )
    if cur.rowcount:
        _sync_service_retainers(conn, company_id)
        conn.commit()
        return True
    return False


# --- Service budget ---

def _pricing(conn: sqlite3.Connection, company_id: str) -> dict[str, Any]:
    row = conn.execute(
        "SELECT * FROM payroll_service_pricing WHERE company_id = ? LIMIT 1",
        (company_id,),
    ).fetchone()
    if not row:
        row = conn.execute(
            "SELECT * FROM payroll_service_pricing WHERE company_id IS NULL LIMIT 1"
        ).fetchone()
    if not row:
        return {
            "cursorMonthlyUsd": DEFAULT_CURSOR_MONTHLY,
            "unityBands": DEFAULT_UNITY_BANDS,
        }
    return {
        "cursorMonthlyUsd": float(row["cursor_monthly_usd"]),
        "unityBands": json.loads(row["unity_bands_json"] or "[]") or DEFAULT_UNITY_BANDS,
    }


def unity_band_for_revenue(lifetime: float, bands: list[dict[str, Any]]) -> dict[str, Any]:
    chosen = bands[0] if bands else {"name": "free", "seatUsd": 0}
    for b in bands:
        mn = float(b.get("minLifetime") or 0)
        mx = b.get("maxLifetime")
        if lifetime >= mn and (mx is None or lifetime < float(mx)):
            chosen = b
    return chosen


def compute_service_budget(conn: sqlite3.Connection, company_id: str) -> dict[str, Any]:
    ensure_payroll_schema(conn)
    company = get_company(conn, company_id)
    if not company:
        raise KeyError("company_not_found")
    pricing = _pricing(conn, company_id)
    lifetime = company["lifetimeNetUsd"]
    band = unity_band_for_revenue(lifetime, pricing["unityBands"])
    members = [m for m in list_members(conn, company_id) if m["active"]]
    gameplay = [m for m in members if m["gameplay"]]
    technical = [m for m in members if m["technical"]]
    unity_seats = len(gameplay)
    cursor_seats = len(technical)
    enterprise = bool(band.get("custom"))
    if enterprise:
        seat_usd = company.get("unityEnterpriseOverrideUsd")
        if seat_usd is None:
            unity_monthly = None
        else:
            unity_monthly = float(seat_usd) * unity_seats
        seat_price = seat_usd
    else:
        seat_price = float(band.get("seatUsd") or 0)
        unity_monthly = seat_price * unity_seats
    cursor_unit = float(pricing["cursorMonthlyUsd"])
    cursor_monthly = cursor_unit * cursor_seats
    total = None
    if unity_monthly is not None:
        total = unity_monthly + cursor_monthly
    contact_label = None
    if enterprise:
        contact_label = band.get("contactLabel") or UNITY_ENTERPRISE_CONTACT_LABEL
    return {
        "companyId": company_id,
        "lifetimeNetUsd": lifetime,
        "unityBand": band.get("name"),
        "unityEnterprise": enterprise,
        "unityEnterpriseThresholdUsd": UNITY_ENTERPRISE_THRESHOLD_USD,
        "unityEnterpriseContactLabel": contact_label,
        "unitySeatUsd": seat_price,
        "unitySeats": unity_seats,
        "unityMonthlyUsd": unity_monthly,
        "cursorSeatUsd": cursor_unit,
        "cursorSeats": cursor_seats,
        "cursorMonthlyUsd": cursor_monthly,
        "totalMonthlyUsd": total,
        "gameplayMemberIds": [m["id"] for m in gameplay],
        "technicalMemberIds": [m["id"] for m in technical],
    }


def _update_service_retainer_amounts(conn: sqlite3.Connection, company_id: str) -> None:
    company = conn.execute(
        "SELECT * FROM payroll_companies WHERE id = ?", (company_id,)
    ).fetchone()
    if not company:
        return
    pricing = _pricing(conn, company_id)
    lifetime = float(company["lifetime_net_usd"])
    band = unity_band_for_revenue(lifetime, pricing["unityBands"])
    members = conn.execute(
        """SELECT id, gameplay, technical FROM payroll_team_members
           WHERE company_id = ? AND active = 1""",
        (company_id,),
    ).fetchall()
    g_ids = [m["id"] for m in members if m["gameplay"]]
    t_ids = [m["id"] for m in members if m["technical"]]
    enterprise = bool(band.get("custom"))
    if enterprise:
        ov = company["unity_enterprise_override_usd"]
        unity_monthly = float(ov) * len(g_ids) if ov is not None else 0.0
    else:
        unity_monthly = float(band.get("seatUsd") or 0) * len(g_ids)
    cursor_monthly = float(pricing["cursorMonthlyUsd"]) * len(t_ids)
    now = _now()
    # Always refresh user lists; only overwrite amount when not admin-locked.
    for kind, amount, user_ids in (
        ("service_unity", unity_monthly, g_ids),
        ("service_cursor", cursor_monthly, t_ids),
    ):
        row = conn.execute(
            """SELECT id, amount_locked FROM payroll_retainers
               WHERE company_id = ? AND kind = ? LIMIT 1""",
            (company_id, kind),
        ).fetchone()
        if not row:
            continue
        locked = bool(row["amount_locked"]) if "amount_locked" in row.keys() else False
        if locked:
            conn.execute(
                """UPDATE payroll_retainers SET user_ids_json = ?, updated_at = ?
                   WHERE id = ?""",
                (json.dumps(user_ids), now, row["id"]),
            )
        else:
            conn.execute(
                """UPDATE payroll_retainers SET amount_usd = ?, user_ids_json = ?, updated_at = ?
                   WHERE id = ?""",
                (amount, json.dumps(user_ids), now, row["id"]),
            )


def _sync_service_retainers(conn: sqlite3.Connection, company_id: str) -> None:
    _seed_service_retainers(conn, company_id)


# --- Retainers CRUD ---

def _retainer_dict(row: sqlite3.Row | dict) -> dict[str, Any]:
    r = dict(row)
    try:
        users = json.loads(r.get("user_ids_json") or "[]")
    except json.JSONDecodeError:
        users = []
    return {
        "id": r["id"],
        "companyId": r["company_id"],
        "name": r["name"],
        "kind": r["kind"],
        "mode": r["mode"],
        "percent": r.get("percent"),
        "amountUsd": r.get("amount_usd"),
        "amountLocked": bool(r.get("amount_locked", 0)),
        "cronExpr": r.get("cron_expr"),
        "forwardCompanyId": r.get("forward_company_id"),
        "forwardLabel": r.get("forward_label"),
        "userIds": users,
        "autoTrack": r.get("auto_track"),
        "enabled": bool(r.get("enabled", 1)),
        "createdAt": r.get("created_at"),
        "updatedAt": r.get("updated_at"),
    }


def list_retainers(conn: sqlite3.Connection, company_id: str) -> list[dict[str, Any]]:
    ensure_payroll_schema(conn)
    _sync_service_retainers(conn, company_id)
    rows = conn.execute(
        """SELECT * FROM payroll_retainers WHERE company_id = ?
           ORDER BY kind, name COLLATE NOCASE""",
        (company_id,),
    ).fetchall()
    return [_retainer_dict(r) for r in rows]


def _users_for_auto_track(conn: sqlite3.Connection, company_id: str, track: str | None) -> list[str]:
    if track == "gameplay":
        col = "gameplay"
    elif track == "technical":
        col = "technical"
    else:
        return []
    rows = conn.execute(
        f"""SELECT id FROM payroll_team_members
            WHERE company_id = ? AND active = 1 AND {col} = 1""",
        (company_id,),
    ).fetchall()
    return [r["id"] for r in rows]


def create_retainer(conn: sqlite3.Connection, company_id: str, body: dict[str, Any]) -> dict[str, Any]:
    ensure_payroll_schema(conn)
    if get_company(conn, company_id) is None:
        raise KeyError("company_not_found")
    name = (body.get("name") or "").strip()
    if not name:
        raise ValueError("name required")
    mode = body.get("mode") or "fixed_cron"
    if mode not in ("percent", "fixed_cron"):
        raise ValueError("mode must be percent|fixed_cron")
    kind = body.get("kind") or "custom"
    auto_track = body.get("autoTrack")
    if auto_track not in (None, "gameplay", "technical", ""):
        raise ValueError("autoTrack must be gameplay|technical|null")
    if auto_track == "":
        auto_track = None
    user_ids = body.get("userIds")
    if auto_track:
        user_ids = _users_for_auto_track(conn, company_id, auto_track)
    elif not isinstance(user_ids, list):
        user_ids = []
    rid = new_id("prt")
    now = _now()
    conn.execute(
        """INSERT INTO payroll_retainers
           (id, company_id, name, kind, mode, percent, amount_usd, cron_expr,
            forward_company_id, forward_label, user_ids_json, auto_track, enabled,
            created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            rid,
            company_id,
            name,
            kind,
            mode,
            float(body["percent"]) if body.get("percent") is not None else None,
            float(body["amountUsd"]) if body.get("amountUsd") is not None else None,
            body.get("cronExpr") or MONTHLY_CRON,
            body.get("forwardCompanyId"),
            body.get("forwardLabel"),
            json.dumps(user_ids),
            auto_track,
            0 if body.get("enabled") is False else 1,
            now,
            now,
        ),
    )
    conn.commit()
    return _retainer_dict(
        conn.execute("SELECT * FROM payroll_retainers WHERE id = ?", (rid,)).fetchone()
    )


def patch_retainer(
    conn: sqlite3.Connection, company_id: str, retainer_id: str, body: dict[str, Any]
) -> dict[str, Any] | None:
    ensure_payroll_schema(conn)
    row = conn.execute(
        "SELECT * FROM payroll_retainers WHERE id = ? AND company_id = ?",
        (retainer_id, company_id),
    ).fetchone()
    if not row:
        return None
    r = dict(row)
    name = body.get("name", r["name"])
    mode = body.get("mode", r["mode"])
    kind = body.get("kind", r["kind"])
    percent = body.get("percent", r["percent"])
    amount = body.get("amountUsd", r["amount_usd"])
    cron = body.get("cronExpr", r["cron_expr"])
    forward = body.get("forwardCompanyId", r["forward_company_id"])
    label = body.get("forwardLabel", r["forward_label"])
    auto_track = body.get("autoTrack", r["auto_track"])
    if auto_track == "":
        auto_track = None
    if "userIds" in body:
        user_ids = body["userIds"] if isinstance(body["userIds"], list) else []
    elif auto_track:
        user_ids = _users_for_auto_track(conn, company_id, auto_track)
    else:
        try:
            user_ids = json.loads(r.get("user_ids_json") or "[]")
        except json.JSONDecodeError:
            user_ids = []
    enabled = body.get("enabled", r["enabled"])
    # Service retainers keep amount synced from seats unless admin sets/locks amountUsd.
    amount_override = "amountUsd" in body or "amount_usd" in body
    if kind in ("service_unity", "service_cursor") and not amount_override:
        amount = r["amount_usd"]
    if amount_override:
        if amount is None or amount == "":
            raise ValueError("amountUsd required")
        amount = float(amount)
        if amount < 0:
            raise ValueError("amountUsd must be >= 0")
    locked = r.get("amount_locked", 0)
    if "amountLocked" in body or "amount_locked" in body:
        locked = body.get("amountLocked", body.get("amount_locked"))
        locked = 1 if locked else 0
    elif amount_override:
        locked = 1
    conn.execute(
        """UPDATE payroll_retainers SET
             name = ?, kind = ?, mode = ?, percent = ?, amount_usd = ?, amount_locked = ?,
             cron_expr = ?, forward_company_id = ?, forward_label = ?, user_ids_json = ?,
             auto_track = ?, enabled = ?, updated_at = ?
           WHERE id = ?""",
        (
            name,
            kind,
            mode,
            float(percent) if percent is not None else None,
            float(amount) if amount is not None else None,
            int(locked),
            cron,
            forward,
            label,
            json.dumps(user_ids),
            auto_track,
            1 if enabled else 0,
            _now(),
            retainer_id,
        ),
    )
    # Refresh user lists (and seat amounts only when unlocked).
    if kind in ("service_unity", "service_cursor"):
        _update_service_retainer_amounts(conn, company_id)
    conn.commit()
    return _retainer_dict(
        conn.execute("SELECT * FROM payroll_retainers WHERE id = ?", (retainer_id,)).fetchone()
    )


def delete_retainer(conn: sqlite3.Connection, company_id: str, retainer_id: str) -> bool:
    ensure_payroll_schema(conn)
    row = conn.execute(
        "SELECT kind FROM payroll_retainers WHERE id = ? AND company_id = ?",
        (retainer_id, company_id),
    ).fetchone()
    if not row:
        return False
    if row["kind"] in ("service_unity", "service_cursor", "hwm_pct"):
        raise ValueError("cannot delete system retainer; disable instead")
    conn.execute("DELETE FROM payroll_retainers WHERE id = ?", (retainer_id,))
    conn.commit()
    return True


def _cron_due(cron_expr: str | None, now: datetime) -> bool:
    if not cron_expr:
        return False
    try:
        from croniter import croniter

        base = now.replace(second=0, microsecond=0)
        itr = croniter(cron_expr, base)
        prev = itr.get_prev(datetime)
        return (base - prev).total_seconds() < 60
    except Exception:
        return False


def tick_retainers(conn: sqlite3.Connection, now: datetime | None = None) -> int:
    """Accrue due fixed_cron retainers. Returns number of runs recorded."""
    ensure_payroll_schema(conn)
    now = now or datetime.now(timezone.utc)
    fire_key = now.strftime("%Y-%m-%dT%H:%MZ")
    rows = conn.execute(
        """SELECT * FROM payroll_retainers
           WHERE enabled = 1 AND mode = 'fixed_cron'"""
    ).fetchall()
    n = 0
    for row in rows:
        r = dict(row)
        if not _cron_due(r.get("cron_expr"), now):
            continue
        amount = float(r.get("amount_usd") or 0)
        if amount <= 0:
            continue
        exists = conn.execute(
            """SELECT id FROM payroll_retainer_runs
               WHERE retainer_id = ? AND fire_key = ?""",
            (r["id"], fire_key),
        ).fetchone()
        if exists:
            continue
        target = r.get("forward_company_id") or r["company_id"]
        _bump_company(conn, target, retainer=amount)
        conn.execute(
            """INSERT INTO payroll_retainer_runs
               (id, retainer_id, company_id, fire_key, amount_usd, created_at)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (new_id("prr"), r["id"], r["company_id"], fire_key, amount, _now()),
        )
        product = conn.execute(
            "SELECT saurce_product_id FROM payroll_companies WHERE id = ?",
            (target,),
        ).fetchone()
        _try_saurce_ledger(
            conn,
            "payroll_retainer_accrual",
            product["saurce_product_id"] if product else None,
            net_amount=amount,
            idempotency_key=f"{r['id']}:{fire_key}",
            meta={
                "retainerId": r["id"],
                "retainerName": r.get("name"),
                "fireKey": fire_key,
            },
        )
        n += 1
    if n:
        conn.commit()
    return n


def company_summary(conn: sqlite3.Connection, company_id: str) -> dict[str, Any] | None:
    company = get_company(conn, company_id)
    if not company:
        return None
    bal = conn.execute(
        """SELECT ops_usd, retainer_usd, distributed_usd FROM payroll_beneficiary_balances
           WHERE company_id = ? AND beneficiary = 'company'""",
        (company_id,),
    ).fetchone()
    ops = float(bal["ops_usd"]) if bal else 0.0
    retainer = float(bal["retainer_usd"]) if bal else 0.0
    hwm = company["highWaterMarkUsd"]
    lifetime = company["lifetimeNetUsd"]
    progress = min(1.0, lifetime / hwm) if hwm > 0 else 1.0
    budget = compute_service_budget(conn, company_id)
    return {
        **company,
        "opsUsd": ops,
        "retainerUsd": retainer,
        "retainerTotalUsd": retainer,
        "hwmProgress": progress,
        "hwmRemainingUsd": max(0.0, hwm - lifetime),
        "serviceBudget": budget,
        "members": list_members(conn, company_id),
        "retainers": list_retainers(conn, company_id),
    }


def draw_retainer(
    conn: sqlite3.Connection,
    company_id: str,
    *,
    amount_usd: float,
    reason: str | None = None,
    beneficiary: str = "company",
) -> dict[str, Any]:
    ensure_payroll_schema(conn)
    amount = float(amount_usd)
    if amount <= 0:
        raise ValueError("amount must be > 0")
    bal = conn.execute(
        """SELECT retainer_usd FROM payroll_beneficiary_balances
           WHERE company_id = ? AND beneficiary = 'company'""",
        (company_id,),
    ).fetchone()
    if not bal:
        raise KeyError("company_not_found")
    available = float(bal["retainer_usd"])
    if amount > available + 1e-9:
        raise ValueError(f"insufficient retainer: have {available}")
    now = _now()
    did = new_id("prd")
    conn.execute(
        """INSERT INTO payroll_retainer_draws
           (id, company_id, beneficiary, amount_usd, reason, created_at)
           VALUES (?, ?, 'company', ?, ?, ?)""",
        (did, company_id, amount, reason, now),
    )
    _bump_company(conn, company_id, retainer=-amount, ops=amount)
    company = conn.execute(
        "SELECT saurce_product_id FROM payroll_companies WHERE id = ?",
        (company_id,),
    ).fetchone()
    _try_saurce_ledger(
        conn,
        "payroll_retainer_draw",
        company["saurce_product_id"] if company else None,
        net_amount=amount,
        idempotency_key=f"{did}:draw",
        meta={"drawId": did, "reason": reason},
    )
    conn.commit()
    return {
        "id": did,
        "companyId": company_id,
        "beneficiary": "company",
        "amountUsd": amount,
        "reason": reason,
        "createdAt": now,
        "summary": company_summary(conn, company_id),
    }


def maybe_post_income_for_product(
    conn: sqlite3.Connection,
    product_id: str | None,
    net_amount: float,
    *,
    source: str,
    idempotency_key: str,
    gross_amount: float | None = None,
    meta: dict[str, Any] | None = None,
) -> dict[str, Any] | None:
    if not product_id or net_amount is None:
        return None
    ensure_payroll_schema(conn)
    company = find_company_by_product(conn, product_id)
    if not company:
        return None
    return post_income(
        conn,
        company["id"],
        float(net_amount),
        gross_amount=gross_amount,
        source=source,
        idempotency_key=idempotency_key,
        meta=meta,
    )
