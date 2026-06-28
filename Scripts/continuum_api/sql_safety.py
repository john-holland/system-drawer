"""Read-only SQL validation and execution for the SQL Viewer."""

from __future__ import annotations

import re
import sqlite3
import time
from pathlib import Path
from typing import Any
from urllib.parse import quote

import sqlparse
from sqlparse.tokens import DML, Keyword

BANNED_KEYWORDS = frozenset(
    {
        "INSERT",
        "UPDATE",
        "DELETE",
        "DROP",
        "ALTER",
        "CREATE",
        "REPLACE",
        "ATTACH",
        "DETACH",
        "VACUUM",
        "REINDEX",
        "TRUNCATE",
        "GRANT",
        "REVOKE",
        "MERGE",
    }
)

ALLOWED_ROOT_KEYWORDS = frozenset({"SELECT", "WITH", "EXPLAIN", "PRAGMA"})

READONLY_PRAGMAS = frozenset(
    {
        "table_info",
        "index_list",
        "index_info",
        "foreign_key_list",
        "database_list",
        "compile_options",
        "table_xinfo",
        "integrity_check",
        "quick_check",
    }
)

DEFAULT_LIMIT = 500
MAX_LIMIT = 2000

_IDENTIFIER_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


class SqlSafetyError(Exception):
    def __init__(self, message: str, code: str = "sql_rejected"):
        super().__init__(message)
        self.code = code


def validate_identifier(name: str) -> str:
    """Return a safe SQLite identifier or raise."""
    n = (name or "").strip()
    if not n or not _IDENTIFIER_RE.match(n):
        raise SqlSafetyError(f"Invalid identifier: {name!r}", "invalid_identifier")
    return n


def _strip_trailing_semicolon(sql: str) -> str:
    return sql.strip().rstrip(";")


def _first_significant_token(statement: sqlparse.sql.Statement):
    for tok in statement.tokens:
        if tok.is_whitespace or tok.ttype in (
            sqlparse.tokens.Comment,
            sqlparse.tokens.Comment.Multiline,
            sqlparse.tokens.Comment.Single,
        ):
            continue
        return tok
    return None


def _keyword_value(token) -> str:
    if token is None:
        return ""
    return (token.value or "").strip().upper()


def _collect_keywords(statement: sqlparse.sql.Statement) -> set[str]:
    found: set[str] = set()
    for tok in statement.flatten():
        if tok.ttype in (Keyword, DML):
            found.add(tok.value.upper())
    return found


def _pragma_name(statement: sqlparse.sql.Statement) -> str | None:
    text = statement.value.strip()
    m = re.match(r"(?is)^PRAGMA\s+(?:\w+\.)?(\w+)", text)
    if not m:
        return None
    return m.group(1).lower()


def _validate_pragma(statement: sqlparse.sql.Statement) -> None:
    name = _pragma_name(statement)
    if not name:
        raise SqlSafetyError("PRAGMA statement could not be parsed.", "pragma_rejected")
    if name not in READONLY_PRAGMAS:
        raise SqlSafetyError(
            f"PRAGMA {name} is not allowed. Allowed: {', '.join(sorted(READONLY_PRAGMAS))}.",
            "pragma_rejected",
        )


def _validate_with_leads_to_select(statement: sqlparse.sql.Statement) -> None:
    text = statement.value.upper()
    if "SELECT" not in text:
        raise SqlSafetyError("WITH must lead to a SELECT query.", "with_rejected")


def validate_readonly_sql(sql: str) -> tuple[str, list[str]]:
    """
    Validate SQL for read-only execution.
    Returns (normalized_sql, warnings).
    """
    raw = (sql or "").strip()
    if not raw:
        raise SqlSafetyError("SQL is empty.", "empty_sql")

    statements = sqlparse.parse(raw)
    if len(statements) != 1:
        raise SqlSafetyError("Only a single SQL statement is allowed.", "multi_statement")

    statement = statements[0]
    normalized = _strip_trailing_semicolon(statement.value)
    if not normalized:
        raise SqlSafetyError("SQL is empty.", "empty_sql")

    first = _first_significant_token(statement)
    root = _keyword_value(first)
    if root not in ALLOWED_ROOT_KEYWORDS:
        raise SqlSafetyError(
            f"Statement must start with one of: {', '.join(sorted(ALLOWED_ROOT_KEYWORDS))}.",
            "root_keyword_rejected",
        )

    keywords = _collect_keywords(statement)
    banned = keywords & BANNED_KEYWORDS
    if banned:
        raise SqlSafetyError(
            f"Forbidden keyword(s): {', '.join(sorted(banned))}.",
            "keyword_rejected",
        )

    if root == "PRAGMA":
        _validate_pragma(statement)
    elif root == "WITH":
        _validate_with_leads_to_select(statement)

    warnings: list[str] = []
    if root in ("SELECT", "WITH") and "LIMIT" not in keywords:
        warnings.append(f"No LIMIT clause; server will cap results at {DEFAULT_LIMIT} rows.")

    return normalized, warnings


