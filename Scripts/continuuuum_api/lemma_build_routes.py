"""Lemma Build web API: settings, chat proxy, sessions, batch file extract."""

from __future__ import annotations

import json
import os
import re
import sqlite3
import threading
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Callable

from flask import jsonify, request, send_file

try:
    from continuuuum_api.lemma_build_db import (
        ENGINES,
        ensure_lemma_build_schema,
        get_settings,
        load_system_preface,
        new_id,
        normalize_engine,
        put_settings,
        _now,
    )
except ImportError:
    from lemma_build_db import (
        ENGINES,
        ensure_lemma_build_schema,
        get_settings,
        load_system_preface,
        new_id,
        normalize_engine,
        put_settings,
        _now,
    )

GetConn = Callable[[], sqlite3.Connection]
IsAdmin = Callable[[], bool]

_REPO_ROOT = Path(__file__).resolve().parents[2]
_active_chats: dict[str, int] = {}
_active_lock = threading.Lock()

DESCRIPTOR_FENCE = re.compile(
    r"```(?:json\s+)?lemma-mechanism-descriptor\s*([\s\S]*?)```",
    re.IGNORECASE,
)
CODE_FENCE = re.compile(
    r"```([a-zA-Z0-9_+-]*)[^\n]*?(?:path\s*=\s*[\"']?([^\s\"'`]+)[\"']?|[^\n]*?([A-Za-z0-9_./\\-]+\.(?:cs|hx|py|js|ts|json|md|txt)))?\s*\n([\s\S]*?)```",
    re.IGNORECASE,
)
WRITE_FILE_TOOL = {
    "type": "function",
    "function": {
        "name": "write_file",
        "description": "Write a generated source file for the lemma batch.",
        "parameters": {
            "type": "object",
            "properties": {
                "path": {"type": "string"},
                "content": {"type": "string"},
            },
            "required": ["path", "content"],
        },
    },
}


def _is_admin_header() -> bool:
    return request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")


def _user_id() -> str:
    return request.headers.get("X-User-ID") or "anonymous"


def _resolve_batch_root(settings: dict) -> Path:
    raw = settings.get("batchOutputDir") or "Library/LemmaBuild/batches"
    p = Path(raw)
    if not p.is_absolute():
        p = _REPO_ROOT / p
    p.mkdir(parents=True, exist_ok=True)
    return p


def _safe_rel_path(name: str) -> str:
    name = (name or "file.txt").replace("\\", "/").lstrip("/")
    parts = [p for p in name.split("/") if p and p not in (".", "..")]
    return "/".join(parts) or "file.txt"


def parse_descriptor(text: str) -> dict | None:
    if not text:
        return None
    m = DESCRIPTOR_FENCE.search(text)
    blob = None
    if m:
        blob = m.group(1).strip()
    else:
        gm = re.search(r"```json\s*([\s\S]*?)```", text, re.IGNORECASE)
        if gm:
            blob = gm.group(1).strip()
        else:
            a, b = text.find("{"), text.rfind("}")
            if a >= 0 and b > a:
                blob = text[a : b + 1]
    if not blob:
        return None
    try:
        data = json.loads(blob)
    except json.JSONDecodeError:
        return None
    if not isinstance(data, dict):
        return None
    if not data.get("lemma") or not data.get("posTag") or not data.get("mechanicalRole"):
        return None
    return data


def extract_code_files(assistant_text: str, tool_calls: list | None = None) -> list[tuple[str, str]]:
    files: list[tuple[str, str]] = []
    if tool_calls:
        for tc in tool_calls:
            fn = (tc.get("function") or {}) if isinstance(tc, dict) else {}
            if fn.get("name") != "write_file":
                continue
            try:
                args = json.loads(fn.get("arguments") or "{}")
            except json.JSONDecodeError:
                continue
            path = _safe_rel_path(str(args.get("path") or "generated/file.txt"))
            content = str(args.get("content") or "")
            files.append((path, content))
    for m in CODE_FENCE.finditer(assistant_text or ""):
        lang = (m.group(1) or "").strip()
        path = m.group(2) or m.group(3)
        body = m.group(4) or ""
        if "lemma-mechanism-descriptor" in (lang + (path or "")).lower():
            continue
        if not path:
            ext = {"csharp": "cs", "cs": "cs", "haxe": "hx", "hx": "hx", "python": "py", "js": "js"}.get(
                lang.lower(), "txt"
            )
            path = f"generated/snippet_{len(files)+1}.{ext}"
        files.append((_safe_rel_path(path), body.rstrip() + "\n"))
    return files


