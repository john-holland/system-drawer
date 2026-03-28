"""
CLI entrypoint for video_storage_tool.
Store: --input video.mp4 or image --out dir/ -> audio + script + resultant video.
Reconstitute: --reconstitute --input stored_dir/ [--out reconstituted.mp4]
"""

import argparse
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable

from . import __version__
from .audio import extract_and_compress_audio, _find_ffmpeg
from .diff import compute_diff
from .media_utils import get_image_format, is_image_input
from .reconstitute import extract_first_frame, reconstitute
from .script_to_video import script_to_video
from .video_to_script import video_to_script


def _deep_merge(base: dict, override: dict) -> dict:
    """Recursively merge override into base. Override values take precedence."""
    out = dict(base)
    for key, val in override.items():
        if isinstance(val, dict) and isinstance(out.get(key), dict):
            out[key] = _deep_merge(out[key], val)
        else:
            out[key] = val
    return out


def _load_config(config_path: Path | None) -> dict:
    """Load YAML config. Merges default_config.yaml with user config. Returns empty dict if PyYAML missing."""
    try:
        import yaml
    except ImportError:
        return {}
    _package_dir = Path(__file__).resolve().parent
    default_path = _package_dir / "default_config.yaml"
    base: dict = {}
    if default_path.is_file():
        try:
            with open(default_path, "r", encoding="utf-8") as f:
                base = yaml.safe_load(f) or {}
        except Exception:
            base = {}
    if config_path is None or not config_path.exists():
        return base
    try:
        with open(config_path, "r", encoding="utf-8") as f:
            override = yaml.safe_load(f) or {}
        return _deep_merge(base, override)
    except Exception:
        return base


def _ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def _noop_progress(_phase: str, _progress: float, _message: str) -> None:
    pass


def _image_to_video(image_path: Path, out_path: Path, ffmpeg_path: str | None = None) -> Path:
    """Convert an image to a short video (1 frame, silent) for pipeline processing."""
    ffmpeg = _find_ffmpeg(ffmpeg_path)
    subprocess.run(
        [
            ffmpeg,
            "-y",
            "-loop", "1",
            "-i", str(image_path),
            "-c:v", "libx264",
            "-t", "1",
            "-pix_fmt", "yuv420p",
            "-an",
            str(out_path),
        ],
        check=True,
        capture_output=True,
    )
    return out_path


