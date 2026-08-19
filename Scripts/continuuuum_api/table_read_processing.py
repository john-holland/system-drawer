"""Table-read processing: quote map, USC Whisper match, pauses, Save/Sync, turn retreat."""

from __future__ import annotations

import json
import os
import sqlite3
import sys
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from urllib.parse import urljoin

from flask import jsonify, request

try:
    from continuuuum_api.usc_whisper import (
        UscUnavailable,
        extract_transcript_text,
        has_transcribe_impl,
        transcript_matches_quote,
        transcribe_via_usc,
    )
except ImportError:
    from usc_whisper import (
        UscUnavailable,
        extract_transcript_text,
        has_transcribe_impl,
        transcript_matches_quote,
        transcribe_via_usc,
    )

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]

_usc_upload_impl: Callable[..., Any] | None = None
_usc_download_impl: Callable[..., Any] | None = None


def set_usc_upload_impl(fn: Callable[..., Any] | None) -> None:
    global _usc_upload_impl
    _usc_upload_impl = fn
    for name in ("table_read_processing", "continuuuum_api.table_read_processing"):
        mod = sys.modules.get(name)
        if mod is not None:
            mod._usc_upload_impl = fn


def set_usc_download_impl(fn: Callable[..., Any] | None) -> None:
    global _usc_download_impl
    _usc_download_impl = fn
    for name in ("table_read_processing", "continuuuum_api.table_read_processing"):
        mod = sys.modules.get(name)
        if mod is not None:
            mod._usc_download_impl = fn


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def _ensure_column(conn: sqlite3.Connection, table: str, column: str, ddl: str) -> None:
    cols = {r[1] for r in conn.execute(f"PRAGMA table_info({table})").fetchall()}
    if column not in cols:
        conn.execute(f"ALTER TABLE {table} ADD COLUMN {ddl}")


def ensure_processing_tables(conn: sqlite3.Connection) -> None:
    schema_path = Path(__file__).resolve().parents[1] / "continuuuum_table_read_schema.sql"
    if schema_path.exists():
        conn.executescript(schema_path.read_text(encoding="utf-8"))
        conn.commit()
    if _table_exists(conn, "table_read_processing_segments"):
        _ensure_column(conn, "table_read_processing_segments", "process_video_animation", "process_video_animation INTEGER NOT NULL DEFAULT 0")
        _ensure_column(conn, "table_read_processing_segments", "detector_profile_id", "detector_profile_id TEXT")
        _ensure_column(conn, "table_read_processing_segments", "anim_props_json", "anim_props_json TEXT")
        _ensure_column(conn, "table_read_processing_segments", "video_library_doc_id", "video_library_doc_id TEXT")
        _ensure_column(conn, "table_read_processing_segments", "webcam_recording_id", "webcam_recording_id TEXT")
        _ensure_column(conn, "table_read_processing_segments", "pose_track_path", "pose_track_path TEXT")
        _ensure_column(conn, "table_read_processing_segments", "anim_status", "anim_status TEXT NOT NULL DEFAULT 'idle'")
    if _table_exists(conn, "table_read_processing_choices"):
        _ensure_column(conn, "table_read_processing_choices", "processed_assets_json", "processed_assets_json TEXT NOT NULL DEFAULT '[]'")
    conn.commit()
    try:
        from continuuuum_api.webcam_anim_routes import ensure_webcam_anim_schema
    except ImportError:
        from webcam_anim_routes import ensure_webcam_anim_schema
    ensure_webcam_anim_schema(conn)


def _row_get(r: sqlite3.Row, key: str, default: Any = None) -> Any:
    try:
        if key in r.keys():
            val = r[key]
            return default if val is None else val
    except Exception:
        pass
    return default


def _parse_json(raw: Any, default: Any) -> Any:
    if raw is None or raw == "":
        return default
    if isinstance(raw, (dict, list)):
        return raw
    try:
        return json.loads(raw)
    except (TypeError, json.JSONDecodeError):
        return default


def _webcam_imports():
    try:
        from continuuuum_api.webcam_anim_routes import (
            apply_detector_profile,
            drain_queue,
            insert_and_enqueue_recording,
            list_detector_profiles,
            resolve_detector_profile,
            _row_to_dict,
        )
    except ImportError:
        from webcam_anim_routes import (
            apply_detector_profile,
            drain_queue,
            insert_and_enqueue_recording,
            list_detector_profiles,
            resolve_detector_profile,
            _row_to_dict,
        )
    return (
        apply_detector_profile,
        drain_queue,
        insert_and_enqueue_recording,
        list_detector_profiles,
        resolve_detector_profile,
        _row_to_dict,
    )


def usc_upload_file(file_path: str, document_type: str, library_base: str) -> str:
    impl = _usc_upload_impl
    if impl is None:
        for name in ("table_read_processing", "continuuuum_api.table_read_processing"):
            mod = sys.modules.get(name)
            if mod is not None and getattr(mod, "_usc_upload_impl", None) is not None:
                impl = mod._usc_upload_impl
                break
    if impl is not None:
        return str(impl(file_path, document_type, library_base))
    import requests

    url = urljoin((library_base or "").rstrip("/") + "/", "api/library/upload")
    with open(file_path, "rb") as fh:
        resp = requests.post(
            url,
            files={"file": (Path(file_path).name, fh)},
            data={"document_type": document_type, "type_metadata": "{}"},
            timeout=120,
        )
    if resp.status_code >= 400:
        raise RuntimeError(resp.text or f"USC upload failed ({resp.status_code})")
    payload = resp.json() if resp.content else {}
    doc_id = payload.get("id")
    if doc_id is None:
        raise RuntimeError("USC upload missing id")
    return str(doc_id)


def usc_download_library_doc(doc_id: str, library_base: str, dest_path: str | None = None) -> str:
    impl = _usc_download_impl
    if impl is None:
        for name in ("table_read_processing", "continuuuum_api.table_read_processing"):
            mod = sys.modules.get(name)
            if mod is not None and getattr(mod, "_usc_download_impl", None) is not None:
                impl = mod._usc_download_impl
                break
    if impl is not None:
        return str(impl(doc_id, library_base, dest_path))
    import requests

    url = urljoin((library_base or "").rstrip("/") + "/", f"api/library/documents/{doc_id}/download")
    resp = requests.get(url, timeout=120)
    if resp.status_code >= 400:
        raise RuntimeError(resp.text or f"USC download failed ({resp.status_code})")
    dest = Path(dest_path) if dest_path else Path(tempfile.gettempdir()) / "table_read_video" / f"{doc_id}.bin"
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(resp.content)
    return str(dest)


