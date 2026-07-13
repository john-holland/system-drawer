"""Bulk auto-add single lemma bindings for script output drafts."""

from __future__ import annotations

import re
import sqlite3
import uuid
from datetime import datetime, timezone
from typing import Any, Optional

try:
    from continuuuum_api.lemma_import import upsert_lemma_row
    from continuuuum_api.lemma_merge import filter_entries, merge_vocabulary
    from continuuuum_api.localization_helpers import (
        ensure_clause_binding_columns,
        resolve_clause_ref_farey,
        resolve_draft_script_id,
    )
    from continuuuum_api.mod_db import upsert_moddable_target
    from continuuuum_api.table_read_blocks import _is_character_line
except ImportError:
    from lemma_import import upsert_lemma_row
    from lemma_merge import filter_entries, merge_vocabulary
    from localization_helpers import (
        ensure_clause_binding_columns,
        resolve_clause_ref_farey,
        resolve_draft_script_id,
    )
    from mod_db import upsert_moddable_target
    from table_read_blocks import _is_character_line

AUTO_ADD_TYPES = (
    "builtin",
    "prefab",
    "localization",
    "mod_slot",
    "prompt_placeholder",
    "new_lemma",
)

DEFAULT_PRIORITY = list(AUTO_ADD_TYPES)
WORD_RE = re.compile(r"\S+")
PREFAB_KEY_MARKERS = ("prefab", "usc", "asset")


def _now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def normalize_priority(priority: list[str] | None) -> list[str]:
    out: list[str] = []
    seen: set[str] = set()
    for t in priority or []:
        if t in AUTO_ADD_TYPES and t not in seen:
            out.append(t)
            seen.add(t)
    for t in AUTO_ADD_TYPES:
        if t not in seen:
            out.append(t)
            seen.add(t)
    return out[: len(AUTO_ADD_TYPES)]


def swap_priority_slots(priority: list[str], slot_index: int, new_type: str) -> list[str]:
    """Mirror client swap rule: picking a type already used exchanges slot values."""
    lst = normalize_priority(priority)
    if slot_index < 0 or slot_index >= len(lst):
        return lst
    other = lst.index(new_type) if new_type in lst else -1
    if other >= 0 and other != slot_index:
        prev = lst[slot_index]
        lst[slot_index] = new_type
        lst[other] = prev
    else:
        lst[slot_index] = new_type
    return normalize_priority(lst)


def pick_auto_select_lemma(items: list[dict[str, Any]], query: str) -> Optional[dict[str, Any]]:
    q_lower = (query or "").strip().lower()
    if not q_lower or not items:
        return None
    exact = [e for e in items if (e.get("term") or "").lower() == q_lower]
    if len(exact) == 1:
        return exact[0]
    built_in = [e for e in exact if e.get("isBuiltIn")]
    if len(built_in) == 1:
        return built_in[0]
    return None


def binding_template_key(binding: dict[str, Any]) -> str:
    kind = binding.get("bindingKind") or binding.get("binding_kind") or "property"
    if kind == "lemma":
        eid = binding.get("entryId") or binding.get("entry_id") or binding.get("propertyValue") or binding.get("property_value") or ""
        return f"lemma:{eid}"
    if kind == "localization":
        pk = binding.get("propertyKey") or binding.get("property_key") or ""
        pv = binding.get("propertyValue") or binding.get("property_value") or ""
        return f"localization:{pk}:{pv}"
    if kind == "prompt_placeholder":
        name = binding.get("promptPlaceholderName") or binding.get("prompt_placeholder_name") or ""
        return f"prompt:{name}"
    pk = binding.get("propertyKey") or binding.get("property_key") or ""
    pv = binding.get("propertyValue") or binding.get("property_value") or ""
    return f"property:{pk}:{pv}"


def _entry_for_binding(binding: dict[str, Any], vocabulary: dict[str, dict[str, Any]]) -> Optional[dict[str, Any]]:
    eid = binding.get("entryId") or binding.get("entry_id") or binding.get("propertyValue") or binding.get("property_value")
    if not eid:
        return None
    return vocabulary.get(str(eid))