def _chat_completions(base_url: str, model: str, messages: list[dict], tools: bool = True) -> dict:
    url = base_url.rstrip("/") + "/chat/completions"
    payload: dict[str, Any] = {
        "model": model,
        "messages": messages,
        "temperature": 0.4,
    }
    if tools:
        payload["tools"] = [WRITE_FILE_TOOL]
        payload["tool_choice"] = "auto"
    data = json.dumps(payload).encode()
    req = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        # Retry without tools if model rejects tools
        body = e.read().decode() if e.fp else ""
        if tools and e.code in (400, 404, 422):
            return _chat_completions(base_url, model, messages, tools=False)
        raise RuntimeError(f"chat_completions_failed:{e.code}:{body[:400]}") from e
    except urllib.error.URLError as e:
        raise RuntimeError(f"model_unreachable:{e}") from e


def _list_models(base_url: str) -> list[str]:
    url = base_url.rstrip("/") + "/models"
    req = urllib.request.Request(url, method="GET")
    with urllib.request.urlopen(req, timeout=20) as resp:
        data = json.loads(resp.read().decode())
    out = []
    for m in data.get("data") or []:
        mid = m.get("id") if isinstance(m, dict) else None
        if mid:
            out.append(mid)
    return out


def _append_chat_txt(batch_dir: Path, role: str, content: str) -> None:
    batch_dir.mkdir(parents=True, exist_ok=True)
    with open(batch_dir / "chat.txt", "a", encoding="utf-8") as f:
        f.write(f"\n### {role} ({_now()})\n{content}\n")


def _persist_generated(batch_dir: Path, files: list[tuple[str, str]], descriptor: dict | None) -> list[str]:
    written = []
    gen = batch_dir / "generated"
    for rel, content in files:
        dest = batch_dir / rel
        if not str(dest.resolve()).startswith(str(batch_dir.resolve())):
            continue
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_text(content, encoding="utf-8")
        written.append(rel)
    if descriptor is not None:
        (batch_dir / "descriptor.json").write_text(
            json.dumps(descriptor, indent=2, ensure_ascii=False),
            encoding="utf-8",
        )
        written.append("descriptor.json")
    return written


