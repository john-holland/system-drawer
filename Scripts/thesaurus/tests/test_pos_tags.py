"""Tests for canonical POS tag catalog."""

import unittest

from thesaurus.pos_tags import list_pos_tags, normalize_pos_tag, pos_segment


class PosTagTests(unittest.TestCase):
    def test_normalize_pos_tag(self):
        self.assertEqual(normalize_pos_tag("Noun"), "noun")
        self.assertEqual(normalize_pos_tag(""), "unknown")
        self.assertEqual(normalize_pos_tag(None), "unknown")

    def test_pos_segment_builtin_paths(self):
        self.assertEqual(pos_segment("determiner"), "det")
        self.assertEqual(pos_segment("type_name"), "literal")
        self.assertEqual(pos_segment("verb"), "verb")

    def test_catalog_includes_literal_type_name(self):
        tags = {row["posTag"] for row in list_pos_tags()}
        self.assertIn("type_name", tags)
        self.assertIn("noun", tags)


if __name__ == "__main__":
    unittest.main()
