"""Tests for audit_binding_edit."""

import unittest

from thesaurus.script_edit_diff import audit_binding_edit


class BindingEditAuditTests(unittest.TestCase):
    def test_property_only_change(self):
        old = {
            "id": "b1",
            "char_start": 5,
            "char_end": 11,
            "selection_text": "clause",
            "property_key": "non-ik-animation",
            "property_value": "false",
        }
        new = dict(old)
        new["property_value"] = "true"
        req, warn = audit_binding_edit(old, new, "AAAA clause BBBB")
        self.assertEqual(len(req), 1)
        self.assertEqual(req[0].item_type, "binding_property_updated")
        self.assertEqual(len(warn), 0)

    def test_span_only_change(self):
        old = {
            "id": "b1",
            "char_start": 5,
            "char_end": 11,
            "selection_text": "clause",
            "property_key": "x",
            "property_value": "y",
        }
        new = dict(old)
        new["char_start"] = 10
        new["char_end"] = 16
        req, warn = audit_binding_edit(old, new, "XXXX AAAA clause BBBB")
        self.assertEqual(len(req), 1)
        self.assertEqual(req[0].item_type, "binding_span_updated")
        self.assertEqual(len(warn), 0)

    def test_span_change_with_selection_mismatch_warning(self):
        old = {
            "id": "b1",
            "char_start": 5,
            "char_end": 11,
            "selection_text": "clause",
            "property_key": "x",
            "property_value": "y",
        }
        new = dict(old)
        new["char_start"] = 0
        new["char_end"] = 4
        req, warn = audit_binding_edit(old, new, "AAAA clause BBBB")
        self.assertEqual(len(req), 1)
        self.assertEqual(req[0].item_type, "binding_span_updated")
        self.assertEqual(len(warn), 1)
        self.assertEqual(warn[0].item_type, "binding_selection_mismatch")

    def test_combined_property_and_span(self):
        old = {
            "id": "b1",
            "char_start": 5,
            "char_end": 11,
            "selection_text": "clause",
            "property_key": "a",
            "property_value": "1",
        }
        new = {
            "id": "b1",
            "char_start": 6,
            "char_end": 12,
            "selection_text": "clause",
            "property_key": "b",
            "property_value": "2",
        }
        req, _ = audit_binding_edit(old, new, "AAAA clause BBBB")
        types = {i.item_type for i in req}
        self.assertIn("binding_property_updated", types)
        self.assertIn("binding_span_updated", types)

    def test_no_op_returns_empty(self):
        old = {"id": "b1", "char_start": 1, "char_end": 2, "property_key": "k", "property_value": "v"}
        req, warn = audit_binding_edit(old, dict(old), "ab")
        self.assertEqual(req, [])
        self.assertEqual(warn, [])


if __name__ == "__main__":
    unittest.main()
