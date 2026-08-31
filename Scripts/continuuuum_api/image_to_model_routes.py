"""Image-to-model / Modly: artwork_media on disk + Flask APIs.

Canonical: /api/image-to-model/*
Aliases: /api/video-generation/* (Unity ImageToModelWindow).
Modly weights are not vendored — set MODLY_ROOT to a local checkout.
"""

from __future__ import annotations

import json
import os
import sqlite3
import subprocess
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from flask import jsonify, request, send_file, send_from_directory

GetConn = Callable[[], sqlite3.Connection]

FACES = ("north", "south", "east", "west", "up", "down")

MINECRAFT_GRAN = {
    "preset": "minecraft",
    "pixelGrid": 16,
    "blockMeters": 1.0,
    "texelsPerMeter": 16,
    "voxelCell": 0.0625,
    "skinLayout": "64x64",
    "maxBones": 33,
    "snapToGrid": True,
}

CONTINUUUUM_GRAN = {
    "preset": "continuuuum",
    "pixelGrid": 16,
    "blockMeters": 1.0,
    "texelsPerMeter": 16,
    "voxelCell": 0.0625,
    "skinLayout": "custom",
    "maxBones": 256,
    "snapToGrid": False,
}

SCHEMA = """
CREATE TABLE IF NOT EXISTS artwork_media (
  artwork_id TEXT NOT NULL,
  t INTEGER NOT NULL DEFAULT -1,
  kind TEXT NOT NULL,
  mime TEXT,
  width INTEGER,
  height INTEGER,
  blob_ref TEXT,
  granularity_json TEXT,
  axis_json TEXT,
  library_doc_id TEXT,
  created_at TEXT NOT NULL,
  PRIMARY KEY (artwork_id, t, kind)
);
CREATE INDEX IF NOT EXISTS idx_artwork_media_art ON artwork_media(artwork_id);
"""


def ensure_artwork_media_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(SCHEMA)
    conn.commit()


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _num(v: Any, default: float = 0.0) -> float:
    try:
        return float(v)
    except (TypeError, ValueError):
        return default


def normalize_granularity(raw: Any) -> dict[str, Any]:
    src = raw if isinstance(raw, dict) else {}
    if isinstance(raw, str) and raw.strip():
        try:
            parsed = json.loads(raw)
            if isinstance(parsed, dict):
                src = parsed
        except json.JSONDecodeError:
            src = {}
    g = dict(MINECRAFT_GRAN)
    if src:
        for key in (
            "pixelGrid",
            "blockMeters",
            "texelsPerMeter",
            "voxelCell",
            "skinLayout",
            "maxBones",
            "snapToGrid",
            "preset",
        ):
            if key in src and src[key] is not None:
                g[key] = src[key]
    if "blocksPerMeter" in src and src.get("blockMeters") is None:
        bpm = _num(src.get("blocksPerMeter"), 1.0)
        if bpm > 0:
            g["blockMeters"] = 1.0 / bpm
    g["pixelGrid"] = int(_num(g.get("pixelGrid"), 16))
    g["blockMeters"] = _num(g.get("blockMeters"), 1.0)
    if g["blockMeters"] <= 0:
        g["blockMeters"] = 1.0
    g["texelsPerMeter"] = int(_num(g.get("texelsPerMeter"), 16))
    g["voxelCell"] = _num(g.get("voxelCell"), 0.0625)
    g["maxBones"] = int(_num(g.get("maxBones"), 33))
    g["snapToGrid"] = bool(g.get("snapToGrid"))
    g["skinLayout"] = str(g.get("skinLayout") or "64x64")
    g["blocksPerMeter"] = 1.0 / g["blockMeters"]
    preset = str(g.get("preset") or "")
    if preset == "minecraft" and _matches(g, MINECRAFT_GRAN):
        g["preset"] = "minecraft"
    elif preset == "continuuuum" and _matches(g, CONTINUUUUM_GRAN):
        g["preset"] = "continuuuum"
    elif _matches(g, MINECRAFT_GRAN):
        g["preset"] = "minecraft"
    elif _matches(g, CONTINUUUUM_GRAN):
        g["preset"] = "continuuuum"
    else:
        g["preset"] = "custom"
    return g


