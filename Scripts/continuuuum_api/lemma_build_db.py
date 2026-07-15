"""Lemma Build settings / session schema helpers."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent
PREFACE_DIR = Path(__file__).resolve().parent / "data" / "lemma_build_preface"

ENGINES = [
    {"id": "unity", "label": "Unity", "enabled": True},
    {"id": "haxe", "label": "Haxe", "enabled": True},
    {"id": "unreal", "label": "Unreal", "enabled": False},
]

ENGINE_PREFACE = {
    "unity": "LemmaBuildEngineUnity.md",
    "haxe": "LemmaBuildEngineHaxe.md",
    "unreal": "LemmaBuildEngineUnreal.md",
}


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def new_id(prefix: str = "lbs") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_lemma_build_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "lemma_build_settings"):
        sql = (_SCHEMA_ROOT / "continuuuum_lemma_build_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    # seed default row
    row = conn.execute(
        "SELECT tenant_id FROM lemma_build_settings WHERE tenant_id = ?",
        ("default",),
    ).fetchone()
    if row is None:
        conn.execute(
            """INSERT INTO lemma_build_settings
               (tenant_id, lm_studio_base_url, default_model_id, max_concurrent_builds,
                batch_output_dir, default_engine, updated_at, updated_by)
               VALUES (?,?,?,?,?,?,?,?)""",
            (
                "default",
                "http://localhost:1234/v1",
                "mistralai/codestral-22b-v0.1",
                1,
                "Library/LemmaBuild/batches",
                "unity",
                _now(),
                None,
            ),
        )
    conn.commit()


def get_settings(conn: sqlite3.Connection, tenant_id: str = "default") -> dict:
    ensure_lemma_build_schema(conn)
    row = conn.execute(
        "SELECT * FROM lemma_build_settings WHERE tenant_id = ?",
        (tenant_id,),
    ).fetchone()
    if row is None:
        ensure_lemma_build_schema(conn)
        row = conn.execute(
            "SELECT * FROM lemma_build_settings WHERE tenant_id = ?",
            ("default",),
        ).fetchone()
    d = dict(row)
    return {
        "tenantId": d["tenant_id"],
        "lmStudioBaseUrl": d["lm_studio_base_url"],
        "defaultModelId": d["default_model_id"],
        "maxConcurrentBuilds": int(d["max_concurrent_builds"]),
        "batchOutputDir": d["batch_output_dir"],
        "defaultEngine": d["default_engine"],
        "updatedAt": d.get("updated_at"),
        "updatedBy": d.get("updated_by"),
        "engines": ENGINES,
    }


def put_settings(conn: sqlite3.Connection, tenant_id: str, body: dict, updated_by: str | None) -> dict:
    ensure_lemma_build_schema(conn)
    cur = get_settings(conn, tenant_id)
    engine = (body.get("defaultEngine") or body.get("default_engine") or cur["defaultEngine"]).strip().lower()
    if engine == "unreal":
        raise ValueError("default_engine unreal is disabled")
    if engine not in ("unity", "haxe"):
        raise ValueError("default_engine must be unity or haxe")
    max_c = int(body.get("maxConcurrentBuilds", body.get("max_concurrent_builds", cur["maxConcurrentBuilds"])))
    max_c = max(0, min(16, max_c))
    base = body.get("lmStudioBaseUrl") or body.get("lm_studio_base_url") or cur["lmStudioBaseUrl"]
    model = body.get("defaultModelId") or body.get("default_model_id") or cur["defaultModelId"]
    batch = body.get("batchOutputDir") or body.get("batch_output_dir") or cur["batchOutputDir"]
    now = _now()
    conn.execute(
        """INSERT INTO lemma_build_settings
           (tenant_id, lm_studio_base_url, default_model_id, max_concurrent_builds,
            batch_output_dir, default_engine, updated_at, updated_by)
           VALUES (?,?,?,?,?,?,?,?)
           ON CONFLICT(tenant_id) DO UPDATE SET
             lm_studio_base_url=excluded.lm_studio_base_url,
             default_model_id=excluded.default_model_id,
             max_concurrent_builds=excluded.max_concurrent_builds,
             batch_output_dir=excluded.batch_output_dir,
             default_engine=excluded.default_engine,
             updated_at=excluded.updated_at,
             updated_by=excluded.updated_by""",
        (tenant_id, base, model, max_c, batch, engine, now, updated_by),
    )
    conn.commit()
    return get_settings(conn, tenant_id)


def normalize_engine(engine: str | None, default: str = "unity") -> str:
    e = (engine or default or "unity").strip().lower()
    if e == "unreal":
        raise ValueError("engine unreal is disabled")
    if e not in ("unity", "haxe"):
        raise ValueError("engine must be unity or haxe")
    return e


def load_system_preface(engine: str) -> str:
    base = (PREFACE_DIR / "LemmaBuildSystemPreface.md").read_text(encoding="utf-8")
    appendix_name = ENGINE_PREFACE.get(engine, ENGINE_PREFACE["unity"])
    appendix_path = PREFACE_DIR / appendix_name
    appendix = appendix_path.read_text(encoding="utf-8") if appendix_path.is_file() else ""
    return base.rstrip() + "\n\n" + appendix.strip() + "\n"
