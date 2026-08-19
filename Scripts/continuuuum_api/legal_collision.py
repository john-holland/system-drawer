"""Agile legal collision checks against legal_cases and platform_feature_gates."""

from __future__ import annotations

import json
import sqlite3
from typing import Any


def _case_kind(row: sqlite3.Row) -> str:
    try:
        return (row["case_kind"] or "internal_agile") if "case_kind" in row.keys() else "internal_agile"
    except (KeyError, IndexError):
        return "internal_agile"


def _is_external_litigation(row: sqlite3.Row | None) -> bool:
    return bool(row) and _case_kind(row) == "external_litigation"


def _open_cases_for_asset(conn: sqlite3.Connection, asset_kind: str, asset_ref: dict[str, Any]) -> list[dict]:
    warnings: list[dict] = []
    legal_case_id = asset_ref.get("legalCaseId") or asset_ref.get("legal_case_id")
    lemma_id = asset_ref.get("lemmaEntryId") or asset_ref.get("lemma_entry_id")
    doc_id = asset_ref.get("libraryDocumentId") or asset_ref.get("library_document_id")
    prefab = asset_ref.get("prefabPath") or asset_ref.get("prefab_path")

    if legal_case_id:
        row = conn.execute("SELECT * FROM legal_cases WHERE id = ?", (str(legal_case_id),)).fetchone()
        if row and row["status"] in ("open", "investigating") and not _is_external_litigation(row):
            warnings.append(
                {
                    "severity": row["severity"] or "medium",
                    "caseId": row["id"],
                    "title": row["title"],
                    "message": f"Open legal case: {row['title']}",
                }
            )

    if asset_kind in ("lemma", "prefab", "usc") and (lemma_id or doc_id or prefab):
        rows = conn.execute(
            "SELECT * FROM legal_cases WHERE status IN ('open', 'investigating') ORDER BY severity DESC"
        ).fetchall()
        for row in rows:
            if _is_external_litigation(row):
                continue
            refs = json.loads(row["patent_refs_json"] or "[]")
            hay = json.dumps({"lemma": lemma_id, "doc": doc_id, "prefab": prefab, "refs": refs}).lower()
            if prefab and any(str(r).lower() in hay for r in refs if r):
                warnings.append(
                    {
                        "severity": row["severity"] or "medium",
                        "caseId": row["id"],
                        "title": row["title"],
                        "message": f"Possible collision with {row['title']}",
                    }
                )

    gates = conn.execute(
        "SELECT feature_key, status, legal_case_id FROM platform_feature_gates WHERE status IN ('blocked', 'investigating')"
    ).fetchall()
    feature_key = asset_ref.get("featureKey") or asset_ref.get("feature_key")
    for g in gates:
        if feature_key and g["feature_key"] == feature_key:
            warnings.append(
                {
                    "severity": "critical",
                    "caseId": g["legal_case_id"],
                    "featureKey": g["feature_key"],
                    "message": f"Feature gate {g['feature_key']} is {g['status']}",
                }
            )
    return warnings


def check_story_legal_collisions(
    conn: sqlite3.Connection,
    asset_kind: str | None,
    asset_ref_json: str | None,
) -> list[dict]:
    if not asset_kind:
        return []
    ref: dict = {}
    if asset_ref_json:
        try:
            ref = json.loads(asset_ref_json)
        except json.JSONDecodeError:
            pass
    return _open_cases_for_asset(conn, asset_kind, ref)


def has_critical_collision(warnings: list[dict]) -> bool:
    return any(w.get("severity") == "critical" for w in warnings)
