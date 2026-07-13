"""Play-within-dream statistical improbability + script reproduction coeff bake.

Plain float/string score records. Never gates developer narrative: weak foundation
clamps improbability01 to 1.0 (escapism preview); strong foundation yields
play-thought unpack.
"""

from __future__ import annotations

import hashlib
import math
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any

UNWRAP_ESCAPISM_PREVIEW = "escapism_preview"
UNWRAP_PLAY_THOUGHT = "play_thought_unpack"

DEFAULT_THRESHOLD = 0.72
DEFAULT_INDUCTION_FLOOR = 0.25
DEFAULT_REPRODUCTION_COEFF = 0.5
DEFAULT_BASE_NEST = 0.55
W_TABLE_READ = 0.5
W_LEMMA = 0.35
W_USC = 0.15


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def clamp01(value: float) -> float:
    return max(0.0, min(1.0, float(value)))


def _column_exists(conn: sqlite3.Connection, table: str, column: str) -> bool:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return any(row[1] == column for row in cur.fetchall())


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_script_reproduction_columns(conn: sqlite3.Connection) -> None:
    """Add dream_reproduction_coeff to script tables when missing."""
    for table in ("draft_episode_script", "episode_script"):
        if not _table_exists(conn, table):
            continue
        if not _column_exists(conn, table, "dream_reproduction_coeff"):
            conn.execute(
                f"ALTER TABLE {table} ADD COLUMN dream_reproduction_coeff REAL NOT NULL DEFAULT 0.5"
            )
    conn.commit()


def mean_logistic_coeffs(coeffs: list[float]) -> float:
    if not coeffs:
        return DEFAULT_REPRODUCTION_COEFF
    return clamp01(sum(coeffs) / len(coeffs))


def logistic_coeff_for_lemma(entry_id: str | None, explicit: float | None = None) -> float:
    """Per-lemma logistic coeff in [0,1]. Explicit wins; else stable default 0.5."""
    if explicit is not None:
        return clamp01(explicit)
    if not entry_id:
        return DEFAULT_REPRODUCTION_COEFF
    # Stable mid-band default so empty metadata does not force preview_only.
    return DEFAULT_REPRODUCTION_COEFF


def collect_prompt_lemma_coeffs(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    *,
    explicit_coeffs: list[float] | None = None,
) -> list[float]:
    """Logistic coeffs for current prompt/lemma bindings on a draft."""
    if explicit_coeffs is not None:
        return [clamp01(c) for c in explicit_coeffs]

    ensure_script_reproduction_columns(conn)
    if not _table_exists(conn, "localization_clause_bindings"):
        return []

    cur = conn.execute(
        """SELECT b.entry_id, b.binding_kind, b.property_key, b.property_value,
                  b.prompt_placeholder_name
           FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ?""",
        (draft_episode_id,),
    )
    coeffs: list[float] = []
    for row in cur.fetchall():
        kind = (row["binding_kind"] if "binding_kind" in row.keys() else "") or ""
        pkey = (row["property_key"] if "property_key" in row.keys() else "") or ""
        pval = (row["property_value"] if "property_value" in row.keys() else "") or ""
        entry_id = row["entry_id"] if "entry_id" in row.keys() else None
        is_prompt = (
            kind in ("prompt_placeholder", "lemma")
            or pkey in ("lemma-prompt", "logistic-coeff", "entry-id")
            or bool(row["prompt_placeholder_name"] if "prompt_placeholder_name" in row.keys() else None)
        )
        if not is_prompt:
            continue
        if pkey == "logistic-coeff":
            try:
                coeffs.append(clamp01(float(pval)))
                continue
            except (TypeError, ValueError):
                pass
        coeffs.append(logistic_coeff_for_lemma(str(entry_id) if entry_id else None))
    return coeffs


def bake_dream_reproduction_coeff(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    *,
    explicit_coeffs: list[float] | None = None,
) -> float:
    """Mean logistic coeffs → draft_episode_script.dream_reproduction_coeff (and linked episode_script)."""
    ensure_script_reproduction_columns(conn)
    coeffs = collect_prompt_lemma_coeffs(conn, draft_episode_id, explicit_coeffs=explicit_coeffs)
    value = mean_logistic_coeffs(coeffs)
    now = _now()
    row = conn.execute(
        """SELECT id, episode_script_id FROM draft_episode_script
           WHERE draft_episode_id = ?
           ORDER BY updated_at DESC LIMIT 1""",
        (draft_episode_id,),
    ).fetchone()
    if not row:
        return value
    conn.execute(
        """UPDATE draft_episode_script
           SET dream_reproduction_coeff = ?, updated_at = ?
           WHERE id = ?""",
        (value, now, row["id"]),
    )
    episode_script_id = row["episode_script_id"] if "episode_script_id" in row.keys() else None
    if episode_script_id and _table_exists(conn, "episode_script"):
        conn.execute(
            """UPDATE episode_script
               SET dream_reproduction_coeff = ?
               WHERE id = ?""",
            (value, episode_script_id),
        )
    conn.commit()
    return value


