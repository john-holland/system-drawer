#!/usr/bin/env python3
"""Sync stories and schedule milestones to Google Calendar, iCal files, or Outlook (Graph)."""

from __future__ import annotations

import argparse
import json
import os
import sqlite3
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

_api = Path(__file__).resolve().parents[1]
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _ical_escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace(";", "\\;").replace(",", "\\,").replace("\n", "\\n")


def _resaurce_schedules() -> list[dict]:
    url = os.environ.get("RESAURCE_CAVE_URL", "http://127.0.0.1:3456").rstrip("/")
    import urllib.request

    body = json.dumps(
        {"route": "resaurce:production/schedule/list", "payload": {}, "trace_id": f"cal_{uuid.uuid4().hex[:8]}"}
    ).encode()
    req = urllib.request.Request(
        f"{url}/cave/route",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            data = json.loads(resp.read().decode())
            return data.get("production_schedules") or []
    except Exception:
        return []


def build_ics_events(conn: sqlite3.Connection, story_id: str | None = None) -> str:
    where, params = "calendar_start_date IS NOT NULL", []
    if story_id:
        where += " AND id = ?"
        params.append(story_id)
    rows = conn.execute(
        f"SELECT id, summary, description, calendar_start_date, calendar_end_date FROM stories WHERE {where}",
        params,
    ).fetchall()

    overlay = conn.execute(
        "SELECT custom_start_date, events_json FROM narrative_timeline_overlay ORDER BY updated_at DESC LIMIT 1"
    ).fetchone()
    overlay_events: list[dict] = []
    if overlay and overlay["events_json"]:
        try:
            overlay_events = json.loads(overlay["events_json"])
        except json.JSONDecodeError:
            pass

    lines = ["BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Continuuuum//Agile PM//EN"]
    for r in rows:
        uid = r["id"]
        start = (r["calendar_start_date"] or "").replace("-", "")[:8]
        end = (r["calendar_end_date"] or r["calendar_start_date"] or "").replace("-", "")[:8]
        lines.extend(
            [
                "BEGIN:VEVENT",
                f"UID:{uid}@continuuuum",
                f"DTSTAMP:{_now().replace('-', '').replace(':', '')}",
                f"DTSTART;VALUE=DATE:{start}",
                f"DTEND;VALUE=DATE:{end}",
                f"SUMMARY:{_ical_escape(r['summary'] or 'Story')}",
                f"DESCRIPTION:{_ical_escape(r['description'] or '')}",
                "END:VEVENT",
            ]
        )

    for sched in _resaurce_schedules():
        for m in sched.get("milestones") or []:
            if not m.get("start_date"):
                continue
            ms_id = m.get("id") or uuid.uuid4().hex[:8]
            start = str(m["start_date"]).replace("-", "")[:8]
            end = str(m.get("end_date") or m["start_date"]).replace("-", "")[:8]
            lines.extend(
                [
                    "BEGIN:VEVENT",
                    f"UID:ms_{ms_id}@continuuuum",
                    f"DTSTAMP:{_now().replace('-', '').replace(':', '')}",
                    f"DTSTART;VALUE=DATE:{start}",
                    f"DTEND;VALUE=DATE:{end}",
                    f"SUMMARY:{_ical_escape('Milestone: ' + (m.get('label') or 'Milestone'))}",
                    "END:VEVENT",
                ]
            )

    if overlay and overlay["custom_start_date"]:
        for i, ev in enumerate(overlay_events):
            label = ev.get("label") or ev.get("title") or f"Narrative event {i}"
            t = float(ev.get("t") or 0)
            base = datetime.fromisoformat(str(overlay["custom_start_date"])[:10])
            from datetime import timedelta

            day = base + timedelta(days=int(t))
            ds = day.strftime("%Y%m%d")
            lines.extend(
                [
                    "BEGIN:VEVENT",
                    f"UID:ntl_{i}@continuuuum",
                    f"DTSTAMP:{_now().replace('-', '').replace(':', '')}",
                    f"DTSTART;VALUE=DATE:{ds}",
                    f"DTEND;VALUE=DATE:{ds}",
                    f"SUMMARY:{_ical_escape(label)}",
                    "END:VEVENT",
                ]
            )

    lines.append("END:VCALENDAR")
    return "\r\n".join(lines) + "\r\n"


def _sync_google(conn: sqlite3.Connection, sub: sqlite3.Row, ics: str) -> dict:
    creds_ref = sub["oauth_token_ref"] or os.environ.get("GOOGLE_CALENDAR_CREDENTIALS")
    cal_id = sub["target_url"] or os.environ.get("GOOGLE_CALENDAR_ID", "primary")
    if not creds_ref:
        return {"ok": False, "message": "Google Calendar sync requires GOOGLE_CALENDAR_CREDENTIALS (not configured)"}
    try:
        from google.oauth2 import service_account
        from googleapiclient.discovery import build
    except ImportError:
        return {"ok": False, "message": "pip install google-api-python-client google-auth"}

    creds = service_account.Credentials.from_service_account_file(
        creds_ref,
        scopes=["https://www.googleapis.com/auth/calendar"],
    )
    service = build("calendar", "v3", credentials=creds)
    for line_block in ics.split("BEGIN:VEVENT"):
        if "SUMMARY:" not in line_block:
            continue
        summary = ""
        for ln in line_block.splitlines():
            if ln.startswith("SUMMARY:"):
                summary = ln[8:]
        if not summary:
            continue
        service.events().insert(
            calendarId=cal_id,
            body={
                "summary": summary,
                "description": "Continuuuum agile sync",
                "start": {"date": _extract_date(line_block, "DTSTART")},
                "end": {"date": _extract_date(line_block, "DTEND")},
            },
        ).execute()
    return {"ok": True, "message": "Google Calendar events inserted", "calendarId": cal_id}


def _extract_date(block: str, key: str) -> str:
    for ln in block.splitlines():
        if ln.startswith(key):
            raw = ln.split(":")[-1]
            if len(raw) >= 8:
                return f"{raw[0:4]}-{raw[4:6]}-{raw[6:8]}"
    return datetime.now(timezone.utc).strftime("%Y-%m-%d")


def _sync_outlook(conn: sqlite3.Connection, sub: sqlite3.Row, ics: str) -> dict:
    token = sub["oauth_token_ref"] or os.environ.get("MS_GRAPH_TOKEN")
    if not token:
        return {"ok": False, "message": "Outlook sync requires MS_GRAPH_TOKEN (not configured)"}
    import urllib.request

    user = sub["target_url"] or "me"
    count = 0
    for line_block in ics.split("BEGIN:VEVENT"):
        if "SUMMARY:" not in line_block:
            continue
        summary = next((ln[8:] for ln in line_block.splitlines() if ln.startswith("SUMMARY:")), "Event")
        start = _extract_date(line_block, "DTSTART")
        end = _extract_date(line_block, "DTEND")
        body = json.dumps(
            {
                "subject": summary,
                "body": {"contentType": "text", "content": "Continuuuum agile sync"},
                "start": {"dateTime": f"{start}T09:00:00", "timeZone": "UTC"},
                "end": {"dateTime": f"{end}T17:00:00", "timeZone": "UTC"},
            }
        ).encode()
        req = urllib.request.Request(
            f"https://graph.microsoft.com/v1.0/users/{user}/events",
            data=body,
            headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
            method="POST",
        )
        try:
            with urllib.request.urlopen(req, timeout=20):
                count += 1
        except Exception as e:
            return {"ok": False, "message": str(e), "inserted": count}
    return {"ok": True, "message": f"Outlook events inserted: {count}"}


def run_sync(
    get_conn,
    subscription_id: str | None = None,
    provider: str | None = None,
    output_dir: str | None = None,
) -> dict:
    conn = get_conn()
    where, params = "1=1", []
    if subscription_id:
        where += " AND id = ?"
        params.append(subscription_id)
    if provider:
        where += " AND provider = ?"
        params.append(provider)
    subs = conn.execute(f"SELECT * FROM calendar_sync_subscriptions WHERE {where}", params).fetchall()
    results = []
    out_dir = Path(output_dir or Path.cwd() / "calendar_export")
    out_dir.mkdir(parents=True, exist_ok=True)

    for sub in subs:
        prov = sub["provider"]
        log = {"subscriptionId": sub["id"], "provider": prov}
        try:
            ics = build_ics_events(conn, sub["story_id"])
            if prov == "ical":
                path = out_dir / f"sync_{sub['id']}.ics"
                path.write_text(ics, encoding="utf-8")
                log["path"] = str(path)
                log["ok"] = True
            elif prov == "google":
                log.update(_sync_google(conn, sub, ics))
            elif prov == "outlook":
                log.update(_sync_outlook(conn, sub, ics))
            else:
                log["ok"] = False
                log["message"] = f"Unknown provider {prov}"
        except Exception as e:
            log["ok"] = False
            log["message"] = str(e)
        conn.execute(
            "UPDATE calendar_sync_subscriptions SET last_sync_at = ?, last_sync_status = ?, last_sync_log = ? WHERE id = ?",
            (_now(), "ok" if log.get("ok") else "error", json.dumps(log), sub["id"]),
        )
        results.append(log)
    conn.commit()
    conn.close()
    return {"ok": all(r.get("ok") for r in results) if results else True, "results": results}


def main() -> int:
    parser = argparse.ArgumentParser(description="Continuuuum calendar sync")
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--subscription-id")
    parser.add_argument("--provider", choices=["ical", "google", "outlook"])
    parser.add_argument("--db", default=None)
    parser.add_argument("--output-dir", default=None)
    args = parser.parse_args()

    db_path = args.db or os.environ.get("CONTINUUUUM_DB", str(Path(__file__).resolve().parents[2] / "continuuuum.db"))

    def get_conn():
        c = sqlite3.connect(db_path)
        c.row_factory = sqlite3.Row
        return c

    result = run_sync(get_conn, args.subscription_id, args.provider, args.output_dir)
    print(json.dumps(result, indent=2))
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    raise SystemExit(main())