def run_store(
    input_video: Path,
    out_dir: Path,
    *,
    audio_format: str = "aac",
    audio_max_mb: float = 5.0,
    t2v_backend: str = "stub",
    t2v_model_path: str | None = None,
    t2v_model_id: str | None = None,
    script_backend: str = "whisper",
    config: dict,
    progress_callback: Callable[[str, float, str], None] | None = None,
    force_script: bool = False,
    input_image_format: str | None = None,
    source_image: Path | None = None,
) -> None:
    """Run the store pipeline: extract audio, video→script, script→resultant video.
    Each step writes its output to out_dir immediately. If a step's output already
    exists (e.g. after a crash or when retrying), that step is skipped to save time.
    If force_script is True, script.txt and resultant.mp4 are removed first so script
    (and T2V) are regenerated.
    """
    cb = progress_callback or _noop_progress
    _ensure_dir(out_dir)
    script_path = out_dir / "script.txt"
    resultant_path = out_dir / "resultant.mp4"
    if force_script:
        if script_path.is_file():
            script_path.unlink()
        if resultant_path.is_file():
            resultant_path.unlink()
    store_cfg = config.get("store", {})
    loss_coef = float(store_cfg.get("loss_coefficient", 0.0))
    audio_cfg = config.get("audio", {})
    audio_fmt = audio_cfg.get("format", audio_format)
    if loss_coef == 0 and audio_fmt not in ("flac",):
        audio_fmt = "flac"
    ext = "flac" if audio_fmt == "flac" else ("aac" if audio_fmt == "aac" else "mp3")
    audio_path = out_dir / f"audio.{ext}"
    if not audio_path.is_file():
        cb("extracting_audio", 0.1, "Extracting audio…")
        audio_path = extract_and_compress_audio(
            input_video,
            out_dir,
            format=audio_fmt,
            max_mb=audio_cfg.get("max_mb", audio_max_mb),
            ffmpeg_path=audio_cfg.get("ffmpeg_path"),
        )
        cb("extracting_audio", 0.2, "Audio done.")
    else:
        cb("extracting_audio", 0.2, "Audio cached, skipping.")
    # Step 2: video (or audio) to script (skip if script.txt exists)
    if not script_path.is_file():
        cb("transcribing", 0.25, "Transcribing…")
        script_cfg = config.copy()
        if input_image_format is not None:
            script_cfg["input_image_format"] = input_image_format
        script_path = video_to_script(
            input_video,
            audio_path,
            out_dir,
            backend=config.get("script", {}).get("backend", script_backend),
            config=script_cfg,
            progress_callback=progress_callback,
        )
        cb("transcribing", 0.4, "Script done.")
    else:
        cb("transcribing", 0.4, "Script cached, skipping.")
    # Step 3: script to resultant video (skip if resultant.mp4 exists — saves T2V time after crash)
    if not resultant_path.is_file():
        t2v_cfg = config.get("t2v", {})
        script_to_video(
            script_path,
            out_dir,
            backend=t2v_cfg.get("backend", t2v_backend),
            model_path=t2v_cfg.get("model_path") or t2v_model_path,
            model_id=t2v_cfg.get("model_id") or t2v_model_id,
            config=t2v_cfg,
            progress_callback=cb,
            ffmpeg_path=config.get("audio", {}).get("ffmpeg_path"),
        )
    else:
        cb("cogvideox", 1.0, "Resultant cached, skipping.")
    # Step 4: optional diff (original - resultant); run if resultant exists (idempotent)
    diff_config = config.get("diff", {})
    diff_lossless = diff_config.get("lossless", loss_coef == 0)
    diff_path = compute_diff(
        input_video,
        resultant_path,
        out_dir,
        enabled=diff_config.get("enabled", True),
        quality=diff_config.get("quality", 6),
        lossless=diff_lossless,
        ffmpeg_path=config.get("audio", {}).get("ffmpeg_path"),
    )
    _write_manifest(
        out_dir,
        input_video,
        audio_path,
        script_path,
        diff_path,
        loss_coefficient=loss_coef,
        input_image_format=input_image_format,
        source_image=source_image,
    )


def _check_cache(config: dict) -> None:
    """Print Hugging Face / T2V cache locations and whether the configured model is present."""
    import os
    default_hf_home = os.path.join(os.path.expanduser("~"), ".cache", "huggingface")
    hf_home = os.environ.get("HF_HOME") or default_hf_home
    hub_dir = os.environ.get("HF_HUB_CACHE") or os.path.join(hf_home, "hub")
    print("Hugging Face cache locations:", file=sys.stderr)
    print(f"  HF_HOME / HF_HUB_CACHE env: {os.environ.get('HF_HOME') or '(not set)'} / {os.environ.get('HF_HUB_CACHE') or '(not set)'}", file=sys.stderr)
    print(f"  Resolved hub directory:     {hub_dir}", file=sys.stderr)
    hub_path = Path(hub_dir)
    if not hub_path.is_dir():
        print(f"  -> Hub directory does not exist (no models downloaded yet).", file=sys.stderr)
    else:
        dirs = [d for d in hub_path.iterdir() if d.is_dir()]
        model_like = [d.name for d in dirs if "cogvideo" in d.name.lower() or "THUDM" in d.name]
        print(f"  -> Hub exists; {len(dirs)} top-level entries.", file=sys.stderr)
        if model_like:
            print(f"  -> T2V-related: {model_like}", file=sys.stderr)
        else:
            print("  -> No CogVideoX/THUDM model folders found in hub.", file=sys.stderr)
    t2v_cfg = config.get("t2v", {})
    model_id = t2v_cfg.get("model_id") or ""
    model_path = t2v_cfg.get("model_path")
    if model_path:
        p = Path(model_path)
        print(f"\nConfigured t2v.model_path: {model_path}", file=sys.stderr)
        print(f"  -> Exists: {p.exists()}, is_dir: {p.is_dir() if p.exists() else 'N/A'}", file=sys.stderr)
    if model_id:
        print(f"\nConfigured t2v.model_id: {model_id}", file=sys.stderr)
        # Hugging Face hub stores as models--org--name
        slug = "models--" + model_id.replace("/", "--")
        expected = hub_path / slug
        if expected.is_dir():
            snapshots = expected / "snapshots"
            revs = list(snapshots.iterdir()) if snapshots.is_dir() else []
            print(f"  -> Cached: yes ({expected})", file=sys.stderr)
            if revs:
                print(f"  -> Revisions: {[r.name for r in revs]}", file=sys.stderr)
        else:
            print(f"  -> Cached: no (expected path {expected} not found)", file=sys.stderr)
    try:
        from huggingface_hub import scan_cache_dir
        cache = scan_cache_dir()
        cache_dir = getattr(cache, "cache_dir", getattr(cache, "cache_path", str(cache)))
        print(f"\nhuggingface_hub.scan_cache_dir(): {cache_dir}", file=sys.stderr)
        for repo in cache.repos:
            if "cogvideo" in repo.repo_id.lower() or "THUDM" in repo.repo_id:
                print(f"  Repo: {repo.repo_id} size={repo.size_on_disk_str} refs={list(repo.refs)}", file=sys.stderr)
    except ImportError:
        print("\nInstall 'huggingface_hub' for detailed cache scan (pip install huggingface_hub).", file=sys.stderr)
    except Exception as e:
        print(f"\nCache scan error: {e}", file=sys.stderr)


