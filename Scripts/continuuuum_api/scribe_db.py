"""Scribe document configs, pages, and anchors."""

from __future__ import annotations

import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent

ALLOWED_FORMATS = ("plain", "markdown", "odt", "docx", "pdf", "lemma")
ALLOWED_ANCHOR_KINDS = ("lemma", "dialogue", "comment", "bookmark")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def ensure_scribe_schema(conn: sqlite3.Connection) -> None:
    if not _table_exists(conn, "localization_property_specs"):
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS localization_property_specs (
                key TEXT PRIMARY KEY,
                value_type TEXT NOT NULL,
                allowed_values_json TEXT,
                default_value TEXT,
                description TEXT
            )
            """
        )
    if not _table_exists(conn, "scribe_document_configs"):
        sql = (_SCHEMA_ROOT / "continuuuum_scribe_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    conn.commit()


def _row_config(row: sqlite3.Row | tuple) -> dict[str, Any]:
    return {
        "id": row[0],
        "libraryDocId": row[1],
        "title": row[2],
        "format": row[3],
        "formatOptionsJson": row[4],
        "peckingOrder": row[5],
        "tenant": row[6],
        "updatedAt": row[7],
    }


def upsert_config(
    conn: sqlite3.Connection,
    *,
    config_id: str | None,
    title: str,
    fmt: str = "plain",
    format_options_json: str | None = None,
    pecking_order: int = 20,
    tenant: str = "default",
    library_doc_id: str | None = None,
) -> dict[str, Any]:
    ensure_scribe_schema(conn)
    fmt = (fmt or "plain").strip().lower()
    if fmt not in ALLOWED_FORMATS:
        raise ValueError(f"unsupported_format:{fmt}")
    cid = config_id or str(uuid.uuid4())
    now = _now()
    conn.execute(
        """
        INSERT INTO scribe_document_configs
            (id, library_doc_id, title, format, format_options_json, pecking_order, tenant, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(id) DO UPDATE SET
            library_doc_id=excluded.library_doc_id,
            title=excluded.title,
            format=excluded.format,
            format_options_json=excluded.format_options_json,
            pecking_order=excluded.pecking_order,
            tenant=excluded.tenant,
            updated_at=excluded.updated_at
        """,
        (cid, library_doc_id, title, fmt, format_options_json, int(pecking_order), tenant, now),
    )
    return get_config(conn, cid)


def get_config(conn: sqlite3.Connection, config_id: str) -> dict[str, Any] | None:
    ensure_scribe_schema(conn)
    cur = conn.execute(
        """
        SELECT id, library_doc_id, title, format, format_options_json, pecking_order, tenant, updated_at
        FROM scribe_document_configs WHERE id = ?
        """,
        (config_id,),
    )
    row = cur.fetchone()
    return _row_config(row) if row else None


def list_configs(conn: sqlite3.Connection, tenant: str | None = None) -> list[dict[str, Any]]:
    ensure_scribe_schema(conn)
    if tenant:
        cur = conn.execute(
            """
            SELECT id, library_doc_id, title, format, format_options_json, pecking_order, tenant, updated_at
            FROM scribe_document_configs WHERE tenant = ? ORDER BY title
            """,
            (tenant,),
        )
    else:
        cur = conn.execute(
            """
            SELECT id, library_doc_id, title, format, format_options_json, pecking_order, tenant, updated_at
            FROM scribe_document_configs ORDER BY title
            """
        )
    return [_row_config(r) for r in cur.fetchall()]


def upsert_page(
    conn: sqlite3.Connection,
    *,
    config_id: str,
    page_index: int,
    body_text: str | None = None,
    body_blob_id: str | None = None,
    body_library_doc_id: str | None = None,
    surface_kind: str | None = None,
    page_id: str | None = None,
) -> dict[str, Any]:
    ensure_scribe_schema(conn)
    now = _now()
    cur = conn.execute(
        "SELECT id FROM scribe_pages WHERE config_id = ? AND page_index = ?",
        (config_id, int(page_index)),
    )
    row = cur.fetchone()
    pid = (row[0] if row else None) or page_id or str(uuid.uuid4())
    conn.execute(
        """
        INSERT INTO scribe_pages
            (id, config_id, page_index, body_text, body_blob_id, body_library_doc_id, surface_kind, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(id) DO UPDATE SET
            body_text=excluded.body_text,
            body_blob_id=excluded.body_blob_id,
            body_library_doc_id=excluded.body_library_doc_id,
            surface_kind=excluded.surface_kind,
            updated_at=excluded.updated_at
        """,
        (pid, config_id, int(page_index), body_text, body_blob_id, body_library_doc_id, surface_kind, now),
    )
    return get_page(conn, config_id, int(page_index))


def get_page(conn: sqlite3.Connection, config_id: str, page_index: int) -> dict[str, Any] | None:
    ensure_scribe_schema(conn)
    cur = conn.execute(
        """
        SELECT id, config_id, page_index, body_text, body_blob_id, body_library_doc_id, surface_kind, updated_at
        FROM scribe_pages WHERE config_id = ? AND page_index = ?
        """,
        (config_id, int(page_index)),
    )
    row = cur.fetchone()
    if not row:
        return None
    return {
        "id": row[0],
        "configId": row[1],
        "pageIndex": row[2],
        "bodyText": row[3],
        "bodyBlobId": row[4],
        "bodyLibraryDocId": row[5],
        "surfaceKind": row[6],
        "updatedAt": row[7],
        "anchors": list_anchors(conn, row[0]),
    }


def list_pages(conn: sqlite3.Connection, config_id: str) -> list[dict[str, Any]]:
    ensure_scribe_schema(conn)
    cur = conn.execute(
        """
        SELECT id, config_id, page_index, body_text, body_blob_id, body_library_doc_id, surface_kind, updated_at
        FROM scribe_pages WHERE config_id = ? ORDER BY page_index
        """,
        (config_id,),
    )
    out = []
    for row in cur.fetchall():
        out.append(
            {
                "id": row[0],
                "configId": row[1],
                "pageIndex": row[2],
                "bodyText": row[3],
                "bodyBlobId": row[4],
                "bodyLibraryDocId": row[5],
                "surfaceKind": row[6],
                "updatedAt": row[7],
                "anchors": list_anchors(conn, row[0]),
            }
        )
    return out


def upsert_anchor(
    conn: sqlite3.Connection,
    *,
    page_id: str,
    anchor_key: str,
    kind: str = "bookmark",
    char_start: int | None = None,
    char_end: int | None = None,
    payload_json: str | None = None,
) -> dict[str, Any]:
    ensure_scribe_schema(conn)
    kind = (kind or "bookmark").strip().lower()
    if kind not in ALLOWED_ANCHOR_KINDS:
        raise ValueError(f"unsupported_anchor_kind:{kind}")
    aid = str(uuid.uuid4())
    cur = conn.execute(
        "SELECT id FROM scribe_page_anchors WHERE page_id = ? AND anchor_key = ?",
        (page_id, anchor_key),
    )
    row = cur.fetchone()
    if row:
        aid = row[0]
        conn.execute(
            """
            UPDATE scribe_page_anchors
            SET kind=?, char_start=?, char_end=?, payload_json=?
            WHERE id=?
            """,
            (kind, char_start, char_end, payload_json, aid),
        )
    else:
        conn.execute(
            """
            INSERT INTO scribe_page_anchors
                (id, page_id, anchor_key, char_start, char_end, kind, payload_json)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            (aid, page_id, anchor_key, char_start, char_end, kind, payload_json),
        )
    return {
        "id": aid,
        "pageId": page_id,
        "anchorKey": anchor_key,
        "charStart": char_start,
        "charEnd": char_end,
        "kind": kind,
        "payloadJson": payload_json,
    }


def list_anchors(conn: sqlite3.Connection, page_id: str) -> list[dict[str, Any]]:
    ensure_scribe_schema(conn)
    cur = conn.execute(
        """
        SELECT id, page_id, anchor_key, char_start, char_end, kind, payload_json
        FROM scribe_page_anchors WHERE page_id = ? ORDER BY char_start
        """,
        (page_id,),
    )
    return [
        {
            "id": r[0],
            "pageId": r[1],
            "anchorKey": r[2],
            "charStart": r[3],
            "charEnd": r[4],
            "kind": r[5],
            "payloadJson": r[6],
        }
        for r in cur.fetchall()
    ]
