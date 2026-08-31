"""Provider verification: Continuuuum Flask honors the Minecraftuuuum UCC pact.

Consumer: minecraftuuuum Spring UCC (`ContinuuuumTenantClient`).
Provider: this continuuuum_api (payroll split, oauth connections, library search).

Pact file: `minecraftuuuum/pacts/minecraftuuuum-continuuuum.json` (written by
`gradlew :spring-server:test`). Override with `MINECRAFTUUUUM_PACT`.
"""

from __future__ import annotations

import json
import os
import re
import sqlite3
import sys
from pathlib import Path
from typing import Any
from urllib.parse import urlencode

from flask import Flask, jsonify, request

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from payroll_engine import ensure_minecraftuuuum_tenant_payroll, ensure_payroll_schema  # noqa: E402
from payroll_routes import register_payroll_routes  # noqa: E402
from vote_routes import ensure_vote_tables, register_vote_routes  # noqa: E402

PACT_NAME = "minecraftuuuum-continuuuum.json"


def _pact_path() -> Path:
    env = os.environ.get("MINECRAFTUUUUM_PACT")
    if env:
        p = Path(env)
        if p.is_file():
            return p
        raise FileNotFoundError(f"MINECRAFTUUUUM_PACT is not a file: {p}")
    candidates = (
        Path(r"D:\Development\prompt-to-minecraft\minecraftuuuum\pacts") / PACT_NAME,
        ROOT / "pacts" / PACT_NAME,
        Path(__file__).resolve().parents[3]
        / "Development"
        / "prompt-to-minecraft"
        / "minecraftuuuum"
        / "pacts"
        / PACT_NAME,
    )
    for p in candidates:
        if p.is_file():
            return p
    raise FileNotFoundError(
        f"Pact {PACT_NAME} not found. Run `gradlew :spring-server:test` in minecraftuuuum "
        "or set MINECRAFTUUUUM_PACT."
    )


def _make_provider(db_path: Path) -> Flask:
    app = Flask("continuuuum-pact-provider")

    def get_conn() -> sqlite3.Connection:
        conn = sqlite3.connect(db_path)
        conn.row_factory = sqlite3.Row
        return conn

    bootstrap = get_conn()
    ensure_payroll_schema(bootstrap)
    ensure_vote_tables(bootstrap)
    ensure_minecraftuuuum_tenant_payroll(bootstrap, "minecraftuuuum")
    bootstrap.commit()
    bootstrap.close()

    register_payroll_routes(app, get_conn)
    register_vote_routes(app, get_conn)

    @app.route("/api/library/search")
    def library_search():
        """Empty array is a valid USC search; used for UCC reachability."""
        _ = request.args.get("limit")
        _ = request.headers.get("X-Tenant-ID")
        return jsonify([])

    return app


def _flatten_headers(raw: Any) -> dict[str, str]:
    if not raw:
        return {}
    out: dict[str, str] = {}
    for key, value in raw.items():
        out[key] = value[0] if isinstance(value, list) else str(value)
    return out


def _request_path(req: dict) -> str:
    path = req["path"]
    query = req.get("query")
    if not query:
        return path
    if isinstance(query, str):
        return f"{path}?{query}"
    items: list[tuple[str, str]] = []
    for key, value in query.items():
        if isinstance(value, list):
            items.extend((key, str(v)) for v in value)
        else:
            items.append((key, str(value)))
    return f"{path}?{urlencode(items)}"


def _rules_for(matching_rules: dict, path: str) -> list[dict]:
    body_rules = matching_rules.get("body") or matching_rules
    node = body_rules.get(path) or body_rules.get(path.replace("$.", "$."))
    if not node:
        # V3: {"body": {"$.foo": {"matchers": [...]}}}
        return []
    if isinstance(node, dict) and "matchers" in node:
        return list(node["matchers"])
    if isinstance(node, dict) and "match" in node:
        return [node]
    return []


def _match_type(actual: Any, example: Any) -> None:
    if example is None:
        assert actual is None
        return
    if isinstance(example, bool):
        assert isinstance(actual, bool), f"expected bool, got {type(actual)}"
        return
    if isinstance(example, int) and not isinstance(example, bool):
        assert isinstance(actual, (int, float)) and not isinstance(actual, bool)
        return
    if isinstance(example, float):
        assert isinstance(actual, (int, float)) and not isinstance(actual, bool)
        return
    if isinstance(example, str):
        assert isinstance(actual, str)
        return
    if isinstance(example, list):
        assert isinstance(actual, list)
        return
    if isinstance(example, dict):
        assert isinstance(actual, dict)
        return


