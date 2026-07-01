"""Tests for Mayor Dog Mods API and merge helpers."""

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuum_api.lemma_prompt import expand_lemma_prompt, ensure_lemma_prompt_schema, upsert_lemma_prompt_bundle
from continuum_api.mod_db import (
    audit_mayor_dog_mod_sections,
    build_bootstrap_manifest,
    build_mod_context_from_manifest,
    ensure_mayor_dog_mods_schema,
    resolve_mod_placeholders,
    upsert_moddable_target,
)


class MayorDogModRoutesTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[1]
        self.conn.executescript((root / "continuum_mayor_dog_mods_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY, value_type TEXT, allowed_values_json TEXT,
                default_value TEXT, description TEXT
            );
            """
        )
        self.conn.executescript((root / "continuum_lemma_composition_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript((root / "continuum_lemma_prompt_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS thesaurus_entries (
                id TEXT PRIMARY KEY, term TEXT, pos_tag TEXT, language_id TEXT
            );
            CREATE TABLE IF NOT EXISTS languages (id TEXT PRIMARY KEY, code TEXT);
            INSERT OR IGNORE INTO languages (id, code) VALUES ('en', 'en');
            CREATE TABLE IF NOT EXISTS thesaurus_entry_properties (
                entry_id TEXT NOT NULL, property_key TEXT NOT NULL,
                property_value TEXT NOT NULL, PRIMARY KEY (entry_id, property_key)
            );
            """
        )
        self.conn.execute(
            "INSERT INTO thesaurus_entries (id, term, pos_tag, language_id) VALUES ('e1', 'dog', 'noun', 'en')"
        )
        self.conn.commit()
        ensure_lemma_prompt_schema(self.conn)
        ensure_mayor_dog_mods_schema(self.conn)

    def tearDown(self):
        self.conn.close()

    def test_moddable_target_and_bootstrap(self):
        target = upsert_moddable_target(
            self.conn,
            {
                "targetKind": "lemma_prompt",
                "entryId": "e1",
                "charStart": 0,
                "charEnd": 3,
                "slotKey": "greeting",
                "label": "Greeting",
            },
        )
        self.assertEqual(target["slotKey"], "greeting")
        now = "2020-01-01T00:00:00Z"
        self.conn.execute(
            """INSERT INTO mayor_dog_mods (id, slug, display_name, author_user_id, status, created_at, updated_at)
               VALUES ('mod1', 'test-mod', 'Test Mod', 'author', 'published', ?, ?)""",
            (now, now),
        )
        self.conn.execute(
            """INSERT INTO mod_packages (
                id, mod_id, version, payload_json, status, uploaded_by_user_id,
                published_at, created_at, updated_at
            ) VALUES ('pkg1', 'mod1', '1.0.0', '{}', 'published', 'author', ?, ?, ?)""",
            (now, now, now),
        )
        self.conn.execute(
            """INSERT INTO mod_lemma_overrides (
                id, package_id, target_id, override_text, patch_properties_json,
                composition_patch_json, created_at
            ) VALUES ('lo1', 'pkg1', ?, 'Howdy', '{}', '{}', ?)""",
            (target["id"], now),
        )
        self.conn.execute(
            "INSERT INTO user_enabled_mods (user_id, mod_package_id, priority, enabled_at) VALUES ('player1', 'pkg1', 0, ?)",
            (now,),
        )
        self.conn.commit()
        manifest = build_bootstrap_manifest(self.conn, user_id="player1")
        self.assertEqual(len(manifest["lemmaOverrides"]), 1)
        self.assertEqual(manifest["lemmaOverrides"][0]["overrideText"], "Howdy")

    def test_resolve_mod_placeholders(self):
        ctx = build_mod_context_from_manifest(
            {"lemmaOverrides": [{"slotKey": "greeting", "overrideText": "Hello mod", "priority": 0}]}
        )
        out = resolve_mod_placeholders("Before {M:greeting} after", ctx)
        self.assertEqual(out, "Before Hello mod after")

    def test_expand_with_mod_context(self):
        upsert_lemma_prompt_bundle(self.conn, "e1", {"lemmaPrompt": "Default {M:greeting}"})
        ctx = build_mod_context_from_manifest(
            {"lemmaOverrides": [{"slotKey": "greeting", "overrideText": "MOD", "priority": 0}]}
        )
        data = expand_lemma_prompt(self.conn, "e1", mod_context=ctx)
        self.assertIn("MOD", data["expandedText"])

    def test_audit_mod_section_overlap(self):
        upsert_moddable_target(
            self.conn,
            {
                "targetKind": "episode_section",
                "draftEpisodeId": "draft-1",
                "charStart": 5,
                "charEnd": 10,
                "slotKey": "scene-a",
                "label": "Scene A",
            },
        )
        items = audit_mayor_dog_mod_sections(self.conn, "draft-1", "hello world", "hello brave world")
        self.assertTrue(any(i.item_type == "mayor_dog_mod_section_altered" for i in items))


if __name__ == "__main__":
    unittest.main()