def _matches(g: dict[str, Any], preset: dict[str, Any]) -> bool:
    return (
        int(g.get("pixelGrid") or 0) == int(preset["pixelGrid"])
        and abs(_num(g.get("blockMeters")) - float(preset["blockMeters"])) < 1e-6
        and int(g.get("texelsPerMeter") or 0) == int(preset["texelsPerMeter"])
        and abs(_num(g.get("voxelCell")) - float(preset["voxelCell"])) < 1e-6
        and str(g.get("skinLayout")) == str(preset["skinLayout"])
        and int(g.get("maxBones") or 0) == int(preset["maxBones"])
        and bool(g.get("snapToGrid")) == bool(preset["snapToGrid"])
    )


def _upload_root() -> Path:
    env = os.environ.get("CONTINUUUUM_LIBRARY_UPLOADS")
    if env:
        root = Path(env)
    else:
        drawer = Path(__file__).resolve().parents[2]
        root = drawer / "library_uploads"
    dest = root / "image-to-model"
    dest.mkdir(parents=True, exist_ok=True)
    return dest


def _modly_root() -> Path:
    return Path(os.environ.get("MODLY_ROOT", r"D:/Development/modly"))


def _mocap_root() -> Path:
    return Path(os.environ.get("MOCAPANYTHING_ROOT", r"D:/Development/MocapAnything"))


def _webgl_index() -> Path:
    env = os.environ.get("CONTINUUUUM_REPO", "")
    candidates = []
    if env:
        candidates.append(Path(env) / "library" / "continuuuum_editor_webgl" / "index.html")
    drawer = Path(__file__).resolve().parents[2]
    candidates.append(drawer / "continuuuum" / "library" / "continuuuum_editor_webgl" / "index.html")
    candidates.append(Path(r"C:\Users\John\continuuuum") / "library" / "continuuuum_editor_webgl" / "index.html")
    for p in candidates:
        if p.is_file():
            return p
    return candidates[0]


def _image_size(data: bytes) -> tuple[int | None, int | None]:
    try:
        from PIL import Image
        import io

        im = Image.open(io.BytesIO(data))
        return im.size[0], im.size[1]
    except Exception:
        return None, None


def _write_blob(artwork_id: str, kind: str, filename: str, data: bytes) -> str:
    ext = Path(filename).suffix.lower() or ".bin"
    safe_kind = "".join(c if c.isalnum() or c in "-_" else "_" for c in kind)
    name = f"{artwork_id}_{safe_kind}{ext}"
    path = _upload_root() / name
    path.write_bytes(data)
    return str(path)


def _upsert_row(
    conn: sqlite3.Connection,
    artwork_id: str,
    t: int,
    kind: str,
    mime: str | None,
    width: int | None,
    height: int | None,
    blob_ref: str,
    gran_json: str,
    axis_json: str | None,
    library_doc_id: str | None,
) -> dict[str, Any]:
    conn.execute(
        """
        INSERT INTO artwork_media
          (artwork_id, t, kind, mime, width, height, blob_ref, granularity_json, axis_json, library_doc_id, created_at)
        VALUES (?,?,?,?,?,?,?,?,?,?,?)
        ON CONFLICT(artwork_id, t, kind) DO UPDATE SET
          mime = excluded.mime,
          width = excluded.width,
          height = excluded.height,
          blob_ref = excluded.blob_ref,
          granularity_json = excluded.granularity_json,
          axis_json = excluded.axis_json,
          library_doc_id = COALESCE(excluded.library_doc_id, artwork_media.library_doc_id)
        """,
        (
            artwork_id,
            t,
            kind,
            mime,
            width,
            height,
            blob_ref,
            gran_json,
            axis_json,
            library_doc_id,
            _now(),
        ),
    )
    conn.commit()
    return {
        "artworkId": artwork_id,
        "t": t,
        "kind": kind,
        "mime": mime,
        "width": width,
        "height": height,
        "blobRef": blob_ref,
        "bytes": Path(blob_ref).stat().st_size if blob_ref and Path(blob_ref).is_file() else 0,
        "granularityJson": gran_json,
        "axisJson": axis_json,
        "libraryDocId": library_doc_id,
    }


