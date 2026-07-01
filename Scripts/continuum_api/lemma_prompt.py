"""Recursive lemma prompt expansion, web overlays, and prompt bundle CRUD."""

from __future__ import annotations

import json
import re
import sqlite3
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

_SCHEMA_ROOT = Path(__file__).resolve().parent.parent

P_PROMPT_RE = re.compile(
    r"\{\{?P:([^}|]+)(?:\|([^}]+))?\}?\}?|\{P:([^}|]+)(?:\|([^}]+))?\}",
    re.IGNORECASE,
)

try:
    from continuum_api.mod_db import build_mod_context_from_manifest, resolve_mod_placeholders
except ImportError:
    from mod_db import build_mod_context_from_manifest, resolve_mod_placeholders

SPATIAL_PROP_KEYS = (
    "spatial-center-x",
    "spatial-center-y",
    "spatial-center-z",
    "spatial-size-x",
    "spatial-size-y",
    "spatial-size-z",
    "spatial-t-min",
    "spatial-t-max",
    "spatial-4d-id",
)

try:
    from continuum_api.lemma_composition import (
        ensure_lemma_composition_schema,
        load_composition,
        replace_composition,
        validate_children,
        would_create_cycle,
    )
    from continuum_api.lemma_merge import is_builtin_urn, merge_vocabulary
except ImportError:
    from lemma_composition import (
        ensure_lemma_composition_schema,
        load_composition,
        replace_composition,
        validate_children,
        would_create_cycle,
    )
    from lemma_merge import is_builtin_urn, merge_vocabulary

try:
    from continuum_api.lemma_composition_spatial import upsert_entry_spatial
except ImportError:
    from lemma_composition_spatial import upsert_entry_spatial


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _table_exists(conn: sqlite3.Connection, name: str) -> bool:
    cur = conn.execute(
        "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
        (name,),
    )
    return cur.fetchone() is not None


def _column_exists(conn: sqlite3.Connection, table: str, column: str) -> bool:
    cur = conn.execute(f"PRAGMA table_info({table})")
    return any(r[1] == column for r in cur.fetchall())


def ensure_lemma_prompt_schema(conn: sqlite3.Connection) -> None:
    """Idempotent schema for overlays, property specs, and composition patch columns."""
    ensure_lemma_composition_schema(conn)
    if not _table_exists(conn, "thesaurus_lemma_overlays"):
        sql = (_SCHEMA_ROOT / "continuum_lemma_prompt_schema.sql").read_text(encoding="utf-8")
        conn.executescript(sql)
    else:
        needs_prompt_specs = not _table_exists(conn, "localization_property_specs")
        if not needs_prompt_specs:
            cur = conn.execute(
                "SELECT 1 FROM localization_property_specs WHERE key = 'lemma-prompt' LIMIT 1"
            )
            needs_prompt_specs = not cur.fetchone()
        if needs_prompt_specs:
            if not _table_exists(conn, "localization_property_specs"):
                conn.execute(
                    """
                    CREATE TABLE IF NOT EXISTS localization_property_specs (
                        key TEXT PRIMARY KEY,
                        value_type TEXT NOT NULL,
                        allowed_values_json TEXT,
                        default_value TEXT,
                        description TEXT
                    )
                    """
                )
            sql = (_SCHEMA_ROOT / "continuum_lemma_prompt_schema.sql").read_text(encoding="utf-8")
            conn.executescript(sql)
    for col in ("patch_properties_json", "timing_override_json"):
        if not _column_exists(conn, "thesaurus_entry_compositions", col):
            conn.execute(f"ALTER TABLE thesaurus_entry_compositions ADD COLUMN {col} TEXT")
    conn.commit()


def _parse_json_obj(raw: str | dict | None) -> dict[str, Any]:
    if not raw:
        return {}
    if isinstance(raw, dict):
        return dict(raw)
    try:
        data = json.loads(raw)
        return data if isinstance(data, dict) else {}
    except json.JSONDecodeError:
        return {}


def _load_spec_defaults(conn: sqlite3.Connection) -> dict[str, str]:
    defaults: dict[str, str] = {}
    try:
        cur = conn.execute("SELECT key, default_value FROM localization_property_specs")
        for r in cur.fetchall():
            defaults[r["key"]] = r["default_value"] or ""
    except sqlite3.OperationalError:
        pass
    return defaults


