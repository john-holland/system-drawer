# Lumber yard

Forests, chopping, mill stations, and delivery (vehicle / river / canal). Botany stays on [`LotGrassPlantDef`](../pathing/civil/roads/LotGrassPlantDef.cs) + **Locomotion → Park Plant Planner**; `TreeGrowthTravelAgent` authors stages on top. Fiber crops for clothing (`flax` / `hemp` / `cotton`) reuse the same plant engine via `FiberFarmTravelAgent` — see [ClothingTayloring.md](ClothingTayloring.md). Commodity analog: mill `FactoryLoadZoneCard` / `TAVehicleDeliveryCard` → store shelves.

## Growth

**Locomotion → Tree Growth Travel Agent** — diamond Weather / Water / Sun / Minerals (red limits, blue optimal, dashed white actual). `enforceNaturalGrowthFromPhysicsManifolds` consults `WeatherPhysicsManifold`. Mineral white/black lists. `GroundCompositionNarrativeEvent` applies manifold **after** open/close completes. Omitted failure events stop the plan. Replant slot uses the same plot topology.

`TreeHealthBioRhythm` + `TreePhysicsManifoldGrowthTravelAgent`: phloem down, sapwood up, cambium, `PlantLayerDurationTexture` rings, heartwood. Paint treegen via `PaintCanvas`.

## Chopping

`LumberChopSdf.ChopLimb` → `DiggableVolume.ApplyScoop` (SDF Subtract) + wood `DestructibleMaterialProfile` density. Limb-choose a branch capsule then `PlantCutTakeRuntime.destructibleTrunkFall`.

IK: `LumberIkTrainingCatalog` (`back_cut`, `axe_swing`, `move_log`, `move_log_vehicle`) in **IK Animation Training**.

**Locomotion → Chainsaw Designer**: `ChainsawSpec` sharpness/hardness/failure lanes, `GarageChainSpec` bar, motor `GearboxSpec` (belt or chain-link drive).

## Mill

`WoodTravelAgent` + **Locomotion → Wood Travel Agent** — receive → debark / lathe → **plank cut** → **section** → convey → grade → convey → pile → bind → ship. Diamond Size / Weight / Grade / Moisture.

Log plank cut / section is the Lathe **PixelLight mill-kerf grid** (`MillKerfCuts`: odd kerf count, center line, dual-crescent). n through-kerfs → n+1 planks. **Apply lathe PixelLight kerfs → plank/section** stamps those counts on the PlankCut / Section steps.

`WoodMillBioRhythm` (`DispatchBioRhythm`) hours via `StationShiftSchedule` (24h or 18h banana-stand). Cards:

- `FactoryLoadZoneCard` → `FactoryTruckUnloadCard`
- `TAVehicleDeliveryCard` mechanisms: `vehicle`, `river`, `canal`, `river_canal_dam`

**Locomotion → Station Conveyer Travel Agent** — Size / Weight clamp. Belt UV shader `Locomotion/ConveyorScroll` driven by `RopeSystem.WindRateMps` (`ConveyorScrollUvDriver`). Anchor pattern matches `SanitationSortingStation.conveyorAnchor`.

**Locomotion → Lathe Designer** (`LatheSpec`): carriage, bedways, saddle, cross-slide, compound rest, tool post, apron lemma, half-nut, lead screw, headstock/tailstock/knurl. Headstock and quick-change gearboxes use **Locomotion → Gearbox Designer**. Cutter is rotating `CutToolComponent` (`CutToolKind.Lathe`) + SDF Subtract. Mill PixelLight kerfs use odd cut count + center line (`MillKerfCuts`).

Debark: `LumberJackingDebarkCard` from TreeHealth bark/phloem layers. Chipper: `ChipperStation` SPH infeed + Subtract.

Grading: `WritingCard` applies `ScribeCard` / PenInk to UV bounds; dialog grade; OCR via `OcrSheetMusicImporter` with dialog fallback.

**Locomotion → Wood Pile Designer**: PixelLight brush as quadtree bucket, depth-index layers, `Move Log` IK, `GarageDoorSgPackSettings` stack. `PhysicsSuperDeformo` wraps `RopeSystem` (optional tension override) + high-μ capsules; bake uses `Locomotion/SuperDeformoSkew`.

Sawdust: `SawdustHoseSpec` hose start/end + nozzle + vacuum; SPH prebake via `DigScoopSph` / `PaintCanvasHydroSolver`.

Axes/saws/chainsaw/lathe are `CutToolKind` on `CutToolComponent`.
