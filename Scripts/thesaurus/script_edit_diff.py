"""Script edit diff engine for localization clause bindings and {P:...} spans."""

from __future__ import annotations

import re
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Iterable, List, Optional, Sequence, Tuple

P_PROMPT_RE = re.compile(r"\{\{?P:[^}]+\}?\}?|\{P:[^}]+\}")


@dataclass
class EditRegion:
    offset: int
    old_len: int
    new_len: int

    @property
    def delta(self) -> int:
        return self.new_len - self.old_len


@dataclass
class SpanRef:
    char_start: int
    char_end: int
    binding_id: Optional[str] = None
    label: str = ""
    kind: str = "binding"  # binding | prompt


@dataclass
class DiffItem:
    severity: str  # required | warning
    item_type: str
    description: str
    binding_id: Optional[str] = None
    old_char_start: Optional[int] = None
    old_char_end: Optional[int] = None
    new_char_start: Optional[int] = None
    new_char_end: Optional[int] = None
    auto_applied: bool = False


def compute_edit_regions(old_text: str, new_text: str) -> List[EditRegion]:
    """Return minimal edit regions via prefix/suffix trim (simple v1 diff)."""
    if old_text == new_text:
        return []
    prefix = 0
    max_prefix = min(len(old_text), len(new_text))
    while prefix < max_prefix and old_text[prefix] == new_text[prefix]:
        prefix += 1
    suffix = 0
    max_suffix = min(len(old_text) - prefix, len(new_text) - prefix)
    while suffix < max_suffix and old_text[len(old_text) - 1 - suffix] == new_text[len(new_text) - 1 - suffix]:
        suffix += 1
    old_len = len(old_text) - prefix - suffix
    new_len = len(new_text) - prefix - suffix
    if old_len == 0 and new_len == 0:
        return []
    return [EditRegion(prefix, old_len, new_len)]


def parse_prompt_spans(text: str) -> List[SpanRef]:
    spans: List[SpanRef] = []
    for m in P_PROMPT_RE.finditer(text or ""):
        spans.append(SpanRef(m.start(), m.end(), label=m.group(0), kind="prompt"))
    return spans


def bindings_to_spans(bindings: Sequence[dict], script_text: str = "") -> List[SpanRef]:
    from thesaurus.clause_audit import resolve_binding_char_span

    out: List[SpanRef] = []
    for b in bindings or []:
        cs, ce = resolve_binding_char_span(b, script_text or "")
        out.append(
            SpanRef(
                cs,
                ce,
                binding_id=b.get("id"),
                label=b.get("selection_text") or b.get("selectionText") or "",
                kind="binding",
            )
        )
    return out


def _map_point_through_edit(p: int, edit: EditRegion, bias: str = "start") -> int:
    if edit.old_len == 0:
        if bias == "end":
            if p <= edit.offset:
                return p
            return p + edit.new_len
        if p < edit.offset:
            return p
        return p + edit.new_len
    edit_end = edit.offset + edit.old_len
    if p <= edit.offset:
        return p
    if p >= edit_end:
        return p + edit.delta
    return edit.offset


def _edit_allows_reanchor(edit: EditRegion, old_text: str, span_start: int, span_end: int) -> bool:
    if edit.old_len == 0:
        return True
    if edit.offset >= span_end or edit.offset + edit.old_len <= span_start:
        return False
    deleted = (old_text or "")[edit.offset : edit.offset + edit.old_len]
    return deleted.strip() == ""


def _first_letter_in_selection(selection_text: str) -> Optional[Tuple[str, int]]:
    for i, ch in enumerate(selection_text or ""):
        if not ch.isspace():
            return ch, i
    if selection_text:
        return selection_text[0], 0
    return None


def _reanchor_span_by_first_letter(
    current_text: str,
    selection_text: str,
    shifted_start: int,
    shifted_end: int,
) -> Optional[Tuple[int, int]]:
    anchor = _first_letter_in_selection(selection_text)
    if not anchor or shifted_end <= shifted_start or not selection_text:
        return None
    letter, offset_in_sel = anchor
    span_len = shifted_end - shifted_start
    expect_letter_pos = shifted_start + offset_in_sel
    slack = max(40, len(selection_text) + 20)
    search_from = max(0, expect_letter_pos - slack)
    search_to = min(len(current_text), expect_letter_pos + slack)
    best_start = shifted_start
    best_score = -1
    for pos in range(search_from, search_to):
        if pos >= len(current_text) or current_text[pos] != letter:
            continue
        start = pos - offset_in_sel
        if start < 0:
            continue
        slice_text = current_text[start : start + len(selection_text)]
        score = sum(1 for a, b in zip(slice_text, selection_text) if a == b)
        dist = abs(start - shifted_start)
        combined = score * 1000 - dist
        if combined > best_score:
            best_score = combined
            best_start = start
    if best_score >= 0:
        return best_start, best_start + span_len
    return None


def _shift_span(span: SpanRef, edit: EditRegion) -> Tuple[int, int, bool, bool]:
    """Return (new_start, new_end, overlapped, shifted_only)."""
    new_start = _map_point_through_edit(span.char_start, edit, "start")
    new_end = _map_point_through_edit(span.char_end, edit, "end")
    edit_end = edit.offset + edit.old_len
    overlapped = edit_end > span.char_start and edit.offset < span.char_end
    shifted = new_start != span.char_start or new_end != span.char_end
    return new_start, new_end, overlapped, shifted


