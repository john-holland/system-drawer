"""SQL Viewer API — readonly schema browser and query playpen."""

from __future__ import annotations

import re
import sqlite3
from pathlib import Path
from typing import Any, Callable

from flask import jsonify, request, send_from_directory

try:
    from continuuuum_api.sql_safety import (
        DEFAULT_LIMIT,
        MAX_LIMIT,
        SqlSafetyError,
        execute_readonly_query,
        parse_recipe_file,
        validate_identifier,
        validate_readonly_sql,
    )
except ImportError:
    from sql_safety import (
        DEFAULT_LIMIT,
        MAX_LIMIT,
        SqlSafetyError,
        execute_readonly_query,
        parse_recipe_file,
        validate_identifier,
        validate_readonly_sql,
    )

GetConn = Callable[[], sqlite3.Connection]
GetCurrentUser = Callable[[], str]

STATIC_DIR = Path(__file__).resolve().parent / "static" / "sql-viewer"
RECIPES_PATH = Path(__file__).resolve().parent / "data" / "sql_viewer_recipes.sql"
SCHEMA_ROOT = Path(__file__).resolve().parents[1]


def _require_authenticated_user(get_current_user: GetCurrentUser):
    uid = (get_current_user() or "").strip()
    if not uid or uid.lower() == "anonymous":
        return None, (jsonify({"error": "authentication required", "code": "auth_required"}), 401)
    return uid, None


def _table_schema_files() -> dict[str, str]:
    """Best-effort map table name -> originating schema sql filename."""
    mapping: dict[str, str] = {}
    for path in SCHEMA_ROOT.glob("continuuuum_*.sql"):
        try:
            text = path.read_text(encoding="utf-8")
        except OSError:
            continue
        for m in re.finditer(
            r"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_]*)",
            text,
            re.IGNORECASE,
        ):
            mapping.setdefault(m.group(1), path.name)
    return mapping


def _introspect_object(conn: sqlite3.Connection, name: str, obj_type: str) -> dict[str, Any]:
    safe = validate_identifier(name)
    columns = []
    for row in conn.execute(f'PRAGMA table_info("{safe}")').fetchall():
        columns.append(
            {
                "cid": row["cid"],
                "name": row["name"],
                "type": row["type"],
                "notnull": bool(row["notnull"]),
                "defaultValue": row["dflt_value"],
                "pk": bool(row["pk"]),
            }
        )

    indexes = []
    for idx in conn.execute(f'PRAGMA index_list("{safe}")').fetchall():
        idx_name = idx["name"]
        cols = []
        try:
            for info in conn.execute(f'PRAGMA index_info("{idx_name}")').fetchall():
                cols.append(info["name"])
        except sqlite3.OperationalError:
            pass
        indexes.append(
            {
                "name": idx_name,
                "unique": bool(idx["unique"]),
                "origin": idx["origin"],
                "columns": cols,
            }
        )

    foreign_keys = []
    for fk in conn.execute(f'PRAGMA foreign_key_list("{safe}")').fetchall():
        foreign_keys.append(
            {
                "id": fk["id"],
                "seq": fk["seq"],
                "table": fk["table"],
                "from": fk["from"],
                "to": fk["to"],
                "onUpdate": fk["on_update"],
                "onDelete": fk["on_delete"],
                "match": fk["match"],
            }
        )

    return {
        "name": safe,
        "type": obj_type,
        "columns": columns,
        "indexes": indexes,
        "foreignKeys": foreign_keys,
    }


def register_sql_viewer_routes(app, get_conn: GetConn, get_current_user: GetCurrentUser, get_db_path: Callable[[], Path]) -> None:
    schema_file_map = _table_schema_files()

    @app.route("/sql-viewer")
    @app.route("/sql-viewer/<path:subpath>")
    def serve_sql_viewer(subpath: str | None = None):
        return send_from_directory(STATIC_DIR, "index.html")

    @app.route("/api/sql-viewer/schema", methods=["GET"])
    def sql_viewer_schema():
        _, err = _require_authenticated_user(get_current_user)
        if err:
            return err
        try:
            conn = get_conn()
            cur = conn.execute(
                """
                SELECT name, type, sql
                FROM sqlite_master
                WHERE type IN ('table', 'view')
                  AND name NOT LIKE 'sqlite_%'
                ORDER BY type, name
                """
            )
            objects = []
            for row in cur.fetchall():
                name = row["name"]
                obj_type = row["type"]
                detail = _introspect_object(conn, name, obj_type)
                detail["ddl"] = row["sql"]
                detail["schemaFile"] = schema_file_map.get(name)
                objects.append(detail)
            conn.close()
            tables = [o for o in objects if o["type"] == "table"]
            views = [o for o in objects if o["type"] == "view"]
            return jsonify({"tables": tables, "views": views, "objects": objects}), 200
        except SqlSafetyError as e:
            return jsonify({"error": str(e), "code": e.code}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/sql-viewer/tables/<table_name>/preview", methods=["GET"])
    def sql_viewer_table_preview(table_name: str):
        _, err = _require_authenticated_user(get_current_user)
        if err:
            return err
        try:
            safe = validate_identifier(table_name)
            limit = min(int(request.args.get("limit", 100)), MAX_LIMIT)
            offset = max(int(request.args.get("offset", 0)), 0)
            sql = f'SELECT * FROM "{safe}" LIMIT {limit} OFFSET {offset}'
            result = execute_readonly_query(get_db_path(), sql, limit=limit)
            return jsonify(result), 200
        except SqlSafetyError as e:
            return jsonify({"error": str(e), "code": e.code}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/sql-viewer/validate", methods=["POST"])
    def sql_viewer_validate():
        _, err = _require_authenticated_user(get_current_user)
        if err:
            return err
        body = request.get_json(silent=True) or {}
        sql = body.get("sql") or body.get("query") or ""
        try:
            normalized, warnings = validate_readonly_sql(sql)
            return jsonify({"ok": True, "sql": normalized, "warnings": warnings}), 200
        except SqlSafetyError as e:
            return jsonify({"ok": False, "errors": [str(e)], "code": e.code}), 200

    @app.route("/api/sql-viewer/query", methods=["POST"])
    def sql_viewer_query():
        _, err = _require_authenticated_user(get_current_user)
        if err:
            return err
        body = request.get_json(silent=True) or {}
        sql = body.get("sql") or body.get("query") or ""
        limit = min(int(body.get("limit", DEFAULT_LIMIT)), MAX_LIMIT)
        try:
            result = execute_readonly_query(get_db_path(), sql, limit=limit)
            return jsonify(result), 200
        except SqlSafetyError as e:
            return jsonify({"error": str(e), "code": e.code}), 400
        except sqlite3.OperationalError as e:
            return jsonify({"error": str(e)}), 500

    @app.route("/api/sql-viewer/recipes", methods=["GET"])
    def sql_viewer_recipes():
        _, err = _require_authenticated_user(get_current_user)
        if err:
            return err
        if not RECIPES_PATH.is_file():
            return jsonify({"items": []}), 200
        text = RECIPES_PATH.read_text(encoding="utf-8")
        return jsonify({"items": parse_recipe_file(text)}), 200
