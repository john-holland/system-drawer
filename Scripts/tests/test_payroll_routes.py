"""Tests for Continuuuum company payroll (HWM %, seats, retainers)."""

from __future__ import annotations

import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import patch

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from payroll_engine import (  # noqa: E402
    add_member,
    company_summary,
    compute_service_budget,
    create_company,
    create_retainer,
    draw_retainer,
    ensure_payroll_schema,
    list_events,
    list_retainers,
    post_income,
    tick_retainers,
)
from payroll_routes import register_payroll_routes  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "payroll.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_payroll_schema(conn)
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_payroll_routes(app, get_conn)
    return app.test_client(), get_conn


def test_create_and_summary_defaults(app_client):
    client, _ = app_client
    r = client.post(
        "/api/payroll/companies",
        json={"name": "Resaurce Co", "highWaterMarkUsd": 1000},
    )
    assert r.status_code == 201
    c = r.get_json()
    assert c["phase"] == "pre_hwm"
    assert abs(c["hwmRetainerPct"] - 0.10) < 1e-9
    assert "savingIndexes" not in c
    assert "postHwmShares" not in c

    s = client.get(f"/api/payroll/companies/{c['id']}/summary").get_json()
    assert s["retainerTotalUsd"] == 0
    assert s["hwmRemainingUsd"] == 1000
    kinds = {r["kind"] for r in s["retainers"]}
    assert "service_unity" in kinds
    assert "service_cursor" in kinds


def test_free_until_100k_migration_one_shot(tmp_path):
    """Pre-prod migration rewrites HWM model once and clears old ledger rows."""
    db = tmp_path / "mig.db"
    conn = sqlite3.connect(db)
    conn.row_factory = sqlite3.Row
    # Bootstrap tables without running migration marker
    conn.executescript(
        """
        CREATE TABLE payroll_companies (
          id TEXT PRIMARY KEY,
          name TEXT NOT NULL,
          saurce_product_id TEXT,
          high_water_mark_usd REAL NOT NULL DEFAULT 100000000,
          hwm_retainer_pct REAL NOT NULL DEFAULT 0.12,
          lifetime_net_usd REAL NOT NULL DEFAULT 0,
          phase TEXT NOT NULL DEFAULT 'pre_hwm',
          currency TEXT NOT NULL DEFAULT 'USD',
          unity_enterprise_override_usd REAL,
          created_at TEXT NOT NULL,
          updated_at TEXT NOT NULL
        );
        CREATE TABLE payroll_beneficiary_balances (
          id TEXT PRIMARY KEY,
          company_id TEXT NOT NULL,
          beneficiary TEXT NOT NULL,
          ops_usd REAL NOT NULL DEFAULT 0,
          retainer_usd REAL NOT NULL DEFAULT 0,
          distributed_usd REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE payroll_income_events (
          id TEXT PRIMARY KEY,
          company_id TEXT NOT NULL,
          idempotency_key TEXT NOT NULL,
          source TEXT,
          gross_usd REAL,
          net_usd REAL NOT NULL,
          phase_applied TEXT NOT NULL,
          pre_hwm_portion_usd REAL NOT NULL DEFAULT 0,
          post_hwm_portion_usd REAL NOT NULL DEFAULT 0,
          meta_json TEXT,
          created_at TEXT NOT NULL
        );
        CREATE TABLE payroll_allocations (
          id TEXT PRIMARY KEY,
          event_id TEXT NOT NULL,
          company_id TEXT NOT NULL,
          beneficiary TEXT NOT NULL,
          bucket TEXT NOT NULL,
          amount_usd REAL NOT NULL,
          rate REAL NOT NULL,
          phase TEXT NOT NULL,
          retainer_id TEXT
        );
        CREATE TABLE payroll_saving_indexes (
          id TEXT PRIMARY KEY,
          company_id TEXT NOT NULL,
          beneficiary TEXT NOT NULL,
          rate REAL NOT NULL
        );
        CREATE TABLE payroll_post_hwm_shares (
          id TEXT PRIMARY KEY,
          company_id TEXT NOT NULL,
          beneficiary TEXT NOT NULL,
          rate REAL NOT NULL
        );
        """
    )
    conn.execute(
        """INSERT INTO payroll_companies
           (id, name, high_water_mark_usd, hwm_retainer_pct, lifetime_net_usd, phase, currency, created_at, updated_at)
           VALUES ('pc_old', 'Legacy Co', 100000000, 0.12, 50000, 'pre_hwm', 'USD', 't', 't')"""
    )
    conn.execute(
        """INSERT INTO payroll_beneficiary_balances
           (id, company_id, beneficiary, ops_usd, retainer_usd, distributed_usd)
           VALUES ('b1', 'pc_old', 'company', 44000, 6000, 0)"""
    )
    conn.execute(
        """INSERT INTO payroll_income_events
           (id, company_id, idempotency_key, net_usd, phase_applied, created_at)
           VALUES ('e1', 'pc_old', 'k', 50000, 'pre_hwm', 't')"""
    )
    conn.execute(
        """INSERT INTO payroll_allocations
           (id, event_id, company_id, beneficiary, bucket, amount_usd, rate, phase)
           VALUES ('a1', 'e1', 'pc_old', 'company', 'retainer', 6000, 0.12, 'pre_hwm')"""
    )
    conn.execute(
        "INSERT INTO payroll_saving_indexes (id, company_id, beneficiary, rate) VALUES ('s1','pc_old','company',0.1)"
    )
    conn.commit()

    ensure_payroll_schema(conn)
    row = conn.execute("SELECT * FROM payroll_companies WHERE id='pc_old'").fetchone()
    assert abs(row["high_water_mark_usd"] - 100_000) < 1e-6
    assert abs(row["hwm_retainer_pct"] - 0.10) < 1e-9
    assert abs(row["lifetime_net_usd"] - 0) < 1e-9
    assert row["phase"] == "pre_hwm"
    assert conn.execute("SELECT COUNT(*) AS n FROM payroll_income_events").fetchone()["n"] == 0
    assert conn.execute("SELECT COUNT(*) AS n FROM payroll_allocations").fetchone()["n"] == 0
    bal = conn.execute(
        "SELECT * FROM payroll_beneficiary_balances WHERE company_id='pc_old'"
    ).fetchone()
    assert abs(bal["ops_usd"]) < 1e-9 and abs(bal["retainer_usd"]) < 1e-9
    assert not conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name='payroll_saving_indexes'"
    ).fetchone()
    assert not conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name='payroll_post_hwm_shares'"
    ).fetchone()
    meta = conn.execute(
        "SELECT value FROM payroll_meta WHERE key='hwm_model'"
    ).fetchone()
    assert meta["value"] == "free_until_100k_v1"

    # Idempotent: admin override survives a second ensure
    conn.execute(
        "UPDATE payroll_companies SET high_water_mark_usd=555, hwm_retainer_pct=0.2 WHERE id='pc_old'"
    )
    conn.commit()
    ensure_payroll_schema(conn)
    row2 = conn.execute("SELECT * FROM payroll_companies WHERE id='pc_old'").fetchone()
    assert abs(row2["high_water_mark_usd"] - 555) < 1e-6
    assert abs(row2["hwm_retainer_pct"] - 0.2) < 1e-9
    conn.close()


