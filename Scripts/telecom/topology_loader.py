"""Load and sync telecom playbooks with SQLite."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import yaml

from telecom.geo_assign import auto_assign_ip
from telecom.phone_registry import format_e164, parse_phone

REPO_ROOT = Path(__file__).resolve().parents[2]
TELECOM_ROOT = REPO_ROOT / "telecom"
PLAYBOOKS_ROOT = TELECOM_ROOT / "playbooks"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def load_playbook_yaml(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as f:
        data = yaml.safe_load(f)
    if not isinstance(data, dict):
        raise ValueError(f"playbook must be mapping: {path}")
    return data


def list_playbook_files() -> list[dict[str, str]]:
    out: list[dict[str, str]] = []
    if not PLAYBOOKS_ROOT.exists():
        return out
    for p in sorted(PLAYBOOKS_ROOT.rglob("*.yaml")):
        rel = p.relative_to(PLAYBOOKS_ROOT).as_posix()
        out.append({"path": rel, "name": p.stem})
    return out


def sync_playbook_to_db(conn: sqlite3.Connection, rel_path: str) -> dict[str, Any]:
    """Import playbook devices/networks into DB (upsert)."""
    path = PLAYBOOKS_ROOT / rel_path
    data = load_playbook_yaml(path)
    now = _now()

    for net in data.get("networks") or []:
        net_id = net["id"]
        conn.execute(
            """INSERT INTO telecom_networks (id, name, virtual, discovery_cross_route, playbook_path, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?)
               ON CONFLICT(id) DO UPDATE SET
                 name=excluded.name, virtual=excluded.virtual,
                 discovery_cross_route=excluded.discovery_cross_route,
                 playbook_path=excluded.playbook_path, updated_at=excluded.updated_at""",
            (
                net_id,
                net.get("name", net_id),
                1 if net.get("virtual") else 0,
                1 if (net.get("discovery") or {}).get("crossRoute", True) else 0,
                rel_path,
                now,
                now,
            ),
        )

    synced_devices = []
    for dev in data.get("devices") or []:
        dev_id = dev["id"]
        network_id = dev.get("network_id") or (data.get("networks") or [{}])[0].get("id", "ubiquitous")
        phone_e164 = None
        if dev.get("phone"):
            phone_e164 = format_e164(parse_phone(dev["phone"]))
        ip_full = dev.get("ip")
        if ip_full == "auto" or not ip_full:
            ip_full = auto_assign_ip(causality_leaf_id=dev.get("causality_leaf_id"))
        conn.execute(
            """INSERT INTO telecom_devices
               (id, network_id, display_name, phone_e164, ipv6_full, causality_leaf_id, usc_asset_id,
                spatial_geohash, metadata_json, created_at, updated_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
               ON CONFLICT(id) DO UPDATE SET
                 network_id=excluded.network_id, display_name=excluded.display_name,
                 phone_e164=excluded.phone_e164, ipv6_full=excluded.ipv6_full,
                 causality_leaf_id=excluded.causality_leaf_id, updated_at=excluded.updated_at""",
            (
                dev_id,
                network_id,
                dev.get("display_name", dev_id),
                phone_e164,
                ip_full,
                dev.get("causality_leaf_id"),
                dev.get("usc_asset_id"),
                dev.get("spatial_geohash"),
                json.dumps({"permissions": dev.get("permissions") or []}),
                now,
                now,
            ),
        )
        synced_devices.append(dev_id)

    conn.commit()
    return {"playbook": rel_path, "devices": synced_devices}


def ensure_telecom_tables(conn: sqlite3.Connection) -> None:
    schema_path = Path(__file__).resolve().parents[1] / "continuuuum_telecom_schema.sql"
    if schema_path.exists():
        conn.executescript(schema_path.read_text(encoding="utf-8"))
        conn.commit()