def _is_prefab_property_key(key: str) -> bool:
    low = (key or "").lower()
    return any(m in low for m in PREFAB_KEY_MARKERS)


def classify_action_type(action: dict[str, Any], vocabulary: dict[str, dict[str, Any]]) -> str:
    kind = action.get("actionKind") or action.get("bindingKind") or action.get("binding_kind") or ""
    if kind == "mod_slot":
        return "mod_slot"
    if kind == "new_lemma":
        return "new_lemma"
    if kind == "prompt_placeholder" or kind == "prompt":
        return "prompt_placeholder"
    if kind == "localization":
        return "localization"
    if kind == "lemma":
        entry = action.get("_entry") or _entry_for_binding(action, vocabulary)
        if entry:
            if entry.get("isComposedLemma"):
                return ""
            if entry.get("isBuiltIn"):
                return "builtin"
            if entry.get("prefabId") or (entry.get("properties") or {}).get("prefabId"):
                return "prefab"
            return "prefab"
        return "prefab"
    pk = action.get("propertyKey") or action.get("property_key") or ""
    if _is_prefab_property_key(pk):
        return "prefab"
    if kind == "property" and pk:
        return "prefab"
    return ""


def enumerate_candidate_spans(script_text: str) -> list[dict[str, Any]]:
    """Non-empty whitespace tokens and screenplay character cues."""
    text = script_text or ""
    spans: list[dict[str, Any]] = []
    seen: set[tuple[int, int]] = set()

    for line_match in re.finditer(r"^[^\n]*$", text, re.MULTILINE):
        raw_line = line_match.group()
        stripped = raw_line.strip()
        if not _is_character_line(stripped):
            continue
        name = stripped.split("(")[0].strip()
        if not name:
            continue
        rel = raw_line.find(name)
        if rel < 0:
            continue
        start = line_match.start() + rel
        end = start + len(name)
        key = (start, end)
        if key not in seen:
            seen.add(key)
            spans.append({"charStart": start, "charEnd": end, "selectionText": name})

    for match in WORD_RE.finditer(text):
        token = match.group()
        if not token.strip():
            continue
        key = (match.start(), match.end())
        if key not in seen:
            seen.add(key)
            spans.append({"charStart": match.start(), "charEnd": match.end(), "selectionText": token})

    spans.sort(key=lambda s: (s["charStart"], s["charEnd"]))
    return spans


def _span_covered_by_binding(span: dict[str, Any], bindings: list[dict[str, Any]]) -> bool:
    cs, ce = span["charStart"], span["charEnd"]
    for b in bindings:
        bcs = int(b.get("charStart") if b.get("charStart") is not None else b.get("char_start") or 0)
        bce = int(b.get("charEnd") if b.get("charEnd") is not None else b.get("char_end") or 0)
        if bcs <= cs and bce >= ce and (bcs < cs or bce > ce or (bcs == cs and bce == ce)):
            return True
    return False


def _load_templates_for_selection(conn: sqlite3.Connection, selection_text: str) -> list[dict[str, Any]]:
    ensure_clause_binding_columns(conn)
    cur = conn.execute(
        """SELECT b.*, s.script_text AS draft_script_text FROM localization_clause_bindings b
           LEFT JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE b.selection_text = ?
           ORDER BY b.updated_at DESC""",
        (selection_text,),
    )
    items: list[dict[str, Any]] = []
    for row in cur.fetchall():
        items.append(
            {
                "bindingKind": row["binding_kind"] or "property",
                "propertyKey": row["property_key"] or "",
                "propertyValue": row["property_value"] or "",
                "entryId": row["entry_id"],
                "promptPlaceholderName": row["prompt_placeholder_name"],
                "draftScriptId": row["draft_script_id"],
                "charStart": row["char_start"],
                "charEnd": row["char_end"],
            }
        )
    return items


def _lemma_entry_template(entry: dict[str, Any]) -> dict[str, Any]:
    eid = entry["id"]
    return {
        "bindingKind": "lemma",
        "propertyKey": "entry-id",
        "propertyValue": eid,
        "entryId": eid,
        "_entry": entry,
    }


