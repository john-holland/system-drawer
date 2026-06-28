"""Tests for composed lemmas CRUD, cycle guard, and recombobulate drift detection."""

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuum_api.lemma_composition import (
    load_composition,
    replace_composition,
    validate_children,
    would_create_cycle,
)
from continuum_api.lemma_composition_spatial import audit_composition_spatial, recombobulate_spatial
from continuum_api.lemma_merge import merge_vocabulary


class LemmaCompositionTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[1]
        self.conn.executescript((root / "continuum_spatial_4d_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY,
                value_type TEXT NOT NULL,
                allowed_values_json TEXT,
                default_value TEXT,
                description TEXT
            );
            """
        )
        self.conn.executescript((root / "continuum_lemma_composition_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS thesaurus_entries (
                id TEXT PRIMARY KEY, term TEXT, pos_tag TEXT, language_id TEXT
            );
            CREATE TABLE IF NOT EXISTS languages (
                id TEXT PRIMARY KEY, code TEXT
            );
            INSERT OR IGNORE INTO languages (id, code) VALUES ('en', 'en');
            CREATE TABLE IF NOT EXISTS thesaurus_entry_properties (
                entry_id TEXT NOT NULL,
                property_key TEXT NOT NULL,
                property_value TEXT NOT NULL,
                PRIMARY KEY (entry_id, property_key)
            );
            CREATE TABLE IF NOT EXISTS localization_clause_bindings (
                id TEXT PRIMARY KEY,
                episode_script_id TEXT,
                draft_script_id TEXT,
                farey_left_num INTEGER NOT NULL,
                farey_left_den INTEGER NOT NULL,
                farey_right_num INTEGER NOT NULL,
                farey_right_den INTEGER NOT NULL,
                char_start INTEGER NOT NULL,
                char_end INTEGER NOT NULL,
                selection_text TEXT NOT NULL,
                property_key TEXT NOT NULL,
                property_value TEXT NOT NULL,
                binding_kind TEXT NOT NULL DEFAULT 'lemma',
                ast_node_id TEXT,
                prompt_placeholder_name TEXT,
                entry_id TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """
        )
        for eid, term in [("parent-1", "kitchen"), ("child-a", "oven"), ("child-b", "stove")]:
            self.conn.execute(
                "INSERT INTO thesaurus_entries (id, term, pos_tag, language_id) VALUES (?, ?, 'noun', 'en')",
                (eid, term),
            )
        self.conn.commit()

    def tearDown(self):
        self.conn.close()

    def test_replace_and_load_composition(self):
        data = replace_composition(
            self.conn,
            "parent-1",
            [{"entryId": "child-a", "sortOrder": 0}, {"entryId": "child-b", "sortOrder": 1}],
        )
        self.conn.commit()
        self.assertTrue(data["isComposedLemma"])
        self.assertEqual(len(data["children"]), 2)
        self.assertEqual(data["children"][0]["entryId"], "child-a")

        merged = merge_vocabulary(self.conn)
        self.assertTrue(merged["parent-1"]["isComposedLemma"])
        self.assertEqual(len(merged["parent-1"]["compositionChildren"]), 2)

    def test_cycle_rejection(self):
        replace_composition(self.conn, "child-a", [{"entryId": "parent-1"}])
        self.conn.commit()
        err = validate_children(self.conn, "parent-1", [{"entryId": "child-a"}])
        self.assertIsNotNone(err)
        self.assertIn("cycle", err.lower())
        self.assertTrue(would_create_cycle(self.conn, "parent-1", "child-a"))

    def test_anchor_drift_issue(self):
        replace_composition(
            self.conn,
            "parent-1",
            [
                {
                    "entryId": "child-a",
                    "anchorText": "old oven text",
                    "anchorFarey": {"ln": 0, "ld": 1, "rn": 1, "rd": 1},
                }
            ],
        )
        self.conn.commit()
        script = "The oven is hot."
        issues = audit_composition_spatial(self.conn, "parent-1", script_text=script)
        drift = [i for i in issues if i["code"] == "anchor_drift"]
        self.assertTrue(drift)
        self.assertNotEqual(drift[0]["storedText"], drift[0]["currentText"])

    def test_recombobulate_apply_creates_spatial(self):
        replace_composition(self.conn, "parent-1", [{"entryId": "child-a"}])
        self.conn.commit()
        audit = recombobulate_spatial(self.conn, "parent-1", {"scriptText": "oven"})
        missing = [i for i in audit["issues"] if i["code"] == "missing_spatial"]
        self.assertTrue(missing)
        repair = recombobulate_spatial(
            self.conn,
            "parent-1",
            {"scriptText": "oven", "apply": True, "acknowledgedIssueIds": []},
        )
        self.conn.commit()
        comp = load_composition(self.conn, "parent-1")
        self.assertTrue(comp["children"][0]["spatial4dId"])
        remaining_missing = [i for i in repair["issues"] if i["code"] == "missing_spatial"]
        self.assertFalse(remaining_missing)


if __name__ == "__main__":
    unittest.main()
