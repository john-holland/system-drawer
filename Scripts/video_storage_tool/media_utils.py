"""
Media format detection and conversion. Preserves input image format when processing images.
Uses ffmpeg (required) or ImageMagick (optional fallback) for conversion.
"""
import shutil
import subprocess
from pathlib import Path
from typing import Optional

from .audio import _find_ffmpeg

IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff", ".tif", ".gif"}
EXT_TO_FORMAT = {
    ".png": "png",
    ".jpg": "jpg",
    ".jpeg": "jpg",
    ".webp": "webp",
    ".bmp": "bmp",
    ".tiff": "tiff",
    ".tif": "tiff",
    ".gif": "gif",
}


def is_image_input(path: Path) -> bool:
    """Return True if the path appears to be an image file (by extension)."""
    p = Path(path)
    return p.suffix.lower() in IMAGE_EXTENSIONS


def get_image_format(path: Path) -> Optional[str]:
    """
    Return the image format string (e.g. 'png', 'jpg') from the path extension.
    Returns None if not a recognized image extension.
    """
    ext = Path(path).suffix.lower()
    return EXT_TO_FORMAT.get(ext)


def convert_image_format(
    src: Path,
    dst: Path,
    *,
    ffmpeg_path: str | Path | None = None,
    quality: int = 2,
) -> Path:
    """
    Convert image from src to dst format using ffmpeg (or ImageMagick fallback).
    dst extension determines output format. Returns dst on success.
    quality: 1-31 for JPEG (lower = better); 0-100 for WebP.
    """
    src, dst = Path(src), Path(dst)
    ext = dst.suffix.lower()
    ffmpeg_exe = _find_ffmpeg(ffmpeg_path)

    # ffmpeg supports: png, mjpeg (jpg), webp, bmp, gif, tiff
    ffmpeg_formats = {".png": ("png", []), ".jpg": ("mjpeg", ["-q:v", str(quality)]), ".jpeg": ("mjpeg", ["-q:v", str(quality)]),
                      ".webp": ("libwebp", ["-quality", str(min(100, max(0, quality)))]), ".bmp": ("bmp", []),
                      ".gif": ("gif", []), ".tiff": ("tiff", []), ".tif": ("tiff", [])}
    if ext in ffmpeg_formats:
        codec, extra = ffmpeg_formats[ext]
        cmd = [ffmpeg_exe, "-y", "-i", str(src), "-c:v", codec] + extra + [str(dst)]
        try:
            subprocess.run(cmd, check=True, capture_output=True, timeout=60)
            return dst
        except (subprocess.CalledProcessError, FileNotFoundError):
            pass

    # Fallback: ImageMagick
    magick = shutil.which("magick") or shutil.which("convert")
    if magick:
        q = "95" if ext not in (".jpg", ".jpeg", ".webp") else str(min(100, max(0, quality)))
        try:
            args = [magick, str(src)]
            if ext in (".jpg", ".jpeg", ".webp"):
                args.extend(["-quality", q])
            args.append(str(dst))
            subprocess.run(args, check=True, capture_output=True, timeout=60)
            return dst
        except (subprocess.CalledProcessError, FileNotFoundError):
            pass

    # Last resort: ffmpeg to PNG (always works)
    subprocess.run([ffmpeg_exe, "-y", "-i", str(src), str(dst)], check=True, capture_output=True, timeout=60)
    return dst
