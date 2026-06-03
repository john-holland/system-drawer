# Planetary systems architecture

Planet surfaces use **PlanetaryPlanarBase** (multi-source height), **SdfMax** solver primitives (noise, stamps, lat/lon shell), **volleyball-stitched** cube-sphere meshes, **Continuum** tile streaming, and **progressive SDF horizon LOD**.

## Core components

- `PlanetBody` — composition rebake, planar context, chunk renderer, SDF LOD overlay, `IExternalHeightProvider`
- `PlanetaryCompositionBaker` / `PlanetaryCompositionProfile` — layered SDF (mantle, lava, crust, water, atmosphere, weather)
- `ElementalCompositionRulesEngine` / `MaterialRegressionService` — mineral regression and spherical Voronoi plates
- `PlateTectonicsPhysicsSolver` / `PlanetInteriorPhysicsUpdater` — plate stress and interior rebake
- `LavaPhysicsManifold` / `VolcanoStressSolver` — breaches, volcano cones, emission thermodynamics
- `PlanetaryHorizonLodController` / `PlanetaryWeatherStreamingService` — altitude + distance LOD, weather tiles API
- `PlanetarySdfLodRenderer` — nadir chunks + horizon SDF dither (see render stack below)
- `PlanetaryWeatherTimeTravelSystem` — undo/redo frames in `PlanetWeatherCache`
- `PlanetarySimulationScheduler` / `PhysicsManifoldPhaseShaderMap` — sparse sim and phase materials

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

See [SdfMaxArchitecture.md](../../SdfMax/docs/SdfMaxArchitecture.md) for SDF-as-source-of-truth.
