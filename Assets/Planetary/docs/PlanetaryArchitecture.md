# Planetary systems architecture

Planet surfaces use **PlanetaryPlanarBase** (multi-source height), **SdfMax** solver primitives (noise, stamps, lat/lon shell), **volleyball-stitched** cube-sphere meshes, **Continuum** tile streaming, and **progressive SDF horizon LOD**.

## Core components

- [`PlanetBody`](../../Runtime/PlanetBody.cs) — composition rebake, planar context, chunk renderer, SDF LOD overlay, `IExternalHeightProvider`
- `PlanetaryCompositionBaker` / `PlanetaryCompositionProfile` — layered SDF (mantle, lava, crust, water, atmosphere, weather)
- `ElementalCompositionRulesEngine` / `MaterialRegressionService` — mineral regression and spherical Voronoi plates
- [`PlanetInteriorPhysicsUpdater`](../../Runtime/Tectonics/PlanetInteriorPhysicsUpdater.cs) / `PlateTectonicsPhysicsSolver` — plate stress and interior rebake
- [`LavaPhysicsManifold`](../../Runtime/Lava/LavaPhysicsManifold.cs) / `VolcanoStressSolver` — breaches, volcano cones, emission thermodynamics
- `PlanetaryHorizonLodController` / `PlanetaryWeatherStreamingService` — altitude + distance LOD, weather tiles API
- `PlanetarySdfLodRenderer` — nadir chunks + horizon SDF dither (see render stack below)
- [`PlanetaryWeatherTimeTravelSystem`](../../Runtime/TimeTravel/PlanetaryWeatherTimeTravelSystem.cs) — undo/redo frames in `PlanetWeatherCache`
- `PlanetarySimulationScheduler` / `PhysicsManifoldPhaseShaderMap` — sparse sim and phase materials

## Shell manifold & physics bridge

Planet-centric indexing for scalar weather fields and pathing (replaces axis-aligned AABB-only sampling at poles and the lon seam).

| Component | Role |
|-----------|------|
| [`PlanetShellManifoldGrid`](../../Runtime/Bridges/PlanetShellManifoldGrid.cs) | `(latBin, lonBin, altitudeBand)` cells; pole caps at \|lat\| ≥ 89°; longitude wrap |
| [`PlanetShellToWeatherManifoldAdapter`](../../Runtime/Bridges/PlanetShellToWeatherManifoldAdapter.cs) | Write-through to [`WeatherPhysicsManifold`](../../../Weather/WeatherPhysicsManifold.cs) via cell centers |
| [`PlanetPhysicsManifoldBridge`](../../Runtime/Bridges/PlanetPhysicsManifoldBridge.cs) | Stamp surface friction/porosity from composition rebake |
| [`CanonicalSpatiotemporalField`](../../Runtime/Field/CanonicalSpatiotemporalField.cs) | Chart pullbacks blended with horizon LOD transition weights |
| [`SphericalCoordinates`](../../Runtime/SphericalCoordinates.cs) | World ↔ lat/lon/radius conversion |

**Service key:** `planet.shellGrid` (registered by [`PlanetServiceWizardComponent`](../../../SystemDrawer/PlanetServiceWizardComponent.cs)).

**Editor:** Physics Bridge Editor → shell grid panel, rebuild/sync, scene overlay ([`PhysicsBridgeEditorWindow.ShellGridPanels.cs`](../../../SystemDrawer/Editor/PhysicsBridgeEditorWindow.ShellGridPanels.cs)).

**Tests:** [`PlanetShellManifoldGridTests`](../../Tests/PlanetShellManifoldGridTests.cs) — lon wrap, pole caps, round-trip ε.

## Pathing on the shell

| Component | Role |
|-----------|------|
| [`PlanetShellPathingSolver`](../../Runtime/Pathing/PlanetShellPathingSolver.cs) | Great-circle slerp edges on [`PhysicalPathingMedium.Space`](../../../HierarchicalPathFinding/PhysicalPathingSolverRegistry.cs) when shell grid is present |
| [`CurvedSpacetimeSd2PathingSolver`](../../Runtime/Pathing/CurvedSpacetimeSd2PathingSolver.cs) | Metric-weighted paths; slerp when [`PlanetPathingBackend`](../../Runtime/Pathing/PlanetPathingBackend.cs) resolves a planet |
| [`PhysicalMediumVolumeIndex`](../../../HierarchicalPathFinding/PhysicalMediumVolumeIndex.cs) | `TryResolveAltitudeBand` samples shell grid at a world position |

## Scene services

Planet and weather code resolve dependencies through canonical keys (not `FindObject`):

- [`SystemDrawerServiceKeys`](../../../SystemDrawer/SystemDrawerServiceKeys.cs) — `planet.body`, `weather.physicsManifold`, `planet.shellGrid`, …
- [`SceneServiceLookup`](../../../Weather/SceneServiceLookup.cs) — cross-assembly bridge from Planetary / Roads / Locomotion

## Render stack (high altitude)

1. **Nadir** — `PlanetRenderer` volleyball chunks (streamed via `PlanetMeshStreamingService`)
2. **Horizon** — `PlanetarySdfLodRenderer` with `Planetary/SdfLod` shader (`_HorizonSdfWeight`, `_RevealAmountNadir`)
3. Handoff raises `revealNadir` when tile coverage under the player is sufficient

## Continuum API

- `GET/POST /api/planet/tiles`
- `GET/POST /api/planet/weather_tiles` (`cloud_base_m`, `cloud_top_m`, `cloud_cover`, `pressure_scale_height`, `altitude_band_mask`)
- `GET/POST /api/planet/composition`
- `GET /api/planet/gpx`
- `POST /api/planet/google/shapes`

## Editor

- `Window/System Drawer/Planet/Bake Composition`
- `PlanetBody` inspector: Rebuild Planet, Rebake SDF LOD Mesh, Update Interior Planet Physics
- `Window/System Drawer/Physics/Physics Bridge Editor` — validate bridges and shell grid

See [SdfMaxArchitecture.md](../../SdfMax/docs/SdfMaxArchitecture.md) for SDF-as-source-of-truth.
