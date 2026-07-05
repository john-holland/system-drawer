"""Maslow need aspects mapped to society features, zones, devices, and 2D spatial slots."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True)
class NeedAspect:
    aspect_id: str
    display_name: str
    tier: int
    society_features: tuple[str, ...]
    zone_ids: tuple[str, ...]
    property_classes: tuple[str, ...]
    device_kinds: tuple[str, ...]
    spatial_slot_id: str
    lemma_entry_hint: str = ""


NEED_ASPECTS: tuple[NeedAspect, ...] = (
    NeedAspect(
        aspect_id="need_physiological",
        display_name="Physiological",
        tier=1,
        society_features=("healthcare_coverage", "water", "power"),
        zone_ids=("residential_low", "public_services"),
        property_classes=("private", "public"),
        device_kinds=("home_terminal", "cctv"),
        spatial_slot_id="need_physiological",
        lemma_entry_hint="dream.physiological",
    ),
    NeedAspect(
        aspect_id="need_safety",
        display_name="Safety",
        tier=2,
        society_features=("tax_burden", "congress_stability"),
        zone_ids=("public_services", "public"),
        property_classes=("public",),
        device_kinds=("security_alarm",),
        spatial_slot_id="need_safety",
        lemma_entry_hint="dream.safety",
    ),
    NeedAspect(
        aspect_id="need_belonging",
        display_name="Belonging",
        tier=3,
        society_features=("civic_trust", "religious_attendance"),
        zone_ids=("religious", "hobby_venue"),
        property_classes=("religious", "hobby_venue"),
        device_kinds=("social_chat",),
        spatial_slot_id="need_belonging",
        lemma_entry_hint="dream.belonging",
    ),
    NeedAspect(
        aspect_id="need_esteem",
        display_name="Esteem",
        tier=4,
        society_features=("hobby_participation", "commercial_activity"),
        zone_ids=("commercial_core", "commercial"),
        property_classes=("commercial", "private"),
        device_kinds=("work_webtop",),
        spatial_slot_id="need_esteem",
        lemma_entry_hint="dream.esteem",
    ),
    NeedAspect(
        aspect_id="need_self_actualization",
        display_name="Self-Actualization",
        tier=5,
        society_features=("spirituality_index", "creative_lemma"),
        zone_ids=("hobby_venue", "commercial_core"),
        property_classes=("hobby_venue", "commercial"),
        device_kinds=("creative_terminal", "lemma_terminal"),
        spatial_slot_id="need_self_actualization",
        lemma_entry_hint="dream.self_actualization",
    ),
)


def get_aspect(aspect_id: str) -> NeedAspect | None:
    for a in NEED_ASPECTS:
        if a.aspect_id == aspect_id:
            return a
    return None


def all_aspects() -> list[NeedAspect]:
    return list(NEED_ASPECTS)


def aspect_to_dict(aspect: NeedAspect) -> dict[str, Any]:
    return {
        "aspectId": aspect.aspect_id,
        "displayName": aspect.display_name,
        "tier": aspect.tier,
        "societyFeatures": list(aspect.society_features),
        "zoneIds": list(aspect.zone_ids),
        "propertyClasses": list(aspect.property_classes),
        "deviceKinds": list(aspect.device_kinds),
        "spatialSlotId": aspect.spatial_slot_id,
        "lemmaEntryHint": aspect.lemma_entry_hint,
    }


def registry_json() -> dict[str, Any]:
    return {"items": [aspect_to_dict(a) for a in NEED_ASPECTS]}
