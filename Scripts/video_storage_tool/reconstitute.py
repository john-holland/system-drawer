"""
Reconstitute: merge stored resultant video + audio into one output file (ffmpeg).
Handles duration mismatch by extending/looping the resultant video to match audio length.
Optional: apply diff for original-quality output (resultant + diff + audio).
Optional: --extract-frame to output the first frame as an image in the input format.
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
        diff_str = data.get("diff_video")
        if audio and resultant:
            ap, rp = Path(audio), Path(resultant)
            if ap.exists() and rp.exists():
                preferred_mkv = stored_dir / "diff.mkv"
                preferred_ogv = stored_dir / "diff.ogv"
                if diff_str and Path(diff_str).exists():
                    dp = Path(diff_str)
                    if preferred_mkv.exists() and dp.name.lower() != "diff.mkv":
                        dp = preferred_mkv
                else:
                    dp = preferred_mkv if preferred_mkv.exists() else (preferred_ogv if preferred_ogv.exists() else None)
                return ap, rp, dp
    # Discover by name
    audio_path = None
    for f in stored_dir.iterdir():
        if f.suffix.lower() in (".aac", ".mp3", ".flac") and f.name.lower().startswith("audio"):
            audio_path = f
            break
    resultant_path = stored_dir / "resultant.mp4"
    if not resultant_path.exists():
        resultant_path = None
    diff_path = stored_dir / "diff.mkv" if (stored_dir / "diff.mkv").exists() else stored_dir / "diff.ogv"
    if not diff_path.exists():
        diff_path = None
    return audio_path, resultant_path, diff_path


def _probe_video_size(path: Path, ffprobe_exe: str = "ffprobe") -> tuple[int, int]:
    """Return (width, height) of first video stream; defaults suitable for HD metadata if probe fails."""
    try:
        out = subprocess.run(
            [
                ffprobe_exe,
                "-v",
                "error",
                "-select_streams",
                "v:0",
                "-show_entries",
                "stream=width,height",
                "-of",
                "csv=p=0:s=x",
                str(path),
            ],
            capture_output=True,
            text=True,
            check=True,
            timeout=30,
        )
        line = (out.stdout or "").strip().splitlines()[0] if out.stdout else ""
        if "x" in line:
            w, h = line.split("x", 1)
            return int(w), int(h)
    except (subprocess.CalledProcessError, ValueError, IndexError, FileNotFoundError, OSError):
        pass
    return 1920, 1080


def _mpeg4_color_metadata_args(width: int, height: int) -> list[str]:
    """
    Tag color matrix/TRC/primaries on libx264 re-encode. Untagged 480p is often interpreted as BT.709;
    re-encoding then shifts hue (e.g. bluer). SD -> BT.601 (smpte170m); HD -> BT.709.
    """
    if height > 0 and height <= 576:
        return [
            "-colorspace",
            "smpte170m",
            "-color_primaries",
            "smpte170m",
            "-color_trc",
            "smpte170m",
            "-color_range",
            "tv",
        ]
    return [
        "-colorspace",
        "bt709",
        "-color_primaries",
        "bt709",
        "-color_trc",
        "bt709",
        "-color_range",
        "tv",
    ]


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
    loop_strategy: str = "loop",
    trim_audio: bool = False,
    ffmpeg_path: str | Path | None = None,
) -> None:
    """
    Merge resultant video and audio into one file. If audio is longer than resultant video,
    behavior is controlled by loop_strategy ("loop" or "hold").
    If use_diff is True and diff exists, output is original-quality (resultant + diff + audio).
    If trim_audio is True, output duration is trimmed to available video duration.
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
        loss_coef = 0.0
        manifest_path = stored_dir / "manifest.json"
        if manifest_path.exists():
            try:
                with open(manifest_path, "r", encoding="utf-8") as f:
                    m = json.load(f)
                loss_coef = float(m.get("loss_coefficient", 0.0))
            except (json.JSONDecodeError, KeyError, TypeError, ValueError):
                pass
        lossless_out = loss_coef == 0
        log.info("Muxing resultant + diff + audio (original quality), duration=%.1fs, lossless=%s", audio_dur, lossless_out)
        resultant_dur = get_media_duration_seconds(resultant_path, ffprobe_exe=ffprobe_exe)
        diff_dur = get_media_duration_seconds(diff_path, ffprobe_exe=ffprobe_exe)
        target_duration = audio_dur
        if trim_audio:
            candidate = min(d for d in (resultant_dur, diff_dur) if d > 0) if (resultant_dur > 0 or diff_dur > 0) else 0.0
            if candidate > 0:
                target_duration = min(target_duration, candidate)
        apply_diff_ffmpeg(
            resultant_path,
            diff_path,
            audio_path,
            out_path,
            target_duration_sec=target_duration,
            loop_strategy=loop_strategy,
            trim_audio=trim_audio,
            lossless_output=lossless_out,
            ffmpeg_exe=ffmpeg_exe,
            ffprobe_exe=ffprobe_exe,
        )
        log.info("Reconstituted: %s", out_path)
        return
    if use_diff:
        log.warning("--original requested but no diff artifact found; using resultant + audio only.")
        print("Warning: --original requested but no diff artifact found; using resultant + audio only.", file=sys.stderr)
    video_dur = get_media_duration_seconds(resultant_path, ffprobe_exe=ffprobe_exe)
    log.info("Muxing resultant + audio, audio_dur=%.1fs video_dur=%.1fs", audio_dur, video_dur)
    target_duration = min(audio_dur, video_dur) if (trim_audio and video_dur > 0) else audio_dur
    _merge_ffmpeg(
        resultant_path,
        audio_path,
        out_path,
        target_duration_sec=target_duration,
        video_duration_sec=video_dur,
        loop_strategy=loop_strategy,
        ffmpeg_exe=ffmpeg_exe,
        ffprobe_exe=ffprobe_exe,
    )
    log.info("Reconstituted: %s", out_path)