def _apply_matcher(matcher: dict, actual: Any, example: Any) -> None:
    kind = matcher.get("match")
    if kind in (None, "equality"):
        assert actual == example, f"{actual!r} != {example!r}"
        return
    if kind in ("type", "number", "integer", "decimal"):
        if kind == "integer":
            assert isinstance(actual, int) and not isinstance(actual, bool)
        elif kind in ("number", "decimal"):
            assert isinstance(actual, (int, float)) and not isinstance(actual, bool)
        else:
            _match_type(actual, example)
        if "min" in matcher and isinstance(actual, list):
            assert len(actual) >= int(matcher["min"])
        return
    if kind == "regex":
        assert isinstance(actual, str)
        assert re.search(matcher["regex"], actual)
        return
    if kind == "include":
        assert matcher.get("value", "") in str(actual)
        return
    _match_type(actual, example)


def _walk_expected(actual: Any, expected: Any, matching_rules: dict, path: str) -> None:
    rules = _rules_for(matching_rules, path)
    if rules:
        _apply_matcher(rules[0], actual, expected)
        if isinstance(expected, dict) and isinstance(actual, dict):
            for key, val in expected.items():
                child = f"{path}.{key}" if path != "$" else f"$.{key}"
                assert key in actual, f"missing {child}"
                _walk_expected(actual[key], val, matching_rules, child)
        elif isinstance(expected, list) and expected and isinstance(actual, list):
            for i, val in enumerate(expected):
                _walk_expected(actual[i], val, matching_rules, f"{path}[{i}]")
        return
    if isinstance(expected, dict):
        assert isinstance(actual, dict), f"{path}: expected object"
        for key, val in expected.items():
            child = f"{path}.{key}" if path != "$" else f"$.{key}"
            assert key in actual, f"missing {child}"
            _walk_expected(actual[key], val, matching_rules, child)
        return
    if isinstance(expected, list):
        assert isinstance(actual, list), f"{path}: expected array"
        min_items = 0
        star = _rules_for(matching_rules, f"{path}[*]")
        for matcher in star:
            if "min" in matcher:
                min_items = int(matcher["min"])
        assert len(actual) >= min_items
        if not expected:
            # Empty example with min 0: any list is ok; exact empty when no min-type.
            if min_items == 0 and not star:
                assert actual == []
            return
        for i, val in enumerate(expected):
            _walk_expected(actual[i], val, matching_rules, f"{path}[{i}]")
        return
    assert actual == expected, f"{path}: {actual!r} != {expected!r}"


def _response_matching_rules(interaction: dict) -> dict:
    resp = interaction["response"]
    rules = resp.get("matchingRules") or {}
    if "body" in rules:
        return rules
    return {"body": rules}


def test_minecraftuuuum_continuuuum_pact(tmp_path):
    pact_file = _pact_path()
    pact = json.loads(pact_file.read_text(encoding="utf-8"))
    assert pact["consumer"]["name"] == "minecraftuuuum"
    assert pact["provider"]["name"] == "continuuuum"

    app = _make_provider(tmp_path / "pact.db")
    client = app.test_client()
    failures: list[str] = []

    for interaction in pact["interactions"]:
        desc = interaction.get("description", "?")
        req = interaction["request"]
        expected = interaction["response"]
        url = _request_path(req)
        headers = _flatten_headers(req.get("headers"))
        try:
            resp = client.open(url, method=req["method"], headers=headers)
            status = expected["status"]
            assert resp.status_code == status, f"status {resp.status_code} != {status}"
            body = expected.get("body")
            if body is not None:
                actual = resp.get_json(silent=True)
                if actual is None and resp.data:
                    actual = json.loads(resp.data.decode("utf-8"))
                _walk_expected(actual, body, _response_matching_rules(interaction), "$")
        except AssertionError as exc:
            failures.append(f"{desc}: {exc}")

    assert not failures, "Pact provider mismatches:\n" + "\n".join(failures)
