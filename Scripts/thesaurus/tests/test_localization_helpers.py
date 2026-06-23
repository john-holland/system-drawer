"""Tests for localization_helpers clause ref resolution."""

import sqlite3
import unittest

from continuum_api.localization_helpers import resolve_clause_ref_farey


class ResolveClauseRefFareyTests(unittest.TestCase):
    def setUp(self):
        self.conn = sqlite3.connect(":memory:")
        self.conn.row_factory = sqlite3.Row
        self.conn.executescript(
            """
            CREATE TABLE draft_episode_script (
                id TEXT PRIMARY KEY,
                draft_episode_id TEXT NOT NULL,
                episode_script_id TEXT
            );
            CREATE TABLE thesaurus_ast_nodes (
                id TEXT PRIMARY KEY,
                farey_left_num INTEGER NOT NULL,
                farey_left_den INTEGER NOT NULL,
                farey_right_num INTEGER NOT NULL,
                farey_right_den INTEGER NOT NULL,
                token_or_phrase TEXT NOT NULL,
                language_id TEXT NOT NULL,
                episode_script_id TEXT
            );
            """
        )
        self.conn.execute(
            "INSERT INTO draft_episode_script (id, draft_episode_id, episode_script_id) VALUES (?, ?, ?)",
            ("draft-script-1", "draft-1", "ep-script-1"),
        )
        self.conn.execute(
            """INSERT INTO thesaurus_ast_nodes (
                id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                token_or_phrase, language_id, episode_script_id
            ) VALUES (?, 0, 1, 1, 2, 'hello', 'lang-en', ?)""",
            ("node-1", "ep-script-1"),
        )

    def tearDown(self):
        self.conn.close()

    def test_char_start_zero_recomputes_farey_without_sql_error(self):
        """Caret at index 0 sends default Farey (0/1, 1/n) and must not reference missing columns."""
        body = {
            "draftScriptId": "draft-script-1",
            "charStart": 0,
            "charEnd": 1,
            "fareyLeftNum": 0,
            "fareyLeftDen": 1,
            "fareyRightNum": 1,
            "fareyRightDen": 10,
        }
        ref = resolve_clause_ref_farey(self.conn, body, "hello world")
        self.assertEqual(ref.char_start, 0)
        self.assertEqual(ref.char_end, 1)
        self.assertGreater(ref.farey_left_den, 0)
        self.assertGreater(ref.farey_right_den, 0)

    def test_uses_ast_nodes_when_linked(self):
        body = {
            "draftScriptId": "draft-script-1",
            "charStart": 0,
            "charEnd": 5,
            "fareyLeftNum": 0,
            "fareyLeftDen": 1,
            "fareyRightNum": 1,
            "fareyRightDen": 1,
        }
        ref = resolve_clause_ref_farey(self.conn, body, "hello world")
        self.assertEqual((ref.farey_left_num, ref.farey_left_den, ref.farey_right_num, ref.farey_right_den), (0, 1, 1, 2))


if __name__ == "__main__":
    unittest.main()
