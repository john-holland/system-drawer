# Road Lanes

Lateral **lane bins** on civil road ribbons (`RoadSpline3D` / `VehicleRoadCenterSpline`), authored in **Locomotion → Road Lanes Designer** and reusable as CityPixelGrid brushes. Roads stay centerline ribbons; TravelAgent snaps Drive waypoints onto `(s, laneIndex)` before convoy multibody.

## Runtime

| Type | Role |
|------|------|
| `RoadLaneLayout` | `laneCount`, `laneWidthM`, per-lane `directionSign` |
| `RoadLaneGridSettings` | `followTimeSec` (default 3), `gridCarLengths`, occupancy, `CellLengthM` / `MinSeparationM` |
| `RoadLaneOccupancy` | `(roadSegmentId, laneIndex, cellIndex)` slots |
| `RoadLaneSnap` | StayInLanes / IgnoreLaneGrid / AlignGridIgnoreLanes |
| `RoadLaneSplineBinding` | Layout on the same GameObject as `RoadSpline3D` (no Roads↔Locomotion asmdef cycle) |
| `RoadLaneConfigAsset` | Layout, grid, composite recipes, sidewalk/curb/grass, prefabs |
| `TravelAgentCard.lanePolicy` | Written onto `TravelAgent` before `RebuildCachedPlan` |
| `PlayerVehicleTravelSlowOverride` | Player cars skip speed/hold unless `brake01` or `selfDriving` (occupancy still applies) |

`Roads.RoadNetwork.SnapWaypointsToRoadLaneCenter` keeps `distanceAlong` and offsets by lane center. `RoadTravelBinding.EnrichDriveSegment` then applies `RoadLaneSnap`. Video plans from `VehicleTrackProjector.ApplyToTravelAgent` run the same enrich.

## Designer

**Locomotion → Road Lanes Designer** (hub **City Planning**, next to City Pixel Grid). The window embeds the same click-to-paint pixel grid as City Pixel Grid Designer: create/assign a `CityPixelGrid`, **Align Origin / Size From Spline**, then click-drag cells. X is across lanes/shoulder, Y is along the ribbon. Highway brushes (Select, RoadLanes, Overpass, lights, Crosswalk, Sidewalk, GrassStrip, jersey/guard rail, WireEnd, Debris) write stacked stamps. **Select** click-drags a rectangle of cells (Shift adds, Ctrl/Cmd toggles, Esc clears); **Paint Selected** / **Erase Selected** apply the current tool to that set. Drag-paint is one undo step; **Undo / Redo** buttons and Ctrl/Cmd+Z · Ctrl+Y work on paints, grid size, and lane config. **Scan debris** uses `SetBrushStampStacked` (overlap increments `floorIndex`). Save Unity `.asset` + JSON export. Pull/Push Continuuuum `/phone-wires`.

## CityPixelGrid

New layers: Highway, Overpass, Underpass, Debris, StreetLight, GrassStrip. New brushes include RoadLanes, Bridge, BridgeAndUnderpass, lights, Crosswalk, Sidewalk, GrassStrip, JerseyBarrier, GuardRail, WireEnd. Highway cells seed corridors; luminaires / crosswalks / guard rails do not close Drive cells; `laneDisabled` and jersey can.

See [CityPixelGrid.md](CityPixelGrid.md).

## Intersections, signs, sidewalks

`IntersectionLot` composes `RoadLot` (`lotKind = Intersection`). Drive snaps to the **leg outlet**, not pad center. `TAIntersectionCard` writes `PlannerHints` (`preferWalkAcross`, headings, yield). `SignStopPotential` (0 = no slow, 1 = full stop, >1 stretches hold) applies only after a Visual/Eyes **read**.

Sidewalk bake: walkable width = `sidewalkWidthM - 2*padding`. Curb = `SdfMax` SplineExtrusion + dapple bevel. Grass strip width parents `LotGrassGrowthController`.

## Wires, shoes, emergency

`PhonePoleIndex` / `StreetWireIndex` / `StreetWireEnd` (both ids required). `PowerLineSpan.EnsureRope` is required (`RopeSystem`). `HangingShoesComponent` is two child rigidbodies + lace `RopeSystem` (`knotLengthM`). Continuuuum page `/phone-wires` + SQL `phone_poles` / `phone_wires` / `phone_wire_associations`. See [PowerLineRoadside.md](PowerLineRoadside.md).

`EmergencyWarningBar` (16×2 wig-wag) + `EmergencyVehiclePresence` (local hear/see, `emergency_yield` / `emergency_flee` grafts, pull-over overlay, fleeing-birds gizmo). See [TrafficFireDispatch.md](TrafficFireDispatch.md).

## VehicleTrack

`laneIndex` on frames / segments / projected waypoints. Projector infers from civil layout lateral or image `cx` bins.
