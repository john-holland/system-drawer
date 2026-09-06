# Skinned mesh loop section

**Window → System Drawer → Mesh → Skinned Loop Section** authors closed vertex loops on a `SkinnedMeshRenderer` **or** `MeshRenderer`, then splits the mesh into multiple objects and saves a prefab that still points at the picker asset.

Hub filter: Mesh / Skinned Loop Section.

## Data

`SkinnedMeshLoopSectionAsset` stores:

- Original mesh and textures plus SHA1 (`originalMeshSha1`, `originalTextureSha1s`)
- Optional `savedCache*` snapshot when the live model changes
- **Multiple named loops** (`id`, `displayName`, ordered **bespoke** `vertexIndices`, optional `seedTriangle` / `assignedTriangles`, optional `submeshIndex` / `materialIndex`, bound **split bounds**)
- Selected **breakout material indices**
- **Split mode** (default **CutSeam**)

`SkinnedMeshLoopSection` sits on the same GameObject (or child) as the renderer. It resolves `SkinnedMeshRenderer` first, then `MeshRenderer`. `meshUpdated` flips when live SHA1 diverges. `useCached` is only valid while `meshUpdated` is true; it snapshots the current mesh/textures into `savedCache*` and keeps using the authored loop indices. **Overwrite & Update Saved Cache** promotes the live mesh to originals and clears the cache.

If `meshUpdated && !useCached`, apply/split is skipped.

## Split bounds

On the **active loop**, **Create Split Bounds** adds a child `SplitBounds_<loop>` under the selected mesh (`SkinnedMeshLoopSplitBounds` + trigger `BoxCollider`). The component stores:

- **Section Asset** — the loop-section configuration
- **Mesh Prefab** — the mesh prefab (or scene object if the renderer is not a prefab instance)
- **Loop Name** — copied from the loop’s display name and kept in sync when that name changes

The active loop has an object field for the bounds that **controls automatic vertex selection** (overlap). Scene persistence lives on `SkinnedMeshLoopSection.splitBoundsBindings` (by loop id). Move / rotate / scale the cube; overlapping verts draw as blinking wireframe nodes.

**Bespoke selected vertices** is a collapsible list of extra picks (scene clicks) beyond the bounds overlap. Each row has **Remove**. Close Loop / split use **overlap ∪ bespoke**. Creating again when a bounds already exists for that loop selects and re-associates it.

On the bounds inspector, **Update Loop Triangles** is enabled while **Skinned Loop Section** is open for that mesh and loop. It replaces the loop’s assigned triangles (and seed) with faces that currently have a vertex inside the cube.

## Picker

Scene gizmos raycast the **baked** posed skinned mesh, or the **static** `MeshFilter` mesh for a MeshRenderer (or the **cached** mesh when `useCached` is on). Highlight is the **1-ring** neighborhood plus radius zone. Wireframe uses the **complement of zone-average albedo** (vertex colors, or main texture at UVs, or material `_Color`). Click appends the nearest vertex to the **active** loop’s **bespoke** list (duplicates skipped). **Shift + mouse-wheel** cycles vertices in the zone; the cycled vertex **blinks** (~2 Hz sine) between albedo and complement. Debounce freezes hover rebuild until the cursor moves or ~2s elapse. **Close Loop** walks mesh edges across bounds overlap plus bespoke verts. **Save Asset** writes loop indices without remapping a new topology.

NamedAssign mode: click assigns the hit triangle to the active loop.

## Materials / auto break-out

The window lists renderer materials with submesh triangle and vertex counts. Toggle the materials to extract, then **Auto Break Out Selected Materials**:

- Uses **submesh triangles** (Unity material islands) as NamedAssign ownership
- Collects **material vertex sets** (unique verts used by those triangles)
- Walks **boundary edges** (edges used once in that submesh — rims and material seams) into vertex loops for gizmos / CutSeam

Split then emits one piece per selected material with that material only. A MeshRenderer source gets `MeshFilter` + `MeshRenderer` children; a skinned source keeps `SkinnedMeshRenderer` + bones.

If the mesh has a single submesh and several materials, extra slots share the same triangles — the window warns; add submeshes for true per-material seams.

## Split (ABC, B-heavy)

One asset / object can hold **many loops** and emit **many objects**.

| Mode | Behavior |
|------|----------|
| **CutSeam** (default) | Each loop is a cut. Duplicate rim verts; connected components of remaining triangles become pieces (N separating loops → multiple objects). |
| **FloodInterior** | Each loop + seed triangle flood-fills interior; leftover is **Remainder**. |
| **NamedAssign** | Assigned triangles per loop name; leftover is **Remainder**. Used by material auto break-out. |

Pieces keep `uv`, `boneWeights`, and `bindposes` when present. Skinned children reuse the source `bones` / `rootBone`.

## CustomRadialSide (JointMiddle / FlyAway)

`CustomRadialSideAsset` reuses this loop grabber as a tenth radial origin (an edge-loop face, not a cell corner). **Recognize and resize** walks the same boundary loops as material auto break-out.

On the piece, two unit-cube bounds parent to the loop frame:

| Object | Role |
|--------|------|
| **JointMiddle** | Overlap ∪ bespoke verts are the working-joint patch the solver reads |
| **FlyAway** | Leave / clearance side — anti-overlap for neighbor pieces |

`startPostBounds` on a `RadialBuildHost` CenterPost uses the same grabber. See [RadialBuild.md](../../BedogaGenerator/RadialBuild.md).

## Prefab

**Save Prefab…** writes meshes, copied materials/textures, and a prefab under `Assets/locomotion/Prefabs/SkinnedLoopPieces/<name>/` (or the folder you pick). Children are named `Piece_<loopOrComponent>`. The prefab **root** has `SkinnedMeshLoopSection.sectionAsset` pointing at the same picker asset. Each child has `SkinnedMeshLoopSectionPiece` with `sectionAsset`, `loopIds`, and `splitMode`. The source instance is left in the scene; the save connects a new prefab instance.