def freeze_script_if_needed(conn: sqlite3.Connection, session_id: str) -> dict[str, Any] | None:
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr

    session = tr._get_session(conn, session_id)
    if not session:
        return None
    cur = conn.execute(
        "SELECT * FROM table_read_script_storage WHERE session_id = ? ORDER BY created_at DESC LIMIT 1",
        (session_id,),
    )
    row = cur.fetchone()
    if row:
        return dict(row)
    text = _load_draft_script(conn, session)
    sid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO table_read_script_storage (id, session_id, draft_episode_id, script_text, created_at)
           VALUES (?, ?, ?, ?, ?)""",
        (sid, session_id, session["draft_episode_id"], text or "", now),
    )
    conn.commit()
    return {
        "id": sid,
        "session_id": session_id,
        "draft_episode_id": session["draft_episode_id"],
        "script_text": text or "",
        "created_at": now,
    }


def _load_draft_script(conn: sqlite3.Connection, session: sqlite3.Row | dict) -> str:
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr
    orig = getattr(tr, "_load_script_text_orig", None)
    if orig:
        return orig(conn, session) or ""
    draft_id = session["draft_episode_id"]
    cur = conn.execute(
        "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY updated_at DESC LIMIT 1",
        (draft_id,),
    )
    row = cur.fetchone()
    return (row["script_text"] or "") if row else ""


def latest_script_storage(conn: sqlite3.Connection, session_id: str) -> dict[str, Any] | None:
    if not _table_exists(conn, "table_read_script_storage"):
        return None
    cur = conn.execute(
        "SELECT * FROM table_read_script_storage WHERE session_id = ? ORDER BY created_at DESC LIMIT 1",
        (session_id,),
    )
    row = cur.fetchone()
    return dict(row) if row else None


def list_quotes(conn: sqlite3.Connection, session_id: str) -> list[dict[str, Any]]:
    if not _table_exists(conn, "table_read_character_quotes"):
        return []
    _scrub_unlocated_quotes(conn, session_id)
    cur = conn.execute(
        "SELECT * FROM table_read_character_quotes WHERE session_id = ? ORDER BY char_start, created_at",
        (session_id,),
    )
    return [q for q in (_quote_row(r) for r in cur.fetchall()) if is_located_quote(q["charStart"], q["charEnd"])]


def list_characters(conn: sqlite3.Connection, session_id: str) -> list[dict[str, Any]]:
    if not _table_exists(conn, "table_read_characters"):
        return []
    cur = conn.execute(
        "SELECT * FROM table_read_characters WHERE session_id = ? ORDER BY created_at, character_name",
        (session_id,),
    )
    return [
        {
            "characterName": r["character_name"],
            "dialogActorId": r["dialog_actor_id"] or "",
        }
        for r in cur.fetchall()
    ]


def is_located_quote(start: Any, end: Any) -> bool:
    """True when a span is a real quote location: end > start, not the empty-cursor dummy [0, 1]."""
    try:
        cs = int(start)
        ce = int(end)
    except (TypeError, ValueError):
        return False
    if ce <= cs or ce <= 0:
        return False
    if cs == 0 and ce == 1:
        return False
    return True


def _scrub_unlocated_quotes(conn: sqlite3.Connection, session_id: str) -> None:
    try:
        conn.execute(
            """DELETE FROM table_read_character_quotes
               WHERE session_id = ? AND (char_end <= char_start OR (char_start = 0 AND char_end = 1))""",
            (session_id,),
        )
        conn.commit()
    except sqlite3.OperationalError:
        pass


def quote_map(
    quotes: list[dict[str, Any]],
    characters: list[dict[str, Any]] | None = None,
) -> list[dict[str, Any]]:
    by_char: dict[str, dict[str, Any]] = {}
    order: list[str] = []
    for c in characters or []:
        name = (c.get("characterName") or "").strip()
        if not name:
            continue
        if name not in by_char:
            by_char[name] = {
                "characterName": name,
                "dialogActorId": c.get("dialogActorId") or "",
                "quotes": [],
            }
            order.append(name)
        elif c.get("dialogActorId"):
            by_char[name]["dialogActorId"] = c["dialogActorId"]
    for q in quotes:
        if not is_located_quote(q.get("charStart"), q.get("charEnd")):
            continue
        name = q["characterName"] or ""
        if name not in by_char:
            by_char[name] = {
                "characterName": name,
                "dialogActorId": q.get("dialogActorId") or "",
                "quotes": [],
            }
            order.append(name)
        entry = by_char[name]
        if q.get("dialogActorId"):
            entry["dialogActorId"] = q["dialogActorId"]
        entry["quotes"].append({"id": q["id"], "start": q["charStart"], "end": q["charEnd"]})
    return [by_char[n] for n in order]


def session_quote_payload(conn: sqlite3.Connection, session_id: str) -> dict[str, Any]:
    seed_characters_and_located_quotes(conn, session_id)
    chars = list_characters(conn, session_id)
    quotes = list_quotes(conn, session_id)
    return {"quotes": quotes, "characters": chars, "quoteMap": quote_map(quotes, chars)}


def upsert_character(
    conn: sqlite3.Connection,
    session_id: str,
    character_name: str,
    dialog_actor_id: str = "",
) -> dict[str, Any]:
    name = (character_name or "").strip()
    actor = (dialog_actor_id or "").strip()
    if not name:
        raise ValueError("characterName required")
    if not _table_exists(conn, "table_read_characters"):
        ensure_processing_tables(conn)
    now = _now()
    cur = conn.execute(
        "SELECT dialog_actor_id FROM table_read_characters WHERE session_id = ? AND character_name = ?",
        (session_id, name),
    )
    row = cur.fetchone()
    if row:
        if actor and actor != (row["dialog_actor_id"] or ""):
            conn.execute(
                "UPDATE table_read_characters SET dialog_actor_id = ? WHERE session_id = ? AND character_name = ?",
                (actor, session_id, name),
            )
        else:
            actor = row["dialog_actor_id"] or actor
    else:
        conn.execute(
            """INSERT INTO table_read_characters (session_id, character_name, dialog_actor_id, created_at)
               VALUES (?, ?, ?, ?)""",
            (session_id, name, actor, now),
        )
    conn.commit()
    return {"characterName": name, "dialogActorId": actor, "quotes": []}


def _character_name_at_offset(script_text: str, offset: int) -> str:
    try:
        from continuuuum_api.table_read_blocks import parse_reading_blocks
    except ImportError:
        from table_read_blocks import parse_reading_blocks
    for block in parse_reading_blocks(script_text or ""):
        if block.get("kind") != "dialogue":
            continue
        start = int(block.get("charStart") or 0)
        end = int(block.get("charEnd") or 0)
        if start <= offset < end:
            heading = (block.get("text") or "").split("\n", 1)[0].strip()
            return heading.split("(")[0].strip()
    return ""


def seed_characters_and_located_quotes(conn: sqlite3.Connection, session_id: str) -> None:
    """Roster from character headings; quote spans only from lemma/prefab/voice_actor_line bindings."""
    freeze_script_if_needed(conn, session_id)
    stored = latest_script_storage(conn, session_id)
    script = (stored or {}).get("script_text") or ""
    try:
        from continuuuum_api.table_read_blocks import parse_reading_blocks
    except ImportError:
        from table_read_blocks import parse_reading_blocks
    for block in parse_reading_blocks(script):
        if block.get("kind") != "dialogue":
            continue
        heading = (block.get("text") or "").split("\n", 1)[0].strip()
        name = heading.split("(")[0].strip()
        if name:
            upsert_character(conn, session_id, name, "")
    if not _table_exists(conn, "localization_clause_bindings"):
        return
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr
    session = tr._get_session(conn, session_id)
    if not session:
        return
    try:
        cur = conn.execute(
            """SELECT b.char_start, b.char_end, b.binding_kind, b.property_value, b.selection_text
               FROM localization_clause_bindings b
               JOIN draft_episode_script s ON s.id = b.draft_script_id
               WHERE s.draft_episode_id = ?""",
            (session["draft_episode_id"],),
        )
        rows = cur.fetchall()
    except sqlite3.OperationalError:
        return
    quote_kinds = {"voice_actor_line", "lemma", "prefab"}
    existing = {(q["charStart"], q["charEnd"], q["characterName"]) for q in list_quotes(conn, session_id)}
    for r in rows:
        kind = (r["binding_kind"] or "").lower()
        if kind not in quote_kinds:
            continue
        start, end = int(r["char_start"] or 0), int(r["char_end"] or 0)
        if not is_located_quote(start, end):
            continue
        name = _character_name_at_offset(script, start)
        if not name:
            continue
        heading = name
        sel = (script or "")[start:end].strip()
        if kind != "voice_actor_line" and (sel == heading or ("\n" not in sel and sel.startswith(heading))):
            upsert_character(conn, session_id, name, r["property_value"] or "")
            continue
        actor = (r["property_value"] or "") if kind == "voice_actor_line" else ""
        upsert_character(conn, session_id, name, actor)
        key = (start, end, name)
        if key in existing:
            continue
        add_quote(
            conn,
            session_id,
            character_name=name,
            dialog_actor_id=actor,
            char_start=start,
            char_end=end,
        )
        existing.add(key)


def _quote_row(r: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": r["id"],
        "sessionId": r["session_id"],
        "characterName": r["character_name"],
        "dialogActorId": r["dialog_actor_id"],
        "charStart": r["char_start"],
        "charEnd": r["char_end"],
        "createdAt": r["created_at"],
    }


def _segment_row(r: sqlite3.Row, quote_text: str = "") -> dict[str, Any]:
    whisper = None
    raw = r["whisper_json"]
    if raw:
        try:
            whisper = json.loads(raw)
        except json.JSONDecodeError:
            whisper = {"text": raw}
    props = _parse_json(_row_get(r, "anim_props_json"), {})
    if not isinstance(props, dict):
        props = {}
    return {
        "id": r["id"],
        "sessionId": r["session_id"],
        "quoteId": r["quote_id"],
        "recordingId": r["recording_id"],
        "uscJobId": r["usc_job_id"],
        "whisperJson": whisper,
        "whisperText": extract_transcript_text(whisper) if whisper else "",
        "quoteText": quote_text,
        "include": bool(r["include"]),
        "pauseBefore": bool(r["pause_before"]),
        "pauseBeforeSec": r["pause_before_sec"],
        "pauseAfter": bool(r["pause_after"]),
        "pauseAfterSec": r["pause_after_sec"],
        "insertPause": bool(r["insert_pause"]),
        "insertPausePos": r["insert_pause_pos"],
        "insertPauseSec": r["insert_pause_sec"],
        "uploadLibraryDocId": r["upload_library_doc_id"],
        "matchOk": None if r["match_ok"] is None else bool(r["match_ok"]),
        "status": r["status"],
        "audioUrl": r["audio_url"],
        "processVideoAnimation": bool(_row_get(r, "process_video_animation", 0)),
        "detectorProfileId": _row_get(r, "detector_profile_id") or "",
        "animProps": props,
        "videoLibraryDocId": _row_get(r, "video_library_doc_id") or "",
        "webcamRecordingId": _row_get(r, "webcam_recording_id") or "",
        "poseTrackPath": _row_get(r, "pose_track_path") or "",
        "animStatus": _row_get(r, "anim_status") or "idle",
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def _quote_text(conn: sqlite3.Connection, session_id: str, quote: dict[str, Any]) -> str:
    stored = latest_script_storage(conn, session_id)
    text = (stored or {}).get("script_text") or ""
    start = int(quote.get("charStart") or 0)
    end = int(quote.get("charEnd") or 0)
    if 0 <= start < end <= len(text):
        return text[start:end]
    return ""


def _ensure_segment_for_quote(conn: sqlite3.Connection, session_id: str, quote_id: str) -> str:
    cur = conn.execute(
        "SELECT id FROM table_read_processing_segments WHERE session_id = ? AND quote_id = ?",
        (session_id, quote_id),
    )
    row = cur.fetchone()
    if row:
        return row["id"]
    sid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO table_read_processing_segments
           (id, session_id, quote_id, include, pause_before, pause_before_sec, pause_after, pause_after_sec,
            insert_pause, insert_pause_pos, insert_pause_sec, status, created_at, updated_at)
           VALUES (?, ?, ?, 1, 0, 0, 0, 0, 0, 0, 0, 'pending', ?, ?)""",
        (sid, session_id, quote_id, now, now),
    )
    return sid


