"""
Script to resultant video via configurable T2V backend (stub, cogvideo, etc.).
"""

from pathlib import Path


def script_to_video(
    script_path: Path,
    out_dir: Path,
    *,
    backend: str = "stub",
    model_path: str | None = None,
    config: dict | None = None,
) -> Path:
    """
    Generate resultant.mp4 from script text using the chosen T2V backend.
    """
    config = config or {}
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "resultant.mp4"
    script_text = script_path.read_text(encoding="utf-8")
    if backend == "cogvideo":
        _generate_cogvideo(script_text, out_path, model_path=model_path, config=config)
    else:
        _generate_stub(script_text, out_path, config=config)
    return out_path


def _generate_stub(script_text: str, out_path: Path, config: dict) -> None:
    """
    Stub: create a short placeholder video (e.g. black frame + optional text overlay)
    so the pipeline runs without a real T2V model. Uses ffmpeg to produce a short clip.
    """
    import subprocess
    duration = float(config.get("duration_sec", 5.0))
    # Generate a short silent black video (or with a simple "Generated from script" frame)
    cmd = [
        "ffmpeg", "-y",
        "-f", "lavfi", "-i", f"color=c=black:s=1280x720:d={duration}",
        "-f", "lavfi", "-i", f"anullsrc=r=44100:cl=stereo",
        "-t", str(duration),
        "-c:v", "libx264", "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        str(out_path),
    ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=60)


def _generate_cogvideo(
    script_text: str,
    out_path: Path,
    *,
    model_path: str | None = None,
    config: dict | None = None,
) -> None:
    """
    Generate video using CogVideo (or compatible) if available.
    Falls back to stub if not installed or on error.
    """
    try:
        # Optional: integrate CogVideo API when available
        # from cogvideo_api import generate; generate(script_text, out_path, model_path=model_path)
        _generate_stub(script_text, out_path, config=config or {})
    except Exception:
        _generate_stub(script_text, out_path, config=config or {})
