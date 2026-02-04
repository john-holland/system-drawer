"""
Video (or extracted audio) to text script: Whisper ASR or stub.
"""

from pathlib import Path


def video_to_script(
    video_path: Path,
    audio_path: Path,
    out_dir: Path,
    *,
    backend: str = "whisper",
    config: dict | None = None,
) -> Path:
    """
    Produce script.txt from video/audio. Uses Whisper if available, else stub.
    """
    config = config or {}
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    script_path = out_dir / "script.txt"
    if backend == "whisper":
        text = _transcribe_whisper(audio_path, config)
    else:
        text = _stub_script(video_path, config)
    script_path.write_text(text, encoding="utf-8")
    return script_path


def _transcribe_whisper(audio_path: Path, config: dict) -> str:
    """Run Whisper on audio file. Falls back to stub if whisper not installed."""
    try:
        import whisper
        model_name = config.get("model", "base")
        model = whisper.load_model(model_name)
        result = model.transcribe(str(audio_path), fp16=False)
        return (result.get("text") or "").strip() or "(no speech detected)"
    except ImportError:
        return _stub_script(audio_path, config)
    except Exception:
        return _stub_script(audio_path, config)


def _stub_script(media_path: Path, config: dict) -> str:
    """Placeholder script when no ASR/captioning is available."""
    return f"[Script placeholder for: {media_path.name}. Install openai-whisper and use --script-backend whisper for transcription.]"
