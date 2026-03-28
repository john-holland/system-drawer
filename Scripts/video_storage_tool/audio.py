"""
Extract and compress audio from a video file to a target size (few MB) using ffmpeg.
"""

import shutil
import subprocess
import sys
from pathlib import Path


def _subprocess_kwargs() -> dict:
    """Kwargs for subprocess.run to avoid WinError 50 in some environments (e.g. Cursor)."""
    kwargs: dict = {}
    if sys.platform == "win32":
        # CREATE_NO_WINDOW avoids handle inheritance issues that cause OSError 50
        if hasattr(subprocess, "CREATE_NO_WINDOW"):
            kwargs["creationflags"] = subprocess.CREATE_NO_WINDOW
    return kwargs

FFMPEG_NOT_FOUND = (
    "ffmpeg not found. Install ffmpeg and add it to your PATH "
    "(https://ffmpeg.org/download.html). On Windows, add the folder containing ffmpeg.exe to PATH."
)


def _find_ffmpeg(ffmpeg_path: str | Path | None = None) -> str:
    """Return path to ffmpeg executable. Raises FileNotFoundError if not found."""
    if ffmpeg_path:
        p = Path(ffmpeg_path)
        if p.is_file():
            return str(p)
        if p.is_dir():
            exe = p / ("ffmpeg.exe" if sys.platform == "win32" else "ffmpeg")
            if exe.is_file():
                return str(exe)
    exe = shutil.which("ffmpeg")
    if not exe:
        raise FileNotFoundError(FFMPEG_NOT_FOUND)
    return exe


def _find_ffprobe(ffprobe_path: str | Path | None = None) -> str:
    """Return path to ffprobe executable. Falls back to ffprobe on PATH."""
    if ffprobe_path:
        p = Path(ffprobe_path)
        if p.is_file():
            return str(p)
        if p.is_dir():
            exe = p / ("ffprobe.exe" if sys.platform == "win32" else "ffprobe")
            if exe.is_file():
                return str(exe)
    exe = shutil.which("ffprobe")
    return exe or "ffprobe"


def get_video_duration_seconds(
    video_path: Path,
    ffprobe_path: str | Path | None = None,
) -> float:
    """Probe duration with ffprobe. Returns 0.0 on failure."""
    ffprobe_exe = _find_ffprobe(ffprobe_path)
    try:
        out = subprocess.run(
            [
                ffprobe_exe,
                "-v", "error",
                "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1",
                str(video_path),
            ],
            capture_output=True,
            text=True,
            check=True,
            timeout=60,
            **_subprocess_kwargs(),
        )
        return float(out.stdout.strip() or 0)
    except (subprocess.CalledProcessError, ValueError, FileNotFoundError, OSError):
        return 0.0


def extract_and_compress_audio(
    video_path: Path,
    out_dir: Path,
    *,
    format: str = "aac",
    max_mb: float = 5.0,
    ffmpeg_path: str | Path | None = None,
) -> Path:
    """
    Extract audio from video and encode. FLAC is lossless; AAC/MP3 use bitrate from max_mb.
    """
    ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    if format == "flac":
        out_path = out_dir / "audio.flac"
        cmd = [
            ffmpeg_exe, "-y", "-i", str(video_path),
            "-vn", "-acodec", "flac",
            "-ar", "44100",
            "-ac", "2",
            str(out_path),
        ]
    else:
        duration = get_video_duration_seconds(video_path, ffprobe_path=ffmpeg_path)
        if duration > 0:
            target_bits = max_mb * 1e6 * 8
            bitrate_k = int(target_bits / duration / 1000)
            bitrate_k = max(32, min(320, bitrate_k))
        else:
            bitrate_k = 128
        ext = "aac" if format == "aac" else "mp3"
        out_path = out_dir / f"audio.{ext}"
        if format == "aac":
            codec = "aac"
            bitrate_arg = ["-b:a", f"{bitrate_k}k"]
        else:
            codec = "libmp3lame"
            bitrate_arg = ["-b:a", f"{bitrate_k}k"]
        cmd = [
            ffmpeg_exe, "-y", "-i", str(video_path),
            "-vn", "-acodec", codec,
            *bitrate_arg,
            "-ar", "44100",
            "-ac", "2",
            str(out_path),
        ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600, **_subprocess_kwargs())
    return out_path