def load_overlay(conn: sqlite3.Connection, entry_id: str) -> dict[str, Any] | None:
    ensure_lemma_prompt_schema(conn)
    cur = conn.execute(
        "SELECT * FROM thesaurus_lemma_overlays WHERE target_entry_id = ?",
        (entry_id,),
    )
    row = cur.fetchone()
    if not row:
        return None
    return dict(row)


def _entry_exists_in_vocab(conn: sqlite3.Connection, entry_id: str) -> bool:
    merged = merge_vocabulary(conn)
    return entry_id in merged


def _load_entry_properties(conn: sqlite3.Connection, entry_id: str) -> dict[str, str]:
    props: dict[str, str] = {}
    try:
        cur = conn.execute(
            "SELECT property_key, property_value FROM thesaurus_entry_properties WHERE entry_id = ?",
            (entry_id,),
        )
        for r in cur.fetchall():
            props[r["property_key"]] = r["property_value"]
    except sqlite3.OperationalError:
        pass
    return props


def _set_entry_property(conn: sqlite3.Connection, entry_id: str, key: str, value: str) -> None:
    if not value and key != "lemma-prompt":
        conn.execute(
            "DELETE FROM thesaurus_entry_properties WHERE entry_id = ? AND property_key = ?",
            (entry_id, key),
        )
        return
    conn.execute(
        """
        INSERT INTO thesaurus_entry_properties (entry_id, property_key, property_value)
        VALUES (?, ?, ?)
        ON CONFLICT(entry_id, property_key) DO UPDATE SET property_value = excluded.property_value
        """,
        (entry_id, key, value),
    )


def _synthesize_prompt_from_children(children: list[dict[str, Any]]) -> str:
    parts: list[str] = []
    for c in sorted(children, key=lambda x: int(x.get("sortOrder") or 0)):
        term = (c.get("term") or c.get("entryId") or "").strip()
        if term:
            parts.append(f"{{P:{term}}}")
    return " ".join(parts)


def _parse_placeholder_params(params_str: str | None) -> dict[str, str]:
    out: dict[str, str] = {}
    if not params_str:
        return out
    for part in params_str.split("|"):
        part = part.strip()
        if not part or "=" not in part:
            continue
        k, _, v = part.partition("=")
        out[k.strip()] = v.strip()
    return out


def _resolve_child_for_placeholder(
    name: str,
    children: list[dict[str, Any]],
    merged: dict[str, dict[str, Any]],
) -> tuple[str | None, dict[str, str] | None]:
    name_lc = name.strip().lower()
    for c in children:
        term = (c.get("term") or "").strip().lower()
        eid = (c.get("entryId") or "").strip()
        if term == name_lc or eid == name:
            patch = _parse_json_obj(c.get("patchProperties") or c.get("patch_properties_json"))
            return eid or None, patch or None
    for eid, view in merged.items():
        if (view.get("term") or "").strip().lower() == name_lc or eid == name:
            return eid, None
    return None, None


def _merge_properties(
    conn: sqlite3.Connection,
    entry_id: str,
    *,
    inline_params: dict[str, str] | None = None,
    child_patch: dict[str, str] | None = None,
    overlay_patch: dict[str, str] | None = None,
) -> dict[str, str]:
    spec_defaults = _load_spec_defaults(conn)
    merged_vocab = merge_vocabulary(conn)
    base: dict[str, str] = dict(spec_defaults)
    view = merged_vocab.get(entry_id) or {}
    base.update(view.get("properties") or {})
    if overlay_patch:
        base.update({str(k): str(v) for k, v in overlay_patch.items()})
    if child_patch:
        base.update({str(k): str(v) for k, v in child_patch.items()})
    if inline_params:
        base.update({str(k): str(v) for k, v in inline_params.items()})
    return base


def _raw_template_for_entry(
    conn: sqlite3.Connection,
    entry_id: str,
    children: list[dict[str, Any]],
) -> str:
    overlay = load_overlay(conn, entry_id)
    if overlay and overlay.get("lemma_prompt"):
        return str(overlay["lemma_prompt"])
    props = _load_entry_properties(conn, entry_id)
    if props.get("lemma-prompt"):
        return props["lemma-prompt"]
    return _synthesize_prompt_from_children(children)


