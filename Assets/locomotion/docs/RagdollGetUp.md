# Ragdoll On-Ground Check + Get-Up BehaviorTree

Default recovery tree merged onto a ragdoll `Brain` when the actor lands fallen, so limp ragdolls stand back up instead of staying on the floor.

## Flag

On `RagdollActor`:

- `enableGetUp` — **true by default**. When false, no get-up tree is merged.
- `getUpBehaviorTreePrefab` — optional override. Null uses `Resources/RagdollGetUpBehaviorTree` or an in-code factory build.

AutoWire / From-Scratch Replicator assign the default prefab at  
`Assets/locomotion/Prefabs/ActorRagdolls/RagdollGetUpBehaviorTree.prefab` when the field is null.

## Merge rules

`RagdollGetUpBootstrap.TryMerge` runs from `RagdollActor` `Awake` / `OnEnable`:

1. Bail if `!enableGetUp` or no `Brain`.
2. Merge at most once (`GetUpMerged` / existing get-up selector root).
3. Instantiate the template under the Brain.
4. If `brain.behaviorTree` is null → assign the new tree.
5. If a tree already exists → set the Selector’s `passthroughChild` to the previous root, then assign the get-up tree as `brain.behaviorTree`.

## Tree shape

```
RagdollGetUpSelector
  ├─ GetUpSequence
  │    ├─ OnGroundAndFallen (condition)
  │    └─ GetUp (pelvis-lift action)
  └─ IdleSuccess (passthrough; or prior BT root after merge)
```

Menu: **Locomotion → Create Ragdoll Get-Up Behavior Tree Prefab** (also writes a Resources copy for runtime `Resources.Load`).

## Thresholds

| Probe | Default | Meaning |
|-------|---------|---------|
| `groundProbeDistance` | `0.35` | Raycast down from pelvis/root (+ small lift) |
| `uprightDotThreshold` | `0.5` | `pelvis.up · world.up` below this ⇒ fallen |
| `standHeightOffset` | `0.5` | Pelvis raise during get-up |
| `standUpSpeed` | `2` | Progress units per second |
| `standTolerance` | `0.08` | Distance to target for Success |

Shared helpers: `RagdollGroundCheck.IsOnGround` / `IsFallen` / `IsOnGroundAndFallen`.

## Get-up motion

First working recover ports the Narrative stand approach: release `FixedJoint`s, zero muscle-group activations, lerp pelvis upward. Muscle-sequence get-up remains future work.
