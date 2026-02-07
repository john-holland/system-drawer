"""
Video (or extracted audio) to text script: Whisper ASR + optional visual description.
Whisper uses ffmpeg to decode audio; if ffmpeg is not on PATH, set config audio.ffmpeg_path.
Visual description samples frames and captions them with BLIP/BLIP2 (optional).
"""

import logging
import os
import subprocess
import tempfile
from pathlib import Path
from typing import Any, Callable

log = logging.getLogger(__name__)

SCRIPT_SECTION_PREFACE = "[Preface]"
SCRIPT_SECTION_TRANSCRIPT = "[Transcript]"
SCRIPT_SECTION_VISUAL = "[Visual description]"
SCRIPT_SECTION_COLOR_GRADIENT = "[Color gradient]"


def video_to_script(
    video_path: Path,
    audio_path: Path,
    out_dir: Path,
    *,
    backend: str = "whisper",
    config: dict | None = None,
    progress_callback: Callable[[str, float, str], None] | None = None,
) -> Path:
    """
    Produce script.txt from video/audio: Whisper transcript plus optional exhaustive
    visual description (frame-by-frame captions via BLIP/BLIP2).
    """
    config = config or {}
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    script_path = out_dir / "script.txt"
    cb = progress_callback or (lambda _p, _v, _m: None)

    if backend == "whisper":
        transcript = _transcribe_whisper(audio_path, config)
    else:
        transcript = _stub_script(video_path, config)

    script_cfg = config.get("script") or {}
    parts: list[str] = [f"{SCRIPT_SECTION_TRANSCRIPT}\n{transcript}"]

    visual_backend = script_cfg.get("visual_backend", "none")
    if visual_backend and visual_backend != "none":
        cb("visual", 0.35, "Describing frames…")
        visual, gradient, style_comment = _describe_video_frames(video_path, config, progress_callback=cb)
        if script_cfg.get("style_preface", True) and style_comment:
            parts.insert(0, f"{SCRIPT_SECTION_PREFACE}\n{style_comment}")
        if visual:
            parts.append(f"{SCRIPT_SECTION_VISUAL}\n{visual}")
        if gradient:
            parts.append(f"{SCRIPT_SECTION_COLOR_GRADIENT}\n{gradient}")
        cb("visual", 0.4, "Visual description done.")

    content = "\n\n".join(parts)
    # Atomic write: write to temp then rename, so we don't leave a half-written file
    tmp_path = script_path.with_suffix(".txt.tmp")
    try:
        tmp_path.write_text(content, encoding="utf-8")
        try:
            tmp_path.replace(script_path)
        except OSError:
            # e.g. target open on Windows; fall back to direct write
            script_path.write_text(content, encoding="utf-8")
            try:
                if tmp_path.is_file():
                    tmp_path.unlink()
            except OSError:
                pass
    except Exception as e:
        log.exception("Failed to write script.txt: %s", e)
        raise
    return script_path


def _transcribe_whisper(audio_path: Path, config: dict) -> str:
    """Run Whisper on audio file. Falls back to stub if whisper not installed or on error."""
    try:
        # Whisper uses ffmpeg to decode audio; add config ffmpeg dir to PATH if set (Windows often needs this)
        ffmpeg_path = (config.get("audio") or {}).get("ffmpeg_path")
        if ffmpeg_path:
            try:
                from .audio import _find_ffmpeg
                ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
                ffmpeg_dir = str(Path(ffmpeg_exe).resolve().parent)
                path_env = os.environ.get("PATH", "")
                if ffmpeg_dir not in path_env.split(os.pathsep):
                    os.environ["PATH"] = ffmpeg_dir + os.pathsep + path_env
            except Exception as e:
                log.warning("Could not add ffmpeg to PATH for Whisper: %s", e)

        import whisper
        model_name = (config.get("script") or {}).get("model", "base")
        model = whisper.load_model(model_name)
        result = model.transcribe(str(audio_path), fp16=False)
        return (result.get("text") or "").strip() or "(no speech detected)"
    except ImportError:
        return _stub_script(audio_path, config)
    except Exception as e:
        log.exception("Whisper transcription failed: %s", e)
        return (
            f"[Whisper failed for {audio_path.name}: {e!s}. "
            "Check logs; install openai-whisper if missing, or try a different audio file.]"
        )


