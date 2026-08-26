# IK Webcam / Video Interpretation

Menus:

- **Window → System Drawer → Animation → IK Webcam Video Interpretation**
- **Window → System Drawer → Animation → Vehicle Video Steering**
- **Locomotion → Vehicle Video Steering**
- **Window → System Drawer → Animation → Dance Animation Editor**

Shared take asset: `WebcamAnimRecordingAsset` (`Assets/WebcamAnim/`). Pose uses `IPoseAnimationDetector` — local stub, or Continuuuum remote loading PoseTrack JSON from `mediapipe_holistic@v1` / `mocapanything@v2` hops. See [AnimalAnimationFitting.md](AnimalAnimationFitting.md).

## Window layout

1. Actor viewport | live webcam / picked video (side by side RenderTextures).
2. **Recording preview** — optional test-scene camera under the clip at **50% opacity** (scale/offset nudges toward 1:1). `WebcamAnimPreviewRig` holds the overlay camera + camera list.
3. Scene bone dimension grabbers (`BoneDimensionGrabber` on `BoneMap.traitId` targets). Spawn from the inspector strip.
4. **Time scrubber** (`WebcamAnimTimeScrubber`): Play / Pause / Stop, playhead **text** field, tick bar (vehicle cuts + camera switch-overs). Granularity slider (decimillisecond → minute), **In / Out / Duration / Limit** text fields (ms). Empty duration or limit auto-fills from **video length**, then loaded pose/animation, then the span already on the recording. Typing In/Out/Duration locks the span so a later video prepare will not overwrite it. IK training preview duration uses the same order (user seconds, video, clip.length).
5. Inspector: `model_spec`, `subsection` (reedit key), animation list index from `RagdollIKAnimationManager.availableAnimations`, local detect vs Continuuuum upload.

**Sync vehicle ragdoll** (default on): playhead applies `PoseTrackPlayer` plus projected chassis pose (`VehicleTrackProjector.TrySample`) onto `VehicleActor` / occupant seat.

**Multi-mode cameras:** `cameraShots[]` on the recording — `startMs`, `CameraFocusMode`, list index, transition (`Cut` / `Blend` / `Crossfade`) and duration. Blend uses `CameraTransitionController`. Preview rig optional camera list + `CameraPathingRig`.

Vehicle / misc takes set `targetHint` (`magneto_bt`, `toaster`, `ragdoll`, …). Interpretation feeds magneto/vehicle BT segments; it does not replace the Magneto designer. `targetHint` stays a downstream label.

## Live mirror (scene ragdoll)

While this window is open and webcam or a VideoPlayer is playing, **Live mirror** (default on) applies `_asset.lastTrack` onto the assigned `RagdollIKAnimationManager` ragdoll:

1. Resolve `BoneMap` from `GetRagdollActorTransform()` (`GetComponent` / children). Missing maps run `RagdollAutoWire.EnsureBoneMap` once.
2. Fast-follow time is the **latest** sample `timeMs` (not the timeline slider). Video-file fallback: `VideoPlayer.time * 1000` after a full Detect.
3. `PoseTrackPlayer.Apply` then `RagdollPoseUtility.ZeroRagdollVelocities`. `SceneView.RepaintAll`.
4. Closing the window stops applying and does **not** reset the pose. No clip bake, no second preview actor.

**Local detect** skips the live MediaPipe hop and still Applies `lastTrack` if Detect already filled it. Uncheck Local detect for `POST /api/webcam-animations/live-pose` (~10 Hz JPEG from the video RT, one in-flight request). Continuuuum needs MediaPipe; missing weights return 503.

## Optional IK training (editor)

Assign a `PhysicsIKTrainingRunAsset` and optional measurement `SceneAsset`. **Activate training objects in editor** (default on when a scene or object-weight list is set) `SetActive(true)` listed props without Play Mode and restores them when the sweep ends. Additive `measurementScenePath` opens on Start Training and closes after.

Object weights (`hierarchyPath`, `weight`) and limb weights (`Human:RightHand`, `weight`) score as `1 / (1 + sum(limbW * objectW * distance))` in edit mode when a BoneMap and at least one object resolve. Solver coefficient sweeps stay as they are.

**Start IK training from current pose** sets `initialPoseMode = Current` and opens the IK Animation Training window so the live-mirrored pose is the start pose.

## Edit-mode contact → Consider

**Edit-mode contact activation** (default on with live mirror or activate-objects) overlaps ragdoll colliders with measurement objects, sends `SensoryData` through `NervousSystem.SendImpulseUp("Spinal")` so Consider + registered `considerComponents` run, and `GetAvailableGoodSections`. `SystemDrawerAnimator.TickLayersFromEditor` evaluates BT layers without Play Mode. `Sensor.DetectContacts` uses `OverlapSphere`. Do not put `[ExecuteAlways]` on Consider.

## Reset to state

The button starts disabled. It enables on the first GoodSection enabled by the contact handler **or** a physics translation of an activated object (not live `PoseTrackPlayer.Apply`). Click restores first-seen snapshots (including cascade objects seen at first overlap), clears handler-enabled GoodSections, zeroes velocities, and disables again. Editor-only; does not write the run asset.

