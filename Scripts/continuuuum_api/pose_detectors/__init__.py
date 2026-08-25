"""Optional pose hops for webcam_anim_upload_queue (MediaPipe, MoCapAnything)."""

from .dispatch import (
    DetectPending,
    GPU_SPECS,
    is_cabin_spec,
    is_gpu_spec,
    is_vehicle_spec,
    is_whisper_spec,
    run_detect_hop,
    set_hop_runner,
)

__all__ = [
    "DetectPending",
    "GPU_SPECS",
    "is_cabin_spec",
    "is_gpu_spec",
    "is_vehicle_spec",
    "is_whisper_spec",
    "run_detect_hop",
    "set_hop_runner",
]