def _stub_script(media_path: Path, config: dict) -> str:
    """Placeholder script when no ASR/captioning is available."""
    return f"[Script placeholder for: {media_path.name}. Install openai-whisper and use --script-backend whisper for transcription.]"


def _describe_video_frames(
    video_path: Path,
    config: dict,
    *,
    progress_callback: Callable[[str, float, str], None] | None = None,
) -> tuple[str, str, str]:
    """
    Sample frames from the input video and describe each with a vision model (BLIP/BLIP2).
    Also computes an overall color gradient per frame for diff matching.
    video_path must be the original input video file.
    Returns (visual_description_text, color_gradient_text, style_preface_comment).
    """
    video_path = Path(video_path).resolve()
    if not video_path.is_file():
        log.warning("Visual description skipped: input video not found: %s", video_path)
        return "", "", ""

    script_cfg = config.get("script") or {}
    interval_sec = float(script_cfg.get("visual_interval_sec", 1.0))
    max_frames = int(script_cfg.get("visual_max_frames", 60))
    model_id = script_cfg.get("visual_model") or (
        "Salesforce/blip2-opt-2.7b" if script_cfg.get("visual_backend") == "blip2"
        else "Salesforce/blip-image-captioning-base"
    )

    try:
        from PIL import Image
        import torch
    except ImportError as e:
        log.warning("Visual description skipped (torch/PIL not installed): %s", e)
        return "", "", ""

    ffmpeg_path = (config.get("audio") or {}).get("ffmpeg_path")
    try:
        from .audio import _find_ffmpeg, _find_ffprobe
        ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
        ffprobe_exe = _find_ffprobe(ffmpeg_path)
    except FileNotFoundError as e:
        log.warning("Visual description skipped (ffmpeg not found): %s", e)
        return "", "", ""

    # Get duration for timestamps
    try:
        out = subprocess.run(
            [ffprobe_exe, "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", str(video_path)],
            capture_output=True,
            text=True,
            check=True,
            timeout=30,
        )
        duration_sec = float(out.stdout.strip() or 0)
    except (subprocess.CalledProcessError, ValueError, subprocess.TimeoutExpired) as e:
        log.warning("Could not get video duration for visual description: %s", e)
        duration_sec = 0

    with tempfile.TemporaryDirectory(prefix="video_storage_visual_") as tmpdir:
        tmp = Path(tmpdir)
        # Extract frames: fps = 1/interval_sec, cap count with -frames:v
        fps = 1.0 / interval_sec if interval_sec > 0 else 1.0
        try:
            subprocess.run(
                [
                    ffmpeg_exe, "-y", "-i", str(video_path),
                    "-vf", f"fps={fps}", "-vsync", "0",
                    "-frames:v", str(max_frames),
                    str(tmp / "frame_%04d.png"),
                ],
                capture_output=True,
                check=True,
                timeout=600,
            )
        except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as e:
            log.warning("Frame extraction failed for visual description (input video %s): %s", video_path.name, e)
            return "", "", ""

        frames = sorted(tmp.glob("frame_*.png"))
        if not frames:
            log.warning("No frames extracted from input video for visual description.")
            return "", "", ""

        # Use BlipProcessor + BlipForConditionalGeneration (avoids pipeline task name changes)
        processor, model = _load_visual_caption_model(model_id, script_cfg.get("visual_backend"))
        if processor is None or model is None:
            return "", "", ""

        dev = (config.get("device") or "auto").lower()
        if dev == "cpu":
            device = "cpu"
        elif dev == "cuda":
            device = "cuda" if torch.cuda.is_available() else "cpu"
        else:
            device = "cuda" if torch.cuda.is_available() else "cpu"
        model = model.to(device)

        grid_size = int(script_cfg.get("visual_grid", 2) or 2)  # 2 = 2x2, 3 = 3x3; 1 = whole only
        grid_size = max(1, min(3, grid_size))

        # Style preface: one conditional caption on a representative frame (middle)
        style_comment = ""
        style_prompt = (script_cfg.get("style_prompt") or "The visual style of this scene is").strip()
        if style_prompt:
            try:
                mid_idx = len(frames) // 2
                style_img = Image.open(frames[mid_idx]).convert("RGB")
                style_comment = _caption_image_blip_conditional(
                    processor, model, style_img, device, style_prompt
                )
            except Exception as e:
                log.debug("Style preface skipped: %s", e)

        n = len(frames)
        lines = []
        gradient_lines = []
        for i, f in enumerate(frames):
            t_sec = i * interval_sec
            if t_sec > duration_sec and duration_sec > 0:
                break
            if progress_callback:
                progress_callback("visual", 0.35 + 0.05 * (i / max(n, 1)), f"Frame {i + 1}/{n}…")
            try:
                img = Image.open(f).convert("RGB")
                caption = _exhaustive_frame_description(
                    processor, model, img, device,
                    grid_size=grid_size,
                )
                if caption:
                    lines.append(f"{t_sec:.1f}s: {caption}")
                grad = _compute_color_gradient(img)
                if grad:
                    gradient_lines.append(f"{t_sec:.1f}s: {grad}")
            except Exception as e:
                log.debug("Frame %s caption failed: %s", f.name, e)
        visual_text = "\n".join(lines) if lines else ""
        gradient_text = "\n".join(gradient_lines) if gradient_lines else ""
        return visual_text, gradient_text, style_comment


