"""Parse draft/suggestion script text into table-read dialogue blocks."""

from __future__ import annotations

import re

CHARACTER_RE = re.compile(r"^[A-Z][A-Z0-9 .'\-()]+$")
SCENE_HEADING_RE = re.compile(r"^(INT\.|EXT\.|INT/EXT\.|I/E\.)", re.IGNORECASE)


def _is_character_line(line: str) -> bool:
    s = line.strip()
    if not s or len(s) > 60:
        return False
    if SCENE_HEADING_RE.match(s):
        return False
    return bool(CHARACTER_RE.match(s))


def parse_reading_blocks(script_text: str) -> list[dict]:
    """Return ordered blocks with text spans for round-robin assignment."""
    text = script_text or ""
    if not text.strip():
        return []

    lines = text.splitlines(keepends=True)
    blocks: list[dict] = []
    offset = 0
    i = 0
    block_index = 0

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        line_start = offset
        line_end = offset + len(line)
        offset = line_end
        i += 1

        if not stripped:
            continue

        if _is_character_line(stripped):
            parts = [stripped]
            char_start = line_start
            while i < len(lines):
                nxt = lines[i]
                nxt_strip = nxt.strip()
                nxt_start = offset
                offset += len(nxt)
                i += 1
                if not nxt_strip:
                    break
                if _is_character_line(nxt_strip) or SCENE_HEADING_RE.match(nxt_strip):
                    offset -= len(nxt)
                    i -= 1
                    break
                parts.append(nxt_strip)
            block_text = "\n".join(parts)
            blocks.append(
                {
                    "index": block_index,
                    "kind": "dialogue",
                    "text": block_text,
                    "charStart": char_start,
                    "charEnd": char_start + len(block_text),
                }
            )
            block_index += 1
            continue

        if SCENE_HEADING_RE.match(stripped):
            kind = "scene"
        else:
            kind = "action"

        para_lines = [stripped]
        char_start = line_start
        while i < len(lines):
            nxt = lines[i]
            if not nxt.strip():
                offset += len(nxt)
                i += 1
                break
            if _is_character_line(nxt.strip()) or SCENE_HEADING_RE.match(nxt.strip()):
                break
            para_lines.append(nxt.strip())
            offset += len(nxt)
            i += 1

        block_text = "\n".join(para_lines)
        blocks.append(
            {
                "index": block_index,
                "kind": kind,
                "text": block_text,
                "charStart": char_start,
                "charEnd": char_start + len(block_text),
            }
        )
        block_index += 1

    if not blocks and text.strip():
        blocks.append(
            {
                "index": 0,
                "kind": "paragraph",
                "text": text.strip(),
                "charStart": 0,
                "charEnd": len(text.strip()),
            }
        )

    return blocks


def assign_round_robin(blocks: list[dict], participant_user_ids: list[str]) -> list[dict]:
    """Attach assignedUserId to each block by join order."""
    if not participant_user_ids:
        return [{**b, "assignedUserId": None} for b in blocks]
    out = []
    for i, block in enumerate(blocks):
        out.append({**block, "assignedUserId": participant_user_ids[i % len(participant_user_ids)]})
    return out
