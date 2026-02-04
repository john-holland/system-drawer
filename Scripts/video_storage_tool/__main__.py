"""
CLI entrypoint for video_storage_tool.
Store: --input video.mp4 --out dir/ → audio + script + resultant video.
Reconstitute: --reconstitute --input stored_dir/ [--out reconstituted.mp4]
"""

import argparse
import sys
from pathlib import Path

from . import __version__
from .audio import extract_and_compress_audio
from .reconstitute import reconstitute
from .script_to_video import script_to_video
from .video_to_script import video_to_script


def _load_config(config_path: Path | None) -> dict:
    """Load optional YAML config. Returns empty dict if no config or PyYAML missing."""
    if config_path is None or not config_path.exists():
        return {}
    try:
        import yaml
        with open(config_path, "r", encoding="utf-8") as f:
            return yaml.safe_load(f) or {}
    except ImportError:
        return {}
    except Exception:
        return {}


def _ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def run_store(
    input_video: Path,
    out_dir: Path,
    *,
    audio_format: str = "aac",
    audio_max_mb: float = 5.0,
    t2v_backend: str = "stub",
    t2v_model_path: str | None = None,
    script_backend: str = "whisper",
    config: dict,
) -> None:
    """Run the store pipeline: extract audio, video→script, script→resultant video."""
    _ensure_dir(out_dir)
    # Step 1: extract and compress audio
    audio_path = extract_and_compress_audio(
        input_video,
        out_dir,
        format=config.get("audio", {}).get("format", audio_format),
        max_mb=config.get("audio", {}).get("max_mb", audio_max_mb),
    )
    # Step 2: video (or audio) to script
    script_path = video_to_script(
        input_video,
        audio_path,
        out_dir,
        backend=config.get("script", {}).get("backend", script_backend),
        config=config.get("script", {}),
    )
    # Step 3: script to resultant video
    script_to_video(
        script_path,
        out_dir,
        backend=config.get("t2v", {}).get("backend", t2v_backend),
        model_path=config.get("t2v", {}).get("model_path") or t2v_model_path,
        config=config.get("t2v", {}),
    )
    # Optional: write manifest
    _write_manifest(out_dir, input_video, audio_path, script_path)


def _write_manifest(
    out_dir: Path,
    original_video: Path,
    audio_path: Path,
    script_path: Path,
) -> None:
    import json
    resultant = out_dir / "resultant.mp4"
    manifest = {
        "original_video": str(original_video),
        "audio": str(audio_path),
        "script": str(script_path),
        "resultant_video": str(resultant) if resultant.exists() else None,
    }
    with open(out_dir / "manifest.json", "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Store large video as audio + script + T2V resultant video, or reconstitute from stored dir."
    )
    parser.add_argument("--input", "-i", type=Path, required=True, help="Input video file or (if reconstitute) stored directory")
    parser.add_argument("--out", "-o", type=Path, default=None, help="Output directory (store) or output file (reconstitute). Default reconstitute: stored_dir/reconstituted.mp4")
    parser.add_argument("--reconstitute", action="store_true", help="Reconstitute: merge stored resultant video + audio into one file")
    parser.add_argument("--audio-format", choices=("aac", "mp3"), default="aac", help="Audio codec for stored audio")
    parser.add_argument("--audio-max-mb", type=float, default=5.0, help="Target max size for extracted audio (MB)")
    parser.add_argument("--t2v-backend", default="stub", help="T2V backend: stub, cogvideo, or other (see config)")
    parser.add_argument("--t2v-model-path", type=str, default=None, help="Path to T2V model (overrides config)")
    parser.add_argument("--script-backend", default="whisper", help="Script backend: whisper, stub")
    parser.add_argument("--config", type=Path, default=None, help="Optional config.yaml path")
    parser.add_argument("--version", action="version", version=f"%(prog)s {__version__}")
    args = parser.parse_args()

    config_path = args.config or (Path(__file__).resolve().parent / "config.yaml")
    config = _load_config(config_path)

    if args.reconstitute:
        stored_dir = args.input
        out_file = args.out or (stored_dir / "reconstituted.mp4")
        reconstitute(stored_dir, out_file)
        return 0

    # Store mode
    if not args.input.is_file():
        print(f"Error: input is not a file: {args.input}", file=sys.stderr)
        return 1
    out_dir = args.out
    if out_dir is None:
        out_dir = args.input.parent / (args.input.stem + "_stored")
    run_store(
        args.input,
        out_dir,
        audio_format=args.audio_format,
        audio_max_mb=args.audio_max_mb,
        t2v_backend=args.t2v_backend,
        t2v_model_path=args.t2v_model_path,
        script_backend=args.script_backend,
        config=config,
    )
    print(f"Stored artifacts in: {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
