"""Lemma library API routes."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

try:
    from continuuuum_api.lemma_import import (
        _valid_property_keys,
        import_rows,
        parse_default_properties,
        parse_tabular_file,
        upsert_lemma_row,
    )
    from continuuuum_api.lemma_merge import BUILTIN_URN_PREFIX, filter_entries, is_builtin_urn, merge_vocabulary
except ImportError:
    from lemma_import import (
        _valid_property_keys,
        import_rows,
        parse_default_properties,
        parse_tabular_file,
        upsert_lemma_row,
    )
    from lemma_merge import BUILTIN_URN_PREFIX, filter_entries, is_builtin_urn, merge_vocabulary

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _resolve_clause_lemma_id(
    merged: dict[str, Any],
    entry_id: str | None,
    property_key: str | None,
    property_value: str | None,
    selection_text: str | None,
    lemma_by_selection: dict[str, str],
) -> str | None:
    """Resolve a clause binding row to a lemma library entry id when possible."""
    candidates: list[str] = []
    if entry_id:
        candidates.append(entry_id)
    if property_key == "entry-id" and property_value:
        candidates.append(property_value)
    for candidate in candidates:
        if candidate in merged:
            return candidate
    sel = (selection_text or "").strip()
    if sel:
        mapped = lemma_by_selection.get(sel)
        if mapped and mapped in merged:
            return mapped
        term = sel.lower()
        matches = [e for e in merged.values() if (e.get("term") or "").lower() == term]
        if len(matches) == 1:
            return matches[0]["id"]
    return candidates[0] if candidates else None


def _create_lemma_error_response(err: str, entry_id: str | None = None):
    """Map upsert/import errors to structured JSON for the create form."""
    if err == "missing word":
        return jsonify({"error": "Word is required.", "code": "word_required", "field": "word"}), 400
    if err == "cannot create built-in URN as custom entry":
        return (
            jsonify(
                {
                    "error": "Word cannot be a built-in URN. Use a plain term such as the-car.",
                    "code": "builtin_urn",
                    "field": "word",
                }
            ),
            400,
        )
    if err == "matches built-in entry":
        payload: dict[str, Any] = {
            "error": (
                "This word matches a built-in lemma for this language. "
                "Use the existing entry or change the word."
            ),
            "code": "builtin_conflict",
            "field": "word",
        }
        if entry_id:
            payload["existingEntryId"] = entry_id
        return jsonify(payload), 409
    payload = {"error": err, "code": "validation_error"}
    if entry_id:
        payload["existingEntryId"] = entry_id
    return jsonify(payload), 400


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
        "componentCreation": e.get("componentCreation"),
        "compositionChildren": e.get("compositionChildren") or [],
        "isComposedLemma": bool(e.get("isComposedLemma")),
        "lemmaPrompt": e.get("lemmaPrompt"),
        "spatial4dId": e.get("spatial4dId"),
        "defaultTiming": e.get("defaultTiming"),
        "patchProperties": e.get("patchProperties") or {},
        "usesOverlay": bool(e.get("usesOverlay")),
        "spatialGeneratorDefinitions": e.get("spatialGeneratorDefinitions") or [],
        "spatialGen2d": e.get("spatialGen2d") or "",
        "spatialGen3d": e.get("spatialGen3d") or "",
        "spatialGen4d": e.get("spatialGen4d") or "",
        "spatialGenDims": e.get("spatialGenDims") or "",
    }


def register_lemma_routes(app, get_conn: GetConn) -> None:
    @app.route("/lemma-library")
    @app.route("/lemma-library/<path:subpath>")
    def serve_lemma_library(subpath=None):
        from flask import send_from_directory
        from pathlib import Path

        static_dir = Path(__file__).resolve().parent / "static" / "lemma-library"
        return send_from_directory(static_dir, "index.html")

    @app.route("/api/thesaurus/pos-tags", methods=["GET"])
    def list_pos_tags_route():
        try:
            from thesaurus.pos_tags import list_pos_tags
        except ImportError:
            from pos_tags import list_pos_tags
        return jsonify({"items": list_pos_tags()}), 200

    @app.route("/api/thesaurus/entries", methods=["GET"])
    def list_thesaurus_entries():
        q = request.args.get("q")
        language = request.args.get("language")
        pos = request.args.get("pos")
        source = request.args.get("source", "all")
        property_key = request.args.get("propertyKey")
        component = request.args.get("component")
        component_type = request.args.get("componentType")
        bucket_id = request.args.get("bucketId")
        causality_leaf = request.args.get("causalityLeaf")
        has_meta_raw = request.args.get("hasComponentMetadata")
        has_component_metadata = None
        if has_meta_raw is not None:
            has_component_metadata = has_meta_raw.lower() in ("1", "true", "yes")
        has_clause_raw = request.args.get("hasClause")
        has_clause = None
        if has_clause_raw is not None:
            has_clause = has_clause_raw.lower() in ("1", "true", "yes")
        spatial_dimension = request.args.get("spatialDimension") or request.args.get("spatial_dimension")
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
                component_type=component_type,
                bucket_id=bucket_id,
                causality_leaf=causality_leaf,
                has_component_metadata=has_component_metadata,
                spatial_dimension=spatial_dimension,
            )
            q_norm = (q or "").strip().lower()

            def _entry_sort_key(x: dict[str, Any]):
                term = (x.get("term") or "").lower()
                # Exact term matches first so resolve-or-create / pickers see built-ins
                # even when many substring hits would otherwise fill the page.
                exact = 0 if q_norm and term == q_norm else 1
                return (exact, term, x.get("id") or "")

            items.sort(key=_entry_sort_key)
            total = len(items)
            page = items[offset : offset + limit]
            return jsonify({"items": [_entry_json(e) for e in page], "total": total}), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500
        except Exception as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entries", methods=["POST"])
    def create_thesaurus_entry():
        body = request.get_json() or {}
        word = (body.get("word") or body.get("term") or "").strip()
        if not word:
            return jsonify({"error": "Word is required.", "code": "word_required", "field": "word"}), 400
        row = {
            "word": word,
            "description": body.get("description") or body.get("definition") or "",
            "synonyms": body.get("synonyms"),
            "language": body.get("language") or "en",
            "partOfSpeech": body.get("partOfSpeech") or body.get("posTag") or "unknown",
            "prefabId": body.get("prefabId") or "",
            "defaultProperties": body.get("defaultProperties"),
        }
        try:
            from thesaurus.pos_tags import normalize_pos_tag
        except ImportError:
            from pos_tags import normalize_pos_tag
        row["partOfSpeech"] = normalize_pos_tag(row["partOfSpeech"])
        if isinstance(row["synonyms"], list):
            row["synonyms"] = "|".join(str(s) for s in row["synonyms"])
        conn = None
        try:
            conn = get_conn()
            status, err, entry_id = upsert_lemma_row(conn, row, _valid_property_keys(conn))
            if err:
                return _create_lemma_error_response(err, entry_id)
            composition = body.get("composition")
            if composition and entry_id:
                try:
                    try:
                        from continuuuum_api.lemma_composition import replace_composition
                    except ImportError:
                        from lemma_composition import replace_composition
                    replace_composition(conn, entry_id, composition)
                except ValueError as e:
                    return jsonify({"error": str(e), "code": "composition_invalid"}), 400
            conn.commit()
            merged = merge_vocabulary(conn)
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
            message = (
                "Lemma created."
                if status == "created"
                else "A lemma with this word, language, and part of speech already exists; prefab and properties were updated."
            )
            code = 201 if status == "created" else 200
            return (
                jsonify(
                    {
                        "ok": True,
                        "status": status,
                        "message": message,
                        "entry": _entry_json(entry) if entry else None,
                    }
                ),
                code,
            )
        except sqlite3.IntegrityError as e:
            msg = str(e).lower()
            field = "word"
            if "property" in msg:
                field = "defaultProperties"
            elif "prefab" in msg:
                field = "prefabId"
            return (
                jsonify(
                    {
                        "error": "Could not save lemma because it conflicts with an existing record.",
                        "code": "duplicate_entry",
                        "field": field,
                        "detail": str(e),
                    }
                ),
                409,
            )
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "code": "database_error"}), 500
        except Exception as e:
            return jsonify({"error": f"Failed to create lemma: {e}", "code": "internal_error"}), 500
        finally:
            if conn is not None:
                conn.close()

    @app.route("/api/thesaurus/entries/<entry_id>/component-blueprint", methods=["POST"])
    def post_component_blueprint(entry_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_component_metadata import upsert_blueprint
            except ImportError:
                from lemma_component_metadata import upsert_blueprint
            result = upsert_blueprint(conn, entry_id, body)
            conn.commit()
            conn.close()
            return jsonify(result), 201
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_component_metadata_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<entry_id>/component-reports", methods=["POST"])
    def post_component_report(entry_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_component_metadata import append_report
            except ImportError:
                from lemma_component_metadata import append_report
            result = append_report(conn, entry_id, body)
            conn.commit()
            conn.close()
            return jsonify(result), 201
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_component_metadata_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<entry_id>/component-metadata", methods=["GET"])
    def get_component_metadata(entry_id: str):
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_component_metadata import load_metadata_for_entry
            except ImportError:
                from lemma_component_metadata import load_metadata_for_entry
            data = load_metadata_for_entry(conn, entry_id)
            conn.close()
            return jsonify(data), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_component_metadata_schema.sql"}), 500

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
        language = request.args.get("language")
        try:
            conn = get_conn()
            merged = merge_vocabulary(conn)
            entries = filter_entries(
                list(merged.values()), q=q, property_key=property_key, language=language
            )

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
            lemma_by_selection: dict[str, str] = {}
            try:
                where = ["binding_kind IN ('lemma', 'localization')"]
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
                binding_rows = cur.fetchall()
                for r in binding_rows:
                    if r["binding_kind"] == "lemma" and r["entry_id"]:
                        lemma_by_selection.setdefault(r["selection_text"], r["entry_id"])
                for r in binding_rows:
                    clause = {
                        "id": r["id"],
                        "selectionText": r["selection_text"],
                        "propertyKey": r["property_key"],
                        "propertyValue": r["property_value"],
                        "draftScriptId": r["draft_script_id"],
                        "entryId": r["entry_id"],
                        "bindingKind": r["binding_kind"],
                    }
                    clause["lemmaId"] = _resolve_clause_lemma_id(
                        merged,
                        r["entry_id"],
                        r["property_key"],
                        r["property_value"],
                        r["selection_text"],
                        lemma_by_selection,
                    )
                    clauses.append(clause)
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
                        "kind": "localizationClause" if c.get("bindingKind") == "localization" else "clause",
                        "lemmaId": c.get("lemmaId"),
                        "lemmaTerm": c["selectionText"],
                        "posTag": None,
                        "propertyKey": c["propertyKey"],
                        "propertyValue": c["propertyValue"],
                        "specType": next((s["valueType"] for s in specs if s["key"] == c["propertyKey"]), "String"),
                        "component": c["propertyKey"],
                        "clauseId": c["id"],
                        "draftScriptId": c.get("draftScriptId"),
                        "bindingKind": c.get("bindingKind"),
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

    @app.route("/api/thesaurus/entries/<entry_id>/composition", methods=["GET"])
    def get_entry_composition(entry_id: str):
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_composition import load_composition
            except ImportError:
                from lemma_composition import load_composition
            data = load_composition(conn, entry_id)
            conn.close()
            return jsonify(data), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_composition_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<entry_id>/composition", methods=["PUT"])
    def put_entry_composition(entry_id: str):
        body = request.get_json() or {}
        children = body.get("children") or body.get("composition") or []
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_composition import replace_composition
            except ImportError:
                from lemma_composition import replace_composition
            data = replace_composition(conn, entry_id, children)
            conn.commit()
            conn.close()
            return jsonify(data), 200
        except ValueError as e:
            return jsonify({"error": str(e), "code": "composition_invalid"}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_composition_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<path:entry_id>/prompt", methods=["GET"])
    def get_entry_prompt(entry_id: str):
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_prompt import load_prompt_bundle
            except ImportError:
                from lemma_prompt import load_prompt_bundle
            data = load_prompt_bundle(conn, entry_id)
            conn.close()
            return jsonify(data), 200
        except ValueError as e:
            return jsonify({"error": str(e), "code": "not_found"}), 404
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_prompt_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<path:entry_id>/prompt", methods=["PUT"])
    def put_entry_prompt(entry_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_prompt import upsert_lemma_prompt_bundle
            except ImportError:
                from lemma_prompt import upsert_lemma_prompt_bundle
            data = upsert_lemma_prompt_bundle(conn, entry_id, body)
            conn.commit()
            conn.close()
            return jsonify(data), 200
        except ValueError as e:
            return jsonify({"error": str(e), "code": "prompt_invalid"}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_prompt_schema.sql"}), 500

    @app.route("/api/thesaurus/entries/<path:entry_id>/expand-prompt", methods=["POST"])
    def post_expand_prompt(entry_id: str):
        body = request.get_json(silent=True) or {}
        mod_context = body.get("modContext") or body.get("mod_context")
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_prompt import expand_lemma_prompt
                from continuuuum_api.mod_db import build_mod_context_from_manifest
            except ImportError:
                from lemma_prompt import expand_lemma_prompt
                from mod_db import build_mod_context_from_manifest
            if mod_context and mod_context.get("lemmaOverrides"):
                mod_context = build_mod_context_from_manifest(mod_context)
            data = expand_lemma_prompt(conn, entry_id, mod_context=mod_context)
            conn.close()
            return jsonify(data), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/thesaurus/entries/<entry_id>/recombobulate-spatial", methods=["POST"])
    def post_recombobulate_spatial(entry_id: str):
        body = request.get_json() or {}
        try:
            conn = get_conn()
            try:
                from continuuuum_api.lemma_composition_spatial import recombobulate_spatial
            except ImportError:
                from lemma_composition_spatial import recombobulate_spatial
            data = recombobulate_spatial(conn, entry_id, body)
            conn.commit()
            conn.close()
            return jsonify(data), 200
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e), "hint": "Apply continuuuum_lemma_composition_schema.sql"}), 500
