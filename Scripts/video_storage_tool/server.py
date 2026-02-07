"""
Small Flask server for video dehydration (store) and reconstitution.
- POST /api/store: upload video → run store pipeline (background), return job id.
- GET /api/stored: list stored items. GET /api/stored/<id>/status: job status + manifest when ready.
- POST /api/reconstitute: run reconstitute for a stored id, return stream URL.
- GET /api/stream/<id>/info: stream file size (content_length) for download progress bar.
- GET /stream/<id>: serve reconstituted video with Range support (seekable); logs download progress to stderr.
- GET /: upload UI + list + HTML5 video player.
"""

import json
import logging
import re
import signal
import sys
import threading
import uuid
from pathlib import Path

from flask import Flask, Response, abort, jsonify, request, send_file

# Import pipeline from same package
from . import __main__ as cli
from .reconstitute import reconstitute

# Storage root (relative to server module dir)
STORAGE_ROOT = Path(__file__).resolve().parent / "storage"
CONFIG_PATH = Path(__file__).resolve().parent / "config.yaml"
SETTINGS_PATH = Path(__file__).resolve().parent / "settings.json"
ALLOWED_EXTENSIONS = {"mp4", "mov", "avi", "mkv", "webm"}

# Progress for store jobs: job_id -> { "phase", "progress", "message" }
store_progress: dict[str, dict] = {}

# T2V model download: "status" (idle|downloading|done|error), "message", "model_id"
t2v_download_status: dict[str, str | float] = {"status": "idle", "message": "", "model_id": ""}

# Optional overrides from CLI (set by main())
_script_backend_override: str | None = None
_device_override: str | None = None

log = logging.getLogger("video_storage_tool.server")

app = Flask(__name__, template_folder="templates")
app.config["MAX_CONTENT_LENGTH"] = 1024 * 1024 * 1024  # 1 GB max upload


def _storage_dir(job_id: str) -> Path:
    return STORAGE_ROOT / job_id


def _deep_merge(base: dict, override: dict) -> dict:
    """Merge override into base recursively. Mutates base."""
    for k, v in override.items():
        if k in base and isinstance(base[k], dict) and isinstance(v, dict):
            _deep_merge(base[k], v)
        else:
            base[k] = v
    return base


def _config() -> dict:
    cfg = cli._load_config(CONFIG_PATH)
    if SETTINGS_PATH.exists():
        try:
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                overrides = json.load(f)
            if overrides:
                _deep_merge(cfg, overrides)
        except Exception:
            pass
    if cfg.get("device"):
        cfg.setdefault("t2v", {})["device"] = cfg.get("device")
    return cfg


def _run_store(input_path: Path, out_dir: Path, job_id: str, *, force_script: bool = False) -> None:
    short_id = job_id[:8] if len(job_id) >= 8 else job_id

    def progress_cb(phase: str, progress: float, message: str) -> None:
        store_progress[job_id] = {"phase": phase, "progress": progress, "message": message}
        log.info("[store %s] %s (%.0f%%) %s", short_id, phase, progress * 100, message)

    log.info("[store %s] Starting store pipeline: %s (force_script=%s)", short_id, input_path.name, force_script)
    try:
        cfg = _config()
        if _device_override:
            cfg["device"] = _device_override
            cfg.setdefault("t2v", {})["device"] = _device_override
        t2v = cfg.get("t2v", {})
        script_cfg = cfg.get("script", {})
        script_backend = _script_backend_override or script_cfg.get("backend", "whisper")
        cli.run_store(
            input_path,
            out_dir,
            config=cfg,
            t2v_backend=t2v.get("backend", "stub"),
            t2v_model_path=t2v.get("model_path"),
            t2v_model_id=t2v.get("model_id"),
            script_backend=script_backend,
            progress_callback=progress_cb,
            force_script=force_script,
        )
        log.info("[store %s] Done.", short_id)
    except Exception as e:
        log.exception("[store %s] Failed: %s", short_id, e)
    finally:
        store_progress.pop(job_id, None)


@app.route("/")
def index():
    return send_file(Path(__file__).resolve().parent / "templates" / "index.html", mimetype="text/html")


@app.route("/api/store", methods=["POST"])
def api_store():
    if "video" not in request.files and "file" not in request.files:
        return jsonify({"error": "No video file"}), 400
    f = request.files.get("video") or request.files.get("file")
    if not f or f.filename == "":
        return jsonify({"error": "Empty filename"}), 400
    ext = (Path(f.filename).suffix or "").lstrip(".").lower()
    if ext not in ALLOWED_EXTENSIONS:
        return jsonify({"error": f"Unsupported format. Allowed: {ALLOWED_EXTENSIONS}"}), 400
    job_id = str(uuid.uuid4())
    st = _storage_dir(job_id)
    st.mkdir(parents=True, exist_ok=True)
    input_path = st / "input.mp4"
    f.save(str(input_path))
    log.info("[store %s] Upload saved, starting pipeline: %s", job_id[:8], f.filename)
    threading.Thread(target=_run_store, args=(input_path, st, job_id), daemon=True).start()
    return jsonify({"id": job_id, "status": "processing"}), 202


