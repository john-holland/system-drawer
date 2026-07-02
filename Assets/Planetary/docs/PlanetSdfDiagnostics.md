# Planet SDF Max — diagnostics & debug walkthrough

Step-by-step guide for **invisible SDF**, **play-mode churn**, and **stale horizon meshes** on `PlanetBody` planets (including Little Prince / B-612).

Related docs:

- [LittlePrincePlanetSetup.md](./LittlePrincePlanetSetup.md) — asset values and scene wiring
- [PlanetaryArchitecture.md](./PlanetaryArchitecture.md) — render stack overview
- [SdfMaxSurfaceMesh.md](../../SdfMax/docs/SdfMaxSurfaceMesh.md) — `SpatialVolumeProvider` surface mesh modes
- [PerfTraceView.md](../../SystemDrawer/docs/PerfTraceView.md) — scoped timing profiler

---

## 1. Know which renderer you expect

Planets use **three separate visual/query paths**. Only one SDF path typically draws the horizon shell.

| Path | Component | What it draws | When it runs |
|------|-----------|---------------|--------------|
| **Nadir chunks** | `PlanetRenderer` + `PlanetMeshStreamingService` | Cube-sphere height mesh near the player | After `RebuildChunkMeshes()` |
| **Horizon SDF LOD** | `PlanetarySdfLodRenderer` | Iso-surface mesh at distance; `Planetary/SdfLod` shader | `Rebake()` / `EnsureLodMeshes()` + `LateUpdate` |
| **Volume surface (optional)** | `SpatialVolumeProvider` + `SdfMaxMeshSurface` | Static mesh on the **same** object as the provider | Only when `renderMode = StaticMesh` |

**Default Little Prince layout:** `SpatialVolumeProvider.renderMode = None` → SDF is drawn by **`PlanetarySdfLodRenderer`**, not by `SdfMaxMeshSurface`.

If you see **chunk terrain but no horizon shell**, debug the LOD renderer (Section 4).  
If you enabled **StaticMesh** on the provider, debug `SdfMaxMeshSurface` (Section 5).

---

## 2. Symptom → first check (30 seconds)

```
Play mode: nothing / pink / frozen?
│
├─ Console spam: RebuildAll / RebakeComposition / SdfMaxSurfaceMesher.Build
│   → Section 6 (performance churn)
│
├─ MeshFilter on PlanetarySdfLodRenderer: mesh = None
│   → Section 4 (LOD renderer)
│
├─ MeshFilter has mesh but invisible / wrong shader
│   → Section 4.3 (lodMaterial)
│
├─ Horizon OK in Scene view, broken in Game view only
│   → Camera culling / layer / shader on Planetary/SdfLod
│
└─ Only pathfinding broken, visuals OK
    → Section 5 (SpatialVolumeProvider / volume cache)
```

---

## 3. Pre-flight inspector checklist

Select the planet root (`PlanetBody`). Confirm **before** Enter Play:

### PlanetBody

| Field | Required | Notes |
|-------|----------|-------|
| `planetRadius` | Yes | Little Prince: **500** |
| `planarBase` | Yes | Height sampling for chunks + regression |
| `compositionProfile` | Yes (layered planet) | Drives `PlanetaryCompositionBaker` |
| `solverProfile` | Yes | Grid res, iso level; copied to volume provider |
| `composition` | **Yes for fast Play** | Must have **nodes.Count > 0** when `rebuildOnPlayAwake` is off |
| `volumeProvider` | Yes | Same root or assigned |
| `sdfLodRenderer` | Yes | Child or self; `GetComponentInChildren` |
| `rebuildOnPlayAwake` | **Off** (recommended) | Off = use baked composition; On = full rebake every Play |

### PlanetarySdfLodRenderer

| Field | Required | Notes |
|-------|----------|-------|
| `profile` | Yes | `tierGridRes` non-empty, e.g. `{ 12, 24, 32 }` |
| `lodMaterial` | **Strongly recommended** | Uses `Planetary/SdfLod` (or compatible). Without it, mesh may show pink/default and horizon shader props are skipped |
| `horizonSettings` | Optional | Handoff with streaming |

### SpatialVolumeProvider (same object as PlanetBody)

| Field | Typical value | Notes |
|-------|---------------|-------|
| `backend` | `SdfMaxComposition` | |
| `composition` | Synced from PlanetBody on bake | |
| `profile` | Same as `solverProfile` | |
| `renderMode` | **`None`** | Use LOD child for horizon; set `StaticMesh` only if you want a second surface on this object |

