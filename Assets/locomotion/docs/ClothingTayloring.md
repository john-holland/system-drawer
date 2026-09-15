# Clothing stores and tayloring

Feature budget: `FeatureBudgetIds.Clothing` (`clothing`). Venue kind `CivilSystemKind.ClothingStore`. Unity `Cloth` is not used (stripped by `RagdollComponentStripper`).

Farm fiber (`LotGrassPlantDef` species `flax` / `hemp` / `cotton` + `FiberFarmTravelAgent`) → `TextileMillBioRhythm` (`FactoryRuntime` load/unload + `TAVehicleDeliveryCard`) → `ClothingStoreRagdoll` shelves (`fiber`, `thread`, `bolt`, `dye`, `stuffing`, `garment`) → `TayloringTravelAgent`.

## Store

`ClothingStoreRagdoll` embeds `StoreBase` (`storeType = clothing_store`) and stations: cutting table, sewing, serger, fitting, dye, stuffing. `ClothingStoreBootstrap` on `CivilInstitutionStub`. PersonaDay ticks hours via `StoreBase.TickHours` and `TayloringBioRhythm`.

Empty `bolt` shelf blocks `TayloringCutCard` / `TryCutBolt`.

## Tayloring travel agent

**Locomotion → Tayloring Travel Agent** — steps Measure, Cut, Fold, Stitch, Serge, Press, Stuff, Invert, Fit, Tag. Diamond Tension / Pin / Bunch / Join (red limits, blue optimal, dashed white actual). Fold-step preview is the baked mesh cache. Bake completeness is diamond progress, not live SDF.

Open/close BT: editor calls `Locomotion.Open.TayloringOpenCloseBt` (Open.Runtime). Locomotion.Runtime does not reference Open.

Cards: `TayloringCutCard`, `TayloringFoldCard`, `TayloringStitchCard`, `TayloringSergeCard`, `TayloringDyeCard`, `TayloringStuffInvertCard`, plus thread pull / knot / jam.

## Bolt editor

**Locomotion → Cloth Pattern Designer** — `ClothBoltSpec` rectangle bolt, 3D spline ribbons (`Cut` / `Fold` / `Stitch`) with grabber pins, edge-loop join via `SkinnedMeshLoopSectionAsset`, SVG path import (`ClothSvgPathParser`).

Cloth motion: polar pin weights + linear Mandelbrot bunch (`ClothMandelbrotScrunch` / `SdfMaxNoiseUtility.SampleMandelbrot`). µ friction from ragdoll hands. Gameplay uses `ClothFoldBake` meshes + `Locomotion/SuperDeformoSkew`. Pleats reuse `InsulationBattBaker` along the fold spline.

## Machines and thread

**Locomotion → Sewing + Serger Designer** — Frame/Shell PixelLight, needle `CutToolKind.Needle`, threading guides, serger needle/looper phase. `ThreadSpoolDriver` wraps `RopeSystem` spool; jam is tension over the diamond limit. Dye is `PaintCanvas` on bolt UV (`paint_ink` for pigment). Worn garments bind `ClothUvStretchDriver` + `ClothingDamageLayer` (`WornGarmentBinder`).

IK: `TayloringIkTrainingCatalog` (`needle_down`, `looper`, `differential_feed`, `presser_foot`, `serge_overlock`).
