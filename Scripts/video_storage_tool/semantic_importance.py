"""
Semantic frame importance for video causality. Scores frames by caption/embedding
similarity and applies sparse sampling to select important frames for causality linking.
Replaces or complements uniform frame dropping; can feed into willcrichton/hyperlapse later.
"""

import logging
from pathlib import Path
from typing import Callable

log = logging.getLogger(__name__)


def compute_frame_importance(
    script_path: Path,
    frame_timestamps: list[float],
    frame_captions: list[str] | None = None,
    *,
    top_k: int = 30,
    min_gap_sec: float = 2.0,
) -> list[tuple[int, float, float]]:
    """
    Compute importance scores for frames. Returns list of (frame_index, timestamp, score).
    Uses caption diversity / keyphrase overlap as proxy when no embeddings available.

    Args:
        script_path: Path to script.txt (for vocabulary/keyphrase extraction)
        frame_timestamps: Timestamps for each frame
        frame_captions: Optional per-frame captions (from BLIP); if None, uses uniform sampling
        top_k: Max number of frames to keep
        min_gap_sec: Minimum gap between selected frames (temporal smoothing)

    Returns:
        List of (frame_index, timestamp, importance_score) sorted by timestamp
    """
    if not frame_timestamps:
        return []

    if frame_captions is None or len(frame_captions) != len(frame_timestamps):
        # Fallback: uniform importance, apply sparse sampling by gap
        scores = [1.0] * len(frame_timestamps)
    else:
        # Simple heuristic: score by caption length + uniqueness (naive proxy for "informative")
        seen = set()
        scores = []
        for cap in frame_captions:
            norm = (cap or "").strip().lower()
            words = set(norm.split()) if norm else set()
            overlap = len(seen & words)
            seen |= words
            # Prefer frames with new vocabulary
            score = 0.5 + 0.5 * (1.0 - min(1.0, overlap / max(1, len(words))))
            scores.append(score)

    # Sparse sampling: keep top_k by score, enforcing min_gap_sec
    indexed = [(i, frame_timestamps[i], scores[i]) for i in range(len(frame_timestamps))]
    indexed.sort(key=lambda x: -x[2])  # descending by score

    result: list[tuple[int, float, float]] = []
    last_t = -999.0
    for i, t, s in indexed:
        if len(result) >= top_k:
            break
        if t - last_t >= min_gap_sec:
            result.append((i, t, s))
            last_t = t

    result.sort(key=lambda x: x[1])  # by timestamp
    return result


def causality_candidates_from_script(
    script_path: Path,
    config: dict | None = None,
) -> list[tuple[float, str, float]]:
    """
    Extract causality candidates from script: (timestamp, term, weight).
    Parses [Transcript] and [Visual description] sections; uses simple keyword extraction.
    Full implementation would use vocabulary definitions and NLP.

    Returns:
        List of (timestamp_estimate, term, causal_weight)
    """
    config = config or {}
    if not script_path.exists():
        log.warning("Script not found: %s", script_path)
        return []

    text = script_path.read_text(encoding="utf-8")
    # Dummy: no timestamps in script.txt by default; return term-weighted chunks
    words = text.lower().split()
    # Simple term frequency as weight proxy
    from collections import Counter
    counts = Counter(words)
    max_c = max(counts.values()) if counts else 1
    vocab = config.get("vocabulary") or {}
    # If vocabulary provided, prefer those terms
    result: list[tuple[float, str, float]] = []
    for w, c in counts.most_common(50):
        if len(w) < 3:
            continue
        weight = c / max_c
        if w in vocab:
            weight *= 1.5
        result.append((0.0, w, weight))
    return result[:20]