def _ensure_limit(sql: str, limit: int) -> str:
    """Append LIMIT if missing on SELECT/WITH queries."""
    parsed = sqlparse.parse(sql)
    if not parsed:
        return sql
    statement = parsed[0]
    first = _keyword_value(_first_significant_token(statement))
    if first not in ("SELECT", "WITH"):
        return sql
    keywords = _collect_keywords(statement)
    if "LIMIT" in keywords:
        return sql
    return f"{sql.rstrip()} LIMIT {int(limit)}"


def open_readonly_connection(db_path: Path | str) -> sqlite3.Connection:
    path = Path(db_path)
    uri = f"file:{quote(str(path.resolve()).replace(chr(92), '/'))}?mode=ro"
    try:
        conn = sqlite3.connect(uri, uri=True)
    except sqlite3.OperationalError:
        conn = sqlite3.connect(str(path))
        conn.execute("PRAGMA query_only = ON")
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA busy_timeout = 5000")
    return conn


def execute_readonly_query(
    db_path: Path | str,
    sql: str,
    *,
    limit: int = DEFAULT_LIMIT,
) -> dict[str, Any]:
    """Validate and execute read-only SQL; return tabular result metadata."""
    cap = max(1, min(int(limit or DEFAULT_LIMIT), MAX_LIMIT))
    normalized, warnings = validate_readonly_sql(sql)
    exec_sql = _ensure_limit(normalized, cap)

    conn = open_readonly_connection(db_path)
    try:
        t0 = time.perf_counter()
        cur = conn.execute(exec_sql)
        elapsed_ms = round((time.perf_counter() - t0) * 1000, 2)

        if cur.description is None:
            return {
                "columns": [],
                "rows": [],
                "rowCount": 0,
                "truncated": False,
                "elapsedMs": elapsed_ms,
                "sql": exec_sql,
                "warnings": warnings,
            }

        columns = [d[0] for d in cur.description]
        fetched = cur.fetchmany(cap + 1)
        truncated = len(fetched) > cap
        if truncated:
            fetched = fetched[:cap]
        rows = [[_serialize_cell(row[col]) for col in columns] for row in fetched]
        return {
            "columns": columns,
            "rows": rows,
            "rowCount": len(rows),
            "truncated": truncated,
            "elapsedMs": elapsed_ms,
            "sql": exec_sql,
            "warnings": warnings,
        }
    finally:
        conn.close()


def _serialize_cell(value: Any) -> Any:
    if value is None:
        return None
    if isinstance(value, (int, float, bool)):
        return value
    if isinstance(value, bytes):
        try:
            return value.decode("utf-8")
        except UnicodeDecodeError:
            return value.hex()
    return str(value)


def parse_recipe_file(text: str) -> list[dict[str, str]]:
    """Parse sql_viewer_recipes.sql blocks into recipe dicts."""
    items: list[dict[str, str]] = []
    current_meta: dict[str, str] = {}
    sql_lines: list[str] = []

    def flush() -> None:
        nonlocal current_meta, sql_lines
        sql = "\n".join(sql_lines).strip()
        if not sql and not current_meta:
            return
        rid = current_meta.get("id") or f"recipe_{len(items) + 1}"
        items.append(
            {
                "id": rid,
                "label": current_meta.get("label") or rid,
                "description": current_meta.get("description") or "",
                "sql": sql,
            }
        )
        current_meta = {}
        sql_lines = []

    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("-- @recipe"):
            if sql_lines or current_meta:
                flush()
            payload = stripped[len("-- @recipe") :].strip()
            for part in re.split(r"\s+", payload):
                if "=" in part:
                    k, v = part.split("=", 1)
                    current_meta[k.strip()] = v.strip().strip('"').strip("'")
            continue
        if stripped.startswith("--") and not current_meta and not sql_lines:
            continue
        if current_meta or sql_lines:
            sql_lines.append(line)

    flush()
    return [i for i in items if i.get("sql")]
