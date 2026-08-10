# Feature Budget

System Drawer Feature Budget profiles per-feature CPU frame cost, ranks features by importance, and in **Auto** mode reduces **aesthetic granularity** through **ratio-locked scalars** shared with the Planetary Composition UI.

## Overview

- **Budget metric:** rolling average total frame CPU ms (`FrameTimingManager`)
- **Attribution:** `PerfTrace` rough scope prefixes per feature
- **Control modes:** Off / Manual / Auto per feature
- **Auto behavior:** warn, then enter **BudgetMode** and step granularity down on lowest-importance features first

## Ratio-linked granularity

When a field is **ratio-locked** to planet anchor **R**:

```
effectiveValue = ratio × anchorR × granularityLevel
```

Unlocked fields use `manualOverride` only after supplying a **non-empty unlock reason**.

Budget-governed fields (horizon, SDF, weather shell, lava/mantle) default to locked. The governor never unlocks ratios—it only scales within locked ratios.

## Setup

1. **GameObject → System Drawer → System Drawer Hub** — creates `FeatureBudgetRuntime` and a default profile asset under `Assets/SystemDrawer/FeatureBudget/`.
2. Or open **Window → System Drawer → Diagnostics → Feature Budget** and assign/create a profile.
3. Ensure a `PlanetBody` is in scene; it registers ratio readings on play via `PlanetRatioSource`.

## Composition UI integration

**Window → System Drawer → Planet → Composition UI**

- Budget-governed ratio toggles stay disabled until an unlock reason is entered.
- **Sync to Feature Budget** pushes wizard readings into the active `FeatureBudgetProfile`.
- **Push To Profiles** also snapshots `ratioModel` on `PlanetBody`.

## Phase 1 consumers

| Feature | Hook |
|---------|------|
| Weather | `activationWeight × granularity`; weather.thickness ratio |
| Planet | Horizon LOD thresholds, SDF near/far km, detail coeff |
| Planet sim | Plate step interval; skip FullSim below 0.35 granularity |
| Asteroid belt | Disc opacity × granularity |
| Planet streaming | Tile LOD offset + radius chunks |
| Pathing | Replan interval + horizon distance ratio |
| Civil systems | `PersonaDayManager` tick interval via `CivilSystemsFeatureBudgetConsumer` |
| PixelLight | `FeatureBudgetIds.PixelLight` (`pixel_light`) — PixelLight rigs/optics/grid mounts on heli, airplane, airport; catalog `maxRecommendedSlots` soft cap |
| Train / Rail | `FeatureBudgetIds.TrainRail` (`train_rail`) — `TrainVehicleRagdoll` / coupling / Rail legs |
| Planar spline | `FeatureBudgetIds.PlanarSplinePath` (`planar_spline_path`) |
| Rail track | `FeatureBudgetIds.RailTrack` (`rail_track`) — mesh/SDF track structure |
| Train dispatch | `FeatureBudgetIds.TrainDispatch` (`train_dispatch`) |
| Elevator | `FeatureBudgetIds.Elevator` (`elevator`) |
| Inspectors | `FeatureBudgetIds.Inspectors` (`inspectors`) |
| Pet warden | `FeatureBudgetIds.PetWarden` (`pet_warden`) — OpinionFor |
| Cargo lash | `FeatureBudgetIds.CargoLash` (`cargo_lash`) — bay/limb lash joints and stability eval |
| Gas station | `FeatureBudgetIds.GasStation` (`gas_station`) — pumps, store shelves, rail-parallel refill |
| Park | `FeatureBudgetIds.Park` (`park`) — grounds, lots/paths, grass, plant planner |
| Sanitation | `FeatureBudgetIds.SanitationFacility` (`sanitation_facility`) |
| Garbage truck | `FeatureBudgetIds.GarbageTruck` (`garbage_truck`) — TrashWarden / hopper SPH |
| Sewer graph | `FeatureBudgetIds.SewerGraph` (`sewer_graph`) |
| Street blocks | `FeatureBudgetIds.StreetBlocks` (`street_blocks`) |
| Factory | `FeatureBudgetIds.Factory` (`factory`) — gate/line base runtime |
| Parkour fall | `FeatureBudgetIds.ParkourFall` (`parkour_fall`) — fall/land BT |

### PixelLight / grid slots budget

- **Feature id:** `pixel_light` (`FeatureBudgetIds.PixelLight`)
- **Perf prefixes:** `PixelLight`, `PixelLightRig`, `PixelLightOptic`, `PixelLightGridMount`
- **Authoring:** `PixelLightMultiSlotCatalog` holds many grid slots + per view×scope settings for helicopter / airplane / airport designers. Prefer keeping `gridSlots.Count ≤ maxRecommendedSlots` (default 16).
- **Docs:** [`Assets/locomotion/docs/PixelLightMultiSlot.md`](../../locomotion/docs/PixelLightMultiSlot.md)

## Adding a new ratio field

1. Add field id to `FeatureBudgetRatioFieldIds` and default bindings in `FeatureBudgetDefaults`.
2. Link field to a `FeatureBudgetEntry.ratioFieldIds`.
3. Read `FeatureBudget.GetRatioEffective(fieldId)` in the consumer.
4. Mark field budget-governed in Composition UI if it comes from the planetary wizard.

## API

```csharp
FeatureBudget.IsFeatureActive(FeatureBudgetIds.Weather);
FeatureBudget.GetGranularity(FeatureBudgetIds.Planet);
FeatureBudget.GetRatioEffective(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm);
FeatureBudget.Ratios.TryUnlock(fieldId, reason); // reason required
```

## Tests

`Assets/SystemDrawer/FeatureBudget/Tests/` — ratio unlock policy, effective value math, governor stepping order.
