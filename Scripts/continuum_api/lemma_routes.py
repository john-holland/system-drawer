"""Lemma library API routes."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuum_api.lemma_import import import_rows, parse_default_properties, parse_tabular_file, upsert_lemma_row
    from continuum_api.lemma_merge import BUILTIN_URN_PREFIX, filter_entries, is_builtin_urn, merge_vocabulary
except ImportError:
    from lemma_import import import_rows, parse_default_properties, parse_tabular_file, upsert_lemma_row
    from lemma_merge import BUILTIN_URN_PREFIX, filter_entries, is_builtin_urn, merge_vocabulary

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _entry_json(e: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": e.get("id"),
        "term": e.get("term"),
        "posTag": e.get("posTag"),
        "languageCode": e.get("languageCode"),
        "languageId": e.get("languageId"),
        "definition": e.get("definition"),
        "synonyms": e.get("synonyms") or [],
        "tags": e.get("tags") or [],
        "isBuiltIn": bool(e.get("isBuiltIn")),
        "builtInCategory": e.get("builtInCategory"),
        "properties": e.get("properties") or {},
        "clauseCount": int(e.get("clauseCount") or 0),
        "linkedAssetIds": e.get("linkedAssetIds") or [],
        "components": e.get("components") or [],
    }


def register_lemma_routes(app, get_conn: GetConn) -> None:
    @app.route("/lemma-library")
    @app.route("/lemma-library/<path:subpath>")
    def serve_lemma_library(subpath=None):
        from flask import send_from_directory
        from pathlib import Path

        static_dir = Path(__file__).resolve().parent / "static" / "lemma-library"
        return send_from_directory(static_dir, "index.html")

    @app.route("/api/thesaurus/entries", methods=["GET"])
    def list_thesaurus_entries():
        q = request.args.get("q")
        language = request.args.get("language")
        pos = request.args.get("pos")
        source = request.args.get("source", "all")
        property_key = request.args.get("propertyKey")
        component = request.args.get("component")
        has_clause_raw = request.args.get("hasClause")
        has_clause = None
        if has_clause_raw is not None:
            has_clause = has_clause_raw.lower() in ("1", "true", "yes")
        limit = min(int(request.args.get("limit", 500)), 2000)
        offset = int(request.args.get("offset", 0))
        entry_id = request.args.get("entryId") or request.args.get("id")

        try:
            conn = get_conn()
            merged = merge_vocabulary(conn)
            conn.close()
            items = list(merged.values())
            if entry_id:
                item = merged.get(entry_id)
                if not item:
                    return jsonify({"error": "not found"}), 404
                return jsonify(_entry_json(item)), 200
            items = filter_entries(
                items,
                q=q,
                language=language,
                pos=pos,
                source=source,
                property_key=property_key,
                has_clause=has_clause,
                component=component,
            )
            items.sort(key=lambda x: ((x.get("term") or "").lower(), x.get("id") or ""))
            total = len(items)
            page = items[offset : offset + limit]
            return jsonify({"items": [_entry_json(e) for e in page], "total": total}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entries", methods=["POST"])
    def create_thesaurus_entry():
        body = request.get_json() or {}
        word = (body.get("word") or body.get("term") or "").strip()
        if not word:
            return jsonify({"error": "word required"}), 400
        row = {
            "word": word,
            "description": body.get("description") or body.get("definition") or "",
            "synonyms": body.get("synonyms"),
            "language": body.get("language") or "en",
            "partOfSpeech": body.get("partOfSpeech") or body.get("posTag") or "unknown",
            "prefabId": body.get("prefabId") or "",
            "defaultProperties": body.get("defaultProperties"),
        }
        if isinstance(row["synonyms"], list):
            row["synonyms"] = "|".join(str(s) for s in row["synonyms"])
        try:
            conn = get_conn()
            from lemma_import import _valid_property_keys

            status, err, entry_id = upsert_lemma_row(conn, row, _valid_property_keys(conn))
            if err:
                conn.close()
                return jsonify({"error": err}), 400
            conn.commit()
            merged = merge_vocabulary(conn)
            conn.close()
            entry = merged.get(entry_id) if entry_id else None
            if not entry:
                entry = next(
                    (
                        v
                        for v in merged.values()
                        if v.get("term") == word
                        and (v.get("languageCode") or "en") == (row["language"] or "en")
                        and not v.get("isBuiltIn")
                    ),
                    None,
                )
            return jsonify({"ok": True, "status": status, "entry": _entry_json(entry) if entry else None}), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entries/import", methods=["POST"])
    def import_thesaurus_entries():
        fmt = request.form.get("format")
        column_map_raw = request.form.get("columnMap")
        column_map = json.loads(column_map_raw) if column_map_raw else None
        f = request.files.get("file")
        if not f:
            return jsonify({"error": "file required"}), 400
        content = f.read()
        try:
            _, rows = parse_tabular_file(content, fmt=fmt, column_map=column_map)
            conn = get_conn()
            result = import_rows(conn, rows)
            conn.close()
            return jsonify(result), 200
        except Exception as e:
            return jsonify({"error": str(e)}), 400

    @app.route("/api/thesaurus/clauses", methods=["GET"])
    def list_clauses():
        q = request.args.get("q")
        property_key = request.args.get("propertyKey")
        entry_id = request.args.get("entryId")
        draft_script_id = request.args.get("draftScriptId")
        limit = min(int(request.args.get("limit", 200)), 1000)
        try:
            conn = get_conn()
            where = ["binding_kind = 'lemma'"]
            params: list[Any] = []
            if property_key:
                where.append("property_key = ?")
                params.append(property_key)
            if draft_script_id:
                where.append("draft_script_id = ?")
                params.append(draft_script_id)
            if q:
                where.append("LOWER(selection_text) LIKE ?")
                params.append(f"%{q.lower()}%")
            sql = f"SELECT * FROM localization_clause_bindings WHERE {' AND '.join(where)} ORDER BY selection_text LIMIT ?"
            params.append(limit)
            cur = conn.execute(sql, params)
            items = []
            merged = merge_vocabulary(conn) if entry_id else {}
            term = None
            if entry_id and entry_id in merged:
                term = (merged[entry_id].get("term") or "").lower()
            for r in cur.fetchall():
                if entry_id and term and (r["selection_text"] or "").lower() != term:
                    continue
                items.append(
                    {
                        "id": r["id"],
                        "selectionText": r["selection_text"],
                        "propertyKey": r["property_key"],
                        "propertyValue": r["property_value"],
                        "bindingKind": r["binding_kind"],
                        "draftScriptId": r["draft_script_id"],
                        "charStart": r["char_start"],
                        "charEnd": r["char_end"],
                    }
                )
            conn.close()
            return jsonify({"items": items}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "items": []}), 500

    @app.route("/api/thesaurus/localization-view", methods=["GET"])
    def localization_view():
        q = request.args.get("q")
        property_key = request.args.get("propertyKey")
        try:
            conn = get_conn()
            merged = merge_vocabulary(conn)
            entries = filter_entries(list(merged.values()), q=q, property_key=property_key)

            specs = []
            try:
                cur = conn.execute(
                    "SELECT key, value_type, allowed_values_json, default_value, description FROM localization_property_specs ORDER BY key"
                )
                specs = [
                    {
                        "key": r["key"],
                        "valueType": r["value_type"],
                        "allowedValuesJson": r["allowed_values_json"],
                        "defaultValue": r["default_value"],
                        "description": r["description"],
                    }
                    for r in cur.fetchall()
                ]
            except sqlite3.OperationalError:
                pass

            clauses = []
            try:
                where = ["binding_kind = 'lemma'"]
                params: list[Any] = []
                if property_key:
                    where.append("property_key = ?")
                    params.append(property_key)
                if q:
                    where.append("(LOWER(selection_text) LIKE ? OR LOWER(property_key) LIKE ?)")
                    params.extend([f"%{q.lower()}%", f"%{q.lower()}%"])
                cur = conn.execute(
                    f"SELECT * FROM localization_clause_bindings WHERE {' AND '.join(where)} LIMIT 500",
                    params,
                )
                for r in cur.fetchall():
                    clauses.append(
                        {
                            "id": r["id"],
                            "selectionText": r["selection_text"],
                            "propertyKey": r["property_key"],
                            "propertyValue": r["property_value"],
                            "draftScriptId": r["draft_script_id"],
                        }
                    )
            except sqlite3.OperationalError:
                pass

            rows = []
            for e in entries:
                props = e.get("properties") or {}
                if props:
                    for pk, pv in props.items():
                        rows.append(
                            {
                                "kind": "entryProperty",
                                "lemmaId": e.get("id"),
                                "lemmaTerm": e.get("term"),
                                "posTag": e.get("posTag"),
                                "propertyKey": pk,
                                "propertyValue": pv,
                                "specType": next((s["valueType"] for s in specs if s["key"] == pk), "String"),
                                "component": pk,
                                "isBuiltIn": e.get("isBuiltIn"),
                            }
                        )
                else:
                    rows.append(
                        {
                            "kind": "lemma",
                            "lemmaId": e.get("id"),
                            "lemmaTerm": e.get("term"),
                            "posTag": e.get("posTag"),
                            "propertyKey": None,
                            "propertyValue": None,
                            "specType": None,
                            "component": None,
                            "isBuiltIn": e.get("isBuiltIn"),
                        }
                    )
            for c in clauses:
                rows.append(
                    {
                        "kind": "clause",
                        "lemmaId": None,
                        "lemmaTerm": c["selectionText"],
                        "posTag": None,
                        "propertyKey": c["propertyKey"],
                        "propertyValue": c["propertyValue"],
                        "specType": next((s["valueType"] for s in specs if s["key"] == c["propertyKey"]), "String"),
                        "component": c["propertyKey"],
                        "clauseId": c["id"],
                        "draftScriptId": c.get("draftScriptId"),
                    }
                )

            conn.close()
            return jsonify(
                {
                    "specs": specs,
                    "entries": [_entry_json(e) for e in entries],
                    "clauses": clauses,
                    "rows": rows,
                }
            ), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/parse-properties", methods=["POST"])
    def parse_properties():
        body = request.get_json() or {}
        raw = body.get("defaultProperties") or body.get("text") or ""
        return jsonify({"properties": parse_default_properties(raw)}), 200