def _insert_voice_actor_binding(conn: sqlite3.Connection, session: sqlite3.Row, quote: dict[str, Any], selection: str) -> None:
    if not _table_exists(conn, "localization_clause_bindings"):
        return
    draft_script_id = None
    try:
        cur = conn.execute(
            "SELECT id FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY updated_at DESC LIMIT 1",
            (session["draft_episode_id"],),
        )
        row = cur.fetchone()
        if row:
            draft_script_id = row["id"]
    except sqlite3.OperationalError:
        draft_script_id = None
    now = _now()
    try:
        conn.execute(
            """INSERT INTO localization_clause_bindings
               (id, draft_script_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                char_start, char_end, selection_text, property_key, property_value, binding_kind, created_at, updated_at)
               VALUES (?, ?, 0, 1, 1, 1, ?, ?, ?, 'dialogActorId', ?, 'voice_actor_line', ?, ?)""",
            (
                str(uuid.uuid4()),
                draft_script_id,
                quote["charStart"],
                quote["charEnd"],
                selection,
                quote.get("dialogActorId") or quote.get("characterName") or "",
                now,
                now,
            ),
        )
    except sqlite3.OperationalError:
        pass


def add_quote(
    conn: sqlite3.Connection,
    session_id: str,
    *,
    character_name: str,
    dialog_actor_id: str,
    char_start: int,
    char_end: int,
) -> dict[str, Any]:
    if not is_located_quote(char_start, char_end):
        raise ValueError("quote span must have end > start (not the empty [0, 1] cursor)")
    upsert_character(conn, session_id, character_name, dialog_actor_id)
    qid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO table_read_character_quotes
           (id, session_id, character_name, dialog_actor_id, char_start, char_end, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?)""",
        (qid, session_id, character_name, dialog_actor_id or "", int(char_start), int(char_end), now),
    )
    _ensure_segment_for_quote(conn, session_id, qid)
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr
    session = tr._get_session(conn, session_id)
    quote = {
        "id": qid,
        "characterName": character_name,
        "dialogActorId": dialog_actor_id,
        "charStart": int(char_start),
        "charEnd": int(char_end),
    }
    if session:
        _insert_voice_actor_binding(conn, session, quote, _quote_text(conn, session_id, quote))
    conn.commit()
    return quote | {"id": qid, "sessionId": session_id, "createdAt": now}


def _choices_row(conn: sqlite3.Connection, session_id: str) -> dict[str, Any]:
    cur = conn.execute(
        "SELECT * FROM table_read_processing_choices WHERE session_id = ?",
        (session_id,),
    )
    row = cur.fetchone()
    if not row:
        return {"sessionId": session_id, "savedAt": None, "dialogueSetId": None, "composition": {}, "processedAssets": [], "saveLabel": "Save"}
    try:
        composition = json.loads(row["composition_json"] or "{}")
    except json.JSONDecodeError:
        composition = {}
    saved_at = row["saved_at"]
    assets = _parse_json(_row_get(row, "processed_assets_json"), [])
    if not isinstance(assets, list):
        assets = []
    return {
        "sessionId": session_id,
        "savedAt": saved_at,
        "dialogueSetId": row["dialogue_set_id"],
        "composition": composition,
        "processedAssets": assets,
        "saveLabel": "Sync" if saved_at else "Save",
        "updatedAt": row["updated_at"],
    }


def composition_from_segments(segments: list[dict[str, Any]]) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    for seg in segments:
        if seg.get("pauseBefore") and float(seg.get("pauseBeforeSec") or 0) > 0:
            items.append({"kind": "silence", "seconds": float(seg["pauseBeforeSec"]), "segmentId": seg["id"]})
        if seg.get("include"):
            clip: dict[str, Any] = {"kind": "clip", "segmentId": seg["id"], "audioUrl": seg.get("audioUrl")}
            if seg.get("insertPause") and float(seg.get("insertPauseSec") or 0) > 0:
                clip["insertPausePos"] = float(seg.get("insertPausePos") or 0)
                clip["insertPauseSec"] = float(seg["insertPauseSec"])
            items.append(clip)
        if seg.get("pauseAfter") and float(seg.get("pauseAfterSec") or 0) > 0:
            items.append({"kind": "silence", "seconds": float(seg["pauseAfterSec"]), "segmentId": seg["id"]})
    return items


def _apply_whisper_to_segment(
    conn: sqlite3.Connection,
    session_id: str,
    segment_id: str,
    whisper_json: dict[str, Any],
    *,
    status: str = "ready",
    usc_job_id: str | None = None,
) -> None:
    cur = conn.execute(
        "SELECT * FROM table_read_processing_segments WHERE id = ? AND session_id = ?",
        (segment_id, session_id),
    )
    seg = cur.fetchone()
    if not seg:
        return
    qcur = conn.execute(
        "SELECT * FROM table_read_character_quotes WHERE id = ?",
        (seg["quote_id"],),
    )
    qrow = qcur.fetchone()
    quote_text = _quote_text(conn, session_id, _quote_row(qrow)) if qrow else ""
    match_ok = transcript_matches_quote(whisper_json, quote_text)
    now = _now()
    conn.execute(
        """UPDATE table_read_processing_segments
           SET whisper_json = ?, match_ok = ?, include = ?, status = ?, usc_job_id = COALESCE(?, usc_job_id), updated_at = ?
           WHERE id = ?""",
        (json.dumps(whisper_json), 1 if match_ok else 0, 1 if match_ok else 0, status, usc_job_id, now, segment_id),
    )


def _fail_segment(conn: sqlite3.Connection, segment_id: str, error: str) -> None:
    now = _now()
    conn.execute(
        """UPDATE table_read_processing_segments
           SET status = 'failed', whisper_json = ?, include = 0, match_ok = 0, updated_at = ?
           WHERE id = ?""",
        (json.dumps({"error": error}), now, segment_id),
    )


def _audio_file_for_segment(conn: sqlite3.Connection, seg: sqlite3.Row) -> str | None:
    url = seg["audio_url"] if "audio_url" in seg.keys() else None
    if url and os.path.isfile(url):
        return url
    rec_id = seg["recording_id"]
    if rec_id:
        cur = conn.execute("SELECT library_doc_ids_json FROM table_read_recordings WHERE id = ?", (rec_id,))
        row = cur.fetchone()
        if row:
            try:
                ids = json.loads(row["library_doc_ids_json"] or "[]")
            except json.JSONDecodeError:
                ids = []
            for item in ids:
                if isinstance(item, str) and os.path.isfile(item):
                    return item
    return None


def run_whisper_for_segment(conn: sqlite3.Connection, session_id: str, segment_id: str, library_base: str) -> dict[str, Any]:
    cur = conn.execute(
        "SELECT * FROM table_read_processing_segments WHERE id = ? AND session_id = ?",
        (segment_id, session_id),
    )
    seg = cur.fetchone()
    if not seg:
        return {"error": "not found"}
    path = _audio_file_for_segment(conn, seg)
    if not path and not has_transcribe_impl():
        return {"ok": False, "status": "pending", "error": "no audio"}
    try:
        whisper_json = transcribe_via_usc(path or "", library_base)
    except UscUnavailable as exc:
        _fail_segment(conn, segment_id, str(exc))
        conn.commit()
        return {"error": str(exc), "status": "failed"}
    job_id = whisper_json.get("usc_job_id") or whisper_json.get("id")
    _apply_whisper_to_segment(conn, session_id, segment_id, whisper_json, usc_job_id=str(job_id) if job_id else None)
    conn.commit()
    return {"ok": True}


def list_segments(conn: sqlite3.Connection, session_id: str) -> list[dict[str, Any]]:
    quotes = {q["id"]: q for q in list_quotes(conn, session_id)}
    cur = conn.execute(
        "SELECT * FROM table_read_processing_segments WHERE session_id = ? ORDER BY created_at",
        (session_id,),
    )
    out = []
    for r in cur.fetchall():
        q = quotes.get(r["quote_id"])
        text = _quote_text(conn, session_id, q) if q else ""
        row = _segment_row(r, text)
        if q:
            row["characterName"] = q["characterName"]
            row["dialogActorId"] = q["dialogActorId"]
            row["charStart"] = q["charStart"]
            row["charEnd"] = q["charEnd"]
        out.append(row)
    return out


def processing_payload(conn: sqlite3.Connection, session_id: str, library_base: str, *, run_whisper: bool = True) -> dict[str, Any]:
    freeze_script_if_needed(conn, session_id)
    seed_characters_and_located_quotes(conn, session_id)
    quotes = list_quotes(conn, session_id)
    for q in quotes:
        _ensure_segment_for_quote(conn, session_id, q["id"])
    conn.commit()
    if run_whisper:
        cur = conn.execute(
            "SELECT id, status, whisper_json FROM table_read_processing_segments WHERE session_id = ?",
            (session_id,),
        )
        for row in cur.fetchall():
            if row["status"] in ("pending",) and not row["whisper_json"]:
                run_whisper_for_segment(conn, session_id, row["id"], library_base)
    segments = list_segments(conn, session_id)
    choices = _choices_row(conn, session_id)
    composition = composition_from_segments(segments)
    todos = [
        {
            "quoteId": q["id"],
            "characterName": q["characterName"],
            "status": "recorded" if any(s["quoteId"] == q["id"] and s.get("audioUrl") for s in segments) else "pending",
        }
        for q in quotes
    ]
    stored = latest_script_storage(conn, session_id)
    assets = build_processed_assets(conn, session_id)
    _, _, _, list_detector_profiles, _, _ = _webcam_imports()
    try:
        profiles = list_detector_profiles(conn)
    except Exception:
        profiles = []
    return {
        "quotes": quotes,
        "characters": list_characters(conn, session_id),
        "quoteMap": quote_map(quotes, list_characters(conn, session_id)),
        "segments": segments,
        "composition": composition,
        "recordingTodos": todos,
        "savedAt": choices.get("savedAt"),
        "saveLabel": choices.get("saveLabel") or "Save",
        "dialogueSetId": choices.get("dialogueSetId"),
        "scriptStorageId": stored["id"] if stored else None,
        "scriptText": (stored or {}).get("script_text") or "",
        "processedAssets": assets,
        "detectorProfiles": profiles,
        "videoParts": _session_video_parts(conn, session_id),
    }


def patch_segment(conn: sqlite3.Connection, session_id: str, segment_id: str, body: dict[str, Any]) -> dict[str, Any] | None:
    cur = conn.execute(
        "SELECT id FROM table_read_processing_segments WHERE id = ? AND session_id = ?",
        (segment_id, session_id),
    )
    if not cur.fetchone():
        return None
    fields = {
        "include": "include",
        "pauseBefore": "pause_before",
        "pauseBeforeSec": "pause_before_sec",
        "pauseAfter": "pause_after",
        "pauseAfterSec": "pause_after_sec",
        "insertPause": "insert_pause",
        "insertPausePos": "insert_pause_pos",
        "insertPauseSec": "insert_pause_sec",
        "recordingId": "recording_id",
        "audioUrl": "audio_url",
        "uploadLibraryDocId": "upload_library_doc_id",
        "processVideoAnimation": "process_video_animation",
        "detectorProfileId": "detector_profile_id",
        "videoLibraryDocId": "video_library_doc_id",
        "webcamRecordingId": "webcam_recording_id",
        "poseTrackPath": "pose_track_path",
        "animStatus": "anim_status",
    }
    sets = ["updated_at = ?"]
    vals: list[Any] = [_now()]
    if "animProps" in body:
        props = body["animProps"]
        if isinstance(props, dict):
            sets.append("anim_props_json = ?")
            vals.append(json.dumps(props))
        elif isinstance(props, str):
            sets.append("anim_props_json = ?")
            vals.append(props)
    for js, col in fields.items():
        if js not in body:
            continue
        val = body[js]
        if js in ("include", "pauseBefore", "pauseAfter", "insertPause", "processVideoAnimation"):
            val = 1 if val else 0
        sets.append(f"{col} = ?")
        vals.append(val)
    vals.append(segment_id)
    conn.execute(f"UPDATE table_read_processing_segments SET {', '.join(sets)} WHERE id = ?", vals)
    conn.commit()
    segs = [s for s in list_segments(conn, session_id) if s["id"] == segment_id]
    return segs[0] if segs else None


def _require_host(conn: sqlite3.Connection, session_id: str, user_id: str) -> tuple[Any, str | None, int]:
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr
    session = tr._get_session(conn, session_id)
    if not session:
        return None, "not found", 404
    if session["host_user_id"] != user_id:
        return None, "host only", 403
    return session, None, 200


def retreat_turn(conn: sqlite3.Connection, session_id: str, user_id: str) -> tuple[dict | None, str | None]:
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr

    session = tr._get_session(conn, session_id)
    if not session or session["status"] != "active":
        return None, "not found"
    turn = tr._current_turn(conn, session_id)
    if not turn:
        return None, "no active turn"
    if user_id != session["host_user_id"] and turn["assigned_user_id"] != user_id:
        return None, "not your turn"
    prev_index = session["current_turn_index"] - 1
    if prev_index < 0:
        return None, "no previous turn"
    cur = conn.execute(
        "SELECT * FROM table_read_turns WHERE session_id = ? AND turn_index = ?",
        (session_id, prev_index),
    )
    prev = cur.fetchone()
    if not prev:
        return None, "no previous turn"
    conn.execute("UPDATE table_read_turns SET status = 'pending' WHERE id = ?", (turn["id"],))
    conn.execute("UPDATE table_read_turns SET status = 'active' WHERE id = ?", (prev["id"],))
    conn.execute(
        "UPDATE table_read_sessions SET current_turn_index = ? WHERE id = ?",
        (prev_index, session_id),
    )
    conn.commit()
    return tr._session_snapshot(conn, session_id, user_id), None


def _build_dialog_tree(session_id: str, segments: list[dict[str, Any]], quotes: list[dict[str, Any]]) -> dict[str, Any]:
    qmap = {q["id"]: q for q in quotes}
    nodes: list[dict[str, Any]] = []
    for item in composition_from_segments(segments):
        nid = str(uuid.uuid4())
        if item["kind"] == "silence":
            nodes.append(
                {
                    "id": nid,
                    "kind": "wait",
                    "seconds": item["seconds"],
                    "presentation": "wait",
                    "text": "",
                }
            )
            continue
        seg = next((s for s in segments if s["id"] == item["segmentId"]), None)
        if not seg or not seg.get("include"):
            continue
        q = qmap.get(seg["quoteId"]) or {}
        nodes.append(
            {
                "id": nid,
                "kind": "voice_actor_line",
                "text": seg.get("quoteText") or "",
                "speakerKey": q.get("dialogActorId") or q.get("characterName") or "",
                "dialogActorId": q.get("dialogActorId") or "",
                "charStart": q.get("charStart"),
                "charEnd": q.get("charEnd"),
                "audioRef": f"usc://{seg['uploadLibraryDocId']}" if seg.get("uploadLibraryDocId") else (seg.get("audioUrl") or ""),
                "presentation": "audio" if (seg.get("uploadLibraryDocId") or seg.get("audioUrl")) else "text",
            }
        )
        if item.get("insertPauseSec"):
            nodes.append(
                {
                    "id": str(uuid.uuid4()),
                    "kind": "wait",
                    "seconds": item["insertPauseSec"],
                    "presentation": "wait",
                    "text": "",
                    "insertPos": item.get("insertPausePos"),
                }
            )
    set_id = f"table-read-{session_id}"
    return {"setId": set_id, "nodes": nodes, "generateSpans": [], "issues": []}


def _session_video_parts(conn: sqlite3.Connection, session_id: str) -> list[dict[str, Any]]:
    if not _table_exists(conn, "table_read_recordings"):
        return []
    rows = conn.execute(
        "SELECT * FROM table_read_recordings WHERE session_id = ? ORDER BY created_at",
        (session_id,),
    ).fetchall()
    parts: list[dict[str, Any]] = []
    for r in rows:
        try:
            ids = json.loads(r["library_doc_ids_json"] or "[]")
        except json.JSONDecodeError:
            ids = []
        for i, did in enumerate(ids):
            if did is None or str(did).strip() == "":
                continue
            parts.append(
                {
                    "recordingId": r["id"],
                    "partIndex": i,
                    "libraryDocId": str(did),
                    "mediaKind": r["media_kind"],
                }
            )
    return parts


def _segment_label(seg: dict[str, Any], quotes: dict[str, dict[str, Any]]) -> str:
    q = quotes.get(seg.get("quoteId") or "") or {}
    name = q.get("characterName") or seg.get("characterName") or ""
    text = (seg.get("quoteText") or "").strip()
    return f"{name} {text}".strip() or (seg.get("id") or "")


def build_processed_assets(
    conn: sqlite3.Connection, session_id: str, dialogue_set_id: str | None = None
) -> list[dict[str, Any]]:
    stored = latest_script_storage(conn, session_id)
    choices = _choices_row(conn, session_id)
    segments = list_segments(conn, session_id)
    quotes = {q["id"]: q for q in list_quotes(conn, session_id)}
    assets: list[dict[str, Any]] = []
    if stored:
        assets.append(
            {
                "kind": "script",
                "id": stored["id"],
                "libraryDocId": None,
                "documentType": "document",
                "audioRef": None,
                "label": "Frozen script",
                "segmentId": None,
                "recordingId": None,
                "poseTrackPath": None,
                "modelSpec": None,
            }
        )
    dset = dialogue_set_id if dialogue_set_id is not None else choices.get("dialogueSetId")
    if dset:
        assets.append(
            {
                "kind": "dialogue",
                "id": dset,
                "libraryDocId": None,
                "documentType": "document",
                "audioRef": None,
                "label": "Dialogue set",
                "segmentId": None,
                "recordingId": None,
                "poseTrackPath": None,
                "modelSpec": None,
            }
        )
    for seg in segments:
        if not seg.get("include"):
            continue
        label = _segment_label(seg, quotes)
        lib = seg.get("uploadLibraryDocId")
        if lib or seg.get("audioUrl"):
            assets.append(
                {
                    "kind": "audio",
                    "id": str(lib or seg["id"]),
                    "libraryDocId": str(lib) if lib else None,
                    "documentType": "audio",
                    "audioRef": f"usc://{lib}" if lib else (seg.get("audioUrl") or ""),
                    "label": label,
                    "segmentId": seg["id"],
                    "recordingId": None,
                    "poseTrackPath": None,
                    "modelSpec": None,
                }
            )
        if seg.get("processVideoAnimation") and seg.get("videoLibraryDocId"):
            assets.append(
                {
                    "kind": "video",
                    "id": str(seg["videoLibraryDocId"]),
                    "libraryDocId": str(seg["videoLibraryDocId"]),
                    "documentType": "video",
                    "audioRef": None,
                    "label": label,
                    "segmentId": seg["id"],
                    "recordingId": None,
                    "poseTrackPath": None,
                    "modelSpec": None,
                }
            )
        if seg.get("webcamRecordingId") or seg.get("poseTrackPath"):
            props = seg.get("animProps") or {}
            model_spec = props.get("modelSpec") or props.get("model_spec") or ""
            if not model_spec:
                try:
                    _, _, _, _, resolve_detector_profile, _ = _webcam_imports()
                    resolved = resolve_detector_profile(conn, seg.get("detectorProfileId") or "")
                    model_spec = resolved.get("model_spec") or ""
                except Exception:
                    model_spec = ""
            assets.append(
                {
                    "kind": "animation",
                    "id": str(seg.get("webcamRecordingId") or seg["id"]),
                    "libraryDocId": str(seg.get("videoLibraryDocId") or "") or None,
                    "documentType": "video",
                    "audioRef": None,
                    "label": label,
                    "segmentId": seg["id"],
                    "recordingId": seg.get("webcamRecordingId") or None,
                    "poseTrackPath": seg.get("poseTrackPath") or None,
                    "modelSpec": model_spec or None,
                }
            )
    return assets


def _persist_processed_assets(conn: sqlite3.Connection, session_id: str, assets: list[dict[str, Any]]) -> None:
    now = _now()
    conn.execute(
        """INSERT INTO table_read_processing_choices (session_id, composition_json, processed_assets_json, updated_at)
           VALUES (?, '{}', ?, ?)
           ON CONFLICT(session_id) DO UPDATE SET
             processed_assets_json=excluded.processed_assets_json,
             updated_at=excluded.updated_at""",
        (session_id, json.dumps(assets), now),
    )


def _update_segment_anim(
    conn: sqlite3.Connection,
    segment_id: str,
    *,
    status: str,
    webcam_recording_id: str | None = None,
    pose_track_path: str | None = None,
) -> None:
    now = _now()
    conn.execute(
        """UPDATE table_read_processing_segments
           SET anim_status = ?,
               webcam_recording_id = COALESCE(?, webcam_recording_id),
               pose_track_path = COALESCE(?, pose_track_path),
               updated_at = ?
           WHERE id = ?""",
        (status, webcam_recording_id, pose_track_path, now, segment_id),
    )


def process_segment_animation(
    conn: sqlite3.Connection,
    session_id: str,
    segment_id: str,
    library_base: str,
    user_id: str | None = None,
) -> tuple[dict[str, Any] | None, str | None, int]:
    segs = [s for s in list_segments(conn, session_id) if s["id"] == segment_id]
    if not segs:
        return None, "not found", 404
    seg = segs[0]
    if not seg.get("processVideoAnimation"):
        return {"ok": True, "skipped": True, "segment": seg}, None, 200
    video_id = str(seg.get("videoLibraryDocId") or "").strip()
    if not video_id:
        return None, "videoLibraryDocId required", 400
    apply_detector_profile, drain_queue, insert_and_enqueue_recording, _, resolve_detector_profile, row_to_dict = _webcam_imports()
    try:
        resolved = resolve_detector_profile(conn, seg.get("detectorProfileId") or "")
    except KeyError as exc:
        return None, str(exc), 400
    engine = resolved.get("engine") or "mediapipe"
    props = seg.get("animProps") if isinstance(seg.get("animProps"), dict) else {}
    species = str(props.get("species") or resolved.get("speciesDefault") or "").strip()
    if engine == "mocapanything" and not species:
        return None, "species required for MoCapAnything", 400
    dest = Path(tempfile.gettempdir()) / "table_read_video" / session_id / f"{segment_id}_{video_id}.bin"
    dest.parent.mkdir(parents=True, exist_ok=True)
    try:
        local_path = usc_download_library_doc(video_id, library_base, str(dest))
    except Exception as exc:  # noqa: BLE001
        _update_segment_anim(conn, segment_id, status="failed")
        conn.commit()
        segs = [s for s in list_segments(conn, session_id) if s["id"] == segment_id]
        return {"ok": False, "animStatus": "failed", "error": str(exc), "segment": segs[0] if segs else seg}, None, 200
    if not local_path or not Path(local_path).is_file():
        _update_segment_anim(conn, segment_id, status="failed")
        conn.commit()
        segs = [s for s in list_segments(conn, session_id) if s["id"] == segment_id]
        return {"ok": False, "animStatus": "failed", "error": "USC video download missing", "segment": segs[0] if segs else seg}, None, 200
    meta = {
        "kind": "webcam_anim_recording",
        "webcamAnimKind": props.get("webcamAnimKind") or "ambulatory",
        "subsection": props.get("subsection") or "",
        "timelineStartMs": float(props.get("timelineStartMs") or 0),
        "timelineEndMs": float(props.get("timelineEndMs") or 0),
        "granularity": props.get("granularity") or "millisecond",
        "species": species,
        "targetHint": props.get("targetHint") or "ragdoll",
    }
    try:
        meta = apply_detector_profile(conn, meta, seg.get("detectorProfileId") or resolved.get("profile", {}).get("id") or "")
    except KeyError as exc:
        return None, str(exc), 400
    props = dict(props)
    props["modelSpec"] = meta.get("model_spec")
    props["species"] = species
    conn.execute(
        "UPDATE table_read_processing_segments SET anim_props_json = ?, detector_profile_id = COALESCE(NULLIF(detector_profile_id, ''), ?) WHERE id = ?",
        (json.dumps(props), (resolved.get("profile") or {}).get("id") or "", segment_id),
    )
    _update_segment_anim(conn, segment_id, status="queued")
    conn.commit()
    doc = insert_and_enqueue_recording(
        conn,
        meta,
        library_doc_id=video_id,
        file_path=local_path,
        created_by=user_id,
        drain_complete=True,
    )
    drain_queue(conn, complete=True)
    rec_id = doc.get("id")
    rec_row = conn.execute("SELECT * FROM webcam_anim_recordings WHERE id = ?", (rec_id,)).fetchone()
    if rec_row is not None:
        doc = row_to_dict(conn, rec_row)
    pose = doc.get("poseTrackPath") or ""
    qstatus = doc.get("queueStatus") or "done"
    anim_status = "done" if qstatus == "done" else ("failed" if qstatus == "failed" else qstatus)
    _update_segment_anim(
        conn,
        segment_id,
        status=anim_status,
        webcam_recording_id=rec_id,
        pose_track_path=pose or None,
    )
    conn.commit()
    segs = [s for s in list_segments(conn, session_id) if s["id"] == segment_id]
    return {"ok": anim_status != "failed", "segment": segs[0] if segs else seg, "recording": doc}, None, 200


def process_checked_animations(
    conn: sqlite3.Connection,
    session_id: str,
    library_base: str,
    user_id: str | None = None,
) -> tuple[None, str | None, int]:
    for seg in list_segments(conn, session_id):
        if not seg.get("include") or not seg.get("processVideoAnimation"):
            continue
        if not str(seg.get("videoLibraryDocId") or "").strip():
            return None, "videoLibraryDocId required", 400
        _out, err, code = process_segment_animation(conn, session_id, seg["id"], library_base, user_id)
        if err:
            return None, err, code
    return None, None, 200


def save_or_sync(conn: sqlite3.Connection, session_id: str, user_id: str, library_base: str = "") -> tuple[dict[str, Any] | None, str | None, int]:
    session, err, code = _require_host(conn, session_id, user_id)
    if err:
        return None, err, code
    _, err, code = process_checked_animations(conn, session_id, library_base, user_id)
    if err:
        return None, err, code
    segments = list_segments(conn, session_id)
    quotes = list_quotes(conn, session_id)
    tree = _build_dialog_tree(session_id, segments, quotes)
    try:
        from continuuuum_api.dialogue_db import save_compiled_set, ensure_dialogue_schema
    except ImportError:
        from dialogue_db import save_compiled_set, ensure_dialogue_schema
    ensure_dialogue_schema(conn)
    saved = save_compiled_set(
        conn,
        set_id=tree["setId"],
        lemma_entry_id=None,
        name=f"Table read {session_id[:8]}",
        compiled=tree,
    )
    now = _now()
    assets = build_processed_assets(conn, session_id, dialogue_set_id=tree["setId"])
    conn.execute(
        """INSERT INTO table_read_processing_choices
           (session_id, composition_json, saved_at, dialogue_set_id, processed_assets_json, updated_at)
           VALUES (?, ?, ?, ?, ?, ?)
           ON CONFLICT(session_id) DO UPDATE SET
             composition_json=excluded.composition_json,
             saved_at=excluded.saved_at,
             dialogue_set_id=excluded.dialogue_set_id,
             processed_assets_json=excluded.processed_assets_json,
             updated_at=excluded.updated_at""",
        (session_id, json.dumps(composition_from_segments(segments)), now, tree["setId"], json.dumps(assets), now),
    )
    conn.commit()
    return {
        "ok": True,
        "savedAt": now,
        "saveLabel": "Sync",
        "dialogueSetId": tree["setId"],
        "set": saved,
        "nodeCount": len(tree["nodes"]),
        "processedAssets": assets,
        "partial": any(not s.get("include") for s in segments) or any(not s.get("audioUrl") and not s.get("uploadLibraryDocId") for s in segments),
    }, None, 200


def update_script(conn: sqlite3.Connection, session_id: str, user_id: str) -> tuple[dict[str, Any] | None, str | None, int]:
    session, err, code = _require_host(conn, session_id, user_id)
    if err:
        return None, err, code
    new_text = _load_draft_script(conn, session)
    old = latest_script_storage(conn, session_id)
    old_text = (old or {}).get("script_text") or ""
    sid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO table_read_script_storage (id, session_id, draft_episode_id, script_text, created_at)
           VALUES (?, ?, ?, ?, ?)""",
        (sid, session_id, session["draft_episode_id"], new_text, now),
    )
    kept = 0
    dropped = 0
    for q in list_quotes(conn, session_id):
        span = old_text[q["charStart"] : q["charEnd"]] if old_text else ""
        if span and span in new_text:
            start = new_text.index(span)
            conn.execute(
                "UPDATE table_read_character_quotes SET char_start = ?, char_end = ? WHERE id = ?",
                (start, start + len(span), q["id"]),
            )
            kept += 1
        elif span and new_text[q["charStart"] : q["charEnd"]] == span:
            kept += 1
        else:
            dropped += 1
    conn.commit()
    return {"ok": True, "scriptStorageId": sid, "keptQuotes": kept, "driftedQuotes": dropped}, None, 200