def test_pre_hwm_is_free(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1_000_000)
    event = post_income(conn, company["id"], 10_000, source="manual", idempotency_key="k1")
    assert event["phaseApplied"] == "pre_hwm"
    by_ben = {
        (a["beneficiary"], a["bucket"]): a["amountUsd"] for a in event["allocations"]
    }
    # Free until HWM: 100% → ops, no HWM retainer skim
    assert ("company", "retainer") not in by_ben
    assert abs(by_ben[("company", "ops")] - 10_000) < 1e-6
    assert ("cursor", "retainer") not in by_ben
    assert ("unity", "retainer") not in by_ben
    summary = company_summary(conn, company["id"])
    assert abs(summary["retainerTotalUsd"] - 0) < 1e-6
    assert abs(summary["lifetimeNetUsd"] - 10_000) < 1e-6
    conn.close()


def test_post_hwm_10pct_retainer(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=100)
    post_income(conn, company["id"], 100, idempotency_key="fill")
    event = post_income(conn, company["id"], 1000, idempotency_key="post")
    assert event["phaseApplied"] == "post_hwm"
    by_ben = {
        (a["beneficiary"], a["bucket"]): a["amountUsd"] for a in event["allocations"]
    }
    # 10% of 1000 → retainer; 90% → ops
    assert abs(by_ben[("company", "retainer")] - 100) < 1e-6
    assert abs(by_ben[("company", "ops")] - 900) < 1e-6
    assert ("company", "distributed") not in by_ben
    conn.close()


