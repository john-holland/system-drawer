"""Mayor Dog Mods schema bootstrap and manifest helpers."""

from __future__ import annotations

import hashlib
import json
import re
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[1]
MODS_SCHEMA = REPO_ROOT / "continuum_mayor_dog_mods_schema.sql"

M_SLOT_RE = re.compile(r"\{\{?M:([^}|]+)(?:\|([^}]+))?\}?\}?|\{M:([^}|]+)(?:\|([^}]+))?\}", re.IGNORECASE)
MOD_SCHEMA_VERSION = 1


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _new_id(prefix: str = "mdmod") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def ensure_mayor_dog_mods_schema(conn: sqlite3.Connection) -> None:
    if MODS_SCHEMA.is_file():
        conn.executescript(MODS_SCHEMA.read_text(encoding="utf-8"))
        conn.commit()


def compute_source_hash(text: str, char_start: int, char_end: int) -> str:
    snippet = (text or "")[max(0, char_start) : max(char_start, char_end)]
    return hashlib.sha256(snippet.encode("utf-8")).hexdigest()[:16]


def slugify_slot_key(label: str, prefix: str = "slot") -> str:
    base = re.sub(r"[^a-z0-9]+", "-", (label or prefix).lower()).strip("-") or prefix
    return f"{base}-{uuid.uuid4().hex[:6]}"


def _row_target(row: sqlite3.Row) -> dict[str, Any]:
    d = dict(row)
    return {
        "id": d["id"],
        "targetKind": d["target_kind"],
        "entryId": d.get("entry_id"),
        "draftEpisodeId": d.get("draft_episode_id"),
        "compositionChildIndex": d.get("composition_child_index"),
        "charStart": d.get("char_start", 0),
        "charEnd": d.get("char_end", 0),
        "fareyLeft": d.get("farey_left"),
        "fareyRight": d.get("farey_right"),
        "slotKey": d["slot_key"],
        "label": d.get("label"),
        "description": d.get("description"),
        "sourceHash": d.get("source_hash"),
        "createdAt": d.get("created_at"),
        "updatedAt": d.get("updated_at"),
    }


def list_moddable_targets(
    conn: sqlite3.Connection,
    *,
    entry_id: str | None = None,
    draft_episode_id: str | None = None,
    target_kind: str | None = None,
) -> list[dict[str, Any]]:
    ensure_mayor_dog_mods_schema(conn)
    clauses: list[str] = []
    params: list[Any] = []
    if entry_id:
        clauses.append("entry_id = ?")
        params.append(entry_id)
    if draft_episode_id:
        clauses.append("draft_episode_id = ?")
        params.append(draft_episode_id)
    if target_kind:
        clauses.append("target_kind = ?")
        params.append(target_kind)
    where = f" WHERE {' AND '.join(clauses)}" if clauses else ""
    rows = conn.execute(
        f"SELECT * FROM moddable_targets{where} ORDER BY slot_key",
        params,
    ).fetchall()
    return [_row_target(r) for r in rows]


