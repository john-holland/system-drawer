# Spatial Volume Provider

> **Theory & architecture:** [SdfMaxArchitecture.md](../../SdfMax/docs/SdfMaxArchitecture.md) — dual-backend design, SDF-as-source-of-truth, and consumer data flow.

Unified volume component in namespace `SpatialVolumes` (assembly `HierarchicalPathFinding`).

## VolumeBackend

- `MeshConvexTree` — convex `MeshCollider` triangle octree
- `SdfMaxComposition` — `SdfMaxCompositionAsset` + integral convex tree

## API

- `ISpatialVolumeQuery.TrySample` / `SearchLeaves` / `GetWorldBounds`
- `SpatialVolumeCacheRegistry.EnsureBuilt` / `Invalidate`
- `static event Changed` — pathfinding and 4D systems subscribe

See also [SdfMaxComposition.md](../../SdfMax/docs/SdfMaxComposition.md).
