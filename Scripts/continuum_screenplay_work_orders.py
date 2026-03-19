"""
Extract work orders from screenplay: one work order per script_speech_audio (dialogue)
and per script_sound_effects (SFX), ordered by Farey clause, with depends_on chaining.
Requires script_speech_audio, script_sound_effects (continuum_screenplay_schema.sql)
and work_orders extended with work_order_source, speech_audio_id, sound_effect_id,
episode_script_id, farey_* (continuum_work_orders_screenplay_schema.sql).
"""

from __future__ import annotations

import json
import uuid
from typing import Any



def _farey_sort_key(ln: int, ld: int, rn: int, rd: int) -> float:
    return (ln + rn) / (ld + rd) if (ld + rd) else 0.0


def _quote_text_for_clause(conn, episode_script_id: str, ln: int, ld: int, rn: int, rd: int) -> str | None:
    """If a quote node's Farey interval contains (ln/ld, rn/rd], return its children's text."""
    try:
        cur = conn.execute("PRAGMA table_info(thesaurus_ast_nodes)")
        has_node_kind = any(r[1] == "node_kind" for r in cur.fetchall())
        if not has_node_kind:
            return None
        cur = conn.execute(
            """SELECT id, farey_left_num, farey_left_den, farey_right_num, farey_right_den
               FROM thesaurus_ast_nodes
               WHERE episode_script_id = ? AND node_kind = 'quote' AND parent_id IS NULL""",
            (episode_script_id,),
        )
    except Exception:
        return None
    clause_left = ln / ld if ld else 0
    clause_right = rn / rd if rd else 0
    for row in cur.fetchall():
        qln, qld, qrn, qrd = row["farey_left_num"], row["farey_left_den"], row["farey_right_num"], row["farey_right_den"]
        q_left = qln / qld if qld else 0
        q_right = qrn / qrd if qrd else 0
        if q_left <= clause_left and clause_right <= q_right:
            cur2 = conn.execute(
                """SELECT token_or_phrase FROM thesaurus_ast_nodes
                   WHERE parent_id = ? ORDER BY sort_key ASC""",
                (row["id"],),
            )
            parts = [r["token_or_phrase"] or "" for r in cur2.fetchall()]
            return " ".join(parts).strip() or None
    return None


def _has_work_order_screenplay_columns(conn) -> bool:
    cur = conn.execute("PRAGMA table_info(work_orders)")
    names = {row[1] for row in cur.fetchall()}
    return "work_order_source" in names and "speech_audio_id" in names


def extract_work_orders_from_screenplay(
    conn,
    episode_id: str,
    episode_script_id: str | None = None,
) -> list[dict[str, Any]]:
    """
    Load script_speech_audio and script_sound_effects for the episode's script,
    order by Farey, create one work order per dialogue and per SFX, chain depends_on.
    Returns list of created work order dicts. If screenplay tables or work_orders
    extension are missing, returns empty list.
    """
    if not _has_work_order_screenplay_columns(conn):
        return []
    cur = conn.execute(
        "SELECT id FROM episode_script WHERE episode_id = ? LIMIT 1",
        (episode_id,),
    )
    row = cur.fetchone()
    script_id = episode_script_id or (row["id"] if row else None)
    if not script_id:
        return []
    segments: list[dict[str, Any]] = []
    try:
        cur = conn.execute(
            """SELECT id, episode_script_id, language_id, farey_left_num, farey_left_den,
                      farey_right_num, farey_right_den, audio_ref
               FROM script_speech_audio WHERE episode_script_id = ?""",
            (script_id,),
        )
        for r in cur.fetchall():
            segments.append({
                "type": "dialogue",
                "id": r["id"],
                "episode_script_id": r["episode_script_id"],
                "farey_left_num": r["farey_left_num"],
                "farey_left_den": r["farey_left_den"],
                "farey_right_num": r["farey_right_num"],
                "farey_right_den": r["farey_right_den"],
                "audio_ref": r["audio_ref"],
                "sort_key": _farey_sort_key(
                    r["farey_left_num"], r["farey_left_den"],
                    r["farey_right_num"], r["farey_right_den"],
                ),
            })
    except Exception:
        pass
    try:
        cur = conn.execute(
            """SELECT id, episode_script_id, farey_left_num, farey_left_den,
                      farey_right_num, farey_right_den, audio_ref, effect_kind
               FROM script_sound_effects WHERE episode_script_id = ?""",
            (script_id,),
        )
        for r in cur.fetchall():
            segments.append({
                "type": "sfx",
                "id": r["id"],
                "episode_script_id": r["episode_script_id"],
                "farey_left_num": r["farey_left_num"],
                "farey_left_den": r["farey_left_den"],
                "farey_right_num": r["farey_right_num"],
                "farey_right_den": r["farey_right_den"],
                "audio_ref": r["audio_ref"],
                "effect_kind": r.get("effect_kind"),
                "sort_key": _farey_sort_key(
                    r["farey_left_num"], r["farey_left_den"],
                    r["farey_right_num"], r["farey_right_den"],
                ),
            })
    except Exception:
        pass
    if not segments:
        return []
    segments.sort(key=lambda x: x["sort_key"])
    conn.execute(
        """DELETE FROM work_orders WHERE episode_id = ? AND work_order_source IN ('dialogue', 'sfx')""",
        (episode_id,),
    )
    created: list[dict[str, Any]] = []
    prev_wo_id: str | None = None
    for seg in segments:
        wo_id = "wo_" + uuid.uuid4().hex[:12]
        if seg["type"] == "dialogue":
            quote_text = _quote_text_for_clause(
                conn, script_id,
                seg["farey_left_num"], seg["farey_left_den"],
                seg["farey_right_num"], seg["farey_right_den"],
            )
            prompt = f"Record dialogue: {quote_text}" if quote_text else f"Record dialogue: clause ({seg['farey_left_num']}/{seg['farey_left_den']}, {seg['farey_right_num']}/{seg['farey_right_den']})"
            conn.execute(
                """INSERT INTO work_orders
                   (id, episode_id, work_order_source, speech_audio_id, episode_script_id,
                    farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    narrative_type, depends_on, prompt_description, status)
                   VALUES (?, ?, 'dialogue', ?, ?, ?, ?, ?, ?, 'linear', ?, ?, 'pending')""",
                (wo_id, episode_id, seg["id"], script_id,
                 seg["farey_left_num"], seg["farey_left_den"], seg["farey_right_num"], seg["farey_right_den"],
                 json.dumps([prev_wo_id] if prev_wo_id else []), prompt),
            )
        else:
            ef = seg.get("effect_kind") or seg.get("audio_ref") or "SFX"
            prompt = f"Add SFX: {ef}"
            conn.execute(
                """INSERT INTO work_orders
                   (id, episode_id, work_order_source, sound_effect_id, episode_script_id,
                    farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    narrative_type, depends_on, prompt_description, status)
                   VALUES (?, ?, 'sfx', ?, ?, ?, ?, ?, ?, 'linear', ?, ?, 'pending')""",
                (wo_id, episode_id, seg["id"], script_id,
                 seg["farey_left_num"], seg["farey_left_den"], seg["farey_right_num"], seg["farey_right_den"],
                 json.dumps([prev_wo_id] if prev_wo_id else []), prompt),
            )
        created.append({"id": wo_id, "type": seg["type"], "prompt_description": prompt})
        prev_wo_id = wo_id
    conn.commit()
    return created