def _load_children_for_entry(conn: sqlite3.Connection, entry_id: str) -> list[dict[str, Any]]:
    if is_builtin_urn(entry_id):
        overlay = load_overlay(conn, entry_id)
        if overlay and overlay.get("composition_json"):
            try:
                raw = json.loads(overlay["composition_json"])
                if isinstance(raw, list):
                    merged = merge_vocabulary(conn)
                    out: list[dict[str, Any]] = []
                    for i, item in enumerate(raw):
                        eid = (item.get("entryId") or item.get("childEntryId") or "").strip()
                        term = item.get("term") or (merged.get(eid) or {}).get("term") or ""
                        out.append(
                            {
                                "entryId": eid,
                                "term": term,
                                "sortOrder": int(item.get("sortOrder", i)),
                                "patchProperties": _parse_json_obj(
                                    item.get("patchProperties") or item.get("patch_properties_json")
                                ),
                                "timingOverride": _parse_json_obj(
                                    item.get("timingOverride") or item.get("timing_override_json")
                                ),
                            }
                        )
                    return out
            except json.JSONDecodeError:
                pass
        return []
    comp = load_composition(conn, entry_id)
    children: list[dict[str, Any]] = []
    ensure_lemma_prompt_schema(conn)
    cur = conn.execute(
        """
        SELECT child_entry_id, sort_order, patch_properties_json, timing_override_json
        FROM thesaurus_entry_compositions
        WHERE parent_entry_id = ?
        ORDER BY sort_order, child_entry_id
        """,
        (entry_id,),
    )
    patch_by_child = {
        r["child_entry_id"]: (
            _parse_json_obj(r["patch_properties_json"]),
            _parse_json_obj(r["timing_override_json"]),
        )
        for r in cur.fetchall()
    }
    for c in comp.get("children") or []:
        eid = c.get("entryId")
        patch, timing = patch_by_child.get(eid, ({}, {}))
        children.append(
            {
                **c,
                "patchProperties": patch,
                "timingOverride": timing,
            }
        )
    return children


def load_effective_spatial(conn: sqlite3.Connection, entry_id: str) -> dict[str, Any]:
    ensure_lemma_prompt_schema(conn)
    overlay = load_overlay(conn, entry_id)
    props = _load_entry_properties(conn, entry_id)
    spec = _load_spec_defaults(conn)

    spatial_id = None
    if overlay and overlay.get("spatial_4d_id"):
        spatial_id = overlay["spatial_4d_id"]
    elif props.get("spatial-4d-id"):
        spatial_id = props["spatial-4d-id"]

    timing = {"tMin": float(spec.get("spatial-t-min") or 0), "tMax": float(spec.get("spatial-t-max") or 3600)}
    if overlay and overlay.get("default_timing_json"):
        timing.update(_parse_json_obj(overlay["default_timing_json"]))

    bounds = {
        "centerX": float(props.get("spatial-center-x") or spec.get("spatial-center-x") or 0),
        "centerY": float(props.get("spatial-center-y") or spec.get("spatial-center-y") or 0),
        "centerZ": float(props.get("spatial-center-z") or spec.get("spatial-center-z") or 0),
        "sizeX": float(props.get("spatial-size-x") or spec.get("spatial-size-x") or 1),
        "sizeY": float(props.get("spatial-size-y") or spec.get("spatial-size-y") or 1),
        "sizeZ": float(props.get("spatial-size-z") or spec.get("spatial-size-z") or 1),
    }

    row = None
    if spatial_id:
        cur = conn.execute("SELECT * FROM spatial_4d WHERE id = ?", (spatial_id,))
        row = cur.fetchone()
    if row:
        bounds = {
            "centerX": float(row["center_x"]),
            "centerY": float(row["center_y"]),
            "centerZ": float(row["center_z"]),
            "sizeX": float(row["size_x"]),
            "sizeY": float(row["size_y"]),
            "sizeZ": float(row["size_z"]),
        }
        timing = {"tMin": float(row["t_min"]), "tMax": float(row["t_max"])}

    return {
        "spatial4dId": spatial_id,
        "bounds": bounds,
        "timing": timing,
    }


