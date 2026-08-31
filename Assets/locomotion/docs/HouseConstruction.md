# House Construction Pipeline

Composable house authoring: diggable volumes, SG 3D/4D parts, SDF max (torus canvas + extrusions), city-pixel lots, doorway edge portals, and an RTS `HouseConstructionTravelAgent` with open/close BT and a per-step power diamond.

The **toroid is an authoring canvas**, not the building shape. Houses stay box/extrusion SDF. Displacement and PixelLight patterns paint in torus UV, then stamp onto wall/roof/deck faces.

## Floor numbering

Canonical helper: `HouseFloorIndex` (`TryParse` / `Format` / `FloorY`).

| Index | Labels |
|-------|--------|
| **1** | first, first floor, 1st, ground |
| **0** | basement, B |
| **-1** | sub-basement, SB, sub |
| **&lt; -1** | deeper (`-2`, `B2`, …) |

`FloorY(index, storyHeight)` places basement slab at origin; first floor at `+height`; sub-basement at `-height`. Designers use a **text field**, not a slider.

The same convention applies to elevator `minFloor` / button labels (`ElevatorButtonPanel.FormatFloorLabel`), stairwell landings, and [`FloorPlanIndexMap`](FloorPlanIndexMap.md) `floorIndex`.

## Diggable volumes

`DiggableVolume` (kind Wall / Soil / Foundation, `floorIndex`, optional SDF). `PrisonWallVolume` is a thin subclass. `ConsiderDiggingCards` scans the generic type. `DiggingBehaviorTreeNode` enqueues `DigActionQueue` and `ApplyScoop` subtracts a sphere into the bound `SdfMaxCompositionAsset`.

Dig site = soil volume on the foundation layer. Sub-basement digs use negative `floorIndex`.

## SG house grammar

`HousePartNode` children under `HouseShellNode` (3D octree; 4D time = construction frames). Each part has `floorIndex`, optional SDF fragment, and optional `PixelLightGridMountGameObject`.

Interior/structure: dig site, foundation, wall volume, stud bay, insulation batt, opening (`HouseOpeningNode`), garage door, doorway edge portal, frame, floor, trim, feature, electrical span, vent duct, PixelLight fixture, eave, gutter.

Yard/deck: awning, front steps, front walk, patio, grass, yard feature, railing, deck wall, support post, fence run (posts / panels / gates).

`BuildingRequirementSpec` / `HouseReferenceSlots` include `dig_site`, `foundation`, `studs`, `insulation`, `hvac`, `awning`, `front_steps`, `front_walk`, `patio`, `grass`, `yard_features`, `railings`, `deck`, `fence`, `garage_door`, plus `drivewayLot` / `garageLot` as `RoadLot`s.

## SDF max

Primitives: **Torus**, **SplineExtrusion**, **DisplacedTorus**. Walls/roofs = box max-union + opening subtracts + eave/trim/awning/railing extrusions. Soft PixelLight/height layers composite (`SdfMaxSoftToHardBaker.CompositeHeight`) then freeze to `SdfMaxCompositionAsset`. Envelope stamps max-union a displaced torus onto the house box (`HouseConstructionPlan.StampEnvelopeOntoHardSdf`).

See [SdfMaxComposition.md](../../SdfMax/docs/SdfMaxComposition.md).

## Editors

| Menu | Role |
|------|------|
| **Locomotion → House Foundation Layers** | Active brush (Select / Paint / Erase), RYB primary+complement mode colors, wall-brush stamps, **Add brush+!**, select-brush cell info, stamp painted cells to prefabs, SPH slab pour, soft→hard SDF |
| **Locomotion → Wall Brush Designer** | Discrete wall-piece brushes (electrical / plumbing / HVAC / insulation / drywall / slats / studs / custom) with prefabs; **Add brush+!**; bake cube+SG prefab onto the matching construction layer |
| **Locomotion → House Envelope Designer** | Toroid UV height paint filtered by floor text and house side (front/left/right/back); bake onto wall/roof SDF |
| **Locomotion → House Construction Travel Agent** | Step slider, harden, open/close BT bake, power diamond (blue optimal / red limits / dashed-white active / ThreatWarden halo) |
| **Locomotion → City Pixel Grid Designer** | **Add House Street / Yard / Side Layers**; `DrivewayLot` / `GarageLot` brushes |
| **Locomotion → Window PixelLight Grid Designer** | Pane/muntin/sill/trim/shutter/shade assembly; auto-fit PixelLight (3×3 panes → ≥7×7) with arbitrary-size override; trim runs + separate elbows |

