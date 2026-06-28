"""Bootstrap resaurce/saurce/cave tables and built-in legal seeds."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE

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
                description, patent_refs_json, opened_at)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                BUILTIN_PREORDER_CASE_ID,
                "platform-preorder-patent-review",
                "Preorder feature — patent clearance",
                "patent",
                "open",
                "high",
                1,
                PLATFORM_PREORDER_FEATURE,
                "Track whether offering preordering as a platform feature infringes existing patents.",
                json.dumps([]),
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
            (PLATFORM_PREORDER_FEATURE, BUILTIN_PREORDER_CASE_ID, "blocked", now),
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
