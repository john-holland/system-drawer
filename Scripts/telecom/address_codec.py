"""Dual-layer IPv6 address codec (galactic prefix + terrestrial suffix)."""

from __future__ import annotations

import re
from dataclasses import dataclass

# Earth default galactic prefix: dimensional=0, galactic=1, system=0, planet=1
EARTH_GALACTIC_PREFIX = 0x01000001

_IPV6_RE = re.compile(
    r"^(?P<a>[0-9a-f]{1,4}):(?P<b>[0-9a-f]{1,4}):"
    r"(?P<c>[0-9a-f]{1,4}):(?P<d>[0-9a-f]{1,4}):"
    r"(?P<e>[0-9a-f]{1,4}):(?P<f>[0-9a-f]{1,4}):"
    r"(?P<g>[0-9a-f]{1,4}):(?P<h>[0-9a-f]{1,4})$",
    re.I,
)


@dataclass(frozen=True)
class GalacticPrefix:
    dimensional: int
    galactic: int
    system: int
    planet: int

    @property
    def value(self) -> int:
        return (
            ((self.dimensional & 0xFF) << 24)
            | ((self.galactic & 0xFF) << 16)
            | ((self.system & 0xFF) << 8)
            | (self.planet & 0xFF)
        )

    @property
    def hex8(self) -> str:
        return f"{self.value:08x}"

    @classmethod
    def from_int(cls, value: int) -> GalacticPrefix:
        return cls(
            dimensional=(value >> 24) & 0xFF,
            galactic=(value >> 16) & 0xFF,
            system=(value >> 8) & 0xFF,
            planet=value & 0xFF,
        )


@dataclass(frozen=True)
class TerrestrialSuffix:
    global_region: int
    region: int
    country: int
    city_grid: int
    device: int

    @property
    def segments(self) -> tuple[int, int, int, int, int, int]:
        """96-bit suffix as six 16-bit hex groups (IPv6-shaped)."""
        g = self.global_region & 0xFF
        r = self.region & 0xFF
        c = self.country & 0xFF
        cg = self.city_grid & 0xFFFF
        dev = self.device & 0xFFFFFFFFFFFF
        return (
            (g << 8) | r,
            (c << 8) | ((cg >> 8) & 0xFF),
            ((cg & 0xFF) << 8) | ((dev >> 40) & 0xFF),
            (dev >> 24) & 0xFFFF,
            (dev >> 8) & 0xFFFF,
            ((dev & 0xFF) << 8),
        )


@dataclass(frozen=True)
class TelecomAddress:
    galactic: GalacticPrefix
    terrestrial: TerrestrialSuffix

    def to_ipv6_string(self) -> str:
        gp = self.galactic
        ts = self.terrestrial.segments
        parts = [
            f"{gp.value >> 16:04x}",
            f"{gp.value & 0xFFFF:04x}",
        ]
        parts.extend(f"{s:04x}" for s in ts)
        return ":".join(parts)

    @classmethod
    def from_ipv6_string(cls, addr: str) -> TelecomAddress:
        m = _IPV6_RE.match(addr.strip())
        if not m:
            raise ValueError(f"invalid telecom IPv6: {addr!r}")
        groups = [int(m.group(ch), 16) for ch in "abcdefgh"]
        galactic_val = (groups[0] << 16) | groups[1]
        galactic = GalacticPrefix.from_int(galactic_val)
        g = (groups[2] >> 8) & 0xFF
        r = groups[2] & 0xFF
        c = (groups[3] >> 8) & 0xFF
        cg_hi = groups[3] & 0xFF
        cg_lo = (groups[4] >> 8) & 0xFF
        city_grid = (cg_hi << 8) | cg_lo
        dev = (
            ((groups[4] & 0xFF) << 40)
            | (groups[5] << 24)
            | (groups[6] << 8)
            | ((groups[7] >> 8) & 0xFF)
        )
        terrestrial = TerrestrialSuffix(g, r, c, city_grid, dev)
        return cls(galactic, terrestrial)


def encode_address(galactic: GalacticPrefix, terrestrial: TerrestrialSuffix) -> str:
    return TelecomAddress(galactic, terrestrial).to_ipv6_string()


def decode_address(addr: str) -> TelecomAddress:
    return TelecomAddress.from_ipv6_string(addr)


def earth_default_prefix() -> GalacticPrefix:
    return GalacticPrefix.from_int(EARTH_GALACTIC_PREFIX)
