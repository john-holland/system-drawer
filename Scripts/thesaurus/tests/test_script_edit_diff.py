"""Tests for script_edit_diff audit engine."""

import unittest

from thesaurus.script_edit_diff import audit_edit, compute_edit_regions


class ScriptEditDiffTests(unittest.TestCase):
    def test_append_at_end_no_required(self):
        old = "this is the script {P:hello|key=value}"
        new = old + " world"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "is the"}]
        req, warn, _ = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)

    def test_insert_before_script_warning_shift(self):
        old = "this is the script {P:hello|key=value}"
        new = "this is the, script {P:hello|key=value}"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "is the"}]
        req, warn, updated = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertTrue(len(warn) >= 0)
        self.assertEqual(updated[0]["char_start"], 5)

    def test_overlap_required(self):
        old = "this is the script"
        new = "this WAS the script"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "is the"}]
        req, _, _ = audit_edit(old, new, binding)
        self.assertGreaterEqual(len(req), 1)

    def test_edit_regions_single(self):
        regions = compute_edit_regions("abc", "axbc")
        self.assertEqual(len(regions), 1)
        self.assertEqual(regions[0].offset, 1)


if __name__ == "__main__":
    unittest.main()
