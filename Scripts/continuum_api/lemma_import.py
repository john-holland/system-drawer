"""CSV/TSV bulk import and default-property parsing for lemma library."""

from __future__ import annotations

import csv
import io
import re
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any

try:
    from continuum_api.lemma_merge import (
        BUILTIN_URN_PREFIX,
        find_builtin_entries_for_term,
        find_builtin_entry,
        is_builtin_urn,
    )
except ImportError:
    from lemma_merge import (
        BUILTIN_URN_PREFIX,
        find_builtin_entries_for_term,
        find_builtin_entry,
        is_builtin_urn,
    )

try:
    from thesaurus.pos_tags import normalize_pos_tag
except ImportError:
    from pos_tags import normalize_pos_tag

P_PROMPT_RE = re.compile(r"\{\{?P:([^}|]+)(?:\|([^}]+))?\}?\}?|\{P:([^}|]+)(?:\|([^}]+))?\}", re.IGNORECASE)

COLUMN_ALIASES: dict[str, str] = {
    "word": "word",
    "term": "word",
    "lemma": "word",
    "description": "description",
    "def": "description",
    "definition": "description",
    "synonyms": "synonyms",
    "synonym": "synonyms",
    "language": "language",
    "lang": "language",
    "part of speech": "partOfSpeech",
    "part_of_speech": "partOfSpeech",
    "pos": "partOfSpeech",
    "prefab id": "prefabId",
    "prefab_id": "prefabId",
    "prefabid": "prefabId",
    "default properties": "defaultProperties",
    "default_properties": "defaultProperties",
    "properties": "defaultProperties",
}


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def parse_default_properties(raw: str | dict | None) -> dict[str, str]:
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return {str(k): str(v) for k, v in raw.items()}
    text = str(raw).strip()
    if not text:
        return {}
    out: dict[str, str] = {}
    for m in P_PROMPT_RE.finditer(text):
        params = m.group(2) or m.group(4) or ""
        for part in params.split("|"):
            part = part.strip()
            if not part or "=" not in part:
                continue
            k, _, v = part.partition("=")
            out[k.strip()] = v.strip()
    if not out and "=" in text:
        for part in re.split(r"[;,|]", text):
            part = part.strip()
            if "=" in part:
                k, _, v = part.partition("=")
                out[k.strip()] = v.strip()
    return out


def normalize_header(h: str) -> str | None:
    key = (h or "").strip().lower()
    return COLUMN_ALIASES.get(key)


def detect_delimiter(sample: str, fmt: str | None) -> str:
    if fmt == "tsv":
        return "\t"
    if fmt == "csv":
        return ","
    try:
        dialect = csv.Sniffer().sniff(sample[:4096], delimiters=",\t;")
        return dialect.delimiter
    except csv.Error:
        return "\t" if "\t" in sample and sample.count("\t") > sample.count(",") else ","


def parse_tabular_file(
    content: bytes | str,
    fmt: str | None = None,
    column_map: dict[str, str] | None = None,
) -> tuple[list[str], list[dict[str, str]]]:
    text = content.decode("utf-8-sig") if isinstance(content, bytes) else content
    delim = detect_delimiter(text, fmt)
    reader = csv.reader(io.StringIO(text), delimiter=delim)
    rows = list(reader)
    if not rows:
        return [], []

    headers = [normalize_header(h) or h.strip() for h in rows[0]]
    if column_map:
        inv = {v: k for k, v in column_map.items()}
        headers = [column_map.get(h, h) if h in column_map else inv.get(h, h) for h in rows[0]]

    # Single column with no header match -> word-only
    if len(rows[0]) == 1 and headers[0] not in COLUMN_ALIASES.values():
        headers = ["word"]
        data_rows = rows
    else:
        data_rows = rows[1:]

    parsed: list[dict[str, str]] = []
    for row in data_rows:
        if not row or all(not (c or "").strip() for c in row):
            continue
        item: dict[str, str] = {}
        for i, val in enumerate(row):
            if i >= len(headers):
                break
            field = headers[i]
            if field in ("word", "description", "synonyms", "language", "partOfSpeech", "prefabId", "defaultProperties"):
                item[field] = (val or "").strip()
            elif field == headers[i] and field == "word":
                item["word"] = (val or "").strip()
        if not item.get("word") and row:
            item["word"] = row[0].strip()
        if item.get("word"):
            parsed.append(item)
    return headers, parsed


