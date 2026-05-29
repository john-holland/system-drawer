# SDF Max: Theory and Architecture

This document explains **why** the SDF Max backend exists, how it relates to the mesh surface system, and how both plug into the shared spatial-volume layer. For component-level reference, see [SdfMaxComposition.md](SdfMaxComposition.md) and [SdfMaxSurfaceMesh.md](SdfMaxSurfaceMesh.md).

## The problem

Several systems need the same answer to the same question: *is this point inside this object’s occupied volume?*

| Consumer | Use |
|----------|-----|
| `HierarchicalPathingSolver` | Treat volume interiors as off-limits during path rebuild |
| `NarrativePathfindingCoverage` | Exclude cells inside authored volumes |
| `SpatialGenerator4DOrchestrator` | Merge provider bounds into the 4D spatial grid |
| Runtime queries | `ISpatialVolumeQuery.TrySample` / `SearchLeaves` |

Before SDF Max, the only backend was **MeshConvexTree**: a convex `MeshCollider` triangle mesh baked into an octree. That works for imported art, but it is a poor fit when the shape is **procedural**, **editable**, or must stay **analytically consistent** across pathfinding, narrative logic, and rendering.

SDF Max adds a second backend that treats the volume as a **composed signed-distance field** rather than a frozen triangle soup.

## Signed distance as the source of truth

A signed distance function (SDF) returns the shortest distance from a point to the nearest surface, with sign indicating inside (−) vs outside (+).

SDF Max composes fields with **max** (union), subtract, and smooth blends over primitives (box, sphere, capsule, etc.). The composition is stored in `SdfMaxCompositionAsset` as a node graph and evaluated at runtime by `SdfMaxExpressionGraph` → `SdfMaxEvaluator`.

**Why max for union?** For inside/outside tests, the union of two solids is the pointwise maximum of their SDFs. One evaluator, one consistent inside/outside rule, no mesh boolean cleanup.

The field is the **authoritative representation**. Everything else—acceleration structures, render meshes, cache keys—is derived from it.

## Two backends, one query surface

`SpatialVolumeProvider` (namespace `SpatialVolumes`) is the per-object entry point. It implements `ISpatialVolumeQuery` regardless of backend:

```
                    ┌─────────────────────────────┐
  Consumers ───────►│   SpatialVolumeProvider     │
  (pathfinding,     │   ISpatialVolumeQuery       │
   narrative, 4D)   └──────────┬──────────────────┘
                               │
              ┌────────────────┴────────────────┐
              ▼                                 ▼
   MeshConvexTreeBackend            SdfMaxCompositionBackend
   (triangle octree)                 (SDF graph + integral tree)
```

| Backend | Source geometry | Best for |
|---------|-----------------|----------|
| `MeshConvexTree` | Convex `MeshCollider` mesh | Imported assets, legacy colliders |
| `SdfMaxComposition` | `SdfMaxCompositionAsset` graph | Procedural shapes, editor-authored volumes, unified render + query |

Both backends expose the same leaf search and sampling API so consumers never branch on implementation. See [SpatialVolumeProvider.md](../../HierarchicalPathFinding/docs/SpatialVolumeProvider.md) for the API summary.

## Why an SDF backend helps

**Parametric motion without rebaking.** With `SyncSDFTreeShape` enabled (default), the SDF backend evaluates samples using the provider’s live `localToWorldMatrix`. Moving the object updates inside/outside tests immediately; the integral tree does not need a full rebuild on every transform change. The mesh backend, by contrast, invalidates its triangle octree when the transform changes.

**Editable, inspectable shapes.** The composition editor manipulates primitives and blend ops directly. Auto Calculate can seed a graph from mesh bounds; artists can refine from there without re-exporting geometry.

**One field, many consumers.** Pathfinding, narrative exclusion, and optional surface rendering all read the same evaluator. There is no drift between “what the pathfinder thinks” and “what the SDF says.”

**Acceleration without losing analyticity.** Raw SDF evaluation at every query point is too slow for broad spatial search. The backend builds an **integral convex tree** (`IntegralConvexTreeSolver`): an adaptive spatial partition whose leaves subdivide where the field varies enough. `SearchLeaves` culls to overlapping bounds; `TrySample` evaluates the graph (optionally via `SdfMaxGridCache`) at the point. The tree approximates *where* the field is interesting, not *what* the field is—the evaluator remains exact.

## Surface mesh system: derived artifact, not second truth

