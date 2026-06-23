"""Resolve language codes to continuum DB language ids."""

from __future__ import annotations

import sqlite3
import uuid
from typing import Iterable

DEFAULT_LANGUAGE_CODES = ("en", "fr", "it", "de", "es")


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
        return row["id"]
    if not create:
        return None
    lang_id = str(uuid.uuid4())
    _insert_language(conn, lang_id, code_norm)
    return lang_id


def _insert_language(conn: sqlite3.Connection, lang_id: str, code: str) -> None:
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


def ensure_language_codes(conn: sqlite3.Connection, codes: Iterable[str]) -> None:
    for code in codes:
        if (code or "").strip():
            resolve_language_id(conn, code, create=True)


def ensure_default_languages(conn: sqlite3.Connection) -> None:
    """Seed common translation languages used by the lemma library UI."""
    ensure_language_codes(conn, DEFAULT_LANGUAGE_CODES)
    try:
        from continuum_api.lemma_merge import load_builtin_vocabulary
    except ImportError:
        return
    builtin_codes = {
        normalize_language_code(item.get("languageCode") or "en")
        for item in load_builtin_vocabulary()
    }
    ensure_language_codes(conn, builtin_codes)