def audit_edit(
    old_text: str,
    new_text: str,
    bindings: Sequence[dict],
) -> Tuple[List[DiffItem], List[DiffItem], List[dict]]:
    """Return (required, warnings, updated_bindings)."""
    required: List[DiffItem] = []
    warnings: List[DiffItem] = []
    regions = compute_edit_regions(old_text or "", new_text or "")
    all_spans = bindings_to_spans(bindings, old_text or "") + parse_prompt_spans(old_text or "")

    updated_bindings = [dict(b) for b in (bindings or [])]
    binding_by_id = {b.get("id"): b for b in updated_bindings if b.get("id")}

    for span in all_spans:
        label = span.label or span.kind
        new_start, new_end = span.char_start, span.char_end
        overlapped = False
        shifted = False
        cur_start, cur_end = span.char_start, span.char_end
        for edit in regions:
            ns, ne, ov, sh = _shift_span(SpanRef(cur_start, cur_end), edit)
            if ov:
                overlapped = True
            if sh:
                shifted = True
            cur_start, cur_end = ns, ne
        new_start, new_end = cur_start, cur_end

        if overlapped and span.kind == "binding" and any(
            _edit_allows_reanchor(edit, old_text or "", span.char_start, span.char_end) for edit in regions
        ):
            reanchored = _reanchor_span_by_first_letter(new_text or "", label, new_start, new_end)
            if reanchored:
                new_start, new_end = reanchored
                overlapped = False
                shifted = True

        if overlapped:
            required.append(
                DiffItem(
                    severity="required",
                    item_type="property_stale" if span.kind == "binding" else "prompt_span_shift",
                    description=f"Edit overlaps {span.kind} '{label}' — review property/text",
                    binding_id=span.binding_id,
                    old_char_start=span.char_start,
                    old_char_end=span.char_end,
                    new_char_start=new_start,
                    new_char_end=new_end,
                )
            )
        elif shifted and regions:
            warnings.append(
                DiffItem(
                    severity="warning",
                    item_type="auto_fixed_offset",
                    description=f"Span '{label}' shifted by edit — please review",
                    binding_id=span.binding_id,
                    old_char_start=span.char_start,
                    old_char_end=span.char_end,
                    new_char_start=new_start,
                    new_char_end=new_end,
                    auto_applied=True,
                )
            )
            if span.binding_id and span.binding_id in binding_by_id:
                b = binding_by_id[span.binding_id]
                b["char_start"] = new_start
                b["char_end"] = new_end
                b["updated_at"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    return required, warnings, updated_bindings


def _binding_field(binding: dict, *keys: str, default: str = "") -> str:
    for k in keys:
        if k in binding and binding[k] is not None:
            return str(binding[k])
    return default


def _binding_int(binding: dict, *keys: str, default: int = 0) -> int:
    for k in keys:
        if k in binding and binding[k] is not None:
            return int(binding[k])
    return default


def audit_binding_edit(
    old: dict,
    new: dict,
    script_text: str = "",
) -> Tuple[List[DiffItem], List[DiffItem]]:
    """Return (required, warnings) for direct binding metadata/span edits."""
    required: List[DiffItem] = []
    warnings: List[DiffItem] = []
    bid = old.get("id")
    label = _binding_field(old, "selection_text", "selectionText") or "clause"

    old_cs = _binding_int(old, "char_start", "charStart")
    old_ce = _binding_int(old, "char_end", "charEnd")
    new_cs = _binding_int(new, "char_start", "charStart", default=old_cs)
    new_ce = _binding_int(new, "char_end", "charEnd", default=old_ce)

    old_pk = _binding_field(old, "property_key", "propertyKey")
    new_pk = _binding_field(new, "property_key", "propertyKey", default=old_pk)
    old_pv = _binding_field(old, "property_value", "propertyValue")
    new_pv = _binding_field(new, "property_value", "propertyValue", default=old_pv)
    old_eid = _binding_field(old, "entry_id", "entryId")
    new_eid = _binding_field(new, "entry_id", "entryId", default=old_eid)

    property_changed = old_pk != new_pk or old_pv != new_pv or old_eid != new_eid
    span_changed = old_cs != new_cs or old_ce != new_ce

    if not property_changed and not span_changed:
        return [], []

    if property_changed:
        required.append(
            DiffItem(
                severity="required",
                item_type="binding_property_updated",
                description=f"Updated property on clause '{label}' ({old_pk} → {new_pk})",
                binding_id=bid,
                old_char_start=old_cs,
                old_char_end=old_ce,
                new_char_start=new_cs,
                new_char_end=new_ce,
            )
        )

    if span_changed:
        required.append(
            DiffItem(
                severity="required",
                item_type="binding_span_updated",
                description=f"Moved clause '{label}' from [{old_cs},{old_ce}) to [{new_cs},{new_ce})",
                binding_id=bid,
                old_char_start=old_cs,
                old_char_end=old_ce,
                new_char_start=new_cs,
                new_char_end=new_ce,
            )
        )
        text = script_text or ""
        stored = _binding_field(new, "selection_text", "selectionText", default=label)
        if stored and 0 <= new_cs < new_ce <= len(text):
            actual = text[new_cs:new_ce]
            if actual != stored:
                warnings.append(
                    DiffItem(
                        severity="warning",
                        item_type="binding_selection_mismatch",
                        description=f"Clause text at [{new_cs},{new_ce}) is '{actual}' but binding stores '{stored}'",
                        binding_id=bid,
                        old_char_start=old_cs,
                        old_char_end=old_ce,
                        new_char_start=new_cs,
                        new_char_end=new_ce,
                    )
                )

    return required, warnings