def test_hwm_crossing_split(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1000)
    post_income(conn, company["id"], 800, idempotency_key="a")
    event = post_income(conn, company["id"], 500, idempotency_key="b")
    assert event["phaseApplied"] == "crossing"
    assert abs(event["preHwmPortionUsd"] - 200) < 1e-6
    assert abs(event["postHwmPortionUsd"] - 300) < 1e-6
    # Pre 200 → free ops; post 300 → 10% = 30 retainer, 270 ops
    retainer = sum(
        a["amountUsd"] for a in event["allocations"] if a["bucket"] == "retainer"
    )
    assert abs(retainer - 30) < 1e-6
    ops_pre = next(
        a["amountUsd"]
        for a in event["allocations"]
        if a["bucket"] == "ops" and a.get("phase") == "pre_hwm"
    )
    ops_post = next(
        a["amountUsd"]
        for a in event["allocations"]
        if a["bucket"] == "ops" and a.get("phase") == "post_hwm"
    )
    assert abs(ops_pre - 200) < 1e-6
    assert abs(ops_post - 270) < 1e-6
    summary = company_summary(conn, company["id"])
    assert summary["phase"] == "post_hwm"
    assert abs(summary["lifetimeNetUsd"] - 1300) < 1e-6
    conn.close()


def test_idempotency(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1e9)
    e1 = post_income(conn, company["id"], 100, idempotency_key="same")
    e2 = post_income(conn, company["id"], 100, idempotency_key="same")
    assert e1["id"] == e2["id"]
    summary = company_summary(conn, company["id"])
    assert abs(summary["lifetimeNetUsd"] - 100) < 1e-6
    conn.close()


def test_service_budget_free_and_pro_and_cursor(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1e9)
    add_member(conn, company["id"], {"displayName": "A", "role": "designer"})
    add_member(conn, company["id"], {"displayName": "B", "role": "engineer"})
    add_member(
        conn,
        company["id"],
        {"displayName": "C", "role": "other", "gameplay": True, "technical": True},
    )
    budget = compute_service_budget(conn, company["id"])
    assert budget["unityBand"] == "free"
    assert budget["unitySeats"] == 2  # designer + C
    assert budget["cursorSeats"] == 2  # engineer + C
    assert budget["unityMonthlyUsd"] == 0
    assert abs(budget["cursorMonthlyUsd"] - 80) < 1e-6

    # Cross into Pro band ($200k+)
    post_income(conn, company["id"], 200_000, idempotency_key="rev")
    budget = compute_service_budget(conn, company["id"])
    assert budget["unityBand"] == "pro"
    assert abs(budget["unitySeatUsd"] - 210) < 1e-6
    assert abs(budget["unityMonthlyUsd"] - 420) < 1e-6
    assert abs(budget["totalMonthlyUsd"] - 500) < 1e-6  # 420 + 80

    retainers = {r["kind"]: r for r in list_retainers(conn, company["id"])}
    assert abs(retainers["service_unity"]["amountUsd"] - 420) < 1e-6
    assert abs(retainers["service_cursor"]["amountUsd"] - 80) < 1e-6
    assert set(retainers["service_unity"]["userIds"]) == set(budget["gameplayMemberIds"])
    assert set(retainers["service_cursor"]["userIds"]) == set(budget["technicalMemberIds"])
    conn.close()


def test_member_api_auto_association(app_client):
    client, _ = app_client
    c = client.post("/api/payroll/companies", json={"name": "Co"}).get_json()
    cid = c["id"]
    m = client.post(
        f"/api/payroll/companies/{cid}/members",
        json={"displayName": "Tech Designer", "role": "designer", "technical": True},
    ).get_json()
    assert m["gameplay"] is True  # designer soft default
    assert m["technical"] is True
    budget = client.get(f"/api/payroll/companies/{cid}/service-budget").get_json()
    assert budget["unitySeats"] == 1
    assert budget["cursorSeats"] == 1


def test_retainer_cron_accrual(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1e9)
    create_retainer(
        conn,
        company["id"],
        {
            "name": "Custom float",
            "mode": "fixed_cron",
            "amountUsd": 40,
            "cronExpr": "0 0 1 * *",
            "kind": "custom",
        },
    )
    fire_at = datetime(2026, 8, 1, 0, 0, tzinfo=timezone.utc)
    with patch("payroll_engine._cron_due", return_value=True):
        n = tick_retainers(conn, now=fire_at)
    assert n >= 1
    # Idempotent same fire window
    with patch("payroll_engine._cron_due", return_value=True):
        n2 = tick_retainers(conn, now=fire_at)
    assert n2 == 0
    summary = company_summary(conn, company["id"])
    assert summary["retainerUsd"] >= 40
    conn.close()


