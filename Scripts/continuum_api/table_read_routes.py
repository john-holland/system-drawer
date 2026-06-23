"""Table read API — multi-user reading sessions, turn queue, recordings, Socket.IO sync."""

from __future__ import annotations

import json
import os
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable
from urllib.parse import urljoin

from flask import jsonify, request, send_from_directory

try:
    from continuum_api.table_read_blocks import assign_round_robin, parse_reading_blocks
except ImportError:
    from table_read_blocks import assign_round_robin, parse_reading_blocks

GetConn = Callable[[], sqlite3.Connection]
GetUser = Callable[[], str]

LIBRARY_APP_BASE = os.environ.get("CONTINUUM_LIBRARY_BASE", "http://127.0.0.1:5051").rstrip("/")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_table_read_tables(conn: sqlite3.Connection) -> None:
    schema_path = Path(__file__).resolve().parents[1] / "continuum_table_read_schema.sql"
    if schema_path.exists():
        conn.executescript(schema_path.read_text(encoding="utf-8"))
        conn.commit()


def _session_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "draftEpisodeId": r["draft_episode_id"],
        "hostUserId": r["host_user_id"],
        "status": r["status"],
        "contentSource": r["content_source"],
        "suggestionId": r["suggestion_id"],
        "segmentMode": r["segment_mode"],
        "commentMode": r["comment_mode"],
        "currentTurnIndex": r["current_turn_index"],
        "createdAt": r["created_at"],
        "endedAt": r["ended_at"],
    }


def _participant_row(r: sqlite3.Row) -> dict:
    return {
        "userId": r["user_id"],
        "displayName": r["display_name"],
        "joinOrder": r["join_order"],
        "role": r["role"],
        "joinedAt": r["joined_at"],
        "leftAt": r["left_at"],
    }


def _turn_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "turnIndex": r["turn_index"],
        "segmentType": r["segment_type"],
        "segmentRef": r["segment_ref"],
        "assignedUserId": r["assigned_user_id"],
        "textSnapshot": r["text_snapshot"],
        "status": r["status"],
        "charStart": r["char_start"],
        "charEnd": r["char_end"],
    }


def _recording_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "sessionId": r["session_id"],
        "userId": r["user_id"],
        "mediaKind": r["media_kind"],
        "status": r["status"],
        "partCount": r["part_count"],
        "libraryDocIds": json.loads(r["library_doc_ids_json"] or "[]"),
        "createdAt": r["created_at"],
        "finalizedAt": r["finalized_at"],
    }


def _get_session(conn: sqlite3.Connection, session_id: str) -> sqlite3.Row | None:
    cur = conn.execute("SELECT * FROM table_read_sessions WHERE id = ?", (session_id,))
    return cur.fetchone()


def _active_participants(conn: sqlite3.Connection, session_id: str) -> list[sqlite3.Row]:
    cur = conn.execute(
        """SELECT * FROM table_read_participants
           WHERE session_id = ? AND left_at IS NULL
           ORDER BY join_order ASC""",
        (session_id,),
    )
    return list(cur.fetchall())


def _load_script_text(conn: sqlite3.Connection, session: sqlite3.Row) -> str:
    draft_id = session["draft_episode_id"]
    if session["content_source"] == "suggestion" and session["suggestion_id"]:
        cur = conn.execute(
            "SELECT suggested_script_text FROM script_suggestions WHERE id = ? AND draft_episode_id = ?",
            (session["suggestion_id"], draft_id),
        )
        row = cur.fetchone()
        if row:
            return row["suggested_script_text"] or ""
        cur = conn.execute(
            "SELECT suggested_script_text FROM script_suggestions_archive WHERE id = ? AND draft_episode_id = ?",
            (session["suggestion_id"], draft_id),
        )
        row = cur.fetchone()
        return row["suggested_script_text"] or "" if row else ""

    cur = conn.execute(
        "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY updated_at DESC LIMIT 1",
        (draft_id,),
    )
    row = cur.fetchone()
    return row["script_text"] or "" if row else ""


