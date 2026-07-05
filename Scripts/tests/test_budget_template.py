"""Budget template export."""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from continuuuum_api.scripts.sheets_publish import build_budget_template_tabs, budget_template_payload


def test_budget_template_has_core_tabs():
    tabs = build_budget_template_tabs()
    assert "Budget" in tabs
    assert "Journal" in tabs
    assert tabs["Budget"][0] == ["field", "value"]
    payload = budget_template_payload()
    assert payload["ok"] is True
    assert len(payload["sheetOrder"]) >= 6