def _mod_slot_candidate(span: dict[str, Any], draft_id: str) -> dict[str, Any]:
    label = (span.get("selectionText") or "").strip()[:48]
    slot_key = re.sub(r"[^a-z0-9]+", "-", label.lower()).strip("-") or "mod-slot"
    return {
        "actionKind": "mod_slot",
        "targetKind": "episode_section",
        "draftEpisodeId": draft_id,
        "charStart": span["charStart"],
        "charEnd": span["charEnd"],
        "slotKey": f"{slot_key}-auto",
        "label": label,
    }


def build_span_candidates(
    conn: sqlite3.Connection,
    span: dict[str, Any],
    *,
    draft_id: str,
    draft_script_id: str,
    vocabulary: dict[str, dict[str, Any]],
    existing_bindings: list[dict[str, Any]],
    script_text: str,
) -> list[dict[str, Any]]:
    """Mirror fetchClauseSuggestions non-bundle candidates for a span."""
    del draft_id  # reserved for mod-slot synthesis during priority walk
    selection_text = (span.get("selectionText") or script_text[span["charStart"] : span["charEnd"]]).strip()
    if not selection_text:
        return []

    templates = _load_templates_for_selection(conn, selection_text)
    entries = filter_entries(list(vocabulary.values()), q=selection_text)[:20]
    applied: set[str] = set()
    for b in existing_bindings:
        bcs = int(b.get("charStart") if b.get("charStart") is not None else b.get("char_start") or -1)
        bce = int(b.get("charEnd") if b.get("charEnd") is not None else b.get("char_end") or -1)
        if bcs == span["charStart"] and bce == span["charEnd"]:
            applied.add(binding_template_key(b))

    candidates: list[dict[str, Any]] = []
    seen: set[str] = set()

    for tpl in templates:
        same_span = (
            (tpl.get("draftScriptId") or "") == draft_script_id
            and int(tpl.get("charStart") or -1) == span["charStart"]
            and int(tpl.get("charEnd") or -1) == span["charEnd"]
        )
        if same_span:
            continue
        key = binding_template_key(tpl)
        if key in applied or key in seen:
            continue
        seen.add(key)
        entry = _entry_for_binding(tpl, vocabulary)
        if entry and entry.get("isComposedLemma"):
            continue
        if (tpl.get("bindingKind") or "") == "lemma":
            if entry:
                picked = pick_auto_select_lemma([entry], selection_text)
                if not picked:
                    continue
            else:
                continue
        candidates.append(tpl)

    auto_entry = pick_auto_select_lemma(entries, selection_text)
    if auto_entry and not auto_entry.get("isComposedLemma"):
        tpl = _lemma_entry_template(auto_entry)
        key = binding_template_key(tpl)
        if key not in applied and key not in seen:
            seen.add(key)
            candidates.append(tpl)

    return candidates


def mod_slot_eligible(span: dict[str, Any]) -> bool:
    return bool((span.get("selectionText") or "").strip())


def resolve_winning_action(
    candidates: list[dict[str, Any]],
    *,
    priority: list[str],
    new_lemma_required: bool,
    vocabulary: dict[str, dict[str, Any]],
    selection_text: str,
    span: dict[str, Any] | None = None,
    draft_id: str = "",
    scope_single_chip: bool = True,
) -> tuple[Optional[dict[str, Any]], str]:
    """Return (action, reason)."""
    non_bundle = [c for c in candidates if not c.get("_bundle")]

    if scope_single_chip and len(non_bundle) > 1:
        return None, "ambiguous"

    if len(non_bundle) == 0:
        if new_lemma_required:
            return (
                {
                    "actionKind": "new_lemma",
                    "bindingKind": "lemma",
                    "selectionText": selection_text,
                },
                "new_lemma",
            )
        return None, "no_match"

    if scope_single_chip and len(non_bundle) == 1:
        only = non_bundle[0]
        action_type = classify_action_type(only, vocabulary) or "unknown"
        return only, action_type

    by_type: dict[str, list[dict[str, Any]]] = {t: [] for t in AUTO_ADD_TYPES}
    for c in non_bundle:
        t = classify_action_type(c, vocabulary)
        if t:
            by_type.setdefault(t, []).append(c)

    order = normalize_priority(priority)
    for slot in order:
        if slot == "mod_slot":
            if mod_slot_eligible(span or {"selectionText": selection_text}) and span and draft_id:
                return _mod_slot_candidate(span, draft_id), "mod_slot"
            continue
        if slot == "new_lemma":
            continue
        bucket = by_type.get(slot) or []
        if len(bucket) == 1:
            return bucket[0], slot

    if new_lemma_required:
        return (
            {
                "actionKind": "new_lemma",
                "bindingKind": "lemma",
                "selectionText": selection_text,
            },
            "new_lemma",
        )

    return None, "ambiguous"