def _maybe_library_doc(
    conn: sqlite3.Connection,
    artwork_id: str,
    blob_ref: str,
    mime: str | None,
    gran: dict[str, Any],
) -> str | None:
    try:
        conn.execute("SELECT 1 FROM library_documents LIMIT 1")
    except sqlite3.OperationalError:
        return None
    meta = json.dumps(
        {
            "artworkId": artwork_id,
            "kind": "image_to_model",
            "title": artwork_id,
            "granularity": gran,
            "mime": mime,
        }
    )
    try:
        cur = conn.execute(
            """
            INSERT INTO library_documents
              (document_type, blob_ref, type_metadata, tenant_id, updated_at)
            VALUES ('image', ?, ?, 'default', datetime('now'))
            """,
            (blob_ref, meta),
        )
        conn.commit()
        return str(cur.lastrowid)
    except sqlite3.Error:
        return None


def list_media(conn: sqlite3.Connection, artwork_id: str) -> list[dict[str, Any]]:
    rows = conn.execute(
        """
        SELECT artwork_id, t, kind, mime, width, height, blob_ref, granularity_json, axis_json,
               library_doc_id, created_at
        FROM artwork_media WHERE artwork_id = ? ORDER BY t, kind
        """,
        (artwork_id,),
    ).fetchall()
    out = []
    for r in rows:
        ref = r["blob_ref"]
        nbytes = Path(ref).stat().st_size if ref and Path(ref).is_file() else 0
        out.append(
            {
                "artworkId": r["artwork_id"],
                "t": r["t"],
                "kind": r["kind"],
                "mime": r["mime"],
                "width": r["width"],
                "height": r["height"],
                "blobRef": ref,
                "bytes": nbytes,
                "granularityJson": r["granularity_json"],
                "axisJson": r["axis_json"],
                "libraryDocId": r["library_doc_id"],
                "createdAt": r["created_at"],
            }
        )
    return out


def get_media_row(conn: sqlite3.Connection, artwork_id: str, t: int, kind: str) -> dict[str, Any] | None:
    row = conn.execute(
        """
        SELECT artwork_id, t, kind, mime, width, height, blob_ref, granularity_json, axis_json,
               library_doc_id, created_at
        FROM artwork_media WHERE artwork_id = ? AND t = ? AND kind = ?
        """,
        (artwork_id, t, kind),
    ).fetchone()
    if row is None:
        return None
    return dict(row)


def features_payload() -> dict[str, Any]:
    modly = _modly_root()
    mocap = _mocap_root()
    webgl = _webgl_index()
    build_dir = webgl.parent / "Build"
    return {
        "features": [
            {
                "id": "modly@local",
                "label": "Modly image-to-model",
                "kind": "mesh",
                "hint": "https://github.com/lightningpixel/modly — not vendored",
                "available": modly.is_dir(),
            },
            {
                "id": "mediapipe_holistic@v1",
                "label": "MediaPipe Holistic",
                "kind": "pose",
                "hint": "Humanoid pose. Do not vendor .tflite",
                "available": True,
            },
            {
                "id": "mocapanything@v2",
                "label": "MoCapAnything v2",
                "kind": "pose",
                "hint": str(mocap),
                "available": mocap.is_dir(),
            },
            {
                "id": "pixellight",
                "label": "PixelLight voxel skin",
                "kind": "voxel",
                "hint": "Axis faces bind as textures; pixelGrid from granularity",
                "available": True,
            },
            {
                "id": "voxel-ragdoll",
                "label": "VoxelRagdollActor",
                "kind": "actor",
                "hint": "Minecraft-scale analog; 1 block = 1 m unless blockMeters is edited",
                "available": True,
            },
        ],
        "webglPreview": "/continuuuum_editor/index.html",
        "webglBuild": build_dir.is_dir(),
        "modlyRoot": str(modly),
        "granularityMinecraft": {**MINECRAFT_GRAN, "blocksPerMeter": 1.0},
        "granularityContinuuuum": {**CONTINUUUUM_GRAN, "blocksPerMeter": 1.0},
    }