def _write_manifest(
    out_dir: Path,
    original_video: Path,
    audio_path: Path,
    script_path: Path,
    diff_path: Path | None = None,
    *,
    loss_coefficient: float = 0.0,
    input_image_format: str | None = None,
    source_image: Path | None = None,
) -> None:
    import json
    resultant = out_dir / "resultant.mp4"
    manifest = {
        "original_video": str(original_video),
        "audio": str(audio_path),
        "script": str(script_path),
        "resultant_video": str(resultant) if resultant.exists() else None,
        "diff_video": str(diff_path) if diff_path and diff_path.exists() else None,
        "loss_coefficient": loss_coefficient,
    }
    if input_image_format is not None:
        manifest["input_image_format"] = input_image_format
    if source_image is not None and source_image.exists():
        manifest["source_image"] = str(source_image)
    with open(out_dir / "manifest.json", "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Store large video as audio + script + T2V resultant video, or reconstitute from stored dir."
    )
    parser.add_argument("--input", "-i", type=Path, default=None, help="Input video file or (if reconstitute) stored directory")
    parser.add_argument("--out", "-o", type=Path, default=None, help="Output directory (store) or output file (reconstitute). Default reconstitute: stored_dir/reconstituted.mp4")
    parser.add_argument("--reconstitute", action="store_true", help="Reconstitute: merge stored resultant video + audio into one file")
    parser.add_argument("--audio-format", choices=("aac", "mp3", "flac"), default="aac", help="Audio codec for stored audio")
    parser.add_argument("--audio-max-mb", type=float, default=5.0, help="Target max size for extracted audio (MB)")
    parser.add_argument("--t2v-backend", default="stub", help="T2V backend: stub, cogvideo, or other (see config)")
    parser.add_argument("--t2v-model-path", type=str, default=None, help="Path to T2V model (overrides config)")
    parser.add_argument("--t2v-model-id", type=str, default=None, help="Hub model id for T2V (e.g. THUDM/CogVideoX-2b, overrides config)")
    parser.add_argument("--script-backend", default="whisper", help="Script backend: whisper, stub")
    parser.add_argument("--config", type=Path, default=None, help="Optional config.yaml path")
    parser.add_argument("--original", action="store_true", help="Reconstitute with diff to recover original-quality video (if diff artifact is present)")
    parser.add_argument(
        "--loop-strategy",
        choices=("loop", "hold"),
        default=None,
        help="Reconstitution behavior when video is shorter than audio: loop or hold last frame (default from config: reconstitute.loop_strategy or loop).",
    )
    parser.add_argument(
        "--trim-audio",
        action="store_true",
        help="Trim output duration to available video duration instead of extending video to audio duration.",
    )
    parser.add_argument("--extract-frame", action="store_true", help="Extract first frame as image (use with --reconstitute). Output format matches input_image_format from manifest or png")
    parser.add_argument("--check-cache", action="store_true", help="Print Hugging Face / T2V cache locations and whether configured model is present; then exit")
    parser.add_argument("--version", action="version", version=f"%(prog)s {__version__}")
    args = parser.parse_args()

    config_path = args.config or (Path(__file__).resolve().parent / "config.yaml")
    config = _load_config(config_path)

    if args.check_cache:
        # Merge settings.json (UI-saved) so we check the model the user actually configured
        settings_path = Path(__file__).resolve().parent / "settings.json"
        if settings_path.exists():
            try:
                import json
                with open(settings_path, "r", encoding="utf-8") as f:
                    settings = json.load(f)
                for key, val in (settings or {}).items():
                    if isinstance(val, dict) and key in config and isinstance(config[key], dict):
                        config[key] = {**config[key], **val}
                    else:
                        config[key] = val
            except Exception:
                pass
        _check_cache(config)
        return 0

    if args.reconstitute:
        if not args.input:
            parser.error("--input required (stored directory)")
        stored_dir = args.input
        if args.extract_frame:
            out_file = args.out or (stored_dir / "reconstituted.png")
            extract_first_frame(
                stored_dir,
                out_file,
                ffmpeg_path=config.get("audio", {}).get("ffmpeg_path"),
            )
            print(f"Extracted first frame: {out_file}")
        else:
            out_file = args.out or (stored_dir / "reconstituted.mp4")
            recon_cfg = config.get("reconstitute", {}) if isinstance(config, dict) else {}
            loop_strategy = args.loop_strategy or recon_cfg.get("loop_strategy", "loop")
            trim_audio = bool(args.trim_audio or recon_cfg.get("trim_audio", False))
            reconstitute(
                stored_dir,
                out_file,
                use_diff=args.original,
                loop_strategy=loop_strategy,
                trim_audio=trim_audio,
                ffmpeg_path=config.get("audio", {}).get("ffmpeg_path"),
            )
            print(f"Reconstituted: {out_file}")
        return 0

    # Store mode
    if not args.input:
        parser.error("--input required (video file)")
    if not args.input.is_file():
        print(f"Error: input is not a file: {args.input}", file=sys.stderr)
        return 1
    out_dir = args.out
    if out_dir is None:
        out_dir = args.input.parent / (args.input.stem + "_stored")
    _ensure_dir(out_dir)

    input_image_format = None
    source_image_path = None
    input_video = args.input
    temp_video = None

    if is_image_input(args.input):
        input_image_format = get_image_format(args.input)
        ext = "jpg" if input_image_format == "jpeg" else input_image_format
        source_image_path = out_dir / f"source_image.{ext}"
        shutil.copy2(args.input, source_image_path)
        ffmpeg_path = config.get("audio", {}).get("ffmpeg_path")
        temp_video = Path(tempfile.mktemp(suffix=".mp4"))
        try:
            input_video = _image_to_video(args.input, temp_video, ffmpeg_path)
        except Exception as e:
            if temp_video.exists():
                temp_video.unlink(missing_ok=True)
            print(f"Error converting image to video: {e}", file=sys.stderr)
            return 1

    t2v_cfg = config.get("t2v", {})
    try:
        run_store(
            input_video,
            out_dir,
            audio_format=args.audio_format,
            audio_max_mb=args.audio_max_mb,
            t2v_backend=args.t2v_backend,
            t2v_model_path=args.t2v_model_path,
            t2v_model_id=args.t2v_model_id or t2v_cfg.get("model_id"),
            script_backend=args.script_backend,
            config=config,
            input_image_format=input_image_format,
            source_image=source_image_path,
        )
    finally:
        if temp_video is not None and temp_video.exists():
            temp_video.unlink(missing_ok=True)

    print(f"Stored artifacts in: {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
