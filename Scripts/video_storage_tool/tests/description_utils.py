"""
Helper for description-based verification: extract semantic description from video
and compute similarity between descriptions.
"""
import re
import tempfile
from pathlib import Path
from typing import Optional

# Section headers in script.txt
SCRIPT_SECTION_TRANSCRIPT = "[Transcript]"
SCRIPT_SECTION_VISUAL = "[Visual description]"


def describe_video(
    video_path: Path,
    config: Optional[dict] = None,
    *,
    backend: str = "whisper",
    include_visual: bool = False,
) -> str:
    """
    Produce a semantic description of the video (transcript + optional visual).
    Uses video_storage_tool pipeline: extract audio, then video_to_script.
    Returns combined description string. Raises ImportError if whisper not installed.
    """
    config = config or {}
    if not include_visual:
        config = {**config, "script": {**(config.get("script") or {}), "visual_backend": "none"}}
    with tempfile.TemporaryDirectory(prefix="vst_describe_") as tmpdir:
        tmp = Path(tmpdir)
        from video_storage_tool.audio import extract_and_compress_audio
        from video_storage_tool.video_to_script import video_to_script

        audio_path = extract_and_compress_audio(
            video_path, tmp, format="aac", max_mb=5.0,
            ffmpeg_path=config.get("audio", {}).get("ffmpeg_path"),
        )
        script_path = video_to_script(
            video_path, audio_path, tmp,
            backend=backend,
            config=config,
        )
        content = script_path.read_text(encoding="utf-8")
    return content


def extract_transcript_from_script(script_content: str) -> str:
    """Extract [Transcript] section from script.txt content."""
    if SCRIPT_SECTION_TRANSCRIPT not in script_content:
        return script_content.strip()
    start = script_content.find(SCRIPT_SECTION_TRANSCRIPT) + len(SCRIPT_SECTION_TRANSCRIPT)
    end = script_content.find("[", start)
    if end < 0:
        end = len(script_content)
    return script_content[start:end].strip()


def description_similarity(a: str, b: str) -> float:
    """
    Compute word-overlap similarity between two description strings.
    Returns a value in [0, 1]. Uses Jaccard-like token overlap.
    """
    def tokens(s: str) -> set[str]:
        normalized = re.sub(r"\s+", " ", s.lower().strip())
        return set(w for w in re.findall(r"\b\w+\b", normalized) if len(w) > 1)

    ta, tb = tokens(a), tokens(b)
    if not ta and not tb:
        return 1.0
    if not ta or not tb:
        return 0.0
    intersection = len(ta & tb)
    union = len(ta | tb)
    return intersection / union if union else 0.0
