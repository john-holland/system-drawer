"""Tests for clause audit and effective property resolution."""

import unittest

from thesaurus.clause_audit import char_to_farey, farey_contains, farey_to_char, resolve_effective_properties


class ClauseAuditTests(unittest.TestCase):
    def test_char_to_farey_proportional(self):
        text = "abcdefghij"
        ln, ld, rn, rd = char_to_farey(text, 2, 5)
        self.assertEqual((ln, ld), (1, 5))
        self.assertEqual((rn, rd), (1, 2))

    def test_farey_contains(self):
        outer = (0, 1, 1, 1)
        inner = (1, 4, 1, 2)
        self.assertTrue(farey_contains(outer, inner))

    def test_farey_to_char_round_trip(self):
        text = "abcdefghij"
        cs, ce = 2, 5
        ln, ld, rn, rd = char_to_farey(text, cs, ce)
        back_cs, back_ce = farey_to_char(text, ln, ld, rn, rd)
        self.assertEqual((back_cs, back_ce), (cs, ce))

    def test_resolve_precedence_prompt_over_clause(self):
        bindings = [{"property_key": "non-ik-animation", "property_value": "false", "binding_kind": "property"}]
        val = resolve_effective_properties(
            "non-ik-animation",
            bindings,
            {},
            {"non-ik-animation": "false"},
            prompt_value="true",
        )
        self.assertEqual(val, "true")

    def test_resolve_clause_over_entry(self):
        bindings = [{"property_key": "non-ik-animation", "property_value": "true", "binding_kind": "property"}]
        val = resolve_effective_properties(
            "non-ik-animation",
            bindings,
            {"non-ik-animation": "false"},
            {"non-ik-animation": "false"},
        )
        self.assertEqual(val, "true")

    def test_resolve_entry_over_spec_default(self):
        val = resolve_effective_properties(
            "non-ik-animation",
            [],
            {"non-ik-animation": "true"},
            {"non-ik-animation": "false"},
        )
        self.assertEqual(val, "true")


if __name__ == "__main__":
    unittest.main()
