"""
Farey nested-interval encoding for thesaurus AST. Tree position is a rational
interval (left_num/left_den, right_num/right_den); insertion between nodes uses
the mediant (a+c)/(b+d) of Farey neighbors to avoid full renumbering.
"""
# todo: review: let's review the differences between this and the Farey interval encoding from wan-os
# eqyptian division with orders of magnitude for the denominators and numerators - should we just use NumPy decimals?
# i like storing them  as strings better because then the infinitely large text blob

from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Iterator


@dataclass(frozen=True)
class FareyInterval:
    """Semi-open interval (left_num/left_den, right_num/right_den] as integers."""
    left_num: int
    left_den: int
    right_num: int
    right_den: int

    def mid_mediant(self) -> tuple[int, int]:
        """Mediant of left and right: (left_num + right_num), (left_den + right_den)."""
        return (self.left_num + self.right_num), (self.left_den + self.right_den)

    def midpoint_rational(self) -> tuple[int, int]:
        """Rational at center of interval (for sort_key)."""
        n, d = self.mid_mediant()
        g = math.gcd(n, d)
        return (n // g, d // g) if g else (n, d)


def mediant_between(a_num: int, a_den: int, b_num: int, b_den: int) -> tuple[int, int]:
    """Farey mediant between a and b: (a_num + b_num), (a_den + b_den)."""
    return (a_num + b_num), (a_den + b_den)


def subinterval(parent: FareyInterval, index: int, count: int) -> FareyInterval:
    """
    Divide parent into count equal Farey subintervals; return the index-th one (0-based).
    Uses mediant chain: between parent.left and parent.right insert (count-1) mediants.
    """
    if count <= 0:
        raise ValueError("count must be positive")
    if index < 0 or index >= count:
        raise ValueError("index must be in [0, count)")
    ln, ld = parent.left_num, parent.left_den
    rn, rd = parent.right_num, parent.right_den
    # Mediant chain: we need count subintervals, so count+1 boundaries
    # boundaries[0] = (ln, ld), boundaries[count] = (rn, rd)
    # boundaries[i] = mediant between boundaries[i-1] and (rn, rd) with weight
    # Simple approach: binary mediant insertion. For index i of count, we want
    # left = fraction i/count along (ln/ld, rn/rd), right = (i+1)/count.
    # Farey: use (ln*(count-i) + rn*i) / (ld*(count-i) + rd*i) for left bound
    # and similar for right. Ensure denominators > 0.
    left_num = ln * (count - index) + rn * index
    left_den = ld * (count - index) + rd * index
    right_num = ln * (count - index - 1) + rn * (index + 1)
    right_den = ld * (count - index - 1) + rd * (index + 1)
    if left_den <= 0:
        left_den = 1
    if right_den <= 0:
        right_den = 1
    return FareyInterval(left_num, left_den, right_num, right_den)


def root_interval() -> FareyInterval:
    """Root node interval (0, 1] in Farey form: (0/1, 1/1]."""
    return FareyInterval(0, 1, 1, 1)


def parent_interval_from_node(parent_node: dict) -> FareyInterval:
    """Build FareyInterval from a parent node dict (farey_left_num, farey_left_den, farey_right_num, farey_right_den)."""
    return FareyInterval(
        parent_node.get("farey_left_num", 0),
        parent_node.get("farey_left_den", 1),
        parent_node.get("farey_right_num", 1),
        parent_node.get("farey_right_den", 1),
    )


def rebalance_intervals(
    ordered_nodes: list[dict],
    parent_interval: FareyInterval | None = None,
) -> list[dict]:
    """
    Assign new Farey intervals to an ordered list of sibling nodes (or root children).
    ordered_nodes: list of dicts with at least id; each gets farey_left_num, farey_left_den, farey_right_num, farey_right_den.
    parent_interval: when None, use root_interval(); otherwise use this interval (for non-root children).
    """
    if not ordered_nodes:
        return []
    parent = parent_interval if parent_interval is not None else root_interval()
    n = len(ordered_nodes)
    out = []
    for i, node in enumerate(ordered_nodes):
        interval = subinterval(parent, i, n)
        new_node = dict(node)
        new_node["farey_left_num"] = interval.left_num
        new_node["farey_left_den"] = interval.left_den
        new_node["farey_right_num"] = interval.right_num
        new_node["farey_right_den"] = interval.right_den
        mid_n, mid_d = interval.midpoint_rational()
        new_node["sort_key"] = mid_n / mid_d if mid_d else 0.0
        out.append(new_node)
    return out


def insert_between(
    left_num: int, left_den: int,
    right_num: int, right_den: int
) -> tuple[int, int, int, int]:
    """
    Return Farey interval for a new node inserted between (left_num/left_den) and (right_num/right_den).
    Returns (new_left_num, new_left_den, new_right_num, new_right_den) so the new node's
    interval is (new_left, new_right] between left and right.
    """
    mid_n = left_num + right_num
    mid_d = left_den + right_den
    # New node occupies (left, mid] so it sits between left and right
    return left_num, left_den, mid_n, mid_d


def ast_inorder_sort_key(node: dict) -> float:
    """Sort key for inorder traversal: use midpoint of Farey interval."""
    ln = node.get("farey_left_num", 0)
    ld = node.get("farey_left_den", 1)
    rn = node.get("farey_right_num", 1)
    rd = node.get("farey_right_den", 1)
    mid = (ln + rn) / (ld + rd) if (ld + rd) else 0.0
    return mid