def invoke_modly(conn: sqlite3.Connection, artwork_id: str, t: int, prompt: str | None, mesh_format: str, steps: int | None) -> dict[str, Any]:
    out: dict[str, Any] = {"artworkId": artwork_id}
    root = _modly_root()
    if not root.is_dir():
        out["ok"] = False
        out["available"] = False
        out["hint"] = "Set MODLY_ROOT; Modly is not vendored. https://github.com/lightningpixel/modly"
        return out
    row = get_media_row(conn, artwork_id, t, "source_image")
    if row is None or not row.get("blob_ref") or not Path(row["blob_ref"]).is_file():
        out["ok"] = False
        out["error"] = "no source_image for artwork"
        return out
    fmt = "".join(c for c in (mesh_format or "glb").lower() if c.isalnum()) or "glb"
    out_mesh = _upload_root() / f"{artwork_id}_generated_mesh.{fmt}"
    exe = root / "modly.exe"
    if not exe.is_file():
        exe = root / "modly"
    cmd = [str(exe) if exe.is_file() else "modly", "--image", row["blob_ref"], "--out", str(out_mesh)]
    if prompt:
        cmd.extend(["--prompt", prompt])
    if steps and steps > 0:
        cmd.extend(["--steps", str(steps)])
    try:
        proc = subprocess.run(cmd, cwd=str(root), capture_output=True, text=True, timeout=600)
        log = (proc.stdout or "") + (proc.stderr or "")
        out["exit"] = proc.returncode
        out["log"] = log[:4000]
        if proc.returncode == 0 and out_mesh.is_file():
            gran = row.get("granularity_json")
            saved = _upsert_row(
                conn,
                artwork_id,
                t,
                "generated_mesh",
                f"model/{fmt}",
                None,
                None,
                str(out_mesh),
                gran or json.dumps(MINECRAFT_GRAN),
                row.get("axis_json"),
                row.get("library_doc_id"),
            )
            out["ok"] = True
            out["bytes"] = saved["bytes"]
            out["kind"] = "generated_mesh"
        else:
            out["ok"] = False
            out["hint"] = f"Modly CLI did not write {out_mesh.name}"
    except Exception as exc:
        out["ok"] = False
        out["available"] = True
        out["error"] = str(exc)
        out["hint"] = f"Install Modly at {root}"
    return out


