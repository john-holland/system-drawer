#!/usr/bin/env python3
"""Publish budget plan, journal, schedule, stories, and work orders to Google Sheets."""

from __future__ import annotations

import json
import os
import sqlite3
import urllib.error
import urllib.request
import uuid
from pathlib import Path
from typing import Any

RESAURCE_CAVE_URL = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
CONTINUUM_DB = os.environ.get("CONTINUUM_DB", str(Path(__file__).resolve().parents[2] / "continuum.db"))


def _cave(route: str, payload: dict) -> dict[str, Any]:
    body = json.dumps(
        {"route": f"resaurce:{route}", "payload": payload, "trace_id": f"sheets_{uuid.uuid4().hex[:10]}"}
    ).encode()
    req = urllib.request.Request(
        f"{RESAURCE_CAVE_URL}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.loads(resp.read().decode())


def _load_stories_for_plan(plan_id: str) -> list[dict]:
    conn = sqlite3.connect(CONTINUUM_DB)
    conn.row_factory = sqlite3.Row
    rows = conn.execute(
        "SELECT id, summary, status, story_value, resaurce_schedule_id, resaurce_budget_plan_id FROM stories WHERE resaurce_budget_plan_id = ?",
        (plan_id,),
    ).fetchall()
    conn.close()
    return [dict(r) for r in rows]


def _load_work_orders(story_ids: list[str]) -> list[dict]:
    if not story_ids:
        return []
    conn = sqlite3.connect(CONTINUUM_DB)
    conn.row_factory = sqlite3.Row
    placeholders = ",".join("?" * len(story_ids))
    rows = conn.execute(
        f"""SELECT wo.id, wo.story_id, wo.status, wo.asset_kind, wo.asset_ref_json
            FROM work_orders wo
            WHERE wo.story_id IN ({placeholders})""",
        story_ids,
    ).fetchall()
    conn.close()
    return [dict(r) for r in rows]


def _sheets_api_update(spreadsheet_id: str, tabs: dict[str, list[list[Any]]], dry_run: bool) -> dict[str, Any]:
    creds_path = os.environ.get("GOOGLE_SHEETS_CREDENTIALS")
    if not creds_path or not Path(creds_path).exists():
        export_dir = Path(os.environ.get("SHEETS_EXPORT_DIR", Path.cwd() / "sheets_export"))
        export_dir.mkdir(parents=True, exist_ok=True)
        paths = {}
        for name, rows in tabs.items():
            path = export_dir / f"{spreadsheet_id}_{name}.json"
            path.write_text(json.dumps(rows, indent=2), encoding="utf-8")
            paths[name] = str(path)
        return {
            "ok": True,
            "mode": "dry_run_file_export",
            "message": "GOOGLE_SHEETS_CREDENTIALS not configured; wrote JSON exports",
            "paths": paths,
        }
    if dry_run:
        return {"ok": True, "mode": "dry_run", "tabs": list(tabs.keys()), "rowCounts": {k: len(v) for k, v in tabs.items()}}

    try:
        from google.oauth2 import service_account
        from googleapiclient.discovery import build
    except ImportError:
        return {"ok": False, "error": "google_api_client_not_installed", "message": "pip install google-api-python-client google-auth"}

    scopes = ["https://www.googleapis.com/auth/spreadsheets"]
    creds = service_account.Credentials.from_service_account_file(creds_path, scopes=scopes)
    service = build("sheets", "v4", credentials=creds)
    sheet = service.spreadsheets()

    for tab_name, rows in tabs.items():
        if not rows:
            continue
        range_name = f"{tab_name}!A1"
        sheet.values().update(
            spreadsheetId=spreadsheet_id,
            range=range_name,
            valueInputOption="RAW",
            body={"values": rows},
        ).execute()
    return {"ok": True, "mode": "google_sheets", "spreadsheetId": spreadsheet_id}


def build_tabs(plan_id: str, all_linked: bool = True) -> dict[str, list[list[Any]]]:
    plan_out = _cave("production/budget/get", {"budget_plan_id": plan_id})
    plan = plan_out.get("budget_plan") or {}
    journal_out = _cave("production/budget/journal/list", {"budget_plan_id": plan_id})
    entries = journal_out.get("journal_entries") or []

    schedules = _cave("production/schedule/list", {}).get("production_schedules") or []
    linked_schedules = [s for s in schedules if s.get("budget_plan_id") == plan_id]

    stories = _load_stories_for_plan(plan_id)
    if all_linked and not stories:
        stories_rows = _load_all_stories()
        stories = stories_rows
    wo = _load_work_orders([s["id"] for s in stories])

    budget_tab = [
        ["field", "value"],
        ["id", plan.get("id")],
        ["name", plan.get("name")],
        ["capacity_usd", plan.get("capacity_usd")],
        ["water_level_usd", plan.get("water_level_usd")],
        ["saurce_product_id", plan.get("saurce_product_id")],
    ]
    journal_tab = [["id", "entry_type", "debit", "credit", "amount_usd", "story_id", "memo"]]
    for e in entries:
        journal_tab.append(
            [
                e.get("id"),
                e.get("entry_type"),
                e.get("debit_account"),
                e.get("credit_account"),
                e.get("amount_usd"),
                e.get("story_id"),
                e.get("memo"),
            ]
        )
    schedule_tab = [["schedule_id", "milestone", "start", "end", "story_ids"]]
    for s in linked_schedules:
        for m in s.get("milestones") or []:
            schedule_tab.append(
                [
                    s.get("id"),
                    m.get("label"),
                    m.get("start_date"),
                    m.get("end_date"),
                    json.dumps(m.get("continuum_story_ids") or []),
                ]
            )
    stories_tab = [["id", "summary", "status", "story_value", "schedule_id"]]
    for s in stories:
        stories_tab.append([s.get("id"), s.get("summary"), s.get("status"), s.get("story_value"), s.get("resaurce_schedule_id")])
    wo_tab = [["id", "story_id", "status", "asset_kind", "asset_ref_json"]]
    for w in wo:
        wo_tab.append([w.get("id"), w.get("story_id"), w.get("status"), w.get("asset_kind"), w.get("asset_ref_json")])
    assets_tab = [["kind", "ref", "label", "cost_usd"]]
    for a in plan.get("asset_list") or []:
        assets_tab.append([a.get("kind"), json.dumps(a.get("ref")), a.get("label"), a.get("cost_usd")])

    return {
        "Budget": budget_tab,
        "Journal": journal_tab,
        "Schedule": schedule_tab,
        "Stories": stories_tab,
        "WorkOrders": wo_tab,
        "Assets": assets_tab,
    }


def _load_all_stories() -> list[dict]:
    conn = sqlite3.connect(CONTINUUM_DB)
    conn.row_factory = sqlite3.Row
    rows = conn.execute(
        "SELECT id, summary, status, story_value, resaurce_schedule_id, resaurce_budget_plan_id FROM stories LIMIT 500"
    ).fetchall()
    conn.close()
    return [dict(r) for r in rows]


def publish_budget_plan(plan_id: str, options: dict | None = None) -> dict[str, Any]:
    options = options or {}
    dry_run = bool(options.get("dryRun") or options.get("dry_run"))
    spreadsheet_id = (
        options.get("spreadsheetId")
        or options.get("spreadsheet_id")
        or os.environ.get("GOOGLE_SHEETS_SPREADSHEET_ID")
        or f"local_{plan_id}"
    )
    try:
        tabs = build_tabs(plan_id, all_linked=options.get("allLinked", options.get("all_linked", True)))
        return _sheets_api_update(spreadsheet_id, tabs, dry_run)
    except urllib.error.URLError as e:
        return {"ok": False, "error": "resaurce_unavailable", "detail": str(e)}


def main() -> int:
    import argparse

    parser = argparse.ArgumentParser(description="Publish production data to Google Sheets")
    parser.add_argument("--budget-plan", required=True)
    parser.add_argument("--all-linked", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    result = publish_budget_plan(
        args.budget_plan,
        {"all_linked": args.all_linked, "dry_run": args.dry_run},
    )
    print(json.dumps(result, indent=2))
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    raise SystemExit(main())
