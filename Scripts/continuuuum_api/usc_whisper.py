"""Call USC /api/media for Whisper transcripts. Continuuuum never imports openai-whisper."""

from __future__ import annotations

import json
import os
import time
import sys
from typing import Any, Callable
from urllib.parse import urljoin

LIBRARY_APP_BASE = os.environ.get("CONTINUUUUM_LIBRARY_BASE", "").rstrip("/") or "http://127.0.0.1:5050"

TranscribeFn = Callable[[str, str], dict[str, Any]]

_transcribe_impl: TranscribeFn | None = None


class UscUnavailable(Exception):
    """USC media hop missing or failed — processing segment should be failed, not local Whisper."""


def has_transcribe_impl() -> bool:
    if _transcribe_impl is not None:
        return True
    for name in ("usc_whisper", "continuuuum_api.usc_whisper"):
        mod = sys.modules.get(name)
        if mod is not None and getattr(mod, "_transcribe_impl", None) is not None:
            return True
    return False


def set_transcribe_impl(fn: TranscribeFn | None) -> None:
    global _transcribe_impl
    _transcribe_impl = fn
    for name in ("usc_whisper", "continuuuum_api.usc_whisper"):
        mod = sys.modules.get(name)
        if mod is not None:
            mod._transcribe_impl = fn


def extract_transcript_text(whisper_json: dict[str, Any] | None) -> str:
    if not whisper_json:
        return ""
    for key in ("text", "transcript", "transcription"):
        val = whisper_json.get(key)
        if isinstance(val, str) and val.strip():
            return val.strip()
    manifest = whisper_json.get("manifest")
    if isinstance(manifest, dict):
        return extract_transcript_text(manifest)
    words = whisper_json.get("words")
    if isinstance(words, list):
        parts = []
        for w in words:
            if isinstance(w, dict) and w.get("word"):
                parts.append(str(w["word"]))
            elif isinstance(w, str):
                parts.append(w)
        return " ".join(parts).strip()
    return ""


def _norm(text: str) -> str:
    return " ".join((text or "").lower().split())


def transcript_matches_quote(whisper_json: dict[str, Any] | None, quote_text: str) -> bool:
    a = _norm(extract_transcript_text(whisper_json))
    b = _norm(quote_text)
    if not a or not b:
        return False
    if b in a or a in b:
        return True
    aw = set(a.split())
    bw = set(b.split())
    if not bw:
        return False
    return (len(aw & bw) / len(bw)) >= 0.6


def transcribe_via_usc(file_path: str, library_base: str | None = None) -> dict[str, Any]:
    """Store media on USC and return transcript JSON. Raises UscUnavailable on hop failure."""
    impl = _transcribe_impl
    if impl is None:
        for name in ("usc_whisper", "continuuuum_api.usc_whisper"):
            mod = sys.modules.get(name)
            if mod is not None and getattr(mod, "_transcribe_impl", None) is not None:
                impl = mod._transcribe_impl
                break
    if impl is not None:
        try:
            return impl(file_path, library_base or LIBRARY_APP_BASE)
        except Exception as exc:
            if exc.__class__.__name__ == "UscUnavailable":
                raise UscUnavailable(str(exc)) from exc
            raise

    base = (library_base or LIBRARY_APP_BASE).rstrip("/")
    if not file_path:
        raise UscUnavailable("no audio file for USC Whisper")
    try:
        import requests
    except ImportError as exc:
        raise UscUnavailable("requests package required for USC Whisper") from exc

    url = urljoin(base + "/", "api/media/store")
    try:
        with open(file_path, "rb") as fh:
            resp = requests.post(url, files={"file": (os.path.basename(file_path), fh)}, timeout=60)
    except OSError as exc:
        raise UscUnavailable(f"cannot read audio: {exc}") from exc
    except requests.RequestException as exc:
        raise UscUnavailable(f"USC media unreachable: {exc}") from exc

    if resp.status_code >= 400:
        raise UscUnavailable(f"USC media store failed: {resp.status_code}")
    try:
        payload = resp.json()
    except json.JSONDecodeError as exc:
        raise UscUnavailable("USC media store returned non-JSON") from exc

    job_id = payload.get("id") or payload.get("job_id")
    if payload.get("transcript") or payload.get("text"):
        return payload
    if not job_id:
        raise UscUnavailable("USC media store did not return a job id")

    status_url = urljoin(base + "/", f"api/media/stored/{job_id}/status")
    deadline = time.time() + 90
    last: dict[str, Any] = payload
    while time.time() < deadline:
        try:
            st = requests.get(status_url, timeout=20)
        except requests.RequestException as exc:
            raise UscUnavailable(f"USC media status unreachable: {exc}") from exc
        if st.status_code == 404:
            raise UscUnavailable("USC media job not found")
        try:
            last = st.json()
        except json.JSONDecodeError:
            last = {"raw": st.text}
        status = str(last.get("status") or "").lower()
        if status in ("ready", "done", "complete", "completed"):
            last["usc_job_id"] = job_id
            return last
        if status in ("failed", "error"):
            raise UscUnavailable(last.get("error") or "USC Whisper failed")
        time.sleep(0.4)
    raise UscUnavailable("USC Whisper timed out")
