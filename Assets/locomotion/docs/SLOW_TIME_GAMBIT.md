# Slow-Time Gambit Aperture Selection

Select pathing apertures under slow/stop time, then commit a hierarchical TravelAgent plan with steering enforcement.

## Runtime

- `SlowTimeController` — restore-safe `Time.timeScale`
- `PathingAperture` / `PathingApertureRegistry`
- `GambitSelectionSession` + `AngularTargetSelectMode` + `ApertureHighlightRenderer`
- `GambitSteeringEnforcer` — `enforcement01` 0 (highlight only) … 1 (full control)
- `VehicleGambitPathAsset` / `VehicleGambitPathAuthoring` — stops + spline gizmos; upsert when clearance is narrow

## Narrative / input

| Action | Behavior |
|--------|----------|
| `NarrativeEnterSlowTimeGambitAction` | Start session + slow time |
| `NarrativeChooseGambitApertureAction` | Running until LMB confirm (or cancel) |
| `NarrativeCommitGambitPathAction` | `TravelAgent.previewGoalWorld` + rebuild |

Drivers: `GambitMouseScanDriver`, `GambitMouseClickDriver` (unscaled). BT: `WaitForGambitInputTriggerNode`.

Registered in Narrative Tree editor as EnterSlowTimeGambit / ChooseGambitAperture / CommitGambitPath.

## Advisor

`GambitPhysicsMaterialAdvisor.Suggest` returns friction/bounciness/manifold deltas for easier or harder retries.

## Pass / crash + crowding

`PathingAperture.passMode`: `SelectOnly` (default gambit pick), `AngularPassThrough`, `CrashThrough` (optional `breakable` + `materialHint`).

`ApertureCrowdSampler` updates `crowdOccupancy01` for Stuntman / Safety Warden risk. See `StuntmanSafetyWarden.md`.
