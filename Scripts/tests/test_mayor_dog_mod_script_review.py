"""Script review integration for Mayor Dog Mod sections."""

import sqlite3
import tempfile
import unittest
from pathlib import Path

from continuum_api.mod_db import audit_mayor_dog_mod_sections, ensure_mayor_dog_mods_schema, upsert_moddable_target
from thesaurus.script_edit_diff import audit_edit


class MayorDogModScriptReviewTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.NamedTemporaryFile(suffix=".db", delete=False)
        self.tmp.close()
        self.conn = sqlite3.connect(self.tmp.name)
        self.conn.row_factory = sqlite3.Row
        root = Path(__file__).resolve().parents[1]
        self.conn.executescript((root / "continuum_mayor_dog_mods_schema.sql").read_text(encoding="utf-8"))
        ensure_mayor_dog_mods_schema(self.conn)

    def tearDown(self):
        self.conn.close()

    def test_mod_token_change_triggers_required(self):
        upsert_moddable_target(
            self.conn,
            {
                "targetKind": "episode_section",
                "draftEpisodeId": "d1",
                "charStart": 0,
                "charEnd": 5,
                "slotKey": "x",
                "label": "x",
            },
        )
        old = "Line one {M:slot-a} end"
        new = "Line one {M:slot-b} end"
        req, _, _ = audit_edit(old, new, [])
        mod_req = audit_mayor_dog_mod_sections(self.conn, "d1", old, new)
        combined = list(req) + mod_req
        types = {i.item_type for i in combined}
        self.assertIn("mayor_dog_mod_section_altered", types)


if __name__ == "__main__":
    unittest.main()
