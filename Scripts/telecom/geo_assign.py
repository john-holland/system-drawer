"""Map USC geohash + SG4D bucket to terrestrial IPv6 suffix."""

from __future__ import annotations

import hashlib

from telecom.address_codec import GalacticPrefix, TerrestrialSuffix, encode_address, earth_default_prefix


def _hash_nibbles(value: str, count: int) -> int:
    digest = hashlib.sha256(value.encode("utf-8")).hexdigest()
    return int(digest[:count], 16)


def geohash_to_terrestrial(geohash: str | None, causality_leaf_id: str | None = None) -> TerrestrialSuffix:
    """Derive terrestrial suffix from geohash and optional SG4D leaf."""
    gh = (geohash or "unknown").strip().lower()
    leaf = (causality_leaf_id or "").strip()
    seed = f"{gh}|{leaf}"
    h = _hash_nibbles(seed, 16)
    return TerrestrialSuffix(
        global_region=(h >> 56) & 0xFF,
        region=(h >> 48) & 0xFF,
        country=(h >> 40) & 0xFF,
        city_grid=(h >> 24) & 0xFFFF,
        device=h & 0xFFFFFFFFFFFF,
    )


def auto_assign_ip(
    geohash: str | None = None,
    causality_leaf_id: str | None = None,
    galactic: GalacticPrefix | None = None,
) -> str:
    """Return full composite IPv6 string for a device."""
    prefix = galactic or earth_default_prefix()
    suffix = geohash_to_terrestrial(geohash, causality_leaf_id)
    return encode_address(prefix, suffix)
