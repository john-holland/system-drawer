"""Assign virtual telecom networks and IPv6 prefixes to society cities."""

from __future__ import annotations

import hashlib
import json
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

from telecom.address_codec import GalacticPrefix, TerrestrialSuffix, encode_address
from telecom.topology_loader import ensure_telecom_tables

from society_db import _now


def _galactic_from_json(data: dict) -> GalacticPrefix:
    return GalacticPrefix(
        dimensional=int(data.get("dimensional", 0)),
        galactic=int(data.get("galactic", 1)),
        system=int(data.get("system", 0)),
        planet=int(data.get("planet", 1)),
    )


def allocate_city_grid(conn: sqlite3.Connection, planet_id: str) -> int:
    cur = conn.execute(
        "SELECT COALESCE(MAX(city_grid), 0) + 1 FROM society_cities WHERE planet_id = ?",
        (planet_id,),
    )
    return int(cur.fetchone()[0])


def city_ipv6_prefix(galactic: GalacticPrefix, city_grid: int, geohash: str | None = None) -> str:
    suffix = TerrestrialSuffix(
        global_region=1,
        region=1,
        country=1,
        city_grid=city_grid & 0xFFFF,
        device=0,
    )
    return encode_address(galactic, suffix)


def device_ipv6_from_stable_id(prefix_addr: str, stable_id: str) -> str:
    from telecom.address_codec import decode_address

    base = decode_address(prefix_addr)
    h = int(hashlib.sha256(stable_id.encode()).hexdigest()[:12], 16) & 0xFFFFFFFFFFFF
    suffix = TerrestrialSuffix(
        base.terrestrial.global_region,
        base.terrestrial.region,
        base.terrestrial.country,
        base.terrestrial.city_grid,
        h,
    )
    return encode_address(base.galactic, suffix)


def provision_city_network(
    conn: sqlite3.Connection,
    planet_id: str,
    city_id: str,
    display_name: str,
    geohash: str | None = None,
) -> dict[str, Any]:
    ensure_telecom_tables(conn)
    now = _now()
    cur = conn.execute("SELECT * FROM society_planets WHERE planet_id = ?", (planet_id,))
    planet = cur.fetchone()
    if not planet:
        raise ValueError(f"planet not found: {planet_id}")

    galactic = _galactic_from_json(json.loads(planet["galactic_prefix_json"]))
    planet_net_id = planet["default_network_id"]
    city_grid = allocate_city_grid(conn, planet_id)
    network_id = f"society.city.{city_id}"
    ipv6_prefix = city_ipv6_prefix(galactic, city_grid, geohash)
    gateway_id = f"city-{city_id}-gw"

    conn.execute(
        """INSERT INTO telecom_networks (id, name, virtual, discovery_cross_route, playbook_path, created_at, updated_at)
           VALUES (?, ?, 1, 1, NULL, ?, ?)
           ON CONFLICT(id) DO UPDATE SET name=excluded.name, updated_at=excluded.updated_at""",
        (network_id, f"City {display_name}", now, now),
    )
    conn.execute(
        """INSERT INTO telecom_networks (id, name, virtual, discovery_cross_route, playbook_path, created_at, updated_at)
           VALUES (?, ?, 1, 1, NULL, ?, ?)
           ON CONFLICT(id) DO NOTHING""",
        (planet_net_id, f"Planet {planet_id}", now, now),
    )
    conn.execute(
        """INSERT INTO telecom_devices
           (id, network_id, display_name, phone_e164, ipv6_full, causality_leaf_id, usc_asset_id,
            spatial_geohash, metadata_json, created_at, updated_at)
           VALUES (?, ?, ?, NULL, ?, NULL, NULL, ?, '{}', ?, ?)""",
        (gateway_id, network_id, f"{display_name} Gateway", ipv6_prefix, geohash, now, now),
    )
    route_id = str(uuid.uuid4())
    conn.execute(
        """INSERT INTO telecom_routes (id, network_id, prefix, next_hop, metric, created_at)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (route_id, planet_net_id, ipv6_prefix.split(":")[0] + "::/32", gateway_id, 100, now),
    )
    conn.execute(
        """INSERT INTO city_network_bindings
           (city_id, network_id, ipv6_city_prefix, gateway_device_id, planet_network_id, updated_at)
           VALUES (?, ?, ?, ?, ?, ?)
           ON CONFLICT(city_id) DO UPDATE SET
             network_id=excluded.network_id,
             ipv6_city_prefix=excluded.ipv6_city_prefix,
             gateway_device_id=excluded.gateway_device_id,
             updated_at=excluded.updated_at""",
        (city_id, network_id, ipv6_prefix, gateway_id, planet_net_id, now),
    )
    return {
        "cityGrid": city_grid,
        "networkId": network_id,
        "ipv6CityPrefix": ipv6_prefix,
        "gatewayDeviceId": gateway_id,
        "planetNetworkId": planet_net_id,
    }


def provision_building_device(
    conn: sqlite3.Connection,
    city_id: str,
    stable_id: str,
    display_name: str,
    causality_leaf_id: str | None = None,
) -> str | None:
    cur = conn.execute("SELECT * FROM city_network_bindings WHERE city_id = ?", (city_id,))
    binding = cur.fetchone()
    if not binding:
        return None
    now = _now()
    dev_id = f"bld-dev-{stable_id[:8]}"
    ip = device_ipv6_from_stable_id(binding["ipv6_city_prefix"], stable_id)
    conn.execute(
        """INSERT INTO telecom_devices
           (id, network_id, display_name, phone_e164, ipv6_full, causality_leaf_id, usc_asset_id,
            spatial_geohash, metadata_json, created_at, updated_at)
           VALUES (?, ?, ?, NULL, ?, ?, NULL, NULL, '{}', ?, ?)
           ON CONFLICT(id) DO UPDATE SET ipv6_full=excluded.ipv6_full, updated_at=excluded.updated_at""",
        (dev_id, binding["network_id"], display_name, ip, causality_leaf_id, now, now),
    )
    conn.execute(
        "UPDATE building_registry SET telecom_device_id = ?, updated_at = ? WHERE stable_id = ?",
        (dev_id, now, stable_id),
    )
    return dev_id