Training window help: activate-in-editor + additive measurement scene + the same Reset to state control.

## Vehicle takes (Intel YOLO26)

Vehicle kind **requires** `model_spec` `yolo26_vehicle@intel` ([Intel/vehicle-detection](https://huggingface.co/Intel/vehicle-detection)) for **chase-cam / exterior** clips. There is no MediaPipe fallback. Classes: `car`(2), `motorcycle`(3), `bus`(5), `truck`(7), confidence ≥ 0.4.

The Continuuuum hop writes `vehicleTrackPath` JSON (still no new SQL columns): per-frame `{ tMs, trackId, classId, className, conf, bbox, cx, cy }` plus `segments[]` from cheap OpenCV scene cuts (HSV histogram drop or primary bbox jump). Weights live in `YOLO26_CACHE` (default `~/.cache/intel-yolo26`); do not vendor `.xml`/`.bin` in git.

**Facing yaw** (0–360°) is the world yaw of camera forward. Inferred heading is `atan2(dx, -dy) + facingYaw` (image +x right, +y down). Persist `facingYawDegrees` on the recording; optional per-segment `facingYawOverride`.

**Road center spline** (`VehicleRoadCenterSpline`) is an authored Catmull-Rom with start/end/control gizmos. It does not fork civil `RoadNetwork`. **Locomotion → Vehicle Video Steering** (hub Animation / City Planning) binds the recording, spline, `TravelAgent`, and `VehicleActor`.

Projection identity cascade (strict order):

1. Same `trackId` across a cut when the hop kept it.
2. Nearest same `classId` by spline arc-length `s` (ties by bbox centroid). Do not jump to a closer different type.
3. Facing slider last — reprojects the already-bound centroid onto the spline.

The baker (`VehicleSteeringBtBaker`) emits `SeedVehicleVelocityNode` then per-segment `ApplyDrivePhaseNode(Enter)` + `TravelLegDriveNode` waypoints. Drive nodes route steer/throttle/brake stubs through `VehicleInstrumentPhysicsProxy` when bound. See [VEHICLE_INSTRUMENT_PROXY.md](VEHICLE_INSTRUMENT_PROXY.md).

## Cabin camera (in-vehicle) takes

Same `WebcamAnimKind.Vehicle`, plus `cabinCamera` on the recording. YOLO26 cannot see the ego chassis from inside the cabin, so Detect does **not** lock to YOLO-only. The hop is `cabin_composite@v1`:

| Stream | Role |
|--------|------|
| MediaPipe Holistic (or MoCapAnything when `species` is set) | Occupant PoseTrack → ragdoll / `availableAnimations` |
| Polar VO (`polarVelocityPath`) | Ego speed / yaw from windshield optical flow. Seeds `SeedVehicleVelocityNode` via `CabinPolarVelocity.ToSeedSlot` |
| YOLO26 (optional) | **Traffic through glass only.** Empty frames are success. Never `PickPrimarySubject` as self |

Polar JSON per frame: `{ tMs, radialExpand, azimuthalYaw, speedHint, yawRateHint }`. Far-field mask ignores the lower dashboard band. Scene cuts reuse the same HSV splitter as YOLO so polar segments line up when both ran.

**Project** integrates polar `speedHint` along vehicle forward (~2 m samples), optionally snapping to an assigned road-center spline. **Bake** prefers polar for chassis velocity; occupant Transform snaps to `VehicleSeating.occupantAnchors[0]` when present.

**Infer shoulder shifts** (checkbox): shoulder AP minus polar longitudinal accel. Residual forward → throttle stub; residual back → brake stub. Hands still drive steer. Direct foot motion (brake/clutch/accelerator) overrides inferred pedals. Overlay in IK / Vehicle Video Steering shows polar speed, lean, residual, and agree/disagree.

Editor: Vehicle + cabin camera shows pose model popup, facing (window-forward), Infer shoulder shifts, Detect (composite), Project (polar ± spline), Bake.

## USC `type_metadata`

No new library columns. JSON:

```json
{
  "kind": "webcam_anim_recording",
  "webcamAnimKind": "vehicle",
  "model_spec": "yolo26_vehicle@intel",
  "subsection": "takeoff_roll_0",
  "animationListIndex": 3,
  "timelineStartMs": 1200,
  "timelineEndMs": 8400,
  "granularity": "millisecond",
  "targetHint": "magneto_bt",
  "species": "Lion",
  "poseTrackPath": "",
  "vehicleTrackPath": "",
  "polarVelocityPath": "",
  "facingYawDegrees": 0,
  "cabinCamera": false,
  "inferShoulderShifts": false
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

Vehicle video steering writes `laneIndex` on `VehicleTrack` frames/segments and projected waypoints (civil `RoadLaneLayout` or image `cx` bins). See [RoadLanes.md](RoadLanes.md).

WebGL preview of takes stays `/continuuuum_editor/index.html?docId=&apiBase=&subsection=`. Docker/CI for that host: `docker/webgl/` and `.github/workflows/webgl.yml`.

