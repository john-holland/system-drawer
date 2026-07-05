# How-to: Interpret and play — *"The player idles in third person."*

This walkthrough covers the **systems you need in the scene**, **editor steps to interpret the prompt**, and **how to get third-person idle playback** on a ragdoll player. It reflects the current Drawer 2 / Locomotion / Continuuuum stack.

## What the prompt means in this project

| Phrase | Built-in / runtime role |
|--------|-------------------------|
| **player** | Continuuuum built-in lemma (`VocabularyBuiltInRegistry`). Should also be registered in **SceneObjectRegistry** so narrative bindings can resolve to your actor GameObject. |
| **third person** | Built-in lemma `third-person` (synonym: `third person`). Drives **third-person camera + control** via **PlayerVocabBuiltIn** + `ThirdPersonRagdollControllerBehaviorTree` prefab — not automatic from LSTM alone. |
| **idles** | **Not** a Continuuuum built-in lemma today. Idle playback uses **no movement input** on the player BT and/or **NarrativeChangeAnimationAction** (`AnimationState.Idle`) on the calendar event. |

**Important:** `NarrativeLSTMPromptInterpreter` decodes each event’s **title from LSTM vocab indices** (often a single token), not a full grammatical parse of the sentence. Treat interpretation as **authoring assistance**; you may need to **edit the calendar event** after interpret to match intent.

---

## Systems overview

```mermaid
flowchart TB
  subgraph interpret [Interpretation]
    PromptAsset["NarrativePromptAsset"]
    Interpreter["NarrativeLSTMPromptInterpreter"]
    ORM["SceneObjectRegistry"]
    BuiltIn["VocabularyBuiltInLookup"]
    PromptAsset --> Interpreter
    Interpreter --> ORM
    Interpreter --> BuiltIn
  end

  subgraph play [Playback]
    Calendar["NarrativeCalendarAsset"]
    Scheduler["NarrativeScheduler"]
    Executor["NarrativeExecutor"]
    Bindings["NarrativeBindings"]
    Calendar --> Scheduler --> Executor
    Bindings --> Executor
  end

  subgraph actor [Player actor]
    Brain["Brain"]
    Vocab["PlayerVocabBuiltIn"]
    BT["ThirdPersonRagdollControllerBehaviorTree"]
    Brain --> BT
    Vocab --> BT
  end

  Interpreter -->|"ApplyToCalendar"| Calendar
  Executor -->|"actions / tree"| actor
```

You need all three layers for end-to-end “prompt → play”:

1. **Interpretation** — prompt asset, ONNX model + vocab, interpreter, ORM.
2. **Narrative playback** — calendar, clock, scheduler, executor, bindings.
3. **Player embodiment** — ragdoll, brain, third-person BT, optional animator idle.

---

## Part A — Stand up core systems (one-time per scene)

### A1. Narrative rig (calendar + executor)

On a dedicated GameObject (e.g. `NarrativeRig`):

1. Add **NarrativeDemoRig** (auto-adds **NarrativeBindings**, **NarrativeClock**, **UnityNarrativeTimeProvider**, **NarrativeExecutor**, **NarrativeScheduler**), **or** add those components manually.
2. Add **NarrativeCalendarAsset** on the same object (or a child).
3. Assign **NarrativeDemoRig.calendar** → your calendar.
4. On **NarrativeScheduler**: set **calendar**, **clock**, **executor**; leave **positionKeys** containing `"player"` if you use 4D volumes later.

Optional: **NarrativePromptServiceWizard** on the rig → inspector **Create LSTM prompt rig** (adds interpreter, summarizer, UI).

Menu alternative: **Window → System Drawer → Narrative → Calendar Wizard** to inspect events after you add them.

### A2. LSTM prompt interpreter

On the narrative rig (or child):

1. **NarrativeLSTMPromptInterpreter**
   - **modelPath**: `NarrativeLSTM/narrative_prompt_interpreter.onnx` (under `StreamingAssets`, or assign **modelAsset** in editor).
   - **vocabPath**: `NarrativeLSTM/vocab` (JSON in `StreamingAssets` or **vocabAsset** TextAsset).
   - **calendar**: your **NarrativeCalendarAsset**.
   - **sceneObjectRegistry**: registry on the scene (see A3).

2. Confirm **Is Ready** in play mode (vocab + model loaded). If missing, use **Locomotion → Narrative → Export for LSTM training...** to export calendars, train externally, then place ONNX + vocab under `Assets/StreamingAssets/NarrativeLSTM/`.

3. Optional: **NarrativeLSTMUI** for an in-game Interpret button (runtime debug).

Requires **Unity Barracuda** (`UNITY_BARRACUDA`) for inference.

### A3. Scene object registry (ORM)

On a scene object (e.g. `SceneORM`):

