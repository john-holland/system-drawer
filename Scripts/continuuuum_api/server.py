"""
Flask API for continuuuum: episode script, thesaurus alternatives, AST nodes, rebalance, change-of-basis.
Run with continuuuum.db path; script-output app proxies /api to this server.
"""

import json
import os
import sys
import sqlite3
import uuid
from pathlib import Path

from flask import Flask, Response, g, jsonify, redirect, request, send_from_directory

# Allow importing thesaurus when run from repo root or Scripts
_scripts = Path(__file__).resolve().parent.parent
_api = Path(__file__).resolve().parent
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))
if str(_api) not in sys.path:
    sys.path.insert(0, str(_api))
from thesaurus import farey_ast, xliff_converter, script_to_ast
from thesaurus.language_resolver import ensure_default_languages
import continuuuum_screenplay_work_orders as screenplay_wo

try:
    from continuuuum_api import cave_adapter, cave_hierarchy_adapter, herb_garden
except ImportError:
    cave_adapter = None
    cave_hierarchy_adapter = None
    herb_garden = None

try:
    from continuuuum_api.localization_routes import archive_review_comments_on_deny, register_localization_routes
except ImportError:
    from localization_routes import archive_review_comments_on_deny, register_localization_routes

try:
    from continuuuum_api.lemma_routes import register_lemma_routes
except ImportError:
    from lemma_routes import register_lemma_routes

try:
    from continuuuum_api.script_output_routes import register_script_output_routes
except ImportError:
    from script_output_routes import register_script_output_routes

try:
    from continuuuum_api.telecom_routes import register_telecom_routes
    from continuuuum_api.society_routes import register_society_routes
    from continuuuum_api.galactic_routes import register_galactic_routes
except ImportError:
    from telecom_routes import register_telecom_routes
    from society_routes import register_society_routes
    from galactic_routes import register_galactic_routes

try:
    from continuuuum_api.camera_routes import register_camera_routes
except ImportError:
    from camera_routes import register_camera_routes

try:
    from continuuuum_api.table_read_routes import register_table_read_routes
except ImportError:
    from table_read_routes import register_table_read_routes

try:
    from continuuuum_api.cave_routes import register_cave_routes
    from continuuuum_api.resaurce_routes import register_resaurce_routes
    from continuuuum_api.saurce_routes import register_saurce_routes
    from continuuuum_api.drawer_game_routes import register_drawer_game_routes
    from continuuuum_api.library_routes import register_library_routes
    from continuuuum_api.sql_viewer_routes import register_sql_viewer_routes
    from continuuuum_api.story_routes import register_story_routes
    from continuuuum_api.chat_routes import register_chat_routes
    from continuuuum_api.production_proxy_routes import register_production_proxy_routes
    from continuuuum_api.calendar_routes import register_calendar_routes
    from continuuuum_api.agile_ui_routes import register_agile_ui_routes
    from continuuuum_api.mod_routes import register_mod_routes
except ImportError:
    from cave_routes import register_cave_routes
    from resaurce_routes import register_resaurce_routes
    from saurce_routes import register_saurce_routes
    from drawer_game_routes import register_drawer_game_routes
    from library_routes import register_library_routes
    from sql_viewer_routes import register_sql_viewer_routes
    from story_routes import register_story_routes
    from chat_routes import register_chat_routes
    from production_proxy_routes import register_production_proxy_routes
    from calendar_routes import register_calendar_routes
    from agile_ui_routes import register_agile_ui_routes
    from mod_routes import register_mod_routes

try:
    from flask_socketio import SocketIO
except ImportError:
    SocketIO = None

app = Flask(__name__, static_folder=str(Path(__file__).resolve().parent / "static"), static_url_path="/static")
app.secret_key = os.environ.get("CONTINUUUUM_SECRET_KEY", "continuuuum-dev-secret-change-in-production")

DEV_CORS_ORIGINS = {
    "http://localhost:5174",
    "http://127.0.0.1:5174",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5175",
    "http://127.0.0.1:5175",
    "http://localhost:8080",
    "http://127.0.0.1:8080",
}

# USC spatial library — served on same origin via library_routes (inline serve_library API).
# Set CONTINUUUUM_LIBRARY_BASE=http://127.0.0.1:5051 only for dual-server dev.
LIBRARY_APP_BASE = os.environ.get("CONTINUUUUM_LIBRARY_BASE", "").rstrip("/") or "http://127.0.0.1:5050"


def _apply_cors_headers(response):
    origin = request.headers.get("Origin")
    if origin in DEV_CORS_ORIGINS:
        response.headers["Access-Control-Allow-Origin"] = origin
        response.headers["Access-Control-Allow-Headers"] = "Content-Type, X-User-ID, X-Admin"
        response.headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS"
        response.headers["Access-Control-Max-Age"] = "86400"
    return response


@app.before_request
def handle_cors_preflight():
    if request.method != "OPTIONS":
        return None
    origin = request.headers.get("Origin", "")
    if origin not in DEV_CORS_ORIGINS:
        return None
    return _apply_cors_headers(Response(status=204))

# Audit: log API access; user from X-User-ID, admin from X-Admin
AUDIT_ENABLED = os.environ.get("CONTINUUUUM_AUDIT", "1").lower() in ("1", "true", "yes")


def _get_current_user():
    return request.headers.get("X-User-ID", "anonymous")


def _is_admin():
    return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")


@app.before_request
def before_audit():
    if AUDIT_ENABLED:
        g._audit_start = __import__("time").time()
        g._audit_request_id = str(uuid.uuid4())[:8]


