"""
Standalone script to run store→reconstitute tests with custom media.
Avoids pytest subprocess quirks on Windows. Run from Scripts/:

    python -m video_storage_tool.tests.run_custom_media_tests
    python -m video_storage_tool.tests.run_custom_media_tests --loss-coefficient 0
    python -m video_storage_tool.tests.run_custom_media_tests --loss-coefficient 0.05
"""
import argparse
import random
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path

# Add parent so we can import video_storage_tool
sys.path.insert(0, str(Path(__file__).resolve().parent.parent.parent))

from video_storage_tool.__main__ import run_store
from video_storage_tool.reconstitute import reconstitute, get_media_duration_seconds

from .assert_lossless import assert_roundtrip_quality

ROYKSOPP = Path(r"c:\Users\John\Downloads\royksopp-meets-chopin.mp4")
PEPSI = Path(r"c:\Users\John\OneDrive\Videes\misc\Pepsi_Can_001.mp4")
FIXTURES = Path(__file__).resolve().parent / "fixtures"
RANDOM_TEST_IMAGE = FIXTURES / "random_test_image.jpg"
IMAGE_VIDEO = FIXTURES / "flight_sim_image_3s.mp4"


def _config_for_loss(loss_coefficient: float) -> dict:
    """Build store config for given loss coefficient."""
    if loss_coefficient == 0:
        return {
            "store": {"loss_coefficient": 0.0},
            "audio": {"format": "flac", "max_mb": 5.0},
            "script": {"backend": "stub", "visual_backend": "none"},
            "t2v": {"backend": "stub"},
            "diff": {"enabled": True, "lossless": True},
        }
    return {
        "store": {"loss_coefficient": loss_coefficient},
        "audio": {"format": "aac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": True, "lossless": False},
    }


def _fetch_random_image() -> Path | None:
    """Download a random image from Unsplash (or Picsum fallback)."""
    FIXTURES.mkdir(parents=True, exist_ok=True)
    if RANDOM_TEST_IMAGE.is_file():
        return RANDOM_TEST_IMAGE
    urls = [
        f"https://source.unsplash.com/random/800x600?{random.randint(1, 99999)}",
        f"https://picsum.photos/seed/{random.randint(1, 99999)}/800/600",
        "https://picsum.photos/800/600",
    ]
    for url in urls:
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "video_storage_tool-tests/1.0"})
            with urllib.request.urlopen(req, timeout=15) as resp:
                data = resp.read()
            if len(data) > 1000:
                RANDOM_TEST_IMAGE.write_bytes(data)
                return RANDOM_TEST_IMAGE
        except Exception:
            continue
    return None


def ensure_image_video() -> Path | None:
    img_path = _fetch_random_image()
    if img_path is None:
        print("  Skip: could not fetch random image from Unsplash/Picsum (check network)")
        return None
    FIXTURES.mkdir(parents=True, exist_ok=True)
    subprocess.run(
        [
            "ffmpeg", "-y", "-loop", "1", "-i", str(img_path),
            "-f", "lavfi", "-i", "anullsrc=channel_layout=stereo:sample_rate=44100",
            "-t", "3", "-c:v", "libx264", "-c:a", "aac", "-pix_fmt", "yuv420p",
            "-shortest", str(IMAGE_VIDEO),
        ],
        check=True,
        timeout=30,
        capture_output=True,
    )
    return IMAGE_VIDEO


def run_one(name: str, video_path: Path, loss_coefficient: float) -> bool:
    if not video_path.is_file():
        print(f"  {name}: SKIP (file not found)")
        return False
    orig_dur = get_media_duration_seconds(video_path)
    if orig_dur <= 0:
        print(f"  {name}: SKIP (could not get duration, ffprobe may have failed)")
        return False
    config = _config_for_loss(loss_coefficient)
    with tempfile.TemporaryDirectory(prefix="vst_test_") as tmp:
        out_dir = Path(tmp) / "stored"
        out_dir.mkdir()
        try:
            run_store(video_path, out_dir, config=config)
            recon_path = out_dir / "reconstituted.mp4"
            reconstitute(out_dir, recon_path, use_diff=True)
            dur = get_media_duration_seconds(recon_path)
            tol = max(0.5, orig_dur * 0.15)
            ok = abs(dur - orig_dur) <= tol
            if ok:
                try:
                    assert_roundtrip_quality(video_path, recon_path, loss_coefficient)
                except AssertionError as e:
                    print(f"  {name}: FAIL (quality assertion: {e})")
                    return False
            status = "PASS" if ok else "FAIL"
            print(f"  {name}: {status} (orig={orig_dur:.1f}s recon={dur:.1f}s)")
            return ok
        except Exception as e:
            print(f"  {name}: FAIL ({e})")
            return False


def main() -> int:
    parser = argparse.ArgumentParser(description="Run custom media integration tests")
    parser.add_argument(
        "--loss-coefficient",
        type=float,
        default=0.0,
        help="0 for bit-exact lossless, 0.05 for dynamic assets (PSNR/SSIM)",
    )
    args = parser.parse_args()
    loss_coef = args.loss_coefficient

    print(f"Custom media integration tests (store -> reconstitute, loss_coefficient={loss_coef})\n")
    results = []
    results.append(("royksopp (audio-heavy)", run_one("royksopp", ROYKSOPP, loss_coef)))
    results.append(("pepsi (video-heavy)", run_one("pepsi", PEPSI, loss_coef)))
    image_vid = ensure_image_video()
    if image_vid:
        results.append(("random_image", run_one("random_image", image_vid, loss_coef)))
    else:
        results.append(("random_image", False))
    passed = sum(1 for _, ok in results if ok)
    print(f"\n{passed}/{len(results)} passed")
    return 0 if passed == len(results) else 1


if __name__ == "__main__":
    sys.exit(main())