### Scene services (warnings, not always fatal)

| Key | Provider | Symptom if missing |
|-----|----------|-------------------|
| `weather.physicsManifold` | `WeatherPhysicsManifold` + wizard | Console fallback warning; atmosphere bake uses defaults |
| `planet.body` | `PlanetServiceWizardComponent` | Roads / society / external systems fail to resolve planet |

Run **Window → System Drawer → Facilitator Hub** (or Physics Bridge Editor) and resolve missing required keys.

---

## 4. Debug: PlanetarySdfLodRenderer (horizon SDF)

### 4.1 Confirm composition exists

1. Select `PlanetBody`.
2. Expand **Composition** — `nodes` should list Core/Mantle/Crust blends (or at least LatLonShell + noise).
3. If **empty or missing**:
   - Inspector → **Rebuild Planet**, or
   - **Window → System Drawer → Planet → Bake Composition**, then **Rebake SDF LOD Mesh**.

**Important:** `RebakeComposition()` creates a **runtime** `SdfMaxCompositionAsset` via `ScriptableObject.CreateInstance`. It is assigned to `PlanetBody.composition` and serialized **in the scene** when you save. For a reusable asset file, create one via **SDF Max Composition Editor → Auto Calculate** (prompts for save path) or assign an existing asset under `Assets/Planetary/LittlePrince/`.

### 4.2 Bake LOD tiers in Edit Mode

1. `PlanetBody` inspector → **Rebake SDF LOD Mesh**.
2. Select the object with `PlanetarySdfLodRenderer`.
3. **MeshFilter → Mesh** should show `PlanetSdfLod_T{n}` (not None).

If still None:

| Cause | Fix |
|-------|-----|
| `profile` null | Assign `PlanetarySdfLodProfile` |
| `profile.tierGridRes` empty | Add at least one tier, e.g. `{ 16 }` |
| `body.composition` null | Rebuild Planet first |
| `PlanetarySdfLodRenderer` not under `PlanetBody` | Move component or assign `sdfLodRenderer` ref |

### 4.3 Material and shader

1. Assign **`lodMaterial`** on `PlanetarySdfLodRenderer`.
2. Material shader should be **`Planetary/SdfLod`** (horizon dither + detail coeff).
3. Enter Play — `LateUpdate` sets `_PlanetCenter`, `_HorizonSdfWeight`, etc. via `MaterialPropertyBlock` **only when `lodMaterial` is assigned**.

Without `lodMaterial`, the mesh can still assign to `MeshFilter`, but you get no horizon blending and may see Unity’s default pink material.

### 4.4 Play-mode startup path (after recent fix)

When **`rebuildOnPlayAwake` is off** and composition has nodes:

1. `PlanetBody.Awake` → `SyncPlayModeFromBakedComposition()` (no plate regression).
2. `ApplyCompositionToVolumeProvider(false)` — uses volume cache if hash unchanged.
3. `sdfLodRenderer.EnsureLodMeshes()` — bakes tiers **once** if list empty.

When **`rebuildOnPlayAwake` is on**, or composition is empty:

1. Full `RebuildAll()` → `RebakeComposition` + chunk meshes + `sdfLodRenderer.Rebake()`.
2. Expect **multi-second hitch** on first frame (grid sampling × tier count).

**Verify in Play:** Frame Debugger or Inspector on LOD object — `sharedMesh` should update per tier in `LateUpdate`.

---

## 5. Debug: SpatialVolumeProvider & SdfMaxMeshSurface

Use this section when `renderMode != None` or pathfinding/SDF queries fail.

### 5.1 Volume cache

1. Provider inspector → trigger **Rebuild Now** (or context menu on planet: Rebuild Planet).
2. `SpatialVolumeCacheRegistry.EnsureBuilt` runs from `RebuildIfDirty`.

Cache miss (full rebuild) when:

- `composition.GetInstanceID()` changes (every full `RebakeComposition`).
- `SurfaceMeshVersion` changes (profile grid/iso or composition id).
- `force = true` on rebuild.

**Play tip:** With baked composition and `ApplyCompositionToVolumeProvider(false)`, cache should hit on Enter Play.

### 5.2 StaticMesh mode

If `renderMode = StaticMesh`:

