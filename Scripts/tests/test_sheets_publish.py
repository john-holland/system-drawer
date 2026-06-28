"""Dry-run tests for sheets_publish."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "continuum_api"))

from scripts.sheets_publish import publish_budget_plan  # noqa: E402


def test_sheets_publish_dry_run_without_google():
    def fake_cave(route, payload):
        if route.endswith("budget/get"):
            return {"budget_plan": {"id": payload["budget_plan_id"], "name": "Test", "asset_list": []}}
        if route.endswith("journal/list"):
            return {"journal_entries": []}
        if route.endswith("schedule/list"):
            return {"production_schedules": []}
        return {}

    with patch("scripts.sheets_publish._cave", side_effect=fake_cave):
        with patch("scripts.sheets_publish._load_stories_for_plan", return_value=[]):
            with patch("scripts.sheets_publish._load_all_stories", return_value=[]):
                result = publish_budget_plan("budget_test", {"dry_run": True})
    assert result.get("ok") is True
    assert result.get("mode") in ("dry_run", "dry_run_file_export")
