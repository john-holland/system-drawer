"""
Script to resultant video via configurable T2V backend (stub, cogvideox, etc.).
"""

import logging
from pathlib import Path
from typing import Callable

from .audio import _find_ffmpeg

log = logging.getLogger("video_storage_tool.script_to_video")


def _ensure_torchvision_interpolation_mode() -> None:
    """Patch torchvision.transforms with InterpolationMode if missing (e.g. Python 3.14 / older torchvision)."""
    import torchvision.transforms as _tvt
    if hasattr(_tvt, "InterpolationMode"):
        return
    try:
        import PIL.Image
        I = PIL.Image
        class InterpolationMode:
            NEAREST = I.NEAREST
            BILINEAR = I.BILINEAR
            BICUBIC = I.BICUBIC
            NEAREST_EXACT = getattr(I, "NEAREST_EXACT", I.NEAREST)
            BOX = getattr(I, "BOX", 4)
            HAMMING = getattr(I, "HAMMING", 5)
            LANCZOS = getattr(I, "LANCZOS", 1)
        _tvt.InterpolationMode = InterpolationMode
        log.debug("Patched torchvision.transforms.InterpolationMode from PIL")
    except Exception as e:
        log.debug("Could not patch InterpolationMode: %s", e)


# Patch once at import so transformers/diffusers see InterpolationMode (e.g. Python 3.14 / older torchvision)
_ensure_torchvision_interpolation_mode()

# Optional progress callback: (phase: str, progress: float, message: str) -> None
ProgressCallback = Callable[[str, float, str], None]


def script_to_video(
    script_path: Path,
    out_dir: Path,
    *,
    backend: str = "stub",
    model_path: str | None = None,
    model_id: str | None = None,
    config: dict | None = None,
    progress_callback: ProgressCallback | None = None,
    ffmpeg_path: str | Path | None = None,
) -> Path:
    """
    Generate resultant.mp4 from script text using the chosen T2V backend.
    """
    config = config or {}
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "resultant.mp4"
    script_text = script_path.read_text(encoding="utf-8")
    use_cogvideox = backend in ("cogvideox", "cogvideo")
    if use_cogvideox:
        _generate_cogvideo(
            script_text,
            out_path,
            model_path=model_path,
            model_id=model_id,
            config=config,
            progress_callback=progress_callback,
            ffmpeg_path=ffmpeg_path,
        )
    else:
        _generate_stub(script_text, out_path, config=config, ffmpeg_path=ffmpeg_path)
    return out_path


def _generate_stub(
    script_text: str,
    out_path: Path,
    config: dict,
    *,
    ffmpeg_path: str | Path | None = None,
) -> None:
    """
    Stub: create a short placeholder video (e.g. black frame + optional text overlay)
    so the pipeline runs without a real T2V model. Uses ffmpeg to produce a short clip.
    """
    import subprocess
    ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
    duration = float(config.get("duration_sec", 5.0))
    log.info("Generating stub (black placeholder) video, %.1fs — resultant will be black; diff will be ~100%% of original", duration)
    # Generate a short silent black video (or with a simple "Generated from script" frame)
    cmd = [
        ffmpeg_exe, "-y",
        "-f", "lavfi", "-i", f"color=c=black:s=1280x720:d={duration}",
        "-f", "lavfi", "-i", f"anullsrc=r=44100:cl=stereo",
        "-t", str(duration),
        "-c:v", "libx264", "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        str(out_path),
    ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=60)


def _report(progress_callback: ProgressCallback | None, phase: str, progress: float, message: str) -> None:
    if progress_callback:
        try:
            progress_callback(phase, progress, message)
        except Exception:
            pass


def _generate_cogvideo(
    script_text: str,
    out_path: Path,
    *,
    model_path: str | None = None,
    model_id: str | None = None,
    config: dict | None = None,
    progress_callback: ProgressCallback | None = None,
    ffmpeg_path: str | Path | None = None,
) -> None:
    """
    Generate video using CogVideoX (Diffusers) if available.
    Prefer model_id (Hub); else model_path (local). Falls back to stub if not installed or on error.
    """
    config = config or {}
    _ensure_torchvision_interpolation_mode()
    try:
        import torch
        from diffusers import CogVideoXPipeline
        from diffusers.utils import export_to_video
    except ImportError as e:
        _report(progress_callback, "cogvideox", 0, "diffusers/torch not installed")
        log.warning(
            "CogVideoX skipped: torch/diffusers not installed. "
            "Install with: pip install -r %s/requirements-t2v.txt (or: torch diffusers transformers accelerate)",
            Path(__file__).resolve().parent,
        )
        _generate_stub(script_text, out_path, config=config, ffmpeg_path=ffmpeg_path)
        return

    load_from = model_id or model_path
    if not load_from:
        _generate_stub(script_text, out_path, config=config, ffmpeg_path=ffmpeg_path)
        return

    try:
        _report(progress_callback, "downloading_model", 0.5, "Downloading model…" if model_id else "Loading model…")
        dev = (config.get("device") or "auto").lower()
        if dev == "cpu":
            use_cuda = False
        elif dev == "cuda":
            use_cuda = torch.cuda.is_available()
            if not use_cuda:
                log.warning("device=cuda requested but CUDA not available; using CPU")
        else:
            use_cuda = torch.cuda.is_available()
        actual_device = "cuda" if use_cuda else "cpu"
        log.info(
            "CogVideoX using device=%s (config=%s, torch.cuda.is_available()=%s). "
            "If you expected GPU but see cpu, install PyTorch with CUDA: pip install torch torchvision --index-url https://download.pytorch.org/whl/cu128",
            actual_device, dev, torch.cuda.is_available(),
        )
        # float16 not supported on CPU in PyTorch; use float32 when running on CPU
        dtype = torch.float16 if use_cuda else torch.float32
        pipe = CogVideoXPipeline.from_pretrained(
            load_from,
            torch_dtype=dtype,
        )
        _report(progress_callback, "model_loaded", 0.8, "Model loaded.")
        pipe = pipe.to("cuda" if use_cuda else "cpu")

        num_steps = int(config.get("num_inference_steps", 50))
        num_frames = config.get("num_frames")  # default from pipeline
        fps = int(config.get("fps", 8))
        _report(progress_callback, "generating", 0.85, "Generating video…")
        out = pipe(
            prompt=script_text[:2000],
            guidance_scale=float(config.get("guidance_scale", 6)),
            num_inference_steps=num_steps,
            num_frames=num_frames,
        )
        frames = out.frames[0]
        export_to_video(frames, str(out_path), fps=fps)
        _report(progress_callback, "done", 1.0, "Done.")
    except Exception as e:
        _report(progress_callback, "error", 0, str(e))
        _generate_stub(script_text, out_path, config=config, ffmpeg_path=ffmpeg_path)
