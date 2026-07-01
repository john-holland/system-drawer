"""Parse Cave route strings (continuum:stories/list, resaurce:hr/help/request)."""

from __future__ import annotations


def parse_route(route: str) -> tuple[str | None, str]:
    r = str(route or "")
    idx = r.find(":")
    if idx > 0:
        prefix = r[:idx].lower()
        return prefix, r[idx + 1 :]
    return "continuum", r