def expand_lemma_prompt(
    conn: sqlite3.Connection,
    entry_id: str,
    *,
    depth: int = 16,
    visited: set[str] | None = None,
    inline_params: dict[str, str] | None = None,
    child_patch: dict[str, str] | None = None,
    mod_context: dict[str, Any] | None = None,
) -> dict[str, Any]:
    ensure_lemma_prompt_schema(conn)
    visited = set(visited or [])
    issues: list[dict[str, str]] = []

    if entry_id in visited:
        issues.append({"code": "prompt_cycle", "message": f"Cycle detected at {entry_id}"})
        return {
            "expandedText": "",
            "tree": {"entryId": entry_id, "error": "cycle"},
            "mergedProperties": {},
            "spatial": load_effective_spatial(conn, entry_id),
            "issues": issues,
        }
    if depth <= 0:
        issues.append({"code": "max_depth", "message": "Maximum expansion depth reached"})
        return {
            "expandedText": "",
            "tree": {"entryId": entry_id, "error": "max_depth"},
            "mergedProperties": {},
            "spatial": load_effective_spatial(conn, entry_id),
            "issues": issues,
        }

    visited.add(entry_id)
    merged_vocab = merge_vocabulary(conn)
    children = _load_children_for_entry(conn, entry_id)
    overlay = load_overlay(conn, entry_id)
    overlay_patch = _parse_json_obj(overlay.get("patch_properties_json") if overlay else None)

    template = _raw_template_for_entry(conn, entry_id, children)
    merged_props = _merge_properties(
        conn,
        entry_id,
        inline_params=inline_params,
        child_patch=child_patch,
        overlay_patch=overlay_patch,
    )

    def expand_segment(text: str) -> tuple[str, list[dict[str, Any]]]:
        nodes: list[dict[str, Any]] = []
        out_parts: list[str] = []
        last = 0
        for m in P_PROMPT_RE.finditer(text):
            out_parts.append(text[last : m.start()])
            name = (m.group(1) or m.group(3) or "").strip()
            params = _parse_placeholder_params(m.group(2) or m.group(4))
            child_id, cpatch = _resolve_child_for_placeholder(name, children, merged_vocab)
            if not child_id:
                issues.append({"code": "unresolved_placeholder", "message": f"Unknown placeholder {name!r}"})
                out_parts.append(m.group(0))
            else:
                sub = expand_lemma_prompt(
                    conn,
                    child_id,
                    depth=depth - 1,
                    visited=set(visited),
                    inline_params=params,
                    child_patch=cpatch,
                )
                issues.extend(sub.get("issues") or [])
                nodes.append({
                    "placeholder": name,
                    "entryId": child_id,
                    "tree": sub.get("tree"),
                    "mergedProperties": sub.get("mergedProperties"),
                })
                out_parts.append(sub.get("expandedText") or "")
            last = m.end()
        out_parts.append(text[last:])
        return "".join(out_parts), nodes

    expanded, child_nodes = expand_segment(template)
    if mod_context:
        expanded = resolve_mod_placeholders(expanded, mod_context)
    spatial = load_effective_spatial(conn, entry_id)

    for node in child_nodes:
        sub = node.get("tree") if isinstance(node.get("tree"), dict) else {}
        if isinstance(sub, dict) and sub.get("mergedProperties"):
            merged_props.update(sub["mergedProperties"])

    return {
        "expandedText": expanded,
        "tree": {
            "entryId": entry_id,
            "template": template,
            "children": child_nodes,
            "mergedProperties": merged_props,
        },
        "mergedProperties": merged_props,
        "spatial": spatial,
        "issues": issues,
    }


def load_prompt_bundle(conn: sqlite3.Connection, entry_id: str) -> dict[str, Any]:
    ensure_lemma_prompt_schema(conn)
    merged = merge_vocabulary(conn)
    view = merged.get(entry_id)
    if not view and not is_builtin_urn(entry_id):
        raise ValueError(f"Entry not found: {entry_id}")

    is_builtin = is_builtin_urn(entry_id) or bool((view or {}).get("isBuiltIn"))
    overlay = load_overlay(conn, entry_id)
    children = _load_children_for_entry(conn, entry_id)
    template = _raw_template_for_entry(conn, entry_id, children)
    spatial = load_effective_spatial(conn, entry_id)
    overlay_patch = _parse_json_obj(overlay.get("patch_properties_json") if overlay else None)

    props = _load_entry_properties(conn, entry_id)
    patch_props = dict(overlay_patch)
    for k, v in props.items():
        if k.startswith("spatial-") or k == "lemma-prompt":
            continue

    return {
        "entryId": entry_id,
        "term": (view or {}).get("term"),
        "isBuiltIn": is_builtin,
        "usesOverlay": bool(overlay),
        "lemmaPrompt": template,
        "compositionChildren": children,
        "isComposedLemma": len(children) > 0,
        "patchProperties": patch_props if is_builtin else {},
        "spatial": spatial,
        "timing": spatial.get("timing") or {"tMin": 0, "tMax": 3600},
    }