@app.after_request
def after_audit(response):
    response = _apply_cors_headers(response)
    if not AUDIT_ENABLED:
        return response
    path = request.path
    if not path.startswith("/api/"):
        return response
    try:
        conn = get_conn()
        user_id = _get_current_user()
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        audit_id = str(uuid.uuid4())
        episode_id = None
        if "/episodes/" in path:
            parts = path.split("/")
            for i, p in enumerate(parts):
                if p == "episodes" and i + 1 < len(parts) and parts[i + 1] not in ("extract-screenplay-work-orders", ""):
                    episode_id = parts[i + 1]
                    break
        conn.execute(
            """INSERT INTO api_audit_log (id, timestamp, user_id, api_path, method, remark, request_id, episode_id, status_code)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (audit_id, now, user_id, path, request.method, None, g.get("_audit_request_id"), episode_id, response.status_code),
        )
        conn.execute(
            "INSERT OR REPLACE INTO user_presence (user_id, last_seen_at, session_id) VALUES (?, ?, ?)",
            (user_id, now, g.get("_audit_request_id")),
        )
        conn.commit()
        conn.close()
    except Exception:
        pass
    return response


# Set via env or default relative to repo
DEFAULT_DB = Path(__file__).resolve().parent.parent.parent / "continuuuum.db"


def get_db_path() -> Path:
    return Path(os.environ.get("CONTINUUUUM_DB", str(DEFAULT_DB)))


_schema_initialized = False


def get_conn():
    """Open DB connection; on first call in this process, run USC ensure_database if available."""
    global _schema_initialized
    conn = sqlite3.connect(get_db_path())
    conn.row_factory = sqlite3.Row
    if not _schema_initialized:
        try:
            from unified_semantic_archiver.db import ensure_database

            ensure_database(conn)
        except ImportError:
            pass
        try:
            from continuuuum_api.thesaurus_db import ensure_thesaurus_schema
        except ImportError:
            from thesaurus_db import ensure_thesaurus_schema
        ensure_thesaurus_schema(conn)
        try:
            from continuuuum_api.story_db import ensure_stories_schema
        except ImportError:
            from story_db import ensure_stories_schema
        ensure_stories_schema(conn)
        try:
            from continuuuum_api.lemma_composition import ensure_lemma_composition_schema
        except ImportError:
            from lemma_composition import ensure_lemma_composition_schema
        ensure_lemma_composition_schema(conn)
        try:
            from continuuuum_api.lemma_prompt import ensure_lemma_prompt_schema
        except ImportError:
            from lemma_prompt import ensure_lemma_prompt_schema
        ensure_lemma_prompt_schema(conn)
        try:
            from continuuuum_api.quest_db import ensure_quest_schema
        except ImportError:
            from quest_db import ensure_quest_schema
        ensure_quest_schema(conn)
        try:
            from continuuuum_api.dream_cycle_db import ensure_dream_cycle_schema
        except ImportError:
            from dream_cycle_db import ensure_dream_cycle_schema
        ensure_dream_cycle_schema(conn)
        try:
            from continuuuum_api.mod_db import ensure_mayor_dog_mods_schema
        except ImportError:
            from mod_db import ensure_mayor_dog_mods_schema
        ensure_mayor_dog_mods_schema(conn)
        try:
            from continuuuum_api.audit_db import ensure_audit_schema
        except ImportError:
            from audit_db import ensure_audit_schema
        ensure_audit_schema(conn)
        try:
            from continuuuum_api.draft_review_db import ensure_draft_review_schema
        except ImportError:
            from draft_review_db import ensure_draft_review_schema
        ensure_draft_review_schema(conn)
        _schema_initialized = True
    return conn


@app.route("/api/episodes", methods=["GET"])
def list_episodes():
    """List episodes with optional filters. Query: tenant_id, limit, offset, engine, scene_path."""
    tenant_id = request.args.get("tenant_id")
    limit = int(request.args.get("limit", 100))
    offset = int(request.args.get("offset", 0))
    engine = request.args.get("engine")
    scene_path = request.args.get("scene_path")
    try:
        conn = get_conn()
        where_parts = []
        params = []
        if tenant_id:
            where_parts.append("tenant_id = ?")
            params.append(tenant_id)
        if engine:
            where_parts.append("engine = ?")
            params.append(engine)
        if scene_path:
            where_parts.append("scene_path LIKE ?")
            params.append("%" + scene_path + "%")
        where_sql = " AND ".join(where_parts) if where_parts else "1=1"
        cur = conn.execute(f"SELECT COUNT(*) FROM episodes WHERE {where_sql}", params)
        total = cur.fetchone()[0]
        params.extend([limit, offset])
        cur = conn.execute(
            f"""SELECT id, tenant_id, title, created_at, engine, scene_path, t_start, t_end, plot_description
                FROM episodes WHERE {where_sql}
                ORDER BY created_at DESC LIMIT ? OFFSET ?""",
            params,
        )
        rows = cur.fetchall()
        conn.close()
        items = [
            {
                "id": r["id"],
                "tenantId": r["tenant_id"],
                "title": r["title"],
                "createdAt": r["created_at"],
                "engine": r["engine"],
                "scenePath": r["scene_path"],
                "tStart": r["t_start"],
                "tEnd": r["t_end"],
                "plotDescription": r["plot_description"],
            }
            for r in rows
        ]
        return jsonify({"items": items, "total": total}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episodes", methods=["POST"])
def create_episode():
    """Create episode. Body: title, tenantId, engine, scenePath, tStart, tEnd, plotDescription."""
    body = request.get_json() or {}
    ep_id = body.get("id") or str(uuid.uuid4())
    tenant_id = body.get("tenantId", "default")
    title = body.get("title", "Untitled")
    engine = body.get("engine", "unity")
    scene_path = body.get("scenePath")
    t_start = float(body.get("tStart", 0))
    t_end = float(body.get("tEnd", 3600))
    plot_description = body.get("plotDescription")
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        conn = get_conn()
        conn.execute(
            """INSERT INTO episodes (id, tenant_id, title, created_at, engine, scene_path, t_start, t_end, plot_description)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (ep_id, tenant_id, title, now, engine, scene_path, t_start, t_end, plot_description),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": ep_id}), 201
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episode-script", methods=["POST"])
def create_episode_script():
    """Create episode_script. Body: episodeId, scriptText, language."""
    body = request.get_json() or {}
    episode_id = body.get("episodeId")
    script_text = body.get("scriptText", "")
    language = body.get("language", "en")
    if not episode_id:
        return jsonify({"error": "episodeId required"}), 400
    script_id = body.get("id") or str(uuid.uuid4())
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        conn = get_conn()
        conn.execute(
            """INSERT INTO episode_script (id, episode_id, script_ref, script_text, language, created_at)
               VALUES (?, ?, NULL, ?, ?, ?)""",
            (script_id, episode_id, script_text, language, now),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": script_id, "episodeId": episode_id}), 201
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episode-assets", methods=["POST"])
def create_episode_asset():
    """Link USC asset to episode. Body: episodeId, uscAssetId, assetType, role, causalityLeafId."""
    body = request.get_json() or {}
    episode_id = body.get("episodeId")
    usc_asset_id = body.get("uscAssetId")
    asset_type = body.get("assetType", "document")
    role = body.get("role")
    causality_leaf_id = body.get("causalityLeafId")
    if not episode_id or not usc_asset_id:
        return jsonify({"error": "episodeId and uscAssetId required"}), 400
    asset_id = body.get("id") or str(uuid.uuid4())
    try:
        conn = get_conn()
        conn.execute(
            """INSERT INTO episode_assets (id, episode_id, usc_asset_id, asset_type, role, causality_leaf_id)
               VALUES (?, ?, ?, ?, ?, ?)""",
            (asset_id, episode_id, usc_asset_id, asset_type, role, causality_leaf_id),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": asset_id, "episodeId": episode_id}), 201
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episode-script/<episode_id>", methods=["GET"])
def get_episode_script(episode_id: str):
    """Return episode_script row for episode_id."""
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, episode_id, script_ref, script_text, language, created_at FROM episode_script WHERE episode_id = ? LIMIT 1",
            (episode_id,),
        )
        row = cur.fetchone()
        conn.close()
        if not row:
            return jsonify(None), 200
        return jsonify({
            "id": row["id"],
            "episodeId": row["episode_id"],
            "scriptRef": row["script_ref"],
            "scriptText": row["script_text"],
            "language": row["language"],
            "createdAt": row["created_at"],
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episode-script/<episode_id>", methods=["PUT"])
def put_episode_script(episode_id: str):
    """Create or update episode_script for episode_id. Body: scriptText, language (default en)."""
    body = request.get_json() or {}
    script_text = body.get("scriptText", "")
    language = body.get("language", "en")
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id FROM episode_script WHERE episode_id = ? LIMIT 1",
            (episode_id,),
        )
        row = cur.fetchone()
        if row:
            conn.execute(
                "UPDATE episode_script SET script_text = ?, language = ? WHERE id = ?",
                (script_text, language, row["id"]),
            )
            script_id = row["id"]
        else:
            script_id = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO episode_script (id, episode_id, script_ref, script_text, language, created_at)
                   VALUES (?, ?, NULL, ?, ?, ?)""",
                (script_id, episode_id, script_text, language, now),
            )
        conn.commit()
        conn.close()
        return jsonify({"id": script_id, "episodeId": episode_id}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episode-script/<script_id>/screenplay", methods=["GET"])
def get_screenplay(script_id: str):
    """Return screenplay structure: blocks (scene, action, dialogue, sfx) with text, farey_span, audio_ref for language."""
    language = request.args.get("language", "en")
    try:
        conn = get_conn()
        # todo: review: should the script_text be selected here, or should we omit assuming we want all screenplays for the episode_id, or
        # do we treat this like the localization, and if so, why are we implementing the script draft system?
        # shouldn't we be feeling secure in our commitment of script drafts to the episode_id?
        cur = conn.execute(
            "SELECT id, episode_id, script_text, language FROM episode_script WHERE id = ? LIMIT 1",
            (script_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "episode script not found"}), 404
        episode_id = row["episode_id"]
        script_text = row["script_text"] or ""
        cur = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (language,))
        lang_row = cur.fetchone()
        language_id = lang_row["id"] if lang_row else None
        cur = conn.execute(
            "SELECT * FROM thesaurus_ast_nodes WHERE episode_script_id = ? ORDER BY sort_key ASC",
            (script_id,),
        )
        nodes = [dict(r) for r in cur.fetchall()]
        audio_by_farey = {}
        if language_id:
            try:
                cur = conn.execute(
                    """SELECT farey_left_num, farey_left_den, farey_right_num, farey_right_den, kind, audio_ref
                       FROM script_audio_by_language WHERE episode_script_id = ? AND language_id = ?
                       ORDER BY farey_left_num, farey_left_den""",
                    (script_id, language_id),
                )
                for r in cur.fetchall():
                    key = (r["farey_left_num"], r["farey_left_den"], r["farey_right_num"], r["farey_right_den"])
                    audio_by_farey[key] = {"kind": r["kind"], "audioRef": r["audio_ref"]}
            except sqlite3.OperationalError:
                pass
        conn.close()
        root_nodes = [n for n in nodes if n["parent_id"] is None]
        blocks = []
        for n in root_nodes:
            ln, ld = n["farey_left_num"], n["farey_left_den"] #lane, load
            rn, rd = n["farey_right_num"], n["farey_right_den"] #rain road
            farey_span = {"leftNum": ln, "leftDen": ld, "rightNum": rn, "rightDen": rd}
            audio = audio_by_farey.get((ln, ld, rn, rd))
            text = (n.get("token_or_phrase") or "").strip()
            node_kind = n.get("node_kind") if "node_kind" in n else "token"
            if node_kind == "quote":
                children = [c for c in nodes if c.get("parent_id") == n["id"]]
                text = " ".join((c.get("token_or_phrase") or "").strip() for c in sorted(children, key=lambda x: x.get("sort_key", 0)))
                blocks.append({"type": "dialogue", "text": text, "fareySpan": farey_span, "audioRef": audio["audioRef"] if audio and audio["kind"] == "speech" else None, "languageId": language_id})
            elif audio and audio["kind"] == "sfx":
                blocks.append({"type": "sfx", "text": text or None, "fareySpan": farey_span, "audioRef": audio["audioRef"], "languageId": language_id})
            elif text:
                blocks.append({"type": "action", "text": text, "fareySpan": farey_span, "audioRef": None, "languageId": language_id})
        episode_title = ""
        try:
            conn = get_conn()
            cur = conn.execute("SELECT title FROM episodes WHERE id = ? LIMIT 1", (episode_id,))
            ep = cur.fetchone()
            if ep:
                episode_title = ep["title"] or ""
            conn.close()
        except Exception:
            pass
        return jsonify({
            "episodeScriptId": script_id,
            "episodeId": episode_id,
            "title": episode_title,
            "language": language,
            "blocks": blocks,
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/episodes/<episode_id>/extract-screenplay-work-orders", methods=["POST"])
def extract_screenplay_work_orders(episode_id: str):
    """Extract work orders from screenplay (dialogue and SFX). Body: optional episodeScriptId."""
    body = request.get_json() or {}
    episode_script_id = body.get("episodeScriptId")
    try:
        conn = get_conn()
        created = screenplay_wo.extract_work_orders_from_screenplay(conn, episode_id, episode_script_id)
        conn.close()
        return jsonify({"ok": True, "count": len(created), "workOrderIds": [c["id"] for c in created]}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/alternatives", methods=["GET"])
def get_thesaurus_alternatives():
    """Return token detail: lemma, POS, alternatives for token and language."""
    token = request.args.get("token", "")
    language = request.args.get("language", "en")
    if not token:
        return jsonify({"error": "token required"}), 400
    try:
        conn = get_conn()
        # Resolve language_id from code
        cur = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (language,))
        lang_row = cur.fetchone()
        if not lang_row:
            conn.close()
            return jsonify({"token": token, "language": language}), 200
        language_id = lang_row["id"]
        cur = conn.execute(
            "SELECT id, term, pos_tag FROM thesaurus_entries WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1",
            (language_id, token.lower(), token),
        )
        entry = cur.fetchone()
        if not entry:
            conn.close()
            return jsonify({"token": token, "language": language}), 200
        cur = conn.execute(
            "SELECT pos_tag, form, role FROM thesaurus_alternatives WHERE entry_id = ?",
            (entry["id"],),
        )
        alternatives = [{"form": r["form"], "role": r["role"]} for r in cur.fetchall()]
        conn.close()
        return jsonify({
            "token": token,
            "lemma": entry["term"],
            "posTag": entry["pos_tag"],
            "language": language,
            "alternatives": alternatives,
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/ast-nodes", methods=["GET"])
def get_ast_nodes():
    """Return AST nodes for episode_script, optionally filtered by parent. Ordered by Farey (sort_key)."""
    episode_script_id = request.args.get("episodeScriptId")
    parent_id = request.args.get("parentId")  # null/omit = root (parent_id IS NULL)
    if not episode_script_id:
        return jsonify({"error": "episodeScriptId required"}), 400
    try:
        conn = get_conn()
        if parent_id:
            cur = conn.execute(
                """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                          token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                   FROM thesaurus_ast_nodes
                   WHERE episode_script_id = ? AND parent_id = ?
                   ORDER BY sort_key ASC""",
                (episode_script_id, parent_id),
            )
        else:
            cur = conn.execute(
                """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                          token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                   FROM thesaurus_ast_nodes
                   WHERE episode_script_id = ? AND parent_id IS NULL
                   ORDER BY sort_key ASC""",
                (episode_script_id,),
            )
        rows = cur.fetchall()
        conn.close()
        nodes = [
            {
                "id": r["id"],
                "parentId": r["parent_id"],
                "fareyLeftNum": r["farey_left_num"],
                "fareyLeftDen": r["farey_left_den"],
                "fareyRightNum": r["farey_right_num"],
                "fareyRightDen": r["farey_right_den"],
                "tokenOrPhrase": r["token_or_phrase"],
                "posTag": r["pos_tag"],
                "languageId": r["language_id"],
                "episodeScriptId": r["episode_script_id"],
                "sortKey": r["sort_key"],
            }
            for r in rows
        ]
        return jsonify(nodes), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/ast-nodes/<node_id>", methods=["PATCH"])
def update_ast_node(node_id: str):
    """Update AST node: tokenOrPhrase, posTag, or reorder (reorderAfter / newIndex)."""
    body = request.get_json() or {}
    try:
        conn = get_conn()
        if "tokenOrPhrase" in body:
            conn.execute(
                "UPDATE thesaurus_ast_nodes SET token_or_phrase = ? WHERE id = ?",
                (body["tokenOrPhrase"], node_id),
            )
        if "posTag" in body:
            conn.execute(
                "UPDATE thesaurus_ast_nodes SET pos_tag = ? WHERE id = ?",
                (body["posTag"], node_id),
            )
        # Reorder: reorderAfter (sibling id) or newIndex (0-based among siblings)
        reorder_after = body.get("reorderAfter")
        new_index = body.get("newIndex")
        if reorder_after is not None or new_index is not None:
            cur = conn.execute(
                "SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den, "
                "token_or_phrase, pos_tag, language_id, episode_script_id, sort_key FROM thesaurus_ast_nodes WHERE id = ?",
                (node_id,),
            )
            node_row = cur.fetchone()
            if not node_row:
                conn.close()
                return jsonify({"error": "node not found"}), 404
            parent_id = node_row["parent_id"]
            episode_script_id = node_row["episode_script_id"]
            if parent_id:
                cur = conn.execute(
                    """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                              token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                       FROM thesaurus_ast_nodes
                       WHERE episode_script_id = ? AND parent_id = ?
                       ORDER BY sort_key ASC""",
                    (episode_script_id, parent_id),
                )
            else:
                cur = conn.execute(
                    """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                              token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                       FROM thesaurus_ast_nodes
                       WHERE episode_script_id = ? AND parent_id IS NULL
                       ORDER BY sort_key ASC""",
                    (episode_script_id,),
                )
            siblings = [dict(r) for r in cur.fetchall()]
            if not siblings:
                conn.close()
                return jsonify({"ok": True}), 200
            ids = [s["id"] for s in siblings]
            if node_id not in ids:
                conn.close()
                return jsonify({"error": "node not in siblings"}), 400
            current_idx = ids.index(node_id)
            if new_index is not None:
                target_idx = max(0, min(int(new_index), len(siblings) - 1))
            else:
                if reorder_after not in ids:
                    conn.close()
                    return jsonify({"error": "reorderAfter node not found"}), 400
                target_idx = ids.index(reorder_after)  # place node_id after reorder_after
                target_idx = min(target_idx + 1, len(siblings) - 1)
            if current_idx == target_idx:
                conn.close()
                return jsonify({"ok": True}), 200
            # Remove node from list, insert at target_idx
            node = siblings.pop(current_idx)
            siblings.insert(target_idx, node)
            parent_interval = None
            if parent_id:
                cur = conn.execute(
                    "SELECT farey_left_num, farey_left_den, farey_right_num, farey_right_den FROM thesaurus_ast_nodes WHERE id = ?",
                    (parent_id,),
                )
                par = cur.fetchone()
                if par:
                    parent_interval = farey_ast.FareyInterval(
                        par["farey_left_num"], par["farey_left_den"],
                        par["farey_right_num"], par["farey_right_den"],
                    )
            rebalanced = farey_ast.rebalance_intervals(siblings, parent_interval)
            for n in rebalanced:
                conn.execute(
                    """UPDATE thesaurus_ast_nodes SET farey_left_num=?, farey_left_den=?, farey_right_num=?, farey_right_den=?, sort_key=?
                       WHERE id=?""",
                    (n["farey_left_num"], n["farey_left_den"], n["farey_right_num"], n["farey_right_den"], n["sort_key"], n["id"]),
                )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/rebalance", methods=["POST"])
def rebalance_ast():
    """Rebalance Farey intervals for episode_script_id (optional parentId)."""
    body = request.get_json() or {}
    episode_script_id = body.get("episodeScriptId")
    parent_id = body.get("parentId")  # optional; None = root
    if not episode_script_id:
        return jsonify({"error": "episodeScriptId required"}), 400
    try:
        conn = get_conn()
        if parent_id:
            cur = conn.execute(
                """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                          token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                   FROM thesaurus_ast_nodes
                   WHERE episode_script_id = ? AND parent_id = ?
                   ORDER BY sort_key ASC""",
                (episode_script_id, parent_id),
            )
            parent_row = conn.execute(
                "SELECT farey_left_num, farey_left_den, farey_right_num, farey_right_den FROM thesaurus_ast_nodes WHERE id = ?",
                (parent_id,),
            ).fetchone()
            parent_interval = None
            if parent_row:
                parent_interval = farey_ast.FareyInterval(
                    parent_row["farey_left_num"], parent_row["farey_left_den"],
                    parent_row["farey_right_num"], parent_row["farey_right_den"],
                )
        else:
            cur = conn.execute(
                """SELECT id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                          token_or_phrase, pos_tag, language_id, episode_script_id, sort_key
                   FROM thesaurus_ast_nodes
                   WHERE episode_script_id = ? AND parent_id IS NULL
                   ORDER BY sort_key ASC""",
                (episode_script_id,),
            )
            parent_interval = None
        siblings = [dict(r) for r in cur.fetchall()]
        conn.close()
        if not siblings:
            return jsonify({"ok": True}), 200
        rebalanced = farey_ast.rebalance_intervals(siblings, parent_interval)
        conn = get_conn()
        for n in rebalanced:
            conn.execute(
                """UPDATE thesaurus_ast_nodes SET farey_left_num=?, farey_left_den=?, farey_right_num=?, farey_right_den=?, sort_key=?
                   WHERE id=?""",
                (n["farey_left_num"], n["farey_left_den"], n["farey_right_num"], n["farey_right_den"], n["sort_key"], n["id"]),
            )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/build-ast", methods=["POST"])
def build_ast():
    """Build AST from script text; detect quote blocks. Body: episodeScriptId, scriptText?, languageId?."""
    body = request.get_json() or {}
    episode_script_id = body.get("episodeScriptId")
    if not episode_script_id:
        return jsonify({"error": "episodeScriptId required"}), 400
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, script_text, language FROM episode_script WHERE id = ? LIMIT 1",
            (episode_script_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "episode script not found"}), 404
        script_text = body.get("scriptText") or row["script_text"] or ""
        lang_code = (row["language"] or body.get("languageId") or "en").strip()
        cur = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (lang_code,))
        lang_row = cur.fetchone()
        language_id = lang_row["id"] if lang_row else None
        if not language_id:
            conn.close()
            return jsonify({"error": "language not found for script"}), 400
        node_ids = script_to_ast.build_ast_from_script(conn, episode_script_id, script_text, language_id)
        conn.close()
        return jsonify({"ok": True, "nodeCount": len(node_ids), "nodeIds": node_ids}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/change-of-basis", methods=["POST"])
def change_of_basis():
    """Translate script to target language using thesaurus_translations, rules, and word overrides."""
    body = request.get_json() or {}
    episode_script_id = body.get("episodeScriptId")
    target_language = body.get("targetLanguage", "")
    if not episode_script_id or not target_language:
        return jsonify({"error": "episodeScriptId and targetLanguage required"}), 400
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, episode_id, script_text, language FROM episode_script WHERE id = ? LIMIT 1",
            (episode_script_id,),
        )
        row = cur.fetchone()
        if not row or not row["script_text"]:
            conn.close()
            return jsonify({"scriptText": ""}), 200
        script_text = row["script_text"]
        source_lang_code = (row["language"] or "en").strip()
        # Resolve source and target language ids
        cur = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (source_lang_code,))
        source_lang_row = cur.fetchone()
        source_language_id = source_lang_row["id"] if source_lang_row else None
        cur = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (target_language.strip(),))
        target_lang_row = cur.fetchone()
        if not target_lang_row:
            conn.close()
            return jsonify({"error": "target language not found"}), 400
        target_language_id = target_lang_row["id"]
        # Word overrides for target language (term, context_type -> target_form; None = leave as-is)
        overrides = {}
        try:
            cur = conn.execute(
                "SELECT term, context_type, target_form FROM change_of_basis_word_overrides WHERE target_language_id = ?",
                (target_language_id,),
            )
            overrides = {(r["term"].lower(), r["context_type"]): r["target_form"] for r in cur.fetchall()}
        except sqlite3.OperationalError:
            pass
        # Tokenize (whitespace)
        tokens = script_text.split()
        if not tokens:
            conn.close()
            return jsonify({"scriptText": ""}), 200
        # Resolve each token: override (default/place/person) else thesaurus_translations else leave token
        result_parts = []
        for i, token in enumerate(tokens):
            used_override = False
            for ctx in ("default", "place", "person"):
                if (token.lower(), ctx) in overrides:
                    val = overrides[(token.lower(), ctx)]
                    result_parts.append(token if val is None else val)
                    used_override = True
                    break
            if used_override:
                continue
            entry_id = None
            if source_language_id:
                cur = conn.execute(
                    "SELECT id FROM thesaurus_entries WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1",
                    (source_language_id, token.lower(), token),
                )
                entry = cur.fetchone()
                if entry:
                    entry_id = entry["id"]
            if entry_id:
                cur = conn.execute(
                    "SELECT form FROM thesaurus_translations WHERE entry_id = ? AND language_id = ? LIMIT 1",
                    (entry_id, target_language_id),
                )
                tr = cur.fetchone()
                if tr:
                    result_parts.append(tr["form"])
                    continue
            result_parts.append(token)
        script_text_out = " ".join(result_parts)
        # Update script_audio_by_language for target language (so translation context has audio refs per clause)
        try:
            conn.execute(
                "DELETE FROM script_audio_by_language WHERE episode_script_id = ? AND language_id = ?",
                (episode_script_id, target_language_id),
            )
            cur = conn.execute(
                """SELECT id, episode_script_id, language_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den, audio_ref
                   FROM script_speech_audio WHERE episode_script_id = ?""",
                (episode_script_id,),
            )
            for r in cur.fetchall():
                conn.execute(
                    """INSERT INTO script_audio_by_language
                       (id, episode_script_id, language_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den, kind, audio_ref, source_speech_id)
                       VALUES (?, ?, ?, ?, ?, ?, ?, 'speech', ?, ?)""",
                    (str(uuid.uuid4()), r["episode_script_id"], target_language_id,
                     r["farey_left_num"], r["farey_left_den"], r["farey_right_num"], r["farey_right_den"],
                     r["audio_ref"], r["id"]),
                )   #todo: review: should we be using the source_speech_id or the id?
            cur = conn.execute(
                """SELECT id, episode_script_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den, audio_ref
                   FROM script_sound_effects WHERE episode_script_id = ?""",
                (episode_script_id,),
            )
            for r in cur.fetchall():
                conn.execute(
                    """INSERT INTO script_audio_by_language
                       (id, episode_script_id, language_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den, kind, audio_ref, source_sfx_id)
                       VALUES (?, ?, ?, ?, ?, ?, ?, 'sfx', ?, ?)""",
                    (str(uuid.uuid4()), r["episode_script_id"], target_language_id,
                     r["farey_left_num"], r["farey_left_den"], r["farey_right_num"], r["farey_right_den"],
                     r["audio_ref"], r["id"]),
                )
            conn.commit()
        except sqlite3.OperationalError:
            pass
        conn.close()
        return jsonify({"scriptText": script_text_out}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# --- Draft API ---
@app.route("/api/drafts/episodes", methods=["GET"])
def list_drafts():
    """List draft episodes. Query: user (created_by), episodeId."""
    user = request.args.get("user")
    episode_id = request.args.get("episodeId")
    try:
        conn = get_conn()
        where_parts = []
        params = []
        if user:
            where_parts.append("created_by = ?")
            params.append(user)
        if episode_id:
            where_parts.append("episode_id = ?")
            params.append(episode_id)
        where_sql = " AND ".join(where_parts) if where_parts else "1=1"
        cur = conn.execute(
            f"""SELECT id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end,
                       plot_description, created_at, updated_at, created_by, committed_at
                FROM draft_episodes WHERE {where_sql}
                ORDER BY updated_at DESC""",
            params,
        )
        rows = cur.fetchall()
        conn.close()
        items = [
            {
                "id": r["id"],
                "episodeId": r["episode_id"],
                "tenantId": r["tenant_id"],
                "title": r["title"],
                "engine": r["engine"],
                "scenePath": r["scene_path"],
                "tStart": r["t_start"],
                "tEnd": r["t_end"],
                "plotDescription": r["plot_description"],
                "createdAt": r["created_at"],
                "updatedAt": r["updated_at"],
                "createdBy": r["created_by"],
                "committedAt": r["committed_at"],
            }
            for r in rows
        ]
        return jsonify(items), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_draft_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes", methods=["POST"])
def create_draft():
    """Create draft from episode or blank. Body: episodeId (optional), title, tenantId, engine, scenePath, tStart, tEnd, plotDescription, createdBy."""
    body = request.get_json() or {}
    episode_id = body.get("episodeId")
    created_by = body.get("createdBy", request.headers.get("X-User-ID", "anonymous"))
    draft_id = str(uuid.uuid4())
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        conn = get_conn()
        if episode_id:
            cur = conn.execute(
                "SELECT tenant_id, title, engine, scene_path, t_start, t_end, plot_description FROM episodes WHERE id = ?",
                (episode_id,),
            )
            ep = cur.fetchone()
            if not ep:
                conn.close()
                return jsonify({"error": "episode not found"}), 404
            tenant_id, title, engine, scene_path, t_start, t_end, plot_description = (
                ep["tenant_id"], ep["title"], ep["engine"], ep["scene_path"],
                ep["t_start"], ep["t_end"], ep["plot_description"],
            )
        else:
            tenant_id = body.get("tenantId", "default")
            title = body.get("title", "Untitled Draft")
            engine = body.get("engine", "unity")
            scene_path = body.get("scenePath")
            t_start = float(body.get("tStart", 0))
            t_end = float(body.get("tEnd", 3600))
            plot_description = body.get("plotDescription")
        conn.execute(
            """INSERT INTO draft_episodes (id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end, plot_description, created_at, updated_at, created_by)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (draft_id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end, plot_description, now, now, created_by),
        )
        script_seeded = False
        if episode_id:
            cur = conn.execute("SELECT id, script_text, language FROM episode_script WHERE episode_id = ? LIMIT 1", (episode_id,))
            es = cur.fetchone()
            if es:
                script_id = str(uuid.uuid4())
                conn.execute(
                    """INSERT INTO draft_episode_script (id, draft_episode_id, episode_script_id, script_text, language, created_at, updated_at)
                       VALUES (?, ?, ?, ?, ?, ?, ?)""",
                    (script_id, draft_id, es["id"], es["script_text"], es["language"] or "en", now, now),
                )
                script_seeded = True
        if not script_seeded:
            script_id = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO draft_episode_script (id, draft_episode_id, episode_script_id, script_text, language, created_at, updated_at)
                   VALUES (?, ?, NULL, ?, ?, ?, ?)""",
                (script_id, draft_id, "", "en", now, now),
            )
        conn.commit()
        conn.close()
        return jsonify({"id": draft_id, "episodeId": episode_id}), 201
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_draft_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes/<draft_id>", methods=["GET"])
def get_draft(draft_id: str):
    """Get draft episode with scripts."""
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end, plot_description, created_at, updated_at, created_by, committed_at FROM draft_episodes WHERE id = ?",
            (draft_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "draft not found"}), 404
        draft = {
            "id": row["id"],
            "episodeId": row["episode_id"],
            "tenantId": row["tenant_id"],
            "title": row["title"],
            "engine": row["engine"],
            "scenePath": row["scene_path"],
            "tStart": row["t_start"],
            "tEnd": row["t_end"],
            "plotDescription": row["plot_description"],
            "createdAt": row["created_at"],
            "updatedAt": row["updated_at"],
            "createdBy": row["created_by"],
            "committedAt": row["committed_at"],
        }
        cur = conn.execute(
            "SELECT id, episode_script_id, script_text, language, min_thesaurus_version, created_at, updated_at FROM draft_episode_script WHERE draft_episode_id = ?",
            (draft_id,),
        )
        scripts = [
            {
                "id": r["id"],
                "episodeScriptId": r["episode_script_id"],
                "scriptText": r["script_text"],
                "language": r["language"],
                "minThesaurusVersion": r["min_thesaurus_version"],
                "createdAt": r["created_at"],
                "updatedAt": r["updated_at"],
            }
            for r in cur.fetchall()
        ]
        conn.close()
        draft["scripts"] = scripts
        return jsonify(draft), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e)}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes/<draft_id>", methods=["PATCH"])
def update_draft(draft_id: str):
    """Update draft episode fields."""
    body = request.get_json() or {}
    updates = []
    params = []
    for key, col in [
        ("title", "title"),
        ("tenantId", "tenant_id"),
        ("engine", "engine"),
        ("scenePath", "scene_path"),
        ("tStart", "t_start"),
        ("tEnd", "t_end"),
        ("plotDescription", "plot_description"),
    ]:
        if key in body:
            updates.append(f"{col} = ?")
            params.append(body[key])
    if not updates:
        return jsonify({"ok": True}), 200
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    updates.append("updated_at = ?")
    params.append(now)
    params.append(draft_id)
    try:
        conn = get_conn()
        conn.execute(f"UPDATE draft_episodes SET {', '.join(updates)} WHERE id = ?", params)
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes/<draft_id>/publish", methods=["POST"])
def publish_draft(draft_id: str):
    """Copy draft to episodes and episode_script."""
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end, plot_description FROM draft_episodes WHERE id = ?",
            (draft_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "draft not found"}), 404
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        ep_id = row["episode_id"] or str(uuid.uuid4())
        if not row["episode_id"]:
            conn.execute(
                """INSERT INTO episodes (id, tenant_id, title, created_at, engine, scene_path, t_start, t_end, plot_description)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (ep_id, row["tenant_id"], row["title"], now, row["engine"], row["scene_path"], row["t_start"], row["t_end"], row["plot_description"]),
            )
        else:
            conn.execute(
                "UPDATE episodes SET tenant_id=?, title=?, engine=?, scene_path=?, t_start=?, t_end=?, plot_description=? WHERE id=?",
                (row["tenant_id"], row["title"], row["engine"], row["scene_path"], row["t_start"], row["t_end"], row["plot_description"], ep_id),
            )
        cur = conn.execute("SELECT id, script_text, language, min_thesaurus_version FROM draft_episode_script WHERE draft_episode_id = ?", (draft_id,))
        for r in cur.fetchall():
            script_id = r["episode_script_id"] or str(uuid.uuid4())
            if r["episode_script_id"]:
                conn.execute(
                    "UPDATE episode_script SET script_text=?, language=?, min_thesaurus_version=? WHERE id=?",
                    (r["script_text"], r["language"], r["min_thesaurus_version"], script_id),
                )
            else:
                conn.execute(
                    "INSERT INTO episode_script (id, episode_id, script_ref, script_text, language, min_thesaurus_version, created_at) VALUES (?, ?, NULL, ?, ?, ?, ?)",
                    (script_id, ep_id, r["script_text"], r["language"], r["min_thesaurus_version"], now),
                )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "episodeId": ep_id}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def _ensure_draft_episode_script(conn, draft_id: str, language: str | None = None):
    """Return draft_episode_script row, seeding from episode script or an empty row when missing."""
    draft = conn.execute(
        "SELECT id, episode_id FROM draft_episodes WHERE id = ?",
        (draft_id,),
    ).fetchone()
    if not draft:
        return None

    def _fetch_row():
        if language:
            return conn.execute(
                "SELECT id, script_text, language, min_thesaurus_version FROM draft_episode_script WHERE draft_episode_id = ? AND language = ?",
                (draft_id, language),
            ).fetchone()
        return conn.execute(
            "SELECT id, script_text, language, min_thesaurus_version FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY created_at LIMIT 1",
            (draft_id,),
        ).fetchone()

    row = _fetch_row()
    if row:
        return row

    script_text = ""
    episode_script_id = None
    min_thesaurus_version = None
    lang = language or "en"
    if draft["episode_id"]:
        ep_script = conn.execute(
            """SELECT id, script_text, language, min_thesaurus_version
               FROM episode_script WHERE episode_id = ? ORDER BY created_at LIMIT 1""",
            (draft["episode_id"],),
        ).fetchone()
        if ep_script:
            script_text = ep_script["script_text"] or ""
            episode_script_id = ep_script["id"]
            min_thesaurus_version = ep_script["min_thesaurus_version"]
            lang = language or ep_script["language"] or "en"

    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    script_id = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO draft_episode_script
           (id, draft_episode_id, episode_script_id, script_text, language, min_thesaurus_version, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
        (script_id, draft_id, episode_script_id, script_text, lang, min_thesaurus_version, now, now),
    )
    conn.commit()
    return _fetch_row()