def test_percent_retainer_on_income_with_name_and_post_note(app_client):
    client, get_conn = app_client
    conn = get_conn()
    # HWM already cleared so income is post-HWM (10% skim) + custom % retainer
    company = create_company(conn, name="Springfield", high_water_mark_usd=0)
    create_retainer(
        conn,
        company["id"],
        {
            "name": "eshielda",
            "mode": "percent",
            "percent": 0.05,
            "kind": "custom",
        },
    )
    conn.close()

    r = client.post(
        f"/api/payroll/companies/{company['id']}/income",
        json={
            "netUsd": 10_000_000,
            "source": "grant",
            "postNote": "clean up the Springfield dustbowl",
        },
    )
    assert r.status_code == 201
    event = r.get_json()
    assert event["postNote"] == "clean up the Springfield dustbowl"
    by_name = {
        a.get("retainerName"): a["amountUsd"]
        for a in event["allocations"]
        if a["bucket"] == "retainer"
    }
    assert "eshielda" in by_name
    assert abs(by_name["eshielda"] - 500_000) < 1e-6  # 5% of 10M
    assert "HWM retainer" in by_name
    assert abs(by_name["HWM retainer"] - 1_000_000) < 1e-6  # 10% of 10M
    ops = sum(a["amountUsd"] for a in event["allocations"] if a["bucket"] == "ops")
    assert abs(ops - 8_500_000) < 1e-6


def test_events_include_retainer_names(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=0)
    create_retainer(
        conn,
        company["id"],
        {
            "name": "Custom float",
            "mode": "fixed_cron",
            "amountUsd": 40,
            "cronExpr": "0 0 1 * *",
            "kind": "custom",
        },
    )
    post_income(conn, company["id"], 10_000, idempotency_key="inc1")
    fire_at = datetime(2026, 8, 1, 0, 0, tzinfo=timezone.utc)
    with patch("payroll_engine._cron_due", return_value=True):
        tick_retainers(conn, now=fire_at)

    items, total = list_events(conn, company["id"], limit=20)
    assert total >= 2
    income = next(e for e in items if e.get("type") == "income")
    retainer_alloc = next(a for a in income["allocations"] if a["bucket"] == "retainer")
    assert retainer_alloc["retainerName"] == "HWM retainer"

    accruals = [e for e in items if e.get("type") == "retainer_accrual"]
    assert accruals
    names = {e["retainerName"] for e in accruals}
    assert "Custom float" in names or "Unity (gameplay)" in names or "Cursor (technical)" in names
    for e in accruals:
        assert e["allocations"][0]["retainerName"] == e["retainerName"]
    conn.close()


def test_retainer_draw(app_client):
    client, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=0)
    # 10% of 10_000 = 1000 retainer; 9000 ops
    post_income(conn, company["id"], 10_000, idempotency_key="inc")
    conn.close()

    r = client.post(
        f"/api/payroll/companies/{company['id']}/retainer/draw",
        json={"amountUsd": 100, "reason": "coverage"},
    )
    assert r.status_code == 201
    body = r.get_json()
    assert abs(body["amountUsd"] - 100) < 1e-6
    assert abs(body["summary"]["retainerUsd"] - 900) < 1e-6  # 1000 - 100
    assert abs(body["summary"]["opsUsd"] - 9100) < 1e-6  # 9000 + 100

    events = client.get(f"/api/payroll/companies/{company['id']}/events?limit=20").get_json()
    draws = [e for e in events["items"] if e.get("type") == "retainer_draw"]
    assert draws
    assert draws[0]["reason"] == "coverage"
    assert abs(draws[0]["amountUsd"] - 100) < 1e-6

    deny = client.post(
        f"/api/payroll/companies/{company['id']}/retainer/draw",
        json={"amountUsd": 99999},
    )
    assert deny.status_code == 400


def test_spa_shell(app_client):
    client, _ = app_client
    r = client.get("/payroll")
    assert r.status_code == 200
    html = r.get_data(as_text=True)
    assert "Company payroll" in html
    assert "payroll.js" in html
    assert "shared/cron/cron-humanize.js" in html
    assert "continuuuum-user-session.js" in html
    assert "highWaterMarkUsd" in html
    assert "gameplay" in html


