"""Stable day collapse seed from aspect digests."""

from __future__ import annotations

from continuuuum_api.dream_cycle import complete_day_for_aspect, compute_day_collapse_seed


def test_collapse_seed_stable():
    snapshot = {"healthcare_coverage": 0.8, "civic_trust": 0.6}
    states = [
        complete_day_for_aspect("need_physiological", snapshot),
        complete_day_for_aspect("need_safety", snapshot),
    ]
    a = compute_day_collapse_seed(states)
    b = compute_day_collapse_seed(list(reversed(states)))
    c = compute_day_collapse_seed(sorted(states, key=lambda x: x["aspectId"]))
    assert a == b == c
    assert a == compute_day_collapse_seed(states)


if __name__ == "__main__":
    test_collapse_seed_stable()
    print("ok")
