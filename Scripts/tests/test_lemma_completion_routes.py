"""Tests for lemma completion seed/summary/patch API."""

from __future__ import annotations

import sqlite3
import sys
from pathlib import Path

import pytest
from flask import Flask

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from lemma_completion_db import (  # noqa: E402
    ensure_lemma_completion_schema,
    seed_lemma_completion,
    summary,
    upsert_definition,
)
from lemma_completion_routes import register_lemma_completion_routes  # noqa: E402


@pytest.fixture
def app_client(tmp_path):
    db = tmp_path / "continuuuum.db"

    def get_conn():
        conn = sqlite3.connect(db)
        conn.row_factory = sqlite3.Row
        ensure_lemma_completion_schema(conn)
        return conn

    app = Flask(__name__)
    app.config["TESTING"] = True
    register_lemma_completion_routes(app, get_conn)
    return app.test_client(), get_conn


def test_seed_and_summary(app_client):
    client, get_conn = app_client
    r = client.post("/api/lemma-completion/seed")
    assert r.status_code == 200
    data = r.get_json()
    assert data["ok"] is True
    assert data["inserted"] > 0
    assert data["summary"]["total"] >= 65

    r2 = client.get("/api/lemma-completion/summary?scope=primes")
    assert r2.status_code == 200
    primes = r2.get_json()
    assert primes["total"] == 65
    assert primes["builtin"] == 65

    r3 = client.get("/api/lemma-completion/summary?scope=common5000")
    assert r3.status_code == 200
    common = r3.get_json()
    assert common["total"] >= 4900


def test_patch_flags(app_client):
    client, get_conn = app_client
    conn = get_conn()
    seed_lemma_completion(conn)
    row = conn.execute(
        "SELECT id FROM lemma_completion WHERE term = ? LIMIT 1", ("the",)
    ).fetchone()
    conn.close()
    assert row is not None
    eid = row["id"]

    r = client.patch(
        f"/api/lemma-completion/entries/{eid}",
        json={
            "isImplemented": True,
            "benefitsFromAssetStore": True,
            "isBuiltin": True,
        },
    )
    assert r.status_code == 200
    body = r.get_json()
    assert body["isImplemented"] is True
    assert body["benefitsFromAssetStore"] is True
    assert body["isBuiltin"] is True


def test_list_filters_and_upsert_definition(app_client):
    client, get_conn = app_client
    conn = get_conn()
    seed_lemma_completion(conn)
    upsert_definition(
        conn,
        term="the",
        rank=1,
        nsm_definition="someone can say this word",
        descriptor={"lemma": "the", "posTag": "determiner", "mechanicalRole": "LiteralPrimitive"},
    )
    conn.close()

    r = client.get("/api/lemma-completion/entries?q=the&limit=10")
    assert r.status_code == 200
    data = r.get_json()
    assert data["total"] >= 1
    hit = next(i for i in data["items"] if i["term"] == "the")
    assert hit["isDefined"] is True
    assert hit["posTag"] == "determiner"

    r2 = client.get("/api/lemma-completion/entries?missingDefinition=1&limit=5")
    assert r2.status_code == 200
    assert all(not i["isDefined"] for i in r2.get_json()["items"])


def test_summary_percent_overall_counts_builtin_or_implemented(app_client):
    _, get_conn = app_client
    conn = get_conn()
    result = seed_lemma_completion(conn)
    assert result.get("glossesUpdated") == 65
    s = summary(conn, scope="primes")
    assert s["builtin"] == 65
    assert s["implemented"] == 65
    assert s["defined"] == 65
    assert s["progressed"] == 65
    assert s["percentOverall"] == 100.0
    s2 = summary(conn, scope="common5000")
    conn.close()
    assert s2["total"] >= 4900
    assert s2["percentOverall"] > 0
    assert s2["progressed"] >= s2["builtin"]


def test_spa_and_lemma_build_href_helper(app_client):
    client, _ = app_client
    r = client.get("/lemma-completion")
    assert r.status_code == 200
    html = r.get_data(as_text=True)
    assert "Lemma Completion" in html
    assert "lemma-completion.js" in html
    assert 'href="/lemma-build/"' in html
    assert "lc-primes-banner" in html
    assert "65 NSM primes" in html

    # Hotlink shape used by the SPA (mirrors lemma-build hydrate query)
    from urllib.parse import urlencode

    term = "open"
    pos = "verb"
    href = "/lemma-build/?" + urlencode({"lemma": term, "engine": "unity", "partOfSpeech": pos})
    assert href == "/lemma-build/?lemma=open&engine=unity&partOfSpeech=verb"


def test_list_is_prime_filter(app_client):
    client, get_conn = app_client
    conn = get_conn()
    seed_lemma_completion(conn)
    conn.close()
    r = client.get("/api/lemma-completion/entries?isPrime=1&limit=100")
    assert r.status_code == 200
    data = r.get_json()
    assert data["total"] == 65
    assert all(i["isPrime"] for i in data["items"])
