# Planar Spline Path Locomotion

Invisible walkable ribbon of planes along a Catmull-Rom spline — train aisles, tree branches, ledges.

## Component

`PlanarSplinePathLocomotion` — control points (local), default width, granularity, custom sections, ledge walls.

| Mode | Behavior |
|------|----------|
| Division | Equal `divisionStopCount` slices along arc length |
| PerLength | One plane every `perLengthMeters` |

Custom sections `{ startT01, endT01, width, hierarchicalPlaneId }` override auto planes whose midpoint falls in range.

## Ledge guard

- `blockFallUnlessJump` + `jumpWallHeight` (default 0, no mantling)
- Thin box colliders on left/right edges after Rebuild

## Rebuild

- `Rebuild()` / narrative action `planar_spline_rebuild`
- `ClampToPath` / `TryProject` for TravelAgent / ambulation

## Editor

Inspector: **Rebuild planes**. Per custom section: **Show transform gizmos** → **Save** / **Revert**.
