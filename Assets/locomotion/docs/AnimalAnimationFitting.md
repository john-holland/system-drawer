# Animal Animation Fitting + MediaPipe / MoCapAnything

Menus:

- **Window → System Drawer → Ragdoll → Fitting Wizard** — humanoid import (MediaPipe PoseTrack or MoCapAnything BVH)
- **Window → System Drawer → Animation → Animal Animation Fitting Wizard** — non-human `Generic:` / `Animal:` BoneMap

Shared fitter: `ArbitrarySkeletonFitter` (name, synonym, hierarchy, unmatched joints offered as `Animal:{Name}` — never dropped).

## Detect hops (Continuuuum)

`mediapipe_holistic@v1` and `mocapanything@v2` run while a queue row is `running`. Metadata-only POSTs still complete on GET. GPU hops with a file stay `running` until the hop returns (or `DetectPending`).

Install MediaPipe in a **Python 3.12** env (not 3.14):

```
pip install mediapipe
```

Weights download via MediaPipe. Do not vendor `.tflite` in this repo.

MoCapAnything: set `MOCAPANYTHING_ROOT` / `MOCAPANYTHING_PYTHON` (default `D:\Development\MocapAnything\.venv`). Capture UI **species** is required for `mocapanything@v2`.

PoseTrack JSON: `{ modelSpec, samples: [{ traitId, timeMs, localPosition, localRotation }] }`. Fetch `/api/webcam-animations/<id>/posetrack` when ready.

## Animation managers

`RagdollAnimationSetManager` + `RagdollIKAnimationManager` are added by `RagdollAutoWire.EnsureAnimationRoots` (Fitting Wizard / Repair / this wizard). They are not fields on `RagdollActor`. The wizards list `availableAnimations` (sets). All scene animation BTs: Nervous System Impulse Viewer.

`SystemDrawerAnimator` owns timing and blending. The set manager only Play/Pause/Stop and switches the active set.