def upsert_moddable_target(conn: sqlite3.Connection, body: dict[str, Any]) -> dict[str, Any]:
    ensure_mayor_dog_mods_schema(conn)
    now = _now()
    target_id = body.get("id") or _new_id("mtgt")
    slot_key = (body.get("slotKey") or body.get("slot_key") or "").strip()
    if not slot_key:
        slot_key = slugify_slot_key(body.get("label") or body.get("targetKind") or "mod")
    label = body.get("label") or slot_key
    target_kind = body.get("targetKind") or body.get("target_kind") or "lemma_prompt"
    char_start = int(body.get("charStart") or body.get("char_start") or 0)
    char_end = int(body.get("charEnd") or body.get("char_end") or 0)
    source_text = body.get("sourceText") or body.get("source_text") or ""
    source_hash = body.get("sourceHash") or body.get("source_hash")
    if source_text and char_end > char_start:
        source_hash = source_hash or compute_source_hash(source_text, char_start, char_end)

    existing = conn.execute("SELECT id FROM moddable_targets WHERE id = ?", (target_id,)).fetchone()
    fields = (
        target_kind,
        body.get("entryId") or body.get("entry_id"),
        body.get("draftEpisodeId") or body.get("draft_episode_id"),
        body.get("compositionChildIndex") or body.get("composition_child_index"),
        char_start,
        char_end,
        body.get("fareyLeft") or body.get("farey_left"),
        body.get("fareyRight") or body.get("farey_right"),
        slot_key,
        label,
        body.get("description"),
        source_hash,
        now,
    )
    if existing:
        conn.execute(
            """UPDATE moddable_targets SET
                target_kind = ?, entry_id = ?, draft_episode_id = ?, composition_child_index = ?,
                char_start = ?, char_end = ?, farey_left = ?, farey_right = ?,
                slot_key = ?, label = ?, description = ?, source_hash = ?, updated_at = ?
               WHERE id = ?""",
            (*fields, target_id),
        )
    else:
        conn.execute(
            """INSERT INTO moddable_targets (
                id, target_kind, entry_id, draft_episode_id, composition_child_index,
                char_start, char_end, farey_left, farey_right, slot_key, label, description,
                source_hash, created_at, updated_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (target_id, *fields, now),
        )
    conn.commit()
    row = conn.execute("SELECT * FROM moddable_targets WHERE id = ?", (target_id,)).fetchone()
    return _row_target(row)


def delete_moddable_target(conn: sqlite3.Connection, target_id: str) -> bool:
    ensure_mayor_dog_mods_schema(conn)
    cur = conn.execute("DELETE FROM moddable_targets WHERE id = ?", (target_id,))
    conn.commit()
    return cur.rowcount > 0


def build_mod_context_from_manifest(manifest: dict[str, Any]) -> dict[str, Any]:
    lemma_by_slot: dict[str, dict[str, Any]] = {}
    for item in manifest.get("lemmaOverrides") or []:
        key = item.get("slotKey") or item.get("slot_key")
        if not key:
            continue
        existing = lemma_by_slot.get(key)
        if existing and int(existing.get("priority", 0)) > int(item.get("priority", 0)):
            continue
        lemma_by_slot[key] = item
    episode_by_target: dict[str, dict[str, Any]] = {}
    for item in manifest.get("episodeOverrides") or []:
        tid = item.get("targetId") or item.get("target_id")
        if not tid:
            continue
        existing = episode_by_target.get(tid)
        if existing and int(existing.get("priority", 0)) > int(item.get("priority", 0)):
            continue
        episode_by_target[tid] = item
    ctx: dict[str, Any] = {"lemmaBySlot": lemma_by_slot, "episodeByTarget": episode_by_target}
    targets = manifest.get("targetsById")
    if targets:
        ctx["_targetsById"] = targets
    return ctx


def build_bootstrap_manifest(
    conn: sqlite3.Connection,
    *,
    user_id: str | None = None,
    episode_id: str | None = None,
) -> dict[str, Any]:
    ensure_mayor_dog_mods_schema(conn)
    now = _now()
    packages: list[dict[str, Any]] = []
    lemma_overrides: list[dict[str, Any]] = []
    episode_overrides: list[dict[str, Any]] = []
    portal_skins: dict[str, Any] = {}

    if user_id and user_id != "anonymous":
        rows = conn.execute(
            """SELECT p.*, e.priority
               FROM user_enabled_mods e
               JOIN mod_packages p ON p.id = e.mod_package_id
               WHERE e.user_id = ? AND p.status = 'published'
               ORDER BY e.priority ASC, e.enabled_at ASC""",
            (user_id,),
        ).fetchall()
    else:
        rows = conn.execute(
            """SELECT p.*, 0 AS priority FROM mod_packages p
               WHERE p.status = 'published'
               ORDER BY p.published_at DESC LIMIT 20"""
        ).fetchall()

    seen_mods: set[str] = set()
    for row in rows:
        pkg = dict(row)
        mod_row = conn.execute("SELECT * FROM mayor_dog_mods WHERE id = ?", (pkg["mod_id"],)).fetchone()
        if not mod_row or mod_row["status"] != "published":
            continue
        mod_id = mod_row["id"]
        packages.append(
            {
                "packageId": pkg["id"],
                "modId": mod_id,
                "slug": mod_row["slug"],
                "displayName": mod_row["display_name"],
                "version": pkg["version"],
                "priority": pkg.get("priority", 0),
            }
        )
        if mod_id not in seen_mods:
            seen_mods.add(mod_id)
            skin = conn.execute(
                "SELECT * FROM mod_portal_usc_sets WHERE mod_id = ?", (mod_id,)
            ).fetchone()
            if skin:
                portal_skins[mod_row["slug"]] = {
                    "libraryDocumentIds": json.loads(skin["library_document_ids_json"] or "[]"),
                    "settings": json.loads(skin["settings_json"] or "{}") if skin["settings_json"] else {},
                }

        priority = int(pkg.get("priority") or 0)
        for lo in conn.execute(
            """SELECT lo.*, t.slot_key, t.target_kind, t.entry_id
               FROM mod_lemma_overrides lo
               JOIN moddable_targets t ON t.id = lo.target_id
               WHERE lo.package_id = ?""",
            (pkg["id"],),
        ).fetchall():
            lemma_overrides.append(
                {
                    "packageId": pkg["id"],
                    "targetId": lo["target_id"],
                    "slotKey": lo["slot_key"],
                    "targetKind": lo["target_kind"],
                    "entryId": lo["entry_id"],
                    "overrideText": lo["override_text"],
                    "patchProperties": json.loads(lo["patch_properties_json"] or "{}"),
                    "compositionPatch": json.loads(lo["composition_patch_json"] or "{}"),
                    "priority": priority,
                }
            )
        for eo in conn.execute(
            """SELECT eo.*, t.slot_key, t.draft_episode_id, t.char_start, t.char_end
               FROM mod_episode_overrides eo
               JOIN moddable_targets t ON t.id = eo.target_id
               WHERE eo.package_id = ?""",
            (pkg["id"],),
        ).fetchall():
            if episode_id and eo["draft_episode_id"] and eo["draft_episode_id"] != episode_id:
                continue
            episode_overrides.append(
                {
                    "packageId": pkg["id"],
                    "targetId": eo["target_id"],
                    "slotKey": eo["slot_key"],
                    "draftEpisodeId": eo["draft_episode_id"],
                    "charStart": eo["char_start"],
                    "charEnd": eo["char_end"],
                    "overrideText": eo["override_text"],
                    "sectionMetadata": json.loads(eo["section_metadata_json"] or "{}"),
                    "priority": priority,
                }
            )

    lemma_overrides.sort(key=lambda x: (x.get("slotKey", ""), x.get("priority", 0)))
    episode_overrides.sort(key=lambda x: (x.get("targetId", ""), x.get("priority", 0)))

    return {
        "schemaVersion": MOD_SCHEMA_VERSION,
        "cachedAt": now,
        "episodeId": episode_id,
        "userId": user_id,
        "packages": packages,
        "lemmaOverrides": lemma_overrides,
        "episodeOverrides": episode_overrides,
        "portalSkins": portal_skins,
    }


def resolve_mod_placeholders(text: str, mod_context: dict[str, Any] | None) -> str:
    if not text or not mod_context:
        return text or ""
    lemma_by_slot = mod_context.get("lemmaBySlot") or {}

    def repl(m: re.Match[str]) -> str:
        key = (m.group(1) or m.group(3) or "").strip()
        entry = lemma_by_slot.get(key)
        if entry and entry.get("overrideText"):
            return str(entry["overrideText"])
        return m.group(0)

    return M_SLOT_RE.sub(repl, text)


def apply_episode_mod_overrides(
    script_text: str,
    mod_context: dict[str, Any] | None,
    *,
    draft_episode_id: str | None = None,
) -> str:
    if not script_text or not mod_context:
        return script_text or ""
    episode_by_target = mod_context.get("episodeByTarget") or {}
    if not episode_by_target:
        return script_text
    ensure = script_text
    conn_targets = mod_context.get("_targetsById")
    if not conn_targets:
        return ensure
    parts: list[tuple[int, int, str]] = []
    for tid, override in episode_by_target.items():
        meta = conn_targets.get(tid)
        if not meta:
            continue
        if draft_episode_id and meta.get("draftEpisodeId") and meta["draftEpisodeId"] != draft_episode_id:
            continue
        cs = int(meta.get("charStart") or 0)
        ce = int(meta.get("charEnd") or 0)
        text = override.get("overrideText") or ""
        if ce > cs and text:
            parts.append((cs, ce, text))
    if not parts:
        return ensure
    parts.sort(key=lambda p: p[0], reverse=True)
    out = ensure
    for cs, ce, text in parts:
        out = out[:cs] + text + out[ce:]
    return out


def audit_mayor_dog_mod_sections(
    conn: sqlite3.Connection,
    draft_episode_id: str,
    old_text: str,
    new_text: str,
) -> list[Any]:
    """Return DiffItem list for edits overlapping moddable targets."""
    from thesaurus.script_edit_diff import DiffItem, compute_edit_regions, parse_mod_spans

    ensure_mayor_dog_mods_schema(conn)
    targets = list_moddable_targets(conn, draft_episode_id=draft_episode_id)
    episode_targets = [t for t in targets if t.get("targetKind") == "episode_section"]
    all_targets = episode_targets

    if not all_targets:
        return []

    regions = compute_edit_regions(old_text or "", new_text or "")
    if not regions:
        mod_token_changed = {s.label for s in parse_mod_spans(old_text)} != {
            s.label for s in parse_mod_spans(new_text)
        }
        if not mod_token_changed:
            return []

    required: list[DiffItem] = []
    for target in all_targets:
        cs = int(target.get("charStart") or 0)
        ce = int(target.get("charEnd") or 0)
        if ce <= cs:
            continue
        overlapped = False
        for edit in regions:
            edit_end = edit.offset + edit.old_len
            if edit_end > cs and edit.offset < ce:
                overlapped = True
                break
        if overlapped:
            required.append(
                DiffItem(
                    severity="required",
                    item_type="mayor_dog_mod_section_altered",
                    description=(
                        f"This edit modifies Mayor Dog Mod slot '{target.get('slotKey')}' "
                        f"({target.get('label') or target.get('targetKind')}); verify slot metadata."
                    ),
                    old_char_start=cs,
                    old_char_end=ce,
                    new_char_start=cs,
                    new_char_end=ce,
                )
            )

    mod_old = parse_mod_spans(old_text or "")
    mod_new = parse_mod_spans(new_text or "")
    if mod_old or mod_new:
        old_keys = {(s.char_start, s.char_end, s.label) for s in mod_old}
        new_keys = {(s.char_start, s.char_end, s.label) for s in mod_new}
        if old_keys != new_keys and not required:
            required.append(
                DiffItem(
                    severity="required",
                    item_type="mayor_dog_mod_section_altered",
                    description=(
                        "This edit alters {M:...} Mayor Dog Mod placeholders; "
                        "verify mod slots remain valid."
                    ),
                )
            )
    return required
