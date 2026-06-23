"""Lemma component creation metadata — ingest, cache rebuild, Farey hierarchy."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

try:
    from thesaurus.farey_ast import FareyInterval, rebalance_intervals, root_interval
except ImportError:
    from farey_ast import FareyInterval, rebalance_intervals, root_interval

REPORT_HISTORY_LIMIT = 20
SCHEMA_VERSION = 1


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_component_metadata_tables(conn: sqlite3.Connection) -> None:
    schema_path = (
        __import__("pathlib").Path(__file__).resolve().parent.parent
        / "continuum_lemma_component_metadata_schema.sql"
    )
    if schema_path.is_file():
        conn.executescript(schema_path.read_text(encoding="utf-8"))


def assign_farey_to_hierarchy(nodes: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Assign Farey intervals to flat node list keyed by path (parent/child paths)."""
    if not nodes:
        return []
    by_path: dict[str, dict[str, Any]] = {}
    for raw in nodes:
        path = (raw.get("path") or raw.get("gameObjectName") or "").strip()
        if not path:
            continue
        node = dict(raw)
        node["path"] = path
        by_path[path] = node

    children: dict[str, list[str]] = {}
    roots: list[str] = []
    for path in sorted(by_path.keys()):
        if "/" not in path:
            roots.append(path)
        else:
            parent = path.rsplit("/", 1)[0]
            children.setdefault(parent, []).append(path)

    def node_interval(node: dict[str, Any]) -> FareyInterval:
        return FareyInterval(
            int(node.get("fareyLeftNum") or node.get("farey_left_num") or 0),
            int(node.get("fareyLeftDen") or node.get("farey_left_den") or 1),
            int(node.get("fareyRightNum") or node.get("farey_right_num") or 1),
            int(node.get("fareyRightDen") or node.get("farey_right_den") or 1),
        )

    def assign_children(parent_path: str | None, parent_interval: FareyInterval | None) -> None:
        if parent_path is None:
            child_paths = sorted(roots)
            interval = root_interval()
        else:
            child_paths = sorted(children.get(parent_path, []))
            interval = parent_interval or root_interval()
        if not child_paths:
            return
        siblings = [by_path[p] for p in child_paths]
        assigned = rebalance_intervals(siblings, interval)
        for item in assigned:
            path = item["path"]
            by_path[path]["farey"] = {
                "ln": item["farey_left_num"],
                "ld": item["farey_left_den"],
                "rn": item["farey_right_num"],
                "rd": item["farey_right_den"],
            }
            by_path[path]["fareyLeftNum"] = item["farey_left_num"]
            by_path[path]["fareyLeftDen"] = item["farey_left_den"]
            by_path[path]["fareyRightNum"] = item["farey_right_num"]
            by_path[path]["fareyRightDen"] = item["farey_right_den"]
            assign_children(path, node_interval(by_path[path]))

    assign_children(None, None)
    return list(by_path.values())


def _component_type_names(payload: dict[str, Any]) -> list[str]:
    names: set[str] = set()
    for node in payload.get("nodes") or []:
        for comp in node.get("components") or []:
            tn = comp.get("typeName") or comp.get("type_name")
            if tn:
                names.add(str(tn))
    return sorted(names)


def _bucket_ids(payload: dict[str, Any]) -> list[str]:
    ids: set[str] = set()
    for row in payload.get("spatialBuckets") or payload.get("spatial_buckets") or []:
        bid = row.get("bucketId") or row.get("bucket_id")
        if bid:
            ids.add(str(bid))
    return sorted(ids)


def _causality_leaf_ids(payload: dict[str, Any]) -> list[str]:
    ids: set[str] = set()
    for row in payload.get("causalityLinks") or payload.get("causality_links") or []:
        for key in ("leafBack", "leafPause", "leafForward", "leaf_back", "leaf_pause", "leaf_forward"):
            val = row.get(key)
            if val:
                ids.add(str(val))
    return sorted(ids)


def _normalize_payload(body: dict[str, Any], entry_id: str, source: str) -> dict[str, Any]:
    payload = dict(body)
    payload["schemaVersion"] = SCHEMA_VERSION
    payload["entryId"] = entry_id
    payload["source"] = source
    payload.setdefault("capturedAt", _now())
    nodes = payload.get("nodes") or []
    if nodes and not (nodes[0].get("farey") or nodes[0].get("fareyLeftNum")):
        payload["nodes"] = assign_farey_to_hierarchy(nodes)
    return payload