@app.route("/api/settings", methods=["GET"])
def api_settings_get():
    """Return effective editable settings (from merged config)."""
    cfg = _config()
    return jsonify({
        "device": cfg.get("device") or cfg.get("t2v", {}).get("device") or "auto",
        "t2v": {
            "backend": cfg.get("t2v", {}).get("backend", "stub"),
            "model_id": cfg.get("t2v", {}).get("model_id") or "",
            "model_path": cfg.get("t2v", {}).get("model_path") or "",
        },
        "script": {
            "model": cfg.get("script", {}).get("model", "base"),
            "visual_backend": cfg.get("script", {}).get("visual_backend", "none"),
            "visual_interval_sec": cfg.get("script", {}).get("visual_interval_sec", 1.0),
            "visual_max_frames": cfg.get("script", {}).get("visual_max_frames", 60),
            "visual_grid": cfg.get("script", {}).get("visual_grid", 2),
        },
        "audio": {
            "ffmpeg_path": cfg.get("audio", {}).get("ffmpeg_path") or "",
        },
    })


def _run_t2v_download(model_id: str) -> None:
    """Background: download T2V model from Hugging Face Hub (e.g. ~14GB for CogVideoX-2b). Resume supported."""
    global t2v_download_status
    t2v_download_status = {"status": "downloading", "message": f"Downloading {model_id}… (resume supported if interrupted)", "model_id": model_id}
    log.info("[t2v download] Starting download of %s (may be 10+ GB; resume supported)", model_id)
    try:
        import os
        # Reduce console spam from tqdm progress bars (optional)
        prev = os.environ.get("HF_HUB_DISABLE_TQDM")
        os.environ["HF_HUB_DISABLE_TQDM"] = "0"  # keep tqdm for now so user sees progress in terminal
        try:
            from huggingface_hub import snapshot_download
            snapshot_download(repo_id=model_id, resume_download=True)
        finally:
            if prev is None:
                os.environ.pop("HF_HUB_DISABLE_TQDM", None)
            else:
                os.environ["HF_HUB_DISABLE_TQDM"] = prev
        t2v_download_status = {"status": "done", "message": f"Downloaded {model_id}", "model_id": model_id}
        log.info("[t2v download] Done: %s", model_id)
    except ImportError:
        t2v_download_status = {"status": "error", "message": "huggingface_hub not installed (pip install huggingface_hub)", "model_id": model_id}
        log.warning("[t2v download] huggingface_hub not installed")
    except Exception as e:
        t2v_download_status = {"status": "error", "message": f"Download failed: {e}. Click Download again to resume.", "model_id": model_id}
        log.exception("[t2v download] Failed: %s", e)


@app.route("/api/t2v/download", methods=["POST"])
def api_t2v_download():
    """Start background download of the configured T2V model (e.g. CogVideoX-2b, ~3GB). Save settings first."""
    cfg = _config()
    model_id = (cfg.get("t2v") or {}).get("model_id") or ""
    if not model_id or not model_id.strip():
        return jsonify({"error": "No t2v.model_id configured. Set model ID in Settings and Save, then try again."}), 400
    model_id = model_id.strip()
    if t2v_download_status.get("status") == "downloading":
        return jsonify({"error": "Download already in progress", "model_id": t2v_download_status.get("model_id")}), 409
    threading.Thread(target=_run_t2v_download, args=(model_id,), daemon=True).start()
    return jsonify({"ok": True, "message": f"Download started for {model_id} (may take several minutes)", "model_id": model_id}), 202


@app.route("/api/t2v/download/status", methods=["GET"])
def api_t2v_download_status():
    """Return current T2V model download status (idle|downloading|done|error)."""
    return jsonify(dict(t2v_download_status))


@app.route("/api/settings", methods=["PUT", "POST"])
def api_settings_put():
    """Update settings.json (partial merge). Next store uses merged config."""
    data = request.get_json(silent=True)
    if not isinstance(data, dict):
        return jsonify({"error": "JSON object required"}), 400
    existing = {}
    if SETTINGS_PATH.exists():
        try:
            with open(SETTINGS_PATH, "r", encoding="utf-8") as f:
                existing = json.load(f)
            if not isinstance(existing, dict):
                existing = {}
        except Exception:
            existing = {}
    _deep_merge(existing, data)
    try:
        with open(SETTINGS_PATH, "w", encoding="utf-8") as f:
            json.dump(existing, f, indent=2)
    except Exception as e:
        return jsonify({"error": str(e)}), 500
    return jsonify({"ok": True})


