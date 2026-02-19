"""
Integration tests for video_storage_tool: store → reconstitute round-trip
and description-based verification that original and reconstructed content match.
"""
from pathlib import Path

import pytest

from video_storage_tool.__main__ import run_store
from video_storage_tool.reconstitute import reconstitute, get_media_duration_seconds

from .assert_lossless import assert_roundtrip_quality
from .description_utils import describe_video, description_similarity, extract_transcript_from_script


def test_roundtrip_reconstitute(
    fixture_video_path: Path,
    temp_output_dir: Path,
    store_config: dict,
) -> None:
    """
    Store (stub script/T2V) → reconstitute. Assert output exists, duration ~expected, format valid.
    """
    run_store(
        fixture_video_path,
        temp_output_dir,
        config=store_config,
    )
    reconstituted_path = temp_output_dir / "reconstituted.mp4"
    reconstitute(temp_output_dir, reconstituted_path)

    assert reconstituted_path.is_file(), "Reconstituted file should exist"
    duration = get_media_duration_seconds(reconstituted_path)
    assert duration > 0, "Reconstituted video should have valid duration"
    # Expect ~3s (fixture length); allow 10% tolerance
    assert 2.5 <= duration <= 3.5, f"Duration {duration}s should be ~3s"


@pytest.mark.slow
def test_description_parity_after_reconstitute(
    fixture_video_path: Path,
    temp_output_dir: Path,
    store_config: dict,
) -> None:
    """
    Store → reconstitute, then use description algorithms (Whisper) to verify
    original and reconstituted content match (transcript similarity above threshold).
    Skips if Whisper not installed.
    For silence-only fixtures, both descriptions indicate no speech → pass.
    """
    pytest.importorskip("whisper")
    run_store(fixture_video_path, temp_output_dir, config=store_config)
    reconstituted_path = temp_output_dir / "reconstituted.mp4"
    reconstitute(temp_output_dir, reconstituted_path)

    desc_original = describe_video(fixture_video_path, store_config)
    desc_reconstituted = describe_video(reconstituted_path, store_config)
    trans_orig = extract_transcript_from_script(desc_original)
    trans_recon = extract_transcript_from_script(desc_reconstituted)

    def indicates_silence(s: str) -> bool:
        lower = s.lower()
        return (
            "no speech" in lower
            or len(s.strip()) < 20
            or len(s.split()) <= 2
        )

    if indicates_silence(trans_orig) and indicates_silence(trans_recon):
        return  # Both indicate silence / minimal content; round-trip preserved it
    similarity = description_similarity(trans_orig, trans_recon)
    assert similarity >= 0.85, (
        f"Description similarity {similarity:.2f} should be >= 0.85. "
        f"Original transcript: {trans_orig[:100]}... Reconstituted: {trans_recon[:100]}..."
    )


def test_roundtrip_lossless_bit_exact(
    fixture_video_path: Path,
    temp_output_dir: Path,
    store_config_lossless: dict,
) -> None:
    """Store (lossless: FLAC + FFV1 diff) → reconstitute with diff → bit-exact assertion."""
    run_store(fixture_video_path, temp_output_dir, config=store_config_lossless)
    reconstituted_path = temp_output_dir / "reconstituted.mp4"
    reconstitute(temp_output_dir, reconstituted_path, use_diff=True)

    assert reconstituted_path.is_file(), "Reconstituted file should exist"
    assert_roundtrip_quality(fixture_video_path, reconstituted_path, 0.0)


def test_roundtrip_lossy_quality(
    fixture_video_path: Path,
    temp_output_dir: Path,
    store_config_lossy: dict,
) -> None:
    """Store (lossy: AAC + Theora) → reconstitute with diff → PSNR/SSIM thresholds."""
    run_store(fixture_video_path, temp_output_dir, config=store_config_lossy)
    reconstituted_path = temp_output_dir / "reconstituted.mp4"
    reconstitute(temp_output_dir, reconstituted_path, use_diff=True)

    assert reconstituted_path.is_file(), "Reconstituted file should exist"
    assert_roundtrip_quality(fixture_video_path, reconstituted_path, 0.05)
