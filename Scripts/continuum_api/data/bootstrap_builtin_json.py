"""One-off bootstrap: generate builtin_vocabulary.json from VocabularyBuiltInRegistry.cs.
Prefer Unity menu Continuum > Export Built-in Vocabulary JSON for canonical output."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
CS = ROOT / "Assets" / "Continuum" / "VocabularyBuiltInRegistry.cs"
OUT = Path(__file__).resolve().parent / "builtin_vocabulary.json"
PREFIX = "urn:unity:continuum:builtin:v1:"


def slug(s: str) -> str:
    out: list[str] = []
    last_us = False
    for c in s:
        if c.isalnum():
            out.append(c.lower())
            last_us = False
        elif c in "_- " and out and not last_us:
            out.append("_")
            last_us = True
    r = "".join(out).strip("_")
    return r or "_"


def urn(lang: str, seg: str, term: str) -> str:
    return f"{PREFIX}/{lang.lower()}/{slug(seg)}/{slug(term)}"


def _parse_tags(tags_raw: str | None) -> list[str]:
    if not tags_raw or tags_raw.strip() == "null":
        return []
    if tags_raw.strip() == "prepSpatial":
        return ["spatial"]
    if tags_raw.strip() == "playerControllerTags":
        return ["controller", "spatial", "player"]
    return re.findall(r'"([^"]+)"', tags_raw)


def main() -> None:
    text = CS.read_text(encoding="utf-8")
    items: list[dict] = []

    add_re = re.compile(
        r'Add\("([^"]+)", "([^"]+)", "([^"]+)", VocabularyBuiltInCategory\.(\w+)(?:, ([^)]+))?\)'
    )
    for m in add_re.finditer(text):
        seg, term, pos, cat, tags_raw = m.groups()
        items.append(
            {
                "id": urn("en", seg, term),
                "term": term,
                "posTag": pos,
                "languageCode": "en",
                "builtInCategory": cat,
                "tags": _parse_tags(tags_raw),
            }
        )

    foreach_re = re.compile(
        r"foreach \(var w in new\[\]\s*\{([^}]+)\}\)\s*\n?\s*Add\(\"([^\"]+)\", w, \"([^\"]+)\", VocabularyBuiltInCategory\.(\w+)(?:, ([^)]+))?\)",
        re.MULTILINE,
    )
    for m in foreach_re.finditer(text):
        words_block, seg, pos, cat, tags_raw = m.groups()
        words = re.findall(r'"([^"]+)"', words_block)
        tags = _parse_tags(tags_raw)
        for term in words:
            items.append(
                {
                    "id": urn("en", seg, term),
                    "term": term,
                    "posTag": pos,
                    "languageCode": "en",
                    "builtInCategory": cat,
                    "tags": list(tags),
                }
            )

    for m in re.finditer(r'Lit\("([^"]+)", "([^"]+)"\)', text):
        term, tag = m.groups()
        items.append(
            {
                "id": urn("en", "literal", term),
                "term": term,
                "posTag": "type_name",
                "languageCode": "en",
                "builtInCategory": "LiteralType",
                "tags": ["literal", tag],
            }
        )

    # Deduplicate by id
    by_id = {item["id"]: item for item in items}
    items = list(by_id.values())
    items.sort(key=lambda x: x["term"])

    OUT.write_text(json.dumps({"version": 1, "items": items}, indent=2), encoding="utf-8")
    print(f"Wrote {len(items)} entries to {OUT}")


if __name__ == "__main__":
    main()
