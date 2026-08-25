# Road Lots

`RoadLot` — large flat/graded TravelAgent pads with height maps and **0..N** road outlets.

## Outlets

`RoadLotOutlet.roadSegmentId` + along-arc. Isolated lots (no outlets) are valid. `RoadTravelBinding.EnrichDriveSegmentWithRoadLot` sets `MultiModalSegment.roadLotId` and snaps the last drive waypoint onto the pad. `IntersectionLot` (`lotKind = Intersection`) snaps Drive to the **incoming leg outlet**, not the pad center. See [RoadLanes.md](RoadLanes.md).

## Boundary walls

`RoadLotBoundarySpline` closed loop. Walls start as **one section** covering t=0..1. Splits must keep **sum(Length01) == 1** or `ValidateWallSections()` throws.

`BakeWallMesh()` builds mesh + `MeshCollider` for non-gap sections (gaps = open / gate topology id).

## Walk path ribbons

`RoadLot.pathRibbons` — `PlanarSplinePathLocomotion` between pad and road outlets (`EnsureOutletPathRibbons`). `TravelAgent.EnrichWalkSegmentWithRoadLot` tags `roadLotId` and can sample ribbon endpoints.

## Grass

`LotGrassPlantDef` + `LotGrassGrowthController`:

- SpeedTree prefab spawn, start/end growth, stage BT + mesh morph (`LotGrassMeshMorph` shader)
- Cuts: severity 0–1; 0 blocks cut-through; 1 blocks next-section growth
- Leaf→root section resolve; carry cut length on regrowth; forget cuts above pruning cut
- `PlantCutTakeRuntime` inventory take records for CutTool / chainsaw

## Editor

Travel Pathing Editor → **Road Lots** / **Pilot GPS bake**. Park plants → **Locomotion → Park Plant Planner**. House driveway/garage pads are RoadLots with 4-adjacent outlets (driveway → street/sidewalk; garage → driveway/street/sidewalk) — see [HouseConstruction.md](HouseConstruction.md).
