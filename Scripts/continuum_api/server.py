"""
Flask API for continuum: episode script, thesaurus alternatives, AST nodes, rebalance, change-of-basis.
Run with continuum.db path; script-output app proxies /api to this server.
"""

import json
import os
import sys
import sqlite3
import uuid
from pathlib import Path

from flask import Flask, Response, jsonify, request

# Allow importing thesaurus when run from repo root or Scripts
_scripts = Path(__file__).resolve().parent.parent
if str(_scripts) not in sys.path:
    sys.path.insert(0, str(_scripts))
from thesaurus import farey_ast, xliff_converter, script_to_ast
import continuum_screenplay_work_orders as screenplay_wo

app = Flask(__name__)

# Set via env or default relative to repo
DEFAULT_DB = Path(__file__).resolve().parent.parent.parent / "continuum.db"


def get_db_path() -> Path:
    return Path(os.environ.get("CONTINUUM_DB", str(DEFAULT_DB)))


def get_conn():
    conn = sqlite3.connect(get_db_path())
    conn.row_factory = sqlite3.Row
    return conn


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


@app.route("/api/episode-script/<script_id>/screenplay", methods=["GET"])
def get_screenplay(script_id: str):
    """Return screenplay structure: blocks (scene, action, dialogue, sfx) with text, farey_span, audio_ref for language."""
    language = request.args.get("language", "en")
    try:
        conn = get_conn()
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
            ln, ld = n["farey_left_num"], n["farey_left_den"]
            rn, rd = n["farey_right_num"], n["farey_right_den"]
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
                )
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


@app.route("/api/thesaurus/export-xliff", methods=["GET"])
def export_xliff():
    """Export thesaurus translations to XLIFF 2.0 XML. Query: sourceLang, targetLang."""
    source_lang = request.args.get("sourceLang", "en")
    target_lang = request.args.get("targetLang", "")
    if not target_lang:
        return jsonify({"error": "targetLang required"}), 400
    try:
        conn = get_conn()
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


def main():
    import argparse
    p = argparse.ArgumentParser(description="Continuum API for episode script and thesaurus")
    p.add_argument("--port", type=int, default=5050)
    p.add_argument("--host", default="127.0.0.1")
    p.add_argument("--db", default=None, help="Path to continuum.db")
    args = p.parse_args()
    if args.db:
        os.environ["CONTINUUM_DB"] = args.db
    app.run(host=args.host, port=args.port, debug=True)


if __name__ == "__main__":
    main()