def _valid_property_keys(conn: sqlite3.Connection) -> set[str]:
    try:
        cur = conn.execute("SELECT key FROM localization_property_specs")
        return {r["key"] for r in cur.fetchall()}
    except sqlite3.OperationalError:
        return set()


def _insert_clause_binding(
    conn: sqlite3.Connection,
    body: dict[str, Any],
    *,
    script_text: str,
    draft_script_id: str,
) -> str:
    ensure_clause_binding_columns(conn)
    binding_kind = body.get("bindingKind") or body.get("binding_kind") or "property"
    ref = resolve_clause_ref_farey(conn, {**body, "draftScriptId": draft_script_id}, script_text)
    bid = str(uuid.uuid4())
    now = _now()
    conn.execute(
        """INSERT INTO localization_clause_bindings (
            id, episode_script_id, draft_script_id,
            farey_left_num, farey_left_den, farey_right_num, farey_right_den,
            char_start, char_end, selection_text, property_key, property_value,
            binding_kind, ast_node_id, prompt_placeholder_name, entry_id, created_at, updated_at
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
        (
            bid,
            body.get("episodeScriptId") or body.get("episode_script_id"),
            draft_script_id,
            ref.farey_left_num,
            ref.farey_left_den,
            ref.farey_right_num,
            ref.farey_right_den,
            ref.char_start,
            ref.char_end,
            ref.selection_text or body.get("selectionText") or "",
            body.get("propertyKey") or body.get("property_key") or "",
            body.get("propertyValue") or body.get("property_value") or "",
            binding_kind,
            body.get("astNodeId") or body.get("ast_node_id"),
            body.get("promptPlaceholderName") or body.get("prompt_placeholder_name"),
            body.get("entryId") or body.get("entry_id"),
            now,
            now,
        ),
    )
    return bid


def _apply_action(
    conn: sqlite3.Connection,
    action: dict[str, Any],
    span: dict[str, Any],
    *,
    draft_id: str,
    draft_script_id: str,
    script_text: str,
    vocabulary: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    kind = action.get("actionKind") or action.get("bindingKind") or ""
    selection_text = span.get("selectionText") or script_text[span["charStart"] : span["charEnd"]]

    if kind == "mod_slot":
        body = {
            **action,
            "sourceText": script_text,
        }
        item = upsert_moddable_target(conn, body)
        return {"kind": "mod_slot", "id": item.get("id"), "slotKey": item.get("slotKey")}

    entry_id = action.get("entryId") or action.get("entry_id")
    if kind == "new_lemma":
        row = {
            "word": selection_text,
            "description": "",
            "language": "en",
            "partOfSpeech": "unknown",
        }
        status, err, new_id = upsert_lemma_row(conn, row, _valid_property_keys(conn))
        if err and not new_id:
            raise ValueError(err)
        entry_id = new_id
        vocabulary.update(merge_vocabulary(conn))

    body = {
        "bindingKind": action.get("bindingKind") or "lemma",
        "propertyKey": action.get("propertyKey") or action.get("property_key") or "entry-id",
        "propertyValue": action.get("propertyValue") or action.get("property_value") or entry_id,
        "entryId": entry_id,
        "charStart": span["charStart"],
        "charEnd": span["charEnd"],
        "selectionText": selection_text,
        "draftEpisodeId": draft_id,
        "draftScriptId": draft_script_id,
        "promptPlaceholderName": action.get("promptPlaceholderName") or action.get("prompt_placeholder_name"),
    }
    bid = _insert_clause_binding(conn, body, script_text=script_text, draft_script_id=draft_script_id)
    return {"kind": body["bindingKind"], "id": bid, "entryId": entry_id}


def auto_add_single_lemmas(
    conn: sqlite3.Connection,
    draft_id: str,
    *,
    script_text: str,
    settings: dict[str, Any] | None = None,
    scope_single_chip: bool = True,
) -> dict[str, Any]:
    settings = settings or {}
    priority = normalize_priority(settings.get("autoAddPriority"))
    new_lemma_required = bool(settings.get("newLemmaRequired"))

    row = conn.execute(
        """SELECT s.id AS draft_script_id, s.script_text
           FROM draft_episode_script s
           WHERE s.draft_episode_id = ?
           ORDER BY s.updated_at DESC LIMIT 1""",
        (draft_id,),
    ).fetchone()
    if not row:
        return {"added": 0, "skipped": 0, "byType": {}, "items": [], "error": "draft script not found"}

    draft_script_id = row["draft_script_id"]
    if not script_text:
        script_text = row["script_text"] or ""

    ensure_clause_binding_columns(conn)
    vocabulary = merge_vocabulary(conn)
    cur = conn.execute(
        """SELECT b.* FROM localization_clause_bindings b
           JOIN draft_episode_script s ON s.id = b.draft_script_id
           WHERE s.draft_episode_id = ?""",
        (draft_id,),
    )
    existing_bindings = [dict(r) for r in cur.fetchall()]

    added = 0
    skipped = 0
    by_type: dict[str, int] = {}
    items: list[dict[str, Any]] = []

    for span in enumerate_candidate_spans(script_text):
        if _span_covered_by_binding(span, existing_bindings):
            skipped += 1
            items.append({**span, "status": "skipped", "reason": "already_bound"})
            continue

        candidates = build_span_candidates(
            conn,
            span,
            draft_id=draft_id,
            draft_script_id=draft_script_id,
            vocabulary=vocabulary,
            existing_bindings=existing_bindings,
            script_text=script_text,
        )
        selection_text = span.get("selectionText") or script_text[span["charStart"] : span["charEnd"]]
        action, reason = resolve_winning_action(
            candidates,
            priority=priority,
            new_lemma_required=new_lemma_required,
            vocabulary=vocabulary,
            selection_text=selection_text,
            span=span,
            draft_id=draft_id,
            scope_single_chip=scope_single_chip,
        )
        if not action:
            skipped += 1
            items.append({**span, "status": "skipped", "reason": reason})
            continue

        try:
            applied = _apply_action(
                conn,
                action,
                span,
                draft_id=draft_id,
                draft_script_id=draft_script_id,
                script_text=script_text,
                vocabulary=vocabulary,
            )
            added += 1
            by_type[reason] = by_type.get(reason, 0) + 1
            items.append({**span, "status": "added", "type": reason, "applied": applied})
            existing_bindings.append(
                {
                    "charStart": span["charStart"],
                    "charEnd": span["charEnd"],
                    "bindingKind": applied.get("kind"),
                    "entryId": applied.get("entryId"),
                }
            )
        except (ValueError, sqlite3.OperationalError) as exc:
            skipped += 1
            items.append({**span, "status": "skipped", "reason": str(exc)})

    conn.commit()
    reproduction_coeff = None
    try:
        try:
            from continuuuum_api.dream_improbability import bake_dream_reproduction_coeff
        except ImportError:
            from dream_improbability import bake_dream_reproduction_coeff

        reproduction_coeff = bake_dream_reproduction_coeff(conn, draft_id)
    except (sqlite3.OperationalError, KeyError, TypeError, ValueError):
        reproduction_coeff = None

    return {
        "added": added,
        "skipped": skipped,
        "byType": by_type,
        "items": items,
        "dreamReproductionCoeff": reproduction_coeff,
    }