@app.route("/api/stored", methods=["GET"])
def api_stored_list():
    STORAGE_ROOT.mkdir(parents=True, exist_ok=True)
    items = []
    for p in sorted(STORAGE_ROOT.iterdir(), key=lambda x: x.name):
        if not p.is_dir() or not (p / "input.mp4").exists():
            continue
        status = "ready" if (p / "manifest.json").exists() else "incomplete"
        items.append({"id": p.name, "status": status})
    return jsonify({"items": items, "ids": [x["id"] for x in items if x["status"] == "ready"]})


@app.route("/api/stored/<job_id>/retry", methods=["POST"])
def api_stored_retry(job_id):
    """Re-run the store pipeline for this job. Skips steps whose outputs already exist (e.g. after a crash).
    Body or query: force_script=true to regenerate script.txt (and resultant) with current settings."""
    st = _storage_dir(job_id)
    input_path = st / "input.mp4"
    if not st.is_dir() or not input_path.is_file():
        return jsonify({"error": "Job not found or input.mp4 missing"}), 404
    if store_progress.get(job_id):
        return jsonify({"error": "Job already in progress", "id": job_id}), 409
    data = request.get_json(silent=True) or {}
    force_script = data.get("force_script") or request.args.get("force_script", "").lower() in ("1", "true", "yes")
    short_id = job_id[:8] if len(job_id) >= 8 else job_id
    log.info("[store %s] Retry requested (force_script=%s)", short_id, force_script)
    threading.Thread(target=_run_store, args=(input_path, st, job_id), kwargs={"force_script": force_script}, daemon=True).start()
    msg = "Regenerating script and resultant." if force_script else "Retry started; cached steps will be skipped."
    return jsonify({"id": job_id, "status": "processing", "message": msg}), 202


@app.route("/api/stored/<job_id>/status", methods=["GET"])
def api_stored_status(job_id):
    st = _storage_dir(job_id)
    if not st.is_dir():
        return jsonify({"error": "Not found"}), 404
    manifest_path = st / "manifest.json"
    if manifest_path.exists():
        with open(manifest_path, "r", encoding="utf-8") as f:
            manifest = json.load(f)
        return jsonify({"status": "ready", "manifest": manifest})
    prog = store_progress.get(job_id, {})
    out = {"status": "processing"}
    if prog:
        out["progress"] = prog.get("progress")
        out["phase"] = prog.get("phase")
        out["message"] = prog.get("message")
    return jsonify(out)


@app.route("/api/reconstitute", methods=["POST"])
def api_reconstitute():
    data = request.get_json(silent=True) or {}
    job_id = data.get("stored_id") or data.get("id")
    use_original = bool(data.get("original", False))
    if not job_id:
        return jsonify({"error": "Missing stored_id"}), 400
    st = _storage_dir(job_id)
    if not st.is_dir() or not (st / "manifest.json").exists():
        return jsonify({"error": "Stored job not found or not ready"}), 404
    out_name = "reconstituted_original.mp4" if use_original else "reconstituted.mp4"
    out_path = st / out_name
    short_id = job_id[:8] if len(job_id) >= 8 else job_id
    log.info("[reconstitute %s] Starting (original=%s) -> %s", short_id, use_original, out_name)
    try:
        reconstitute(
            st,
            out_path,
            use_diff=use_original,
            ffmpeg_path=_config().get("audio", {}).get("ffmpeg_path"),
        )
        log.info("[reconstitute %s] Done.", short_id)
    except FileNotFoundError as e:
        log.warning("[reconstitute %s] Not found: %s", short_id, e)
        return jsonify({"error": str(e)}), 400
    except Exception as e:
        log.exception("[reconstitute %s] Failed: %s", short_id, e)
        return jsonify({"error": str(e)}), 500
    stream_url = f"/stream/{job_id}?original={1 if use_original else 0}"
    return jsonify({"stream_url": stream_url, "out_path": out_name})


def _parse_range(header: str | None, total: int) -> tuple[int, int] | None:
    """Parse Range header. Returns (start, end) inclusive, or None if invalid/unsatisfiable."""
    if not header or not header.strip().lower().startswith("bytes="):
        return None
    m = re.match(r"bytes=(\d*)-(\d*)", header.strip(), re.I)
    if not m:
        return None
    s, e = m.group(1), m.group(2)
    start = int(s) if s else 0
    end = int(e) if e else total - 1
    if start > end or start >= total:
        return None
    end = min(end, total - 1)
    return start, end


