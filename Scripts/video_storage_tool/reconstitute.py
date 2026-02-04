"""
Reconstitute: merge stored resultant video + audio into one output file (ffmpeg).
Handles duration mismatch by extending/looping the resultant video to match audio length.
"""

import subprocess
from pathlib import Path


def find_artifacts(stored_dir: Path) -> tuple[Path | None, Path | None]:
    """Return (audio_path, resultant_video_path). Prefer manifest.json if present."""
    stored_dir = Path(stored_dir)
    manifest = stored_dir / "manifest.json"
    if manifest.exists():
        import json
        with open(manifest, "r", encoding="utf-8") as f:
            data = json.load(f)
        audio = data.get("audio")
        resultant = data.get("resultant_video")
        if audio and resultant:
            ap, rp = Path(audio), Path(resultant)
            if ap.exists() and rp.exists():
                return ap, rp
    # Discover by name
    audio_path = None
    for f in stored_dir.iterdir():
        if f.suffix.lower() in (".aac", ".mp3") and f.name.lower().startswith("audio"):
            audio_path = f
            break
    resultant_path = stored_dir / "resultant.mp4"
    if not resultant_path.exists():
        resultant_path = None
    return audio_path, resultant_path


def get_media_duration_seconds(path: Path) -> float:
    """Probe duration with ffprobe."""
    try:
        out = subprocess.run(
            [
                "ffprobe", "-v", "error",
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


def reconstitute(stored_dir: Path, out_path: Path) -> None:
    """
    Merge resultant video and audio into one file. If audio is longer than resultant video,
    loop the resultant video (or pad with last frame) so output duration = audio duration.
    """
    stored_dir = Path(stored_dir)
    out_path = Path(out_path)
    audio_path, resultant_path = find_artifacts(stored_dir)
    if not audio_path or not resultant_path:
        raise FileNotFoundError(
            f"Could not find audio and resultant video in {stored_dir}. "
            "Expected audio.aac/audio.mp3 and resultant.mp4 (or manifest.json)."
        )
    audio_dur = get_media_duration_seconds(audio_path)
    video_dur = get_media_duration_seconds(resultant_path)
    if audio_dur <= 0:
        raise ValueError(f"Could not get audio duration for {audio_path}")
    # If resultant is shorter than audio, we need to loop/pad video to audio_dur
    if video_dur <= 0 or video_dur >= audio_dur:
        # Use resultant as-is (or single stream) and trim/pad audio to match if needed
        # Simple: use video as video track, audio as audio track; ffmpeg will use shortest stream unless we specify
        # We want output length = audio length, so we need to extend video to audio_dur (loop or last frame)
        _merge_ffmpeg(resultant_path, audio_path, out_path, target_duration_sec=audio_dur, video_duration_sec=video_dur)
    else:
        _merge_ffmpeg(resultant_path, audio_path, out_path, target_duration_sec=audio_dur, video_duration_sec=video_dur)


def _merge_ffmpeg(
    video_path: Path,
    audio_path: Path,
    out_path: Path,
    *,
    target_duration_sec: float,
    video_duration_sec: float,
) -> None:
    """
    Mux video + audio. If video is shorter than target_duration_sec, loop the video
    to match (using stream_loop) then trim to target_duration_sec.
    """
    out_path.parent.mkdir(parents=True, exist_ok=True)
    if video_duration_sec <= 0 or video_duration_sec >= target_duration_sec:
        # Mux; trim to target (audio) duration
        cmd = [
            "ffmpeg", "-y",
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
            "ffmpeg", "-y",
            "-stream_loop", str(loop_count),
            "-i", str(video_path),
            "-i", str(audio_path),
            "-t", str(target_duration_sec),
            "-c:v", "libx264", "-c:a", "aac",
            str(out_path),
        ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600)
