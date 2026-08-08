# Ragdoll From-Scratch Replicator

Editor pipeline: select a humanoid source ragdoll → clone skin/mesh + physics → strip custom components → wire System-Drawer actor stack → optional prefab.

## Menu

**Window → System Drawer → Ragdoll → From-Scratch Replicator**

Also: `RagdollServiceWizard` inspector → **Open From-Scratch Replicator**.

## Steps

1. Assign a source with a humanoid `Animator` + Avatar.
2. Options: copy colliders, copy joint limits/drives, strip Animator controller (Avatar kept), register-as key.
3. **Replicate In Scene** or **Replicate And Save Prefab** (default folder `Assets/locomotion/Prefabs/ActorRagdolls/`).

## What is kept vs stripped

**Kept / copied:** transform hierarchy, `SkinnedMeshRenderer` / `MeshRenderer` / materials, `Animator`, `Rigidbody`, joints, colliders (when enabled), **finger stack** (`RagdollFinger` / `RagdollDigit` / `RagdollNailbed`), **hair runtime** drivers, and **`RagdollLimbCapsuleFit`** (per-limb capsule center/euler offsets — Fitting Wizard → Hand capsule fit, or inspector +90° buttons). Hands rebind `fingers` / `digits` lists after `FindOrAddHands`.

**Not copied:** other donor gameplay / locomotion / third-party `MonoBehaviour`s. They are destroyed on the clone and listed under **Components not brought into revamped ragdoll** (hierarchy path → assembly-qualified type names). Use **Copy Leftover JSON**, **Log Leftovers**, or **Clear** in the window. The map is also returned on `RagdollFromScratchResult.leftovers`.

**Recreated clean:** `BoneMap`, `RagdollSystem`, `NervousSystem`, `PhysicsCardSolver`, `WorldInteraction`, head-centroid `Brain` + dual `LSTMPredictor` children, `Hair/` plume runtime, animation managers + `AnimationRoot`, sensors/ears, muscles/groups, body parts via `FindOrAdd*`, `RagdollActor`, `RagdollServiceWizard`.

## Expected prefab shape

- Root: `RagdollActor` + `RagdollSystem` + `BoneMap` + locomotion core + `RagdollServiceWizard` (`ragdollRoot` = self)
- Humanoid bones: RB / ConfigurableJoint / Muscle (and colliders when copied)
- `Head/Brain/` with `LeftLSTM` + `RightLSTM` (`LSTMPredictor`), `enableDualLSTM` on
- `Hair/` with `HairPlumePhysicsDriver` + `HairBodyCapsuleBinder` (scalp → Head)
- `AnimationContainer/` + `Default_animation_tree` (or `{name}_animation_tree`) with `AnimationRoot`
- `RagdollAnimationSetManager` + `RagdollIKAnimationManager` on the actor
- `Senses/` eyes, nose, ears; `MuscleGroups/`

## Brain + Repair Ragdoll

`RepairRagdoll` (Fitting Wizard button) runs:

1. Joint/collider floor repair  
2. `EnsureBrainWithDualLstm` — Brain under head + Left/Right LSTM  
3. `EnsureHairRuntime` — Hair driver/binder + scalp bind (+ default config if missing)  
4. `EnsureAnimationRoots` — animation set/IK managers, `AnimationContainer`, `AnimationRoot` nodes, sync selected trees  

The Replicator calls these again after `FindOrAdd*` so head/scalp resolve correctly.

## Related

- [Ragdoll Fitting Wizard](../Editor/RagdollFittingWizardWindow.cs) — fit/wire an existing actor in place; **Repair Ragdoll**
- `RagdollAutoWire`, `RagdollPhysicsCopy`, `RagdollComponentStripper`
- [Ragdoll Get-Up BT](RagdollGetUp.md) — default on-ground check + get-up Selector merged onto Brain when `RagdollActor.enableGetUp` is true