def rebuild_metadata_cache(conn: sqlite3.Connection, entry_id: str) -> dict[str, Any] | None:
    """Rebuild denormalized cache row from blueprints + recent reports."""
    ensure_component_metadata_tables(conn)
    bp = conn.execute(
        """SELECT prefab_ref, payload_json, captured_at FROM lemma_component_blueprints
           WHERE entry_id = ? ORDER BY updated_at DESC LIMIT 1""",
        (entry_id,),
    ).fetchone()

    reports = conn.execute(
        """SELECT payload_json, captured_at, run_id FROM lemma_component_reports
           WHERE entry_id = ? ORDER BY captured_at DESC LIMIT ?""",
        (entry_id, REPORT_HISTORY_LIMIT),
    ).fetchall()

    if not bp and not reports:
        conn.execute("DELETE FROM lemma_component_metadata_cache WHERE entry_id = ?", (entry_id,))
        return None

    prefab_ref = bp["prefab_ref"] if bp else None
    last_blueprint_at = bp["captured_at"] if bp else None
    blueprint_payload = json.loads(bp["payload_json"]) if bp else {}

    report_payloads: list[dict[str, Any]] = []
    last_report_at = None
    for r in reports:
        report_payloads.append(json.loads(r["payload_json"]))
        if not last_report_at:
            last_report_at = r["captured_at"]

    type_names: set[str] = set()
    bucket_ids: set[str] = set()
    causality_ids: set[str] = set()

    if blueprint_payload:
        type_names.update(_component_type_names(blueprint_payload))
        bucket_ids.update(_bucket_ids(blueprint_payload))
        causality_ids.update(_causality_leaf_ids(blueprint_payload))

    for rp in report_payloads:
        type_names.update(_component_type_names(rp))
        bucket_ids.update(_bucket_ids(rp))
        causality_ids.update(_causality_leaf_ids(rp))

    summary = {
        "schemaVersion": SCHEMA_VERSION,
        "hasBlueprint": bool(bp),
        "hasRuntimeReports": bool(reports),
        "blueprintNodeCount": len(blueprint_payload.get("nodes") or []),
        "recentReports": [
            {
                "runId": r["run_id"],
                "capturedAt": r["captured_at"],
                "bucketIds": _bucket_ids(json.loads(r["payload_json"])),
                "causalityLeafIds": _causality_leaf_ids(json.loads(r["payload_json"])),
            }
            for r in reports[:REPORT_HISTORY_LIMIT]
        ],
        "blueprintTree": (blueprint_payload.get("nodes") or [])[:50],
    }

    cache_key = f"entry:{entry_id}:component_metadata"
    now = _now()
    row = {
        "entry_id": entry_id,
        "cache_key": cache_key,
        "prefab_ref": prefab_ref,
        "component_type_names_json": json.dumps(sorted(type_names)),
        "bucket_ids_json": json.dumps(sorted(bucket_ids)),
        "causality_leaf_ids_json": json.dumps(sorted(causality_ids)),
        "last_blueprint_at": last_blueprint_at,
        "last_report_at": last_report_at,
        "report_count": len(reports),
        "summary_json": json.dumps(summary),
        "updated_at": now,
    }

    conn.execute(
        """INSERT INTO lemma_component_metadata_cache (
            entry_id, cache_key, prefab_ref, component_type_names_json, bucket_ids_json,
            causality_leaf_ids_json, last_blueprint_at, last_report_at, report_count,
            summary_json, updated_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(entry_id) DO UPDATE SET
            cache_key=excluded.cache_key,
            prefab_ref=excluded.prefab_ref,
            component_type_names_json=excluded.component_type_names_json,
            bucket_ids_json=excluded.bucket_ids_json,
            causality_leaf_ids_json=excluded.causality_leaf_ids_json,
            last_blueprint_at=excluded.last_blueprint_at,
            last_report_at=excluded.last_report_at,
            report_count=excluded.report_count,
            summary_json=excluded.summary_json,
            updated_at=excluded.updated_at""",
        (
            row["entry_id"],
            row["cache_key"],
            row["prefab_ref"],
            row["component_type_names_json"],
            row["bucket_ids_json"],
            row["causality_leaf_ids_json"],
            row["last_blueprint_at"],
            row["last_report_at"],
            row["report_count"],
            row["summary_json"],
            row["updated_at"],
        ),
    )
    return row


