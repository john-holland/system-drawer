"""Unit tests for bulk auto-add single lemmas."""

from __future__ import annotations

import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuuuum_api.lemma_auto_bind import (
    AUTO_ADD_TYPES,
    build_span_candidates,
    classify_action_type,
    enumerate_candidate_spans,
    normalize_priority,
    pick_auto_select_lemma,
    resolve_winning_action,
    swap_priority_slots,
)
from continuuuum_api.localization_helpers import ensure_clause_binding_columns


class LemmaAutoBindPureTests(unittest.TestCase):
    def test_swap_priority_slots_exchanges_duplicates(self):
        base = normalize_priority(list(AUTO_ADD_TYPES))
        swapped = swap_priority_slots(base, 0, "prefab")
        self.assertEqual(swapped[0], "prefab")
        self.assertIn("builtin", swapped)

    def test_pick_auto_select_lemma_ambiguous_custom_exact(self):
        items = [
            {"id": "1", "term": "door", "isBuiltIn": False},
            {"id": "2", "term": "door", "isBuiltIn": False},
        ]
        self.assertIsNone(pick_auto_select_lemma(items, "door"))

    def test_pick_auto_select_lemma_prefers_builtin(self):
        items = [
            {"id": "1", "term": "in", "isBuiltIn": False},
            {"id": "2", "term": "in", "isBuiltIn": True},
        ]
        hit = pick_auto_select_lemma(items, "in")
        self.assertEqual(hit["id"], "2")

    def test_priority_builtin_beats_prefab_when_both_match(self):
        vocab = {
            "b1": {"id": "b1", "term": "DOOR", "isBuiltIn": True, "isComposedLemma": False},
            "p1": {"id": "p1", "term": "DOOR", "isBuiltIn": False, "prefabId": "prefab-1", "isComposedLemma": False},
        }
        candidates = [
            {"bindingKind": "lemma", "entryId": "b1", "_entry": vocab["b1"]},
            {"bindingKind": "lemma", "entryId": "p1", "_entry": vocab["p1"]},
        ]
        action, reason = resolve_winning_action(
            candidates,
            priority=normalize_priority(list(AUTO_ADD_TYPES)),
            new_lemma_required=False,
            vocabulary=vocab,
            selection_text="DOOR",
            scope_single_chip=False,
        )
        self.assertIsNotNone(action)
        self.assertEqual(reason, "builtin")
        self.assertEqual(action.get("entryId"), "b1")

    def test_scope_c_skips_multiple_suggestions(self):
        vocab = {
            "b1": {"id": "b1", "term": "DOOR", "isBuiltIn": True},
            "p1": {"id": "p1", "term": "DOOR", "prefabId": "prefab-1"},
        }
        candidates = [
            {"bindingKind": "lemma", "entryId": "b1", "_entry": vocab["b1"]},
            {"bindingKind": "lemma", "entryId": "p1", "_entry": vocab["p1"]},
        ]
        action, reason = resolve_winning_action(
            candidates,
            priority=normalize_priority(list(AUTO_ADD_TYPES)),
            new_lemma_required=False,
            vocabulary=vocab,
            selection_text="DOOR",
            scope_single_chip=True,
        )
        self.assertIsNone(action)
        self.assertEqual(reason, "ambiguous")

    def test_new_lemma_required_when_no_candidates(self):
        action, reason = resolve_winning_action(
            [],
            priority=normalize_priority(list(AUTO_ADD_TYPES)),
            new_lemma_required=True,
            vocabulary={},
            selection_text="UNIQUEWORD",
            span={"selectionText": "UNIQUEWORD", "charStart": 0, "charEnd": 10},
            draft_id="draft-1",
            scope_single_chip=True,
        )
        self.assertIsNotNone(action)
        self.assertEqual(reason, "new_lemma")

    def test_new_lemma_not_used_when_disabled(self):
        action, reason = resolve_winning_action(
            [],
            priority=normalize_priority(list(AUTO_ADD_TYPES)),
            new_lemma_required=False,
            vocabulary={},
            selection_text="UNIQUEWORD",
            scope_single_chip=True,
        )
        self.assertIsNone(action)
        self.assertEqual(reason, "no_match")

    def test_classify_skips_composed_lemma(self):
        entry = {"id": "c1", "isComposedLemma": True, "term": "kitchen"}
        tpl = {"bindingKind": "lemma", "entryId": "c1", "_entry": entry}
        self.assertEqual(classify_action_type(tpl, {"c1": entry}), "")

    def test_enumerate_candidate_spans_tokens_and_character(self):
        text = "ALICE\nHello world."
        spans = enumerate_candidate_spans(text)
        texts = [s["selectionText"] for s in spans]
        self.assertIn("ALICE", texts)
        self.assertIn("Hello", texts)
        self.assertIn("world.", texts)


class LemmaAutoBindDbTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[2]
        for name in (
            "continuuuum_dictionary_schema.sql",
            "continuuuum_draft_schema.sql",
            "continuuuum_episodes_schema.sql",
            "continuuuum_localization_schema.sql",
            "continuuuum_localization_workflow_schema.sql",
        ):
            path = root / name
            if path.is_file():
                self.conn.executescript(path.read_text(encoding="utf-8"))
        ensure_clause_binding_columns(self.conn)
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS thesaurus_entries (
                id TEXT PRIMARY KEY, term TEXT, pos_tag TEXT, language_id TEXT
            );
            CREATE TABLE IF NOT EXISTS languages (id TEXT PRIMARY KEY, code TEXT);
            INSERT OR IGNORE INTO languages (id, code) VALUES ('lang-en', 'en');
            """
        )
        self.conn.execute(
            """INSERT INTO draft_episodes
               (id, title, created_by, t_start, t_end, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?)""",
            ("draft-1", "Test", "author", 0.0, 1.0, "2020-01-01T00:00:00Z", "2020-01-01T00:00:00Z"),
        )
        self.conn.execute(
            """INSERT INTO draft_episode_script
               (id, draft_episode_id, script_text, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?)""",
            ("script-1", "draft-1", "ALICE\nHello.", "2020-01-01T00:00:00Z", "2020-01-01T00:00:00Z"),
        )
        self.conn.execute(
            "INSERT INTO thesaurus_entries (id, term, pos_tag, language_id) VALUES (?, ?, ?, ?)",
            ("entry-alice", "ALICE", "noun", "lang-en"),
        )
        self.conn.commit()

    def tearDown(self):
        self.conn.close()

    def test_build_span_candidates_skips_composed_lemma(self):
        vocabulary = {
            "composed-1": {
                "id": "composed-1",
                "term": "ALICE",
                "isComposedLemma": True,
                "isBuiltIn": False,
            }
        }
        candidates = build_span_candidates(
            self.conn,
            {"charStart": 0, "charEnd": 5, "selectionText": "ALICE"},
            draft_id="draft-1",
            draft_script_id="script-1",
            vocabulary=vocabulary,
            existing_bindings=[],
            script_text="ALICE\nHello.",
        )
        lemma_candidates = [c for c in candidates if c.get("bindingKind") == "lemma"]
        self.assertEqual(len(lemma_candidates), 0)


if __name__ == "__main__":
    unittest.main()
