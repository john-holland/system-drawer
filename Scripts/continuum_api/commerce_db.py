"""Bootstrap resaurce/saurce/cave tables and built-in legal seeds."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE

BUILTIN_PREORDER_RESOLUTION_ID = "00000000-0000-4000-8000-000000000002"

PREORDER_PATENT_RESOLUTION_SUMMARY = (
    "Patent clearance review (industry / Gemini research, 2026-06): Standard digital pre-order "
    "(pay before release) is treated as an abstract business method and is not patentable on its own. "
    "No active patent was found covering a high-yield savings (HYSA) pre-order that automates refunds "
    "and game discounts as one combined system. Publishers already capture time-value-of-money on "
    "deposits via escrow-style holds; separate fintech save-to-buy patents may apply to dedicated "
    "savings accounts but do not block a normal platform pre-order feature. A HYSA-linked discount "
    "model would still need bank partnership and regulatory review if pursued later. "
    "Resolution: waive patent block for standard pre-ordering; re-open only if savings-yield mechanics are added."
)

REPO_ROOT = Path(__file__).resolve().parents[1]
SCHEMA_PATH = REPO_ROOT / "continuum_cave_saurce_schema.sql"
DEFAULT_FOUNDATION_ID = "safe-crypto-foundation-default"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_cave_commerce_tables(conn: sqlite3.Connection) -> None:
    if SCHEMA_PATH.exists():
        conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    conn.commit()
    _seed_builtin_legal(conn)
    _seed_foundation(conn)


def _seed_builtin_legal(conn: sqlite3.Connection) -> None:
    now = _now()
    cur = conn.execute("SELECT 1 FROM legal_cases WHERE id = ?", (BUILTIN_PREORDER_CASE_ID,))
    if not cur.fetchone():
        conn.execute(
            """INSERT INTO legal_cases
               (id, slug, title, category, status, severity, is_built_in, feature_key,
                description, patent_refs_json, opened_at, closed_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                BUILTIN_PREORDER_CASE_ID,
                "platform-preorder-patent-review",
                "Preorder feature — patent clearance",
                "patent",
                "closed",
                "low",
                1,
                PLATFORM_PREORDER_FEATURE,
                "Track whether offering preordering as a platform feature infringes existing patents.",
                json.dumps([]),
                now,
                now,
            ),
        )
    cur = conn.execute(
        "SELECT 1 FROM platform_feature_gates WHERE feature_key = ?",
        (PLATFORM_PREORDER_FEATURE,),
    )
    if not cur.fetchone():
        conn.execute(
            """INSERT INTO platform_feature_gates (feature_key, legal_case_id, status, updated_at)
               VALUES (?, ?, ?, ?)""",
            (PLATFORM_PREORDER_FEATURE, BUILTIN_PREORDER_CASE_ID, "cleared", now),
        )
    _apply_preorder_patent_clearance(conn)


def _apply_preorder_patent_clearance(conn: sqlite3.Connection) -> None:
    """Idempotent: record waiver resolution, close built-in case, clear pre-order gate."""
    now = _now()
    cur = conn.execute(
        "SELECT 1 FROM legal_resolutions WHERE id = ?",
        (BUILTIN_PREORDER_RESOLUTION_ID,),
    )
    if not cur.fetchone():
        conn.execute(
            """INSERT INTO legal_resolutions
               (id, case_id, summary, resolution_type, resolved_at, resolved_by, effective_date, document_refs_json)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                BUILTIN_PREORDER_RESOLUTION_ID,
                BUILTIN_PREORDER_CASE_ID,
                PREORDER_PATENT_RESOLUTION_SUMMARY,
                "waiver",
                now,
                "continuum-legal-seed",
                now[:10],
                json.dumps([{"source": "gemini-industry-review", "topic": "preorder-patent-clearance"}]),
            ),
        )
    conn.execute(
        """UPDATE legal_cases
           SET status = 'closed', severity = 'low', closed_at = COALESCE(closed_at, ?)
           WHERE id = ? AND is_built_in = 1""",
        (now, BUILTIN_PREORDER_CASE_ID),
    )
    conn.execute(
        """UPDATE platform_feature_gates SET status = 'cleared', updated_at = ?
           WHERE feature_key = ?""",
        (now, PLATFORM_PREORDER_FEATURE),
    )
    conn.commit()


def _seed_foundation(conn: sqlite3.Connection) -> None:
    now = _now()
    cur = conn.execute("SELECT 1 FROM saurce_safe_crypto_foundations WHERE id = ?", (DEFAULT_FOUNDATION_ID,))
    if not cur.fetchone():
        conn.execute(
            """INSERT INTO saurce_safe_crypto_foundations (id, name, wallet_id, asset, created_at)
               VALUES (?, ?, ?, ?, ?)""",
            (DEFAULT_FOUNDATION_ID, "Saurce Safe Crypto Foundation", "foundation-wallet-1", "USDC", now),
        )
        conn.commit()


def new_id() -> str:
    return str(uuid.uuid4())