def load_baked_reproduction_coeff(
    conn: sqlite3.Connection | None,
    draft_episode_id: str | None = None,
    *,
    fallback: float = DEFAULT_REPRODUCTION_COEFF,
) -> float:
    if conn is None or not draft_episode_id:
        return clamp01(fallback)
    ensure_script_reproduction_columns(conn)
    if not _table_exists(conn, "draft_episode_script"):
        return clamp01(fallback)
    row = conn.execute(
        """SELECT dream_reproduction_coeff FROM draft_episode_script
           WHERE draft_episode_id = ?
           ORDER BY updated_at DESC LIMIT 1""",
        (draft_episode_id,),
    ).fetchone()
    if not row:
        return clamp01(fallback)
    try:
        return clamp01(float(row["dream_reproduction_coeff"]))
    except (TypeError, ValueError, KeyError):
        return clamp01(fallback)


def _table_read_exposure(conn: sqlite3.Connection | None, session_id: str | None) -> float:
    if not conn or not session_id or not _table_exists(conn, "table_read_sessions"):
        return 0.0
    row = conn.execute(
        "SELECT id FROM table_read_sessions WHERE id = ? LIMIT 1",
        (session_id,),
    ).fetchone()
    if not row:
        return 0.0
    turn_count = 0
    if _table_exists(conn, "table_read_turns"):
        cur = conn.execute(
            "SELECT COUNT(*) AS c FROM table_read_turns WHERE session_id = ?",
            (session_id,),
        ).fetchone()
        turn_count = int(cur["c"] if cur and "c" in cur.keys() else (cur[0] if cur else 0))
    return clamp01(0.35 + min(0.65, turn_count / 40.0))


def _lemma_phrase_novelty(consumed_phrases: list[str] | None) -> float:
    if not consumed_phrases:
        return 0.0
    return clamp01(min(1.0, 0.2 + 0.15 * len(consumed_phrases)))


def _usc_fictional_hours(usc_job_ids: list[str] | None) -> float:
    if not usc_job_ids:
        return 0.0
    return clamp01(min(1.0, 0.25 * len(usc_job_ids)))


def play_fingerprint(inducing_play: dict[str, Any] | None) -> str:
    inducing_play = inducing_play or {}
    raw = "|".join(
        [
            str(inducing_play.get("draftEpisodeId") or inducing_play.get("draft_episode_id") or ""),
            str(inducing_play.get("tableReadSessionId") or inducing_play.get("table_read_session_id") or ""),
            ",".join(inducing_play.get("consumedPhrases") or inducing_play.get("consumed_phrases") or []),
        ]
    )
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()[:16]


def play_echo_match(inducing_play: dict[str, Any] | None, nested_play_event: dict[str, Any] | None) -> float:
    inducing_play = inducing_play or {}
    nested_play_event = nested_play_event or {}
    a = str(inducing_play.get("draftEpisodeId") or inducing_play.get("draft_episode_id") or "")
    b = str(nested_play_event.get("draftEpisodeId") or nested_play_event.get("draft_episode_id") or "")
    if a and b and a == b:
        return 1.0
    phrases = set(inducing_play.get("consumedPhrases") or inducing_play.get("consumed_phrases") or [])
    nested_phrase = nested_play_event.get("phrase") or ""
    if nested_phrase and any(nested_phrase in p or p.endswith(nested_phrase) for p in phrases):
        return 0.85
    if a or b or phrases or nested_phrase:
        return 0.45
    return 0.5


def nest_depth_factor(layer_stack: list[str] | None, base_nest: float = DEFAULT_BASE_NEST) -> tuple[int, float]:
    depth = max(1, len(layer_stack or ["good_day_horizon", "developer_dream"]))
    factor = 1.0 - (1.0 - base_nest) ** depth
    return depth, clamp01(factor)


def _logit(p: float, eps: float = 1e-6) -> float:
    p = clamp01(p)
    p = eps + (1.0 - 2.0 * eps) * p
    return math.log(p / (1.0 - p))


def _softplus(x: float) -> float:
    if x > 30:
        return x
    return math.log1p(math.exp(x))


@dataclass
class ImprobabilityConfig:
    threshold: float = DEFAULT_THRESHOLD
    induction_floor: float = DEFAULT_INDUCTION_FLOOR
    base_nest: float = DEFAULT_BASE_NEST
    softplus_k: float = 1.25
    improbability_cap: float = 8.0


