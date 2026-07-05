# Open/Close Topology Authoring

Hierarchical open/close interactables compile from `OpenCloseTopologyAsset` into locomotion behavior trees with camera stops, ambulation, and optional auto-close branches.

## Quick start

1. **GameObject → Locomotion → Dresser Music Box Demo** — sample scene + topology asset.
2. **Window → Locomotion → Open-Close Topology Preview** — scan hierarchy, tune per-stop arrival blend and auto-close BT mode, compile preview.
3. Assign `OpenCloseSequenceNode.topology` on a BT host; call `RebuildFromTopology()` or use **Compile BT** in the editor.

## Per-stop properties

| Field | Purpose |
|-------|---------|
| `enabledInGameplay` | Uncheck to prune branch for linear play |
| `arrivalBlendCoefficient` | 0 = stop before open; 1 = reach-and-retry |
| `autoCloseBt` | How compiler emits close nodes |
| `approachAnchorWorld` | Pathfinding destination (from concavity scan) |

## Auto-close BT modes

- **None** — open only (music box lid stays open)
- **OnStopExit** — close when actor/camera leaves stop
- **AfterChildren** — close after nested children complete
- **OnSequenceEnd** — reverse close stack at sequence end
- **Manual** — reserved slot for hand-authored close tree

## Lemmas

Built-in verbs: `open`, `close`, plus `unlock`, `latch`, `drawer`, `lid`, `hinge`, `guard`.

Property keys live in `OpenCloseLemmaPropertyKeys.cs`; resolver: `OpenCloseLemmaPropertyResolver`.

## Camera

`OpenCloseCameraSequence` drives `CameraPathingRig` using `OpenCloseCameraStop` poses (concavity center + tangent approach + inward normal blend).

Strict camera/actor sync when `arrivalBlendCoefficient == 0`.

## IK training

Add `PhysicsIKTrainingCategory.Open`, `Close`, or `OpenObject` on training runs; bind via `OpenCloseBeatProfile`.

## Tests

Run `Locomotion.Open.Tests` in Unity Test Runner.