def _resolve_language_id(conn: sqlite3.Connection, code: str) -> str:
    try:
        from thesaurus.language_resolver import resolve_language_id
    except ImportError:
        from language_resolver import resolve_language_id
    lang_id = resolve_language_id(conn, code, create=True)
    if not lang_id:
        code = (code or "en").strip().lower() or "en"
        lang_id = str(uuid.uuid4())
        conn.execute(
            "INSERT INTO languages (id, code, name, script_direction) VALUES (?, ?, ?, 'ltr')",
            (lang_id, code, code),
        )
    return lang_id


def _valid_property_keys(conn: sqlite3.Connection) -> set[str]:
    try:
        cur = conn.execute("SELECT key FROM localization_property_specs")
        keys = {r["key"] for r in cur.fetchall()}
        keys.add("prefab-id")
        return keys
    except sqlite3.OperationalError:
        return {"prefab-id", "non-ik-animation"}


def upsert_lemma_row(
    conn: sqlite3.Connection,
    row: dict[str, Any],
    valid_keys: set[str],
) -> tuple[str, str | None, str | None]:
    """Returns (status, error, entry_id)."""
    word = (row.get("word") or "").strip()
    if not word:
        return "skipped", "missing word", None
    if is_builtin_urn(word) or word.startswith(BUILTIN_URN_PREFIX):
        return "skipped", "cannot create built-in URN as custom entry", None

    language = (row.get("language") or "en").strip().lower()
    pos = normalize_pos_tag(row.get("partOfSpeech") or row.get("pos"))
    language_id = _resolve_language_id(conn, language)

    cur = conn.execute(
        """
        SELECT id FROM thesaurus_entries
        WHERE language_id = ? AND term = ? AND pos_tag = ?
        """,
        (language_id, word, pos),
    )
    existing = cur.fetchone()
    now = _now()

    if existing:
        entry_id = existing["id"]
        if is_builtin_urn(entry_id):
            return "skipped", "matches built-in entry", entry_id
        status = "updated"
    else:
        builtin = find_builtin_entry(word, language, pos)
        if builtin:
            return "skipped", "matches built-in entry", builtin["id"]
        homographs = find_builtin_entries_for_term(word, language)
        if homographs:
            preferred = next(
                (h for h in homographs if (h.get("posTag") or "").lower() == (pos or "")),
                homographs[0],
            )
            return "skipped", "matches built-in entry", preferred["id"]
        entry_id = str(uuid.uuid4())
        conn.execute(
            "INSERT INTO thesaurus_entries (id, language_id, term, pos_tag) VALUES (?, ?, ?, ?)",
            (entry_id, language_id, word, pos),
        )
        status = "created"

    desc = (row.get("description") or "").strip()
    if desc:
        conn.execute(
            """
            INSERT INTO dictionary_definitions (id, entry_id, language_id, definition, source, created_at)
            VALUES (?, ?, ?, ?, 'import', ?)
            """,
            (str(uuid.uuid4()), entry_id, language_id, desc, now),
        )

    syns = row.get("synonyms") or row.get("synonym") or ""
    if syns:
        conn.execute("DELETE FROM thesaurus_alternatives WHERE entry_id = ? AND role = 'synonym'", (entry_id,))
        for syn in re.split(r"[|;,]", str(syns)):
            syn = syn.strip()
            if syn:
                conn.execute(
                    """
                    INSERT INTO thesaurus_alternatives (id, entry_id, pos_tag, form, role)
                    VALUES (?, ?, ?, ?, 'synonym')
                    """,
                    (str(uuid.uuid4()), entry_id, pos, syn),
                )

    props = parse_default_properties(row.get("defaultProperties"))
    prefab = (row.get("prefabId") or row.get("prefab_id") or "").strip()
    if prefab:
        props["prefab-id"] = prefab

    for key, val in props.items():
        if key not in valid_keys:
            continue
        conn.execute(
            """
            INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value)
            VALUES (?, ?, ?)
            ON CONFLICT(entry_id, property_key) DO UPDATE SET property_value = excluded.property_value
            """,
            (entry_id, key, str(val)),
        )

    return status, None, entry_id


def import_rows(conn: sqlite3.Connection, rows: list[dict[str, Any]]) -> dict[str, Any]:
    valid_keys = _valid_property_keys(conn)
    created = updated = skipped = 0
    errors: list[dict[str, Any]] = []
    for i, row in enumerate(rows):
        try:
            status, err, _eid = upsert_lemma_row(conn, row, valid_keys)
            if err:
                skipped += 1
                errors.append({"row": i + 1, "error": err, "word": row.get("word")})
            elif status == "created":
                created += 1
            elif status == "updated":
                updated += 1
            else:
                skipped += 1
        except sqlite3.Error as e:
            skipped += 1
            errors.append({"row": i + 1, "error": str(e), "word": row.get("word")})
    conn.commit()
    return {"created": created, "updated": updated, "skipped": skipped, "errors": errors}
