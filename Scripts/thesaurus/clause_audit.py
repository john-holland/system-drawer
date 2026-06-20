"""Clause audit helpers — Farey containment wrappers."""

from __future__ import annotations

from typing import Optional, Tuple


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


def char_to_farey_stub(_script_text: str, char_start: int, char_end: int) -> Tuple[int, int, int, int]:
    """Stub: map char range to document root until AST bridge wired."""
    return (0, 1, 1, 1)
