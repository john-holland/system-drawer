"""Optional cron worker: merge submitted localization change lists."""

from __future__ import annotations

import sqlite3
from datetime import datetime, timezone


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _in_submit_window(row: sqlite3.Row, now: datetime) -> bool:
    opens = row["submit_window_opens_at"] if "submit_window_opens_at" in row.keys() else None
    closes = row["submit_window_closes_at"] if "submit_window_closes_at" in row.keys() else None
    if not opens and not closes:
        return True
    ts = now.strftime("%Y-%m-%dT%H:%M:%SZ")
    if opens and ts < opens:
        return False
    if closes and ts > closes:
        return False
    return True


def _cron_due(cron_expr: str | None, now: datetime) -> bool:
    if not cron_expr:
        return True
    try:
        from croniter import croniter
        base = now.replace(second=0, microsecond=0)
        itr = croniter(cron_expr, base)
        prev = itr.get_prev(datetime)
        return (base - prev).total_seconds() < 60
    except Exception:
        return True


def process_submitted(conn: sqlite3.Connection) -> int:
    """Move eligible submitted lists to merged. Returns count merged."""
    now_dt = datetime.now(timezone.utc)
    now = _now()
    cur = conn.execute(
        "SELECT id, submit_schedule_cron, submit_window_opens_at, submit_window_closes_at FROM localization_change_lists WHERE workflow_status = 'submitted'"
    )
    rows = cur.fetchall()
    merged = 0
    for row in rows:
        if not _in_submit_window(row, now_dt):
            continue
        if not _cron_due(row["submit_schedule_cron"] if "submit_schedule_cron" in row.keys() else None, now_dt):
            continue
        conn.execute(
            "UPDATE localization_change_lists SET workflow_status = 'merged', merged_at = ?, updated_at = ? WHERE id = ?",
            (now, now, row["id"]),
        )
        merged += 1
    conn.commit()
    return merged


if __name__ == "__main__":
    import os
    import sys

    db = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("CONTINUUM_DB", "continuum.db")
    c = sqlite3.connect(db)
    c.row_factory = sqlite3.Row
    n = process_submitted(c)
    print(f"merged {n} change lists")
