"""
Integration tests with custom media: audio-heavy video, video-heavy video, and image.
Run with: pytest video_storage_tool/tests/test_custom_media.py -v
"""
import random
import subprocess
import urllib.request
from pathlib import Path

import pytest

from video_storage_tool.__main__ import run_store
from video_storage_tool.reconstitute import reconstitute, get_media_duration_seconds

from .assert_lossless import assert_roundtrip_quality
from .description_utils import describe_video, description_similarity, extract_transcript_from_script

# Custom media paths - adjust if needed
ROYKSOPP_VIDEO = Path(r"c:\Users\John\Downloads\royksopp-meets-chopin.mp4")  # audio-heavy
PEPSI_VIDEO = Path(r"c:\Users\John\OneDrive\Videes\misc\Pepsi_Can_001.mp4")  # video-heavy
FIXTURES_DIR = Path(__file__).resolve().parent / "fixtures"
RANDOM_TEST_IMAGE = FIXTURES_DIR / "random_test_image.jpg"
IMAGE_AS_VIDEO = FIXTURES_DIR / "flight_sim_image_3s.mp4"


def _fetch_random_unsplash_image() -> Path | None:
    """Download a random image from Unsplash (via Picsum fallback). Returns path or None."""
    FIXTURES_DIR.mkdir(parents=True, exist_ok=True)
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


def _image_to_video() -> Path:
    """Fetch random image from Unsplash (or Picsum), convert to 3s video for pipeline. Skips if fetch fails."""
    FIXTURES_DIR.mkdir(parents=True, exist_ok=True)
    img_path = _fetch_random_unsplash_image()
    if img_path is None or not img_path.is_file():
        pytest.skip("Could not fetch random image from Unsplash/Picsum (check network)")
    try:
        subprocess.run(
            [
                "ffmpeg", "-y", "-loop", "1", "-i", str(img_path),
                "-f", "lavfi", "-i", "anullsrc=channel_layout=stereo:sample_rate=44100",
                "-t", "3", "-c:v", "libx264", "-c:a", "aac", "-pix_fmt", "yuv420p",
                "-shortest", str(IMAGE_AS_VIDEO),
            ],
            check=True,
            timeout=30,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except (subprocess.CalledProcessError, FileNotFoundError, subprocess.TimeoutExpired) as e:
        pytest.skip(f"Could not convert image to video: {e}")
    return IMAGE_AS_VIDEO


@pytest.fixture
def store_config() -> dict:
    return {
        "audio": {"format": "aac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": False},
    }


@pytest.fixture
def store_config_lossless() -> dict:
    """Config for bit-exact lossless round-trip (coefficient=0)."""
    return {
        "store": {"loss_coefficient": 0.0},
        "audio": {"format": "flac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": True, "lossless": True},
    }


@pytest.fixture
def store_config_lossy() -> dict:
    """Config for lossy round-trip (coefficient>0)."""
    return {
        "store": {"loss_coefficient": 0.05},
        "audio": {"format": "aac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": True, "lossless": False},
    }


def _run_roundtrip(
    video_path: Path,
    temp_output_dir: Path,
    config: dict,
    *,
    use_diff: bool | None = None,
) -> Path:
    """Store then reconstitute. Returns path to reconstituted file."""
    run_store(video_path, temp_output_dir, config=config)
    reconstituted_path = temp_output_dir / "reconstituted.mp4"
    use_diff_flag = use_diff if use_diff is not None else config.get("diff", {}).get("enabled", False)
    reconstitute(temp_output_dir, reconstituted_path, use_diff=use_diff_flag)
    return reconstituted_path


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
        "store": {"loss_coefficient": 0.05},
        "audio": {"format": "aac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": True, "lossless": False},
    }


@pytest.mark.parametrize(
    "media_id,media_path_getter",
    [
        ("royksopp_audio_heavy", lambda: ROYKSOPP_VIDEO),
        ("pepsi_video_heavy", lambda: PEPSI_VIDEO),
        ("random_image", lambda: _image_to_video()),
    ],
    ids=["royksopp", "pepsi", "random_image"],
)
@pytest.mark.parametrize("loss_coefficient", [0.0, 0.05], ids=["lossless", "lossy"])
def test_roundtrip_custom_media(
    media_id: str,
    media_path_getter,
    temp_output_dir: Path,
    loss_coefficient: float,
) -> None:
    """Store → reconstitute for each custom media. Assert output valid and quality."""
    video_path = media_path_getter()
    if not video_path.is_file():
        pytest.skip(f"Media not found: {video_path}")

    orig_duration = get_media_duration_seconds(video_path)
    if orig_duration <= 0:
        pytest.skip(f"Could not get duration for {video_path}")

    config = _config_for_loss(loss_coefficient)
    reconstituted_path = _run_roundtrip(video_path, temp_output_dir, config)

    assert reconstituted_path.is_file(), "Reconstituted file should exist"
    duration = get_media_duration_seconds(reconstituted_path)
    assert duration > 0, "Reconstituted video should have valid duration"
    # Within 15% of original (reconstitute uses audio length which may differ slightly)
    tol = max(0.5, orig_duration * 0.15)
    assert abs(duration - orig_duration) <= tol, (
        f"Duration {duration:.1f}s should be ~{orig_duration:.1f}s (tol={tol:.1f})"
    )
    assert_roundtrip_quality(video_path, reconstituted_path, loss_coefficient)


@pytest.mark.slow
@pytest.mark.parametrize("media_id,video_path", [
    ("royksopp", ROYKSOPP_VIDEO),
    ("pepsi", PEPSI_VIDEO),
], ids=["royksopp_desc", "pepsi_desc"])
def test_description_parity_custom_media(
    media_id: str,
    video_path: Path,
    temp_output_dir: Path,
    store_config: dict,
) -> None:
    """Description-based verification for royksopp and pepsi (Whisper)."""
    pytest.importorskip("whisper")
    if not video_path.is_file():
        pytest.skip(f"Media not found: {video_path}")

    reconstituted_path = _run_roundtrip(video_path, temp_output_dir, store_config)

    desc_original = describe_video(video_path, store_config)
    desc_reconstituted = describe_video(reconstituted_path, store_config)
    trans_orig = extract_transcript_from_script(desc_original)
    trans_recon = extract_transcript_from_script(desc_reconstituted)

    def indicates_silence(s: str) -> bool:
        lower = s.lower()
        return "no speech" in lower or len(s.strip()) < 20 or len(s.split()) <= 2

    if indicates_silence(trans_orig) and indicates_silence(trans_recon):
        return
    similarity = description_similarity(trans_orig, trans_recon)
    assert similarity >= 0.85, (
        f"[{media_id}] Description similarity {similarity:.2f} should be >= 0.85. "
        f"Original: {trans_orig[:80]}... Reconstituted: {trans_recon[:80]}..."
    )