def restart_session(conn: sqlite3.Connection, session_id: str, user_id: str) -> tuple[dict[str, Any] | None, str | None, int]:
    session, err, code = _require_host(conn, session_id, user_id)
    if err:
        return None, err, code
    conn.execute(
        "UPDATE table_read_turns SET status = CASE WHEN turn_index = 0 THEN 'active' ELSE 'pending' END WHERE session_id = ?",
        (session_id,),
    )
    conn.execute(
        "UPDATE table_read_sessions SET status = 'active', ended_at = NULL, current_turn_index = 0 WHERE id = ?",
        (session_id,),
    )
    conn.commit()
    try:
        from continuuuum_api import table_read_routes as tr
    except ImportError:
        import table_read_routes as tr
    return tr._session_snapshot(conn, session_id, user_id), None, 200


def enrich_snapshot(conn: sqlite3.Connection, snap: dict[str, Any], session_id: str) -> dict[str, Any]:
    if not snap:
        return snap
    try:
        ensure_processing_tables(conn)
        freeze_script_if_needed(conn, session_id)
        payload = session_quote_payload(conn, session_id)
        quotes = payload["quotes"]
        choices = _choices_row(conn, session_id)
        snap["quotes"] = quotes
        snap["characters"] = payload["characters"]
        snap["quoteMap"] = payload["quoteMap"]
        snap["savedAt"] = choices.get("savedAt")
        snap["saveLabel"] = choices.get("saveLabel")
        if snap.get("session"):
            snap["session"]["savedAt"] = choices.get("savedAt")
    except sqlite3.OperationalError:
        snap.setdefault("quotes", [])
        snap.setdefault("quoteMap", [])
    return snap