@app.route("/api/drafts/episodes/<draft_id>/script", methods=["GET"])
def get_draft_script(draft_id: str):
    """Get draft script text. Query: language (optional, default first script)."""
    language = request.args.get("language")
    try:
        conn = get_conn()
        row = _ensure_draft_episode_script(conn, draft_id, language)
        conn.close()
        if not row:
            return jsonify({"error": "draft not found"}), 404
        return jsonify({
            "id": row["id"],
            "scriptText": row["script_text"] or "",
            "language": row["language"],
            "minThesaurusVersion": row["min_thesaurus_version"],
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes/<draft_id>/script", methods=["PUT"])
def put_draft_script(draft_id: str):
    """Set draft script text. Body: scriptText, language (default en)."""
    body = request.get_json() or {}
    script_text = body.get("scriptText", "")
    language = body.get("language", "en")
    min_thesaurus_version = body.get("minThesaurusVersion")
    try:
        conn = get_conn()
        try:
            from continuuuum_api.localization_helpers import draft_blocks_author_edit, require_draft_author
        except ImportError:
            from localization_helpers import draft_blocks_author_edit, require_draft_author
        auth_err = require_draft_author(conn, draft_id, request.headers.get("X-User-ID", "anonymous"))
        if auth_err:
            conn.close()
            return jsonify({"error": auth_err}), 403
        blocked = draft_blocks_author_edit(conn, draft_id)
        if blocked:
            conn.close()
            return jsonify({"error": f"draft change list is {blocked}; withdraw before editing"}), 409
        cur = conn.execute(
            "SELECT id FROM draft_episode_script WHERE draft_episode_id = ? AND language = ?",
            (draft_id, language),
        )
        row = cur.fetchone()
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        if row:
            params = [script_text, min_thesaurus_version, now, row["id"]]
            conn.execute(
                "UPDATE draft_episode_script SET script_text=?, min_thesaurus_version=?, updated_at=? WHERE id=?",
                params,
            )
        else:
            script_id = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO draft_episode_script (id, draft_episode_id, episode_script_id, script_text, language, min_thesaurus_version, created_at, updated_at)
                   VALUES (?, ?, NULL, ?, ?, ?, ?, ?)""",
                (script_id, draft_id, script_text, language, min_thesaurus_version, now, now),
            )
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def _create_notification(conn, user_id: str, ntype: str, message: str, draft_id=None, review_id=None):
    """Insert a notification. Caller must commit."""
    nid = str(uuid.uuid4())
    now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    conn.execute(
        "INSERT INTO notifications (id, user_id, type, draft_id, review_id, message, created_at) VALUES (?, ?, ?, ?, ?, ?, ?)",
        (nid, user_id, ntype, draft_id, review_id, message, now),
    )
    return nid


try:
    from continuuuum_api.dialogue_routes import register_dialogue_routes
except ImportError:
    from dialogue_routes import register_dialogue_routes

register_localization_routes(app, get_conn, _get_current_user, _create_notification, _is_admin)
register_script_output_routes(app, get_conn, _get_current_user)
register_lemma_routes(app, get_conn)
register_dialogue_routes(app, get_conn, _get_current_user)
try:
    from continuuuum_api.quest_routes import register_quest_routes
except ImportError:
    from quest_routes import register_quest_routes
register_quest_routes(app, get_conn, _get_current_user)
try:
    from continuuuum_api.dream_cycle_routes import register_dream_cycle_routes
except ImportError:
    from dream_cycle_routes import register_dream_cycle_routes
register_dream_cycle_routes(app, get_conn, _get_current_user)
register_telecom_routes(app, get_conn)
register_society_routes(app, get_conn)
register_galactic_routes(app, get_conn)
register_camera_routes(app, get_conn, _get_current_user)

_socketio_cors = list(DEV_CORS_ORIGINS) + [
    "http://127.0.0.1:5050",
    "http://localhost:5050",
]
socketio = SocketIO(app, cors_allowed_origins=_socketio_cors) if SocketIO else None
register_table_read_routes(app, get_conn, _get_current_user, socketio, LIBRARY_APP_BASE)
register_cave_routes(app, get_conn, _get_current_user)
register_resaurce_routes(app, get_conn)
register_saurce_routes(app, get_conn, _get_current_user)
register_drawer_game_routes(app, get_conn)
register_library_routes(app)
register_sql_viewer_routes(app, get_conn, _get_current_user, get_db_path)
register_story_routes(app, get_conn)
register_chat_routes(app, get_conn)
register_production_proxy_routes(app, get_conn)
register_calendar_routes(app, get_conn)
register_agile_ui_routes(app)
register_mod_routes(app, get_conn, _get_current_user)


def _is_approved_to_commit(conn, user_id: str) -> bool:
    """True if user is in approved_user or is admin."""
    if _is_admin():
        return True
    cur = conn.execute("SELECT 1 FROM approved_user WHERE user_id = ?", (user_id,))
    return cur.fetchone() is not None


# --- Review API ---
@app.route("/api/reviews", methods=["GET"])
def list_reviews():
    """List reviews for current user. Query: asReviewer, asReviewee, status."""
    as_reviewer = request.args.get("asReviewer", "true").lower() in ("1", "true", "yes")
    as_reviewee = request.args.get("asReviewee", "false").lower() in ("1", "true", "yes")
    status = request.args.get("status")
    user_id = _get_current_user()
    try:
        conn = get_conn()
        where_parts = []
        params = []
        if as_reviewer and not as_reviewee:
            where_parts.append("r.reviewer_user_id = ?")
            params.append(user_id)
        elif as_reviewee and not as_reviewer:
            where_parts.append("r.reviewee_user_id = ?")
            params.append(user_id)
        elif as_reviewer and as_reviewee:
            where_parts.append("(r.reviewer_user_id = ? OR r.reviewee_user_id = ? OR d.created_by = ?)")
            params.extend([user_id, user_id, user_id])
        else:
            conn.close()
            return jsonify({"items": [], "total": 0}), 200
        if status:
            where_parts.append("r.status = ?")
            params.append(status)
        where_sql = " AND ".join(where_parts)
        cur = conn.execute(
            f"""SELECT r.id, r.draft_episode_id, r.reviewer_user_id, r.reviewee_user_id, r.status, r.created_at, r.updated_at,
                       d.title, d.committed_at
                FROM reviewer r
                JOIN draft_episodes d ON d.id = r.draft_episode_id
                WHERE {where_sql}
                ORDER BY r.updated_at DESC""",
            params,
        )
        rows = cur.fetchall()
        items = [
            {
                "id": r["id"],
                "draftEpisodeId": r["draft_episode_id"],
                "reviewerUserId": r["reviewer_user_id"],
                "revieweeUserId": r["reviewee_user_id"],
                "status": r["status"],
                "createdAt": r["created_at"],
                "updatedAt": r["updated_at"],
                "draftTitle": r["title"],
                "committedAt": r["committed_at"],
            }
            for r in rows
        ]
        try:
            from continuuuum_api.localization_helpers import list_submitted_change_lists_for_user
        except ImportError:
            from localization_helpers import list_submitted_change_lists_for_user
        seen_drafts = {item["draftEpisodeId"] for item in items}
        for cl in list_submitted_change_lists_for_user(conn, user_id):
            draft_id = cl["draft_episode_id"]
            if draft_id in seen_drafts:
                for item in items:
                    if item["draftEpisodeId"] == draft_id:
                        item["changeListStatus"] = cl["workflow_status"]
                        item["changeListId"] = cl["change_list_id"]
                continue
            items.append(
                {
                    "id": None,
                    "draftEpisodeId": draft_id,
                    "reviewerUserId": None,
                    "revieweeUserId": cl.get("created_by"),
                    "status": "pending_review",
                    "createdAt": cl.get("submitted_at") or cl.get("updated_at"),
                    "updatedAt": cl.get("updated_at"),
                    "draftTitle": cl.get("title"),
                    "committedAt": cl.get("committed_at"),
                    "changeListStatus": cl["workflow_status"],
                    "changeListId": cl["change_list_id"],
                }
            )
            seen_drafts.add(draft_id)
        conn.close()
        return jsonify({"items": items, "total": len(items)}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews", methods=["POST"])
def create_review():
    """Assign reviewer to draft. Body: draftId, reviewerUserId."""
    body = request.get_json() or {}
    draft_id = body.get("draftId")
    reviewer_user_id = body.get("reviewerUserId")
    if not draft_id or not reviewer_user_id:
        return jsonify({"error": "draftId and reviewerUserId required"}), 400
    reviewee = body.get("revieweeUserId") or _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, title, created_by FROM draft_episodes WHERE id = ?",
            (draft_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "draft not found"}), 404
        reviewee = row["created_by"] or reviewee
        draft_title = row["title"] or draft_id
        cur = conn.execute(
            "SELECT id FROM reviewer WHERE draft_episode_id = ? AND reviewer_user_id = ?",
            (draft_id, reviewer_user_id),
        )
        existing = cur.fetchone()
        if existing:
            conn.close()
            return jsonify({"id": existing["id"], "alreadyAssigned": True}), 200
        rid = str(uuid.uuid4())
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        conn.execute(
            "INSERT INTO reviewer (id, draft_episode_id, reviewer_user_id, reviewee_user_id, status, created_at, updated_at) VALUES (?, ?, ?, ?, 'pending', ?, ?)",
            (rid, draft_id, reviewer_user_id, reviewee, now, now),
        )
        _create_notification(
            conn, reviewer_user_id, "review_request",
            f'Draft "{draft_title}" assigned for review',
            draft_id=draft_id, review_id=rid,
        )
        conn.commit()
        conn.close()
        return jsonify({"id": rid}), 201
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews/comment-delete-requests", methods=["GET"])
def list_comment_delete_requests():
    """Pending reviewer comment delete requests. Admin sees all; others see reviews they participate in."""
    user_id = _get_current_user()
    admin = _is_admin()
    try:
        conn = get_conn()
        if admin:
            cur = conn.execute(
                """SELECT c.id AS comment_id, c.reviewer_id AS review_id, c.comment_text,
                          c.delete_requested_at, c.delete_requested_by, c.text_selection_start, c.text_selection_end,
                          r.draft_episode_id, r.reviewer_user_id, r.reviewee_user_id, r.status AS review_status,
                          d.title AS draft_title
                   FROM reviewer_comments c
                   JOIN reviewer r ON r.id = c.reviewer_id
                   JOIN draft_episodes d ON d.id = r.draft_episode_id
                   WHERE c.delete_requested_at IS NOT NULL
                   ORDER BY c.delete_requested_at ASC""",
            )
        else:
            cur = conn.execute(
                """SELECT c.id AS comment_id, c.reviewer_id AS review_id, c.comment_text,
                          c.delete_requested_at, c.delete_requested_by, c.text_selection_start, c.text_selection_end,
                          r.draft_episode_id, r.reviewer_user_id, r.reviewee_user_id, r.status AS review_status,
                          d.title AS draft_title
                   FROM reviewer_comments c
                   JOIN reviewer r ON r.id = c.reviewer_id
                   JOIN draft_episodes d ON d.id = r.draft_episode_id
                   WHERE c.delete_requested_at IS NOT NULL
                     AND (r.reviewer_user_id = ? OR r.reviewee_user_id = ?)
                   ORDER BY c.delete_requested_at ASC""",
                (user_id, user_id),
            )
        rows = cur.fetchall()
        conn.close()
        items = [
            {
                "commentId": r["comment_id"],
                "reviewId": r["review_id"],
                "commentText": r["comment_text"],
                "deleteRequestedAt": r["delete_requested_at"],
                "deleteRequestedBy": r["delete_requested_by"],
                "textSelectionStart": r["text_selection_start"],
                "textSelectionEnd": r["text_selection_end"],
                "draftEpisodeId": r["draft_episode_id"],
                "draftTitle": r["draft_title"],
                "reviewerUserId": r["reviewer_user_id"],
                "revieweeUserId": r["reviewee_user_id"],
                "reviewStatus": r["review_status"],
            }
            for r in rows
        ]
        return jsonify({"items": items, "total": len(items)}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews/<review_id>", methods=["GET"])
def get_review(review_id: str):
    """Get a single review assignment."""
    user_id = _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute(
            """SELECT r.id, r.draft_episode_id, r.reviewer_user_id, r.reviewee_user_id, r.status,
                      r.created_at, r.updated_at, d.title, d.committed_at
               FROM reviewer r
               JOIN draft_episodes d ON d.id = r.draft_episode_id
               WHERE r.id = ?""",
            (review_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "review not found"}), 404
        if row["reviewer_user_id"] != user_id and row["reviewee_user_id"] != user_id and not _is_admin():
            draft = conn.execute(
                "SELECT created_by FROM draft_episodes WHERE id = ?",
                (row["draft_episode_id"],),
            ).fetchone()
            if not draft or (draft["created_by"] or "") != user_id:
                conn.close()
                return jsonify({"error": "forbidden"}), 403
        item = {
            "id": row["id"],
            "draftEpisodeId": row["draft_episode_id"],
            "reviewerUserId": row["reviewer_user_id"],
            "revieweeUserId": row["reviewee_user_id"],
            "status": row["status"],
            "createdAt": row["created_at"],
            "updatedAt": row["updated_at"],
            "draftTitle": row["title"],
            "committedAt": row["committed_at"],
        }
        conn.close()
        return jsonify(item), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500


@app.route("/api/reviews/<review_id>", methods=["PATCH"])
def update_review(review_id: str):
    """Update review status. Blocked if approved + committed."""
    body = request.get_json() or {}
    status = body.get("status")
    if status not in ("pending", "approved", "request_changes"):
        return jsonify({"error": "invalid status"}), 400
    user_id = _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute(
            """SELECT r.id, r.reviewer_user_id, r.draft_episode_id, r.status, d.committed_at
               FROM reviewer r JOIN draft_episodes d ON d.id = r.draft_episode_id WHERE r.id = ?""",
            (review_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "review not found"}), 404
        if row["reviewer_user_id"] != user_id and not _is_admin():
            conn.close()
            return jsonify({"error": "forbidden"}), 403
        if row["committed_at"] and row["status"] == "approved":
            conn.close()
            return jsonify({"error": "cannot change review after commit"}), 409
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        conn.execute("UPDATE reviewer SET status = ?, updated_at = ? WHERE id = ?", (status, now, review_id))
        if status == "approved":
            cur = conn.execute("SELECT reviewee_user_id FROM reviewer WHERE id = ?", (review_id,))
            reviewee = cur.fetchone()["reviewee_user_id"]
            _create_notification(
                conn, reviewee, "review_approved",
                "Your draft has been approved by a reviewer",
                draft_id=row["draft_episode_id"], review_id=review_id,
            )
            try:
                from continuuuum_api.localization_helpers import advance_change_list_on_review_approve
            except ImportError:
                from localization_helpers import advance_change_list_on_review_approve
            advance_change_list_on_review_approve(conn, row["draft_episode_id"])
        elif status == "request_changes":
            cur = conn.execute("SELECT reviewee_user_id FROM reviewer WHERE id = ?", (review_id,))
            reviewee = cur.fetchone()["reviewee_user_id"]
            _create_notification(
                conn, reviewee, "review_denied",
                "Reviewer requested changes on your draft",
                draft_id=row["draft_episode_id"], review_id=review_id,
            )
            try:
                script_row = conn.execute(
                    "SELECT script_text FROM draft_episode_script WHERE draft_episode_id = ? ORDER BY updated_at DESC LIMIT 1",
                    (row["draft_episode_id"],),
                ).fetchone()
                script_text = script_row["script_text"] if script_row else ""
                archive_review_comments_on_deny(conn, review_id, script_text or "")
            except Exception:
                pass
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews/<review_id>/comments", methods=["GET"])
def list_review_comments(review_id: str):
    """List reviewer_comments for a review."""
    try:
        conn = get_conn()
        cur = conn.execute(
            """SELECT id, reviewer_id, script_ref, text_selection_start, text_selection_end,
                      comment_text, created_at, delete_requested_at, delete_requested_by
               FROM reviewer_comments WHERE reviewer_id = ? ORDER BY created_at""",
            (review_id,),
        )
        rows = cur.fetchall()
        conn.close()
        items = [
            {
                "id": r["id"],
                "reviewerId": r["reviewer_id"],
                "scriptRef": r["script_ref"],
                "textSelectionStart": r["text_selection_start"],
                "textSelectionEnd": r["text_selection_end"],
                "commentText": r["comment_text"],
                "createdAt": r["created_at"],
                "deleteRequestedAt": r["delete_requested_at"] if "delete_requested_at" in r.keys() else None,
                "deleteRequestedBy": r["delete_requested_by"] if "delete_requested_by" in r.keys() else None,
            }
            for r in rows
        ]
        return jsonify({"items": items}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews/<review_id>/comments", methods=["POST"])
def add_review_comment(review_id: str):
    """Add comment with text_selection_start, text_selection_end."""
    # todo: review: i'm unfamiliar with string escaping in python and the gotchas + followon questions around including more ids for association
    
    body = request.get_json() or {}
    comment_text = body.get("commentText", "").strip()
    if not comment_text:
        return jsonify({"error": "commentText required"}), 400
    script_ref = body.get("scriptRef")
    text_selection_start = body.get("textSelectionStart")
    text_selection_end = body.get("textSelectionEnd")
    user_id = _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute(
            """SELECT r.id, r.reviewer_user_id, r.reviewee_user_id, r.draft_episode_id, d.committed_at
               FROM reviewer r JOIN draft_episodes d ON d.id = r.draft_episode_id WHERE r.id = ?""",
            (review_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "review not found"}), 404
        if row["reviewer_user_id"] != user_id and not _is_admin():
            conn.close()
            return jsonify({"error": "forbidden"}), 403
        if row["committed_at"]:
            conn.close()
            return jsonify({"error": "cannot add comment after commit"}), 409
        try:
            conn.execute("ALTER TABLE reviewer_comments ADD COLUMN review_cycle INTEGER NOT NULL DEFAULT 0")
        except sqlite3.OperationalError:
            pass
        rev_cycle_row = conn.execute("SELECT review_cycle FROM reviewer WHERE id = ?", (review_id,)).fetchone()
        review_cycle = int(rev_cycle_row["review_cycle"] or 0) if rev_cycle_row else 0
        existing = conn.execute(
            "SELECT COUNT(*) AS c FROM reviewer_comments WHERE reviewer_id = ? AND review_cycle = ?",
            (review_id, review_cycle),
        ).fetchone()
        if existing and existing["c"] > 0:
            conn.close()
            return jsonify({"error": "one comment per review cycle; use Approve or Deny"}), 409
        cid = str(uuid.uuid4())
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        conn.execute(
            "INSERT INTO reviewer_comments (id, reviewer_id, script_ref, text_selection_start, text_selection_end, comment_text, review_cycle, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            (cid, review_id, script_ref, text_selection_start, text_selection_end, comment_text, review_cycle, now),
        )
        _create_notification(
            conn, row["reviewee_user_id"], "comment",
            "New comment on your draft",
            draft_id=row["draft_episode_id"], review_id=review_id,
        )
        conn.commit()
        conn.close()
        return jsonify({"id": cid}), 201
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reviews/<review_id>/comments/<comment_id>", methods=["PATCH"])
def update_review_comment(review_id: str, comment_id: str):
    """Edit or delete comment. Body: commentText (null to delete), requestDelete, approveDelete, denyDelete."""
    body = request.get_json() or {}
    user_id = _get_current_user()
    admin = _is_admin()
    try:
        conn = get_conn()
        cur = conn.execute(
            """SELECT r.reviewer_user_id, r.reviewee_user_id, d.committed_at FROM reviewer r
               JOIN draft_episodes d ON d.id = r.draft_episode_id WHERE r.id = ?""",
            (review_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "review not found"}), 404
        is_reviewer = row["reviewer_user_id"] == user_id
        is_reviewee = row["reviewee_user_id"] == user_id
        if row["committed_at"]:
            conn.close()
            return jsonify({"error": "cannot edit comment after commit"}), 409
        if body.get("requestDelete"):
            if not is_reviewer and not admin:
                conn.close()
                return jsonify({"error": "forbidden"}), 403
            now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
            conn.execute(
                "UPDATE reviewer_comments SET delete_requested_at = ?, delete_requested_by = ? WHERE id = ? AND reviewer_id = ?",
                (now, user_id, comment_id, review_id),
            )
            cur = conn.execute(
                "SELECT r.reviewee_user_id, r.reviewer_user_id, r.draft_episode_id FROM reviewer r WHERE r.id = ?",
                (review_id,),
            )
            rev = cur.fetchone()
            if rev:
                other = rev["reviewee_user_id"] if user_id == rev["reviewer_user_id"] else rev["reviewer_user_id"]
                if other:
                    _create_notification(
                        conn, other, "comment_delete_requested",
                        "Comment delete requested on draft review",
                        draft_id=rev["draft_episode_id"], review_id=review_id,
                    )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        if body.get("approveDelete") or body.get("denyDelete"):
            if not is_reviewee and not admin:
                conn.close()
                return jsonify({"error": "forbidden"}), 403
        if body.get("approveDelete"):
            try:
                from continuuuum_api.localization_routes import build_previously_on
            except ImportError:
                from localization_routes import build_previously_on
            cur = conn.execute(
                "SELECT * FROM reviewer_comments WHERE id = ? AND reviewer_id = ?",
                (comment_id, review_id),
            )
            cmt = cur.fetchone()
            if cmt:
                cdict = dict(cmt)
                now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
                prev = build_previously_on(cdict)
                conn.execute(
                    """INSERT INTO reviewer_comments_archive (
                        id, reviewer_id, original_comment_id, comment_text, previously_on,
                        text_selection_start, text_selection_end, property_key, review_cycle, archived_at, archived_reason
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'delete_approved')""",
                    (
                        str(uuid.uuid4()), review_id, cdict["id"], cdict["comment_text"], prev,
                        cdict["text_selection_start"], cdict["text_selection_end"], cdict.get("property_key"),
                        cdict.get("review_cycle") or 0, now,
                    ),
                )
                conn.execute("DELETE FROM reviewer_comments WHERE id = ?", (comment_id,))
                req_by = cdict.get("delete_requested_by")
                if req_by:
                    _create_notification(
                        conn, req_by, "comment_delete_approved",
                        "Comment delete approved",
                        review_id=review_id,
                    )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        if body.get("denyDelete"):
            conn.execute(
                "UPDATE reviewer_comments SET delete_requested_at = NULL, delete_requested_by = NULL WHERE id = ? AND reviewer_id = ?",
                (comment_id, review_id),
            )
            conn.commit()
            conn.close()
            return jsonify({"ok": True}), 200
        if not is_reviewer and not admin:
            conn.close()
            return jsonify({"error": "forbidden"}), 403
        if "commentText" in body and body["commentText"] is None:
            conn.execute("DELETE FROM reviewer_comments WHERE id = ? AND reviewer_id = ?", (comment_id, review_id))
        else:
            updates, params = [], []
            for k, col in [("commentText", "comment_text"), ("scriptRef", "script_ref"),
                           ("textSelectionStart", "text_selection_start"), ("textSelectionEnd", "text_selection_end")]:
                if k in body and body[k] is not None:
                    val = body[k]
                    if col == "text_selection_start" or col == "text_selection_end":
                        val = val if isinstance(val, int) else None
                    updates.append(f"{col} = ?")
                    params.append(val)
            if updates:
                params.extend([comment_id, review_id])
                conn.execute(f"UPDATE reviewer_comments SET {', '.join(updates)} WHERE id = ? AND reviewer_id = ?", params)
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/drafts/episodes/<draft_id>/commit", methods=["POST"])
def commit_draft(draft_id: str):
    """Commit approved draft. Requires caller in approved_user; all reviewers must have approved. Sets committed_at."""
    user_id = _get_current_user()
    try:
        conn = get_conn()
        if not _is_approved_to_commit(conn, user_id):
            conn.close()
            return jsonify({"error": "not authorized to commit"}), 403
        cur = conn.execute(
            "SELECT id, episode_id, tenant_id, title, engine, scene_path, t_start, t_end, plot_description, created_by, committed_at FROM draft_episodes WHERE id = ?",
            (draft_id,),
        )
        row = cur.fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "draft not found"}), 404
        if row["committed_at"]:
            conn.close()
            return jsonify({"error": "draft already committed"}), 409
        cur = conn.execute("SELECT id, status FROM reviewer WHERE draft_episode_id = ?", (draft_id,))
        reviewers = cur.fetchall()
        if reviewers:
            for r in reviewers:
                if r["status"] != "approved":
                    conn.close()
                    return jsonify({"error": "all reviewers must approve first", "pending": [x["id"] for x in reviewers if x["status"] != "approved"]}), 409
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        ep_id = row["episode_id"] or str(uuid.uuid4())
        if not row["episode_id"]:
            conn.execute(
                """INSERT INTO episodes (id, tenant_id, title, created_at, engine, scene_path, t_start, t_end, plot_description)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (ep_id, row["tenant_id"], row["title"], now, row["engine"], row["scene_path"], row["t_start"], row["t_end"], row["plot_description"]),
            )
        else:
            conn.execute(
                "UPDATE episodes SET tenant_id=?, title=?, engine=?, scene_path=?, t_start=?, t_end=?, plot_description=? WHERE id=?",
                (row["tenant_id"], row["title"], row["engine"], row["scene_path"], row["t_start"], row["t_end"], row["plot_description"], ep_id),
            )
        cur = conn.execute("SELECT id, script_text, language, min_thesaurus_version, episode_script_id FROM draft_episode_script WHERE draft_episode_id = ?", (draft_id,))
        for r in cur.fetchall():
            script_id = r["episode_script_id"] or str(uuid.uuid4())
            if r["episode_script_id"]:
                conn.execute(
                    "UPDATE episode_script SET script_text=?, language=?, min_thesaurus_version=? WHERE id=?",
                    (r["script_text"], r["language"], r["min_thesaurus_version"], script_id),
                )
            else:
                conn.execute(
                    "INSERT INTO episode_script (id, episode_id, script_ref, script_text, language, min_thesaurus_version, created_at) VALUES (?, ?, NULL, ?, ?, ?, ?)",
                    (script_id, ep_id, r["script_text"], r["language"], r["min_thesaurus_version"], now),
                )
        conn.execute("UPDATE draft_episodes SET committed_at = ? WHERE id = ?", (now, draft_id))
        reviewee = row["created_by"]
        if reviewee:
            _create_notification(
                conn, reviewee, "committed",
                f"Draft \"{row['title']}\" has been committed to episode",
                draft_id=draft_id,
            )
        conn.commit()
        conn.close()
        return jsonify({"ok": True, "episodeId": ep_id}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# --- Approved users (admin only) ---
@app.route("/api/approved-users", methods=["GET"])
def list_approved_users():
    """List approved users. Admin only."""
    if not _is_admin():
        return jsonify({"error": "admin required"}), 403
    try:
        conn = get_conn()
        cur = conn.execute("SELECT id, user_id, added_by, added_at FROM approved_user ORDER BY added_at")
        rows = cur.fetchall()
        conn.close()
        items = [{"id": r["id"], "userId": r["user_id"], "addedBy": r["added_by"], "addedAt": r["added_at"]} for r in rows]
        return jsonify({"items": items}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/approved-users", methods=["POST"])
def add_approved_user():
    """Add approved user. Admin only. Body: userId."""
    if not _is_admin():
        return jsonify({"error": "admin required"}), 403
    body = request.get_json() or {}
    uid = body.get("userId")
    if not uid:
        return jsonify({"error": "userId required"}), 400
    try:
        conn = get_conn()
        aid = str(uuid.uuid4())
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        conn.execute(
            "INSERT INTO approved_user (id, user_id, added_by, added_at) VALUES (?, ?, ?, ?)",
            (aid, uid, _get_current_user(), now),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": aid}), 201
    except sqlite3.IntegrityError:
        return jsonify({"error": "user already approved"}), 409
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/approved-users/<user_id>", methods=["DELETE"])
def remove_approved_user(user_id: str):
    """Remove approved user. Admin only."""
    if not _is_admin():
        return jsonify({"error": "admin required"}), 403
    try:
        conn = get_conn()
        conn.execute("DELETE FROM approved_user WHERE user_id = ?", (user_id,))
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# --- Notifications ---
@app.route("/api/notifications", methods=["GET"])
def list_notifications():
    """List notifications for current user. Query: limit (default 20)."""
    limit = int(request.args.get("limit", 20))
    user_id = _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute(
            "SELECT id, user_id, type, draft_id, review_id, message, read_at, created_at FROM notifications WHERE user_id = ? ORDER BY created_at DESC LIMIT ?",
            (user_id, limit),
        )
        rows = cur.fetchall()
        unread = sum(1 for r in rows if not r["read_at"])
        conn.close()
        items = [
            {
                "id": r["id"],
                "userId": r["user_id"],
                "type": r["type"],
                "draftId": r["draft_id"],
                "reviewId": r["review_id"],
                "message": r["message"],
                "readAt": r["read_at"],
                "createdAt": r["created_at"],
            }
            for r in rows
        ]
        return jsonify({"items": items, "unreadCount": unread}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/users", methods=["GET"])
def list_users():
    """List users from user_presence (for reviewer selection)."""
    try:
        conn = get_conn()
        cur = conn.execute("SELECT user_id, last_seen_at FROM user_presence ORDER BY last_seen_at DESC LIMIT 100")
        rows = cur.fetchall()
        conn.close()
        items = [{"userId": r["user_id"], "lastSeenAt": r["last_seen_at"]} for r in rows]
        return jsonify({"items": items}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_audit_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/notifications/<notification_id>/read", methods=["POST"])
def mark_notification_read(notification_id: str):
    """Mark notification as read."""
    user_id = _get_current_user()
    try:
        conn = get_conn()
        cur = conn.execute("SELECT id FROM notifications WHERE id = ? AND user_id = ?", (notification_id, user_id))
        if not cur.fetchone():
            conn.close()
            return jsonify({"error": "not found"}), 404
        now = __import__("datetime").datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
        conn.execute("UPDATE notifications SET read_at = ? WHERE id = ?", (now, notification_id))
        conn.commit()
        conn.close()
        return jsonify({"ok": True}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_review_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/language-audit", methods=["GET"])
def thesaurus_language_audit():
    """Report discrepancies: missing translations, version mismatches, terms without EFIGS fallback.
    Query: episodeId (optional), episodeScriptId (optional).
    EFIGS = en, fr, it, de, es."""
    episode_id = request.args.get("episodeId")
    episode_script_id = request.args.get("episodeScriptId")
    EFIGS = ["en", "fr", "it", "de", "es"]
    try:
        conn = get_conn()
        discrepancies = []
        scripts_to_check = []
        if episode_script_id:
            cur = conn.execute(
                "SELECT id, episode_id, script_text, language, min_thesaurus_version FROM episode_script WHERE id = ?",
                (episode_script_id,),
            )
            row = cur.fetchone()
            if row:
                scripts_to_check.append(dict(row))
        elif episode_id:
            cur = conn.execute(
                "SELECT id, episode_id, script_text, language, min_thesaurus_version FROM episode_script WHERE episode_id = ?",
                (episode_id,),
            )
            scripts_to_check = [dict(r) for r in cur.fetchall()]
        else:
            cur = conn.execute(
                "SELECT id, episode_id, script_text, language, min_thesaurus_version FROM episode_script"
            )
            scripts_to_check = [dict(r) for r in cur.fetchall()]
        lang_ids = {r["code"]: r["id"] for r in conn.execute("SELECT id, code FROM languages").fetchall()}
        for es in scripts_to_check:
            sid, eid, script_text, lang_code, min_ver = (
                es["id"], es["episode_id"], es["script_text"] or "", es["language"] or "en", es.get("min_thesaurus_version"),
            )
            tokens = script_text.split()
            src_lang_id = lang_ids.get(lang_code)
            for token in set(t.strip().lower() for t in tokens if t.strip()):
                entry = None
                if src_lang_id:
                    cur = conn.execute(
                        "SELECT id, term, version FROM thesaurus_entries WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1",
                        (src_lang_id, token, token),
                    )
                    entry = cur.fetchone()
                e = None
                if not entry:
                    for fallback_code in EFIGS:
                        lid = lang_ids.get(fallback_code)
                        if lid:
                            cur = conn.execute(
                                "SELECT id, version FROM thesaurus_entries WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1",
                                (lid, token, token),
                            )
                            e = cur.fetchone()
                            if e:
                                break
                    else:
                        discrepancies.append({
                            "episodeId": eid,
                            "scriptId": sid,
                            "term": token,
                            "language": lang_code,
                            "issue": "no_efigs_fallback",
                        })
                        continue
                entry_id = (entry or e)["id"]
                entry_version = (entry or e).get("version") or "1.0"
                if min_ver and entry_version and _version_gt(entry_version, min_ver):
                    discrepancies.append({
                        "episodeId": eid,
                        "scriptId": sid,
                        "term": token,
                        "language": lang_code,
                        "issue": "version_mismatch",
                        "entryVersion": entry_version,
                        "minThesaurusVersion": min_ver,
                    })
                for target_code in EFIGS:
                    if target_code == (lang_code or "en"):
                        continue
                    tid = lang_ids.get(target_code)
                    if not tid:
                        continue
                    cur = conn.execute(
                        "SELECT 1 FROM thesaurus_translations WHERE entry_id = ? AND language_id = ?",
                        (entry_id, tid),
                    )
                    if not cur.fetchone():
                        cur = conn.execute(
                            "SELECT 1 FROM change_of_basis_word_overrides WHERE term = ? AND target_language_id = ?",
                            (token, tid),
                        )
                        if not cur.fetchone():
                            discrepancies.append({
                                "episodeId": eid,
                                "scriptId": sid,
                                "term": token,
                                "language": target_code,
                                "issue": "missing_translation",
                            })
        conn.close()
        return jsonify({"discrepancies": discrepancies}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e)}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/definition", methods=["GET"])
def get_thesaurus_definition():
    """Get dictionary definition for token. Query: token, language.
    EFIGS fallback order: requested lang, en, fr, it, de, es. Returns most recent version."""
    token = request.args.get("token", "")
    language = request.args.get("language", "en")
    if not token:
        return jsonify({"error": "token required"}), 400
    EFIGS = ["en", "fr", "it", "de", "es"]
    try:
        conn = get_conn()
        lang_order = [language] if language not in EFIGS else []
        for code in EFIGS:
            if code not in lang_order:
                lang_order.append(code)
        cur = conn.execute("SELECT id, code FROM languages")
        lang_ids = {r["code"]: r["id"] for r in cur.fetchall()}
        entry_id = None
        for code in lang_order:
            lid = lang_ids.get(code)
            if not lid:
                continue
            cur = conn.execute(
                "SELECT id FROM thesaurus_entries WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1",
                (lid, token.lower(), token),
            )
            row = cur.fetchone()
            if row:
                entry_id = row["id"]
                break
        if not entry_id:
            conn.close()
            return jsonify({"token": token, "definition": None}), 200
        for code in lang_order:
            lid = lang_ids.get(code)
            if not lid:
                continue
            cur = conn.execute(
                """SELECT definition, source, version, created_at
                   FROM dictionary_definitions WHERE entry_id = ? AND language_id = ?
                   ORDER BY version DESC, created_at DESC LIMIT 1""",
                (entry_id, lid),
            )
            row = cur.fetchone()
            if row:
                conn.close()
                return jsonify({
                    "token": token,
                    "definition": row["definition"],
                    "sourceLanguage": code,
                    "fallback": code != language,
                    "source": row["source"],
                }), 200
        conn.close()
        return jsonify({"token": token, "definition": None}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_dictionary_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def _version_gt(a: str, b: str) -> bool:
    """True if semver a > b."""
    def parse(v):
        parts = (v or "0").split(".")
        return tuple(int(x) if x.isdigit() else 0 for x in (parts + ["0", "0"])[:3])
    return parse(a) > parse(b)


@app.route("/api/thesaurus/languages", methods=["GET"])
def list_thesaurus_languages():
    """Read-only language codes for translation UI."""
    try:
        conn = get_conn()
        ensure_default_languages(conn)
        conn.commit()
        cur = conn.execute("SELECT id, code FROM languages ORDER BY code")
        items = [{"id": r["id"], "code": r["code"]} for r in cur.fetchall()]
        conn.close()
        return jsonify({"items": items}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/export-xliff", methods=["GET"])
def export_xliff():
    """Export thesaurus translations to XLIFF 2.0 XML. Query: sourceLang, targetLang."""
    source_lang = request.args.get("sourceLang", "en")
    target_lang = request.args.get("targetLang", "")
    if not target_lang:
        return jsonify({"error": "targetLang required"}), 400
    try:
        conn = get_conn()
        ensure_default_languages(conn)
        conn.commit()
        xml_str = xliff_converter.export_to_xliff(conn, source_lang, target_lang)
        conn.close()
        return Response(xml_str, mimetype="application/xml", headers={"Content-Disposition": "attachment; filename=thesaurus.xliff"})
    except ValueError as e:
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/thesaurus/import-xliff", methods=["POST"])
def import_xliff():
    """Import XLIFF and update thesaurus_translations. Body: file (multipart) or xliff (JSON string)."""
    xliff_content = None
    if request.content_type and "application/json" in request.content_type:
        body = request.get_json() or {}
        xliff_content = body.get("xliff")
        if isinstance(xliff_content, str):
            pass
        else:
            xliff_content = None
    if xliff_content is None and "file" in request.files:
        xliff_content = request.files["file"].read()
        if isinstance(xliff_content, bytes):
            xliff_content = xliff_content.decode("utf-8", errors="replace")
    if not xliff_content:
        return jsonify({"error": "xliff string (JSON body.xliff) or file (multipart) required"}), 400
    try:
        conn = get_conn()
        updated, inserted = xliff_converter.import_from_xliff(conn, xliff_content)
        conn.close()
        return jsonify({"ok": True, "updated": updated, "inserted": inserted}), 200
    except ValueError as e:
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def _write_deeplink_file(window: str, episode_id: str = "", entry_id: str = "") -> str:
    path = os.environ.get("CONTINUUUUM_DEEPLINK_PATH", os.path.expanduser("~/.continuuuum-deeplink.json"))
    payload = {"window": window}
    if episode_id:
        payload["episodeId"] = episode_id
    if entry_id:
        payload["entryId"] = entry_id
    with open(path, "w") as f:
        json.dump(payload, f)
    return path


@app.route("/api/deeplink", methods=["POST"])
def write_deeplink():
    """Write deeplink file for Unity DeepLinkHandler. Body: window, episodeId."""
    body = request.get_json() or {}
    window = body.get("window", "")
    episode_id = body.get("episodeId", "")
    entry_id = body.get("entryId", "")
    try:
        path = _write_deeplink_file(window, episode_id, entry_id)
        return jsonify({"ok": True, "path": path}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# --- Cave adapters (Tome, config, hierarchy) ---
@app.route("/api/cave/tome/header")
def get_tome_header():
    """Return Tome header HTML from Cave adapter."""
    if cave_adapter:
        return Response(cave_adapter.get_tome_header(), mimetype="text/html")
    return Response("<header></header>", mimetype="text/html")


@app.route("/api/cave/tome/footer")
def get_tome_footer():
    """Return Tome footer HTML from Cave adapter."""
    if cave_adapter:
        return Response(cave_adapter.get_tome_footer(), mimetype="text/html")
    return Response("<footer></footer>", mimetype="text/html")


@app.route("/api/cave/config-overview")
def get_cave_config_overview():
    """Return Cave config overview (Cave, Tome, LogViewMachine, RobotCopy, CaveRobit)."""
    if cave_adapter:
        return jsonify(cave_adapter.get_config_overview())
    return jsonify({"cave": {}, "tome": {}, "logViewMachine": {}, "robotCopy": {}, "caveRobit": {}})


@app.route("/api/herb-garden/compare", methods=["POST"])
def herb_garden_compare():
    """Compare causality trees to plant germination. Body: draftIds, episodeIds, plantScript. Or plantFile (multipart)."""
    if not herb_garden:
        return jsonify({"error": "herb_garden module not available"}), 500
    script_nodes = []
    plant_script = ""
    if request.content_type and "application/json" in (request.content_type or ""):
        body = request.get_json() or {}
        draft_ids = body.get("draftIds") or []
        episode_ids = body.get("episodeIds") or []
        plant_script = body.get("plantScript") or ""
    else:
        draft_ids = request.form.getlist("draftIds") or []
        episode_ids = request.form.getlist("episodeIds") or []
        plant_script = request.form.get("plantScript", "")
        if "plantFile" in request.files:
            plant_script = request.files["plantFile"].read().decode("utf-8", errors="replace")
    try:
        conn = get_conn()
        for eid in episode_ids:
            script_nodes.extend(herb_garden.get_causality_for_episode(conn, eid))
        for did in draft_ids:
            script_nodes.extend(herb_garden.get_causality_for_draft(conn, did))
        conn.close()
        plant_nodes = herb_garden.parse_plant_script(plant_script)
        report = herb_garden.compare_structures(script_nodes, plant_nodes)
        return jsonify(report), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/cave/hierarchy")
def export_cave_hierarchy():
    """Export hierarchy JSON and return it. Query: path (optional output path)."""
    if not cave_hierarchy_adapter:
        return jsonify({"error": "cave_hierarchy_adapter not available"}), 500
    path = request.args.get("path")
    if not path:
        static_dir = Path(__file__).resolve().parent / "static"
        path = str(static_dir / "hierarchy.json")
    try:
        hierarchy = cave_hierarchy_adapter.export_hierarchy_flatfile(path)
        return jsonify(hierarchy)
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/ui", strict_slashes=False)
@app.route("/ui/", strict_slashes=False)
@app.route("/ui/<path:subpath>", strict_slashes=False)
@app.route("/")
def serve_ui(subpath=None):
    """Serve Continuuum web UI (ui.html for all /ui paths)."""
    static_dir = Path(__file__).resolve().parent / "static"
    return send_from_directory(static_dir, "ui.html")


@app.route("/library-legacy-redirect")
def redirect_library_legacy():
    """Deprecated: library is served on same origin via library_routes."""
    return redirect("/library", code=301)


@app.route("/continuuuum_editor-legacy")
def redirect_continuuuum_editor_legacy():
    return redirect("/continuuuum_editor/", code=301)


# Legacy redirects removed — library_routes handles /library and /continuuuum_editor


@app.route("/api/audit", methods=["GET"])
def get_audit_log():
    """Get audit log. Contractors see own rows; admins see all. Query: limit, offset."""
    if not _is_admin():
        user_id = _get_current_user()
        where = " WHERE user_id = ?"
        params = [user_id]
    else:
        where = ""
        params = []
    limit = int(request.args.get("limit", 100))
    offset = int(request.args.get("offset", 0))
    params.extend([limit, offset])
    try:
        conn = get_conn()
        cur = conn.execute(
            f"""SELECT id, timestamp, user_id, api_path, method, remark, request_id, episode_id, status_code
                FROM api_audit_log {where}
                ORDER BY timestamp DESC LIMIT ? OFFSET ?""",
            params,
        )
        rows = cur.fetchall()
        conn.close()
        items = [
            {
                "id": r["id"],
                "timestamp": r["timestamp"],
                "userId": r["user_id"],
                "apiPath": r["api_path"],
                "method": r["method"],
                "remark": r["remark"],
                "requestId": r["request_id"],
                "episodeId": r["episode_id"],
                "statusCode": r["status_code"],
            }
            for r in rows
        ]
        return jsonify({"items": items}), 200
    except sqlite3.OperationalError as e:
        return jsonify({"error": str(e), "hint": "Apply continuuuum_audit_schema.sql"}), 500
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/deeplink", methods=["GET"])
def write_deeplink_get():
    """Write deeplink file via GET (for clickable links). Query: window, episodeId."""
    window = request.args.get("window", "")
    episode_id = request.args.get("episodeId", "")
    entry_id = request.args.get("entryId", "")
    try:
        path = _write_deeplink_file(window, episode_id, entry_id)
        return Response(f"Deeplink written to {path}. Open Unity Editor.", status=200, mimetype="text/plain")
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def main():
    import argparse
    import threading
    import time

    p = argparse.ArgumentParser(description="Continuuuum API for episode script and thesaurus")
    p.add_argument("--port", type=int, default=5050)
    p.add_argument("--host", default="127.0.0.1")
    p.add_argument("--db", default=None, help="Path to continuuuum.db")
    args = p.parse_args()
    if args.db:
        os.environ["CONTINUUUUM_DB"] = args.db

    if os.environ.get("CONTINUUUUM_SUBMIT_CRON", "").strip() in ("1", "true", "yes"):
        def _cron_loop():
            from submit_scheduler import process_submitted

            while True:
                try:
                    conn = sqlite3.connect(os.environ.get("CONTINUUUUM_DB", "continuuuum.db"))
                    conn.row_factory = sqlite3.Row
                    process_submitted(conn)
                    conn.close()
                except Exception as ex:
                    print(f"[submit-cron] {ex}", flush=True)
                time.sleep(60)

        threading.Thread(target=_cron_loop, daemon=True, name="continuuuum-submit-cron").start()

    if socketio:
        socketio.run(app, host=args.host, port=args.port, debug=True, allow_unsafe_werkzeug=True)
    else:
        app.run(host=args.host, port=args.port, debug=True)


if __name__ == "__main__":
    main()