| Check | Expected |
|-------|----------|
| `solverProfile.generateSurfaceMesh` | `true` |
| `SdfMaxMeshSurface` enabled | Yes (auto-added) |
| `MeshRenderer` materials | Assigned on `SdfMaxMeshSurface` |
| Console | No repeated `SdfMaxSurfaceMesher.Build` every frame |

Surface rebuild is debounced; if `SurfaceMeshVersion` changes every frame (composition recreated each update), you get **continuous churn** — fix at composition stability (Section 6.3).

### 5.3 Query-only (`renderMode = None`)

Volume still builds for pathfinding / weather / narrative. No `SdfMaxMeshSurface` — **visual SDF must come from `PlanetarySdfLodRenderer`**.

---

## 6. Debug: play-mode performance churn

### 6.1 Open Perf Trace View

**Window → System Drawer → Diagnostics → Perf Trace View**

Enable **Auto-collect benchmark**, Enter Play, Exit Play. Open the saved run and look for:

| Scope label | Meaning |
|-------------|---------|
| `RebuildAll` | Full planet rebuild (should **not** appear every Play if `rebuildOnPlayAwake` off + baked composition) |
| `RebakeComposition` | Plate regression + composition bake |
| `SdfMaxSurfaceMesher.Build` | Grid iso-surface extraction (expensive) |
| `SyncRenderComponents` | Provider render mode sync |
| `EnsureBuilt` / integral tree | Volume backend build |

**Healthy Play enter (fast path):** little or no `RebuildAll`; at most one `SdfMaxSurfaceMesher.Build` per LOD tier if meshes were never baked in Edit Mode.

**Unhealthy:** `RebuildAll` + multiple `Build` calls + repeating scopes every frame → composition or version changing in a loop.

### 6.2 Common churn causes

| Cause | How to confirm | Fix |
|-------|------------------|-----|
| `rebuildOnPlayAwake` enabled | `RebuildAll` scope on every Play | Turn off; bake in Edit Mode |
| Empty `composition` on Play | Full rebuild in Awake | Rebuild Planet; save scene |
| `PlanetVolcanoController` / roads / interior updater rebaking | Scopes after specific actions | Expected on Activate; not every frame |
| `renderMode = StaticMesh` + unstable composition | `Build` every frame | Use `None` + LOD renderer, or stabilize composition ref |
| High `tierGridRes` | Long `Build` once per tier | Lower tiers for dev, e.g. `{ 8, 12, 16 }` |
| `RebuildIfDirty(true)` after every surface mesh | Forced volume rebuild | Fixed: mesh surface uses `EnsureVolumeBuilt(false)` |

### 6.3 Composition stability

Each `RebakeComposition()` replaces `PlanetBody.composition` with a **new** runtime asset. That bumps `GetInstanceID()` → invalidates volume cache and surface mesh version.

**Workflow that stays stable across Play sessions:**

1. Edit Mode: **Rebuild Planet**.
2. Save scene (embeds composition on `PlanetBody`).
3. **Rebake SDF LOD Mesh** in Edit Mode (stores meshes in baker for session; rebake again if composition changes).
4. `rebuildOnPlayAwake` = **off**.

For disk-backed composition, save an asset via SDF Max editor Auto Calculate and assign it to both `PlanetBody.composition` and `SpatialVolumeProvider.composition`.

---

## 7. Step-by-step debug session (recommended order)

Work through in order; stop when the symptom is explained.

### Step A — Edit Mode baseline

1. Select planet root.
2. **Rebuild Planet**.
3. **Rebake SDF LOD Mesh**.
4. Confirm `composition.nodes.Count > 0`.
5. Confirm LOD `MeshFilter.sharedMesh` is assigned.
6. Assign `lodMaterial` if missing.
7. **Save scene**.

### Step B — Play Mode smoke test

1. `rebuildOnPlayAwake` = **off**.
2. Enter Play.
3. Open **Perf Trace View** — note whether `RebuildAll` ran.
4. Select LOD object in hierarchy — mesh should remain assigned.
5. Move camera above horizon — shell should dither/blend per shader.

### Step C — If still invisible

1. Check **Game** vs **Scene** camera — same object visible in Scene?
2. **Frame Debugger** — is `PlanetarySdfLodRenderer` draw call present?
3. Layer / culling mask on camera vs renderer.
4. Scale: planet at origin? `PlanetRadius` matches scene scale (500 m)? Camera inside the sphere?

### Step D — If churn persists

