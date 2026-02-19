"""
Pytest fixtures for video_storage_tool integration tests.
"""
import subprocess
import sys
from pathlib import Path

import pytest


FIXTURES_DIR = Path(__file__).resolve().parent / "fixtures"
FIXTURE_VIDEO = FIXTURES_DIR / "fixture_3s.mp4"


def pytest_configure(config: pytest.Config) -> None:
    config.addinivalue_line("markers", "slow: marks tests as slow (Whisper/BLIP)")


def _ensure_fixture_video() -> Path:
    """Generate fixture video with ffmpeg if it does not exist. Returns path to fixture."""
    FIXTURES_DIR.mkdir(parents=True, exist_ok=True)
    if FIXTURE_VIDEO.is_file():
        return FIXTURE_VIDEO
    # Generate 3s blue frame + 440Hz tone using ffmpeg
    try:
        subprocess.run(
            [
                "ffmpeg", "-y",
                "-f", "lavfi", "-i", "color=c=blue:s=320x240:d=3",
                "-f", "lavfi", "-i", "sine=frequency=440:duration=3",
                "-c:v", "libx264", "-c:a", "aac", "-shortest",
                str(FIXTURE_VIDEO),
            ],
            check=True,
            timeout=60,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except (subprocess.CalledProcessError, FileNotFoundError, subprocess.TimeoutExpired) as e:
        pytest.skip(f"Could not generate fixture video (ffmpeg required): {e}")
    return FIXTURE_VIDEO


@pytest.fixture
def fixture_video_path() -> Path:
    """Path to a short (3s) test video. Generated if missing."""
    return _ensure_fixture_video()


@pytest.fixture
def temp_output_dir(tmp_path: Path) -> Path:
    """Temporary directory for store/reconstitute output."""
    out = tmp_path / "stored"
    out.mkdir()
    return out


@pytest.fixture
def store_config() -> dict:
    """Default config for store: stub backends, no visual description."""
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
    """Config for lossy round-trip (coefficient>0, dynamic assets)."""
    return {
        "store": {"loss_coefficient": 0.05},
        "audio": {"format": "aac", "max_mb": 5.0},
        "script": {"backend": "stub", "visual_backend": "none"},
        "t2v": {"backend": "stub"},
        "diff": {"enabled": True, "lossless": False},
    }
