"""Parse {P:quest|...} lemma spans into compiled quest set JSON."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from typing import Any

QUEST_SPAN_RE = re.compile(
    r"\{\{?P:quest(?:\|([^}]+))?\}?\}?|\{P:quest(?:\|([^}]+))?\}",
    re.IGNORECASE,
)
INLINE_QUOTE_AFTER_SPAN = re.compile(r'^\s*("(?:\\.|[^"\\])*")\s*$')


@dataclass
class QuestIssue:
    level: str
    message: str
    line: int = 0


@dataclass
class QuestCompileResult:
    set_id: str = ""
    title: str = ""
    root_spatial_4d_id: str | None = None
    nodes: list[dict[str, Any]] = field(default_factory=list)
    generate_summary_spans: list[dict[str, Any]] = field(default_factory=list)
    generate_art_spans: list[dict[str, Any]] = field(default_factory=list)
    issues: list[QuestIssue] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "setId": self.set_id,
            "title": self.title,
            "rootSpatial4dId": self.root_spatial_4d_id,
            "nodes": self.nodes,
            "generateSummarySpans": self.generate_summary_spans,
            "generateArtSpans": self.generate_art_spans,
            "issues": [{"level": i.level, "message": i.message, "line": i.line} for i in self.issues],
        }


def _parse_props(raw: str | None) -> dict[str, str]:
    out: dict[str, str] = {}
    if not raw:
        return out
    for part in raw.split("|"):
        part = part.strip()
        if not part or "=" not in part:
            continue
        k, _, v = part.partition("=")
        k = k.strip()
        v = v.strip()
        if k and k not in out:
            out[k] = v
    return out


def _unquote_text(raw: str) -> str:
    raw = raw.strip()
    if len(raw) >= 2 and raw[0] == '"' and raw[-1] == '"':
        try:
            return json.loads(raw)
        except json.JSONDecodeError:
            return raw[1:-1]
    return raw


def _parse_bounds(raw: str | None, *, four_d: bool) -> dict[str, float] | None:
    if not raw:
        return None
    parts = [p.strip() for p in raw.split(",") if p.strip()]
    keys = ["xMin", "yMin", "zMin", "xMax", "yMax", "zMax", "tMin", "tMax"] if four_d else [
        "xMin", "yMin", "zMin", "xMax", "yMax", "zMax"
    ]
    if len(parts) != len(keys):
        return None
    try:
        return {k: float(v) for k, v in zip(keys, parts)}
    except ValueError:
        return None


def _extract_line(line: str, line_no: int) -> tuple[int, dict[str, str], str, QuestIssue | None]:
    indent = len(line) - len(line.lstrip(" "))
    stripped = line.strip()
    if not stripped or stripped.startswith("#"):
        return indent, {}, "", None

    m = QUEST_SPAN_RE.search(stripped)
    if not m:
        return indent, {}, "", QuestIssue("error", "Expected {P:quest|...} span", line_no)

    props = _parse_props(m.group(1) or m.group(2) or "")
    rest = stripped[m.end() :].strip()
    text = ""
    if rest:
        qm = INLINE_QUOTE_AFTER_SPAN.match(rest)
        if qm:
            text = _unquote_text(qm.group(1))
        elif rest.startswith('"'):
            text = _unquote_text(rest)
        elif not props.get("end-block"):
            return indent, props, "", QuestIssue("warning", f"Unparsed trailing content: {rest[:40]}", line_no)

    return indent, props, text, None


def compile_quest(text: str, default_set_id: str = "quest-set") -> QuestCompileResult:
    result = QuestCompileResult()
    if not text or not text.strip():
        result.issues.append(QuestIssue("error", "Empty quest text"))
        return result

    lines: list[dict[str, Any]] = []
    for i, raw_line in enumerate(text.splitlines(), start=1):
        indent, props, line_text, issue = _extract_line(raw_line, i)
        if issue and issue.level == "error":
            result.issues.append(issue)
            continue
        if issue:
            result.issues.append(issue)
        if not props and not line_text:
            continue
        lines.append({"line": i, "indent": indent, "props": props, "text": line_text})

    if not lines:
        result.issues.append(QuestIssue("error", "No quest spans found"))
        return result

    set_stack: list[str] = []
    block_stack: list[dict[str, Any]] = [{"indent": -1, "children": []}]
    objective_ids: set[str] = set()
    node_counter = 0
    open_summary: dict[str, Any] | None = None
    open_art: dict[str, Any] | None = None

    def new_id() -> str:
        nonlocal node_counter
        node_counter += 1
        return f"q{node_counter}"

    def current_children() -> list:
        return block_stack[-1]["children"]

    for row in lines:
        props = row["props"]
        text = row["text"]
        indent = row["indent"]
        line_no = row["line"]

        while len(block_stack) > 1 and indent <= block_stack[-1]["indent"]:
            block_stack.pop()

        if props.get("quest-set"):
            sid = props["quest-set"]
            set_stack.append(sid)
            if not result.set_id:
                result.set_id = sid
            if text and not result.title:
                result.title = text
            if props.get("spatial4d") and not result.root_spatial_4d_id:
                result.root_spatial_4d_id = props.get("spatial4d")
            if text:
                node = _make_node(new_id(), props, text, line_no)
                current_children().append(node)
            continue

        if props.get("end-block"):
            expected = props["end-block"]
            if not set_stack or set_stack[-1] != expected:
                result.issues.append(
                    QuestIssue(
                        "error",
                        f"end-block={expected} does not match open set {set_stack[-1] if set_stack else 'none'}",
                        line_no,
                    )
                )
            elif set_stack:
                set_stack.pop()
            continue

        if props.get("generate-summary-start"):
            open_summary = {"objectiveId": props.get("objective"), "line": line_no}
            if props.get("generate-summary-end"):
                open_summary["endLine"] = line_no
                result.generate_summary_spans.append(dict(open_summary))
                open_summary = None
            continue

        if props.get("generate-summary-end"):
            if open_summary:
                open_summary["endLine"] = line_no
                result.generate_summary_spans.append(dict(open_summary))
                open_summary = None
            else:
                result.issues.append(QuestIssue("error", "generate-summary-end without start", line_no))
            continue

        if props.get("generate-art-start"):
            open_art = {"objectiveId": props.get("objective"), "line": line_no}
            if props.get("generate-art-end"):
                open_art["endLine"] = line_no
                result.generate_art_spans.append(dict(open_art))
                open_art = None
            continue

        if props.get("generate-art-end"):
            if open_art:
                open_art["endLine"] = line_no
                result.generate_art_spans.append(dict(open_art))
                open_art = None
            else:
                result.issues.append(QuestIssue("error", "generate-art-end without start", line_no))
            continue

        if indent > block_stack[-1]["indent"]:
            parent_children = current_children()
            if parent_children:
                block_stack.append({"indent": indent, "children": parent_children[-1].setdefault("children", [])})
            else:
                block_stack.append({"indent": indent, "children": parent_children})

        node = _make_node(new_id(), props, text, line_no)
        oid = props.get("objective")
        if oid:
            if oid in objective_ids:
                result.issues.append(QuestIssue("warning", f"Duplicate objective id: {oid}", line_no))
            objective_ids.add(oid)

        current_children().append(node)

    if set_stack:
        result.issues.append(QuestIssue("error", f"Unclosed quest sets: {', '.join(set_stack)}"))

    if not result.set_id:
        result.set_id = default_set_id

    result.nodes = block_stack[0]["children"]
    return result


def _make_node(nid: str, props: dict[str, str], text: str, line_no: int) -> dict[str, Any]:
    bounds3d = _parse_bounds(props.get("bounds3d"), four_d=False)
    bounds4d = _parse_bounds(props.get("bounds4d"), four_d=True)
    node: dict[str, Any] = {
        "id": nid,
        "kind": "objective" if props.get("objective") else "control",
        "objectiveId": props.get("objective"),
        "text": text or props.get("summary") or "",
        "summary": props.get("summary") or text,
        "line": line_no,
        "spatial4dId": props.get("spatial4d"),
        "bounds3d": bounds3d,
        "bounds4d": bounds4d,
        "predicate4d": props.get("predicate4d"),
        "completion4d": props.get("completion4d"),
        "style": props.get("style") or props.get("style-suggest"),
        "travelBinding": props.get("travel-binding"),
        "mapLayer": props.get("map-layer") or "composite",
        "uiBt": props.get("ui-bt"),
        "mapBt": props.get("map-bt"),
        "animBt": props.get("anim-bt"),
        "audioCue": props.get("audio-cue"),
        "ambientLoop": props.get("ambient-loop"),
        "inpaintRegion": props.get("inpaint-region"),
        "children": [],
    }
    return node


def compile_quest_to_json(text: str, default_set_id: str = "quest-set") -> dict[str, Any]:
    return compile_quest(text, default_set_id).to_dict()


LITTLE_PRINCE_FIXTURE = """
{P:quest|quest-set=little-prince-tour}"Explore the asteroid belt"
  {P:quest|objective=meet-fox|spatial4d=s4d-fox-vol|predicate4d=fox-met|completion4d=fox-dialogue-done}
    {P:quest|summary=Meet the fox on the equator|style=watercolor-storybook}
    {P:quest|generate-summary-start}A wise fox waits where the sunset repeats...{P:quest|generate-summary-end}
    {P:quest|travel-binding=fox-approach|map-layer=emergence|ui-bt=quest-journal-minimal}
{P:quest|end-block=little-prince-tour}
""".strip()