def extract_first_frame(
    stored_dir: Path,
    out_path: Path,
    *,
    image_format: str | None = None,
    ffmpeg_path: str | Path | None = None,
) -> None:
    """
    Extract the first frame from the resultant video and save as an image.
    If image_format is None, read from manifest input_image_format, else default to png.
    """
    stored_dir = Path(stored_dir)
    out_path = Path(out_path)
    ffmpeg_exe = _find_ffmpeg(ffmpeg_path)
    _, resultant_path, _ = find_artifacts(stored_dir)
    if not resultant_path or not resultant_path.exists():
        raise FileNotFoundError(f"Could not find resultant video in {stored_dir}")

    fmt = image_format
    if fmt is None:
        manifest = stored_dir / "manifest.json"
        if manifest.exists():
            with open(manifest, "r", encoding="utf-8") as f:
                m = json.load(f)
            fmt = m.get("input_image_format", "png")
        else:
            fmt = "png"

    ext = "jpg" if fmt == "jpeg" else fmt
    if out_path.suffix.lower() not in (f".{ext}", f".{fmt}"):
        out_path = out_path.with_suffix(f".{ext}")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        ffmpeg_exe, "-y",
        "-i", str(resultant_path),
        "-vframes", "1",
        "-f", "image2",
        str(out_path),
    ]
    if fmt in ("jpg", "jpeg"):
        cmd.insert(-1, "-q:v")
        cmd.insert(-1, "2")
    subprocess.run(cmd, check=True, capture_output=True, timeout=60)
    log.info("Extracted first frame: %s", out_path)


def _merge_ffmpeg(
    video_path: Path,
    audio_path: Path,
    out_path: Path,
    *,
    target_duration_sec: float,
    video_duration_sec: float,
    loop_strategy: str = "loop",
    ffmpeg_exe: str = "ffmpeg",
    ffprobe_exe: str = "ffprobe",
) -> None:
    """
    Mux video + audio. If video is shorter than target_duration_sec, either loop the video
    (loop_strategy=loop) or hold the last frame (loop_strategy=hold), then trim to target.
    """
    out_path.parent.mkdir(parents=True, exist_ok=True)
    mode = str(loop_strategy or "loop").strip().lower()
    if mode not in {"loop", "hold"}:
        raise ValueError(f"Unsupported loop_strategy={loop_strategy!r}; expected 'loop' or 'hold'.")
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
        vw, vh = _probe_video_size(video_path, ffprobe_exe)
        color_args = _mpeg4_color_metadata_args(vw, vh)
        if mode == "hold":
            hold_seconds = max(0.0, target_duration_sec - video_duration_sec)
            cmd = [
                ffmpeg_exe, "-y",
                "-i", str(video_path),
                "-i", str(audio_path),
                "-filter_complex", f"[0:v]tpad=stop_mode=clone:stop_duration={hold_seconds}[v]",
                "-map", "[v]",
                "-map", "1:a",
                "-t", str(target_duration_sec),
                "-c:v", "libx264", "-c:a", "aac",
                *color_args,
                str(out_path),
            ]
        else:
            # Loop video to match audio length, then trim to exact target.
            loop_count = int(target_duration_sec / video_duration_sec) + 1
            cmd = [
                ffmpeg_exe, "-y",
                "-stream_loop", str(loop_count),
                "-i", str(video_path),
                "-i", str(audio_path),
                "-t", str(target_duration_sec),
                "-c:v", "libx264", "-c:a", "aac",
                *color_args,
                str(out_path),
            ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600)
