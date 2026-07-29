# Proxy Mouse → Paint Brush

In-game painting via instrument proxy mouse, hair-tech brushes, SDF Max canvas layers, high-viscosity piles, and optional destructive smudge.

## Control path

`PaintProxyMouseDriver` → `PaintInstrumentMap` channels → `PaintInstrumentProxy` → tip/shaft/wrist → `BrushManipulationSolver` (keep paint off player body capsules).

Channels: `brush.yaw|pitch|roll|press|twist`, `tube.squeeze`, `sealant.spray`.

## Hairline (shared with hair tech)

`HairLineCurve` + `HairLineAngleCurve` on `HairPlumeConfig` define the conical emergence ring. Strand roots use `HairLineSampler.EmergenceDirection` (angle tilt lerped toward `centerPateLocal` by `pateAngleBlend`). Brushes embed the same curves on the ferrule.

## Brushes

`PaintBrushDefinition` / `PaintBrushCatalog` (fan, pointed, square, round, angle, flat liner). `PaintBrushRuntime` steals from `PaintPileLiquidDriver` (plane ∩ saturation) and deposits via `PaintStrokeStamper`.

## Tube & piles

`PaintTubeConfig` + `PaintTubeDeformDriver`: circular nozzle, flared base, finger sphere × depth × volume, SDF Max deform memory. Hang then fall with high viscosity through `WeatherPhysicsManifold` (via `PaintPileLiquidDriver`; avoids a circular ref to `Locomotion.Liquid.Runtime`).

## Canvas

`PaintCanvas` + `PaintCanvasLayerStack` (SDF Max layers + materials + dry01). `PaintPlanarViscosityCache` R=wet G=dry B=mass A=caustic/spec film. Streakiness via `totalViscosity` / `streakiness`. Canvas `surfaceTension` feeds hydro.

### Hydro mixing (surface tension + integral SDF)

`PaintCanvasHydroSolver` (auto-added on canvas) runs canvas-local SPH-style particles constrained to the top wet layer’s SDF Max expression via `IntegralConvexTreeSolver` — not the world weather manifold.

- **Stamp / pile pull** seeds particles; pile `ConsumeMass` injects pigment + tension
- **Wet / dry / caustic** regimes write viscosity RGBA (A = film caustic/spec)
- **Surface tension beads** → matte (`pileFactor` lowers specular / raises roughness)
- **Brush pull-away flux** thins film → semi-gloss
- `PaintMaterialLayer` binds hydro-updated `_Glossiness` / roughness

## Smudge & carry

`enableDestructiveSmudge` → `PaintSmudgeCollider` Subtracts wet SDF; `PaintTransferDecal` paints colliding `MeshRenderer`s. `PaintCarryKeepOutSolver.collisionEnabledCarryMode` (developer opt-in) trains frame grips that fail if wet paint would be touched.

## Sealant

`SealantSprayCan` conical spray raises dry rate / locks wet cells.

## IK training

`PaintingIkTrainingCatalog` entries map to `PhysicsIKTrainingCategory`. Drop clips under `painting/Animations/*`, discover via **Window → IK Animation Training**.

## Editor

**Window → System Drawer → Paint Studio Bake** scaffolds brush builtins + tube/canvas SDF assets.
