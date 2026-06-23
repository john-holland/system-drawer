"""Camera pathing API — scenes, ratings, votes, threaded comments, LSTM hints."""

from __future__ import annotations

import json
import re
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable

from flask import jsonify, request, send_from_directory

GetConn = Callable[[], sqlite3.Connection]
MENTION_RE = re.compile(r"@([a-zA-Z0-9_-]+)")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_camera_tables(conn: sqlite3.Connection) -> None:
    schema_path = Path(__file__).resolve().parents[1] / "continuum_camera_schema.sql"
    if schema_path.exists():
        conn.executescript(schema_path.read_text(encoding="utf-8"))
        conn.commit()


def _parse_mentions(body: str) -> list[str]:
    return list(dict.fromkeys(MENTION_RE.findall(body or "")))


def _scene_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "episodeId": r["episode_id"],
        "shotId": r["shot_id"],
        "focusMode": r["focus_mode"],
        "topology": json.loads(r["topology_json"] or "null"),
        "rigPose": json.loads(r["rig_pose_json"] or "null"),
        "memorabilityMl": r["memorability_ml"],
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def _comment_row(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "domain": r["domain"],
        "anchorType": r["anchor_type"],
        "anchorId": r["anchor_id"],
        "parentCommentId": r["parent_comment_id"],
        "authorUserId": r["author_user_id"],
        "bodyText": r["body_text"],
        "mentions": json.loads(r["mentions_json"] or "[]"),
        "directLink": r["direct_link"],
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def register_camera_routes(app, get_conn: GetConn, get_user: Callable[[], str]) -> None:
    static_dir = Path(__file__).resolve().parent / "static" / "camera-pathing"

    @app.route("/camera-pathing")
    @app.route("/camera-scenes")
    @app.route("/camera-scenes/<path:subpath>")
    def serve_camera_pathing(subpath=None):
        return send_from_directory(static_dir, "index.html")

    def _ensure(conn: sqlite3.Connection) -> None:
        ensure_camera_tables(conn)

    @app.route("/api/camera/scenes", methods=["GET", "POST"])
    def camera_scenes():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                episode_id = request.args.get("episodeId")
                if episode_id:
                    cur = conn.execute(
                        "SELECT * FROM camera_scenes WHERE episode_id = ? ORDER BY created_at DESC",
                        (episode_id,),
                    )
                else:
                    cur = conn.execute("SELECT * FROM camera_scenes ORDER BY created_at DESC LIMIT 200")
                return jsonify({"items": [_scene_row(r) for r in cur.fetchall()]})

            body = request.get_json(force=True) or {}
            sid = body.get("id") or str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO camera_scenes
                   (id, episode_id, shot_id, focus_mode, topology_json, rig_pose_json, memorability_ml, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    sid,
                    body.get("episodeId"),
                    body.get("shotId"),
                    body.get("focusMode", "Character"),
                    json.dumps(body.get("topology")),
                    json.dumps(body.get("rigPose")),
                    body.get("memorabilityMl"),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM camera_scenes WHERE id = ?", (sid,))
            return jsonify(_scene_row(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/camera/scenes/<scene_id>", methods=["GET", "PATCH"])
    def camera_scene(scene_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM camera_scenes WHERE id = ?", (scene_id,))
                row = cur.fetchone()
                if not row:
                    return jsonify({"error": "not found"}), 404
                return jsonify(_scene_row(row))

            body = request.get_json(force=True) or {}
            fields = []
            params = []
            mapping = {
                "episodeId": "episode_id",
                "shotId": "shot_id",
                "focusMode": "focus_mode",
                "memorabilityMl": "memorability_ml",
            }
            for js, col in mapping.items():
                if js in body:
                    fields.append(f"{col} = ?")
                    params.append(body[js])
            if "topology" in body:
                fields.append("topology_json = ?")
                params.append(json.dumps(body["topology"]))
            if "rigPose" in body:
                fields.append("rig_pose_json = ?")
                params.append(json.dumps(body["rigPose"]))
            if fields:
                fields.append("updated_at = ?")
                params.append(_now())
                params.append(scene_id)
                conn.execute(f"UPDATE camera_scenes SET {', '.join(fields)} WHERE id = ?", params)
                conn.commit()
            cur = conn.execute("SELECT * FROM camera_scenes WHERE id = ?", (scene_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(_scene_row(row))
        finally:
            conn.close()

    @app.route("/api/camera/scenes/<scene_id>/rate", methods=["POST"])
    def camera_rate(scene_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            score = int(body.get("score", 0))
            if score < 1 or score > 5:
                return jsonify({"error": "score must be 1-5"}), 400
            user_id = get_user()
            now = _now()
            conn.execute(
                """INSERT INTO camera_scene_ratings (scene_id, user_id, score, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?)
                   ON CONFLICT(scene_id, user_id) DO UPDATE SET score=excluded.score, updated_at=excluded.updated_at""",
                (scene_id, user_id, score, now, now),
            )
            conn.commit()
            return jsonify({"ok": True, "score": score})
        finally:
            conn.close()

    @app.route("/api/camera/scenes/<scene_id>/vote", methods=["POST"])
    def camera_vote(scene_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            vote = int(body.get("vote", 0))
            if vote not in (-1, 1):
                return jsonify({"error": "vote must be -1 or 1"}), 400
            user_id = get_user()
            now = _now()
            conn.execute(
                """INSERT INTO camera_scene_votes (scene_id, user_id, vote, created_at)
                   VALUES (?, ?, ?, ?)
                   ON CONFLICT(scene_id, user_id) DO UPDATE SET vote=excluded.vote, created_at=excluded.created_at""",
                (scene_id, user_id, vote, now),
            )
            conn.commit()
            return jsonify({"ok": True, "vote": vote})
        finally:
            conn.close()

    @app.route("/api/camera/scenes/<scene_id>/comments", methods=["GET", "POST"])
    def camera_comments(scene_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute(
                    """SELECT * FROM continuum_threaded_comments
                       WHERE domain = 'camera' AND anchor_id = ? AND deleted_at IS NULL
                       ORDER BY created_at ASC""",
                    (scene_id,),
                )
                return jsonify({"items": [_comment_row(r) for r in cur.fetchall()]})

            body = request.get_json(force=True) or {}
            text = (body.get("bodyText") or "").strip()
            if not text:
                return jsonify({"error": "bodyText required"}), 400
            cid = str(uuid.uuid4())
            user_id = get_user()
            now = _now()
            mentions = _parse_mentions(text)
            link = f"/camera-scenes/{scene_id}#comment-{cid}"
            conn.execute(
                """INSERT INTO continuum_threaded_comments
                   (id, domain, anchor_type, anchor_id, parent_comment_id, author_user_id, body_text,
                    mentions_json, direct_link, created_at, updated_at)
                   VALUES (?, 'camera', 'scene', ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    cid,
                    scene_id,
                    body.get("parentCommentId"),
                    user_id,
                    text,
                    json.dumps(mentions),
                    link,
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM continuum_threaded_comments WHERE id = ?", (cid,))
            return jsonify(_comment_row(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/camera/scenes/<scene_id>/comments/<comment_id>/reply", methods=["POST"])
    def camera_comment_reply(scene_id: str, comment_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            text = (body.get("bodyText") or "").strip()
            if not text:
                return jsonify({"error": "bodyText required"}), 400
            cid = str(uuid.uuid4())
            user_id = get_user()
            now = _now()
            mentions = _parse_mentions(text)
            link = f"/camera-scenes/{scene_id}#comment-{cid}"
            conn.execute(
                """INSERT INTO continuum_threaded_comments
                   (id, domain, anchor_type, anchor_id, parent_comment_id, author_user_id, body_text,
                    mentions_json, direct_link, created_at, updated_at)
                   VALUES (?, 'camera', 'scene', ?, ?, ?, ?, ?, ?, ?, ?)""",
                (cid, scene_id, comment_id, user_id, text, json.dumps(mentions), link, now, now),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM continuum_threaded_comments WHERE id = ?", (cid,))
            return jsonify(_comment_row(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/camera/hints/<scene_id>", methods=["GET"])
    def camera_hints(scene_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT memorability_ml FROM camera_scenes WHERE id = ?", (scene_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404

            cur = conn.execute(
                "SELECT AVG(score) FROM camera_scene_ratings WHERE scene_id = ?",
                (scene_id,),
            )
            avg = cur.fetchone()[0]

            cur = conn.execute(
                "SELECT SUM(vote) FROM camera_scene_votes WHERE scene_id = ?",
                (scene_id,),
            )
            vote_sum = cur.fetchone()[0] or 0

            mem_ml = float(row["memorability_ml"] or 0.5)
            user_mean = float(avg) if avg is not None else 3.0
            merged_mem = 0.6 * (user_mean / 5.0) + 0.4 * mem_ml

            mode_bias = [0.0] * 8
            for i in range(8):
                mode_bias[i] = (i % 3) * 0.05 - vote_sum * 0.02

            return jsonify(
                {
                    "sceneId": scene_id,
                    "memorabilityMl": mem_ml,
                    "userRatingMean": user_mean,
                    "mergedMemorability": merged_mem,
                    "modeHintBias": mode_bias,
                    "voteSum": vote_sum,
                }
            )
        finally:
            conn.close()
