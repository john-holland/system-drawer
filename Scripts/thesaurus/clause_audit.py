"""Clause audit helpers — Farey containment and char-to-Farey mapping."""

from __future__ import annotations

import math
from fractions import Fraction
from typing import Any, Iterable, List, Optional, Sequence, Tuple


def farey_contains(outer: Tuple[int, int, int, int], inner: Tuple[int, int, int, int]) -> bool:
    """True when inner (ln, ld, rn, rd) interval is contained in outer (open-closed)."""
    ln, ld, rn, rd = inner
    oln, old, orn, ord_ = outer
    if ld <= 0 or rd <= 0 or old <= 0 or ord_ <= 0:
        return False
    i_left = ln / ld
    i_right = rn / rd
    o_left = oln / old
    o_right = orn / ord_
    return i_left >= o_left - 1e-12 and i_right <= o_right + 1e-12


def _farey_tuple(node: dict) -> Tuple[int, int, int, int]:
    return (
        int(node.get("farey_left_num") or node.get("fareyLeftNum") or 0),
        int(node.get("farey_left_den") or node.get("fareyLeftDen") or 1),
        int(node.get("farey_right_num") or node.get("fareyRightNum") or 1),
        int(node.get("farey_right_den") or node.get("fareyRightDen") or 1),
    )


def char_to_farey(
    script_text: str,
    char_start: int,
    char_end: int,
    ast_nodes: Optional[Sequence[dict]] = None,
) -> Tuple[int, int, int, int]:
    """Map char range to Farey interval; prefer smallest containing AST node when available."""
    n = max(len(script_text or ""), 1)
    cs = max(0, min(int(char_start), n))
    ce = max(cs, min(int(char_end), n))
    left = Fraction(cs, n)
    right = Fraction(ce, n)
    ln, ld = left.numerator, left.denominator
    rn, rd = right.numerator, right.denominator

    if ast_nodes:
        best: Optional[Tuple[int, int, int, int]] = None
        best_width = float("inf")
        for node in ast_nodes:
            ft = _farey_tuple(node)
            if farey_contains(ft, (ln, ld, rn, rd)):
                width = ft[2] / ft[3] - ft[0] / ft[1]
                if width < best_width:
                    best_width = width
                    best = ft
        if best:
            return best

    g1 = math.gcd(ln, ld)
    g2 = math.gcd(rn, rd)
    return (ln // g1, ld // g1, rn // g2, rd // g2)


def char_to_farey_stub(script_text: str, char_start: int, char_end: int) -> Tuple[int, int, int, int]:
    """Backward-compatible alias."""
    return char_to_farey(script_text, char_start, char_end, None)


def farey_to_char(
    script_text: str,
    ln: int,
    ld: int,
    rn: int,
    rd: int,
) -> Tuple[int, int]:
    """Map Farey interval back to char range using proportional document root."""
    n = max(len(script_text or ""), 1)
    if ld <= 0 or rd <= 0:
        return 0, 0
    char_start = max(0, min(n, (int(ln) * n) // int(ld)))
    char_end = max(char_start, min(n, (int(rn) * n) // int(rd)))
    return char_start, char_end


def resolve_binding_char_span(binding: dict, script_text: str = "") -> Tuple[int, int]:
    """Return display char span for a clause binding, falling back to Farey when cache is empty."""
    cs = int(binding.get("char_start") or binding.get("charStart") or 0)
    ce = int(binding.get("char_end") or binding.get("charEnd") or 0)
    if ce > cs:
        return cs, ce
    ln = int(binding.get("farey_left_num") or binding.get("fareyLeftNum") or 0)
    ld = int(binding.get("farey_left_den") or binding.get("fareyLeftDen") or 1)
    rn = int(binding.get("farey_right_num") or binding.get("fareyRightNum") or 1)
    rd = int(binding.get("farey_right_den") or binding.get("fareyRightDen") or 1)
    return farey_to_char(script_text, ln, ld, rn, rd)


def resolve_effective_properties(
    property_key: str,
    clause_bindings: Iterable[dict],
    entry_properties: dict,
    spec_defaults: dict,
    char_start: Optional[int] = None,
    char_end: Optional[int] = None,
    prompt_value: Optional[str] = None,
    dimension: Optional[int] = None,
    dimension_entry_properties: Optional[dict] = None,
) -> Optional[str]:
    """Resolution: prompt inline → clause property binding → entry property → spec default.

    When ``dimension`` is set and ``dimension_entry_properties`` is provided (dim-0 bag
    already overlaid with active-dim overrides), that bag is used instead of
    ``entry_properties``. Callers typically build it via game_dimension_dao.resolve_entry_properties.
    """
    if prompt_value is not None:
        return prompt_value

    for b in clause_bindings or []:
        kind = b.get("binding_kind") or b.get("bindingKind") or ""
        pk = b.get("property_key") or b.get("propertyKey") or ""
        if kind not in ("property", "localization") or pk != property_key:
            continue
        if char_start is not None and char_end is not None:
            bs = int(b.get("char_start") or b.get("charStart") or 0)
            be = int(b.get("char_end") or b.get("charEnd") or 0)
            if be <= char_start or bs >= char_end:
                continue
        return b.get("property_value") or b.get("propertyValue")

    bag = entry_properties
    if dimension is not None and dimension_entry_properties is not None:
        bag = dimension_entry_properties
    if property_key in bag:
        return bag[property_key]

    if property_key in spec_defaults:
        return spec_defaults[property_key]

    return None
