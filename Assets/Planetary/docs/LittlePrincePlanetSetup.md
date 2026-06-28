# Little Prince planet setup (radius 500)

Guide for configuring asteroid **B-612** from *The Little Prince*: a story-small but geologically layered world with **core, mantle, lava, crust**, thin atmosphere, and an **activatable volcano**.

Use **`planetRadius = 500`** (1 km diameter, ~3.1 km equatorial walk). Radius 10 is too small for interior shells; radius 500 keeps the fable scale while leaving room for molten layers and a vent you can trigger at runtime.

See also [PlanetaryArchitecture.md](./PlanetaryArchitecture.md).

---

## Scale reference

Fractions are relative to planet radius **R = 500 m**.

| Zone | Radial range | Composition layer |
|------|----------------|-------------------|
| Core | 0 → 0.25 R (125 m) | Core |
| Mantle | 0.25 R → 0.85 R | Mantle |
| Lava | 0.85 R → 0.95 R | Lava |
| Crust | 0.95 R → R (+ relief) | Crust |
| Atmosphere / clouds | R → ~1.8 R | Atmosphere, Weather |

| Quantity | Earth default | Little Prince (R = 500) |
|----------|---------------|-------------------------|
| Planet radius | ~6 371 km | **500 m** |
| Core outer | — | **125 m** |
| Lava shell | −2 km offset | **425–475 m** from center |
| Crust thickness | 5 km | **25 m** |
| Surface relief (auto) | 500 m cap | **25 m** (`5% × R`, clamped) |
| Cloud base / top | 1 / 3 km MSL | **50 / 150 m** above surface |
| Troposphere top | 12 km | **400 m** |
| Volcano cone | — | **15–30 m** radius at vent |
| Planar stamp (baobab) | 500 m | **30–80 m** |

---

## 1. Profile assets

Create `Assets/Planetary/LittlePrince/` and add ScriptableObjects (**Create → Planetary → …**).

### Composition profile (`LittlePrinceComposition`)

Use **shellOffsetM** as outer boundary offset from **R** (negative = deeper). **shellThicknessM** is the band width.

| Layer | Enabled | shellOffsetM | shellThicknessM | smoothRadius | weight | Notes |
|-------|---------|--------------|-----------------|--------------|--------|-------|
| Core | **on** | **−375** | **125** | 10 | 1 | 0 → 125 m (`0.25 R`) |
| Mantle | **on** | **−75** | **300** | 15 | 1 | 125 → 425 m |
| Lava | **on** | **−25** | **50** | 5 | 0.85 | 425 → 475 m |
| Crust | **on** | **0** | **25** | 3 | 1 | 475 → 500 m + relief |
| Water | off | 0 | 10 | 2 | 0.3 | Optional |
| Atmosphere | **on** | 0 | 400 | 20 | 0.25 | Shell above crust |
| Weather | **on** | 0 | 150 | 10 | 0.4 | Cloud band |

Layers bake as **annular shells** (except core center fill). Weather sits **above** crust, not inside rock.

**Quick formulas** (any R):

```
coreOffset     = -0.75 × R
coreThickness  =  0.25 × R
mantleOffset   = -0.15 × R
mantleThickness=  0.60 × R
lavaOffset     = -0.05 × R
lavaThickness  =  0.10 × R
crustThickness =  0.05 × R
```

### Atmosphere profile (`LittlePrinceAtmosphere`)

| Field | Value |
|-------|-------|
| `cloudBaseM` | **50** |
| `cloudTopM` | **150** |
| `troposphereTopM` | **400** |
| `pressureScaleHeightM` | **80** |
| `cloudDensityCoeff` | **0.35** |

### Planar base (`LittlePrincePlanarBase`)

- Low-amplitude procedural noise (frequency ~0.02).
- **Planar Feature** for baobab / rose: `footprintRadiusMeters` **40–80**, `strength` 0.3–0.5.

### SDF & horizon LOD

**Planetary SDF LOD** — `tierGridRes` `{ 12, 24, 32 }`, `nearFullSdfKm` **0.5**, `farFullSdfKm` **2**, `sdfHorizonMinAltM` **50**, `sdfHorizonFullAltM` **400**.

**Horizon LOD** — `fullSimRadiusKm` **1**, `fullSimAltitudeMaxM` **400**, `horizonDistanceKm` **2**, `surfaceBandMaxM` **150**, `troposphereMaxM` **400**.

---

## 2. Scene hierarchy

```
LittlePrince (root)
├── PlanetBody
├── PlanetRenderer
├── PlanetMeshStreamingService
├── SpatialVolumeProvider
├── PlanetarySdfLodRenderer
├── PlanetInteriorPhysicsUpdater
├── PlateTectonicsPhysicsSolver      (optional plate stress)
├── LavaPhysicsManifold
├── VolcanoStressSolver              (optional auto breach scan)
├── PlanetVolcanoController          ← activatable vent
├── PlanetShellManifoldGrid
├── PlanetPhysicsManifoldBridge
├── PlanetShellToWeatherManifoldAdapter
├── WeatherPhysicsManifold           (scene or child)
└── PlanetServiceWizardComponent
```

