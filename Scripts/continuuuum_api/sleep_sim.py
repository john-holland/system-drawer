"""Sleep wave simulation: electrical sheep storm → N1/N2/deep/REM/light."""

from __future__ import annotations

import hashlib
import math
import random
from typing import Any


PHASES = (
    ("ElectricalSheep", 0.00, 0.15),
    ("N1", 0.15, 0.30),
    ("N2", 0.30, 0.55),
    ("Deep", 0.55, 0.75),
    ("REM", 0.75, 0.92),
    ("Wake", 0.92, 1.00),
)


def _phase_at(t: float) -> str:
    for name, a, b in PHASES:
        if a <= t < b:
            return name
    return "Wake"


def _sample_wave(t: float, seed: int, aspect_satisfied: float) -> float:
    rng = random.Random(seed + int(t * 10000))
    phase = _phase_at(t)
    if phase == "ElectricalSheep":
        return rng.uniform(-1, 1) * (1.2 - aspect_satisfied * 0.3)
    if phase == "N1":
        base = math.sin(t * 40 * math.pi) * 0.3
        return base + rng.uniform(-0.4, 0.4)
    if phase == "N2":
        return math.sin(t * 12 * math.pi) * 0.5 * (1 - t)
    if phase == "Deep":
        return math.sin(t * 3 * math.pi) * 0.8
    if phase == "REM":
        return math.sin(t * 25 * math.pi) * 0.35 + rng.uniform(-0.2, 0.2)
    return (t - 0.92) / 0.08


def run_sleep_sim(
    day_state: dict[str, Any] | None = None,
    day_collapse_seed: int | None = None,
    duration_s: float = 480.0,
    sample_count: int = 256,
) -> dict[str, Any]:
    day_state = day_state or {}
    seed = day_collapse_seed if day_collapse_seed is not None else int(day_state.get("dayCollapseSeed") or 0)
    aspects = day_state.get("aspectStates") or day_state.get("aspects") or []
    satisfied = 0.5
    if aspects:
        vals = [float(a.get("satisfied01", 0.5)) for a in aspects if isinstance(a, dict)]
        satisfied = sum(vals) / max(len(vals), 1)

    samples: list[float] = []
    rem_epochs: list[dict[str, float]] = []
    for i in range(sample_count):
        t = i / max(sample_count - 1, 1)
        samples.append(_sample_wave(t, seed, satisfied))
        if _phase_at(t) == "REM" and (i == 0 or _phase_at((i - 1) / max(sample_count - 1, 1)) != "REM"):
            rem_epochs.append({"tStart": t, "tEnd": min(1.0, t + 0.17)})

    phase_markers = [{"name": name, "tStart": a, "tEnd": b} for name, a, b in PHASES]
    io_stats = {
        "entropyStart": _entropy(samples[: max(1, sample_count // 8)]),
        "entropyEnd": _entropy(samples[-max(1, sample_count // 8) :]),
        "meanAbs": sum(abs(s) for s in samples) / len(samples),
    }
    return {
        "phases": phase_markers,
        "waveSamples": samples,
        "remEpochs": rem_epochs,
        "ioStats": io_stats,
        "sleepSeed": seed,
        "durationS": duration_s,
    }


def complete_night_for_aspect(aspect: dict[str, Any], day_state: dict[str, Any]) -> dict[str, Any]:
    seed = int(day_state.get("dayCollapseSeed") or 0) ^ hash(aspect.get("aspectId", ""))
    wave = run_sleep_sim(day_state, seed, sample_count=64)
    return {
        "aspectId": aspect.get("aspectId"),
        "sleepSeed": seed,
        "waveSamples": wave["waveSamples"],
        "ioStats": wave["ioStats"],
    }


def _entropy(vals: list[float]) -> float:
    if not vals:
        return 0.0
    bins = [0] * 16
    for v in vals:
        idx = min(15, max(0, int((v + 1) * 0.5 * 16)))
        bins[idx] += 1
    total = sum(bins)
    ent = 0.0
    for c in bins:
        if c:
            p = c / total
            ent -= p * math.log(p + 1e-12)
    return ent


def stable_collapse_seed(digest: str) -> int:
    h = hashlib.sha256(digest.encode("utf-8")).hexdigest()
    return int(h[:8], 16)