def register_lemma_build_routes(app, get_conn: GetConn, is_admin: IsAdmin | None = None) -> None:
    admin_fn = is_admin or _is_admin_header

    @app.before_request
    def _ensure():
        if getattr(app, "_lemma_build_ready", False):
            return
        if request.path.startswith("/api/lemma-build"):
            conn = get_conn()
            ensure_lemma_build_schema(conn)
            conn.close()
            app._lemma_build_ready = True

    @app.route("/api/lemma-build/engines", methods=["GET"])
    def lemma_build_engines():
        return jsonify({"engines": ENGINES})

    @app.route("/api/lemma-build/settings", methods=["GET"])
    def lemma_build_settings_get():
        tenant = request.args.get("tenantId") or "default"
        conn = get_conn()
        s = get_settings(conn, tenant)
        conn.close()
        if not admin_fn():
            return jsonify(
                {
                    "defaultModelId": s["defaultModelId"],
                    "maxConcurrentBuilds": s["maxConcurrentBuilds"],
                    "defaultEngine": s["defaultEngine"],
                    "engines": s["engines"],
                }
            )
        return jsonify(s)

    @app.route("/api/lemma-build/settings", methods=["PUT"])
    def lemma_build_settings_put():
        if not admin_fn():
            return jsonify({"error": "admin_required"}), 403
        body = request.get_json(silent=True) or {}
        tenant = body.get("tenantId") or "default"
        conn = get_conn()
        try:
            s = put_settings(conn, tenant, body, _user_id())
        except ValueError as e:
            conn.close()
            return jsonify({"error": str(e)}), 400
        conn.close()
        return jsonify(s)

    @app.route("/api/lemma-build/models", methods=["GET"])
    def lemma_build_models():
        if not admin_fn():
            return jsonify({"error": "admin_required"}), 403
        tenant = request.args.get("tenantId") or "default"
        conn = get_conn()
        s = get_settings(conn, tenant)
        conn.close()
        try:
            models = _list_models(s["lmStudioBaseUrl"])
        except Exception as e:
            return jsonify({"error": "models_unreachable", "detail": str(e)}), 502
        return jsonify({"models": models, "baseUrl": s["lmStudioBaseUrl"]})

    @app.route("/api/lemma-build/lm-status", methods=["GET"])
    def lemma_build_lm_status():
        """Public reachability probe (no admin). Does not expose the base URL to non-admins."""
        tenant = request.args.get("tenantId") or "default"
        conn = get_conn()
        s = get_settings(conn, tenant)
        conn.close()
        base = s["lmStudioBaseUrl"]
        try:
            models = _list_models(base)
            payload = {
                "ok": True,
                "reachable": True,
                "modelCount": len(models),
                "models": models[:8],
            }
            if admin_fn():
                payload["baseUrl"] = base
            return jsonify(payload)
        except Exception as e:
            payload = {
                "ok": False,
                "reachable": False,
                "modelCount": 0,
                "error": "models_unreachable",
                "detail": str(e),
            }
            if admin_fn():
                payload["baseUrl"] = base
            return jsonify(payload), 200

    @app.route("/api/lemma-build/parse-descriptor", methods=["POST"])
    def lemma_build_parse_descriptor():
        body = request.get_json(silent=True) or {}
        text = body.get("text") or body.get("assistantText") or ""
        desc = parse_descriptor(text)
        if desc is None:
            return jsonify({"ok": False, "error": "no_descriptor"}), 400
        return jsonify({"ok": True, "descriptor": desc})

    @app.route("/api/lemma-build/sessions", methods=["POST"])
    def lemma_build_sessions_post():
        body = request.get_json(silent=True) or {}
        tenant = body.get("tenantId") or "default"
        conn = get_conn()
        s = get_settings(conn, tenant)
        try:
            engine = normalize_engine(body.get("engine"), s["defaultEngine"])
        except ValueError as e:
            conn.close()
            return jsonify({"error": str(e)}), 400
        sid = new_id("lbsess")
        batch_root = _resolve_batch_root(s)
        batch_dir = batch_root / sid
        batch_dir.mkdir(parents=True, exist_ok=True)
        (batch_dir / "engine.txt").write_text(engine + "\n", encoding="utf-8")
        lemma_phrase = body.get("lemmaPhrase") or body.get("lemma") or ""
        (batch_dir / "meta.json").write_text(
            json.dumps(
                {
                    "sessionId": sid,
                    "engine": engine,
                    "lemma": lemma_phrase,
                    "modelId": body.get("modelId") or s["defaultModelId"],
                },
                indent=2,
            ),
            encoding="utf-8",
        )
        now = _now()
        conn.execute(
            """INSERT INTO lemma_build_sessions
               (id, tenant_id, lemma_phrase, model_id, engine, batch_dir, created_at, updated_at)
               VALUES (?,?,?,?,?,?,?,?)""",
            (
                sid,
                tenant,
                lemma_phrase,
                body.get("modelId") or s["defaultModelId"],
                engine,
                str(batch_dir),
                now,
                now,
            ),
        )
        conn.commit()
        conn.close()
        return jsonify(
            {
                "id": sid,
                "engine": engine,
                "batchDir": str(batch_dir),
                "modelId": body.get("modelId") or s["defaultModelId"],
            }
        ), 201

    @app.route("/api/lemma-build/sessions/<session_id>", methods=["GET"])
    def lemma_build_session_get(session_id: str):
        conn = get_conn()
        ensure_lemma_build_schema(conn)
        row = conn.execute(
            "SELECT * FROM lemma_build_sessions WHERE id = ?",
            (session_id,),
        ).fetchone()
        conn.close()
        if row is None:
            return jsonify({"error": "not_found"}), 404
        batch = Path(row["batch_dir"])
        files = []
        if batch.is_dir():
            for p in batch.rglob("*"):
                if p.is_file():
                    files.append(str(p.relative_to(batch)).replace("\\", "/"))
        return jsonify(
            {
                "id": row["id"],
                "lemma": row["lemma_phrase"],
                "modelId": row["model_id"],
                "engine": row["engine"],
                "batchDir": row["batch_dir"],
                "files": sorted(files),
                "createdAt": row["created_at"],
                "updatedAt": row["updated_at"],
            }
        )

    @app.route("/api/lemma-build/sessions/<session_id>/files/<path:file_path>", methods=["GET"])
    def lemma_build_session_file(session_id: str, file_path: str):
        conn = get_conn()
        row = conn.execute(
            "SELECT batch_dir FROM lemma_build_sessions WHERE id = ?",
            (session_id,),
        ).fetchone()
        conn.close()
        if row is None:
            return jsonify({"error": "not_found"}), 404
        batch = Path(row["batch_dir"]).resolve()
        target = (batch / _safe_rel_path(file_path)).resolve()
        if not str(target).startswith(str(batch)) or not target.is_file():
            return jsonify({"error": "file_not_found"}), 404
        return send_file(target, as_attachment=True)

    @app.route("/api/lemma-build/chat", methods=["POST"])
    def lemma_build_chat():
        body = request.get_json(silent=True) or {}
        tenant = body.get("tenantId") or "default"
        conn = get_conn()
        s = get_settings(conn, tenant)
        try:
            engine = normalize_engine(body.get("engine"), s["defaultEngine"])
        except ValueError as e:
            conn.close()
            return jsonify({"error": str(e)}), 400

        max_c = int(s["maxConcurrentBuilds"])
        with _active_lock:
            cur = _active_chats.get(tenant, 0)
            if max_c > 0 and cur >= max_c:
                conn.close()
                return jsonify({"error": "concurrency_limit", "max": max_c}), 429
            _active_chats[tenant] = cur + 1

        session_id = body.get("sessionId")
        batch_dir: Path | None = None
        if session_id:
            row = conn.execute(
                "SELECT * FROM lemma_build_sessions WHERE id = ?",
                (session_id,),
            ).fetchone()
            if row:
                batch_dir = Path(row["batch_dir"])
        if batch_dir is None:
            # ephemeral batch under root
            batch_dir = _resolve_batch_root(s) / ("ephemeral_" + new_id("tmp"))
            batch_dir.mkdir(parents=True, exist_ok=True)
            (batch_dir / "engine.txt").write_text(engine + "\n", encoding="utf-8")

        model = body.get("modelId") or s["defaultModelId"]
        user_messages = body.get("messages") or []
        system = load_system_preface(engine)
        messages = [{"role": "system", "content": system}]
        for m in user_messages:
            if not isinstance(m, dict):
                continue
            role = m.get("role") or "user"
            content = m.get("content") or ""
            if role in ("user", "assistant", "system") and content:
                messages.append({"role": role, "content": content})

        try:
            raw = _chat_completions(s["lmStudioBaseUrl"], model, messages, tools=True)
            choice = (raw.get("choices") or [{}])[0]
            msg = choice.get("message") or {}
            assistant_text = msg.get("content") or ""
            tool_calls = msg.get("tool_calls") or []
            if body.get("persist", True):
                last_user = ""
                for m in reversed(messages):
                    if m["role"] == "user":
                        last_user = m["content"]
                        break
                if last_user:
                    _append_chat_txt(batch_dir, "user", last_user)
                _append_chat_txt(batch_dir, "assistant", assistant_text or json.dumps(tool_calls))
                files = extract_code_files(assistant_text, tool_calls)
                desc = parse_descriptor(assistant_text)
                written = _persist_generated(batch_dir, files, desc)
            else:
                written = []
                desc = parse_descriptor(assistant_text)
            if session_id:
                conn.execute(
                    "UPDATE lemma_build_sessions SET updated_at = ?, engine = ?, model_id = ? WHERE id = ?",
                    (_now(), engine, model, session_id),
                )
                conn.commit()
            conn.close()
            return jsonify(
                {
                    "ok": True,
                    "assistant": assistant_text,
                    "toolCalls": tool_calls,
                    "engine": engine,
                    "modelId": model,
                    "sessionId": session_id,
                    "descriptor": desc,
                    "filesWritten": written,
                    "batchDir": str(batch_dir),
                }
            )
        except Exception as e:
            conn.close()
            return jsonify({"error": "chat_failed", "detail": str(e)}), 502
        finally:
            with _active_lock:
                _active_chats[tenant] = max(0, _active_chats.get(tenant, 1) - 1)

    # Static SPA
    static_dir = Path(__file__).resolve().parent / "static" / "lemma-build"

    @app.route("/lemma-build")
    @app.route("/lemma-build/")
    @app.route("/lemma-build/<path:subpath>")
    def lemma_build_spa(subpath: str = ""):
        from flask import send_from_directory

        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")
