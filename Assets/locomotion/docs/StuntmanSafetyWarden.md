# Stuntman Planner, Safety Warden, and parkour risk stack

## Services

| Component | Role |
|-----------|------|
| `StuntmanPlannerService` | Proposes runway→terminus chains, crash/pass apertures, parkour anim tags within the risk band |
| `SafetyWardenPlannerService` | Gates/rewrites out-of-band legs; hard crowd inflation; prefers walk over window crash |
| `TravelRiskPlannerPipeline` | Order: Stuntman → Safety Warden after `BuildPlan` + Consider enrichers |

Wire on `TravelAgent` (`stuntmanPlanner`, `safetyWardenPlanner`) or as sibling components. Same fields on `CompositeMultiModalPathNode`.

## Risk / safety band

`PlannerHints`: `maxRisk01`, `minRisk01`, `minSafety01`, `maxSafety01` (NaN = unset).

`TravelRiskBand.Resolve`: `safety = 1 - risk`. Example: `maxSafety=0.9` ⇒ `minRisk=0.1`.

Lemma: `{P:safely|riskMin=0.1|safetyMin=0.9}` → `SafetyWardenLemmaPropertyKeys` / `ConsiderSafetyWardenHints.ApplyLemmaHints`.

## Stunt zones

`StuntZone` (`Runway` / `Terminus` / `Both`): `requiredEntrySpeed01`, `allowAnimations`, optional `linkedAperture`.

## Apertures

`PathingAperture.passMode`: `SelectOnly` | `AngularPassThrough` | `CrashThrough`.  
`ApertureCrowdSampler` fills `crowdOccupancy01` for risk.

## Ragdoll markers

`RagdollSectionStrengthMarker` + child `ControlPoint`: `Vulnerable` | `Strong`. Used in damage bias and `PhysicsCardSolver` feasibility.

## Anim groups / IK

- `ParkourAnimationGroup` / `RopeInchwormAnimationGroup` tags
- `PhysicsIKTrainingCategory` parkour + rope inchworm values
- `PhysicsIKTrainingRunAsset.attachSherpaCarry`, `parkourAnimGroupTag`
- `ConsiderRopeCards` emits inchworm cards when enabled

## Land animation BT IK

Landing tags from `ParkourDamageMinAnimSelect.SelectLanding` (`parkour.spring_landing`, `one_leg_landing`, `one_hand_landing`, `fall_rolls`) are consumed at Acrobatics legs:

| Piece | Role |
|-------|------|
| `ABTClipConfig.landPrep` | Authoring: `GoalType.Land` template + `LandImpactCurve` (keyframes with `isImpact`) |
| `ParkourLandAnimationDriver` | Runtime: `PlayLanding`, `SampleImpact01`, `showGizmo` (default true) |
| `PrepareLandAnimationNode` | Prepended on Acrobatics when tag is a landing tag; sets tree goal + driver |
| `TravelExecutionContext.animationGroupTag` | Carries segment tag into travel activation nodes |

Gizmo: when a land `PhysicsIKTrainingCategory` is selected on `RagdollIKAnimationManager` and `showGizmo` is true, the driver draws an example landing goal sphere, approach arc, and orange ticks at impact keyframes. Call `LandImpactCurve.EnsureExampleCurve()` for a default mid-contact impact key.

Impact attenuation hook: `ParkourLandAnimationDriver.ScaleAttenuationByImpact(base, impact01)`.

## Emergence

`StuntPlanEmergenceSource` + `StuntPlanEmergenceBuffer`: broccoli branching plume from accepted + rejected forks; fades with age.

## See also

- `SLOW_TIME_GAMBIT.md` — aperture selection under slow time
