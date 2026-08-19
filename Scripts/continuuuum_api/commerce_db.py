"""Bootstrap resaurce/saurce/cave tables and built-in legal seeds."""

from __future__ import annotations

import json
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path

from cave_loader import BUILTIN_PREORDER_CASE_ID, PLATFORM_PREORDER_FEATURE

BUILTIN_PREORDER_RESOLUTION_ID = "00000000-0000-4000-8000-000000000002"

ROBLOX_MDL_3166_CASE_ID = "roblox-mdl-3166"
ROBLOX_AG_TEXAS_CASE_ID = "roblox-ag-texas"
ROBLOX_AG_KENTUCKY_CASE_ID = "roblox-ag-kentucky"
ROBLOX_AG_OHIO_CASE_ID = "roblox-ag-ohio"

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
SCHEMA_PATH = REPO_ROOT / "continuuuum_cave_saurce_schema.sql"
DEFAULT_FOUNDATION_ID = "safe-crypto-foundation-default"


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _ensure_column(conn: sqlite3.Connection, table: str, column: str, decl: str) -> None:
    cols = {r[1] for r in conn.execute(f"PRAGMA table_info({table})").fetchall()}
    if column not in cols:
        conn.execute(f"ALTER TABLE {table} ADD COLUMN {column} {decl}")


def ensure_cave_commerce_tables(conn: sqlite3.Connection) -> None:
    if SCHEMA_PATH.exists():
        conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))
    _ensure_column(conn, "legal_cases", "case_kind", "TEXT NOT NULL DEFAULT 'internal_agile'")
    _ensure_column(conn, "legal_cases", "external_metadata_json", "TEXT")
    conn.commit()
    _seed_builtin_legal(conn)
    _seed_external_litigation_docket(conn)
    _seed_foundation(conn)
    try:
        from continuuuum_api.chat_safety_db import ensure_chat_safety_tables
    except ImportError:
        from chat_safety_db import ensure_chat_safety_tables
    ensure_chat_safety_tables(conn)


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
                "continuuuum-legal-seed",
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