def install_snapshot_patches() -> None:
    modules = []
    try:
        from continuuuum_api import table_read_routes as tr1

        modules.append(tr1)
    except ImportError:
        pass
    try:
        import table_read_routes as tr2

        if tr2 not in modules:
            modules.append(tr2)
    except ImportError:
        pass
    for tr in modules:
        if getattr(tr, "_processing_patched", False):
            continue
        orig_snap = tr._session_snapshot
        orig_load = tr._load_script_text
        tr._load_script_text_orig = orig_load

        def load_wrapped(conn: sqlite3.Connection, session: sqlite3.Row, _orig=orig_load) -> str:
            frozen = latest_script_storage(conn, session["id"])
            if frozen and frozen.get("script_text") is not None:
                return frozen["script_text"]
            return _orig(conn, session)

        def snap_wrapped(
            conn: sqlite3.Connection, session_id: str, viewer_user_id: str, _orig=orig_snap
        ) -> dict:
            snap = _orig(conn, session_id, viewer_user_id)
            return enrich_snapshot(conn, snap, session_id)

        tr._load_script_text = load_wrapped
        tr._session_snapshot = snap_wrapped
        tr._processing_patched = True


def register_table_read_processing_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    broadcast: Callable,
    broadcast_state: Callable,
    library_base: str,
    socketio=None,
) -> None:
    install_snapshot_patches()

    def _conn() -> sqlite3.Connection:
        conn = get_conn()
        try:
            from continuuuum_api.table_read_routes import ensure_table_read_tables
        except ImportError:
            from table_read_routes import ensure_table_read_tables
        ensure_table_read_tables(conn)
        ensure_processing_tables(conn)
        return conn

    def _host_guard(conn, session_id):
        return _require_host(conn, session_id, get_user())

    @app.route("/api/table-read/sessions/<session_id>/retreat", methods=["POST"])
    def retreat_table_read_turn(session_id: str):
        user_id = get_user()
        conn = _conn()
        try:
            snap, err = retreat_turn(conn, session_id, user_id)
            if err:
                return jsonify({"error": err}), 403 if err == "not your turn" else 404
            broadcast(session_id, "turn_changed", snap.get("currentTurn") if snap else {})
            broadcast_state(session_id)
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/quotes", methods=["GET", "POST"])
    def table_read_quotes(session_id: str):
        conn = _conn()
        try:
            if request.method == "GET":
                return jsonify(session_quote_payload(conn, session_id))
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            body = request.get_json(force=True) or {}
            name = (body.get("characterName") or body.get("character_name") or "").strip()
            if not name:
                return jsonify({"error": "characterName required"}), 400
            actor = (body.get("dialogActorId") or body.get("dialog_actor_id") or "").strip()
            freeze_script_if_needed(conn, session_id)
            has_start = body.get("charStart") is not None or body.get("start") is not None
            has_end = body.get("charEnd") is not None or body.get("end") is not None
            start = end = None
            if has_start or has_end:
                start = int(body.get("charStart") if body.get("charStart") is not None else body.get("start") or 0)
                end = int(body.get("charEnd") if body.get("charEnd") is not None else body.get("end") or 0)
            if is_located_quote(start, end):
                quote = add_quote(
                    conn,
                    session_id,
                    character_name=name,
                    dialog_actor_id=actor,
                    char_start=start,
                    char_end=end,
                )
                payload = {**session_quote_payload(conn, session_id), "added": quote}
            else:
                char = upsert_character(conn, session_id, name, actor)
                payload = {**session_quote_payload(conn, session_id), "added": None, "character": char}
            broadcast(session_id, "quotes_updated", payload)
            broadcast_state(session_id)
            return jsonify(payload), 201
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/quotes/<quote_id>", methods=["PATCH", "DELETE"])
    def table_read_quote_item(session_id: str, quote_id: str):
        conn = _conn()
        try:
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            if request.method == "DELETE":
                conn.execute(
                    "DELETE FROM table_read_character_quotes WHERE id = ? AND session_id = ?",
                    (quote_id, session_id),
                )
                conn.commit()
                payload = session_quote_payload(conn, session_id)
                broadcast(session_id, "quotes_updated", payload)
                return jsonify(payload)
            body = request.get_json(force=True) or {}
            sets = []
            vals: list[Any] = []
            if "characterName" in body or "character_name" in body:
                sets.append("character_name = ?")
                vals.append(body.get("characterName") or body.get("character_name"))
            if "dialogActorId" in body or "dialog_actor_id" in body:
                sets.append("dialog_actor_id = ?")
                vals.append(body.get("dialogActorId") or body.get("dialog_actor_id") or "")
            if "charStart" in body or "start" in body:
                sets.append("char_start = ?")
                vals.append(int(body.get("charStart") if body.get("charStart") is not None else body.get("start")))
            if "charEnd" in body or "end" in body:
                sets.append("char_end = ?")
                vals.append(int(body.get("charEnd") if body.get("charEnd") is not None else body.get("end")))
            if not sets:
                return jsonify({"error": "no fields"}), 400
            if ("char_start" in " ".join(sets) or "char_end" in " ".join(sets)):
                cur = conn.execute(
                    "SELECT char_start, char_end FROM table_read_character_quotes WHERE id = ? AND session_id = ?",
                    (quote_id, session_id),
                )
                row = cur.fetchone()
                if row:
                    new_start = int(body.get("charStart") if body.get("charStart") is not None else body.get("start") or row["char_start"])
                    new_end = int(body.get("charEnd") if body.get("charEnd") is not None else body.get("end") or row["char_end"])
                    if not is_located_quote(new_start, new_end):
                        return jsonify({"error": "quote span must have end > start (not [0, 1])"}), 400
            vals.extend([quote_id, session_id])
            conn.execute(
                f"UPDATE table_read_character_quotes SET {', '.join(sets)} WHERE id = ? AND session_id = ?",
                vals,
            )
            conn.commit()
            payload = session_quote_payload(conn, session_id)
            broadcast(session_id, "quotes_updated", payload)
            return jsonify(payload)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/processing", methods=["GET", "PATCH"])
    def table_read_processing(session_id: str):
        conn = _conn()
        try:
            if request.method == "GET":
                return jsonify(processing_payload(conn, session_id, library_base))
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            body = request.get_json(force=True) or {}
            for item in body.get("segments") or []:
                sid = item.get("id")
                if sid:
                    patch_segment(conn, session_id, sid, item)
            payload = processing_payload(conn, session_id, library_base, run_whisper=False)
            broadcast(session_id, "processing_updated", payload)
            return jsonify(payload)
        finally:
            conn.close()

    @app.route(
        "/api/table-read/sessions/<session_id>/processing/segments/<segment_id>",
        methods=["PATCH"],
    )
    def table_read_processing_segment(session_id: str, segment_id: str):
        conn = _conn()
        try:
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            body = request.get_json(force=True) or {}
            row = patch_segment(conn, session_id, segment_id, body)
            if not row:
                return jsonify({"error": "not found"}), 404
            payload = processing_payload(conn, session_id, library_base, run_whisper=False)
            broadcast(session_id, "processing_updated", payload)
            return jsonify({"segment": row, **payload})
        finally:
            conn.close()

    @app.route(
        "/api/table-read/sessions/<session_id>/processing/segments/<segment_id>/process-anim",
        methods=["POST"],
    )
    def table_read_processing_process_anim(session_id: str, segment_id: str):
        conn = _conn()
        try:
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            body = request.get_json(silent=True) or {}
            if body:
                patch_segment(conn, session_id, segment_id, body)
            out, err, code = process_segment_animation(
                conn, session_id, segment_id, library_base, get_user()
            )
            if err:
                return jsonify({"error": err}), code
            assets = build_processed_assets(conn, session_id)
            _persist_processed_assets(conn, session_id, assets)
            conn.commit()
            payload = processing_payload(conn, session_id, library_base, run_whisper=False)
            broadcast(session_id, "processing_updated", payload)
            return jsonify({**(out or {}), **payload})
        finally:
            conn.close()

    @app.route(
        "/api/table-read/sessions/<session_id>/processing/segments/<segment_id>/upload",
        methods=["POST"],
    )
    def table_read_processing_upload(session_id: str, segment_id: str):
        conn = _conn()
        try:
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            if "file" not in request.files:
                return jsonify({"error": "file required"}), 400
            f = request.files["file"]
            dest_dir = Path(tempfile.gettempdir()) / "table_read_takes" / session_id
            dest_dir.mkdir(parents=True, exist_ok=True)
            dest = dest_dir / f"{segment_id}_{f.filename or 'take.bin'}"
            f.save(str(dest))
            lib_id = None
            try:
                lib_id = usc_upload_file(str(dest), "audio", library_base)
            except Exception:
                lib_id = None
            if lib_id:
                conn.execute(
                    """UPDATE table_read_processing_segments
                       SET audio_url = ?, upload_library_doc_id = ?, status = 'pending', whisper_json = NULL, updated_at = ?
                       WHERE id = ? AND session_id = ?""",
                    (str(dest), str(lib_id), _now(), segment_id, session_id),
                )
            else:
                conn.execute(
                    """UPDATE table_read_processing_segments
                       SET audio_url = ?, status = 'pending', whisper_json = NULL, updated_at = ?
                       WHERE id = ? AND session_id = ?""",
                    (str(dest), _now(), segment_id, session_id),
                )
            conn.commit()
            result = run_whisper_for_segment(conn, session_id, segment_id, library_base)
            payload = processing_payload(conn, session_id, library_base, run_whisper=False)
            payload["whisperResult"] = result
            broadcast(session_id, "processing_updated", payload)
            return jsonify(payload)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/processing/whisper", methods=["POST"])
    def table_read_processing_whisper(session_id: str):
        conn = _conn()
        try:
            session, err, code = _host_guard(conn, session_id)
            if err:
                return jsonify({"error": err}), code
            body = request.get_json(silent=True) or {}
            segment_id = body.get("segmentId")
            if not segment_id:
                return jsonify({"error": "segmentId required"}), 400
            result = run_whisper_for_segment(conn, session_id, segment_id, library_base)
            payload = processing_payload(conn, session_id, library_base, run_whisper=False)
            payload["whisperResult"] = result
            broadcast(session_id, "processing_updated", payload)
            return jsonify(payload)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/save", methods=["POST"])
    @app.route("/api/table-read/sessions/<session_id>/sync", methods=["POST"])
    def table_read_save_or_sync(session_id: str):
        conn = _conn()
        try:
            out, err, code = save_or_sync(conn, session_id, get_user(), library_base)
            if err:
                return jsonify({"error": err}), code
            broadcast(session_id, "processing_updated", processing_payload(conn, session_id, library_base, run_whisper=False))
            return jsonify(out)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/update-script", methods=["POST"])
    def table_read_update_script(session_id: str):
        conn = _conn()
        try:
            out, err, code = update_script(conn, session_id, get_user())
            if err:
                return jsonify({"error": err}), code
            broadcast(session_id, "quotes_updated", session_quote_payload(conn, session_id))
            return jsonify(out)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/restart", methods=["POST"])
    def table_read_restart(session_id: str):
        conn = _conn()
        try:
            snap, err, code = restart_session(conn, session_id, get_user())
            if err:
                return jsonify({"error": err}), code
            broadcast(session_id, "turn_changed", snap.get("currentTurn") if snap else {})
            broadcast_state(session_id)
            return jsonify(snap)
        finally:
            conn.close()

    if socketio:

        @socketio.on("retreat_turn", namespace="/table-read")
        def tr_retreat_turn(data):
            data = data or {}
            session_id = (data.get("sessionId") or "").strip()
            if not session_id:
                return
            conn = _conn()
            try:
                snap, err = retreat_turn(conn, session_id, get_user())
                if not err:
                    broadcast(session_id, "turn_changed", snap.get("currentTurn") if snap else {})
                    broadcast_state(session_id)
            finally:
                conn.close()
