"""
Test continuum_api.server database path resolution (SQLite file used by the Flask API).

Requires: pip install flask (same as running the API).

Run:
  cd Scripts
  pytest tests/test_continuum_api_sqlite_path.py -v
"""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import pytest

SCRIPTS_DIR = Path(__file__).resolve().parent.parent


@pytest.fixture(scope="module")
def continuum_server_module():
    pytest.importorskip("flask")
    server_path = SCRIPTS_DIR / "continuum_api" / "server.py"
    if not server_path.is_file():
        pytest.skip(f"Missing {server_path}")
    parent = str(SCRIPTS_DIR)
    if parent not in sys.path:
        sys.path.insert(0, parent)
    spec = importlib.util.spec_from_file_location("continuum_server", server_path)
    assert spec and spec.loader
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_get_db_path_uses_continuum_db_env(continuum_server_module, tmp_path, monkeypatch) -> None:
    custom = tmp_path / "custom_continuum.db"
    monkeypatch.setenv("CONTINUUM_DB", str(custom))
    assert continuum_server_module.get_db_path() == custom


def test_get_conn_opens_sqlite_file(continuum_server_module, tmp_path, monkeypatch) -> None:
    db_file = tmp_path / "conn_test.db"
    monkeypatch.setenv("CONTINUUM_DB", str(db_file))
    conn = continuum_server_module.get_conn()
    try:
        conn.execute("CREATE TABLE IF NOT EXISTS _ping (x INTEGER)")
        conn.execute("INSERT INTO _ping (x) VALUES (1)")
        conn.commit()
    finally:
        conn.close()
    assert db_file.is_file()