def upsert_blueprint(conn: sqlite3.Connection, entry_id: str, body: dict[str, Any]) -> dict[str, Any]:
    ensure_component_metadata_tables(conn)
    prefab_ref = body.get("prefabRef") or body.get("prefab_ref") or ""
    content_hash = body.get("contentHash") or body.get("content_hash") or ""
    if not prefab_ref:
        raise ValueError("prefabRef required")
    if not content_hash:
        content_hash = str(hash(json.dumps(body.get("nodes") or [], sort_keys=True)))

    payload = _normalize_payload(body, entry_id, "blueprint")
    now = _now()
    bid = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO lemma_component_blueprints (id, entry_id, prefab_ref, content_hash, payload_json, captured_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?)
           ON CONFLICT(entry_id, prefab_ref, content_hash) DO UPDATE SET
             payload_json=excluded.payload_json,
             updated_at=excluded.updated_at""",
        (bid, entry_id, prefab_ref, content_hash, json.dumps(payload), now, now),
    )
    cache = rebuild_metadata_cache(conn, entry_id)
    return {"id": bid, "entryId": entry_id, "cache": cache_row_to_api(cache) if cache else None}


def append_report(conn: sqlite3.Connection, entry_id: str, body: dict[str, Any]) -> dict[str, Any]:
    ensure_component_metadata_tables(conn)
    run_id = body.get("runId") or body.get("run_id") or str(uuid.uuid4())
    payload = _normalize_payload(body, entry_id, "runtime")
    now = payload.get("capturedAt") or _now()
    rid = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO lemma_component_reports (id, entry_id, run_id, payload_json, captured_at)
           VALUES (?, ?, ?, ?, ?)""",
        (rid, entry_id, run_id, json.dumps(payload), now),
    )
    cache = rebuild_metadata_cache(conn, entry_id)
    return {"id": rid, "entryId": entry_id, "runId": run_id, "cache": cache_row_to_api(cache) if cache else None}


def cache_row_to_api(row: dict[str, Any] | sqlite3.Row | None) -> dict[str, Any] | None:
    if not row:
        return None
    if not isinstance(row, dict):
        row = dict(row)
    types = json.loads(row.get("component_type_names_json") or "[]")
    buckets = json.loads(row.get("bucket_ids_json") or "[]")
    leaves = json.loads(row.get("causality_leaf_ids_json") or "[]")
    summary = json.loads(row.get("summary_json") or "{}")
    return {
        "entryId": row["entry_id"],
        "prefabRef": row.get("prefab_ref"),
        "componentTypes": types,
        "bucketIds": buckets,
        "causalityLeafIds": leaves,
        "lastBlueprintAt": row.get("last_blueprint_at"),
        "lastReportAt": row.get("last_report_at"),
        "reportCount": int(row.get("report_count") or 0),
        "summary": summary,
    }


def component_creation_view(cache_api: dict[str, Any] | None) -> dict[str, Any] | None:
    if not cache_api:
        return None
    summary = cache_api.get("summary") or {}
    return {
        "summary": summary,
        "componentTypes": cache_api.get("componentTypes") or [],
        "bucketIds": cache_api.get("bucketIds") or [],
        "causalityLeafIds": cache_api.get("causalityLeafIds") or [],
        "lastBlueprintAt": cache_api.get("lastBlueprintAt"),
        "lastReportAt": cache_api.get("lastReportAt"),
        "hasBlueprint": bool(summary.get("hasBlueprint")),
        "hasRuntimeReports": bool(summary.get("hasRuntimeReports")),
    }


def load_metadata_for_entry(conn: sqlite3.Connection, entry_id: str) -> dict[str, Any]:
    ensure_component_metadata_tables(conn)
    cache = conn.execute(
        "SELECT * FROM lemma_component_metadata_cache WHERE entry_id = ?",
        (entry_id,),
    ).fetchone()
    blueprint = conn.execute(
        """SELECT id, prefab_ref, content_hash, payload_json, captured_at, updated_at
           FROM lemma_component_blueprints WHERE entry_id = ? ORDER BY updated_at DESC LIMIT 1""",
        (entry_id,),
    ).fetchone()
    reports = conn.execute(
        """SELECT id, run_id, payload_json, captured_at FROM lemma_component_reports
           WHERE entry_id = ? ORDER BY captured_at DESC LIMIT ?""",
        (entry_id, REPORT_HISTORY_LIMIT),
    ).fetchall()
    return {
        "entryId": entry_id,
        "cache": cache_row_to_api(cache),
        "componentCreation": component_creation_view(cache_row_to_api(cache)),
        "blueprint": {
            "id": blueprint["id"],
            "prefabRef": blueprint["prefab_ref"],
            "contentHash": blueprint["content_hash"],
            "payload": json.loads(blueprint["payload_json"]),
            "capturedAt": blueprint["captured_at"],
            "updatedAt": blueprint["updated_at"],
        }
        if blueprint
        else None,
        "reports": [
            {
                "id": r["id"],
                "runId": r["run_id"],
                "payload": json.loads(r["payload_json"]),
                "capturedAt": r["captured_at"],
            }
            for r in reports
        ],
    }


def load_all_cache_maps(conn: sqlite3.Connection) -> dict[str, dict[str, Any]]:
    ensure_component_metadata_tables(conn)
    out: dict[str, dict[str, Any]] = {}
    for r in conn.execute("SELECT * FROM lemma_component_metadata_cache").fetchall():
        api = cache_row_to_api(r)
        if api:
            out[r["entry_id"]] = api
    return out
