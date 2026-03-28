from pathlib import Path

import pytest

from video_storage_tool import video_to_script as vts


def _fake_paths(tmp_path: Path) -> tuple[Path, Path]:
    video = tmp_path / "input.mp4"
    audio = tmp_path / "audio.aac"
    video.write_bytes(b"00")
    audio.write_bytes(b"00")
    return video, audio


def test_gradient_assertion_passes_with_per_frame_and_avg(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    video, audio = _fake_paths(tmp_path)

    def _fake_describe(_video_path, _config, *, progress_callback=None):
        return (
            "0.0s: frame one\n1.0s: frame two",
            "0.0s: top=#101010 mid=#202020 bottom=#303030\n"
            "1.0s: top=#111111 mid=#222222 bottom=#333333\n"
            "avg: top=#111111 mid=#212121 bottom=#323232",
            "",
        )

    monkeypatch.setattr(vts, "_describe_video_frames", _fake_describe)
    out = vts.video_to_script(
        video,
        audio,
        tmp_path,
        backend="stub",
        config={"script": {"visual_backend": "blip", "assert_color_gradient_per_frame": True}},
    )
    content = out.read_text(encoding="utf-8")
    assert "[Color gradient]" in content
    assert "avg:" in content


def test_gradient_assertion_fails_when_frame_gradients_missing(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    video, audio = _fake_paths(tmp_path)

    def _fake_describe(_video_path, _config, *, progress_callback=None):
        return (
            "0.0s: frame one\n1.0s: frame two",
            "0.0s: top=#101010 mid=#202020 bottom=#303030\n"
            "avg: top=#101010 mid=#202020 bottom=#303030",
            "",
        )

    monkeypatch.setattr(vts, "_describe_video_frames", _fake_describe)
    with pytest.raises(AssertionError, match="Color gradient assertion failed"):
        vts.video_to_script(
            video,
            audio,
            tmp_path,
            backend="stub",
            config={"script": {"visual_backend": "blip", "assert_color_gradient_per_frame": True}},
        )
