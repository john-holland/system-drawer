# Seated IK Stress Manifold

Character seating stress for Physics IK training fitness — not planetary `VolcanoStressSolver`.

## Sources

1. **CoG vs contact polygon** on `SitSurfaceContact` (`TryProjectCog` → tip risk 0–1).
2. Optional **`ISpatialVolumeQuery`** (`MeshConvexTree` or SDF Max via `SpatialVolumeProvider`): `SearchLeaves` / `TrySample` near the seat to bias tip risk when the projection is outside the volume.

## Fitness

`SeatedStressManifoldEstimator.TrainingFitness(...)` rewards:

- Low tip risk
- Low CoG error from seat center
- Free-hang brace success
- Stand-on plant stability
- Feet clear casters (rotate)
- Schooch lift-hold success

Restore direction is the in-plane vector from projected CoG toward the seat center (usable as impulse `forceDirection`).
