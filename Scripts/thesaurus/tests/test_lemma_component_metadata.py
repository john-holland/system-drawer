"""Tests for lemma component metadata cache and Farey hierarchy."""

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuum_api.lemma_component_metadata import (
    REPORT_HISTORY_LIMIT,
    append_report,
    assign_farey_to_hierarchy,
    load_metadata_for_entry,
    rebuild_metadata_cache,
    upsert_blueprint,
)
from continuum_api.lemma_merge import filter_entries


class LemmaComponentMetadataTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        schema = Path(__file__).resolve().parents[2] / "continuum_lemma_component_metadata_schema.sql"
        self.conn.executescript(schema.read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS thesaurus_entries (
                id TEXT PRIMARY KEY, term TEXT, pos_tag TEXT, language_id TEXT
            );
            """
        )
        self.conn.execute(
            "INSERT INTO thesaurus_entries (id, term, pos_tag, language_id) VALUES (?, ?, ?, ?)",
            ("entry-1", "oven", "noun", "lang-en"),
        )
        self.conn.commit()

    def tearDown(self):
        self.conn.close()

    def test_assign_farey_hierarchy_two_children(self):
        nodes = [
            {"path": "Root", "gameObjectName": "Root", "components": []},
            {"path": "Root/A", "gameObjectName": "A", "components": [{"typeName": "Door"}]},
            {"path": "Root/B", "gameObjectName": "B", "components": [{"typeName": "Knob"}]},
        ]
        out = assign_farey_to_hierarchy(nodes)
        by_path = {n["path"]: n for n in out}
        self.assertIn("farey", by_path["Root/A"])
        self.assertIn("farey", by_path["Root/B"])
        self.assertNotEqual(by_path["Root/A"]["farey"], by_path["Root/B"]["farey"])

    def test_blueprint_rebuilds_cache_and_search(self):
        body = {
            "prefabRef": "Assets/Oven.prefab",
            "contentHash": "abc123",
            "nodes": [
                {"path": "Oven", "components": [{"typeName": "OvenController"}]},
                {"path": "Oven/Door", "components": [{"typeName": "Door"}]},
            ],
            "spatialBuckets": [{"bucketId": "S3.O2.1.7", "px": 1.0, "py": 0.0, "pz": 2.0}],
            "causalityLinks": [{"leafBack": "S3.O2.1", "leafPause": "S3.O2.1.7", "leafForward": "S3.O2.1.7.3"}],
        }
        upsert_blueprint(self.conn, "entry-1", body)
        self.conn.commit()
        meta = load_metadata_for_entry(self.conn, "entry-1")
        self.assertIsNotNone(meta["blueprint"])
        cc = meta["componentCreation"]
        self.assertIn("OvenController", cc["componentTypes"])
        self.assertIn("Door", cc["componentTypes"])
        self.assertIn("S3.O2.1.7", cc["bucketIds"])
        self.assertTrue(cc["hasBlueprint"])

    def test_report_history_limit(self):
        for i in range(REPORT_HISTORY_LIMIT + 5):
            append_report(
                self.conn,
                "entry-1",
                {
                    "runId": f"run-{i}",
                    "nodes": [{"path": "X", "components": [{"typeName": f"Comp{i}"}]}],
                },
            )
        self.conn.commit()
        cache = rebuild_metadata_cache(self.conn, "entry-1")
        self.assertIsNotNone(cache)
        types = json.loads(cache["component_type_names_json"])
        self.assertTrue(len(types) >= 1)
        meta = load_metadata_for_entry(self.conn, "entry-1")
        self.assertLessEqual(len(meta["reports"]), REPORT_HISTORY_LIMIT)

    def test_filter_entries_by_component_type(self):
        upsert_blueprint(
            self.conn,
            "entry-1",
            {
                "prefabRef": "Assets/X.prefab",
                "contentHash": "h1",
                "nodes": [{"path": "Root", "components": [{"typeName": "UniqueWidget"}]}],
            },
        )
        self.conn.commit()
        from continuum_api.lemma_component_metadata import cache_row_to_api, component_creation_view

        cache = rebuild_metadata_cache(self.conn, "entry-1")
        cc = component_creation_view(cache_row_to_api(cache))
        entry = {"id": "entry-1", "term": "oven", "componentCreation": cc}
        items = filter_entries([entry], component_type="UniqueWidget")
        self.assertEqual(len(items), 1)
        self.assertEqual(items[0]["id"], "entry-1")
        self.assertEqual(filter_entries([entry], component_type="Missing"), [])


if __name__ == "__main__":
    unittest.main()
