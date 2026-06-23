"""Telecom REST API routes."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable

from flask import Response, jsonify, request, send_from_directory

from telecom.frame_processor_client import FrameProcessorClient
from telecom.geo_assign import auto_assign_ip
from telecom.pam import grant_permissions, list_user_permissions, user_can_access_device, user_has_permission
from telecom.phone_registry import format_e164, parse_phone
from telecom.representational_net.validate import repair_hints, validate_site
from telecom.topology_loader import PLAYBOOKS_ROOT, ensure_telecom_tables, list_playbook_files, sync_playbook_to_db
from telecom.usc_export import export_usc_topology

GetConn = Callable[[], sqlite3.Connection]
REPO_ROOT = Path(__file__).resolve().parents[2]
SITES_ROOT = REPO_ROOT / "telecom" / "playbooks" / "resources" / "sites"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _row_network(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "name": r["name"],
        "virtual": bool(r["virtual"]),
        "discoveryCrossRoute": bool(r["discovery_cross_route"]),
        "playbookPath": r["playbook_path"],
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def _row_device(r: sqlite3.Row) -> dict:
    return {
        "id": r["id"],
        "networkId": r["network_id"],
        "displayName": r["display_name"],
        "phoneE164": r["phone_e164"],
        "ipv6Full": r["ipv6_full"],
        "causalityLeafId": r["causality_leaf_id"],
        "uscAssetId": r["usc_asset_id"],
        "spatialGeohash": r["spatial_geohash"],
        "metadata": json.loads(r["metadata_json"] or "{}"),
        "createdAt": r["created_at"],
        "updatedAt": r["updated_at"],
    }


def register_telecom_routes(app, get_conn: GetConn) -> None:
    static_nd = Path(__file__).resolve().parent / "static" / "network-definitions"

    @app.route("/network-definitions")
    @app.route("/network-definitions/<path:subpath>")
    def serve_network_definitions(subpath=None):
        return send_from_directory(static_nd, "index.html")

    def _ensure(conn: sqlite3.Connection) -> None:
        ensure_telecom_tables(conn)

    @app.route("/api/telecom/networks", methods=["GET", "POST"])
    def telecom_networks():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM telecom_networks ORDER BY name")
                return jsonify({"items": [_row_network(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            net_id = body.get("id") or str(uuid.uuid4())
            now = _now()
            conn.execute(
                """INSERT INTO telecom_networks (id, name, virtual, discovery_cross_route, playbook_path, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?)""",
                (
                    net_id,
                    body.get("name", net_id),
                    1 if body.get("virtual") else 0,
                    1 if body.get("discoveryCrossRoute", True) else 0,
                    body.get("playbookPath"),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM telecom_networks WHERE id = ?", (net_id,))
            return jsonify(_row_network(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/telecom/networks/<net_id>", methods=["GET", "PATCH", "DELETE"])
    def telecom_network(net_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM telecom_networks WHERE id = ?", (net_id,))
                row = cur.fetchone()
                if not row:
                    return jsonify({"error": "not found"}), 404
                return jsonify(_row_network(row))
            if request.method == "DELETE":
                conn.execute("DELETE FROM telecom_networks WHERE id = ?", (net_id,))
                conn.commit()
                return jsonify({"deleted": net_id})
            body = request.get_json(force=True) or {}
            fields = []
            params = []
            for key, col in (
                ("name", "name"),
                ("playbookPath", "playbook_path"),
            ):
                if key in body:
                    fields.append(f"{col} = ?")
                    params.append(body[key])
            if "virtual" in body:
                fields.append("virtual = ?")
                params.append(1 if body["virtual"] else 0)
            if "discoveryCrossRoute" in body:
                fields.append("discovery_cross_route = ?")
                params.append(1 if body["discoveryCrossRoute"] else 0)
            if fields:
                fields.append("updated_at = ?")
                params.append(_now())
                params.append(net_id)
                conn.execute(f"UPDATE telecom_networks SET {', '.join(fields)} WHERE id = ?", params)
                conn.commit()
            cur = conn.execute("SELECT * FROM telecom_networks WHERE id = ?", (net_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(_row_network(row))
        finally:
            conn.close()

    @app.route("/api/telecom/network-connections", methods=["GET", "POST"])
    def telecom_connections():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM telecom_network_connections ORDER BY created_at DESC")
                items = [
                    {
                        "id": r["id"],
                        "fromNetworkId": r["from_network_id"],
                        "toNetworkId": r["to_network_id"],
                        "gatewayDeviceId": r["gateway_device_id"],
                        "enabled": bool(r["enabled"]),
                    }
                    for r in cur.fetchall()
                ]
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            cid = body.get("id") or str(uuid.uuid4())
            conn.execute(
                """INSERT INTO telecom_network_connections
                   (id, from_network_id, to_network_id, gateway_device_id, enabled, created_at)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (
                    cid,
                    body["fromNetworkId"],
                    body["toNetworkId"],
                    body.get("gatewayDeviceId"),
                    1 if body.get("enabled", True) else 0,
                    _now(),
                ),
            )
            conn.commit()
            return jsonify({"id": cid}), 201
        finally:
            conn.close()

    @app.route("/api/telecom/devices", methods=["GET", "POST"])
    def telecom_devices():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                network_id = request.args.get("networkId")
                if network_id:
                    cur = conn.execute(
                        "SELECT * FROM telecom_devices WHERE network_id = ? ORDER BY display_name",
                        (network_id,),
                    )
                else:
                    cur = conn.execute("SELECT * FROM telecom_devices ORDER BY display_name")
                return jsonify({"items": [_row_device(r) for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            dev_id = body.get("id") or str(uuid.uuid4())
            now = _now()
            phone_e164 = body.get("phoneE164")
            if body.get("phone"):
                phone_e164 = format_e164(parse_phone(body["phone"]))
            ip_full = body.get("ipv6Full")
            if body.get("ip") == "auto" or (not ip_full and body.get("autoAddress", True)):
                ip_full = auto_assign_ip(body.get("spatialGeohash"), body.get("causalityLeafId"))
            conn.execute(
                """INSERT INTO telecom_devices
                   (id, network_id, display_name, phone_e164, ipv6_full, causality_leaf_id,
                    usc_asset_id, spatial_geohash, metadata_json, created_at, updated_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (
                    dev_id,
                    body.get("networkId", "ubiquitous"),
                    body.get("displayName", dev_id),
                    phone_e164,
                    ip_full,
                    body.get("causalityLeafId"),
                    body.get("uscAssetId"),
                    body.get("spatialGeohash"),
                    json.dumps(body.get("metadata") or {}),
                    now,
                    now,
                ),
            )
            conn.commit()
            cur = conn.execute("SELECT * FROM telecom_devices WHERE id = ?", (dev_id,))
            return jsonify(_row_device(cur.fetchone())), 201
        finally:
            conn.close()

    @app.route("/api/telecom/devices/<dev_id>", methods=["GET", "PATCH", "DELETE"])
    def telecom_device(dev_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM telecom_devices WHERE id = ?", (dev_id,))
                row = cur.fetchone()
                if not row:
                    return jsonify({"error": "not found"}), 404
                return jsonify(_row_device(row))
            if request.method == "DELETE":
                conn.execute("DELETE FROM telecom_devices WHERE id = ?", (dev_id,))
                conn.commit()
                return jsonify({"deleted": dev_id})
            body = request.get_json(force=True) or {}
            mapping = {
                "displayName": "display_name",
                "networkId": "network_id",
                "phoneE164": "phone_e164",
                "ipv6Full": "ipv6_full",
                "causalityLeafId": "causality_leaf_id",
                "uscAssetId": "usc_asset_id",
                "spatialGeohash": "spatial_geohash",
            }
            fields = []
            params = []
            for js_key, col in mapping.items():
                if js_key in body:
                    fields.append(f"{col} = ?")
                    params.append(body[js_key])
            if "phone" in body:
                fields.append("phone_e164 = ?")
                params.append(format_e164(parse_phone(body["phone"])))
            if fields:
                fields.append("updated_at = ?")
                params.append(_now())
                params.append(dev_id)
                conn.execute(f"UPDATE telecom_devices SET {', '.join(fields)} WHERE id = ?", params)
                conn.commit()
            cur = conn.execute("SELECT * FROM telecom_devices WHERE id = ?", (dev_id,))
            row = cur.fetchone()
            if not row:
                return jsonify({"error": "not found"}), 404
            return jsonify(_row_device(row))
        finally:
            conn.close()

    @app.route("/api/telecom/routes", methods=["GET", "POST"])
    def telecom_routes_list():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                network_id = request.args.get("networkId")
                if network_id:
                    cur = conn.execute(
                        "SELECT * FROM telecom_routes WHERE network_id = ? ORDER BY metric",
                        (network_id,),
                    )
                else:
                    cur = conn.execute("SELECT * FROM telecom_routes ORDER BY network_id, metric")
                items = [
                    {
                        "id": r["id"],
                        "networkId": r["network_id"],
                        "prefix": r["prefix"],
                        "nextHop": r["next_hop"],
                        "metric": r["metric"],
                    }
                    for r in cur.fetchall()
                ]
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            rid = body.get("id") or str(uuid.uuid4())
            conn.execute(
                """INSERT INTO telecom_routes (id, network_id, prefix, next_hop, metric, created_at)
                   VALUES (?, ?, ?, ?, ?, ?)""",
                (
                    rid,
                    body["networkId"],
                    body["prefix"],
                    body.get("nextHop"),
                    int(body.get("metric", 100)),
                    _now(),
                ),
            )
            conn.commit()
            return jsonify({"id": rid}), 201
        finally:
            conn.close()

    @app.route("/api/telecom/pam/users", methods=["GET", "POST"])
    def telecom_pam_users():
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute("SELECT * FROM telecom_pam_users ORDER BY name")
                items = []
                for r in cur.fetchall():
                    items.append(
                        {
                            "id": r["id"],
                            "name": r["name"],
                            "permissions": list_user_permissions(conn, r["id"]),
                        }
                    )
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            uid = body.get("id") or str(uuid.uuid4())
            now = _now()
            conn.execute(
                "INSERT INTO telecom_pam_users (id, name, password_hash, metadata_json, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?)",
                (uid, body["name"], body.get("passwordHash"), json.dumps(body.get("metadata") or {}), now, now),
            )
            grant_permissions(conn, uid, body.get("permissions") or [])
            for dev_id in body.get("devices") or []:
                conn.execute(
                    "INSERT OR IGNORE INTO telecom_pam_user_devices (user_id, device_id) VALUES (?, ?)",
                    (uid, dev_id),
                )
            conn.commit()
            return jsonify({"id": uid, "name": body["name"]}), 201
        finally:
            conn.close()

    @app.route("/api/telecom/pam/users/<user_id>/devices", methods=["GET", "POST"])
    def telecom_pam_user_devices(user_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute(
                    "SELECT device_id FROM telecom_pam_user_devices WHERE user_id = ?",
                    (user_id,),
                )
                return jsonify({"items": [r[0] for r in cur.fetchall()]})
            body = request.get_json(force=True) or {}
            for dev_id in body.get("devices") or []:
                conn.execute(
                    "INSERT OR IGNORE INTO telecom_pam_user_devices (user_id, device_id) VALUES (?, ?)",
                    (user_id, dev_id),
                )
            conn.commit()
            return jsonify({"ok": True})
        finally:
            conn.close()

    @app.route("/api/telecom/pam/users/<user_id>/filesystem", methods=["GET", "POST"])
    def telecom_pam_user_fs(user_id: str):
        conn = get_conn()
        try:
            _ensure(conn)
            if request.method == "GET":
                cur = conn.execute(
                    "SELECT id, playbook_path, fs_path, rw FROM telecom_pam_filesystem_grants WHERE user_id = ?",
                    (user_id,),
                )
                items = [
                    {
                        "id": r["id"],
                        "playbookPath": r["playbook_path"],
                        "fsPath": r["fs_path"],
                        "rw": bool(r["rw"]),
                    }
                    for r in cur.fetchall()
                ]
                return jsonify({"items": items})
            body = request.get_json(force=True) or {}
            gid = str(uuid.uuid4())
            conn.execute(
                """INSERT INTO telecom_pam_filesystem_grants (id, user_id, playbook_path, fs_path, rw)
                   VALUES (?, ?, ?, ?, ?)""",
                (gid, user_id, body["playbookPath"], body["fsPath"], 1 if body.get("rw") else 0),
            )
            conn.commit()
            return jsonify({"id": gid}), 201
        finally:
            conn.close()

    @app.route("/api/telecom/discover", methods=["POST"])
    def telecom_discover():
        conn = get_conn()
        try:
            _ensure(conn)
            body = request.get_json(force=True) or {}
            device_id = body.get("deviceId")
            phone = body.get("phone")
            if phone:
                phone = format_e164(parse_phone(phone))
            if device_id:
                cur = conn.execute("SELECT * FROM telecom_devices WHERE id = ?", (device_id,))
            elif phone:
                cur = conn.execute("SELECT * FROM telecom_devices WHERE phone_e164 = ?", (phone,))
            else:
                return jsonify({"error": "deviceId or phone required"}), 400
            row = cur.fetchone()
            if not row:
                return jsonify({"found": False}), 404
            return jsonify({"found": True, "device": _row_device(row)})
        finally:
            conn.close()

    @app.route("/api/telecom/export/usc", methods=["POST"])
    def telecom_export_usc():
        body = request.get_json(force=True) or {}
        selection = body.get("uscSelection") or body.get("items") or []
        result = export_usc_topology(
            selection,
            episode_id=body.get("episodeId"),
            pam_users=body.get("pamUsers"),
        )
        return jsonify(result)

    @app.route("/api/telecom/playbooks", methods=["GET"])
    def telecom_playbooks():
        items = list_playbook_files()
        sync = request.args.get("sync")
        if sync:
            conn = get_conn()
            try:
                _ensure(conn)
                for item in items:
                    if "base/" in item["path"] or item["path"].startswith("base/"):
                        sync_playbook_to_db(conn, item["path"])
            finally:
                conn.close()
        return jsonify({"items": items, "root": str(PLAYBOOKS_ROOT)})

    @app.route("/api/telecom/sites/<site_id>/<path:subpath>")
    def telecom_serve_site(site_id: str, subpath: str):
        site_dir = SITES_ROOT / site_id
        if not site_dir.exists():
            return jsonify({"error": "site not found"}), 404
        return send_from_directory(site_dir, subpath)

    @app.route("/api/telecom/sites/<site_id>/validate", methods=["POST"])
    def telecom_validate_site(site_id: str):
        site_dir = SITES_ROOT / site_id
        if not site_dir.exists():
            return jsonify({"error": "site not found"}), 404
        errors = validate_site(site_dir)
        if errors:
            return jsonify({"valid": False, "errors": errors, "repairHints": repair_hints(errors)}), 422
        return jsonify({"valid": True})

    @app.route("/api/telecom/frame-processor/status", methods=["GET"])
    def telecom_frame_status():
        conn = get_conn()
        try:
            _ensure(conn)
            cur = conn.execute("SELECT backend, base_url FROM telecom_frame_processor_config WHERE id = 'default'")
            row = cur.fetchone()
            backend = row["backend"] if row else "flask"
            base_url = row["base_url"] if row else None
        finally:
            conn.close()
        client = FrameProcessorClient(backend, base_url)
        return jsonify(client.status())
