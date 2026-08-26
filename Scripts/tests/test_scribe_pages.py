"""Tests for scribe document configs, pages, and anchors."""

from __future__ import annotations

import os
import sqlite3
import sys
import tempfile
from pathlib import Path

_scripts = Path(__file__).resolve().parents[1]
_api = _scripts / "continuuuum_api"
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))

from scribe_db import (  # noqa: E402
    ensure_scribe_schema,
    get_page,
    list_configs,
    upsert_anchor,
    upsert_config,
    upsert_page,
)


def _mem():
    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)
    conn = sqlite3.connect(path)
    ensure_scribe_schema(conn)
    return conn, path


def test_scribe_config_page_anchor_roundtrip():
    conn, path = _mem()
    try:
        cfg = upsert_config(
            conn,
            config_id="charter",
            title="Charter",
            fmt="odt",
            pecking_order=4,
            tenant="campus",
        )
        conn.commit()
        assert cfg["id"] == "charter"
        assert cfg["format"] == "odt"
        assert cfg["peckingOrder"] == 4
        assert any(c["id"] == "charter" for c in list_configs(conn, "campus"))

        page = upsert_page(
            conn,
            config_id="charter",
            page_index=0,
            body_text="Article I",
            surface_kind="pen-ink",
        )
        conn.commit()
        assert page["bodyText"] == "Article I"
        assert page["pageIndex"] == 0

        anchor = upsert_anchor(
            conn,
            page_id=page["id"],
            anchor_key="art-1",
            kind="bookmark",
            char_start=0,
            char_end=9,
        )
        conn.commit()
        loaded = get_page(conn, "charter", 0)
        assert loaded is not None
        assert any(a["anchorKey"] == "art-1" and a["kind"] == "bookmark" for a in loaded["anchors"])
        assert anchor["kind"] == "bookmark"
    finally:
        conn.close()
        os.unlink(path)


def test_scribe_rejects_unknown_format():
    conn, path = _mem()
    try:
        try:
            upsert_config(conn, config_id="bad", title="Bad", fmt="xlsx")
            assert False, "expected ValueError"
        except ValueError as e:
            assert "unsupported_format" in str(e)
    finally:
        conn.close()
        os.unlink(path)


def test_scribe_http_routes():
    from flask import Flask
    from scribe_routes import register_scribe_routes

    fd, path = tempfile.mkstemp(suffix=".db")
    os.close(fd)

    def get_conn():
        return sqlite3.connect(path)

    app = Flask(__name__)
    register_scribe_routes(app, get_conn, lambda: "u1")
    client = app.test_client()
    r = client.post("/api/scribe/configs", json={"id": "c1", "title": "Tome", "format": "docx", "peckingOrder": 6})
    assert r.status_code == 200, r.get_data(as_text=True)
    assert r.get_json()["config"]["format"] == "docx"
    r2 = client.post("/api/scribe/configs/c1/pages", json={"pageIndex": 0, "bodyText": "Hello"})
    assert r2.status_code == 200
    page_id = r2.get_json()["page"]["id"]
    r3 = client.post(f"/api/scribe/pages/{page_id}/anchors", json={"anchorKey": "bm", "kind": "bookmark"})
    assert r3.status_code == 200
    r4 = client.get("/api/scribe/configs/c1/pages/0")
    assert r4.status_code == 200
    assert r4.get_json()["page"]["bodyText"] == "Hello"
    os.unlink(path)
