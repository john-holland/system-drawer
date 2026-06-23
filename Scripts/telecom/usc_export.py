"""Build topology export tree from USC selections."""

from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import yaml

from telecom.geo_assign import auto_assign_ip
from telecom.phone_registry import format_e164, parse_phone

REPO_ROOT = Path(__file__).resolve().parents[2]
TOPOLOGY_ROOT = REPO_ROOT / "telecom" / "topology"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def export_usc_topology(
    usc_selection: list[dict[str, Any]],
    *,
    episode_id: str | None = None,
    pam_users: list[dict[str, Any]] | None = None,
) -> dict[str, Any]:
    """Build export manifest and write files under telecom/topology/."""
    TOPOLOGY_ROOT.mkdir(parents=True, exist_ok=True)
    networks_dir = TOPOLOGY_ROOT / "networks"
    networks_dir.mkdir(parents=True, exist_ok=True)
    generated_dir = REPO_ROOT / "telecom" / "playbooks" / "generated"
    generated_dir.mkdir(parents=True, exist_ok=True)

    export_id = str(uuid.uuid4())[:8]
    slug = episode_id or f"export-{export_id}"
    devices = []
    for i, item in enumerate(usc_selection):
        dev_id = item.get("deviceId") or f"usc-device-{i}"
        geohash = item.get("spatialGeohash") or item.get("geohash")
        leaf = item.get("causalityLeafId")
        phone = item.get("phone")
        phone_e164 = format_e164(parse_phone(phone)) if phone else None
        devices.append(
            {
                "id": dev_id,
                "networkId": "ubiquitous",
                "displayName": item.get("displayName", dev_id),
                "phoneE164": phone_e164,
                "ipv6Full": auto_assign_ip(geohash, leaf),
                "causalityLeafId": leaf,
                "uscAssetId": item.get("uscAssetId"),
                "spatialGeohash": geohash,
            }
        )

    topology = {
        "topology": "telecom/v1",
        "exportedAt": _now(),
        "networks": [
            {
                "id": "ubiquitous",
                "name": "Ubiquitous Virtual Network",
                "virtual": True,
                "discoveryCrossRoute": True,
            }
        ],
        "devices": devices,
        "routes": [],
        "connections": [],
    }

    net_path = networks_dir / f"{slug}.json"
    net_path.write_text(json.dumps(topology, indent=2), encoding="utf-8")

    playbook = {
        "playbook": "telecom/v1",
        "name": f"{slug}-export",
        "networks": [{"id": "ubiquitous", "virtual": True, "discovery": {"crossRoute": True}}],
        "resources": [
            {
                "path": f"../topology/networks/{slug}.json",
                "source": "usc",
            }
        ],
        "devices": [
            {
                "id": d["id"],
                "network_id": "ubiquitous",
                "ip": d["ipv6Full"],
                "phone": d.get("phoneE164"),
            }
            for d in devices
        ],
    }
    if pam_users:
        playbook["pam"] = {"users": pam_users}

    pb_path = generated_dir / f"{slug}-export.playbook.yaml"
    pb_path.write_text(yaml.dump(playbook, sort_keys=False), encoding="utf-8")

    return {
        "exportId": export_id,
        "topologyPath": str(net_path.relative_to(REPO_ROOT)).replace("\\", "/"),
        "playbookPath": str(pb_path.relative_to(REPO_ROOT)).replace("\\", "/"),
        "deviceCount": len(devices),
    }
