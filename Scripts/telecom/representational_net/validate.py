"""Validate representational-net HTML sites."""

from __future__ import annotations

import json
import re
from html.parser import HTMLParser
from pathlib import Path
from typing import Any
from urllib.parse import urlparse

REQUIRED_TAGS = {"html", "head", "body", "main", "nav", "title"}
EXTERNAL_SCHEMES = {"http", "https", "//"}


class _TagCollector(HTMLParser):
    def __init__(self):
        super().__init__()
        self.tags: set[str] = set()
        self.links: list[str] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self.tags.add(tag.lower())
        if tag.lower() == "a":
            for k, v in attrs:
                if k.lower() == "href" and v:
                    self.links.append(v)


def load_manifest(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_site(site_dir: Path, *, device_ids: list[str] | None = None) -> list[str]:
    errors: list[str] = []
    manifest_path = site_dir / "manifest.json"
    if not manifest_path.exists():
        return [f"missing manifest: {manifest_path}"]
    manifest = load_manifest(manifest_path)
    pages = manifest.get("pages") or []
    slugs = {p["slug"] for p in pages}
    if "index.html" not in slugs:
        errors.append("manifest must include index.html")

    all_linked: set[str] = set()
    for page in pages:
        slug = page["slug"]
        html_path = site_dir / slug
        if not html_path.exists():
            errors.append(f"missing page file: {slug}")
            continue
        html = html_path.read_text(encoding="utf-8")
        if not html.strip().lower().startswith("<!doctype html"):
            errors.append(f"{slug}: must start with <!DOCTYPE html>")
        parser = _TagCollector()
        try:
            parser.feed(html)
        except Exception as e:
            errors.append(f"{slug}: HTML parse error: {e}")
            continue
        missing = REQUIRED_TAGS - parser.tags
        if missing:
            errors.append(f"{slug}: missing tags: {sorted(missing)}")
        for href in parser.links:
            if href.startswith("#"):
                continue
            parsed = urlparse(href)
            if parsed.scheme in ("http", "https"):
                errors.append(f"{slug}: external link disallowed: {href}")
                continue
            if href.startswith("tel:"):
                continue
            if href.startswith("telecom://device/"):
                dev = href.split("telecom://device/", 1)[-1].split("/", 1)[0]
                if device_ids and dev not in device_ids:
                    errors.append(f"{slug}: unknown telecom device link: {dev}")
                continue
            target = href.split("#")[0].split("?")[0]
            if target and target not in slugs:
                errors.append(f"{slug}: broken link: {href}")
            if target:
                all_linked.add(target)
        for link in page.get("links") or []:
            if link not in slugs:
                errors.append(f"manifest {slug}: declares broken link {link}")

    orphans = slugs - all_linked - {"index.html"}
    if orphans:
        errors.append(f"orphan pages (no inbound links): {sorted(orphans)}")

    return errors


def repair_hints(errors: list[str]) -> str:
    lines = ["Fix the following representational-net validation errors:"]
    lines.extend(f"- {e}" for e in errors)
    lines.append("Ensure every page has DOCTYPE, html/head/body/main/nav/title, and only manifest slugs in href.")
    return "\n".join(lines)


def main() -> int:
    import argparse

    ap = argparse.ArgumentParser()
    ap.add_argument("site_dir", type=Path)
    ap.add_argument("--repair-hints", action="store_true")
    args = ap.parse_args()
    errors = validate_site(args.site_dir)
    if args.repair_hints and errors:
        print(repair_hints(errors))
        return 1
    if errors:
        for e in errors:
            print(e)
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
