# House utility room and city water

Basement utility stack on `HousingBuildingRagdoll`: furnace, recoup water-heater (`imitirrrr__`), HVAC, filters/shutoffs, 100 A circuit panel, wall plugs, SPH flood drainage, and a pit sump that discharges to sewer.

## Room and appliances

`UtilityRoomBootstrap` (basement floor index 0) wires:

| Runtime | Role |
|---------|------|
| `FurnaceRuntime` / `HvacEquipmentRuntime` | Fuel from `HouseBioRhythm.gasAvailable01` / `oilAvailable01` |
| `WaterHeaterRuntime` | Tank temp → `BuildingPlumbingGroup.heaterHot01` |
| `RecoupWheelAlternator` | Lemma `imitirrrr` / id `imitirrrr__`; small kW credit on `HousePowerBus` |
| `JacobsLadderGunkFreeer` | Helical SDF wrap; `gunk01` trap |
| `WaterFilterRuntime` / `BuildingWaterShutoff` | Filter clog + meter shutoff |
| `SumpPumpRuntime` | Off below `minActivationLiters` (no trickle); clamps to `maxFlowLitersPerSecond`; sewer `WaterIn` only |
| `CircuitBreakerPanel` | 100 A ampacity (~24 kW @ 240 V) |
| `WallPlugRuntime` | SDF Max slot cavities / tine subtracts |

SG terms live in `UtilitySgNodes` (`UtilityRoomNode`, appliance nodes, `CircuitBreakerPanelNode` / `CircuitBranchNode`). UniformQueue + `perParentPlacementLimits`. Circuits grow on the first panel until load would exceed 100 A, then Spatial Generator clones a second panel (`ConfigureFromBranches`).

## City water and sewer

`WaterGraph` is separate from `SewerGraph` so shutoff does not collapse sewer. `StreetNamePeckingOrder` names contiguous street runs (lower pecking wins). `HouseServiceTapResolver` taps where driveway or HouseFront 4-touches Street / Sidewalk / Roads; fallback front-walk, then `egressMain`.

`CityPixelGrid.SeedWaterAndSewerFromGrid` / `StreetBlocksPlanAsset.SeedWaterAndSewerFromBuildings` walk streets in pecking order, then attach `HouseUtilityTap` + `SewerBuildingTap`.

## Flood and drainage

`HouseBasementFloodCache.Prebake` emits leak / backup / failed shutoff into a duck-typed rolling-sphere flood (`EmitFromFlow`). `FloodDrainageAmounts` + `RollingSphereFloodSimulator.DrainFromFlow` / `DrainAmount` reduce `standingLiters`, recycle spheres, and fade manifold paint. Master Rebake runs the cache after eaves.

## Cards and IK

`UtilityBioRhythm` writes `HouseBioRhythm.utilityComfort01`. `UtilityCard` kinds: install, start, shut down, maintain (filter, gunk, recoup, breaker, plug/unplug, sump prime/clear), and `StreetBuildingWaterBreaker` (trips building water **and** the panel feed). House chores include `UtilityMaintain`. `MonarchCard` / `MonarchicVenueRuntime` hold an optional `utilityCards` list.

Installation open/close BT is `UtilityInstallationOpenCloseBt` in Open.Runtime only (`SendMessage` hook from Runtime). `UtilityIkTrainingCatalog`: `plug_in`, `plug_out`, `breaker_flip_on`, `breaker_flip_off` → Open / Close.

## FeatureBudget

| Id | Rank | Prefixes |
|----|------|----------|
| `house_utility` | 38 | `UtilityBioRhythm`, `CircuitBreaker`, `RecoupWheel`, `SumpPump` |
| `water_mains` | 39 | `WaterGraph` |
| `basement_flood` | 40 | `HouseBasementFloodCache`, `RollingSphereFloodSimulator` |

Prefer `{P:canonical|…}` in prompts (`circuit-breaker`, `sump-pump`, `imitirrrr`).
