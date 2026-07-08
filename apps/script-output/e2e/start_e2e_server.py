"""Create the E2E SQLite DB if needed, then start continuuuum_api.server."""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "Scripts"
DB_PATH = Path(__file__).resolve().parent / "fixtures" / "e2e.db"
PORT = os.environ.get("CONTINUUUUM_E2E_PORT", "5051")


def ensure_db() -> None:
    if DB_PATH.is_file() and DB_PATH.stat().st_size > 0:
        return
    DB_PATH.parent.mkdir(parents=True, exist_ok=True)
    if DB_PATH.is_file():
        DB_PATH.unlink(missing_ok=True)
    init = Path(__file__).resolve().parent / "init_e2e_db.py"
    subprocess.check_call([sys.executable, str(init)], cwd=str(init.parent))


def main() -> None:
    ensure_db()
    os.chdir(SCRIPTS)
    env = os.environ.copy()
    env["CONTINUUUUM_DB"] = str(DB_PATH)
    subprocess.check_call(
        [
            sys.executable,
            "-m",
            "continuuuum_api.server",
            "--db",
            str(DB_PATH),
            "--port",
            PORT,
            "--host",
            "127.0.0.1",
        ],
        cwd=str(SCRIPTS),
        env=env,
    )


if __name__ == "__main__":
    main()
