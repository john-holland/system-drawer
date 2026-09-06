# Garage door — chain, carpentry, tracks

Sectional garage door: roller chain around an axle, carpentry pieces placed by SpatialGenerator, vertical tracks from road/rail splines, and a drive link that applies axle–chain–wheel force. Radial build is reused (sprocket ring, 4-side moulding). See [RadialBuild.md](../../BedogaGenerator/RadialBuild.md).

## Chain

`GarageChainSpec` (`Create → Locomotion/Civil/Garage Chain`) + `GarageChainAssembly`.

| Link definition | Role |
|-----------------|------|
| Master | Higher steel break (~18 kN) |
| Chain | Standard roller link (~12 kN) |
| Broken | Weakest-link snap (~1 N) — door stalls |

**Locomotion → Garage Chain Designer:** per-link PixelLight (`PixelLightRadialBrushDrawer`), piece curves (`CustomRadialSideAsset`), join kind, axle diameter/pose, tooth count (poses from `RadialSlotMath.PolarSlot`).

Steel limits: `GarageSteelLimits` (7850 kg/m³). Applied to `RopeConfig` (Spool + `WeakestLink`).

**SPH pull bake:** `GarageChainSphPullField.Bake` runs one particle neighborhood along the chain and stores 1D bins. Runtime `SampleTension` / `SampleBend` interpolates only. Rebake when length, pitch, axle radius, or tooth count changes.

## Door carpentry

`DoorAssemblySpec` + **Locomotion → Garage Door Designer**. Pieces:

- Top / bottom rails
- Lock stiles (side rails)
- Lock rails: middle rail; frieze rail if above middle between panels
- Mullion (vertical between panels)
- Moulding: curves + N-gon (**default 4 sides**) via `RadialRunNode` / `GarageDoorSdfBuiltins.MouldingRadial`

`GarageDoorNode.ConfigureRepeat` sets per-parent limits (2 stiles, N−1 mullions, N moulding sides). SDF: box union + opening subtract (`GarageDoorSdfBuiltins.BuildDoorShell`).

Lemmas: `DoorCarpentryLemmaPropertyKeys` and Continuuuum nouns (`top-rail`, `lock-stile`, `frieze-rail`, `mullion`, `moulding`). Pack fragment `pack=3d,placement=uniform,pad=0.04,sides=4` → `GarageDoorSgPackSettings`. Angular defaults: stile ⊥ rail 90°, mullion ∥ stile 0°.

## Tracks + drive

`GarageDoorVerticalTrack` builds a wall-plane `PlanarSplinePathLocomotion` (Y up, perpendicular to ground). Head curve is the wheel groove. Rollers use `PixelLightGridMountGameObject` + `jointId` `garage_roller`.

`GarageDoorDriveLink`: `F = τ / r` × SPH wrap. Winds `RopeSystem` and slides the door Transform (no Open.Runtime). Broken chain → force 0.

`HousingBuildingRagdoll.garageDrive` binds `slots.garageDoor`. Aperture tag stays `garage_door`.
