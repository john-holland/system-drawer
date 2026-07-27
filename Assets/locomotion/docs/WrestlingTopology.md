# Wrestling topology

Topological wrestling on `GoodSection` / `PhysicsCardSolver` with temporary tow links between ragdolls, slow-time card pick, and UV-masked cloth slide.

## Modes

| Mode | Intent | Preferred moves |
|------|--------|-----------------|
| Play | Sport / kayfabe | Lunge, Lock, Lift→Throw, DropOn, Counter (`.pro` anims) |
| Subdue | Control / restrain | Pull, Push, Lock, Pry, Block |
| Pin | Hold for count | Lock → pin pressure; DropOn; surface normal |

Tags use `wrestling_*` — **not** `rope_grapple` semantics.

## Cards

`WrestlingCard : GoodSection` under `pathing/wrestling/`:

- `mode`, `moveKind`, `professionalStyle`
- `requiredLimbBones` / `optionalLimbBones`
- `WrestlingBodySizeGate` (mass + extents; mass band **or** ratio band)
- Branches: `liftBranch`, `throwBranch`
- Slow-time: `hotkey`, `inputActionName`, `aimAnchorOverride`

Factories: `GenerateLunge`, `GenerateLock`, `GenerateThrow`, `GenerateLift`, `GenerateDropOn`, `GenerateCounter`.

### Size gate examples

- **Superman suplex truck** — high `maxOpponentMass` and/or low `minActorToOpponentMassRatio`.
- **Rock cannot (easily) suplex a baby** — high `minOpponentMass` (default Throw/Lift/DropOn use `minOpponentMass = 25`).
- **Gandhi** — limits remain an open research question; author gates per card.

## Topology

`WrestlingTopologyRuntime`:

1. `BeginLock(actor, opponent, card)` — `IkTowLink` between required limbs ↔ opponent chest/hips
2. `UpdatePin(normal, surface)` — CoG press bias for Pin
3. `EndExchange()` — tear down; notifies cloth driver

## Cloth UV slide

Extends the rope strain cache pattern (`RopeRadialStrainCache` / `Locomotion/RopeStrainRadial`) for garments:

| Piece | Role |
|-------|------|
| `ClothUvStretchLayer` | Renderer, masks, `ClothElasticProperties` |
| `ClothUvStretchCache` | R=strain, G=slipU, B=slipV, A=contact |
| `ClothUvStretchDriver` | FixedUpdate integrate + bind `_ClothStretchTex` |
| `Locomotion/ClothStrainSlide` | UV offset + optional extrude |

Masks: `_SlideMaskTex` (R slide vs stick), `_ElasticMaskTex` (elastic scale). Lock/Pin contacts raise A so singlets slide under grapples; Play + professional recovery snaps fabric cleaner.

Rope path unchanged.

## Consider + BT

- `ConsiderWrestlingCards` → `PhysicsCardSolver.AddCards`
- `GoalType.Wrestling` matched in `PhysicsCardSolver`
- `WrestleObjectNode` — Solve → BeginLock → Execute → EndExchange
- `WaitForWrestlingCardSelectNode` — unscaled confirm before wrestle

## Planner

- `WrestlingPlannerService` — enrich stamina/opponent; expand Lift/Throw; stamp anim tags
- `RefereeWardenPlannerService` — soft-gate high-damage Play; Subdue allows control holds

Wired on `TravelAgent` / `CompositeMultiModalPathNode` via `TravelRiskPlannerPipeline` after Stuntman/Warden.

## Slow-time selection

Clone of gambit aperture flow ([SLOW_TIME_GAMBIT.md](SLOW_TIME_GAMBIT.md)):

1. Enter → filter feasible cards → `WrestlingCardSelectionSession.Begin`
2. `SlowTimeController` (~0.2–0.35)
3. Angular cone over card aim anchors (`AngularWrestlingCardSelectMode`)
4. Button map (`WrestlingMoveInputBindings`) — e.g. West→Lock, North→Lift, South→Counter, East→Throw
5. Confirm → commit into `WrestleObjectNode`; cancel restores timescale

Narrative: `NarrativeEnterSlowTimeWrestlingAction` / `NarrativeChooseWrestlingCardAction` / `NarrativeCommitWrestlingCardAction`.

## Bio-rhythm narrative

`NarrativeWrestlingBioRhythmAction` — LifeSystems adrenaline + `BioRhythmClock` amplitude spike; optional `GoalType.Wrestling` queue.

Lemma keys: `WrestlingLemmaPropertyKeys` (`wrestling-mode`, `wrestling-move`, `wrestling-pro`).

## Animation tags

`wrestling.lunge` … `wrestling.counter`; append `.pro` when `professionalStyle`. Matching `PhysicsIKTrainingCategory` wrestling entries exist for tag-driven runs.
