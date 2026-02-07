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
    ffmpeg_path: str | Path | None = None,
) -> Path | None:
    """
    Compute diff = original - resultant (per-frame, 8-bit wrap), encode as Ogg Theora.
    Aligns resolution (scale resultant to original), fps, and duration (min of both).
    Returns path to diff.ogv or None if skipped/failed (does not raise).
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
    out_path = out_dir / "diff.ogv"
    out_dir.mkdir(parents=True, exist_ok=True)
    # Filter: [0]=original, [1]=resultant. Trim both to duration_sec, set same fps, scale resultant to w:h, then blend subtract (original - resultant).
    filter_parts = [
        f"[0:v]trim=duration={duration_sec},setpts=PTS-STARTPTS,fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,pad={w}:{h}:(ow-iw)/2:(oh-ih)/2[orig]",
        f"[1:v]trim=duration={duration_sec},setpts=PTS-STARTPTS,fps={fps},scale={w}:{h}:force_original_aspect_ratio=decrease,pad={w}:{h}:(ow-iw)/2:(oh-ih)/2[res]",
        "[orig][res]blend=all_mode=subtract[out]",
    ]
    filter_complex = ";".join(filter_parts)
    cmd = [
        ffmpeg_exe, "-y",
        "-i", str(original_path),
        "-i", str(resultant_path),
        "-filter_complex", filter_complex,
        "-map", "[out]",
        "-c:v", "libtheora",
        "-q:v", str(min(10, max(0, quality))),
        str(out_path),
    ]
    try:
        log.info("Computing diff (original - resultant), duration=%.1fs, %dx%d @ %.1ffps", duration_sec, w, h, fps)
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
    ffmpeg_exe: str = "ffmpeg",
    ffprobe_exe: str = "ffprobe",
) -> None:
    """
    Produce output = resultant + diff (additive), then mux with audio.
    Assumes diff was computed from same alignment (same duration/fps as resultant segment).
    Output duration = target_duration_sec (audio length); if resultant+diff is shorter, loop video.
    """
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    video_dur = _get_duration(resultant_path, ffprobe_exe=ffprobe_exe)
    t = target_duration_sec
    filter_trim = f"trim=duration={t},setpts=PTS-STARTPTS"
    # Blend: [0]=resultant, [1]=diff. addition mode: A+B. Trim to target duration.
    if video_dur <= 0 or video_dur >= t:
        filter_complex = f"[0:v]{filter_trim}[res];[1:v]{filter_trim}[diff];[res][diff]blend=all_mode=addition[vid]"
        cmd = [
            ffmpeg_exe, "-y",
            "-i", str(resultant_path),
            "-i", str(diff_path),
            "-i", str(audio_path),
            "-filter_complex", filter_complex,
            "-map", "[vid]", "-map", "2:a",
            "-t", str(t),
            "-c:v", "libx264", "-c:a", "aac",
            str(out_path),
        ]
    else:
        loop_count = int(t / video_dur) + 1
        filter_complex = f"[0:v]{filter_trim}[res];[1:v]{filter_trim}[diff];[res][diff]blend=all_mode=addition[vid]"
        cmd = [
            ffmpeg_exe, "-y",
            "-stream_loop", str(loop_count), "-i", str(resultant_path),
            "-stream_loop", str(loop_count), "-i", str(diff_path),
            "-i", str(audio_path),
            "-filter_complex", filter_complex,
            "-map", "[vid]", "-map", "2:a",
            "-t", str(t),
            "-c:v", "libx264", "-c:a", "aac",
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
