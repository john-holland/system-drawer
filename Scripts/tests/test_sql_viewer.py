"""Tests for SQL Viewer sanitizer, recipes, and routes."""

import sqlite3
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "continuum_api"))

from continuum_api.server import app
from continuum_api.sql_safety import (
    SqlSafetyError,
    execute_readonly_query,
    parse_recipe_file,
    validate_readonly_sql,
)


class SqlSafetyTests(unittest.TestCase):
    def test_allows_select(self):
        sql, warnings = validate_readonly_sql("SELECT 1")
        self.assertIn("SELECT", sql.upper())

    def test_allows_with_select(self):
        sql, _ = validate_readonly_sql("WITH cte AS (SELECT 1 AS x) SELECT x FROM cte")
        self.assertIn("WITH", sql.upper())

    def test_rejects_delete(self):
        with self.assertRaises(SqlSafetyError):
            validate_readonly_sql("DELETE FROM thesaurus_entries")

    def test_rejects_multi_statement(self):
        with self.assertRaises(SqlSafetyError):
            validate_readonly_sql("SELECT 1; SELECT 2")

    def test_rejects_insert(self):
        with self.assertRaises(SqlSafetyError):
            validate_readonly_sql("INSERT INTO t VALUES (1)")

    def test_execute_readonly_query(self):
        with tempfile.NamedTemporaryFile(suffix=".db", delete=False) as tmp:
            path = tmp.name
        conn = sqlite3.connect(path)
        conn.execute("CREATE TABLE demo (id INTEGER, name TEXT)")
        conn.execute("INSERT INTO demo VALUES (1, 'alpha')")
        conn.commit()
        conn.close()
        result = execute_readonly_query(path, "SELECT * FROM demo")
        self.assertEqual(result["rowCount"], 1)
        self.assertEqual(result["columns"], ["id", "name"])
        self.assertEqual(result["rows"][0][1], "alpha")

    def test_parse_recipes(self):
        text = """
-- @recipe id=demo label="Demo" description="One row"
SELECT 1 AS x;
"""
        items = parse_recipe_file(text)
        self.assertEqual(len(items), 1)
        self.assertEqual(items[0]["id"], "demo")
        self.assertEqual(items[0]["label"], "Demo")
        self.assertIn("SELECT 1", items[0]["sql"])


class SqlViewerRouteTests(unittest.TestCase):
    def setUp(self):
        self.client = app.test_client()

    def test_anonymous_schema_rejected(self):
        r = self.client.get("/api/sql-viewer/schema")
        self.assertEqual(r.status_code, 401)

    def test_authenticated_schema(self):
        r = self.client.get(
            "/api/sql-viewer/schema",
            headers={"X-User-ID": "test-user"},
        )
        self.assertEqual(r.status_code, 200)
        data = r.get_json()
        self.assertIn("tables", data)
        self.assertIn("objects", data)

    def test_query_respects_limit(self):
        r = self.client.post(
            "/api/sql-viewer/query",
            json={"sql": "SELECT 1 AS n UNION SELECT 2 UNION SELECT 3", "limit": 2},
            headers={"X-User-ID": "test-user"},
        )
        self.assertEqual(r.status_code, 200)
        data = r.get_json()
        self.assertLessEqual(data["rowCount"], 2)

    def test_recipes_endpoint(self):
        r = self.client.get(
            "/api/sql-viewer/recipes",
            headers={"X-User-ID": "test-user"},
        )
        self.assertEqual(r.status_code, 200)
        items = r.get_json().get("items") or []
        self.assertGreater(len(items), 0)

    def test_sql_viewer_page(self):
        r = self.client.get("/sql-viewer")
        self.assertEqual(r.status_code, 200)


if __name__ == "__main__":
    unittest.main()
