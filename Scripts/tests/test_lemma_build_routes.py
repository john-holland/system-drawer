"""Tests for lemma build settings, engines, extract, and chat concurrency."""

from __future__ import annotations

import json
import sqlite3
import sys
from pathlib import Path
from unittest.mock import patch

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from lemma_build_db import ensure_lemma_build_schema, load_system_preface  # noqa: E402
from lemma_build_routes import (  # noqa: E402
    extract_code_files,
    parse_descriptor,
    register_lemma_build_routes,
)


@pytest.fixture
def app_client(tmp_path, monkeypatch):
    db = tmp_path / "continuuuum.db"
    batch = tmp_path / "batches"
    batch.mkdir()

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_lemma_build_schema(conn)
        return conn

    conn = get_conn()
    conn.execute(
        """UPDATE lemma_build_settings
           SET batch_output_dir = ?, max_concurrent_builds = 1
           WHERE tenant_id = 'default'""",
        (str(batch),),
    )
    conn.commit()
    conn.close()

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_lemma_build_routes(app, get_conn)
    return app.test_client(), batch


def test_settings_admin_gate(app_client):
    client, _ = app_client
    r = client.get("/api/lemma-build/settings")
    assert r.status_code == 200
    data = r.get_json()
    assert "defaultModelId" in data
    assert "lmStudioBaseUrl" not in data

    r2 = client.put(
        "/api/lemma-build/settings",
        json={"defaultModelId": "x", "defaultEngine": "haxe"},
    )
    assert r2.status_code == 403

    r3 = client.put(
        "/api/lemma-build/settings",
        headers={"X-Admin": "1"},
        json={
            "defaultModelId": "codestral-test",
            "defaultEngine": "haxe",
            "maxConcurrentBuilds": 2,
            "batchOutputDir": "Library/LemmaBuild/batches",
            "lmStudioBaseUrl": "http://127.0.0.1:1234/v1",
        },
    )
    assert r3.status_code == 200
    body = r3.get_json()
    assert body["defaultModelId"] == "codestral-test"
    assert body["defaultEngine"] == "haxe"
    assert body["lmStudioBaseUrl"].endswith("/v1")


def test_unreal_engine_rejected(app_client):
    client, _ = app_client
    r = client.post("/api/lemma-build/sessions", json={"lemmaPhrase": "x", "engine": "unreal"})
    assert r.status_code == 400
    assert "unreal" in r.get_json()["error"]


def test_unity_vs_haxe_preface():
    unity = load_system_preface("unity")
    haxe = load_system_preface("haxe")
    assert "Unity" in unity or "C#" in unity or "asmdef" in unity.lower()
    assert "Haxe" in haxe
    assert unity != haxe
    assert "LemmaBuild" in unity or len(unity) > 40


def test_parse_descriptor_and_extract():
    text = """
Here is a descriptor:
```json lemma-mechanism-descriptor
{"lemma":"open","posTag":"verb","mechanicalRole":"AtomicAction","outputTier":0}
```
And code:
```csharp path=generated/OpenLemma.cs
public class OpenLemma {}
```
"""
    desc = parse_descriptor(text)
    assert desc["lemma"] == "open"
    files = extract_code_files(text)
    assert any(p.endswith("OpenLemma.cs") for p, _ in files)

    tool_files = extract_code_files(
        "",
        tool_calls=[
            {
                "function": {
                    "name": "write_file",
                    "arguments": json.dumps({"path": "generated/Foo.hx", "content": "class Foo {}"}),
                }
            }
        ],
    )
    assert tool_files[0][0] == "generated/Foo.hx"


def test_chat_writes_chat_txt_and_concurrency(app_client):
    client, batch = app_client

    fake = {
        "choices": [
            {
                "message": {
                    "content": "```csharp path=generated/A.cs\nclass A{}\n```\n",
                    "tool_calls": [],
                }
            }
        ]
    }

    with patch("lemma_build_routes._chat_completions", return_value=fake):
        sess = client.post(
            "/api/lemma-build/sessions",
            json={"lemmaPhrase": "open", "engine": "unity"},
        )
        assert sess.status_code == 201
        sid = sess.get_json()["id"]

        r = client.post(
            "/api/lemma-build/chat",
            json={
                "sessionId": sid,
                "engine": "unity",
                "messages": [{"role": "user", "content": "build open"}],
            },
        )
        assert r.status_code == 200
        data = r.get_json()
        assert "assistant" in data
        chat_path = Path(data["batchDir"]) / "chat.txt"
        assert chat_path.is_file()
        assert "build open" in chat_path.read_text(encoding="utf-8")
        assert any("A.cs" in f for f in data.get("filesWritten") or [])

    # Concurrency: hold one active slot then expect 429
    import lemma_build_routes as lbr

    lbr._active_chats["default"] = 1
    try:
        with patch("lemma_build_routes._chat_completions", return_value=fake):
            r429 = client.post(
                "/api/lemma-build/chat",
                json={"engine": "unity", "messages": [{"role": "user", "content": "x"}]},
            )
        assert r429.status_code == 429
    finally:
        lbr._active_chats["default"] = 0


def test_engines_catalog(app_client):
    client, _ = app_client
    r = client.get("/api/lemma-build/engines")
    assert r.status_code == 200
    engines = {e["id"]: e for e in r.get_json()["engines"]}
    assert engines["unity"]["enabled"] is True
    assert engines["haxe"]["enabled"] is True
    assert engines["unreal"]["enabled"] is False
