"""
Reconstitute: merge stored resultant video + audio into one output file (ffmpeg).
Handles duration mismatch by extending/looping the resultant video to match audio length.
Optional: apply diff for original-quality output (resultant + diff + audio).
"""

import json
import logging
import subprocess
import sys
from pathlib import Path

log = logging.getLogger("video_storage_tool.reconstitute")

from .audio import _find_ffmpeg, _find_ffprobe
from .diff import apply_diff_ffmpeg


def find_artifacts(stored_dir: Path) -> tuple[Path | None, Path | None, Path | None]:
    """Return (audio_path, resultant_video_path, diff_path). Prefer manifest.json if present."""
    stored_dir = Path(stored_dir)
    manifest = stored_dir / "manifest.json"
    diff_path = None
    if manifest.exists():
        with open(manifest, "r", encoding="utf-8") as f:
            data = json.load(f)
        audio = data.get("audio")
        resultant = data.get("resultant_video")
        diff_path = data.get("diff_video")
        if audio and resultant:
            ap, rp = Path(audio), Path(resultant)
            if ap.exists() and rp.exists():
                dp = Path(diff_path) if diff_path and Path(diff_path).exists() else (stored_dir / "diff.ogv" if (stored_dir / "diff.ogv").exists() else None)
                return ap, rp, dp
    # Discover by name
    audio_path = None
    for f in stored_dir.iterdir():
        if f.suffix.lower() in (".aac", ".mp3") and f.name.lower().startswith("audio"):
            audio_path = f
            break
    resultant_path = stored_dir / "resultant.mp4"
    if not resultant_path.exists():
        resultant_path = None
    diff_path = stored_dir / "diff.ogv"
    if not diff_path.exists():
        diff_path = None
    return audio_path, resultant_path, diff_path


def get_media_duration_seconds(path: Path, ffprobe_exe: str = "ffprobe") -> float:
    """Probe duration with ffprobe."""
    try:
        out = subprocess.run(
            [
                ffprobe_exe, "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                str(path),
            ],
            capture_output=True,
            text=True,
            check=True,
            timeout=30,
        )
        return float(out.stdout.strip() or 0)
    except (subprocess.CalledProcessError, ValueError, FileNotFoundError):
        return 0.0


def reconstitute(
    stored_dir: Path,
    out_path: Path,
    *,
    use_diff: bool = False,
    ffmpeg_path: str | Path | None = None,
) -> None:
    """
    Merge resultant video and audio into one file. If audio is longer than resultant video,
    loop the resultant video (or pad with last frame) so output duration = audio duration.
    If use_diff is True and diff.ogv exists, output is original-quality (resultant + diff + audio).
    """
    stored_dir = Path(stored_dir)
    out_path = Path(out_path)
    if ffmpeg_path:
        ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
        ffprobe_exe = _find_ffprobe(ffmpeg_path)
    else:
        ffmpeg_exe = "ffmpeg"
        ffprobe_exe = "ffprobe"
    log.info("Reconstituting %s -> %s (use_diff=%s)", stored_dir, out_path.name, use_diff)
    audio_path, resultant_path, diff_path = find_artifacts(stored_dir)
    if not audio_path or not resultant_path:
        raise FileNotFoundError(
            f"Could not find audio and resultant video in {stored_dir}. "
            "Expected audio.aac/audio.mp3 and resultant.mp4 (or manifest.json)."
        )
    audio_dur = get_media_duration_seconds(audio_path, ffprobe_exe=ffprobe_exe)
    if audio_dur <= 0:
        raise ValueError(f"Could not get audio duration for {audio_path}")
    if use_diff and diff_path and diff_path.exists():
        log.info("Muxing resultant + diff + audio (original quality), duration=%.1fs", audio_dur)
        apply_diff_ffmpeg(
            resultant_path,
            diff_path,
            audio_path,
            out_path,
            target_duration_sec=audio_dur,
            ffmpeg_exe=ffmpeg_exe,
            ffprobe_exe=ffprobe_exe,
        )
        log.info("Reconstituted: %s", out_path)
        return
    if use_diff:
        log.warning("--original requested but no diff.ogv found; using resultant + audio only.")
        print("Warning: --original requested but no diff.ogv found; using resultant + audio only.", file=sys.stderr)
    video_dur = get_media_duration_seconds(resultant_path, ffprobe_exe=ffprobe_exe)
    log.info("Muxing resultant + audio, audio_dur=%.1fs video_dur=%.1fs", audio_dur, video_dur)
    _merge_ffmpeg(
        resultant_path,
        audio_path,
        out_path,
        target_duration_sec=audio_dur,
        video_duration_sec=video_dur,
        ffmpeg_exe=ffmpeg_exe,
    )
    log.info("Reconstituted: %s", out_path)


def _merge_ffmpeg(
    video_path: Path,
    audio_path: Path,
    out_path: Path,
    *,
    target_duration_sec: float,
    video_duration_sec: float,
    ffmpeg_exe: str = "ffmpeg",
) -> None:
    """
    Mux video + audio. If video is shorter than target_duration_sec, loop the video
    to match (using stream_loop) then trim to target_duration_sec.
    """
    out_path.parent.mkdir(parents=True, exist_ok=True)
    if video_duration_sec <= 0 or video_duration_sec >= target_duration_sec:
        # Mux; trim to target (audio) duration
        cmd = [
            ffmpeg_exe, "-y",
            "-i", str(video_path),
            "-i", str(audio_path),
            "-t", str(target_duration_sec),
            "-c:v", "copy", "-c:a", "aac",
            str(out_path),
        ]
    else:
        # Loop video to match audio length, then trim to exact target
        loop_count = int(target_duration_sec / video_duration_sec) + 1
        cmd = [
            ffmpeg_exe, "-y",
            "-stream_loop", str(loop_count),
            "-i", str(video_path),
            "-i", str(audio_path),
            "-t", str(target_duration_sec),
            "-c:v", "libx264", "-c:a", "aac",
            str(out_path),
        ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600)
