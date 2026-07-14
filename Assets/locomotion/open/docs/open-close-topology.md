# Open/Close Topology Authoring

Hierarchical open/close interactables compile from `OpenCloseTopologyAsset` into locomotion behavior trees with camera stops, ambulation, auto-close branches, animation-gated joint drive, and beat message events.

## Quick start

1. **GameObject → Locomotion → Dresser Music Box Demo** — sample scene + topology asset.
2. **Window → Locomotion → Object Open-Close Topology** — scan hierarchy, tune per-stop drive/beat fields, **Bake Steps to BT**.
3. Assign `ObjectOpenCloseTopologyPlanNode.topology` on a BT host; call `BakeFromTopology()` (or use the editor bake button).

`OpenCloseSequenceNode` remains as a compatibility shim and delegates bake/execute to the PlanNode.

## ObjectOpenCloseTopologyPlanNode

Generic BT plan node (`NodeType.Sequence`) that:

- Owns `OpenCloseTopologyAsset` + lemma overrides + camera sequence
- Bakes durable `Stop_{nodeId}` children via `OpenCloseTopologyBtBuilder`
- Walks stops with arrival gating, open/unlock, and auto-close modes
- With `persistBakedSteps` (default true), reuses editor-baked Stop_* children at runtime

### Editor bake

**Bake Steps to BT** on the Object Open/Close Topology window:

1. Assign Topology Asset + PlanNode Host (or Scene Root)
2. Edit per-stop beat profile fields (drive mode, sounds, dialogue, quest, UI)
3. Click **Bake Steps to BT** — persists child BT nodes on the host and dirties the scene

## Per-stop properties

| Field | Purpose |
|-------|---------|
| `enabledInGameplay` | Uncheck to prune branch for linear play |
| `arrivalBlendCoefficient` | 0 = stop before open; 1 = reach-and-retry |
| `autoCloseBt` | How compiler emits close nodes |
| `approachAnchorWorld` | Pathfinding destination (from concavity scan) |
| `beatProfile` | Drive mode, animation refs, sound/dialogue/quest/UI/music |

## Drive modes (`OpenableJointDriver`)

| Mode | Behavior |
|------|----------|
| Physics | Hinge motor / kinematic angle (legacy) |
| Animation | Progress gated by animator float or `SetAnimationProgress`; joint follows `Open01` |
| Hybrid | Animation when refs/animator present; otherwise physics/kinematic |

`Open01` (0..1) is the radial/mechanical progress signal for instrument consumers (`AttenuatedOpenClose`, `InstrumentCaseTopology`).

## Auto-close BT modes

- **None** — open only (music box lid stays open)
- **OnStopExit** — close when actor/camera leaves stop
- **AfterChildren** — close after nested children complete
- **OnSequenceEnd** — reverse close stack at sequence end
- **Manual** — reserved slot for hand-authored close tree

## Beat message bus

`OpenCloseBeatMessageBus` raises typed events on open/close:

| Channel | Source fields |
|---------|---------------|
| Sound | `soundOpen` / `soundClose` |
| Dialogue | `dialogueSpanRef` |
| Quest | `questHintKind` + `questObjectiveId` (including Note) |
| UI | `uiMessageId` / `uiMessageText` / `uiCloseMessageText` |
| Music | `playMusicOnOpen` / `musicPlan` / `musicActiveLeafId` (no hardcoded music-box leaf) |

Add `OpenCloseBeatMessageRouter` in the scene to fan events into `QuestRunner`, `CausalityMusicBridge`, and dialogue notes.

## Instrument case consumer

Audio must not reference Open (Runtime already references Audio). Use [`InstrumentOpenCloseBridge`](../InstrumentOpenCloseBridge.cs) in Open:

- Syncs `OpenableJointDriver.Open01` → `InstrumentCaseTopology.SyncFromOpen01` / `AttenuatedOpenClose.SyncFromOpen01` / keyboard lid
- Subscribes to beat bus Open/Close for case state
- `BakePlan()` assigns topology asset onto `ObjectOpenCloseTopologyPlanNode`

`KeyboardInstrumentSim.keyboardOpenCloseTopology` stays a `ScriptableObject` slot (assign `OpenCloseTopologyAsset` in the inspector).

## Lemmas

Built-in verbs: `open`, `close`, plus `unlock`, `latch`, `drawer`, `lid`, `hinge`, `guard`.

Property keys live in `OpenCloseLemmaPropertyKeys.cs`; resolver: `OpenCloseLemmaPropertyResolver`.

Lemma overrides for `autoCloseBt`, `openAngleDeg`, `driveMode`, and `requireToolLemma` are applied at bake time by `OpenCloseTopologyBtBuilder`.

## Camera

`OpenCloseCameraSequence` drives `CameraPathingRig` using `OpenCloseCameraStop` poses (concavity center + tangent approach + inward normal blend).

Strict camera/actor sync when `arrivalBlendCoefficient == 0`.

## IK training

Add `PhysicsIKTrainingCategory.Open`, `Close`, or `OpenObject` on training runs; bind via `OpenCloseBeatProfile`.

## Tests

Run `Locomotion.Open.Tests` in Unity Test Runner (includes PlanNode bake, beat bus, and animation drive tests).