def register_image_to_model_routes(app: Any, get_conn: GetConn) -> None:
    static_dir = Path(__file__).resolve().parent / "static" / "image-to-model"

    def _conn() -> sqlite3.Connection:
        conn = get_conn()
        ensure_artwork_media_schema(conn)
        return conn

    @app.route("/image-to-model")
    @app.route("/image-to-model/")
    @app.route("/image-to-model/<path:subpath>")
    def image_to_model_spa(subpath: str = ""):
        if not static_dir.is_dir():
            return jsonify({"error": "image-to-model SPA missing"}), 404
        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")

    def features_get():
        return jsonify(features_payload())

    def gran_minecraft():
        return jsonify({**MINECRAFT_GRAN, "blocksPerMeter": 1.0})

    def gran_continuuuum():
        return jsonify({**CONTINUUUUM_GRAN, "blocksPerMeter": 1.0})

    def media_post():
        conn = _conn()
        artwork_id = (request.form.get("artworkId") or request.form.get("artwork_id") or "").strip()
        if not artwork_id:
            artwork_id = "art_" + uuid.uuid4().hex[:8]
        t = int(request.form.get("t") or -1)
        gran = normalize_granularity(request.form.get("granularity") or "{}")
        gran_json = json.dumps(gran)
        axis_raw = request.form.get("axis") or request.form.get("axis_json") or "{}"
        try:
            axis = json.loads(axis_raw) if axis_raw else {}
        except json.JSONDecodeError:
            axis = {}
        if not isinstance(axis, dict):
            axis = {}
        axis_json = json.dumps(axis)
        stored = []
        library_doc_id = None

        def save_upload(kind: str, fs) -> None:
            nonlocal library_doc_id
            if fs is None or not getattr(fs, "filename", None):
                return
            data = fs.read()
            if not data:
                return
            mime = fs.mimetype or "application/octet-stream"
            w, h = _image_size(data)
            ref = _write_blob(artwork_id, kind, fs.filename, data)
            if library_doc_id is None and kind == "source_image":
                library_doc_id = _maybe_library_doc(conn, artwork_id, ref, mime, gran)
            stored.append(
                _upsert_row(conn, artwork_id, t, kind, mime, w, h, ref, gran_json, axis_json, library_doc_id)
            )

        save_upload("source_image", request.files.get("image") or request.files.get("source_image"))
        save_upload("source_mask", request.files.get("mask") or request.files.get("source_mask"))
        for face in FACES:
            save_upload(f"face_{face}", request.files.get(f"face_{face}"))
            save_upload(f"mask_{face}", request.files.get(f"mask_{face}"))
        if not stored:
            return jsonify({"error": "image, mask, or face upload required"}), 400
        return jsonify(
            {
                "artworkId": artwork_id,
                "t": t,
                "granularity": gran,
                "axis": axis,
                "libraryDocId": library_doc_id,
                "stored": stored,
                "media": list_media(conn, artwork_id),
            }
        )

    def media_list(artwork_id: str):
        conn = _conn()
        return jsonify({"artworkId": artwork_id, "media": list_media(conn, artwork_id)})

    def media_blob(artwork_id: str, kind: str):
        conn = _conn()
        t = int(request.args.get("t") or -1)
        row = get_media_row(conn, artwork_id, t, kind)
        if row is None or not row.get("blob_ref") or not Path(row["blob_ref"]).is_file():
            return jsonify({"error": "media"}), 404
        mime = row.get("mime") or "application/octet-stream"
        return send_file(row["blob_ref"], mimetype=mime)

    def modly_post():
        body = request.get_json(silent=True) or {}
        artwork_id = str(body.get("artworkId") or body.get("artwork_id") or "").strip()
        if not artwork_id:
            return jsonify({"error": "artworkId"}), 400
        t = int(body.get("t") if body.get("t") is not None else -1)
        prompt = body.get("prompt")
        fmt = str(body.get("meshFormat") or body.get("mesh_format") or "glb")
        steps = body.get("steps")
        try:
            steps_i = int(steps) if steps is not None else None
        except (TypeError, ValueError):
            steps_i = None
        conn = _conn()
        return jsonify(invoke_modly(conn, artwork_id, t, prompt, fmt, steps_i))

    prefixes = (
        ("/api/image-to-model", "itm"),
        ("/api/video-generation", "vg"),
    )
    for prefix, tag in prefixes:
        app.add_url_rule(f"{prefix}/features", f"{tag}_features", features_get, methods=["GET"])
        app.add_url_rule(f"{prefix}/granularity/minecraft", f"{tag}_gran_mc", gran_minecraft, methods=["GET"])
        app.add_url_rule(f"{prefix}/granularity/continuuuum", f"{tag}_gran_cc", gran_continuuuum, methods=["GET"])
        app.add_url_rule(f"{prefix}/media", f"{tag}_media_post", media_post, methods=["POST"])
        app.add_url_rule(f"{prefix}/media/<artwork_id>", f"{tag}_media_list", media_list, methods=["GET"])
        app.add_url_rule(
            f"{prefix}/media/<artwork_id>/<kind>", f"{tag}_media_blob", media_blob, methods=["GET"]
        )
        app.add_url_rule(f"{prefix}/modly", f"{tag}_modly", modly_post, methods=["POST"])