def test_admin_required_to_change_hwm_and_retainer_pct(app_client):
    client, _ = app_client
    c = client.post(
        "/api/payroll/companies",
        json={"name": "Co", "highWaterMarkUsd": 1000, "hwmRetainerPct": 0.12},
    ).get_json()
    cid = c["id"]

    denied = client.patch(
        f"/api/payroll/companies/{cid}",
        json={"highWaterMarkUsd": 5000, "hwmRetainerPct": 0.2},
    )
    assert denied.status_code == 403

    ok = client.patch(
        f"/api/payroll/companies/{cid}",
        json={"highWaterMarkUsd": 5000, "hwmRetainerPct": 0.2},
        headers={"X-Admin": "1"},
    )
    assert ok.status_code == 200
    body = ok.get_json()
    assert abs(body["highWaterMarkUsd"] - 5000) < 1e-6
    assert abs(body["hwmRetainerPct"] - 0.2) < 1e-9

    # Non-sensitive company fields still patchable without admin
    name_ok = client.patch(
        f"/api/payroll/companies/{cid}",
        json={"name": "Renamed Co"},
    )
    assert name_ok.status_code == 200
    assert name_ok.get_json()["name"] == "Renamed Co"


def test_admin_required_to_change_retainer_amounts(app_client):
    client, _ = app_client
    c = client.post("/api/payroll/companies", json={"name": "Co"}).get_json()
    cid = c["id"]
    ret = client.post(
        f"/api/payroll/companies/{cid}/retainers",
        json={
            "name": "Custom float",
            "mode": "percent",
            "percent": 0.05,
            "kind": "custom",
        },
    ).get_json()
    rid = ret["id"]

    denied = client.patch(
        f"/api/payroll/companies/{cid}/retainers/{rid}",
        json={"percent": 0.08},
    )
    assert denied.status_code == 403

    ok = client.patch(
        f"/api/payroll/companies/{cid}/retainers/{rid}",
        json={"percent": 0.08},
        headers={"X-Admin": "1"},
    )
    assert ok.status_code == 200
    assert abs(ok.get_json()["percent"] - 0.08) < 1e-9


def test_unity_enterprise_contact_label_at_25m(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1e9)
    post_income(conn, company["id"], 25_000_000, idempotency_key="ent")
    budget = compute_service_budget(conn, company["id"])
    assert budget["unityBand"] == "enterprise"
    assert budget["unityEnterprise"] is True
    assert budget["unityEnterpriseThresholdUsd"] == 25_000_000
    assert "Unity Finance" in budget["unityEnterpriseContactLabel"]
    assert "Business" in budget["unityEnterpriseContactLabel"]
    conn.close()


def test_admin_unity_amount_lock_survives_summary_sync(app_client):
    """Service Unity amount used to reset to $0 (free band) on every list/summary."""
    client, _ = app_client
    c = client.post("/api/payroll/companies", json={"name": "Co"}).get_json()
    cid = c["id"]
    client.post(
        f"/api/payroll/companies/{cid}/members",
        json={"displayName": "Designer", "role": "designer"},
    )
    retainers = client.get(f"/api/payroll/companies/{cid}/summary").get_json()["retainers"]
    unity = next(r for r in retainers if r["kind"] == "service_unity")
    assert unity["amountUsd"] == 0  # free band

    patched = client.patch(
        f"/api/payroll/companies/{cid}/retainers/{unity['id']}",
        json={"amountUsd": 420, "amountLocked": True},
        headers={"X-Admin": "1"},
    )
    assert patched.status_code == 200
    assert abs(patched.get_json()["amountUsd"] - 420) < 1e-6
    assert patched.get_json()["amountLocked"] is True

    again = client.get(f"/api/payroll/companies/{cid}/summary").get_json()
    unity2 = next(r for r in again["retainers"] if r["kind"] == "service_unity")
    assert abs(unity2["amountUsd"] - 420) < 1e-6
    assert unity2["amountLocked"] is True

    unlocked = client.patch(
        f"/api/payroll/companies/{cid}/retainers/{unity['id']}",
        json={"amountLocked": False},
        headers={"X-Admin": "1"},
    )
    assert unlocked.status_code == 200
    # Unlock recalculates from seats (still free band → 0)
    assert unlocked.get_json()["amountUsd"] == 0
    assert unlocked.get_json()["amountLocked"] is False


def test_draw_retainer_engine_insufficient(app_client):
    _, get_conn = app_client
    conn = get_conn()
    company = create_company(conn, name="Co", high_water_mark_usd=1e9)
    with pytest.raises(ValueError):
        draw_retainer(conn, company["id"], amount_usd=1)
    conn.close()