def _seed_external_case(
    conn: sqlite3.Connection,
    *,
    case_id: str,
    slug: str,
    title: str,
    category: str,
    description: str,
    metadata: dict,
    now: str,
) -> None:
    cur = conn.execute("SELECT 1 FROM legal_cases WHERE id = ?", (case_id,))
    if cur.fetchone():
        conn.execute(
            """UPDATE legal_cases
               SET case_kind = 'external_litigation',
                   external_metadata_json = COALESCE(external_metadata_json, ?)
               WHERE id = ?""",
            (json.dumps(metadata), case_id),
        )
        return
    conn.execute(
        """INSERT INTO legal_cases
           (id, slug, title, category, status, severity, is_built_in, feature_key,
            description, patent_refs_json, opened_at, case_kind, external_metadata_json)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            case_id,
            slug,
            title,
            category,
            "open",
            "high",
            1,
            None,
            description,
            json.dumps([]),
            now,
            "external_litigation",
            json.dumps(metadata),
        ),
    )


def _seed_docket_entry(
    conn: sqlite3.Connection,
    *,
    entry_id: str,
    case_id: str,
    title: str,
    summary: str,
    source_url: str,
    now: str,
) -> None:
    cur = conn.execute("SELECT 1 FROM legal_docket_entries WHERE id = ?", (entry_id,))
    if cur.fetchone():
        return
    conn.execute(
        """INSERT INTO legal_docket_entries
           (id, case_id, filed_at, entry_kind, title, summary, source_url, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
        (entry_id, case_id, now[:10], "manual", title, summary, source_url, now),
    )


def _seed_watchlist_item(
    conn: sqlite3.Connection,
    *,
    item_id: str,
    slug: str,
    title: str,
    jurisdiction: str,
    agency: str,
    notes: str,
    related_case_id: str | None,
    now: str,
) -> None:
    cur = conn.execute("SELECT 1 FROM legal_watchlist_items WHERE id = ?", (item_id,))
    if cur.fetchone():
        return
    conn.execute(
        """INSERT INTO legal_watchlist_items
           (id, slug, title, jurisdiction, agency, status, related_case_id, notes, source_url, created_at, updated_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (item_id, slug, title, jurisdiction, agency, "watching", related_case_id, notes, None, now, now),
    )


def _seed_code_line_ref(
    conn: sqlite3.Connection,
    *,
    ref_id: str,
    case_id: str,
    file_path: str,
    note: str,
    now: str,
) -> None:
    cur = conn.execute("SELECT 1 FROM legal_code_line_refs WHERE id = ?", (ref_id,))
    if cur.fetchone():
        return
    conn.execute(
        """INSERT INTO legal_code_line_refs
           (id, case_id, resolution_id, repo, file_path, start_line, end_line, branch, note, verified_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (ref_id, case_id, None, "Drawer 2", file_path, 1, 1, "main", note, now),
    )


def _seed_external_litigation_docket(conn: sqlite3.Connection) -> None:
    """Living docket for public Roblox chat/child-safety litigation. Not an agile gate."""
    now = _now()
    _seed_external_case(
        conn,
        case_id=ROBLOX_MDL_3166_CASE_ID,
        slug="roblox-mdl-3166",
        title="In re Roblox Corp. Child Sexual Exploitation Litigation (MDL 3166)",
        category="external_chat_safety",
        description=(
            "Public multi-district litigation in the Northern District of California alleging "
            "failure to protect children on a chat-enabled platform. Tracked as a living docket "
            "only — does not rate Continuuuum and does not gate editor/web tools."
        ),
        metadata={
            "mdlNumber": "3166",
            "court": "N.D. Cal.",
            "judge": "Richard Seeborg",
            "jurisdiction": "US-CA",
            "parties": ["Roblox Corporation", "putative class plaintiffs"],
            "pacerUrl": "https://www.cand.uscourts.gov/",
        },
        now=now,
    )
    _seed_external_case(
        conn,
        case_id=ROBLOX_AG_TEXAS_CASE_ID,
        slug="roblox-ag-texas",
        title="Texas Attorney General — Roblox child-safety / chat investigation",
        category="external_ag_action",
        description="Texas AG action related to child safety and online communication on Roblox.",
        metadata={
            "court": "Texas state / AG",
            "jurisdiction": "US-TX",
            "parties": ["State of Texas", "Roblox Corporation"],
            "agency": "Texas Attorney General",
        },
        now=now,
    )
    _seed_external_case(
        conn,
        case_id=ROBLOX_AG_KENTUCKY_CASE_ID,
        slug="roblox-ag-kentucky",
        title="Kentucky Attorney General — Roblox child-safety / chat investigation",
        category="external_ag_action",
        description="Kentucky AG action related to child safety and online communication on Roblox.",
        metadata={
            "court": "Kentucky state / AG",
            "jurisdiction": "US-KY",
            "parties": ["Commonwealth of Kentucky", "Roblox Corporation"],
            "agency": "Kentucky Attorney General",
        },
        now=now,
    )
    _seed_external_case(
        conn,
        case_id=ROBLOX_AG_OHIO_CASE_ID,
        slug="roblox-ag-ohio",
        title="Ohio Attorney General — Roblox child-safety / chat investigation",
        category="external_ag_action",
        description="Ohio AG action related to child safety and online communication on Roblox.",
        metadata={
            "court": "Ohio state / AG",
            "jurisdiction": "US-OH",
            "parties": ["State of Ohio", "Roblox Corporation"],
            "agency": "Ohio Attorney General",
        },
        now=now,
    )
    _seed_docket_entry(
        conn,
        entry_id="roblox-mdl-3166-seed-transfer",
        case_id=ROBLOX_MDL_3166_CASE_ID,
        title="MDL 3166 docket opened (manual seed)",
        summary="Manual living-docket seed. No PACER scrape. Update filings by hand.",
        source_url="https://www.cand.uscourts.gov/",
        now=now,
    )
    _seed_watchlist_item(
        conn,
        item_id="roblox-ag-louisiana",
        slug="roblox-ag-louisiana",
        title="Louisiana AG — Roblox / child-safety watch",
        jurisdiction="US-LA",
        agency="Louisiana Attorney General",
        notes="Watch item; not a Continuuuum rating or feature gate.",
        related_case_id=ROBLOX_MDL_3166_CASE_ID,
        now=now,
    )
    _seed_watchlist_item(
        conn,
        item_id="roblox-ag-florida",
        slug="roblox-ag-florida",
        title="Florida AG — Roblox / child-safety watch",
        jurisdiction="US-FL",
        agency="Florida Attorney General",
        notes="Watch item; not a Continuuuum rating or feature gate.",
        related_case_id=ROBLOX_MDL_3166_CASE_ID,
        now=now,
    )
    _seed_watchlist_item(
        conn,
        item_id="roblox-ag-arkansas",
        slug="roblox-ag-arkansas",
        title="Arkansas AG — Roblox / child-safety watch",
        jurisdiction="US-AR",
        agency="Arkansas Attorney General",
        notes="Watch item; not a Continuuuum rating or feature gate.",
        related_case_id=ROBLOX_MDL_3166_CASE_ID,
        now=now,
    )
    _seed_watchlist_item(
        conn,
        item_id="roblox-la-county",
        slug="roblox-la-county",
        title="Los Angeles County — Roblox / child-safety watch",
        jurisdiction="US-CA-LA",
        agency="Los Angeles County",
        notes="County-level watch item; not a Continuuuum rating or feature gate.",
        related_case_id=ROBLOX_MDL_3166_CASE_ID,
        now=now,
    )
    _seed_code_line_ref(
        conn,
        ref_id="roblox-mdl-3166-ref-structured-chat",
        case_id=ROBLOX_MDL_3166_CASE_ID,
        file_path="Assets/SystemDrawer/Networking/StructuredChatChannel.cs",
        note="Unity structured multiplayer chat (game option). Not Continuuuum web/editor chat.",
        now=now,
    )
    _seed_code_line_ref(
        conn,
        ref_id="roblox-mdl-3166-ref-entitlement",
        case_id=ROBLOX_MDL_3166_CASE_ID,
        file_path="Scripts/continuuuum_api/chat_safety_routes.py",
        note="Chat entitlement, TOS, convenience fee, and jurisdiction gate.",
        now=now,
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
