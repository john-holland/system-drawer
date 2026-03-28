"""
Compute and apply additive video diff: diff = original - generated (per-frame).
Stored as Ogg Theora (.ogv). Apply: resultant + diff = original.
"""

import json
import logging
import re
import subprocess
from pathlib import Path

log = logging.getLogger("video_storage_tool.diff")


def _resolve_ffmpeg(ffmpeg_path: str | Path | None) -> str:
    if not ffmpeg_path:
        return "ffmpeg"
    try:
        from .audio import _find_ffmpeg
        return _find_ffmpeg(ffmpeg_path)
    except FileNotFoundError:
        return "ffmpeg"


def _probe_video(path: Path) -> dict | None:
    """Get width, height, fps (float), duration_sec from video. Returns None on failure."""
    try:
        out = subprocess.run(
            [
                "ffprobe", "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=width,height,r_frame_rate",
                "-show_entries", "format=duration",
                "-of", "json",
                str(path),
            ],
            capture_output=True,
            text=True,
            check=True,
            timeout=30,
        )
        data = json.loads(out.stdout)
        streams = data.get("streams") or []
        fmt = data.get("format") or {}
        if not streams:
            return None
        s = streams[0]
        w = s.get("width")
        h = s.get("height")
        r = s.get("r_frame_rate")
        dur = fmt.get("duration")
        if w is None or h is None:
            return None
        # Parse fps (e.g. "30000/1001" or "30/1")
        fps = 24.0
        if r:
            parts = r.split("/")
            if len(parts) == 2:
                try:
                    num, den = float(parts[0]), float(parts[1])
                    if den > 0:
                        fps = num / den
                except ValueError:
                    pass
        duration_sec = float(dur) if dur else 0.0
        return {"width": int(w), "height": int(h), "fps": fps, "duration_sec": duration_sec}
    except (subprocess.CalledProcessError, json.JSONDecodeError, ValueError, FileNotFoundError, KeyError):
        return None


def compute_diff(
    original_path: Path,
    resultant_path: Path,
    out_dir: Path,
    *,
    enabled: bool = True,
    quality: int = 6,
    lossless: bool = False,
    ffmpeg_path: str | Path | None = None,
) -> Path | None:
    """
    Compute diff = original - resultant (per-frame, 8-bit wrap).
    When lossless: encode as FFV1 (diff.mkv). Otherwise: Ogg Theora (diff.ogv).
    Aligns resolution (scale resultant to original), fps, and duration (min of both).
    Returns path to diff file or None if skipped/failed (does not raise).
    """
    if not enabled:
        return None
    ffmpeg_exe = _resolve_ffmpeg(ffmpeg_path)
    original_path = Path(original_path)
    resultant_path = Path(resultant_path)
    out_dir = Path(out_dir)
    if not original_path.exists() or not resultant_path.exists():
        return None
    info_orig = _probe_video(original_path)
    info_res = _probe_video(resultant_path)
    if not info_orig or not info_res:
        return None
    w = info_orig["width"]
    h = info_orig["height"]
    fps = info_orig["fps"]
    duration_sec = min(info_orig["duration_sec"], info_res["duration_sec"])
    if duration_sec <= 0 or fps <= 0:
        return None
    if lossless:
        out_path = out_dir / "diff.mkv"
    else:
        out_path = out_dir / "diff.ogv"
    out_dir.mkdir(parents=True, exist_ok=True)
    # Filter: [0]=original, [1]=resultant. Blend in high-precision RGB to reduce chroma artifacts,
    # then convert to a delivery format for codec compatibility.
    filter_parts = [
        f"[0:v]trim=duration={duration_sec},setpts=PTS-STARTPTS,fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,pad={w}:{h}:(ow-iw)/2:(oh-ih)/2:color=black,format=gbrp16le[o16]",
        f"[1:v]trim=duration={duration_sec},setpts=PTS-STARTPTS,fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,pad={w}:{h}:(ow-iw)/2:(oh-ih)/2:color=black,format=gbrp16le[r16]",
        "[o16][r16]blend=all_mode=subtract,format=yuv420p[out]",
    ]
    filter_complex = ";".join(filter_parts)
    if lossless:
        codec_args = ["-c:v", "ffv1", "-level", "3"]
    else:
        codec_args = ["-c:v", "libtheora", "-q:v", str(min(10, max(0, quality)))]
    cmd = [
        ffmpeg_exe, "-y",
        "-i", str(original_path),
        "-i", str(resultant_path),
        "-filter_complex", filter_complex,
        "-map", "[out]",
        *codec_args,
        str(out_path),
    ]
    try:
        log.info("Computing diff (original - resultant), duration=%.1fs, %dx%d @ %.1ffps, lossless=%s", duration_sec, w, h, fps, lossless)
        subprocess.run(cmd, check=True, capture_output=True, timeout=600)
    except (subprocess.CalledProcessError, FileNotFoundError) as e:
        log.warning("compute_diff failed: %s", e)
        return None
    if not out_path.exists():
        return None
    _log_diff_stats(out_path, ffmpeg_exe=ffmpeg_exe)
    return out_path