def score_play_improbability(
    *,
    conn: sqlite3.Connection | None = None,
    layer_stack: list[str] | None = None,
    inducing_play: dict[str, Any] | None = None,
    nested_play_event: dict[str, Any] | None = None,
    reproduction_coeff: float | None = None,
    rem_entropy_norm: float = 0.5,
    config: ImprobabilityConfig | None = None,
) -> dict[str, Any]:
    """Always returns a float/string score record. Never gates narrative."""
    cfg = config or ImprobabilityConfig()
    inducing_play = inducing_play or {}
    nested_play_event = nested_play_event or {}

    draft_id = (
        inducing_play.get("draftEpisodeId")
        or inducing_play.get("draft_episode_id")
        or nested_play_event.get("draftEpisodeId")
        or nested_play_event.get("draft_episode_id")
    )
    if reproduction_coeff is None:
        reproduction_coeff = load_baked_reproduction_coeff(conn, draft_id)
    reproduction_coeff = clamp01(reproduction_coeff)
    fidelity01 = clamp01(1.0 - reproduction_coeff)

    table_read = _table_read_exposure(
        conn,
        inducing_play.get("tableReadSessionId") or inducing_play.get("table_read_session_id"),
    )
    lemma_nov = _lemma_phrase_novelty(
        inducing_play.get("consumedPhrases") or inducing_play.get("consumed_phrases")
    )
    usc = _usc_fictional_hours(inducing_play.get("uscJobIds") or inducing_play.get("usc_job_ids"))
    induction_prior = clamp01(W_TABLE_READ * table_read + W_LEMMA * lemma_nov + W_USC * usc)
    # If caller provided an explicit prior, honor it (tests / Unity).
    if inducing_play.get("inductionPrior") is not None or inducing_play.get("induction_prior") is not None:
        induction_prior = clamp01(
            float(inducing_play.get("inductionPrior", inducing_play.get("induction_prior")))
        )

    depth, nest_factor = nest_depth_factor(layer_stack, cfg.base_nest)
    echo = play_echo_match(inducing_play, nested_play_event)
    rem = clamp01(rem_entropy_norm)

    # Computed improbability before escapism foundation clamp.
    # Softplus/logit retained for audit openness; normalized score is a bounded blend so
    # high reproductionCoeff + nest can clear the play-thought threshold.
    mapped = _softplus(cfg.softplus_k * _logit(reproduction_coeff))
    improbability_open = mapped * nest_factor * (0.5 + 0.5 * echo) * (0.7 + 0.3 * rem)
    improbability01 = clamp01(
        0.12
        + 0.88
        * nest_factor
        * (0.25 + 0.75 * reproduction_coeff)
        * (0.55 + 0.45 * echo)
        * (0.75 + 0.25 * rem)
    )
    success01 = clamp01(fidelity01 * echo)

    foundation_clamped = False
    if induction_prior < cfg.induction_floor or improbability01 < cfg.threshold:
        unwrap_mode = UNWRAP_ESCAPISM_PREVIEW
        refrain_label = "escapism preview (non-authoritative)"
        improbability01 = 1.0
        foundation_clamped = True
    else:
        unwrap_mode = UNWRAP_PLAY_THOUGHT
        refrain_label = "play thought unpack (non-authoritative)"

    if reproduction_coeff <= 0.25:
        memory_mode = "full_reproduction"
    elif reproduction_coeff >= 0.75:
        memory_mode = "preview_only"
    else:
        memory_mode = "partial"

    return {
        "inductionPrior": round(induction_prior, 4),
        "reproductionCoeff": round(reproduction_coeff, 4),
        "fidelity01": round(fidelity01, 4),
        "success01": round(success01, 4),
        "improbability01": round(improbability01, 4),
        "improbabilityOpen": round(improbability_open, 4),
        "unwrapMode": unwrap_mode,
        "refrainLabel": refrain_label,
        "memoryMode": memory_mode,
        "nestDepth": depth,
        "playEchoMatch": round(echo, 4),
        "playFingerprint": play_fingerprint(inducing_play),
        "audit": {
            "foundationClamped": foundation_clamped,
            "threshold": cfg.threshold,
            "inductionFloor": cfg.induction_floor,
            "bakedFrom": "draft_episode_script.dream_reproduction_coeff" if draft_id else None,
            "weights": {"tableRead": W_TABLE_READ, "lemma": W_LEMMA, "usc": W_USC},
            "sources": {
                "tableReadExposure": round(table_read, 4),
                "lemmaPhraseNovelty": round(lemma_nov, 4),
                "uscFictionalHours": round(usc, 4),
            },
        },
    }
