"""
Assert round-trip quality: bit-exact (loss_coefficient=0) or PSNR/SSIM thresholds (>0).
"""
import subprocess
from pathlib import Path


def assert_roundtrip_quality(
    original_path: Path,
    reconstituted_path: Path,
    loss_coefficient: float,
    *,
    ffmpeg_exe: str = "ffmpeg",
) -> None:
    """
    Assert that reconstituted matches original.
    When loss_coefficient == 0: bit-exact (framemd5 + stream hash).
    When loss_coefficient > 0: PSNR and SSIM above threshold.
    """
    orig = Path(original_path)
    recon = Path(reconstituted_path)
    if not orig.is_file() or not recon.is_file():
        raise FileNotFoundError(f"Missing file: {orig} or {recon}")

    if loss_coefficient == 0:
        _assert_bit_exact(orig, recon, ffmpeg_exe=ffmpeg_exe)
    else:
        _assert_quality_threshold(orig, recon, loss_coefficient, ffmpeg_exe=ffmpeg_exe)


def _assert_bit_exact(orig: Path, recon: Path, *, ffmpeg_exe: str = "ffmpeg") -> None:
    """Assert decoded video frames and audio stream hashes are identical."""
    # Video: framemd5
    out_orig = subprocess.run(
        [ffmpeg_exe, "-i", str(orig), "-f", "framemd5", "-"],
        capture_output=True,
        text=True,
        timeout=120,
    )
    out_recon = subprocess.run(
        [ffmpeg_exe, "-i", str(recon), "-f", "framemd5", "-"],
        capture_output=True,
        text=True,
        timeout=120,
    )
    if out_orig.returncode != 0 or out_recon.returncode != 0:
        raise AssertionError(
            f"framemd5 failed: orig={out_orig.returncode} recon={out_recon.returncode}. "
            f"stderr: {out_orig.stderr[:500]} ... {out_recon.stderr[:500]}"
        )
    lines_orig = [l for l in (out_orig.stdout or "").splitlines() if l.strip() and not l.strip().startswith("#")]
    lines_recon = [l for l in (out_recon.stdout or "").splitlines() if l.strip() and not l.strip().startswith("#")]
    if lines_orig != lines_recon:
        raise AssertionError(
            f"Video framemd5 mismatch: {len(lines_orig)} vs {len(lines_recon)} lines. "
            f"First diff: orig={lines_orig[:3]} recon={lines_recon[:3]}"
        )

    # Audio: stream hash
    out_orig_a = subprocess.run(
        [ffmpeg_exe, "-i", str(orig), "-map", "0:a", "-f", "hash", "-hash", "md5", "-"],
        capture_output=True,
        text=True,
        timeout=60,
    )
    out_recon_a = subprocess.run(
        [ffmpeg_exe, "-i", str(recon), "-map", "0:a", "-f", "hash", "-hash", "md5", "-"],
        capture_output=True,
        text=True,
        timeout=60,
    )
    if out_orig_a.returncode != 0 or out_recon_a.returncode != 0:
        raise AssertionError(
            f"Audio hash failed: orig={out_orig_a.returncode} recon={out_recon_a.returncode}"
        )
    hash_orig = (out_orig_a.stdout or "").strip()
    hash_recon = (out_recon_a.stdout or "").strip()
    if hash_orig != hash_recon:
        raise AssertionError(f"Audio stream hash mismatch: {hash_orig} vs {hash_recon}")


def _assert_quality_threshold(
    orig: Path,
    recon: Path,
    loss_coefficient: float,
    *,
    ffmpeg_exe: str = "ffmpeg",
) -> None:
    """Assert PSNR and SSIM above threshold derived from loss_coefficient."""
    psnr_min = max(30.0, 40.0 - 100.0 * loss_coefficient)
    ssim_min = max(0.95, 0.99 - loss_coefficient)

    import re
    # PSNR: stats_file=- sends to stdout; format has psnr_y:XX.XX per frame
    cmd = [
        ffmpeg_exe, "-y",
        "-i", str(recon),
        "-i", str(orig),
        "-lavfi", "psnr=stats_file=-",
        "-f", "null", "-",
    ]
    out = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
    psnr_re = re.compile(r"psnr_y:([\d.]+)")
    psnr_values = [float(m.group(1)) for m in psnr_re.finditer(out.stdout or "")]
    avg_psnr = sum(psnr_values) / len(psnr_values) if psnr_values else 0.0

    # SSIM: format has All:0.99 per frame in stats_file
    cmd_ssim = [
        ffmpeg_exe, "-y",
        "-i", str(recon),
        "-i", str(orig),
        "-lavfi", "ssim=stats_file=-",
        "-f", "null", "-",
    ]
    out_ssim = subprocess.run(cmd_ssim, capture_output=True, text=True, timeout=120)
    ssim_re = re.compile(r"All:([\d.]+)")
    ssim_values = [float(m.group(1)) for m in ssim_re.finditer(out_ssim.stdout or "")]
    avg_ssim = sum(ssim_values) / len(ssim_values) if ssim_values else 0.0

    if avg_psnr < psnr_min:
        raise AssertionError(
            f"PSNR {avg_psnr:.2f} dB below threshold {psnr_min:.2f} (loss_coefficient={loss_coefficient})"
        )
    if avg_ssim < ssim_min:
        raise AssertionError(
            f"SSIM {avg_ssim:.4f} below threshold {ssim_min:.4f} (loss_coefficient={loss_coefficient})"
        )
