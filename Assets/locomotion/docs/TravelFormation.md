# Travel formation assets and multibody pathing

## Overview

`TravelFormationAsset` stores an **ordered list of slot offsets** in formation-local space:

- **X** maps to world **right** (relative to flattened travel forward on XZ).
- **Y** maps to world **up**.
- **Z** maps to **travel forward** (flattened plan direction).

At rebuild time, `TravelFormationPathOffset` adds a **constant world offset** to Walk / Drive / Fly segment waypoints when:

1. `TravelAgent.multibody` references a formation asset with at least one slot, and  
2. `TravelAgent.multibodyFormationGroupId` is non-empty.

Then `TravelMultibodyPathAdjuster` runs as before on that shifted polyline. Cached **pre-multibody-relax** plan is the **post-formation** polyline (solver output plus formation offset).

## JSON schema (`JsonUtility`)

Use a **wrapper object** (Unity cannot deserialize a bare JSON array as root):

```json
{
  "version": 1,
  "slots": [
    { "x": 0, "y": 0, "z": 0 },
    { "x": 1.2, "y": 0, "z": 0 }
  ]
}
```

Load at runtime with `TravelFormationJsonLoader.TryParseSlots` or apply to an asset via `TryApplyToAsset`.

## Editor menus

- **Locomotion → Travel → Bake formation from transform** — Select a root transform; saves a `TravelFormationAsset` with each descendant’s position expressed in the root’s space (depth-first, sibling order).  
- **Locomotion → Travel → Import formation JSON to asset** — Creates an asset from a `.json` file.

## Squad / slots / wrap

- **`multibodyFormationGroupId`**: agents with the same non-empty string form one cohort (stable sort by `GetInstanceID()` for auto slot order).  
- **`formationSlotIndex`**: when `>= 0`, used as the cohort index for slot/wrap math; when `-1`, index is the agent’s position in the sorted cohort.  
- If cohort index `i` exceeds slot count `M`, **`i % M`** picks the slot pattern and **`i / M`** is the **wrap row**.  
- **`TravelAgentMultibodySettings.formationWrapDirection`** defaults to **Back**: row `r > 0` adds **`-travelForward * rowSpacing * r`**. **Left** / **Right** add lateral row offsets.  
- Row spacing: `formationWrapRowSpacing` if set, else the asset’s `defaultWrapRowSpacing`, or **`clearanceRadius * 2`** when **`formationRowSpacingUsesClearance`** is enabled.

## Optional peer filter

When **`limitMultibodyPeersToSameFormationGroup`** is true and this agent has a non-empty group id, multibody relaxation **only** sees peers in that group. Leave off (default) to continue avoiding **all** registered `TravelAgent` peers.

## Notes

- Formation offset is an **MVP constant** along the route (not re-projected per-waypoint tangent yet).  
- Brain / `RagdollAnimationSetManager` gating is unchanged; this affects `TravelAgent` cached plans only.
