"""Parse {P:dialogue|...} lemma spans into compiled dialogue set JSON."""

from __future__ import annotations

import json
import re
import uuid
from dataclasses import dataclass, field
from typing import Any

DIALOGUE_SPAN_RE = re.compile(
    r"\{\{?P:dialogue(?:\|([^}]+))?\}?\}?|\{P:dialogue(?:\|([^}]+))?\}",
    re.IGNORECASE,
)
QUOTE_RE = re.compile(r'^(\s*)("(?:\\.|[^"\\])*")?\s*$')
INLINE_QUOTE_AFTER_SPAN = re.compile(r'^\s*("(?:\\.|[^"\\])*")\s*$')


@dataclass
class DialogueIssue:
    level: str  # error | warning
    message: str
    line: int = 0


@dataclass
class DialogueCompileResult:
    set_id: str = ""
    nodes: list[dict[str, Any]] = field(default_factory=list)
    generate_spans: list[dict[str, Any]] = field(default_factory=list)
    issues: list[DialogueIssue] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "setId": self.set_id,
            "nodes": self.nodes,
            "generateSpans": self.generate_spans,
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


def _parse_options(raw: str | None) -> list[str]:
    if not raw:
        return []
    raw = raw.strip()
    if raw.startswith("[") and raw.endswith("]"):
        raw = raw[1:-1]
    return [p.strip() for p in raw.split(",") if p.strip()]


def _unquote_dialogue_text(raw: str) -> str:
    raw = raw.strip()
    if len(raw) >= 2 and raw[0] == '"' and raw[-1] == '"':
        try:
            return json.loads(raw)
        except json.JSONDecodeError:
            return raw[1:-1]
    return raw


def _extract_line(line: str, line_no: int) -> tuple[int, dict[str, str], str, DialogueIssue | None]:
    indent = len(line) - len(line.lstrip(" "))
    stripped = line.strip()
    if not stripped or stripped.startswith("#"):
        return indent, {}, "", None

    m = DIALOGUE_SPAN_RE.search(stripped)
    if not m:
        return indent, {}, "", DialogueIssue("error", f"Expected {{P:dialogue|...}} span", line_no)

    props_raw = m.group(1) or m.group(2) or ""
    props = _parse_props(props_raw)
    rest = stripped[m.end() :].strip()
    text = ""
    if rest:
        qm = INLINE_QUOTE_AFTER_SPAN.match(rest)
        if qm:
            text = _unquote_dialogue_text(qm.group(1))
        elif rest.startswith('"'):
            text = _unquote_dialogue_text(rest)
        else:
            return indent, props, "", DialogueIssue("warning", f"Unparsed trailing content: {rest[:40]}", line_no)

    return indent, props, text, None


