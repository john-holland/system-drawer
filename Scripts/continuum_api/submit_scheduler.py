"""Optional cron worker: merge submitted localization change lists."""

from __future__ import annotations

import sqlite3
from datetime import datetime, timezone


def process_submitted(conn: sqlite3.Connection) -> int:
    """Move eligible submitted lists to merged. Returns count merged."""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    cur = conn.execute(
        "SELECT id FROM localization_change_lists WHERE workflow_status = 'submitted'"
    )
    ids = [r[0] for r in cur.fetchall()]
    for cid in ids:
        conn.execute(
            "UPDATE localization_change_lists SET workflow_status = 'merged', merged_at = ?, updated_at = ? WHERE id = ?",
            (now, now, cid),
        )
    conn.commit()
    return len(ids)


if __name__ == "__main__":
    import os
    import sys

    db = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("CONTINUUM_DB", "continuum.db")
    c = sqlite3.connect(db)
    n = process_submitted(c)
    print(f"merged {n} change lists")
