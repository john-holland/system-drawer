"""Lemma completion tracking API + SPA shell."""

from __future__ import annotations

import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuuuum_api.lemma_completion_db import (
        ensure_lemma_completion_schema,
        list_entries,
        patch_entry,
        seed_lemma_completion,
        summary,
        sync_builtin_implementation,
    )
except ImportError:
    from lemma_completion_db import (
        ensure_lemma_completion_schema,
        list_entries,
        patch_entry,
        seed_lemma_completion,
        summary,
        sync_builtin_implementation,
    )

GetConn = Callable[[], sqlite3.Connection]


def _bool_arg(name: str) -> bool | None:
    raw = request.args.get(name)
    if raw is None or raw == "":
        return None
    return str(raw).lower() in ("1", "true", "yes")


def _int_arg(name: str) -> int | None:
    raw = request.args.get(name)
    if raw is None or raw == "":
        return None
    try:
        return int(raw)
    except (TypeError, ValueError):
        return None


def register_lemma_completion_routes(app: Any, get_conn: GetConn) -> None:
    static_dir = Path(__file__).resolve().parent / "static" / "lemma-completion"

    @app.route("/lemma-completion")
    @app.route("/lemma-completion/")
    @app.route("/lemma-completion/<path:subpath>")
    def lemma_completion_spa(subpath: str = ""):
        from flask import send_from_directory

        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")

    def _ensure_seeded(conn: sqlite3.Connection, lang: str) -> None:
        ensure_lemma_completion_schema(conn)
        n = conn.execute(
            "SELECT COUNT(*) AS c FROM lemma_completion WHERE language_code = ?",
            (lang,),
        ).fetchone()["c"]
        if int(n) == 0:
            seed_lemma_completion(conn, language_code=lang)

    @app.route("/api/lemma-completion/summary", methods=["GET"])
    def lemma_completion_summary():
        scope = request.args.get("scope") or "all"
        if scope not in ("all", "common5000", "primes"):
            return jsonify({"error": "scope must be all|common5000|primes"}), 400
        lang = request.args.get("language") or "en"
        conn = get_conn()
        try:
            _ensure_seeded(conn, lang)
            return jsonify(summary(conn, scope=scope, language_code=lang))
        finally:
            conn.close()

    @app.route("/api/lemma-completion/entries", methods=["GET"])
    def lemma_completion_entries():
        lang = request.args.get("language") or "en"
        q = request.args.get("q") or None
        limit = _int_arg("limit") or 50
        offset = _int_arg("offset") or 0
        limit = max(1, min(limit, 500))
        offset = max(0, offset)
        conn = get_conn()
        try:
            _ensure_seeded(conn, lang)
            items, total = list_entries(
                conn,
                language_code=lang,
                q=q,
                missing_definition=bool(_bool_arg("missingDefinition")),
                not_implemented=bool(_bool_arg("notImplemented")),
                asset_store=_bool_arg("assetStore"),
                is_builtin=_bool_arg("isBuiltin"),
                is_prime=_bool_arg("isPrime"),
                has_rank=_bool_arg("hasRank"),
                rank_min=_int_arg("rankMin"),
                rank_max=_int_arg("rankMax"),
                limit=limit,
                offset=offset,
            )
            return jsonify({"items": items, "total": total, "limit": limit, "offset": offset})
        finally:
            conn.close()

    @app.route("/api/lemma-completion/entries/<entry_id>", methods=["PATCH"])
    def lemma_completion_patch(entry_id: str):
        body = request.get_json(silent=True) or {}
        conn = get_conn()
        try:
            updated = patch_entry(conn, entry_id, body)
            if updated is None:
                return jsonify({"error": "not_found"}), 404
            return jsonify(updated)
        finally:
            conn.close()

    @app.route("/api/lemma-completion/seed", methods=["POST"])
    def lemma_completion_seed():
        lang = (request.get_json(silent=True) or {}).get("language") or request.args.get("language") or "en"
        conn = get_conn()
        try:
            result = seed_lemma_completion(conn, language_code=lang)
            stats = summary(conn, scope="all", language_code=lang)
            return jsonify({"ok": True, **result, "summary": stats})
        finally:
            conn.close()

    @app.route("/api/lemma-completion/sync-builtins", methods=["POST"])
    def lemma_completion_sync_builtins():
        """Re-apply Unity builtin_vocabulary.json → is_builtin + is_implemented."""
        lang = (request.get_json(silent=True) or {}).get("language") or request.args.get("language") or "en"
        conn = get_conn()
        try:
            _ensure_seeded(conn, lang)
            result = sync_builtin_implementation(conn, language_code=lang)
            stats = summary(conn, scope="all", language_code=lang)
            return jsonify({"ok": True, **result, "summary": stats})
        finally:
            conn.close()
