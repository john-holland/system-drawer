# IK Webcam / Video Interpretation

Menus:

- **Window → System Drawer → Animation → IK Webcam Video Interpretation**
- **Window → System Drawer → Animation → Dance Animation Editor**

Shared take asset: `WebcamAnimRecordingAsset` (`Assets/WebcamAnim/`). Pose uses `IPoseAnimationDetector` — local stub, or Continuuuum remote loading PoseTrack JSON from `mediapipe_holistic@v1` / `mocapanything@v2` hops. See [AnimalAnimationFitting.md](AnimalAnimationFitting.md).

## Window layout

1. Actor viewport | live webcam / picked video (side by side RenderTextures).
2. Scene bone dimension grabbers (`BoneDimensionGrabber` on `BoneMap.traitId` targets). Spawn from the inspector strip.
3. Timeline with granularity slider (decimillisecond → minute), in/out markers, kind (`Ambulatory` / `Vehicle` / `Dance` / `Misc`).
4. Inspector: `model_spec`, `subsection` (reedit key), animation list index from `RagdollIKAnimationManager.availableAnimations`, local detect vs Continuuuum upload.

Vehicle / misc takes set `targetHint` (`magneto_bt`, `toaster`, `ragdoll`, …). Interpretation feeds magneto/vehicle BT segments; it does not replace the Magneto designer.

## USC `type_metadata`

No new library columns. JSON:

```json
{
  "kind": "webcam_anim_recording",
  "webcamAnimKind": "vehicle",
  "model_spec": "mediapipe_holistic@…",
  "subsection": "takeoff_roll_0",
  "animationListIndex": 3,
  "timelineStartMs": 1200,
  "timelineEndMs": 8400,
  "granularity": "millisecond",
  "targetHint": "magneto_bt",
  "species": "Lion",
  "poseTrackPath": ""
}
```

Continuuuum page: `/webcam-animations`. WebGL preview deep-links `/continuuuum_editor/index.html?docId=&apiBase=&subsection=`.

## Dance rainbow mirror map

Call (left) → response (right). Hue starts at **blue** for perpendicular associations and gradients toward red as the pairing goes off-axis (arms↔legs, hands↔feet, lead↔answer).

The diagonal through the map marks intersecting move pairs. **`allowIntersect` defaults to false** — conflicting pairings are blocked in the picker until the checkbox is on.

Moves bind to `availableAnimations` indices. Optional labels: `DanceIkTrainingCatalog` mode ids / `LoveMakingAnimationGroup.DanceClose`.

## Dance song + dialogue columns

Layout: routine list | **time ruler** | **dialogue sequences** | thick center line | Routine / Pairing / Webcam.

- **Contains dialog** / **Contains song** on the routine. Checking one shows that list of start/stop `DanceMediaSpan` objects (`startMs`/`endMs`, `label`, `audioRef`, optional `dialogueSetId`). Unchecking hides the list; spans are kept.
- Time ruler shares webcam granularity (`WebcamAnimTimelineGranularityUtil.SnapMs`) plus bar ticks from `CausalityMusicBridge` / `BeatQuantizedActionBinder` BPM (`60/bpm * beatsPerBar`). **Quantize clips** pulls `AudioClip`s from mixer stems and `DigitalEffectsMachine` child `AudioSource`s onto that grid (`PlayerInteractionQuantizer`).
- `DanceRoutineRunner` delays `PlayStep` onto the same grid when `containsSong` is on. Dialog spans do not auto-fire `DialogueRunner` in v1.

### Audio `model_spec`

| Detector | Default spec | v1 |
|----------|----------------|----|
| `LocalStubAudioSpanDetector` | (any) | synthetic spans |
| `WhisperDialogSpanDetector` | `whisper@base` | stub; future USC `/api/media` transcript |
| `MusicAnalysisSpanDetector` | `music_analysis@stub` | stub |

Do not ship Whisper weights. Real ASR is a Continuuuum/USC media hop later.

WebGL preview of takes stays `/continuuuum_editor/index.html?docId=&apiBase=&subsection=`. Docker/CI for that host: `docker/webgl/` and `.github/workflows/webgl.yml`.