def _load_comment_turns(conn: sqlite3.Connection, draft_id: str) -> list[dict]:
    try:
        cur = conn.execute(
            """SELECT id, comment_text, reviewer_id, text_selection_start, text_selection_end
               FROM reviewer_comments
               WHERE draft_episode_id = ? AND (comment_type IS NULL OR comment_type != 'suggestion')
               ORDER BY created_at ASC""",
            (draft_id,),
        )
    except sqlite3.OperationalError:
        return []
    turns = []
    for i, r in enumerate(cur.fetchall()):
        text = (r["comment_text"] or "").strip()
        if not text:
            continue
        turns.append(
            {
                "index": i,
                "kind": "comment",
                "text": text,
                "commentId": r["id"],
                "authorUserId": r["reviewer_id"],
                "charStart": r["text_selection_start"],
                "charEnd": r["text_selection_end"],
            }
        )
    return turns


def _rebuild_turn_queue(conn: sqlite3.Connection, session_id: str) -> None:
    session = _get_session(conn, session_id)
    if not session:
        return
    conn.execute("DELETE FROM table_read_turns WHERE session_id = ?", (session_id,))
    participants = _active_participants(conn, session_id)
    user_ids = [p["user_id"] for p in participants]
    if not user_ids:
        user_ids = [session["host_user_id"]]

    turns_data: list[dict] = []
    if session["segment_mode"] == "comments":
        turns_data = _load_comment_turns(conn, session["draft_episode_id"])
        if session["comment_mode"] == "round_robin":
            turns_data = assign_round_robin(turns_data, user_ids)
        else:
            for t in turns_data:
                t["assignedUserId"] = session["host_user_id"]
    else:
        script_text = _load_script_text(conn, session)
        blocks = parse_reading_blocks(script_text)
        turns_data = assign_round_robin(blocks, user_ids)

    for idx, block in enumerate(turns_data):
        status = "active" if idx == 0 else "pending"
        seg_ref = str(block.get("commentId") or block.get("index", idx))
        seg_type = "comment" if session["segment_mode"] == "comments" else "script_block"
        conn.execute(
            """INSERT INTO table_read_turns
               (id, session_id, turn_index, segment_type, segment_ref, assigned_user_id,
                text_snapshot, status, char_start, char_end)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                str(uuid.uuid4()),
                session_id,
                idx,
                seg_type,
                seg_ref,
                block.get("assignedUserId"),
                block.get("text", ""),
                status,
                block.get("charStart"),
                block.get("charEnd"),
            ),
        )
    conn.execute(
        "UPDATE table_read_sessions SET current_turn_index = 0 WHERE id = ?",
        (session_id,),
    )
    conn.commit()


def _current_turn(conn: sqlite3.Connection, session_id: str) -> sqlite3.Row | None:
    session = _get_session(conn, session_id)
    if not session:
        return None
    cur = conn.execute(
        """SELECT * FROM table_read_turns
           WHERE session_id = ? AND turn_index = ?
           LIMIT 1""",
        (session_id, session["current_turn_index"]),
    )
    return cur.fetchone()


def _session_snapshot(conn: sqlite3.Connection, session_id: str, viewer_user_id: str) -> dict:
    session = _get_session(conn, session_id)
    if not session:
        return {}
    cur = conn.execute(
        "SELECT * FROM table_read_participants WHERE session_id = ? ORDER BY join_order",
        (session_id,),
    )
    participants = [_participant_row(r) for r in cur.fetchall()]
    cur = conn.execute(
        "SELECT * FROM table_read_turns WHERE session_id = ? ORDER BY turn_index",
        (session_id,),
    )
    turns = [_turn_row(r) for r in cur.fetchall()]
    turn = _current_turn(conn, session_id)
    current = _turn_row(turn) if turn else None
    script_text = _load_script_text(conn, session) if session["segment_mode"] == "script" else ""
    your_turn = bool(current and current.get("assignedUserId") == viewer_user_id)
    cur = conn.execute(
        "SELECT * FROM table_read_recordings WHERE session_id = ?",
        (session_id,),
    )
    recordings = [_recording_row(r) for r in cur.fetchall()]
    return {
        "session": _session_row(session),
        "participants": participants,
        "turns": turns,
        "currentTurn": current,
        "scriptText": script_text,
        "yourTurn": your_turn,
        "recordings": recordings,
    }


def _finalize_user_recordings(conn: sqlite3.Connection, session_id: str, user_id: str) -> None:
    now = _now()
    conn.execute(
        """UPDATE table_read_recordings
           SET status = 'finalized', finalized_at = ?
           WHERE session_id = ? AND user_id = ? AND status = 'recording'""",
        (now, session_id, user_id),
    )
    conn.commit()


def register_table_read_routes(
    app,
    get_conn: GetConn,
    get_user: GetUser,
    socketio=None,
    library_base: str | None = None,
) -> None:
    static_dir = Path(__file__).resolve().parent / "static" / "table-read"
    lib_base = (library_base or LIBRARY_APP_BASE).rstrip("/")

    def _ensure(conn: sqlite3.Connection) -> None:
        ensure_table_read_tables(conn)

    def _broadcast(session_id: str, event: str, payload: dict) -> None:
        if socketio:
            socketio.emit(event, payload, room=session_id, namespace="/table-read")

    def _broadcast_state(session_id: str, viewer_user_id: str | None = None) -> None:
        conn = get_conn()
        try:
            _ensure(conn)
            snap = _session_snapshot(conn, session_id, viewer_user_id or get_user())
            _broadcast(session_id, "session_state", snap)
        finally:
            conn.close()

    @app.route("/table-read")
    @app.route("/table-read/<path:subpath>")
    def serve_table_read(subpath=None):
        return send_from_directory(static_dir, "index.html")

    @app.route("/api/table-read/sessions", methods=["POST"])
    def create_table_read_session():
        body = request.get_json(force=True) or {}
        draft_id = (body.get("draftEpisodeId") or "").strip()
        if not draft_id:
            return jsonify({"error": "draftEpisodeId required"}), 400
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT committed_at FROM draft_episodes WHERE id = ?", (draft_id,))
            draft = cur.fetchone()
            if not draft:
                return jsonify({"error": "draft not found"}), 404
            if draft["committed_at"]:
                return jsonify({"error": "draft is committed"}), 400
            sid = str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO table_read_sessions
                   (id, draft_episode_id, host_user_id, status, content_source, suggestion_id,
                    segment_mode, comment_mode, current_turn_index, created_at)
                   VALUES (?, ?, ?, 'active', ?, ?, ?, ?, 0, ?)""",
                (
                    sid,
                    draft_id,
                    user_id,
                    body.get("contentSource", "draft"),
                    body.get("suggestionId"),
                    body.get("segmentMode", "script"),
                    body.get("commentMode", "all"),
                    now,
                ),
            )
            conn.execute(
                """INSERT INTO table_read_participants
                   (session_id, user_id, display_name, join_order, role, joined_at)
                   VALUES (?, ?, ?, 0, 'host', ?)""",
                (sid, user_id, body.get("displayName") or user_id, now),
            )
            conn.commit()
            _rebuild_turn_queue(conn, sid)
            return jsonify({"id": sid, "draftEpisodeId": draft_id}), 201
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>", methods=["GET"])
    def get_table_read_session(session_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            snap = _session_snapshot(conn, session_id, get_user())
            if not snap:
                return jsonify({"error": "not found"}), 404
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/join", methods=["POST"])
    def join_table_read_session(session_id: str):
        body = request.get_json(force=True) or {}
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            session = _get_session(conn, session_id)
            if not session:
                return jsonify({"error": "not found"}), 404
            if session["status"] != "active":
                return jsonify({"error": "session ended"}), 400
            cur = conn.execute(
                "SELECT * FROM table_read_participants WHERE session_id = ? AND user_id = ?",
                (session_id, user_id),
            )
            existing = cur.fetchone()
            now = _now()
            if existing:
                if existing["left_at"]:
                    conn.execute(
                        """UPDATE table_read_participants SET left_at = NULL, display_name = ?, joined_at = ?
                           WHERE session_id = ? AND user_id = ?""",
                        (body.get("displayName") or user_id, now, session_id, user_id),
                    )
            else:
                cur = conn.execute(
                    "SELECT COALESCE(MAX(join_order), -1) + 1 FROM table_read_participants WHERE session_id = ?",
                    (session_id,),
                )
                order = cur.fetchone()[0]
                conn.execute(
                    """INSERT INTO table_read_participants
                       (session_id, user_id, display_name, join_order, role, joined_at)
                       VALUES (?, ?, ?, ?, 'reader', ?)""",
                    (session_id, user_id, body.get("displayName") or user_id, order, now),
                )
            conn.commit()
            _rebuild_turn_queue(conn, session_id)
            snap = _session_snapshot(conn, session_id, user_id)
            _broadcast(session_id, "participant_joined", {"userId": user_id})
            _broadcast_state(session_id)
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/leave", methods=["POST"])
    def leave_table_read_session(session_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            now = _now()
            conn.execute(
                "UPDATE table_read_participants SET left_at = ? WHERE session_id = ? AND user_id = ?",
                (now, session_id, user_id),
            )
            _finalize_user_recordings(conn, session_id, user_id)
            conn.commit()
            _broadcast(session_id, "participant_left", {"userId": user_id})
            _broadcast_state(session_id)
            return jsonify({"ok": True})
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/end", methods=["POST"])
    def end_table_read_session(session_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            session = _get_session(conn, session_id)
            if not session:
                return jsonify({"error": "not found"}), 404
            if session["host_user_id"] != user_id:
                return jsonify({"error": "host only"}), 403
            now = _now()
            conn.execute(
                "UPDATE table_read_sessions SET status = 'ended', ended_at = ? WHERE id = ?",
                (now, session_id),
            )
            conn.execute(
                """UPDATE table_read_recordings SET status = 'finalized', finalized_at = ?
                   WHERE session_id = ? AND status = 'recording'""",
                (now, session_id),
            )
            conn.commit()
            _broadcast(session_id, "session_ended", {"sessionId": session_id})
            _broadcast_state(session_id)
            return jsonify({"ok": True})
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>", methods=["PATCH"])
    def patch_table_read_session(session_id: str):
        body = request.get_json(force=True) or {}
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            session = _get_session(conn, session_id)
            if not session:
                return jsonify({"error": "not found"}), 404
            if session["host_user_id"] != user_id:
                return jsonify({"error": "host only"}), 403
            fields = []
            params = []
            for key, col in (
                ("contentSource", "content_source"),
                ("suggestionId", "suggestion_id"),
                ("segmentMode", "segment_mode"),
                ("commentMode", "comment_mode"),
            ):
                if key in body:
                    fields.append(f"{col} = ?")
                    params.append(body[key])
            if fields:
                params.append(session_id)
                conn.execute(
                    f"UPDATE table_read_sessions SET {', '.join(fields)} WHERE id = ?",
                    params,
                )
                conn.commit()
                _rebuild_turn_queue(conn, session_id)
            _broadcast(session_id, "mode_changed", body)
            _broadcast_state(session_id)
            snap = _session_snapshot(conn, session_id, user_id)
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/rebuild-queue", methods=["POST"])
    def rebuild_table_read_queue(session_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            session = _get_session(conn, session_id)
            if not session:
                return jsonify({"error": "not found"}), 404
            if session["host_user_id"] != user_id:
                return jsonify({"error": "host only"}), 403
            _rebuild_turn_queue(conn, session_id)
            _broadcast_state(session_id)
            return jsonify(_session_snapshot(conn, session_id, user_id))
        finally:
            conn.close()

    def _advance_turn(conn: sqlite3.Connection, session_id: str, user_id: str) -> tuple[dict | None, str | None]:
        session = _get_session(conn, session_id)
        if not session or session["status"] != "active":
            return None, "not found"
        turn = _current_turn(conn, session_id)
        if not turn:
            return None, "no active turn"
        if user_id != session["host_user_id"] and turn["assigned_user_id"] != user_id:
            return None, "not your turn"
        conn.execute(
            "UPDATE table_read_turns SET status = 'done' WHERE id = ?",
            (turn["id"],),
        )
        next_index = session["current_turn_index"] + 1
        cur = conn.execute(
            """SELECT * FROM table_read_turns
               WHERE session_id = ? AND turn_index = ?""",
            (session_id, next_index),
        )
        nxt = cur.fetchone()
        if nxt:
            conn.execute(
                "UPDATE table_read_turns SET status = 'active' WHERE id = ?",
                (nxt["id"],),
            )
            conn.execute(
                "UPDATE table_read_sessions SET current_turn_index = ? WHERE id = ?",
                (next_index, session_id),
            )
        conn.commit()
        return _session_snapshot(conn, session_id, user_id), None

    @app.route("/api/table-read/sessions/<session_id>/advance", methods=["POST"])
    def advance_table_read_turn(session_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            snap, err = _advance_turn(conn, session_id, user_id)
            if err:
                return jsonify({"error": err}), 403 if err == "not your turn" else 404
            _broadcast(session_id, "turn_changed", snap.get("currentTurn"))
            _broadcast_state(session_id)
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/skip", methods=["POST"])
    def skip_table_read_turn(session_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            session = _get_session(conn, session_id)
            if not session:
                return jsonify({"error": "not found"}), 404
            if session["host_user_id"] != user_id:
                return jsonify({"error": "host only"}), 403
            turn = _current_turn(conn, session_id)
            if turn:
                conn.execute(
                    "UPDATE table_read_turns SET status = 'skipped' WHERE id = ?",
                    (turn["id"],),
                )
            next_index = session["current_turn_index"] + 1
            cur = conn.execute(
                "SELECT * FROM table_read_turns WHERE session_id = ? AND turn_index = ?",
                (session_id, next_index),
            )
            nxt = cur.fetchone()
            if nxt:
                conn.execute("UPDATE table_read_turns SET status = 'active' WHERE id = ?", (nxt["id"],))
                conn.execute(
                    "UPDATE table_read_sessions SET current_turn_index = ? WHERE id = ?",
                    (next_index, session_id),
                )
            conn.commit()
            snap = _session_snapshot(conn, session_id, user_id)
            _broadcast(session_id, "turn_changed", snap.get("currentTurn"))
            _broadcast_state(session_id)
            return jsonify(snap)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/recordings", methods=["POST"])
    def start_table_read_recording(session_id: str):
        body = request.get_json(force=True) or {}
        media_kind = (body.get("mediaKind") or "audio").strip().lower()
        if media_kind not in ("audio", "video"):
            return jsonify({"error": "invalid mediaKind"}), 400
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            rid = str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO table_read_recordings
                   (id, session_id, user_id, media_kind, status, part_count, library_doc_ids_json, created_at)
                   VALUES (?, ?, ?, ?, 'recording', 0, '[]', ?)""",
                (rid, session_id, user_id, media_kind, now),
            )
            conn.commit()
            rec = _recording_row(
                conn.execute("SELECT * FROM table_read_recordings WHERE id = ?", (rid,)).fetchone()
            )
            _broadcast(session_id, "recording_status", rec)
            return jsonify(rec), 201
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/recordings/<rec_id>/parts", methods=["POST"])
    def register_recording_part(session_id: str, rec_id: str):
        body = request.get_json(force=True) or {}
        user_id = get_user()
        library_doc_id = body.get("libraryDocId")
        part_index = body.get("partIndex")
        if library_doc_id is None:
            return jsonify({"error": "libraryDocId required"}), 400
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute(
                "SELECT * FROM table_read_recordings WHERE id = ? AND session_id = ? AND user_id = ?",
                (rec_id, session_id, user_id),
            )
            rec = cur.fetchone()
            if not rec:
                return jsonify({"error": "recording not found"}), 404
            ids = json.loads(rec["library_doc_ids_json"] or "[]")
            if part_index is not None and 0 <= int(part_index) < len(ids):
                ids[int(part_index)] = library_doc_id
            else:
                ids.append(library_doc_id)
            conn.execute(
                """UPDATE table_read_recordings
                   SET part_count = ?, library_doc_ids_json = ?
                   WHERE id = ?""",
                (len(ids), json.dumps(ids), rec_id),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM table_read_recordings WHERE id = ?", (rec_id,)).fetchone()
            out = _recording_row(row)
            _broadcast(session_id, "recording_status", out)
            return jsonify(out)
        finally:
            conn.close()

    @app.route("/api/table-read/sessions/<session_id>/recordings/<rec_id>/finalize", methods=["POST"])
    def finalize_recording(session_id: str, rec_id: str):
        user_id = get_user()
        conn = get_conn()
        try:
            _ensure(conn)
            now = _now()
            cur = conn.execute(
                "SELECT * FROM table_read_recordings WHERE id = ? AND session_id = ?",
                (rec_id, session_id),
            )
            rec = cur.fetchone()
            if not rec:
                return jsonify({"error": "not found"}), 404
            if rec["user_id"] != user_id:
                return jsonify({"error": "forbidden"}), 403
            conn.execute(
                "UPDATE table_read_recordings SET status = 'finalized', finalized_at = ? WHERE id = ?",
                (now, rec_id),
            )
            conn.commit()
            row = conn.execute("SELECT * FROM table_read_recordings WHERE id = ?", (rec_id,)).fetchone()
            out = _recording_row(row)
            _broadcast(session_id, "recording_status", out)
            return jsonify(out)
        finally:
            conn.close()

    @app.route("/api/table-read/usc-upload", methods=["POST"])
    def table_read_usc_upload():
        """Proxy multipart upload to USC library server."""
        document_type = (request.form.get("document_type") or "").strip().lower()
        if document_type not in ("audio", "video"):
            return jsonify({"error": "document_type must be audio or video"}), 400
        if "file" not in request.files:
            return jsonify({"error": "file required"}), 400
        f = request.files["file"]
        try:
            import requests
        except ImportError:
            return jsonify({"error": "requests package required for USC upload proxy"}), 500

        upload_url = urljoin(lib_base + "/", "api/library/upload")
        data = {
            "document_type": document_type,
            "type_metadata": request.form.get("type_metadata") or "{}",
        }
        for key in ("lat", "lon", "altitude_m", "url"):
            if request.form.get(key) is not None:
                data[key] = request.form.get(key)
        headers = {}
        tenant = request.headers.get("X-Tenant-ID")
        if tenant:
            headers["X-Tenant-ID"] = tenant
        files = {"file": (f.filename or "part.webm", f.stream, f.content_type or "application/octet-stream")}
        resp = requests.post(upload_url, data=data, files=files, headers=headers, timeout=120)
        try:
            payload = resp.json()
        except Exception:
            payload = {"error": resp.text}
        return jsonify(payload), resp.status_code

    if socketio:

        @socketio.on("connect", namespace="/table-read")
        def tr_connect():
            pass

        @socketio.on("join_session", namespace="/table-read")
        def tr_join_session(data):
            data = data or {}
            session_id = (data.get("sessionId") or "").strip()
            if not session_id:
                return
            from flask_socketio import join_room

            join_room(session_id)
            conn = get_conn()
            try:
                _ensure(conn)
                snap = _session_snapshot(conn, session_id, get_user())
                socketio.emit("session_state", snap, room=request.sid, namespace="/table-read")
            finally:
                conn.close()

        @socketio.on("leave_session", namespace="/table-read")
        def tr_leave_session(data):
            data = data or {}
            session_id = (data.get("sessionId") or "").strip()
            if session_id:
                from flask_socketio import leave_room

                leave_room(session_id)

        @socketio.on("advance_turn", namespace="/table-read")
        def tr_advance_turn(data):
            data = data or {}
            session_id = (data.get("sessionId") or "").strip()
            if not session_id:
                return
            conn = get_conn()
            try:
                _ensure(conn)
                snap, err = _advance_turn(conn, session_id, get_user())
                if not err:
                    _broadcast(session_id, "turn_changed", snap.get("currentTurn"))
                    _broadcast_state(session_id)
            finally:
                conn.close()

        @socketio.on("disconnect", namespace="/table-read")
        def tr_disconnect():
            pass