## Windows

`WindowAssemblySpec` + `WindowOpeningNode` (`apertureTag = window`). Muntins (glazing bars) divide the sash; mullions sit between units. Auto-fit PixelLight cells: `2N+1` (stile/pane/muntin…). Double-vacuum glazing is two panes plus a non-colliding cavity. Sliding sashes use `OpenCloseJointKind.Slide`. Shades use `PulleySurfaceRagdoll` (`shade.pull_string` proxy instrument + `PulleyPullNode` curve lerp). Do not use `DoorwayEdgePortal` for casement/sash glass.

## City pixel lots

`CityPixelGrid.EnsureHouseLayers()` adds Street, Sidewalk, Yard, Driveway, Garage, HouseFront/Left/Right/Back.

`HouseLotConnectivity`: driveway cells must 4-touch street, sidewalk, or roads; garage cells must 4-touch driveway, street, sidewalk, or roads. TravelAgent drive/walk enrichment already snaps to lot pads.

## Doorway edge portal

`DoorwayEdgePortal` — edge-loop overlay (`doorway_portal`), `PathingAperture`, open/close joint, `Locomotion/DoorwayPortalOcclusion` clip. While a body transits, `RigidbodyPhysicsWalk` owns meshes until the next descendant rigidbody and convex-refits via `ConvexTreeMeshColliderService`. On exit, restore prebaked meshes; **do not rewrite** `DestructibleBakeAsset`. `DestructibleEnvironmentMeshRenderer.DiscoverSources` uses the same walk and skips the portal overlay.

`GarageDoorNode` is a vehicle aperture tagged `garage_door`.

## Insulation, eaves, yard, MEP

- Insulation: `InsulationBattBaker` — 2–4 pleated hair lattice bakes, fiber materials, `TransparentOccluder` dither; inactive until the insulation frame.
- Eaves: `HouseEaveWaterCache.Prebake` writes catchment → gutter → downspout into `Weather.Water`; overflow uses `HousingBuildingRagdoll.overflowLayers`.
- Basement utilities: furnace, recoup heater (`imitirrrr__`), HVAC, filters, 100 A panel, sump, SPH flood drain — see [HouseUtility.md](HouseUtility.md). `HouseBasementFloodCache.Prebake` is on Master Rebake after eaves.
- Yard: `HouseYardFeatures` binds front walk (`PlanarSplinePathLocomotion`), steps (`StairwellTopologyAsset`), patio `RoadLot`, lot grass, and per-floor PixelLight grids on railings.
- Electrical: `HousePowerBus` + `HouseElectricalSpan` (inactive prebake). 100 A service ≈ 24 kW; Spatial Generator clones a second panel when branch amps would exceed 100. HVAC: `HouseVentDuct` full-bore collider.
- Wall brushes: `WallBrushCatalog` / `WallBrushSpec` paint discrete electrical, plumbing, drywall, and slat prefabs onto existing layers (`rough_mep`, `sheathing`, `studs`, …). **Add brush+!** registers a new spec; Foundation Layers stamps occupied cells as separate GameObjects.
- Move-in: `MoveInCard` after the last construction frame.

## Fences (SG3D repeat)

`FenceRunNode` along the lot/yard spline. `ConfigureRepeat(N)` sets `FencePostNode` / `FencePanelNode` `perParentPlacementLimits` and counts (N posts, N-1 panels). Place with `SpatialGenerator.PlacementStrategy.UniformQueue`. `CompileGateJointIds` reads `RoadLotBoundarySpline` gap sections. RTS order is post, panel, post, … then gates.

## Travel agent

`HouseConstructionTravelAgent. `GoalType.Construction` → `ConstructionPhaseCard`. Site open/close on `siteRoot`. Open/close BT is baked in Open.Runtime (`HouseConstructionOpenCloseBt`) so Locomotion.Runtime does not cycle with Open.Runtime. Utility install stops use `UtilityInstallationOpenCloseBt` the same way. Power diamond axes: commodities, resources, vehicle reach, blockage.

`PlanRtsFromFenceRun` / `PlanRtsFromLotOrder` (garage pad/door only after a valid driveway outlet).
