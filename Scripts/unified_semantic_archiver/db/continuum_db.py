"""
Unified Semantic Archiver - continuum database micro ORM.
Lightweight CRUD over SQLite; no heavy migrations.
"""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any

_SCHEMA_PATH = Path(__file__).resolve().parent / "schema.sql"


def get_connection(db_path: str | Path) -> sqlite3.Connection:
    """Open connection to continuum DB; ensures schema exists."""
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    return conn


def init_schema(conn: sqlite3.Connection) -> None:
    """Run schema.sql to create tables if not exist."""
    with open(_SCHEMA_PATH, encoding="utf-8") as f:
        conn.executescript(f.read())
    conn.commit()


class ContinuumDb:
    """Micro ORM for continuum SQLite database."""

    def __init__(self, db_path: str | Path):
        self.db_path = Path(db_path)
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        conn = get_connection(self.db_path)
        init_schema(conn)
        conn.close()

    def _conn(self) -> sqlite3.Connection:
        return get_connection(self.db_path)

    # --- continuum_meta ---
    def meta_get(self, key: str) -> str | None:
        with self._conn() as c:
            row = c.execute("SELECT value FROM continuum_meta WHERE key = ?", (key,)).fetchone()
            return row["value"] if row else None

    def meta_set(self, key: str, value: str) -> None:
        with self._conn() as c:
            c.execute(
                "INSERT OR REPLACE INTO continuum_meta (key, value, updated_at) VALUES (?, ?, datetime('now'))",
                (key, value),
            )
            c.commit()

    # --- spatial_4d ---
    def spatial_4d_insert(self, bounds4_json: str, payload_type: str | None = None, payload_id: int | None = None) -> int:
        with self._conn() as c:
            cur = c.execute(
                "INSERT INTO spatial_4d (bounds4_json, payload_type, payload_id) VALUES (?, ?, ?)",
                (bounds4_json, payload_type, payload_id),
            )
            c.commit()
            return cur.lastrowid

    def spatial_4d_list(self, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            rows = c.execute("SELECT * FROM spatial_4d ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    # --- document_blobs ---
    def document_blob_insert(self, tar_hash: str, path: str, mime_type: str | None = None) -> int:
        with self._conn() as c:
            cur = c.execute(
                "INSERT INTO document_blobs (tar_hash, path, mime_type) VALUES (?, ?, ?)",
                (tar_hash, path, mime_type),
            )
            c.commit()
            return cur.lastrowid

    def document_blob_list(self, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            rows = c.execute("SELECT * FROM document_blobs ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    # --- semantic_chunks ---
    def semantic_chunk_insert(
        self,
        media_type: str,
        chunk_key: str,
        description_text: str | None = None,
        diff_blob_ref: str | None = None,
        parent_id: int | None = None,
        quad_path: str | None = None,
    ) -> int:
        with self._conn() as c:
            cur = c.execute(
                """INSERT INTO semantic_chunks (media_type, chunk_key, description_text, diff_blob_ref, parent_id, quad_path)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (media_type, chunk_key, description_text, diff_blob_ref, parent_id, quad_path),
            )
            c.commit()
            return cur.lastrowid

    def semantic_chunk_list(self, media_type: str | None = None, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            if media_type:
                rows = c.execute(
                    "SELECT * FROM semantic_chunks WHERE media_type = ? ORDER BY id DESC LIMIT ?",
                    (media_type, limit),
                ).fetchall()
            else:
                rows = c.execute("SELECT * FROM semantic_chunks ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    # --- unique_kernels ---
    def unique_kernel_insert(
        self,
        chunk_id: int,
        source_compressor: str,
        residual_metric: float | None = None,
        attempt_count: int = 0,
        status: str = "pending",
    ) -> int:
        with self._conn() as c:
            cur = c.execute(
                """INSERT INTO unique_kernels (chunk_id, source_compressor, residual_metric, attempt_count, status)
                   VALUES (?, ?, ?, ?, ?)""",
                (chunk_id, source_compressor, residual_metric, attempt_count, status),
            )
            c.commit()
            return cur.lastrowid

    def unique_kernel_list(self, status: str | None = None, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            if status:
                rows = c.execute(
                    "SELECT * FROM unique_kernels WHERE status = ? ORDER BY id DESC LIMIT ?",
                    (status, limit),
                ).fetchall()
            else:
                rows = c.execute("SELECT * FROM unique_kernels ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    def unique_kernel_update_status(self, kernel_id: int, status: str, residual_metric: float | None = None) -> None:
        with self._conn() as c:
            if residual_metric is not None:
                c.execute(
                    "UPDATE unique_kernels SET status = ?, residual_metric = ?, attempt_count = attempt_count + 1 WHERE id = ?",
                    (status, residual_metric, kernel_id),
                )
            else:
                c.execute(
                    "UPDATE unique_kernels SET status = ?, attempt_count = attempt_count + 1 WHERE id = ?",
                    (status, kernel_id),
                )
            c.commit()

    # --- compression_runs ---
    def compression_run_insert(
        self,
        strategy: str,
        media_id: int | None = None,
        config_json: str | None = None,
        output_hash: str | None = None,
    ) -> int:
        with self._conn() as c:
            cur = c.execute(
                "INSERT INTO compression_runs (media_id, strategy, config_json, output_hash) VALUES (?, ?, ?, ?)",
                (media_id, strategy, config_json, output_hash),
            )
            c.commit()
            return cur.lastrowid

    def compression_run_list(self, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            rows = c.execute("SELECT * FROM compression_runs ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    # --- research_suggestions ---
    def research_suggestion_insert(
        self,
        source: str,
        recommendation_text: str,
        context_json: str | None = None,
        status: str = "pending",
    ) -> int:
        with self._conn() as c:
            cur = c.execute(
                "INSERT INTO research_suggestions (source, context_json, recommendation_text, status) VALUES (?, ?, ?, ?)",
                (source, context_json, recommendation_text, status),
            )
            c.commit()
            return cur.lastrowid

    def research_suggestion_list(self, limit: int = 100) -> list[dict[str, Any]]:
        with self._conn() as c:
            rows = c.execute("SELECT * FROM research_suggestions ORDER BY id DESC LIMIT ?", (limit,)).fetchall()
            return [dict(r) for r in rows]

    # --- raw SQL for explorer window ---
    def execute_read(self, sql: str, params: tuple[Any, ...] = ()) -> list[dict[str, Any]]:
        """Run read-only SQL; returns list of row dicts."""
        with self._conn() as c:
            rows = c.execute(sql, params).fetchall()
            return [dict(r) for r in rows]
