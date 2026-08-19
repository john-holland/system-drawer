# Parks / Walking Paths

Park venues with RoadLot grounds, wall bake, grass/plant growth, plant planner, and maintenance/justice staffing.

## Runtime

| Type | Role |
|------|------|
| `ParkRuntime` | Grounds + buildings; optional attached `GasStationRuntime`, maintenance, kitchens, shops, spas |
| `ParkBioRhythm` | TA cards for maintenance / horticulture / patrol; peers TA; ticks lot grass |
| `ParkSignageTrigger` | Collider → BT / dialog-tree id / narrative action |
| `ParkBootstrap` | `CivilInstitutionStub` when `CivilSystemKind.Park` |

Staff pecking on `CompanyRegistration` (manager, groundskeeper, justice_patrol, horticulturist). PersonaDay venue hours via lattice kind `Park`.

## Lots / walls / paths

- `RoadLotBoundarySpline.BakeWallMesh()` — mesh + collider; gaps = no wall / gate topology.
- `TravelAgent.EnrichWalkSegmentWithRoadLot` / `EnrichPlanWithRoadLots` — walk + drive lot tags; outlet path ribbons via `PlanarSplinePathLocomotion`. House front walks / patio pads reuse the same lot + ribbon path; see [HouseConstruction.md](HouseConstruction.md).

## Grass / cut / horticulture

- `LotGrassGrowthController` — SpeedTree prefab spawn, stage morph, cut memory leaf→root.
- `PlantCutTakeRuntime` — `(plant, seed, time, cut planes)` + CutTool hook.
- BT nodes: weed pull, seed spread, hand sow, watering, hoeing, flower tending.

## Plant planner

Menu **Locomotion → Park Plant Planner** (`ParkPlantPlannerWindow`):

- Grid granularity (default = plant min size)
- Plant/color dropdown, pencil, fill
- Placement squares auto on paint/move
- Time-layer +/− for staged growth

## FeatureBudget

Id: `park` (`FeatureBudgetIds.Park`); planar spline / grass under park grounds.