def _validate_overlay_children(conn: sqlite3.Connection, parent_id: str, children: list[dict[str, Any]]) -> str | None:
    for i, item in enumerate(children):
        child_id = (item.get("entryId") or item.get("childEntryId") or "").strip()
        if not child_id:
            return f"Child at index {i} is missing entryId"
        if not _entry_exists_in_vocab(conn, child_id):
            return f"Unknown child entry id: {child_id}"
        if would_create_cycle(conn, parent_id, child_id) if not is_builtin_urn(parent_id) else False:
            return f"Composition cycle detected: {child_id}"
    return None


def upsert_overlay_composition(conn: sqlite3.Connection, entry_id: str, children: list[dict[str, Any]]) -> None:
    err = _validate_overlay_children(conn, entry_id, children)
    if err:
        raise ValueError(err)
    normalized = []
    merged = merge_vocabulary(conn)
    for i, item in enumerate(children):
        eid = (item.get("entryId") or item.get("childEntryId") or "").strip()
        normalized.append(
            {
                "entryId": eid,
                "term": item.get("term") or (merged.get(eid) or {}).get("term") or "",
                "sortOrder": int(item.get("sortOrder", i)),
                "patchProperties": item.get("patchProperties") or item.get("patch_properties_json") or {},
                "timingOverride": item.get("timingOverride") or item.get("timing_override_json") or {},
            }
        )
    overlay = load_overlay(conn, entry_id) or {}
    conn.execute(
        """
        INSERT INTO thesaurus_lemma_overlays
            (target_entry_id, lemma_prompt, spatial_4d_id, default_timing_json, patch_properties_json, composition_json, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?)
        ON CONFLICT(target_entry_id) DO UPDATE SET
            composition_json = excluded.composition_json,
            updated_at = excluded.updated_at
        """,
        (
            entry_id,
            overlay.get("lemma_prompt"),
            overlay.get("spatial_4d_id"),
            overlay.get("default_timing_json"),
            overlay.get("patch_properties_json"),
            json.dumps(normalized),
            _now(),
        ),
    )