def _grid_position_labels(rows: int, cols: int) -> list[str]:
    """Return prepositional labels for grid cells in row-major order (e.g. top-left, top-right, …)."""
    labels = []
    for r in range(rows):
        for c in range(cols):
            if rows == 1 and cols == 1:
                labels.append("In the center")
            else:
                rn = "top" if r == 0 else ("bottom" if r == rows - 1 else "middle")
                cn = "center" if cols == 1 else ("left" if c == 0 else ("right" if c == cols - 1 else "center"))
                if rows == 1:
                    labels.append(f"To the {cn}")
                elif cols == 1:
                    labels.append(f"At the {rn}")
                else:
                    labels.append(f"In the {rn}-{cn}")
    return labels


def _compute_color_gradient(pil_image) -> str:
    """
    Compute an overall color gradient (top / mid / bottom bands) for diff matching.
    Returns a string like "top=#RRGGBB mid=#RRGGBB bottom=#RRGGBB".
    """
    w, h = pil_image.size
    if w < 1 or h < 1:
        return ""
    pixels = pil_image.load()
    third = max(1, h // 3)
    bands = [
        (0, third),                    # top
        (third, 2 * third),            # mid
        (2 * third, h),                # bottom
    ]
    parts = []
    names = ["top", "mid", "bottom"]
    for (y0, y1), name in zip(bands, names):
        r_sum = g_sum = b_sum = 0
        n = 0
        step = max(1, min(w, h) // 32)  # sample to avoid huge iteration
        for y in range(y0, y1, step):
            for x in range(0, w, step):
                if y < h and x < w:
                    p = pixels[x, y]
                    r_sum += p[0]
                    g_sum += p[1]
                    b_sum += p[2]
                    n += 1
        if n == 0:
            continue
        r = int(round(r_sum / n))
        g = int(round(g_sum / n))
        b = int(round(b_sum / n))
        parts.append(f"{name}=#{r:02x}{g:02x}{b:02x}")
    return " ".join(parts) if parts else ""


def _chunk_image(pil_image, grid_rows: int, grid_cols: int) -> list[tuple[Any, tuple[int, int, int, int]]]:
    """Crop image into grid cells. Returns list of (PIL.Image crop, (left, top, right, bottom))."""
    w, h = pil_image.size
    crops = []
    for r in range(grid_rows):
        for c in range(grid_cols):
            left = int(c * w / grid_cols)
            top = int(r * h / grid_rows)
            right = int((c + 1) * w / grid_cols)
            bottom = int((r + 1) * h / grid_rows)
            crop = pil_image.crop((left, top, right, bottom))
            crops.append((crop, (left, top, right, bottom)))
    return crops


def _exhaustive_frame_description(processor, model, pil_image, device: str, *, grid_size: int = 2) -> str:
    """
    Build an exhaustive description: full-frame caption first, then grid regions with
    coordinates (row,col) and prepositional labels. grid_size 1 = whole only; 2 = 2x2; 3 = 3x3.
    """
    whole = _caption_image_blip(processor, model, pil_image, device)
    if grid_size <= 1:
        return whole or ""

    grid_rows = grid_cols = grid_size
    chunks = _chunk_image(pil_image, grid_rows, grid_cols)
    labels = _grid_position_labels(grid_rows, grid_cols)
    parts = [whole] if whole else []
    for idx, ((crop, _), label) in enumerate(zip(chunks, labels)):
        r, c = idx // grid_cols, idx % grid_cols
        coord = f"({r + 1},{c + 1})"  # 1-based (1,1) = top-left
        cap = _caption_image_blip(processor, model, crop, device)
        if cap:
            parts.append(f"{coord} {label}, {cap}")
    return " ".join(parts) if parts else ""


def _load_visual_caption_model(model_id: str, visual_backend: str | None):
    """Load BLIP or BLIP2 captioning model. Returns (processor, model) or (None, None)."""
    try:
        import torch
        from transformers import BlipProcessor, BlipForConditionalGeneration
    except ImportError as e:
        log.warning("Visual description skipped (transformers not installed): %s", e)
        return None, None

    if visual_backend == "blip2":
        try:
            from transformers import Blip2Processor, Blip2ForConditionalGeneration
            processor = Blip2Processor.from_pretrained(model_id)
            model = Blip2ForConditionalGeneration.from_pretrained(model_id)
            # float16 only when using CUDA (caller moves to device)
            return processor, model
        except Exception as e:
            log.warning("Could not load BLIP2 model %s: %s", model_id, e)
            return None, None

    try:
        processor = BlipProcessor.from_pretrained(model_id)
        model = BlipForConditionalGeneration.from_pretrained(model_id)
        return processor, model
    except Exception as e:
        err = str(e).lower()
        if "getaddrinfo" in err or "connection" in err or "closed" in err:
            log.warning("Visual model %s unavailable (network error or not cached). Run once with internet to cache.", model_id)
        else:
            log.warning("Could not load visual model %s: %s", model_id, e)
        return None, None


def _caption_image_blip(processor, model, pil_image, device: str) -> str:
    """Run BLIP/BLIP2 caption on one image. Returns caption text or empty string."""
    import torch
    inputs = processor(images=pil_image, return_tensors="pt")
    inputs = {k: v.to(device) for k, v in inputs.items()}
    with torch.no_grad():
        out = model.generate(**inputs, max_length=80)
    caption = processor.decode(out[0], skip_special_tokens=True).strip()
    return caption or ""


def _caption_image_blip_conditional(
    processor, model, pil_image, device: str, prompt: str
) -> str:
    """Run BLIP conditional caption: prompt + generated continuation (e.g. for style opinion)."""
    import torch
    # BLIP conditional: processor(images=..., text=..., return_tensors="pt")
    try:
        inputs = processor(images=pil_image, text=prompt, return_tensors="pt")
    except TypeError:
        # BLIP2 processor may not accept text; fall back to unconditional
        return _caption_image_blip(processor, model, pil_image, device)
    inputs = {k: v.to(device) for k, v in inputs.items()}
    with torch.no_grad():
        out = model.generate(**inputs, max_length=80)
    caption = processor.decode(out[0], skip_special_tokens=True).strip()
    return caption or ""