def _log_diff_stats(diff_path: Path, *, ffmpeg_exe: str = "ffmpeg", max_seconds: float = 2.0) -> None:
    """Run signalstats on the diff video and log mean luma as a percentage (higher = more difference from original)."""
    try:
        proc = subprocess.run(
            [
                ffmpeg_exe, "-y",
                "-i", str(diff_path),
                "-vf", "signalstats",
                "-t", str(max_seconds),
                "-f", "null", "-",
            ],
            capture_output=True,
            text=True,
            timeout=60,
        )
        yavg_re = re.compile(r"YAVG:\s*([\d.]+)")
        values = [float(m.group(1)) for m in yavg_re.finditer(proc.stderr or "")]
        if values:
            mean_luma = sum(values) / len(values)
            pct = (mean_luma / 255.0) * 100.0
            log.info(
                "diff.ogv: mean luma = %.1f (%.0f%% of max) over %d frames — higher = resultant differed more from original",
                mean_luma, pct, len(values),
            )
        else:
            log.info("diff.ogv: written (signalstats not available)")
    except (subprocess.CalledProcessError, FileNotFoundError) as e:
        log.debug("diff stats skipped: %s", e)


def apply_diff_ffmpeg(
    resultant_path: Path,
    diff_path: Path,
    audio_path: Path,
    out_path: Path,
    *,
    target_duration_sec: float,
    loop_strategy: str = "loop",
    trim_audio: bool = False,
    lossless_output: bool = False,
    ffmpeg_exe: str = "ffmpeg",
    ffprobe_exe: str = "ffprobe",
) -> None:
    """
    Produce output = resultant + diff (additive), then mux with audio.
    Assumes diff was computed from same alignment (same duration/fps as resultant segment).
    Output duration = target_duration_sec by default. If trim_audio=True, output is trimmed to
    the shortest available video duration. If resultant+diff is shorter, behavior is controlled by
    loop_strategy ("loop" or "hold").
    When lossless_output: use libx264 -qp 0. When audio is FLAC: use -c:a copy.
    """
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    loop_mode = str(loop_strategy or "loop").strip().lower()
    if loop_mode not in {"loop", "hold"}:
        raise ValueError(f"Unsupported loop_strategy={loop_strategy!r}; expected 'loop' or 'hold'.")

    res_dur = _get_duration(resultant_path, ffprobe_exe=ffprobe_exe)
    diff_dur = _get_duration(diff_path, ffprobe_exe=ffprobe_exe)
    base_video_dur = min(d for d in (res_dur, diff_dur) if d > 0) if (res_dur > 0 or diff_dur > 0) else 0.0
    t = min(target_duration_sec, base_video_dur) if (trim_audio and base_video_dur > 0) else target_duration_sec
    filter_trim = f"trim=duration={t},setpts=PTS-STARTPTS"

    # Reconstruct in diff geometry to match how the residual was authored.
    info_diff = _probe_video(diff_path)
    if info_diff:
        w = int(info_diff["width"])
        h = int(info_diff["height"])
        fps = float(info_diff["fps"])
        prep_res = (
            f"{filter_trim},fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,"
            f"pad={w}:{h}:(ow-iw)/2:(oh-ih)/2:color=black"
        )
        prep_diff = (
            f"{filter_trim},fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,"
            f"pad={w}:{h}:(ow-iw)/2:(oh-ih)/2:color=black"
        )
    else:
        prep_res = filter_trim
        prep_diff = filter_trim

    if lossless_output:
        video_codec = ["-c:v", "libx264", "-qp", "0", "-preset", "ultrafast"]
    else:
        video_codec = ["-c:v", "libx264"]
    audio_is_flac = str(audio_path).lower().endswith(".flac")
    audio_codec = ["-c:a", "copy"] if audio_is_flac else ["-c:a", "aac"]

    # Blend: [0]=resultant, [1]=diff. Use high-precision RGB intermediate to reduce chroma edge artifacts.
    blend_filter = "[res]format=gbrp16le[r16];[diff]format=gbrp16le[d16];[r16][d16]blend=all_mode=addition,format=yuv420p[vid]"
    if base_video_dur <= 0 or base_video_dur >= t:
        filter_complex = f"[0:v]{prep_res}[res];[1:v]{prep_diff}[diff];{blend_filter}"
        cmd = [
            ffmpeg_exe, "-y",
            "-i", str(resultant_path),
            "-i", str(diff_path),
            "-i", str(audio_path),
            "-filter_complex", filter_complex,
            "-map", "[vid]", "-map", "2:a",
            "-t", str(t),
            *video_codec, *audio_codec,
            str(out_path),
        ]
    else:
        if loop_mode == "hold":
            hold_seconds = max(0.0, t - base_video_dur)
            filter_complex = (
                f"[0:v]{prep_res},tpad=stop_mode=clone:stop_duration={hold_seconds}[res];"
                f"[1:v]{prep_diff},tpad=stop_mode=clone:stop_duration={hold_seconds}[diff];"
                f"{blend_filter}"
            )
            cmd = [
                ffmpeg_exe, "-y",
                "-i", str(resultant_path),
                "-i", str(diff_path),
                "-i", str(audio_path),
                "-filter_complex", filter_complex,
                "-map", "[vid]", "-map", "2:a",
                "-t", str(t),
                *video_codec, *audio_codec,
                str(out_path),
            ]
        else:
            loop_count = int(t / base_video_dur) + 1
            filter_complex = f"[0:v]{prep_res}[res];[1:v]{prep_diff}[diff];{blend_filter}"
            cmd = [
                ffmpeg_exe, "-y",
                "-stream_loop", str(loop_count), "-i", str(resultant_path),
                "-stream_loop", str(loop_count), "-i", str(diff_path),
                "-i", str(audio_path),
                "-filter_complex", filter_complex,
                "-map", "[vid]", "-map", "2:a",
                "-t", str(t),
                *video_codec, *audio_codec,
                str(out_path),
            ]
    subprocess.run(cmd, check=True, capture_output=True, timeout=600)


def _get_duration(path: Path, ffprobe_exe: str = "ffprobe") -> float:
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
