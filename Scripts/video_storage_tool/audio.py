"""
Extract and compress audio from a video file to a target size (few MB) using ffmpeg.
"""

import subprocess
from pathlib import Path


def get_video_duration_seconds(video_path: Path) -> float:
    """Probe duration with ffprobe. Returns 0.0 on failure."""
    try:
        out = subprocess.run(
            [
                "ffprobe",
                "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                str(video_path),
            ],
            capture_output=True,
            text=True,
            check=True,
            timeout=60,
        )
        return float(out.stdout.strip() or 0)
    except (subprocess.CalledProcessError, ValueError, FileNotFoundError):
        return 0.0


def extract_and_compress_audio(
    video_path: Path,
    out_dir: Path,
    *,
    format: str = "aac",
    max_mb: float = 5.0,
) -> Path:
    """
    Extract audio from video and encode to stay under max_mb.
    Uses a bitrate derived from duration and max_mb; falls back to 128k if duration unknown.
    """
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    duration = get_video_duration_seconds(video_path)
    # target bytes = max_mb * 1e6; bitrate (bits/s) = target * 8 / duration_sec
    if duration > 0:
        target_bits = max_mb * 1e6 * 8
        bitrate_k = int(target_bits / duration / 1000)
        bitrate_k = max(32, min(320, bitrate_k))
    else:
        bitrate_k = 128
    ext = "aac" if format == "aac" else "mp3"
    out_path = out_dir / f"audio.{ext}"
    # -vn: no video, -ac 1 or 2, -b:a for bitrate
    if format == "aac":
        codec = "aac"
        bitrate_arg = ["-b:a", f"{bitrate_k}k"]
    else:
        codec = "libmp3lame"
        bitrate_arg = ["-b:a", f"{bitrate_k}k"]
    cmd = [
        "ffmpeg", "-y", "-i", str(video_path),
        "-vn", "-acodec", codec,
        *bitrate_arg,
        "-ar", "44100",
        "-ac", "2",
        str(out_path),
    ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600)
    return out_path