Rendering in Unity expects meshes. SDF Max does **not** replace the field with triangles for logic; it optionally **bakes** a mesh from the field for display (and static collision).

```
  SdfMaxCompositionAsset  ──►  SdfMaxEvaluator  ──►  queries / pathfinding
                                      │
                                      ▼
                            SdfMaxSurfaceMesher
                            (uniform grid iso-surface)
                                      │
                    ┌─────────────────┴─────────────────┐
                    ▼                                   ▼
           SdfMaxMeshSurface (1A)            SdfMaxSkinnedMeshSurface (1B)
           MeshFilter + MeshRenderer         SkinnedMeshRenderer + bone weights
```

Set `renderMode` on `SpatialVolumeProvider` when `backend == SdfMaxComposition`:

| Mode | Role |
|------|------|
| `None` | Volume and queries only; no render mesh |
| `StaticMesh` | Baked mesh in provider local space; root motion does not rebake vertices |
| `SkinnedMesh` | Baked mesh in bind-pose bone space; GPU skinning for deformation |

### Separation of concerns

1. **Volume / query path** — Always the SDF (with optional grid cache and integral tree). Transform and bone motion are handled at sample time when `SyncSDFTreeShape` is on.
2. **Render path** — Grid-based iso-surface extraction (`SdfMaxSurfaceMesher`). Rebuilt when composition, `surfaceGridRes`, or iso level changes—not on every frame of motion.
3. **Static collision (1A)** — Optional `MeshCollider` shares the baked static mesh; same rebake rules as the renderer.
4. **Skinned collision (1B)** — Bind-pose collider is static; animated hit-testing uses `TrySample` with `SdfMaxBoneFieldContext` (inverse skin world points into bind space, then evaluate the field).

This split is intentional: rebaking a full grid mesh every frame would be prohibitive; evaluating the SDF at query points is cheap enough for gameplay and pathfinding.

### Cache alignment

`SpatialVolumeCacheRegistry` hashes `SurfaceMeshVersion` (grid resolution, iso level, composition revision) into volume cache keys so spatial rebuilds stay aligned with surface rebakes. Changing the composition invalidates both volume and surface artifacts together.

## End-to-end data flow

```
  Authoring                    Build / runtime                    Consumers
  ─────────                    ───────────────                    ─────────

  SdfMaxCompositionAsset       EnsureBuilt()
  SdfMaxSolverProfile    ──►   ├─ ExpressionGraph + Evaluator  ──► TrySample / IsInside
                               ├─ IntegralConvexTreeSolver     ──► SearchLeaves
                               └─ SdfMaxGridCache (optional)   ──► fast Sample

  Auto Calculate / Editor      Rebuild Surface Mesh (debounced)
                         ──►   SdfMaxSurfaceMesher           ──► MeshFilter / SkinnedMeshRenderer

  Transform / bone motion      SyncSDFTreeShape
                         ──►   live matrix / bone context    ──► queries update without mesh rebake
```

`SpatialVolumeProvider.Changed` notifies subscribers (pathfinding solver, 4D orchestrator, etc.) when geometry or sync state changes so they can invalidate and rebuild their own structures.

## When to use which backend

**Prefer `SdfMaxComposition`** when:

- The shape is built or edited in the composition graph.
- You need transform-aware queries without rebaking meshes.
- You want optional surface rendering from the same field.
- Narrative or procedural systems author volumes analytically.

**Prefer `MeshConvexTree`** when:

- The object already has a final convex mesh collider from art pipeline.
- You only need approximate inside/outside from triangles, not a composed field.
- There is no composition asset and no plan to maintain one.

## Limitations (current generation)

- Surface extraction is CPU grid-based, not GPU marching cubes; quality follows `surfaceGridRes`.
- No automatic LOD chains for surface meshes.
- Skinned surfaces do not rebake `MeshCollider` per animation frame; use `TrySample` for animated queries.
- Mesh backend sampling is bounds-based approximation, not a true mesh SDF.

## Related docs

- [SdfMaxComposition.md](SdfMaxComposition.md) — components, editor, consumers, SyncSDFTreeShape
- [SdfMaxSurfaceMesh.md](SdfMaxSurfaceMesh.md) — static vs skinned render modes, profile fields, editor actions
- [SpatialVolumeProvider.md](../../HierarchicalPathFinding/docs/SpatialVolumeProvider.md) — unified volume API and backend enum
