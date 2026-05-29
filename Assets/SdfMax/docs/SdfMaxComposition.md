# SDF Max Composition

> **Theory & architecture:** [SdfMaxArchitecture.md](SdfMaxArchitecture.md) — why the SDF backend exists, how the field relates to surface meshes, and the end-to-end data flow.

## Overview

SDF Max provides composed signed-distance volumes (union via `max`, subtract, smooth blends) and a unified **SpatialVolumeProvider** that can use either:

- **SdfMaxComposition** — analytic SDF graph + integral convex tree
- **MeshConvexTree** — existing triangle octree via `ConvexTreeMeshColliderService`

## Components

| Component | Role |
|-----------|------|
| `SpatialVolumeProvider` | Per-object entry; `SyncSDFTreeShape`, backend selection |
| `SdfMaxCompositionAsset` | Node list / expression graph |
| `SdfMaxSolverProfile` | Tree depth, grid cache, blend defaults |
| `SdfMaxSolverController` | Convenience wrapper syncing to provider |

## Editor

- **Window → System Drawer → SDF Max Composition Editor**
- **Auto Calculate** — derives local-space primitives from mesh bounds (Undo group `SDF Max Auto Calculate`)
- Open from orchestrator, pathfinding solver, composition asset, or provider inspectors

## Consumers

- `SpatialGenerator4DOrchestrator.volumeProviders` → merged into `SpatialGenerator4D` grid
- `HierarchicalPathingSolver.volumeProviders` → off-limits during rebuild
- `NarrativePathfindingCoverage.volumeProviders` → excludes cells inside volumes
- `NarrativeVolumeQuery.SampleSpatialVolumeImpl` — runtime inside test

## SyncSDFTreeShape

When enabled (default), transform motion and property edits invalidate/rebuild caches (mesh) or evaluate with live `localToWorldMatrix` (SDF). When disabled, run **Auto Calculate** or **Rebuild Cache** manually.

## Surface mesh (1A / 1B)

See [SdfMaxSurfaceMesh.md](SdfMaxSurfaceMesh.md).

- **1A** — `SdfMaxRenderMode.StaticMesh` adds `SdfMaxMeshSurface` (`MeshFilter` / `MeshRenderer`, optional collider).
- **1B** — `SdfMaxRenderMode.SkinnedMesh` adds `SdfMaxSkinnedMeshSurface` (`SkinnedMeshRenderer`, bone-aware `TrySample` when `SyncSDFTreeShape` is on).
- Root transform motion does not rebake static mesh vertices; bone motion does not rebake skinned vertices.
- Composition or `surfaceGridRes` changes rebake the surface and invalidate the volume registry.
