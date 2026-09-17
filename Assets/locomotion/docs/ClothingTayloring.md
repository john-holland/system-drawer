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

**Locomotion → Cloth Pattern Designer** — `ClothBoltSpec` rectangle bolt, 3D spline ribbons (`Cut` / `Fold` / `Stitch` / `Hem`) with grabber pins, edge-loop join via `SkinnedMeshLoopSectionAsset`, SVG path import (`ClothSvgPathParser`). Stitch and hem paths assign gauge, decal vs bake, a `SewingStitchProgram`, and the same PixelLight stitch-program grid as Sewing + Serger Designer (`pixelLightCatalog` optional).

Cloth motion: polar pin weights + linear Mandelbrot bunch (`ClothMandelbrotScrunch` / `SdfMaxNoiseUtility.SampleMandelbrot`). µ friction from ragdoll hands. Gameplay uses `ClothFoldBake` meshes + `Locomotion/SuperDeformoSkew`. Pleats reuse `InsulationBattBaker` along the fold spline.

## Machines and thread

**Locomotion → Sewing + Serger Designer** — 6-face Frame/Shell PixelLight (`magnetoIndex` 0), stitch program grid on Front × Shell × `magnetoIndex` 1 (lockstitch left/right feed, cloth sides, needle entry/exit, step index listing). Serger has the same Frame/Shell mounts, overlock program, and slot accordion. **Ensure sewing/serger hollows/doors** stamps subtraction hollows (`needle_throat`, `bobbin_race`, …) and hinged doors (`door_bobbin` / `door_bed` / `door_looper`) with `frameId`, `doorId`, and `hingeLabel`. **Show stacks** pads and skews overlapping slots by `zIndex`; the listbox reorders Z. Bake sewing/serger SDF (`SewingMachineSdfBuiltins`) then Frame/Shell subtracts hollows in z-order.

Needle is `CutToolKind.Needle`. Thread guides are editable. `ThreadSpoolDriver` wraps `RopeSystem` spool; jam is tension over the diamond limit. Connecting / looper strands use `SewingMachineSpec.ToRopeConfig` / `SergerSpec.ToRopeConfig` via `SewingThreadStrandBinder`. Dye is `PaintCanvas` on bolt UV (`paint_ink` for pigment). Worn garments bind `ClothUvStretchDriver` + `ClothingDamageLayer` (`WornGarmentBinder`).

Hem / seam: `ClothSplineKind.Hem` plus stitch paths take `gauge01` and `HemSeamApplyMode` (Decal or Bake). Shader `Locomotion/ClothHemSeam` displaces and specs thread gage; `ClothHemSeamBake.ApplyToStep` marks Stitch/Serge bake complete.

Lemmas: `SewingLemmaPropertyKeys` / `FrameShellInclusionLemmaPropertyKeys` in Continuuuum builtins (`sewing-machine`, `serger`, `frame-inclusion`, `shell-inclusion`, `hinge-label`, `door-bobbin`, …). Catalog slot ids are underscore forms (`needle_throat`). Sync lemma-completion after export (`POST /api/lemma-completion/sync-builtins`). `{P:sewing-machine|inclusion=shell|frame-id=sewing_shell|door-id=door_bobbin|hinge-label=left}`.

IK: `TayloringIkTrainingCatalog` (`needle_down`, `looper`, `differential_feed`, `presser_foot`, `serge_overlock`).
