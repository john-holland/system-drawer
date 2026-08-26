"""Flask routes for scribe document configs, pages, and anchors."""

from __future__ import annotations

import sqlite3
from typing import Callable

from flask import jsonify, request

GetConn = Callable[[], sqlite3.Connection]


def register_scribe_routes(app, get_conn: GetConn, get_current_user: Callable[[], str]) -> None:
    try:
        from continuuuum_api.scribe_db import (
            ensure_scribe_schema,
            get_config,
            get_page,
            list_anchors,
            list_configs,
            list_pages,
            upsert_anchor,
            upsert_config,
            upsert_page,
        )
    except ImportError:
        from scribe_db import (
            ensure_scribe_schema,
            get_config,
            get_page,
            list_anchors,
            list_configs,
            list_pages,
            upsert_anchor,
            upsert_config,
            upsert_page,
        )

    @app.route("/api/scribe/configs", methods=["GET", "POST"])
    def scribe_configs():
        conn = get_conn()
        try:
            ensure_scribe_schema(conn)
            if request.method == "GET":
                tenant = request.args.get("tenant")
                return jsonify({"ok": True, "configs": list_configs(conn, tenant)}), 200
            body = request.get_json(silent=True) or {}
            cfg = upsert_config(
                conn,
                config_id=body.get("id") or body.get("configId"),
                title=body.get("title") or "Untitled",
                fmt=body.get("format") or "plain",
                format_options_json=body.get("formatOptionsJson"),
                pecking_order=int(body.get("peckingOrder") or 20),
                tenant=body.get("tenant") or "default",
                library_doc_id=body.get("libraryDocId"),
            )
            conn.commit()
            return jsonify({"ok": True, "config": cfg}), 200
        except ValueError as e:
            return jsonify({"ok": False, "error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/scribe/configs/<config_id>", methods=["GET"])
    def scribe_get_config(config_id: str):
        conn = get_conn()
        try:
            cfg = get_config(conn, config_id)
            if not cfg:
                return jsonify({"error": "not_found"}), 404
            pages = list_pages(conn, config_id)
            return jsonify({"ok": True, "config": cfg, "pages": pages}), 200
        finally:
            conn.close()

    @app.route("/api/scribe/configs/<config_id>/pages", methods=["GET", "POST"])
    def scribe_pages(config_id: str):
        conn = get_conn()
        try:
            ensure_scribe_schema(conn)
            if request.method == "GET":
                return jsonify({"ok": True, "pages": list_pages(conn, config_id)}), 200
            body = request.get_json(silent=True) or {}
            page = upsert_page(
                conn,
                config_id=config_id,
                page_index=int(body.get("pageIndex") or 0),
                body_text=body.get("bodyText"),
                body_blob_id=body.get("bodyBlobId"),
                body_library_doc_id=body.get("bodyLibraryDocId"),
                surface_kind=body.get("surfaceKind"),
                page_id=body.get("id"),
            )
            conn.commit()
            return jsonify({"ok": True, "page": page}), 200
        finally:
            conn.close()

    @app.route("/api/scribe/configs/<config_id>/pages/<int:page_index>", methods=["GET"])
    def scribe_get_page(config_id: str, page_index: int):
        conn = get_conn()
        try:
            page = get_page(conn, config_id, page_index)
            if not page:
                return jsonify({"error": "not_found"}), 404
            return jsonify({"ok": True, "page": page}), 200
        finally:
            conn.close()

    @app.route("/api/scribe/pages/<page_id>/anchors", methods=["GET", "POST"])
    def scribe_anchors(page_id: str):
        conn = get_conn()
        try:
            ensure_scribe_schema(conn)
            if request.method == "GET":
                return jsonify({"ok": True, "anchors": list_anchors(conn, page_id)}), 200
            body = request.get_json(silent=True) or {}
            anchor = upsert_anchor(
                conn,
                page_id=page_id,
                anchor_key=body.get("anchorKey") or body.get("key") or "mark",
                kind=body.get("kind") or "bookmark",
                char_start=body.get("charStart"),
                char_end=body.get("charEnd"),
                payload_json=body.get("payloadJson"),
            )
            conn.commit()
            return jsonify({"ok": True, "anchor": anchor}), 200
        except ValueError as e:
            return jsonify({"ok": False, "error": str(e)}), 400
        finally:
            conn.close()