def compile_dialogue(text: str, default_set_id: str = "dialogue-set") -> DialogueCompileResult:
    """Compile lemma dialogue text to JSON tree contract."""
    result = DialogueCompileResult()
    if not text or not text.strip():
        result.issues.append(DialogueIssue("error", "Empty dialogue text"))
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
        result.issues.append(DialogueIssue("error", "No dialogue spans found"))
        return result

    set_stack: list[str] = []
    block_stack: list[dict[str, Any]] = [{"indent": -1, "children": []}]
    answer_ids: set[str] = set()
    node_counter = 0
    open_generate: dict[str, Any] | None = None

    def new_id() -> str:
        nonlocal node_counter
        node_counter += 1
        return f"n{node_counter}"

    def current_children() -> list:
        return block_stack[-1]["children"]

    for row in lines:
        props = row["props"]
        text = row["text"]
        indent = row["indent"]
        line_no = row["line"]

        while len(block_stack) > 1 and indent <= block_stack[-1]["indent"]:
            block_stack.pop()

        if props.get("dialogue-set") or props.get("dialog-set"):
            sid = props.get("dialogue-set") or props.get("dialog-set")
            if props.get("dialogue-set") and not props.get("answer"):
                set_stack.append(sid)
                if not result.set_id:
                    result.set_id = sid
                if text:
                    node = _make_node(new_id(), props, text, line_no)
                    current_children().append(node)
                continue

        if props.get("end-block"):
            expected = props.get("end-block")
            if not set_stack or set_stack[-1] != expected:
                result.issues.append(
                    DialogueIssue(
                        "error",
                        f"end-block={expected} does not match open set {set_stack[-1] if set_stack else 'none'}",
                        line_no,
                    )
                )
            elif set_stack:
                set_stack.pop()
            continue

        if props.get("generate-dialogue-start"):
            open_generate = {
                "startNode": props.get("generate-dialogue-start"),
                "spatial4dId": props.get("generate-dialogue-start"),
                "line": line_no,
            }
            if props.get("generate-dialogue-end"):
                open_generate["endNode"] = props.get("generate-dialogue-end")
                result.generate_spans.append(dict(open_generate))
                open_generate = None
            continue

        if props.get("generate-dialogue-end"):
            if open_generate:
                open_generate["endNode"] = props.get("generate-dialogue-end")
                result.generate_spans.append(dict(open_generate))
                open_generate = None
            else:
                result.issues.append(DialogueIssue("error", "generate-dialogue-end without start", line_no))
            continue

        if indent > block_stack[-1]["indent"]:
            pass
        elif indent < block_stack[-1]["indent"]:
            while len(block_stack) > 1 and indent <= block_stack[-1]["indent"]:
                block_stack.pop()

        parent_children = current_children()
        if indent > block_stack[-1]["indent"]:
            if parent_children:
                block_stack.append({"indent": indent, "children": parent_children[-1].setdefault("children", [])})
            else:
                block_stack.append({"indent": indent, "children": parent_children})

        node = _make_node(new_id(), props, text, line_no)
        aid = props.get("answer")
        if aid:
            if aid in answer_ids:
                result.issues.append(DialogueIssue("warning", f"Duplicate answer id: {aid}", line_no))
            answer_ids.add(aid)

        if not text and props.get("presentation") != "ui" and not props.get("dialogue-set"):
            result.issues.append(DialogueIssue("warning", "Line node missing quoted text", line_no))

        current_children().append(node)

    if set_stack:
        result.issues.append(
            DialogueIssue("error", f"Unclosed dialogue sets: {', '.join(set_stack)}")
        )

    if not result.set_id:
        result.set_id = default_set_id

    result.nodes = block_stack[0]["children"]
    return result


def _make_node(nid: str, props: dict[str, str], text: str, line_no: int) -> dict[str, Any]:
    presentation = props.get("presentation", "text")
    if presentation not in ("text", "ui", "audio"):
        presentation = "text"
    vis = props.get("vis", "auto")
    node: dict[str, Any] = {
        "id": nid,
        "kind": "line",
        "text": text,
        "presentation": presentation,
        "line": line_no,
        "answers": [],
        "goal": props.get("goal"),
        "predicate4d": props.get("predicate4d"),
        "completion4d": props.get("completion4d"),
        "continueWithDialogue": props.get("continue-with-dialogue"),
        "speakerKey": props.get("speaker"),
        "visMode": vis,
        "audioRef": props.get("audio-ref") or props.get("audioRef"),
        "children": [],
    }
    opts = _parse_options(props.get("options"))
    if opts:
        node["options"] = opts
    if props.get("answer"):
        node["answerId"] = props.get("answer")
    sub = props.get("dialog-set") or props.get("dialogue-set")
    if sub and props.get("answer"):
        node["dialogSet"] = sub
    return node


def compile_dialogue_to_json(text: str, default_set_id: str = "dialogue-set") -> dict[str, Any]:
    return compile_dialogue(text, default_set_id).to_dict()


BOOK_CONCERT_FIXTURE = """
{P:dialogue|dialogue-set=book-concert}"What books do you think should play?"
{P:dialogue|answer=windy-man|speaker=fox}"The windy man."
{P:dialogue|answer=long-mover|dialog-set=long-mover|speaker=fox}"The Long Mover: The Python"
  {P:dialogue|answer=handcuff-python|speaker=prince}"Oh, that's the one where they handcuff the python!?"
  {P:dialogue|predicate4d=zoan-understanding|completion4d=node-id-prs8jc|speaker=fox}"You know, you can't handcuff pythons."
  {P:dialogue|continue-with-dialogue=bespoke-id|options=[long-mover]|speaker=fox}"You know, you can't handcuff pythons."
{P:dialogue|end-block=book-concert}
{P:dialogue|generate-dialogue-start=4d-node-id|generate-dialogue-end=4d-node-id}
""".strip()
