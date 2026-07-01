"""Resaurce: media rights, lemma packages, legal cases, platform feature gates."""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime, timezone
from typing import Any, Callable

from flask import jsonify, request

from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE
from commerce_db import ensure_cave_commerce_tables, new_id

GetConn = Callable[[], sqlite3.Connection]


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _row_legal_case(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    d["patentRefs"] = json.loads(d.pop("patent_refs_json") or "[]")
    d["isBuiltIn"] = bool(d.pop("is_built_in"))
    d["featureKey"] = d.pop("feature_key", None)
    d["saurceProductId"] = d.pop("saurce_product_id", None)
    return d


def register_resaurce_routes(app, get_conn: GetConn) -> None:
    @app.before_request
    def _ensure():
        if not getattr(app, "_resaurce_ready", False):
            ensure_cave_commerce_tables(get_conn())
            app._resaurce_ready = True

    @app.route("/api/media-rights/publish", methods=["POST"])
    def media_rights_publish():
        body = request.get_json(silent=True) or {}
        asset_id = body.get("assetId") or body.get("asset_id")
        platform = body.get("platform")
        if not asset_id or not platform:
            return jsonify({"error": "assetId and platform required"}), 400
        rid = new_id()
        now = _now()
        conn = get_conn()
        conn.execute(
            """INSERT INTO media_rights (id, asset_id, platform, territory, effective_from, effective_to, agreement_ref, status, created_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                rid,
                str(asset_id),
                platform,
                body.get("territory"),
                body.get("effectiveFrom"),
                body.get("effectiveTo"),
                body.get("agreementRef"),
                "published",
                now,
            ),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": rid, "status": "published"}), 201

    @app.route("/api/media-rights/<asset_id>")
    def media_rights_get(asset_id: str):
        conn = get_conn()
        rows = conn.execute(
            "SELECT * FROM media_rights WHERE asset_id = ? ORDER BY created_at DESC",
            (asset_id,),
        ).fetchall()
        conn.close()
        return jsonify({"assetId": asset_id, "items": [dict(r) for r in rows]}), 200

    @app.route("/api/lemma-packages", methods=["POST"])
    def lemma_packages_create():
        body = request.get_json(silent=True) or {}
        name = (body.get("name") or "").strip()
        if not name:
            return jsonify({"error": "name required"}), 400
        pid = new_id()
        now = _now()
        conn = get_conn()
        conn.execute(
            """INSERT INTO lemma_packages
               (id, name, lemma_entry_ids_json, premium_cost, currency, vat_rate, state_tax_jurisdiction, saurce_product_id, created_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                pid,
                name,
                json.dumps(body.get("lemmaEntryIds") or body.get("entryIds") or []),
                body.get("premiumCost"),
                body.get("currency") or "USD",
                body.get("vatRate"),
                body.get("stateTaxJurisdiction"),
                body.get("saurceProductId"),
                now,
            ),
        )
        conn.commit()
        conn.close()
        return jsonify({"id": pid}), 201

    @app.route("/api/lemma-packages/<package_id>/price-quote", methods=["GET"])
    def lemma_package_quote(package_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM lemma_packages WHERE id = ?", (package_id,)).fetchone()
        conn.close()
        if not row:
            return jsonify({"error": "not found"}), 404
        subtotal = float(row["premium_cost"] or 0)
        vat_rate = float(row["vat_rate"] or 0)
        vat = subtotal * vat_rate / 100.0
        state_tax = subtotal * 0.05 if row["state_tax_jurisdiction"] else 0
        return jsonify(
            {
                "packageId": package_id,
                "subtotal": subtotal,
                "vat": round(vat, 2),
                "stateTax": round(state_tax, 2),
                "total": round(subtotal + vat + state_tax, 2),
                "currency": row["currency"],
                "taxCalculator": "stub",
            }
        ), 200

    @app.route("/api/legal/cases", methods=["GET", "POST"])
    def legal_cases():
        conn = get_conn()
        if request.method == "POST":
            body = request.get_json(silent=True) or {}
            cid = new_id()
            now = _now()
            conn.execute(
                """INSERT INTO legal_cases
                   (id, slug, title, category, status, severity, is_built_in, feature_key, description,
                    patent_refs_json, saurce_product_id, opened_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    cid,
                    body.get("slug"),
                    body.get("title") or "Untitled case",
                    body.get("category") or "misc",
                    body.get("status") or "open",
                    body.get("severity") or "medium",
                    0,
                    body.get("featureKey"),
                    body.get("description"),
                    json.dumps(body.get("patentRefs") or []),
                    body.get("saurceProductId"),
                    now,
                ),
            )
            conn.commit()
            conn.close()
            return jsonify({"id": cid}), 201
        status = request.args.get("status")
        product_id = request.args.get("saurceProductId")
        q = "SELECT * FROM legal_cases WHERE 1=1"
        params: list[Any] = []
        if status:
            q += " AND status = ?"
            params.append(status)
        if product_id:
            q += " AND saurce_product_id = ?"
            params.append(product_id)
        rows = conn.execute(q, params).fetchall()
        conn.close()
        return jsonify({"items": [_row_legal_case(r) for r in rows]}), 200

    @app.route("/api/legal/feature-gates", methods=["GET"])
    def legal_feature_gates():
        conn = get_conn()
        rows = conn.execute("SELECT * FROM platform_feature_gates ORDER BY feature_key").fetchall()
        conn.close()
        return jsonify({"items": [dict(r) for r in rows]}), 200

    @app.route("/api/legal/cases/<case_id>", methods=["GET", "PATCH"])
    def legal_case_detail(case_id: str):
        conn = get_conn()
        row = conn.execute("SELECT * FROM legal_cases WHERE id = ?", (case_id,)).fetchone()
        if not row:
            conn.close()
            return jsonify({"error": "not found"}), 404
        if request.method == "PATCH":
            body = request.get_json(silent=True) or {}
            if row["is_built_in"] and body.get("delete"):
                conn.close()
                return jsonify({"error": "cannot delete built-in case"}), 403
            fields = []
            params: list[Any] = []
            for key, col in [
                ("status", "status"),
                ("severity", "severity"),
                ("assignedTo", "assigned_to"),
                ("title", "title"),
                ("description", "description"),
                ("category", "category"),
            ]:
                if key in body:
                    fields.append(f"{col} = ?")
                    params.append(body[key])
            if fields:
                params.append(case_id)
                conn.execute(f"UPDATE legal_cases SET {', '.join(fields)} WHERE id = ?", params)
                conn.commit()
                row = conn.execute("SELECT * FROM legal_cases WHERE id = ?", (case_id,)).fetchone()
        resolutions = conn.execute(
            "SELECT * FROM legal_resolutions WHERE case_id = ? ORDER BY resolved_at DESC",
            (case_id,),
        ).fetchall()
        conn.close()
        out = _row_legal_case(row)
        out["resolutions"] = [dict(r) for r in resolutions]
        return jsonify(out), 200

    @app.route("/api/legal/cases/<case_id>/resolutions", methods=["POST"])
    def legal_add_resolution(case_id: str):
        body = request.get_json(silent=True) or {}
        rid = new_id()
        now = _now()
        conn = get_conn()
        conn.execute(
            """INSERT INTO legal_resolutions
               (id, case_id, summary, resolution_type, resolved_at, resolved_by, effective_date, document_refs_json)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                rid,
                case_id,
                body.get("summary") or "",
                body.get("resolutionType") or "fix",
                now,
                body.get("resolvedBy"),
                body.get("effectiveDate"),
                json.dumps(body.get("documentRefs") or []),
            ),
        )
        for ref in body.get("codeLineRefs") or []:
            conn.execute(
                """INSERT INTO legal_code_line_refs
                   (id, case_id, resolution_id, repo, file_path, start_line, end_line, commit_sha, branch, tag, blame_author, note)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    new_id(),
                    case_id,
                    rid,
                    ref.get("repo") or "continuum",
                    ref.get("filePath") or ref.get("file_path") or "",
                    int(ref.get("startLine") or ref.get("start_line") or 1),
                    int(ref.get("endLine") or ref.get("end_line") or 1),
                    ref.get("commitSha"),
                    ref.get("branch"),
                    ref.get("tag"),
                    ref.get("blameAuthor"),
                    ref.get("note"),
                ),
            )
        if case_id == BUILTIN_PREORDER_CASE_ID:
            rtype = body.get("resolutionType") or ""
            summary = (body.get("summary") or "").lower()
            cleared = rtype in ("no_action", "waiver", "fix", "policy_change") and (
                "clear" in summary
                or "waiv" in summary
                or "not patent" in summary
                or "no active patent" in summary
                or "abstract business" in summary
                or rtype == "waiver"
            )
            if cleared:
                conn.execute(
                    "UPDATE platform_feature_gates SET status = ?, updated_at = ? WHERE feature_key = ?",
                    ("cleared", now, PLATFORM_PREORDER_FEATURE),
                )
                conn.execute("UPDATE legal_cases SET status = ?, closed_at = ? WHERE id = ?", ("closed", now, case_id))
        conn.commit()
        conn.close()
        return jsonify({"id": rid}), 201

    @app.route("/api/legal/cases/<case_id>/code-lines")
    def legal_code_lines(case_id: str):
        conn = get_conn()
        rows = conn.execute(
            "SELECT * FROM legal_code_line_refs WHERE case_id = ? ORDER BY file_path, start_line",
            (case_id,),
        ).fetchall()
        conn.close()
        return jsonify({"items": [dict(r) for r in rows]}), 200

    @app.route("/api/legal/platform-features/preordering", methods=["GET", "PATCH"])
    def platform_preordering_gate():
        conn = get_conn()
        if request.method == "PATCH":
            body = request.get_json(silent=True) or {}
            status = body.get("status") or "cleared"
            conn.execute(
                "UPDATE platform_feature_gates SET status = ?, updated_at = ? WHERE feature_key = ?",
                (status, _now(), PLATFORM_PREORDER_FEATURE),
            )
            conn.commit()
        gate = conn.execute(
            "SELECT * FROM platform_feature_gates WHERE feature_key = ?",
            (PLATFORM_PREORDER_FEATURE,),
        ).fetchone()
        case = conn.execute("SELECT * FROM legal_cases WHERE id = ?", (BUILTIN_PREORDER_CASE_ID,)).fetchone()
        conn.close()
        return jsonify(
            {
                "featureKey": PLATFORM_PREORDER_FEATURE,
                "gate": dict(gate) if gate else None,
                "legalCase": _row_legal_case(case) if case else None,
            }
        ), 200
