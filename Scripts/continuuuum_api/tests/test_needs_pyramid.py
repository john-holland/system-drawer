"""Tests for needs pyramid registry."""

from continuuuum_api.needs_pyramid import NEED_ASPECTS, get_aspect, registry_json


def test_five_maslow_tiers():
    assert len(NEED_ASPECTS) == 5


def test_physiological_aspect():
    a = get_aspect("need_physiological")
    assert a is not None
    assert "healthcare_coverage" in a.society_features
    assert a.spatial_slot_id == "need_physiological"


def test_registry_json():
    data = registry_json()
    assert len(data["items"]) == 5