1. Perf Trace: single spike vs every-frame scopes.
2. Search scene for `RebakeComposition` callers (`PlanetVolcanoController`, `RoadWeatherIntegration`, `PlanetInteriorPhysicsUpdater`).
3. Disable `PlanetInteriorPhysicsUpdater` auto updates temporarily to isolate.
4. Set `SpatialVolumeProvider.renderMode` to `None` if you do not need StaticMesh.

### Step E — Volcano / runtime edits

1. **Activate Volcano** (context menu) — expect **one** rebake + LOD rebake.
2. **Deactivate** — another rebake (removes cone).
3. If volcano works but baseline Play does not, baseline composition was never baked (return to Step A).

---

## 8. Console messages (non-fatal vs blocking)

| Message | Severity | Action |
|---------|----------|--------|
| `[SystemDrawerSceneServices] ... FindFirstObjectByType fallback` | Warning | Register service via `PlanetServiceWizardComponent` / weather wizard |
| `weather.physicsManifold` unresolved during rebake | Warning | Atmosphere estimate uses defaults; register manifold for full weather coupling |
| `SdfMaxMeshSurface and SdfMaxSkinnedMeshSurface both enabled` | Warning | Disable one render mode |
| Cloud bake / AlphaMask not readable | Unrelated to planet SDF | Enable Read/Write on texture import |
| Repeated `RebuildAll` in log (custom) | Investigation | Enable Perf Trace; check `rebuildOnPlayAwake` |

---

## 9. Unity Test Runner sanity checks

**Planetary.Tests**

- `PlanetarySdfLodBakerTests.RebuildTiers_ProducesMeshes` — baker + composition pipeline
- `PlanetaryCompositionLayerTests` — annular shell layering

**SdfMax.Tests**

- `PlanetSdfMaxRenderOrderTests.PlanetBody_RebuildAll_DoesNotThrow` — minimal host setup

Run after changing `PlanetBody`, baker, or composition profiles.

---

## 10. Review notes (setup doc & code, Jun 2026)

Findings from reviewing [LittlePrincePlanetSetup.md](./LittlePrincePlanetSetup.md) against current runtime behavior:

### Gaps addressed in this doc (consider adding to setup checklist)

1. **`rebuildOnPlayAwake`** — not documented in setup guide; default **off** avoids Play hitch.
2. **`solverProfile`** — required on `PlanetBody` but not listed in setup table; without it, volume provider and expression graph lack grid/iso settings.
3. **`lodMaterial`** — setup mentions SDF LOD profile but not the **material** slot; missing material explains “invisible” or unshaded horizon.
4. **`renderMode = None`** — setup hierarchy lists both provider and LOD renderer but does not state which draws the shell.
5. **Composition persistence** — “Bake Composition” menu calls `RebakeComposition()` in memory; saving the **scene** (or a `.asset` file) is required for fast Play path.
6. **Edit vs Play LOD bake** — tier meshes are runtime `Mesh` objects; rebake in Edit Mode before Play if composition changed.

### Code behavior summary (current)

| Component | Behavior |
|-----------|----------|
| `PlanetBody.Awake` | Fast path when baked composition + `!rebuildOnPlayAwake` |
| `PlanetarySdfLodRenderer.LateUpdate` | Assigns tier mesh even without material; MPB only if `lodMaterial` set |
| `PlanetarySdfLodRenderer.EnsureLodMeshes` | One-time tier bake when entering Play with empty baker |
| `SdfMaxMeshSurface` | `EnsureVolumeBuilt(false)` after surface build to reduce double rebuild |
| `RebakeComposition` | Always `CreateInstance` new composition — intentional for editor rebake; avoid on every Play |

### Suggested setup doc tweaks

See updated Section 5 and troubleshooting table in [LittlePrincePlanetSetup.md](./LittlePrincePlanetSetup.md) for cross-links and Play-mode rows.

---

## 11. Quick reference commands

| Action | Where |
|--------|--------|
| Rebuild Planet | `PlanetBody` inspector button / context menu |
| Bake Composition | Window → System Drawer → Planet → Bake Composition |
| Rebake SDF LOD Mesh | `PlanetBody` inspector / `PlanetarySdfLodProfile` inspector |
| SDF graph editor | Provider inspector → Open Composition Editor |
| Perf profile | Window → System Drawer → Diagnostics → Perf Trace View |
| Service keys | Window → System Drawer → Facilitator Hub |

---

*Last updated for `PlanetBody.rebuildOnPlayAwake` and `PlanetarySdfLodRenderer.EnsureLodMeshes` play-mode startup path.*