1. **SceneObjectRegistry**
2. Register the player actor under **references**:
   - **key**: `player`
   - **reference**: player root GameObject
   - **synonyms**: `player`, `Player` (optional)

Assign this registry to **NarrativeLSTMPromptInterpreter.sceneObjectRegistry**.

Without this, bindings may stay **UnderstoodNoOrmMatch** even when built-ins like `player` are recognized lexically.

### A4. Narrative bindings (play targets)

On **NarrativeBindings** (same rig as executor):

| key | value |
|-----|--------|
| `player` | Player root GameObject (or `Brain` / `RagdollSystem` component) |
| `animator` | Animator on ragdoll (if using **NarrativeChangeAnimationAction**) |

**RebuildIndex** runs on enable; edit the list in the inspector.

### A5. Player ragdoll + third-person behavior tree

See also [PlayerRagdollControllerSetup.md](PlayerRagdollControllerSetup.md).

**Prefabs**

1. Menu: **Locomotion → Create Player Ragdoll Behavior Tree Prefabs**
2. Produces:
   - `Assets/locomotion/Prefabs/PlayerRagdoll/ThirdPersonRagdollControllerBehaviorTree.prefab`

**Actor root** (your player character):

| Component | Notes |
|-----------|--------|
| **RagdollSystem**, **NervousSystem**, **PhysicsCardSolver** | Locomotion core |
| **RagdollAnimationSetManager** | If stopped/paused, **Brain** skips BT execution |
| **Brain** | Runs **BehaviorTree** each frame |
| **PlayerVocabBuiltIn** | **defaultPerspective** = **ThirdPerson**; assign **thirdPersonTreePrefab** |
| **BaseAmbulatingActor** | Optional marker; used by travel/multibody tooling |

**PlayerVocabBuiltIn**

- **thirdPersonTreePrefab** → `ThirdPersonRagdollControllerBehaviorTree.prefab`
- **targetBrain** → your **Brain** (or leave null for parent lookup)
- At runtime, instantiates the tree and assigns **Brain.behaviorTree** if empty.

**Third-person camera** (on prefab instance in scene):

- **ThirdPersonCameraOrbitNode**: assign **followTarget**, **pivot**, **cameraTransform**
- **ApplyRagdollLocomotionNode**: set **facingReference** (often follow target or ragdoll forward)

**Idle behavior (locomotion BT)**

- With **no WASD input**, **DriveLocomotionAnimationNode** drives low **Speed** / **IsMoving** false — looks idle if the Animator controller supports it.
- Ensure **RagdollPlayerControllerOptions.enableAnimations** is true on **RagdollPlayerInputBuffer**.

**Brain gating**

- **RagdollAnimationSetManager** must not be paused/stopped if you rely on Brain to tick the player tree.

---

## Part B — Editor: interpret the prompt

### B1. Create a prompt asset

1. Project window: **Create → Locomotion → Narrative → Prompt Asset**
2. Set **originalText** to:

   ```text
   The player idles in third person.
   ```

3. Save (e.g. `Assets/.../PlayerIdleThirdPerson.prompt.asset`).

### B2. Run interpretation

**Option 1 — Interpretation Examiner (recommended)**

1. **Window → System Drawer → Narrative → Interpretation Examiner**
2. Assign **Interpreter** → your **NarrativeLSTMPromptInterpreter**
3. Assign **Prompt asset** → your asset
4. Click **Interpret this asset**

**Option 2 — Inspector**

- Select the interpreter; call **Interpret** from custom tooling or a small test script.

**Option 3 — Play mode UI**

- **NarrativeLSTMUI** → enter prompt → **Interpret** (does not use the asset; good for quick tests).

### B3. Read results

In the Examiner:

- **Events and bindings** — event **title** (from LSTM vocab), **status** (`BuiltInLexeme`, `OrmMatched`, `UnderstoodNoOrmMatch`, etc.), **resolved key**
- **Fill missing links** — retries ORM word lookup for unmatched phrases

**Prompt Tree Inspector** (optional):

- If the scene has **SpatialGenerator4D**, use **Open in Prompt Tree Inspector** to visualize phrase spans against the prompt.

### B4. Apply to calendar

1. On **NarrativeLSTMPromptInterpreter**, call **ApplyToCalendar** (add a temporary editor button, or invoke from Examiner/custom menu if exposed).
2. Or **manually** add a **NarrativeCalendarEvent** in **Calendar Wizard** matching your intent (often clearer for this prompt).

Set event fields for immediate play:

- **startDateTime** — now (or past) so **NarrativeScheduler** fires on enable
- **durationSeconds** — `0` for instantaneous, or several seconds for a held idle beat
- **title** — e.g. `player idle third-person` (authoring label; LSTM title may differ)

---

## Part C — Editor: wire playback for idle + third person

