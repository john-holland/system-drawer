"""Spot-check ContinuuuumNav APP_GD_SELECTORS matrix against locked policy."""

from __future__ import annotations

import re
from pathlib import Path

NAV = (
    Path(__file__).resolve().parents[1]
    / "continuuuum_api"
    / "static"
    / "shared"
    / "continuuuum-nav"
    / "continuuuum-nav.js"
)

SESSION = (
    Path(__file__).resolve().parents[1]
    / "continuuuum_api"
    / "static"
    / "shared"
    / "continuuuum-user-session"
    / "continuuuum-user-session.js"
)

EXPECTED = {
    "library": (True, True),
    "lemma": (True, True),
    "hub": (True, False),
    "budget-dashboard": (True, False),
    "credits": (True, False),
    "payroll": (False, False),
    "game-dimensions": (False, False),
    "webcam-animations": (True, True),
    "legal-tracker": (False, False),
    "sql-viewer": (False, False),
    "settings": (False, False),
    "garbage-bags": (True, True),
    "transit": (True, True),
    "restaurants": (True, True),
    "votes": (True, True),
    "game-lobbies": (True, True),
    "table-read": (True, False),
}


def _parse_matrix(src: str) -> dict[str, tuple[bool, bool]]:
    block = re.search(r"var APP_GD_SELECTORS = \{([\s\S]*?)\n  \};", src)
    assert block, "APP_GD_SELECTORS not found"
    out = {}
    for m in re.finditer(
        r"['\"]?([\w-]+)['\"]?\s*:\s*\{\s*game:\s*(true|false)\s*,\s*dimension:\s*(true|false)\s*\}",
        block.group(1),
    ):
        out[m.group(1)] = (m.group(2) == "true", m.group(3) == "true")
    return out


def test_nav_matrix_locked_apps():
    matrix = _parse_matrix(NAV.read_text(encoding="utf-8"))
    for app, expected in EXPECTED.items():
        assert app in matrix, f"missing {app}"
        assert matrix[app] == expected, f"{app}: {matrix[app]} != {expected}"


def test_nav_exports_gd_policy():
    src = NAV.read_text(encoding="utf-8")
    assert "gdPolicyForApp:" in src
    assert "APP_GD_SELECTORS:" in src


def test_nav_admin_menu_includes_chat_tos():
    src = NAV.read_text(encoding="utf-8")
    assert "adminOnly: true" in src
    assert "label: 'Chat TOS'" in src
    assert "continuuuum-admin-apps" in src
    assert "Chat Lexicon" in src


def test_session_uses_getGdPolicy():
    src = SESSION.read_text(encoding="utf-8")
    assert "function getGdPolicy" in src
    assert "getGdPolicy:" in src
    assert "policy.game" in src
    assert "policy.dimension" in src
