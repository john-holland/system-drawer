# Radial Build

Polar placement around a **CenterPost**. Shared spec lives in **BedogaPlacement** (`RadialBuildSpec`, `RadialSlotMath`, `RadialSide`, `RadialJoinKind`). SpatialGenerator is the engine. PixelLight is the first brush consumer.

## Spec

`RadialBuildSpec` holds count, start / wrap, radius, 9-way `RadialSide` **or** `CustomRadialSidePose`, join kind + offset, optional `solvedConfigIndex`.

`RadialSlotMath`:

- `SideOrigin` — Center / Upper Left / Up / Upper Right / Right / Lower Right / Bottom / Left Bottom / Left
- `PolarSlot` — slot `i` of `N` on a wrap (closed 360 or open arc)
- `ResolveWrapDeg` — `customAngleObject` azimuth wins, else `customAngle`, else 360
- `SolveWorkingJoints` — working layouts; when `startPostAnchor` is set, only configs that match that pose

Glue / Hardware are **sockets only** (`RadialJoinSocket.jointId`). Locomotion.Runtime does not reference Open.Runtime.

## Host

`RadialBuildHost` on any object:

- **CenterPost** — ring center (axis = post up, or spec axis)
- **Create Anchor Objects** — children `startPostAnchor` + `startPostBounds` (facing arrow)
- Vertex pick on `startPostBounds` snaps the start azimuth
- **Preview configuration** lists solved joints that match the start post (or all working joints if unset)

`CustomRadialSideAsset` (`Create → Locomotion/Mesh/Custom Radial Side`) authors an edge-loop face via the Skinned Mesh Loop Section grabber. JointMiddle / FlyAway are unit-cube bounds. Recognize-and-resize walks boundary loops on a piece mesh.

## SpatialGenerator

- `PlaceSearchMode.Radial` fills polar slots (`placementIndexInParent`)
- `PlacementMode.Around` sits children outside the parent ring
- `RadialRunNode.ConfigureRepeat(count)` sets per-parent limits on children (same UniformQueue pattern as `FenceRunNode`)

## PixelLight

N×N minigrid stamp or one-level recursive block around a centroid cell / 9-way side / CustomRadialSide. Reuses `pixel_light` Feature Budget. See [PixelLightMultiSlot.md](../locomotion/docs/PixelLightMultiSlot.md).