def upsert_lemma_prompt_bundle(conn: sqlite3.Connection, entry_id: str, body: dict[str, Any]) -> dict[str, Any]:
    ensure_lemma_prompt_schema(conn)
    merged = merge_vocabulary(conn)
    if entry_id not in merged and not is_builtin_urn(entry_id):
        raise ValueError(f"Entry not found: {entry_id}")

    lemma_prompt = body.get("lemmaPrompt")
    if lemma_prompt is None:
        lemma_prompt = body.get("lemma_prompt")
    children = body.get("compositionChildren") or body.get("children") or body.get("composition") or []
    patch_props = body.get("patchProperties") or body.get("patch_properties") or {}
    timing = body.get("timing") or body.get("defaultTiming") or {}
    spatial_body = body.get("spatial") or {}
    bounds = spatial_body.get("bounds") or body.get("bounds") or {}
    episode_id = body.get("episodeId") or body.get("episode_id")

    is_builtin = is_builtin_urn(entry_id) or bool((merged.get(entry_id) or {}).get("isBuiltIn"))

    if is_builtin:
        overlay = load_overlay(conn, entry_id) or {}
        comp_json = overlay.get("composition_json")
        if children is not None:
            err = _validate_overlay_children(conn, entry_id, children)
            if err:
                raise ValueError(err)
            merged_vocab = merge_vocabulary(conn)
            normalized = []
            for i, item in enumerate(children):
                eid = (item.get("entryId") or item.get("childEntryId") or "").strip()
                normalized.append(
                    {
                        "entryId": eid,
                        "term": item.get("term") or (merged_vocab.get(eid) or {}).get("term") or "",
                        "sortOrder": int(item.get("sortOrder", i)),
                        "patchProperties": item.get("patchProperties")
                        or item.get("patch_properties_json")
                        or {},
                        "timingOverride": item.get("timingOverride")
                        or item.get("timing_override_json")
                        or {},
                    }
                )
            comp_json = json.dumps(normalized)

        spatial_id = overlay.get("spatial_4d_id")
        if bounds or timing or spatial_body.get("spatial4dId"):
            spatial_id = upsert_entry_spatial(
                conn,
                entry_id,
                bounds=bounds,
                timing=timing,
                spatial_id=spatial_body.get("spatial4dId") or spatial_id,
                episode_id=episode_id,
            )

        conn.execute(
            """
            INSERT INTO thesaurus_lemma_overlays
                (target_entry_id, lemma_prompt, spatial_4d_id, default_timing_json, patch_properties_json, composition_json, updated_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(target_entry_id) DO UPDATE SET
                lemma_prompt = excluded.lemma_prompt,
                spatial_4d_id = excluded.spatial_4d_id,
                default_timing_json = excluded.default_timing_json,
                patch_properties_json = excluded.patch_properties_json,
                composition_json = COALESCE(excluded.composition_json, thesaurus_lemma_overlays.composition_json),
                updated_at = excluded.updated_at
            """,
            (
                entry_id,
                str(lemma_prompt) if lemma_prompt is not None else overlay.get("lemma_prompt"),
                spatial_id,
                json.dumps(timing) if timing else overlay.get("default_timing_json"),
                json.dumps(patch_props) if patch_props else overlay.get("patch_properties_json"),
                comp_json,
                _now(),
            ),
        )
    else:
        cur = conn.execute("SELECT id FROM thesaurus_entries WHERE id = ?", (entry_id,))
        if not cur.fetchone():
            raise ValueError(f"Parent entry not found: {entry_id}")

        if lemma_prompt is not None:
            _set_entry_property(conn, entry_id, "lemma-prompt", str(lemma_prompt))

        if timing:
            _set_entry_property(conn, entry_id, "spatial-t-min", str(timing.get("tMin", 0)))
            _set_entry_property(conn, entry_id, "spatial-t-max", str(timing.get("tMax", 3600)))

        if bounds:
            for key, prop in (
                ("centerX", "spatial-center-x"),
                ("centerY", "spatial-center-y"),
                ("centerZ", "spatial-center-z"),
                ("sizeX", "spatial-size-x"),
                ("sizeY", "spatial-size-y"),
                ("sizeZ", "spatial-size-z"),
            ):
                if key in bounds:
                    _set_entry_property(conn, entry_id, prop, str(bounds[key]))

        spatial_id = upsert_entry_spatial(
            conn,
            entry_id,
            bounds=bounds,
            timing=timing,
            spatial_id=spatial_body.get("spatial4dId") or _load_entry_properties(conn, entry_id).get("spatial-4d-id"),
            episode_id=episode_id,
        )
        if spatial_id:
            _set_entry_property(conn, entry_id, "spatial-4d-id", spatial_id)

        if children is not None:
            replace_composition_with_patches(conn, entry_id, children)

    return load_prompt_bundle(conn, entry_id)


def replace_composition_with_patches(
    conn: sqlite3.Connection,
    parent_entry_id: str,
    children: list[dict[str, Any]],
) -> dict[str, Any]:
    """Replace composition rows including per-child patch and timing JSON."""
    ensure_lemma_prompt_schema(conn)
    slim = []
    for i, item in enumerate(children):
        slim.append(
            {
                "entryId": item.get("entryId") or item.get("childEntryId"),
                "sortOrder": int(item.get("sortOrder", i)),
                "spatial4dId": item.get("spatial4dId") or item.get("spatial_4d_id"),
                "anchorText": item.get("anchorText") or item.get("anchor_text"),
                "anchorFarey": item.get("anchorFarey"),
                "draftEpisodeId": item.get("draftEpisodeId") or item.get("draft_episode_id"),
            }
        )
    result = replace_composition(conn, parent_entry_id, slim)
    for i, item in enumerate(children):
        child_id = (item.get("entryId") or item.get("childEntryId") or "").strip()
        patch = item.get("patchProperties") or item.get("patch_properties_json")
        timing = item.get("timingOverride") or item.get("timing_override_json")
        patch_json = json.dumps(patch) if isinstance(patch, dict) else patch
        timing_json = json.dumps(timing) if isinstance(timing, dict) else timing
        conn.execute(
            """
            UPDATE thesaurus_entry_compositions
            SET patch_properties_json = ?, timing_override_json = ?
            WHERE parent_entry_id = ? AND child_entry_id = ?
            """,
            (patch_json, timing_json, parent_entry_id, child_id),
        )
    return load_composition(conn, parent_entry_id)
