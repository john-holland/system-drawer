"""Node-backed tests for shared cron-humanize.js (one source of truth)."""

from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
STATIC = ROOT / "continuuuum_api" / "static"
JS = STATIC / "shared" / "cron" / "cron-humanize.js"

NODE = shutil.which("node")
pytestmark = pytest.mark.skipif(NODE is None, reason="node required for cron humanizer tests")


def _run_analyze(cron: str, amount, month_days: int = 30) -> dict:
    with tempfile.TemporaryDirectory() as td:
        td_path = Path(td)
        out_path = td_path / "out.json"
        script_path = td_path / "run_humanize.js"
        runner = f"""
const fs = require('fs');
const vm = require('vm');
const code = fs.readFileSync({json.dumps(str(JS.resolve()))}, 'utf8');
const ctx = {{}};
vm.createContext(ctx);
vm.runInContext(code, ctx);
const H = ctx.CronHumanize || ctx.PayrollCronHumanize;
const result = H.analyze({json.dumps(cron)}, {json.dumps(amount)}, {{ monthDays: {month_days} }});
const out = {{
  narrative: result.narrative,
  narrativeCore: result.narrativeCore,
  fires: result.fires,
  monthlyTotalUsd: result.monthlyTotalUsd,
  byMonthDays: result.byMonthDays,
  parts: result.parts,
  examples: (result.examples || []).map(e => e.id),
  totalsRow: H.formatTotalsRow(result.byMonthDays, {json.dumps(amount)}),
  hasAlias: !!ctx.PayrollCronHumanize && !!ctx.CronHumanize,
}};
fs.writeFileSync({json.dumps(str(out_path.resolve()))}, JSON.stringify(out));
"""
        script_path.write_text(runner, encoding="utf-8")
        log_path = td_path / "node.log"
        with open(log_path, "w", encoding="utf-8") as log:
            proc = subprocess.run(
                [NODE, str(script_path.resolve())],
                stdin=subprocess.DEVNULL,
                stdout=log,
                stderr=subprocess.STDOUT,
                check=False,
                cwd=str(ROOT),
            )
        if proc.returncode != 0 or not out_path.is_file():
            raise AssertionError(
                f"node exit {proc.returncode}; log={log_path.read_text(encoding='utf-8')!r}; "
                f"exists={out_path.is_file()}"
            )
        return json.loads(out_path.read_text(encoding="utf-8"))


def test_compound_monthly_and_nth_tuesday_money():
    r = _run_analyze("0 0 1 * *;0 0 * * 2#2", 40, 30)
    core = r["narrativeCore"].lower()
    assert "per month" in core
    assert "second tuesday" in core
    assert r["fires"] == 2
    assert abs(r["monthlyTotalUsd"] - 80) < 1e-9
    assert "$80" in r["narrative"] or "$80.00" in r["narrative"]
    assert "avg 30" in r["narrative"]
    assert r["narrativeCore"].startswith("$40")
    assert r["narrativeCore"].count("$40") == 1
    assert r["hasAlias"] is True


def test_compound_interval_and_weekly_occurrences():
    r = _run_analyze("0 */15 * * *;0 8 * * 1", 0, 30)
    core = r["narrativeCore"].lower()
    assert "every 15 hours" in core
    assert "monday" in core
    assert r["fires"] == 52
    assert "52 occurrences" in r["narrative"]
    assert "avg 30" in r["narrative"]
    assert r["monthlyTotalUsd"] is None


def test_month_lenses_28_29_30_31():
    r = _run_analyze("0 */15 * * *;0 8 * * 1", 0, 30)
    keys = {int(k) for k in r["byMonthDays"].keys()}
    assert keys == {28, 29, 30, 31}
    assert r["byMonthDays"]["30"]["fires"] == 52
    assert r["byMonthDays"]["28"]["fires"] == 48
    assert r["byMonthDays"]["31"]["fires"] == 53
    assert "28d:" in r["totalsRow"]
    assert "31d:" in r["totalsRow"]


def test_hours_window_no_occurrence_count():
    r = _run_analyze("* 6-22 * * 1-5", 0, 30)
    assert "weekdays" in r["narrativeCore"].lower()
    assert "hours 6-22" in r["narrativeCore"]
    assert "active hours window" in r["narrative"]
    assert r["fires"] is None


def test_examples_catalog_present():
    r = _run_analyze("0 0 1 * *", 40, 30)
    for eid in (
        "monthly",
        "nth_weekday",
        "weekly_time",
        "every_n_hours",
        "daily",
        "compound_money",
        "compound_interval",
    ):
        assert eid in r["examples"]


def test_spa_shells_load_shared_humanizer():
    payroll = (STATIC / "payroll" / "index.html").read_text(encoding="utf-8")
    assert "shared/cron/cron-humanize.js" in payroll
    assert "pay-cron-examples" in payroll

    for rel in (
        "transit/index.html",
        "airplanes/index.html",
        "staff_hours/index.html",
        "restaurants/index.html",
        "stations/index.html",
        "project-calendar/index.html",
    ):
        html = (STATIC / rel).read_text(encoding="utf-8")
        assert "shared/cron/cron-humanize.js" in html, rel
        assert "cron-humanize.css" in html, rel
