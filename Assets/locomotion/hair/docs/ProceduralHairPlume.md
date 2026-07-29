# Procedural Hair Plume (Lattice Waterfall)

Prebaked radial hair: SDF Max gaussian manifold → complete radial texture cache → realtime extrude with soft capsule tension and helmet tuck.

## Pipeline

1. **Author** `HairPlumeConfig` (azimuth/length bins, tip hold, gravity, capsules).
2. **Compose** SDF Max via `HairPlumeSdfComposer.ComposeGaussianPlume` (bake / authoring).
3. **Bake** lattice with `HairLatticeWaterfallBaker` → RGBA radial map.
4. **Bake** fiber diffuse/spec with `HairFiberMaterialBaker`; optional curves with `HairPassthroughShapeBaker`.
5. **Runtime** `HairPlumePhysicsDriver` uploads cache + 10 capsules to `Locomotion/HairPlume`.
   - **Plume SDF Composition (reference)** — slot for the baked `SdfMaxCompositionAsset` (rebake lattice / optional `SpatialVolumeProvider`). Radial texture remains what the shader samples.

Editor: **Window → System Drawer → Hair Lattice Bake** (`Assets/locomotion/Editor/HairLatticeBakeWindow.cs`).

Buttons (lattice features default **on**): Create Default Config, Apply Lattice Bake Defaults, Bake All, Bake Radial / SDF / Fiber only, Bake Hairline + Part Into Radial, Add / Refresh Hair Line Part Gizmo.


## Radial cache channels

| Channel | Meaning |
|---------|---------|
| R | Plume height / shell |
| G | Passthrough occlusion / capsule tension |
| B | Curve hold mask |
| A | Tip break energy |

## Hairline + pate aim

Innate on `HairPlumeConfig`:

| Field | Role |
|-------|------|
| `hairLineCurve` | Radius (and optional height) by azimuth — conical ring base |
| `hairLineAngleCurve` | Emergence tilt (deg) by azimuth |
| `conicalEmergenceCurve` | Flare along length |
| `centerPateLocal` | Crown / ferrule aim point |
| `pateAngleBlend` | Lerp emergence toward pate |
| `hairPartSpline` | Spline that **bisects** the gaussian (density valley along the part) |

`HairLineSampler.EmergenceRingPoint` / `EmergenceDirection` drive lattice bake and SDF ring capsules. `HairPartSampler.ApplyPartToGaussian` carves the part into bake heights.

**Gizmo:** `HairLinePartGizmo` (auto-added by `HairPlumePhysicsDriver`) draws the hairline ticks and a **bright green ribbon** along the part split path. Toggle `drawPartRibbon` / `drawHairline` on the component.

## Tip hold + gaussian flux

`plumeTipHold` ∈ [0, 1]:

- **0** — flat water: **radial flux** drives tip break / break-spread thinning
- **1** — high hold: **cumulative mass (integral of density)** preserves the plume; flux tip-break is suppressed

`HairGaussianFlux` separates:

| Term | Meaning |
|------|---------|
| Density ρ | `exp(-r²/(2σ²))` — height field |
| Radial flux \|F\| | `(r/σ²)·ρ` — \|∇ρ\| along the strand (tip break / outflow) |
| Cumulative mass | ≈ erf integral of ρ — hold / load along the strand |
| Part lateral flux | \|∇carve\| away from the part spline |

`gaussianFluxGain` scales flux contribution. `usePartLateralFlux` deepens the part valley using lateral flux.

Applied in bake (`SampleGaussianHeight` / tip A channel), physics integrate, and shader `lerp(breakSpread, held, tipHold)`.

## Capsule buffer (10)

| Slots | Source |
|-------|--------|
| 0–5 | `HairBodyCapsuleBinder` (head, chest/shoulders, L/R arm, L/R knee) |
| 6–9 | `HairColliderPrimitiveScanner` nearest-fit foreign/finger capsules |

Soft “toroidal balloon” response: no hard hair collision; tension writes G; mesh parts and reseals. `usePhysicsMaterials = false` uses shader bounce only.

## Helmet tuck

`HairHelmetTuckController` plays a φ-conic BT series (`HairHelmetTuckBehaviorTree`), then:

1. Caches `max(hairHeight, helmetInteriorHeight)`
2. Builds cover mask (angle + rim UV)
3. Disables physics per covered azimuth via `HairHelmetSectionCache`
4. Shader renders only pop-out sections; optionally invokes open/close host `RequestClose` / `RequestOpen`

## Key types

- `HairPlumeConfig`, `HairGaussianFlux`, `HairRadialTextureCache`, `HairCapsuleBuffer`
- `HairdoParams`, `HairdoBlend`, `HairdoPresetCatalog`, `HairdoSdfExpressionBuilder` / `HairdoSdfSexpr`
- `HairPlumePhysicsDriver`, `HairHelmetTuckController`
- Shader: `Locomotion/HairPlume`
- Editor: **Window → System Drawer → Hairdo Designer** (power diamond + obscene SDF sexpr)
- Editor: **Window → System Drawer → Hair Lattice Bake**

## Hairdo Designer

**Window → System Drawer → Hairdo Designer**

1. **Default Everything From Actor** — wires `HairPlumePhysicsDriver`, body capsules, scalp/head, config under `Assets/locomotion/hair/Baked/`.
2. **Power diamond** — each basic haircut row: **precedence** (int, default 0) | checkbox | weight slider. List sorts by precedence. Diamond shows blended **Front / Side / Back / Length**.
3. Enabled weights normalize and lerp continuous params; part mode comes from highest weight (lower precedence on ties).
4. **Curls** — blendable **Waves / Curls / Ringlets** rows plus fine-tune `curlAmount` / `curlFrequency` / `curlTightness`. Writes `HairPlumeConfig` curl fields → lattice R ripple, `HairPlume` helix offset uniforms, and helix capsules in the obscene SDF (`;; curls`).
5. **Obscene SDF** — regenerates a dense nested sexpr (`max`/`smax`/ring capsules/per-cut branches/part subtract/curl helices). Hand-edit the TextArea, then **Apply Expression To Composition** → `plumeSdfComposition`. Radial bake remains the realtime look.