### PlanetBody

| Field | Value |
|-------|-------|
| **planetRadius** | **500** |
| **planarBase** | `LittlePrincePlanarBase` |
| **compositionProfile** | `LittlePrinceComposition` |
| **meshResolution** | **24–32** |
| **chunksPerFace** | **2** |
| **societyPlanetId** | `little-prince` |

Assign `interiorUpdater` → `PlanetInteriorPhysicsUpdater` on the same root.

### Planet Shell Manifold Grid

`latCount` **12**, `lonCount` **24**, `altitudeBandCount` **4**, `shellOuterRadiusMultiplier` **1.08** (top band ≈ 540 m).

---

## 3. Activatable volcano

[`PlanetVolcanoController`](../Runtime/Tectonics/PlanetVolcanoController.cs) erupts a cone on the baked SDF and stamps a magma plume in the weather manifold.

### Setup

1. **Add Component → Planetary → Planet Volcano Controller** on the planet root.
2. Assign **planet** → `PlanetBody`.
3. Set vent **latitudeDeg** / **longitudeDeg** (default `12°, 45°` — adjust in Scene view).
4. Tune **coneRadiusMeters** (**20** is a good start at R = 500), **gasPressure**, **ventTemperatureC**.

### Trigger eruption

**In editor (play mode or after bake):**

- Inspector → context menu **Activate Volcano** / **Deactivate Volcano**
- Or call from script / narrative:

```csharp
GetComponent<PlanetVolcanoController>().Activate();
GetComponent<PlanetVolcanoController>().Deactivate();
GetComponent<PlanetVolcanoController>().Toggle();
```

**What happens on Activate:**

1. Rebakes baseline composition (core → crust → sky).
2. Appends a **SmoothMax** volcano cone at the lat/lon vent (world-aligned `localPosition`).
3. Rebakes SDF LOD / volume provider.
4. Sets weather cell at vent to `WeatherMode.MagmaPlume`.

**Deactivate** rebakes without the cone.

### Optional auto pipeline

- **LavaPhysicsManifold** + **VolcanoStressSolver** — stress-driven candidates from lava breaches (`RefreshNearestPlayerFirst`).
- **PlanetInteriorPhysicsUpdater** → **Update Interior Planet Physics** — plate regression + composition rebake from inspector.

For a single story vent, **PlanetVolcanoController** alone is enough.

---

## 4. Weather & clouds

- `WeatherPhysicsManifold` registered as `weather.physicsManifold`.
- Cloud altitudes in world space (planet at origin): surface ≈ **500 m**, clouds **550–650 m**, tropopause ~**900 m**.
- Match `Cloud` / Cloud Bake `cloudBaseM` / `cloudTopM` to `LittlePrinceAtmosphere`.

---

## 5. Bake workflow

1. Select **PlanetBody** → **Rebuild Planet**.
2. **Window → System Drawer → Planet → Bake Composition**.
3. **Update Interior Planet Physics** (if using plates).
4. Place player near vent lat/lon → **Activate Volcano** on controller.
5. **Rebake SDF LOD Mesh** if horizon mesh looks stale.

---

## 6. Verify

**Tests** (Unity Test Runner → Planetary.Tests):

- `PlanetarySurfaceFrameTests` — outward normals, spherical UV
- `PlanetaryCompositionLayerTests` — annular weather above crust

**Manual:**

- Equator walk ≈ **3.14 km**.
- Sample radii from center: core **≤ 125 m**, lava band **425–475 m**, surface **~500 m**, clouds **550–650 m**.
- Activate volcano → visible cone bump + magma mode at vent.

---

## 7. Society API (optional)

```http
POST /api/society/planets
{ "planetId": "little-prince", "displayName": "Asteroid B-612" }
```

Match `PlanetBody.societyPlanetId`.

---

## 8. Troubleshooting

| Symptom | Fix |
|---------|-----|
| No interior layers visible | Enable Core/Mantle/Lava in composition; **Rebuild Planet** |
| Lava band outside planet | Check offsets: lava outer = R + offset must be `< R` |
| Clouds inside rock | Raise `cloudBaseM`; confirm annular weather bake |
| Volcano at world origin | Update to controller that passes planet **Transform** to cone bake |
| Crust SDF huge vs mesh | Relief now scales as `5% × R`; rebake after upgrade |
| Cone too small/large | Adjust `coneRadiusMeters` (min ~10 from stress solver) |

---

## 9. Checklist

- [ ] `planetRadius = 500`
- [ ] Core / Mantle / Lava / Crust enabled with table offsets (or formulas)
- [ ] `LittlePrinceAtmosphere`: cloud base **50**, top **150**
- [ ] `PlanetVolcanoController` wired, vent lat/lon set
- [ ] **Rebuild Planet**
- [ ] **Activate Volcano** once to verify vent
- [ ] Cloud band above **550 m** from center

Still a little prince world — you can walk it in minutes — but now it has a beating core, a lava pocket, and a volcano you can wake on cue.
