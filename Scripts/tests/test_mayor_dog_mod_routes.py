"""Tests for Mayor Dog Mods API and merge helpers."""

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuuuum_api.lemma_prompt import expand_lemma_prompt, ensure_lemma_prompt_schema, upsert_lemma_prompt_bundle
from continuuuum_api.mod_db import (
    audit_mayor_dog_mod_sections,
    build_bootstrap_manifest,
    build_mod_context_from_manifest,
    ensure_mayor_dog_mods_schema,
    list_moddable_targets,
    resolve_mod_placeholders,
    sync_episode_mod_slots,
    sync_lemma_mod_slots,
    upsert_moddable_target,
)


class MayorDogModRoutesTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[1]
        self.conn.executescript((root / "continuuuum_mayor_dog_mods_schema.sql").read_text(encoding="utf-8"))
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY, value_type TEXT, allowed_values_json TEXT,
                default_value TEXT, description TEXT
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

    def test_sync_lemma_mod_slots_from_prompt(self):
        upsert_lemma_prompt_bundle(self.conn, "e1", {"lemmaPrompt": "Say {M:greeting|Hi} friend"})
        n = sync_lemma_mod_slots(self.conn)
        self.assertGreaterEqual(n, 1)
        items = list_moddable_targets(self.conn, target_kind="lemma_prompt")
        keys = {i["slotKey"] for i in items}
        self.assertIn("greeting", keys)
        # idempotent
        sync_lemma_mod_slots(self.conn)
        items2 = list_moddable_targets(self.conn, target_kind="lemma_prompt")
        self.assertEqual(len(items), len(items2))

    def test_sync_episode_mod_slots_from_draft(self):
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS draft_episodes (
                id TEXT PRIMARY KEY, title TEXT, created_at TEXT, updated_at TEXT
            );
            CREATE TABLE IF NOT EXISTS draft_episode_script (
                id TEXT PRIMARY KEY, draft_episode_id TEXT, script_text TEXT,
                created_at TEXT, updated_at TEXT
            );
            """
        )
        self.conn.execute(
            "INSERT INTO draft_episodes (id, title, created_at, updated_at) VALUES ('d1', 'T', 't', 't')"
        )
        self.conn.execute(
            "INSERT INTO draft_episode_script (id, draft_episode_id, script_text, created_at, updated_at) "
            "VALUES ('s1', 'd1', 'INT. ROOM\\n{M:opening}\\nThe end.', 't', 't')"
        )
        self.conn.commit()
        meta = sync_episode_mod_slots(self.conn, "d1")
        self.assertTrue(meta["found"])
        self.assertEqual(meta["synced"], 1)
        items = list_moddable_targets(self.conn, draft_episode_id="d1", target_kind="episode_section")
        self.assertEqual(len(items), 1)
        self.assertTrue(items[0]["slotKey"].endswith("opening") or "opening" in items[0]["slotKey"])


class MayorDogModFlaskApiTests(unittest.TestCase):
    def setUp(self):
        from flask import Flask

        from continuuuum_api.mod_routes import register_mod_routes

        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.db_path = self.tmp.name
        root = Path(__file__).resolve().parents[1]
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        conn.executescript((root / "continuuuum_mayor_dog_mods_schema.sql").read_text(encoding="utf-8"))
        now = "2020-01-01T00:00:00Z"
        conn.execute(
            """INSERT INTO mayor_dog_mods (id, slug, display_name, author_user_id, status, created_at, updated_at)
               VALUES ('mod1', 'test-mod', 'Test Mod', 'author', 'published', ?, ?)""",
            (now, now),
        )
        conn.execute(
            """INSERT INTO moddable_targets (
                id, target_kind, entry_id, draft_episode_id, composition_child_index,
                char_start, char_end, farey_left, farey_right, slot_key, label, description,
                source_hash, created_at, updated_at
            ) VALUES ('t1', 'lemma_prompt', 'e1', NULL, NULL, 0, 3, NULL, NULL, 'greeting', 'Greeting', NULL, NULL, ?, ?)""",
            (now, now),
        )
        conn.execute(
            """INSERT INTO mod_packages (
                id, mod_id, version, payload_json, status, uploaded_by_user_id,
                published_at, created_at, updated_at
            ) VALUES ('pkg1', 'mod1', '1.0.0', '{}', 'published', 'author', ?, ?, ?)""",
            (now, now, now),
        )
        conn.execute(
            """INSERT INTO mod_lemma_overrides (
                id, package_id, target_id, override_text, patch_properties_json,
                composition_patch_json, created_at
            ) VALUES ('lo1', 'pkg1', 't1', 'Howdy', '{}', '{}', ?)""",
            (now,),
        )
        conn.commit()
        conn.close()

        self._user = "author"

        def get_conn():
            c = sqlite3.connect(self.db_path)
            c.row_factory = sqlite3.Row
            return c

        def get_user():
            return self._user

        app = Flask(__name__)
        app.config["TESTING"] = True
        register_mod_routes(app, get_conn, get_user)
        static = root / "continuuuum_api" / "static" / "mayor-dog-mods"

        @app.route("/mayor-dog-mods", strict_slashes=False)
        def _portal():
            return (static / "index.html").read_text(encoding="utf-8")

        self.client = app.test_client()

    def test_get_mod_detail_with_overrides(self):
        r = self.client.get("/api/mods/mod1")
        self.assertEqual(r.status_code, 200)
        body = r.get_json()
        self.assertEqual(body["displayName"], "Test Mod")
        self.assertEqual(body["latestPackage"]["id"], "pkg1")
        self.assertEqual(len(body["lemmaOverrides"]), 1)
        self.assertEqual(body["lemmaOverrides"][0]["slotKey"], "greeting")
        self.assertEqual(body["lemmaOverrides"][0]["overrideText"], "Howdy")

    def test_patch_display_name_as_author(self):
        r = self.client.patch(
            "/api/mods/mod1",
            json={"displayName": "Renamed Mod"},
            headers={"X-User-ID": "author"},
        )
        self.assertEqual(r.status_code, 200)
        self.assertEqual(r.get_json()["displayName"], "Renamed Mod")

    def test_patch_anonymous_rejected(self):
        self._user = "anonymous"
        r = self.client.patch("/api/mods/mod1", json={"displayName": "Nope"})
        self.assertEqual(r.status_code, 401)
        self.assertEqual(r.get_json().get("code"), "presence_required")

    def test_patch_forbidden_for_other_user(self):
        self._user = "user2"
        r = self.client.patch("/api/mods/mod1", json={"displayName": "Nope"})
        self.assertEqual(r.status_code, 403)

    def test_portal_html_loads_user_session(self):
        r = self.client.get("/mayor-dog-mods")
        self.assertEqual(r.status_code, 200)
        html = r.get_data(as_text=True)
        self.assertIn("continuuuum-user-session.js", html)
        self.assertIn("mod-portal.js", html)
        self.assertIn("md-modal", html)


class UserSessionScriptSmokeTests(unittest.TestCase):
    def test_session_script_exports_ensure_present(self):
        root = Path(__file__).resolve().parents[1]
        js = (
            root
            / "continuuuum_api"
            / "static"
            / "shared"
            / "continuuuum-user-session"
            / "continuuuum-user-session.js"
        ).read_text(encoding="utf-8")
        self.assertIn("ensurePresent", js)
        self.assertIn("applyPreset", js)
        for name in ("developer", "admin", "user1", "user6"):
            self.assertIn(name, js)


if __name__ == "__main__":
    unittest.main()
