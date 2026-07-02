"""TTS synthesize and audio inpaint stubs for dialogue speech."""

from __future__ import annotations

import hashlib
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def synthesize_speech(body: dict[str, Any], get_conn) -> dict[str, Any]:
    """v1 stub: derive stable audioRef from text; real TTS adapter plugs in here."""
    text = (body.get("text") or "").strip()
    node_id = body.get("nodeId") or body.get("node_id") or ""
    voice = body.get("voiceProfile") or body.get("voice_profile") or "default"
    speaker_key = body.get("speakerKey") or body.get("speaker")
    if not text:
        return {"ok": False, "error": "text required"}

    digest = hashlib.sha256(f"{voice}:{text}".encode()).hexdigest()[:16]
    audio_ref = f"usc://dialogue/synth/{digest}.wav"

    episode_script_id = body.get("episodeScriptId")
    if episode_script_id and get_conn:
        conn = get_conn()
        try:
            _ensure_speech_table(conn)
            row_id = str(uuid.uuid4())
            conn.execute(
                """
                INSERT OR REPLACE INTO script_speech_audio
                (id, episode_script_id, language_id, farey_left_num, farey_left_den,
                 farey_right_num, farey_right_den, audio_ref)
                VALUES (?, ?, ?, 0, 1, 1, 1, ?)
                """,
                (row_id, episode_script_id, body.get("languageId") or "en", audio_ref),
            )
            conn.commit()
        except sqlite3.OperationalError:
            pass
        finally:
            conn.close()

    return {
        "ok": True,
        "audioRef": audio_ref,
        "nodeId": node_id,
        "speakerKey": speaker_key,
        "text": text,
        "voiceProfile": voice,
        "styleNotes": body.get("styleNotes"),
        "stub": True,
        "generatedAt": _now(),
    }


def inpaint_speech(body: dict[str, Any]) -> dict[str, Any]:
    """v1 stub: returns new audioRef; ffmpeg/model inpainting later."""
    source = body.get("audioRef") or body.get("audio_ref")
    if not source:
        return {"ok": False, "error": "audioRef required"}
    patch_text = body.get("text") or body.get("patchText") or ""
    digest = hashlib.sha256(f"inpaint:{source}:{patch_text}".encode()).hexdigest()[:16]
    return {
        "ok": True,
        "sourceAudioRef": source,
        "audioRef": f"usc://dialogue/inpaint/{digest}.wav",
        "patchText": patch_text,
        "stub": True,
        "generatedAt": _now(),
    }


def _ensure_speech_table(conn: sqlite3.Connection) -> None:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name='script_speech_audio' LIMIT 1"
    )
    if cur.fetchone():
        return
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS script_speech_audio (
            id TEXT PRIMARY KEY,
            episode_script_id TEXT NOT NULL,
            language_id TEXT NOT NULL DEFAULT 'en',
            farey_left_num INTEGER NOT NULL DEFAULT 0,
            farey_left_den INTEGER NOT NULL DEFAULT 1,
            farey_right_num INTEGER NOT NULL DEFAULT 1,
            farey_right_den INTEGER NOT NULL DEFAULT 1,
            audio_ref TEXT
        )
        """
    )