Interpretation **does not** automatically switch camera mode or play idle. Add **actions** (or a **NarrativeTreeAsset**) on the calendar event.

### C1. Third person (required for “in third person”)

**Recommended (scene-level, always on for this demo)**

- **PlayerVocabBuiltIn.defaultPerspective** = **ThirdPerson** before play (see A5).

**Optional (narrative-driven)** — small custom action or script on the event that calls:

```csharp
playerVocab.ApplyPerspective(RagdollPlayerPerspective.ThirdPerson);
```

There is no stock **NarrativeActionSpec** for perspective today; scene wiring is the reliable path.

### C2. Idle (required for “idles”)

**Path 1 — Locomotion BT only (simplest)**

- Ensure no input; third-person tree runs **ReadMovementInput → Camera → ApplyRagdollLocomotion → DriveLocomotionAnimation**.
- Player stands still with idle-style animator parameters.

**Path 2 — Calendar action**

On the calendar event, add to **actions**:

1. **NarrativeChangeAnimationAction**
   - **animatorKey**: `animator`
   - **animationState**: **Idle**
   - **parameterName**: your controller’s idle trigger/bool/state name (if not the default `"Idle"`)

Requires **NarrativeBindings** `animator` entry (see A4).

### C3. Tie event to player ORM key

If interpret produced **OrmMatched** / **positionKeys** with `player`, the scheduler can use 4D checks. For a simple time-only event, set **startDateTime** ≤ clock **Now** and leave **spatiotemporalVolume** empty.

---

## Part D — Play in the editor

1. Open the scene with narrative rig + player + registry + interpreter.
2. Enter **Play Mode**.
3. Confirm **NarrativeLSTMPromptInterpreter** loaded model/vocab (no warnings).
4. **NarrativeScheduler** should trigger the calendar event → **NarrativeExecutor** runs **actions** / **tree**.
5. Observe:
   - Third-person camera orbit (if prefab references assigned)
   - Player not moving without input (idle)
   - Optional animator idle trigger from **NarrativeChangeAnimationAction**

**Debug**

- **NarrativeExecutor.debugLogging** = true
- **NarrativeScheduler.debugLogging** = true
- **Brain** + **RagdollAnimationSetManager** not blocking BT

---

## Quick validation checklist

- [ ] `StreamingAssets/NarrativeLSTM/` model + vocab present (or assets assigned)
- [ ] **SceneObjectRegistry** has `player` → actor
- [ ] **NarrativeBindings** has `player` (and `animator` if using animation action)
- [ ] **ThirdPersonRagdollControllerBehaviorTree** prefab created and wired on **PlayerVocabBuiltIn**
- [ ] Camera / pivot / follow assigned on **ThirdPersonCameraOrbitNode**
- [ ] Calendar event start time ≤ narrative clock now
- [ ] Event has actions or player already in third-person idle via BT

---

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Interpret returns no events | Model/vocab not loaded; check console for `[NarrativeLSTMPromptInterpreter]` |
| Title is one odd token, not the sentence | Expected LSTM decode behavior; edit calendar event manually |
| `player` not resolved | Missing **SceneObjectRegistry** entry or synonym |
| `third-person` shows BuiltInLexeme only | Built-in matched; still wire **PlayerVocabBuiltIn** for gameplay |
| Player moves in first person | **PlayerVocabBuiltIn** still on FirstPerson or tree not assigned |
| Brain does nothing | **RagdollAnimationSetManager** paused/stopped |
| Event never fires | Start time in future; or 4D volume + positionKeys fail region test |
| Idle animation ignored | Wrong **parameterName**; missing **animator** binding |

---

## Related docs and menus

| Resource | Location |
|----------|----------|
| Player BT setup | [PlayerRagdollControllerSetup.md](PlayerRagdollControllerSetup.md) |
| Create player prefabs | **Locomotion → Create Player Ragdoll Behavior Tree Prefabs** |
| Interpretation UI | **Window → System Drawer → Narrative → Interpretation Examiner** |
| Calendar UI | **Window → System Drawer → Narrative → Calendar Wizard** |
| LSTM export | **Locomotion → Narrative → Export for LSTM training...** |
| Built-in lemmas | `Assets/Continuuuum/VocabularyBuiltInRegistry.cs` (`player`, `third-person`) |

---

## Minimal shortcut (skip LSTM)

If the model is not trained yet, you can still **play** the intent without interpretation:

1. Complete **Part A** (player third-person prefab + brain).
2. Add one **NarrativeCalendarEvent** titled `player idle third-person`, start time = now.
3. Optionally add **NarrativeChangeAnimationAction** (Idle).
4. Press Play.

Use the full **Part B** pipeline when the LSTM assets are available and you want binding/status feedback in the **Interpretation Examiner**.
