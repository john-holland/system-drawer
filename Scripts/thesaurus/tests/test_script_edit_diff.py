"""Tests for script_edit_diff audit engine."""

import unittest

from thesaurus.script_edit_diff import (
    audit_edit,
    bindings_to_spans,
    compute_edit_regions,
    _shift_span,
    EditRegion,
    SpanRef,
)


class ScriptEditDiffTests(unittest.TestCase):
    def test_append_at_end_no_required(self):
        old = "this is the script {P:hello|key=value}"
        new = old + " world"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "is the"}]
        req, warn, _ = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertEqual(len(warn), 0)

    def test_insert_before_clause_emits_shift_warning(self):
        old = "AAAA clause BBBB"
        new = "XXXX AAAA clause BBBB"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "clause"}]
        req, warn, updated = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertEqual(len(warn), 1)
        self.assertEqual(warn[0].item_type, "auto_fixed_offset")
        self.assertEqual(updated[0]["char_start"], 10)
        self.assertEqual(updated[0]["char_end"], 16)

    def test_insert_at_clause_start_reanchors_to_first_letter(self):
        old = "AAAA clause BBBB"
        new = "AAAA X clause BBBB"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "clause"}]
        req, warn, updated = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertEqual(len(warn), 1)
        self.assertEqual(updated[0]["char_start"], 7)
        self.assertEqual(updated[0]["char_end"], 13)

    def test_whitespace_delete_inside_clause_shifts_span(self):
        old = "X cl ause Y"
        new = "X clause Y"
        binding = [{"id": "b1", "char_start": 2, "char_end": 9, "selection_text": "cl ause"}]
        req, warn, updated = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertEqual(updated[0]["char_start"], 2)
        self.assertEqual(updated[0]["char_end"], 8)

    def test_char_delete_inside_clause_requires_review(self):
        old = "X clause Y"
        new = "X cluse Y"
        binding = [{"id": "b1", "char_start": 2, "char_end": 8, "selection_text": "clause"}]
        req, _, _ = audit_edit(old, new, binding)
        self.assertGreaterEqual(len(req), 1)

    def test_insert_at_clause_start_is_overlap_required(self):
        old = "AAAA clause BBBB"
        new = "AAAA clXuse BBBB"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "clause"}]
        req, warn, _ = audit_edit(old, new, binding)
        self.assertEqual(len(warn), 0)
        self.assertGreaterEqual(len(req), 1)

    def test_insert_after_clause_no_warning(self):
        old = "AAAA clause BBBB"
        new = "AAAA clause ZZZZ BBBB"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "clause"}]
        req, warn, updated = audit_edit(old, new, binding)
        self.assertEqual(len(req), 0)
        self.assertEqual(len(warn), 0)
        self.assertEqual(updated[0]["char_start"], 5)
        self.assertEqual(updated[0]["char_end"], 11)

    def test_overlap_required(self):
        old = "this is the script"
        new = "this WAS the script"
        binding = [{"id": "b1", "char_start": 5, "char_end": 11, "selection_text": "is the"}]
        req, _, _ = audit_edit(old, new, binding)
        self.assertGreaterEqual(len(req), 1)

    def test_bindings_to_spans_falls_back_to_farey(self):
        text = "abcdefghij"
        binding = {
            "id": "b1",
            "char_start": 0,
            "char_end": 0,
            "farey_left_num": 1,
            "farey_left_den": 5,
            "farey_right_num": 1,
            "farey_right_den": 2,
            "selection_text": "cde",
        }
        spans = bindings_to_spans([binding], text)
        self.assertEqual(spans[0].char_start, 2)
        self.assertEqual(spans[0].char_end, 5)

    def test_shift_span_pure_insert_before(self):
        span = SpanRef(10, 16)
        edit = EditRegion(offset=0, old_len=0, new_len=4)
        ns, ne, overlapped, shifted = _shift_span(span, edit)
        self.assertEqual((ns, ne), (14, 20))
        self.assertFalse(overlapped)
        self.assertTrue(shifted)

    def test_edit_regions_single(self):
        regions = compute_edit_regions("abc", "axbc")
        self.assertEqual(len(regions), 1)
        self.assertEqual(regions[0].offset, 1)


if __name__ == "__main__":
    unittest.main()
