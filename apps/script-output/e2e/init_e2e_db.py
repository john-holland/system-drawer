"""Initialize an isolated SQLite DB for script-output Playwright tests."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "Scripts"
DB_PATH = Path(__file__).resolve().parent / "fixtures" / "e2e.db"

if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

from continuuuum_api.localization_helpers import ensure_script_output_tables  # noqa: E402
from continuuuum_api.thesaurus_db import ensure_thesaurus_schema  # noqa: E402


def main() -> None:
    DB_PATH.parent.mkdir(parents=True, exist_ok=True)
    if DB_PATH.is_file():
        DB_PATH.unlink()
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row

    for name in (
        "continuuuum_dictionary_schema.sql",
        "continuuuum_draft_schema.sql",
        "continuuuum_episodes_schema.sql",
        "continuuuum_script_output_schema.sql",
        "continuuuum_localization_workflow_schema.sql",
        "continuuuum_review_schema.sql",
    ):
        path = SCRIPTS / name
        if path.is_file():
            conn.executescript(path.read_text(encoding="utf-8"))

    ensure_thesaurus_schema(conn)
    ensure_script_output_tables(conn)
    conn.commit()
    conn.close()
    print(f"E2E DB ready at {DB_PATH}")


if __name__ == "__main__":
    main()
