"""Whisper dialog hop via USC /api/media. Never import openai-whisper locally."""

from __future__ import annotations

from typing import Any

try:
    from continuuuum_api.usc_whisper import UscUnavailable, extract_transcript_text, transcribe_via_usc
except ImportError:
    from usc_whisper import UscUnavailable, extract_transcript_text, transcribe_via_usc


def _spans_from_whisper(whisper_json: dict[str, Any]) -> list[dict[str, Any]]:
    words = whisper_json.get("words") if isinstance(whisper_json, dict) else None
    spans: list[dict[str, Any]] = []
    if isinstance(words, list) and words:
        buf: list[str] = []
        start_ms = 0.0
        end_ms = 0.0
        for w in words:
            if not isinstance(w, dict):
                continue
            token = str(w.get("word") or w.get("text") or "").strip()
            if not token:
                continue
            t0 = float(w.get("start") or w.get("start_ms") or 0) * (1000 if float(w.get("start") or 0) < 1000 else 1)
            t1 = float(w.get("end") or w.get("end_ms") or t0) * (1000 if float(w.get("end") or 0) < 1000 else 1)
            if not buf:
                start_ms = t0
            buf.append(token)
            end_ms = t1
            if token.endswith((".", "?", "!")):
                spans.append(
                    {
                        "startMs": start_ms,
                        "endMs": max(end_ms, start_ms + 200),
                        "label": " ".join(buf),
                        "audioRef": "",
                        "dialogueSetId": "",
                    }
                )
                buf = []
        if buf:
            spans.append(
                {
                    "startMs": start_ms,
                    "endMs": max(end_ms, start_ms + 200),
                    "label": " ".join(buf),
                    "audioRef": "",
                    "dialogueSetId": "",
                }
            )
    if not spans:
        text = extract_transcript_text(whisper_json)
        if text:
            spans.append(
                {
                    "startMs": 0,
                    "endMs": 1000,
                    "label": text,
                    "audioRef": "",
                    "dialogueSetId": "",
                }
            )
    return spans


def _parallel_dialog_tree(spans: list[dict[str, Any]], recording_id: str) -> dict[str, Any]:
    nodes = []
    for i, span in enumerate(spans):
        nodes.append(
            {
                "id": f"webcam-val-{recording_id}-{i}",
                "kind": "voice_actor_line",
                "text": span.get("label") or "",
                "speakerKey": "",
                "dialogActorId": "",
                "audioRef": span.get("audioRef") or "",
                "startMs": span.get("startMs"),
                "endMs": span.get("endMs"),
                "presentation": "audio" if span.get("audioRef") else "text",
            }
        )
    return {
        "setId": f"webcam-whisper-{recording_id}",
        "nodes": nodes,
        "generateSpans": [],
        "issues": [],
    }


def run(file_path: str, payload: dict[str, Any]) -> dict[str, Any]:
    spec = (payload.get("model_spec") or "whisper@base").strip()
    rec_id = str(payload.get("recording_id") or "")
    try:
        whisper_json = transcribe_via_usc(file_path)
    except UscUnavailable as exc:
        raise RuntimeError(f"USC Whisper hop failed ({spec}): {exc}") from exc
    spans = _spans_from_whisper(whisper_json)
    tree = _parallel_dialog_tree(spans, rec_id or "anon")
    return {
        "whisper_json": whisper_json,
        "dialogSpans": spans,
        "dialogue_set": tree,
        "dialogue_set_id": tree["setId"],
    }
