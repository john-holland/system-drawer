"""Stub quest summary and art generation (v1 deterministic hashes)."""

from __future__ import annotations

import hashlib
import json
from typing import Any


def suggest_style(body: dict[str, Any]) -> dict[str, Any]:
    hint = str(body.get("styleHint") or body.get("style") or "storybook")
    digest = hashlib.sha256(hint.encode()).hexdigest()[:8]
    profiles = [
        {
            "id": f"style-{digest}-a",
            "tone": hint,
            "palette": ["#c4a882", "#2d5a27"],
            "typography": "serif-hand",
            "mapTreatment": {"inkOutline": 0.6, "paperGrain": 0.3},
        },
        {
            "id": f"style-{digest}-b",
            "tone": f"{hint}-minimal",
            "palette": ["#8899aa", "#334455"],
            "typography": "sans-clean",
            "mapTreatment": {"inkOutline": 0.2, "paperGrain": 0.1},
        },
    ]
    return {"ok": True, "profiles": profiles, "styleHint": hint}


def generate_summary(body: dict[str, Any]) -> dict[str, Any]:
    prompt = str(body.get("prompt") or body.get("context") or "")
    style = str(body.get("styleHint") or body.get("style") or "storybook")
    text = f"{prompt} ({style} draft)".strip() or f"Quest summary ({style})"
    digest = hashlib.sha256(text.encode()).hexdigest()[:12]
    return {
        "ok": True,
        "text": text,
        "suggestionHash": digest,
        "styleNotes": f"Stub LLM style notes for {style}",
    }


def generate_art(body: dict[str, Any]) -> dict[str, Any]:
    prompt = str(body.get("prompt") or "")
    kind = str(body.get("kind") or "banner")
    digest = hashlib.sha256(f"{kind}:{prompt}".encode()).hexdigest()[:16]
    return {
        "ok": True,
        "assetRef": f"quest-art-stub://{digest}",
        "kind": kind,
        "prompt": prompt,
    }


def inpaint_art(body: dict[str, Any]) -> dict[str, Any]:
    base = str(body.get("assetRef") or "")
    mask = body.get("mask") or body.get("inpaintRegion") or {}
    digest = hashlib.sha256(json.dumps({"base": base, "mask": mask}, sort_keys=True).encode()).hexdigest()[:16]
    return {
        "ok": True,
        "assetRef": f"quest-art-inpaint://{digest}",
        "sourceRef": base,
        "mask": mask,
    }
