"""Tests for recursive lemma prompt expansion and web overlays."""

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuuuum_api.lemma_merge import BUILTIN_URN_PREFIX, merge_vocabulary
from continuuuum_api.lemma_prompt import (
    ensure_lemma_prompt_schema,
    expand_lemma_prompt,
    load_prompt_bundle,
    upsert_lemma_prompt_bundle,
)


class LemmaPromptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[1]
        self.conn.executescript((root / "continuuuum_spatial_4d_schema.sql").read_text(encoding="utf-8"))
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
        self.conn.executescript((root / "continuuuum_lemma_composition_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript((root / "continuuuum_lemma_prompt_schema.sql").read_text(encoding="utf-8"))
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
            """
        )
        for eid, term in [
            ("parent-1", "kitchen"),
            ("child-a", "oven"),
            ("child-b", "heat"),
        ]:
            self.conn.execute(
                "INSERT INTO thesaurus_entries (id, term, pos_tag, language_id) VALUES (?, ?, 'noun', 'en')",
                (eid, term),
            )
        self.conn.commit()
        ensure_lemma_prompt_schema(self.conn)

    def tearDown(self):
        self.conn.close()

    def test_recursive_expansion(self):
        upsert_lemma_prompt_bundle(
            self.conn,
            "child-b",
            {"lemmaPrompt": "warm glow"},
        )
        upsert_lemma_prompt_bundle(
            self.conn,
            "child-a",
            {"lemmaPrompt": "{P:heat} inside"},
        )
        upsert_lemma_prompt_bundle(
            self.conn,
            "parent-1",
            {
                "lemmaPrompt": "In the {P:oven}",
                "compositionChildren": [
                    {"entryId": "child-a", "sortOrder": 0},
                    {"entryId": "child-b", "sortOrder": 1},
                ],
            },
        )
        self.conn.commit()
        out = expand_lemma_prompt(self.conn, "parent-1")
        self.assertIn("warm glow", out["expandedText"])
        self.assertIn("inside", out["expandedText"])
        self.assertEqual(out["expandedText"], "In the warm glow inside")

    def test_monkey_patch_overlay_on_builtin(self):
        urn = f"{BUILTIN_URN_PREFIX}/en/noun/building"
        upsert_lemma_prompt_bundle(
            self.conn,
            urn,
            {
                "lemmaPrompt": "{P:oven}",
                "patchProperties": {"non-ik-animation": "true"},
                "compositionChildren": [{"entryId": "child-a", "sortOrder": 0}],
            },
        )
        self.conn.commit()
        merged = merge_vocabulary(self.conn)
        self.assertTrue(merged.get(urn, {}).get("usesOverlay"))
        self.assertEqual(merged[urn]["properties"].get("non-ik-animation"), "true")
        self.assertTrue(merged[urn]["isComposedLemma"])

    def test_per_child_patch_overrides(self):
        upsert_lemma_prompt_bundle(
            self.conn,
            "child-a",
            {"lemmaPrompt": "base"},
        )
        upsert_lemma_prompt_bundle(
            self.conn,
            "parent-1",
            {
                "lemmaPrompt": "{P:oven}",
                "compositionChildren": [
                    {
                        "entryId": "child-a",
                        "sortOrder": 0,
                        "patchProperties": {"non-ik-animation": "true"},
                    }
                ],
            },
        )
        self.conn.commit()
        out = expand_lemma_prompt(self.conn, "parent-1")
        self.assertEqual(out["mergedProperties"].get("non-ik-animation"), "true")

    def test_spatial_timing_upsert(self):
        upsert_lemma_prompt_bundle(
            self.conn,
            "parent-1",
            {
                "timing": {"tMin": 10, "tMax": 500},
                "spatial": {
                    "bounds": {
                        "centerX": 1,
                        "centerY": 2,
                        "centerZ": 3,
                        "sizeX": 4,
                        "sizeY": 5,
                        "sizeZ": 6,
                    },
                },
            },
        )
        self.conn.commit()
        bundle = load_prompt_bundle(self.conn, "parent-1")
        self.assertEqual(bundle["timing"]["tMin"], 10)
        self.assertEqual(bundle["timing"]["tMax"], 500)
        sid = bundle["spatial"]["spatial4dId"]
        self.assertTrue(sid)
        row = self.conn.execute("SELECT * FROM spatial_4d WHERE id = ?", (sid,)).fetchone()
        self.assertEqual(float(row["t_min"]), 10)
        self.assertEqual(float(row["center_x"]), 1)

    def test_prompt_cycle_detection(self):
        upsert_lemma_prompt_bundle(self.conn, "parent-1", {"lemmaPrompt": "{P:kitchen}"})
        upsert_lemma_prompt_bundle(self.conn, "child-a", {"lemmaPrompt": "{P:kitchen}"})
        self.conn.commit()
        out = expand_lemma_prompt(self.conn, "parent-1")
        codes = [i["code"] for i in out.get("issues") or []]
        self.assertTrue(any(c in ("prompt_cycle", "unresolved_placeholder") for c in codes))

    def test_synthesize_prompt_from_children(self):
        upsert_lemma_prompt_bundle(
            self.conn,
            "parent-1",
            {
                "lemmaPrompt": "",
                "compositionChildren": [
                    {"entryId": "child-a", "sortOrder": 0},
                    {"entryId": "child-b", "sortOrder": 1},
                ],
            },
        )
        self.conn.commit()
        bundle = load_prompt_bundle(self.conn, "parent-1")
        self.assertIn("{P:oven}", bundle["lemmaPrompt"])
        self.assertIn("{P:heat}", bundle["lemmaPrompt"])


if __name__ == "__main__":
    unittest.main()
