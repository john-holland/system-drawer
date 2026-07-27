# Combat topology

Topological combat from CQC through vehicle weapons: `CombatCard : GoodSection`, defend wards, typed damage/wound pipeline, cut tools, clothing masks, and proxy instruments (sidewinder / knockoff-F35 safety lock).

## Modes

| Mode | Intent |
|------|--------|
| Cqc | Clinch, strike, grapple break |
| Melee | Slash / stab / block / parry |
| Ranged | Aim / fire / reload / suppress |
| VehicleWeapon | Instrument-proxy fire (safety lock) |
| Explosive | Throw / blast |

Move kinds: Strike, Block, Parry, Dodge, Aim, Fire, Reload, Throw, Slash, Stab, GrappleBreak, Suppress.  
Anim tags: `combat.*` via `CombatAnimationGroup`.

## Cards

`CombatCard` under `pathing/combat/`:

- Targets + `DefendWard` absorb cones
- `CombatImpactSpec` (damage type, limb, through, health mode)
- Prebake / limb health requirements
- `CardInstrumentProxyOptions` (shared with Wrestling/Love) — ~22 N (5 lbf) safety lock + hardware flavor note
- `ConsiderCombatCards` → `PhysicsCardSolver`; `GoalType.Combat`

## Session + planner

| Type | Role |
|------|------|
| `CombatSession` | Participants, time budget, topology goals |
| `CombatPlannerSolver` | Sequence cards for damage/ward/anim goals |
| `CombatPlannerService` | `ITravelRiskPlannerService` id `"combat"` |
| `SafetyLockWardenPlannerService` | Gate fire until lock force met |

Wired after love/consent in `TravelRiskPlannerPipeline`.

## Damage families

`CombatDamageApplier` + handlers: Bullet (through/stop), Slash (open wound, no auto-suture), Electric (jump points), Laser/ContinuousCutter (depth + interval via `CutToolComponent`), Radiation (slow heal burn), Explosion/Gib (detach + SDF cap keys + impulse).

`LimbIntegrityState` (per-limb / overall), `DamageMask` (clothing absorb → tear field), `ClothingDamageLayer`.

## Wound / suture

`WoundSiteRuntime`: spline guestimate, `closeAmount`, parabola coefficient, stitch hold potential (0 and 1 = infinite poles), healed fillet when close=1, smell via `SmellEmitter`.  
`SutureBehaviorNode` BT for pin/drag/stitch progress.

## Editors

- Window/System Drawer/Cards/Combat|Love|Wrestling
- Window/System Drawer/Combat/Damage Types
- Card Planning chips `C:{move}`

## Out of scope

Full gore VFX packs; real F-35 flight model (proxy fire only).
