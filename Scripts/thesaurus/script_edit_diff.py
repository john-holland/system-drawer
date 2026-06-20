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


def bindings_to_spans(bindings: Sequence[dict]) -> List[SpanRef]:
    out: List[SpanRef] = []
    for b in bindings or []:
        out.append(
            SpanRef(
                int(b.get("char_start") or b.get("charStart") or 0),
                int(b.get("char_end") or b.get("charEnd") or 0),
                binding_id=b.get("id"),
                label=b.get("selection_text") or b.get("selectionText") or "",
                kind="binding",
            )
        )
    return out


def _shift_span(span: SpanRef, edit: EditRegion) -> Tuple[int, int, bool, bool]:
    """Return (new_start, new_end, overlapped, shifted_only)."""
    s, e = span.char_start, span.char_end
    edit_end = edit.offset + edit.old_len
    if edit_end <= s or (edit.offset >= e and edit.old_len == 0):
        return s, e, False, False
    if edit.offset >= e:
        return s + edit.delta, e + edit.delta, False, True
    return s, e, True, False


def audit_edit(
    old_text: str,
    new_text: str,
    bindings: Sequence[dict],
) -> Tuple[List[DiffItem], List[DiffItem], List[dict]]:
    """Return (required, warnings, updated_bindings)."""
    required: List[DiffItem] = []
    warnings: List[DiffItem] = []
    regions = compute_edit_regions(old_text or "", new_text or "")
    all_spans = bindings_to_spans(bindings) + parse_prompt_spans(old_text or "")

    updated_bindings = [dict(b) for b in (bindings or [])]
    binding_by_id = {b.get("id"): b for b in updated_bindings if b.get("id")}

    for span in all_spans:
        new_start, new_end = span.char_start, span.char_end
        overlapped = False
        shifted = False
        for edit in regions:
            ns, ne, ov, sh = _shift_span(span, edit)
            if ov:
                overlapped = True
            if sh:
                shifted = True
            new_start, new_end = ns, ne

        label = span.label or span.kind
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
