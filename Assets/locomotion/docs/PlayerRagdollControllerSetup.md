# Ragdoll first / third person player (behavior tree)

This walkthrough wires the locomotion **BehaviorTree** player loop (legacy `Input` axes, optional mouse look, motor impulses, optional Animator parameters) to a **Brain** on a ragdoll actor, and optionally uses **PlayerVocabBuiltIn** to assign a first- or third-person tree prefab.

## 1. Actor components

On the actor root (or the object that already hosts locomotion):

1. **RagdollSystem**, **NervousSystem**, **PhysicsCardSolver**, **RagdollAnimationSetManager**, **Brain**.
2. Add **BehaviorTree** with **decisionTime = 0** so the tree runs every frame when the Brain updates.
3. **RagdollAnimationSetManager**: for gameplay-driven locomotion trees on **Brain**, playback must not be stopped, or `Brain` will skip `behaviorTree.Execute()` when `IsPaused` or `IsStopped` is true. Start or unpause animation playback as your project requires, or keep the manager in a state that allows Brain execution.

## 2. Player tree prefab

Use **Locomotion → Create Player Ragdoll Behavior Tree Prefabs** to generate:

- `Assets/locomotion/Prefabs/PlayerRagdoll/FirstPersonRagdollControllerBehaviorTree.prefab`
- `Assets/locomotion/Prefabs/PlayerRagdoll/ThirdPersonRagdollControllerBehaviorTree.prefab`

Each root contains **RagdollPlayerInputBuffer**, **BehaviorTree**, **RagdollPlayerSequenceNode** (as `rootNode`), and child nodes: read input → look/orbit → apply locomotion → drive animation.

## 3. First person camera hierarchy

Typical hierarchy:

- Body / yaw root (Y rotation only)
  - Camera pitch child (receives pitch)
    - **Camera**

On the **MouseLookFirstPersonNode**:

- Assign **yawRoot** to the body transform.
- Assign **pitchTransform** to the camera (or an intermediate pitch object).

## 4. Third person orbit

On **ThirdPersonCameraOrbitNode**:

- **followTarget**: ragdoll root or upper spine.
- **pivot**: empty transform at the character (orbit center).
- **cameraTransform**: the scene camera.

Tune **orbitDistance** and pitch limits on **RagdollPlayerInputBuffer.options**.

## 5. Locomotion and facing

**ApplyRagdollLocomotionNode** uses **facingReference** for movement direction (horizontal plane). First person: same as **MouseLookFirstPersonNode.yawRoot**. Third person: often **followTarget** or the ragdoll forward.

## 6. PlayerVocabBuiltIn (optional wiring helper)

Add **PlayerVocabBuiltIn** next to **Brain** (or on a child):

- **targetBrain**: leave null to use `GetComponentInParent<Brain>()`.
- **firstPersonTreePrefab** / **thirdPersonTreePrefab**: assign the generated prefabs (or scene prototypes).
- **defaultPerspective**: First or Third person.

At runtime it instantiates the chosen template under **behaviorTreeParent** (defaults to the brain transform) and assigns **Brain.behaviorTree** if it was null. If you already assigned a tree in the inspector, it will not replace it.

If you assign **firstPersonTreePrefab** / **thirdPersonTreePrefab** from code after `AddComponent<PlayerVocabBuiltIn>`, call **`RefreshWiring()`** once fields are set (Awake/OnEnable run before your assignments when using `AddComponent`).

## 7. Built-in vocabulary (Continuuuum)

Registry lemmas **player**, **first-person**, **third-person** (subject, tags `controller`, `spatial`, `player`) support authoring and narrative tooling. **BuiltInSynonyms.TryCanonicalizeMultiWordPhrase** maps `first person` / `third person` to `first-person` / `third-person`.

## 8. Input system note

Nodes use **UnityEngine.Input** legacy axes (`Horizontal`, `Vertical`, `Mouse X/Y`, `Jump`) to match `Assets/Misc/Scripts/FirstPersonController.cs`. Migrating to the new Input System can be done later inside **ReadRagdollPlayerMovementInputNode** without changing the rest of the tree.

## 9. Multiplayer

**RagdollPlayerInputBuffer** is per-actor; avoid static input state so multiple local or networked players can each carry their own buffer and tree instance.