def _stream_file_with_progress(
    file_path: Path,
    *,
    start: int = 0,
    end: int | None = None,
    total: int | None = None,
    log_progress: bool = True,
):
    """Yield file chunks from start to end (inclusive). total = file size. Log progress to stderr."""
    size = file_path.stat().st_size
    total = total or size
    if end is None:
        end = size - 1
    content_length = end - start + 1
    chunk_size = 256 * 1024
    sent = 0
    last_pct_logged = -1

    try:
        with open(file_path, "rb") as f:
            f.seek(start)
            while sent < content_length:
                to_read = min(chunk_size, content_length - sent)
                data = f.read(to_read)
                if not data:
                    break
                sent += len(data)
                if log_progress:
                    pct = int((sent / content_length) * 100)
                    if pct >= last_pct_logged + 10 or pct == 100:
                        last_pct_logged = pct
                        mb = sent / (1024 * 1024)
                        total_mb = content_length / (1024 * 1024)
                        print(f"[stream] {pct}% ({mb:.1f} / {total_mb:.1f} MB)", file=sys.stderr, flush=True)
                yield data
    finally:
        if log_progress and sent > 0:
            print(f"[stream] done ({sent} bytes)", file=sys.stderr, flush=True)


@app.route("/api/stream/<job_id>/info", methods=["GET"])
def stream_info(job_id):
    """Return content length and filename for the stream so the client can show a download progress bar."""
    use_original = request.args.get("original", "0") == "1"
    st = _storage_dir(job_id)
    if not st.is_dir():
        return jsonify({"error": "Not found"}), 404
    out_name = "reconstituted_original.mp4" if use_original else "reconstituted.mp4"
    file_path = st / out_name
    if not file_path.is_file():
        return jsonify({"error": "Not found"}), 404
    size = file_path.stat().st_size
    return jsonify({
        "content_length": size,
        "filename": out_name,
        "original": use_original,
    })


@app.route("/stream/<job_id>", methods=["GET"])
def stream(job_id):
    use_original = request.args.get("original", "0") == "1"
    st = _storage_dir(job_id)
    if not st.is_dir():
        abort(404)
    out_name = "reconstituted_original.mp4" if use_original else "reconstituted.mp4"
    file_path = st / out_name
    if not file_path.is_file():
        abort(404)
    size = file_path.stat().st_size
    range_header = request.headers.get("Range")
    start, end = 0, size - 1
    use_range = False
    if range_header:
        parsed = _parse_range(range_header, size)
        if parsed:
            start, end = parsed
            use_range = True
    content_length = end - start + 1

    def generate():
        return _stream_file_with_progress(
            file_path,
            start=start,
            end=end,
            total=size,
            log_progress=True,
        )

    resp = Response(generate(), status=206 if use_range else 200, mimetype="video/mp4")
    resp.headers["Accept-Ranges"] = "bytes"
    resp.headers["Content-Length"] = str(content_length)
    if use_range:
        resp.headers["Content-Range"] = f"bytes {start}-{end}/{size}"
    return resp


def main():
    import argparse
    p = argparse.ArgumentParser(description="Video store/reconstitute server")
    p.add_argument("--host", default="127.0.0.1", help="Bind host")
    p.add_argument("--port", type=int, default=5000, help="Bind port")
    p.add_argument("--debug", action="store_true", help="Flask debug")
    p.add_argument("--script-backend", default=None, help="Override script backend: whisper or stub (default from config)")
    p.add_argument("--device", default=None, choices=["auto", "cuda", "cpu"], help="Override device for T2V/BLIP: auto (default), cuda, or cpu")
    p.add_argument("--log-status-polls", action="store_true", help="Log every GET /api/stored/<id>/status (default: quiet to avoid spam)")
    args = p.parse_args()
    STORAGE_ROOT.mkdir(parents=True, exist_ok=True)

    global _script_backend_override, _device_override
    _script_backend_override = args.script_backend
    _device_override = args.device

    logging.basicConfig(
        level=logging.INFO,
        format="%(levelname)s: %(message)s",
        stream=sys.stderr,
    )

    if not args.log_status_polls:
        class QuietStatusFilter(logging.Filter):
            def filter(self, record):
                try:
                    msg = record.getMessage()
                except Exception:
                    msg = getattr(record, "msg", "") or ""
                if "/api/stored/" in msg and "/status" in msg and "GET" in msg:
                    return False
                return True
        logging.getLogger("werkzeug").addFilter(QuietStatusFilter())

    def _on_sigint(*_):
        log.info("Shutting down (Ctrl+C).")
        sys.exit(0)

    try:
        signal.signal(signal.SIGINT, _on_sigint)
    except (ValueError, OSError):
        pass  # main thread only / not available on all platforms

    try:
        app.run(host=args.host, port=args.port, debug=args.debug, threaded=True)
    except KeyboardInterrupt:
        log.info("Shutting down (Ctrl+C).")
        sys.exit(0)


if __name__ == "__main__":
    main()
