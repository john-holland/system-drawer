"""Resolve language codes to continuuuum DB language ids."""

from __future__ import annotations

import sqlite3
import uuid
from typing import Iterable

DEFAULT_LANGUAGE_CODES = ("en", "fr", "it", "de", "es", "ja", "ko", "zh")

MORPHOLOGY_RULES_REF = {
    "es": "es_v1",
    "fr": "fr_v1",
    "ja": "ja_v1",
    "ko": "ko_v1",
    "zh": "zh_v1",
}


def normalize_language_code(code: str | None) -> str:
    return (code or "en").strip().lower() or "en"


def resolve_language_id(conn: sqlite3.Connection, code: str, *, create: bool = True) -> str | None:
    """Look up a language id by code (case-insensitive). Optionally insert missing rows."""
    code_norm = normalize_language_code(code)
    cur = conn.execute(
        "SELECT id FROM languages WHERE LOWER(code) = ? LIMIT 1",
        (code_norm,),
    )
    row = cur.fetchone()
    if row:
        lang_id = row["id"]
        _ensure_morphology_ref(conn, lang_id, code_norm)
        return lang_id
    if not create:
        return None
    lang_id = str(uuid.uuid4())
    _insert_language(conn, lang_id, code_norm)
    return lang_id


def _ensure_morphology_ref(conn: sqlite3.Connection, lang_id: str, code: str) -> None:
    ref = MORPHOLOGY_RULES_REF.get(code)
    if not ref:
        return
    try:
        conn.execute(
            """UPDATE languages SET morphology_rules_ref = ?
               WHERE id = ? AND (morphology_rules_ref IS NULL OR morphology_rules_ref = '')""",
            (ref, lang_id),
        )
    except sqlite3.OperationalError:
        pass


def _insert_language(conn: sqlite3.Connection, lang_id: str, code: str) -> None:
    ref = MORPHOLOGY_RULES_REF.get(code)
    try:
        conn.execute(
            """INSERT INTO languages (id, code, name, script_direction, morphology_rules_ref)
               VALUES (?, ?, ?, 'ltr', ?)""",
            (lang_id, code, code, ref),
        )
    except sqlite3.OperationalError:
        try:
            conn.execute(
                "INSERT INTO languages (id, code, name, script_direction) VALUES (?, ?, ?, 'ltr')",
                (lang_id, code, code),
            )
        except sqlite3.OperationalError:
            conn.execute(
                "INSERT INTO languages (id, code) VALUES (?, ?)",
                (lang_id, code),
            )
        _ensure_morphology_ref(conn, lang_id, code)


def ensure_language_codes(conn: sqlite3.Connection, codes: Iterable[str]) -> None:
    for code in codes:
        if (code or "").strip():
            resolve_language_id(conn, code, create=True)


def ensure_default_languages(conn: sqlite3.Connection) -> None:
    """Seed common translation languages used by the lemma library UI."""
    ensure_language_codes(conn, DEFAULT_LANGUAGE_CODES)
    try:
        from continuuuum_api.lemma_merge import load_builtin_vocabulary
    except ImportError:
        return
    builtin_codes = {
        normalize_language_code(item.get("languageCode") or "en")
        for item in load_builtin_vocabulary()
    }
    ensure_language_codes(conn, builtin_codes)
