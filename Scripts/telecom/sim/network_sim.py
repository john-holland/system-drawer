"""In-memory telecom network simulator for PACT playback tests."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass
class SimDevice:
    id: str
    network_id: str
    phone_e164: str | None = None
    ipv6_full: str | None = None


@dataclass
class SimNetwork:
    id: str
    virtual: bool = False
    discovery_cross_route: bool = True
    devices: dict[str, SimDevice] = field(default_factory=dict)
    routes: list[dict[str, Any]] = field(default_factory=list)


@dataclass
class NetworkSim:
    networks: dict[str, SimNetwork] = field(default_factory=dict)
    connections: list[tuple[str, str]] = field(default_factory=list)
    pam_users: dict[str, list[str]] = field(default_factory=dict)
    user_devices: dict[str, list[str]] = field(default_factory=dict)

    def add_network(self, net_id: str, *, virtual: bool = False, cross_route: bool = True) -> None:
        self.networks[net_id] = SimNetwork(id=net_id, virtual=virtual, discovery_cross_route=cross_route)

    def add_device(self, dev_id: str, network_id: str, **kwargs: Any) -> None:
        net = self.networks.setdefault(network_id, SimNetwork(id=network_id))
        net.devices[dev_id] = SimDevice(id=dev_id, network_id=network_id, **kwargs)

    def connect(self, a: str, b: str) -> None:
        self.connections.append((a, b))

    def discover(self, *, device_id: str | None = None, phone: str | None = None) -> SimDevice | None:
        for net in self.networks.values():
            for dev in net.devices.values():
                if device_id and dev.id == device_id:
                    return dev
                if phone and dev.phone_e164 == phone:
                    return dev
        return None

    def route_call(self, from_net: str, to_device: str) -> bool:
        target = self.discover(device_id=to_device)
        if not target:
            return False
        if target.network_id == from_net:
            return True
        for a, b in self.connections:
            if {a, b} == {from_net, target.network_id}:
                return True
        target_net = self.networks.get(target.network_id)
        if target_net and target_net.discovery_cross_route:
            return True
        return False

    def pam_allow(self, user_id: str, device_id: str, permission: str = "call") -> bool:
        perms = self.pam_users.get(user_id, [])
        if "admin" in perms:
            return True
        if permission not in perms:
            return False
        allowed = self.user_devices.get(user_id, [])
        return device_id in allowed or not allowed


def load_playbook_into_sim(playbook: dict[str, Any]) -> NetworkSim:
    sim = NetworkSim()
    for net in playbook.get("networks") or []:
        sim.add_network(
            net["id"],
            virtual=bool(net.get("virtual")),
            cross_route=bool((net.get("discovery") or {}).get("crossRoute", True)),
        )
    for dev in playbook.get("devices") or []:
        net_id = dev.get("network_id") or "ubiquitous"
        sim.add_device(dev["id"], net_id, phone_e164=dev.get("phone"))
    pam = playbook.get("pam") or {}
    for user in pam.get("users") or []:
        uid = user["name"]
        sim.pam_users[uid] = list(user.get("permissions") or [])
        sim.user_devices[uid] = list(user.get("devices") or [])
    return sim
