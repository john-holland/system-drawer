# SDF Max Surface Mesh (1A Static + 1B Skinned)

> **Theory & architecture:** [SdfMaxArchitecture.md](SdfMaxArchitecture.md) — why the render mesh is a derived artifact from the SDF field, and how query vs render paths stay separate.

## Overview

Composition-backed **render meshes** are derived from the same SDF field used for pathfinding and narrative queries. The SDF graph remains the source of truth; the mesh is a baked artifact.

| Mode | Component | Unity rendering |
|------|-----------|-----------------|
| `None` | — | Volume/query only |
| `StaticMesh` (1A) | `SdfMaxMeshSurface` | `MeshFilter` + `MeshRenderer` |
| `SkinnedMesh` (1B) | `SdfMaxSkinnedMeshSurface` | `SkinnedMeshRenderer` |

Set **Render Mode** on `SpatialVolumeProvider` when `backend == SdfMaxComposition`.

## Profile (`SdfMaxSolverProfile`)

| Field | Purpose |
|-------|---------|
| `generateSurfaceMesh` | Auto rebuild after Auto Calculate |
| `surfaceGridRes` | Uniform grid resolution per axis (4–96) |
| `surfaceIsoLevel` | Iso-surface threshold (default 0) |
| `recalculateNormals` | Recompute normals after extraction |
| `generateColliderMesh` | Assign baked mesh to `MeshCollider` (1A) |
| `convexCollider` | Convex hull flag on collider |

## 1A — Static mesh

- Mesh vertices live in **provider local space**.
- With `SyncSDFTreeShape`, moving the root transform does **not** rebake vertices; the SDF evaluator uses live `localToWorldMatrix`.
- Composition or grid resolution changes trigger debounced rebuild (`meshRebuildDebounceSeconds`, default 0.1s).
- Optional `MeshCollider` uses the same baked mesh when `generateColliderMesh` is enabled.

## 1B — Skinned mesh

- Vertices are stored in **root-bone local space**; `bindposes` and up to four bone weights per vertex (distance-based binder v1).
- **Bone animation** does not rebake mesh geometry; GPU skinning drives the renderer.
- **Collision / queries** with `SyncSDFTreeShape`: `SdfMaxBoneFieldContext` inverse-skins world sample points, then evaluates the bind-pose SDF field.
- Bone `Transform.hasChanged` polling (when `notifyOnTransformChange`) calls `NotifyChanged()` on the provider.
- **Physics**: `generateBindPoseCollider` bakes a static bind-pose `MeshCollider`. Animated hit-testing should use `TrySample`, not per-frame collider rebake.

## Editor

- Provider inspector: **Rebuild Surface Mesh**, **Regenerate Skin Weights** (1B)
- SDF Max Composition Editor: **Show surface mesh** preview, **Rebuild Surface Mesh**
- **Auto Calculate** rebuilds surface when `generateSurfaceMesh` is enabled on the profile

## Cache alignment

`SpatialVolumeCacheRegistry` content hash includes `SurfaceMeshVersion` (grid res, iso level, composition revision) so volume caches stay aligned with surface rebuilds.

## Limitations (v1)

- Grid-based surface extraction (not GPU marching cubes)
- No automatic LOD chains
- No runtime animated `MeshCollider` rebake for skinned surfaces
