# SharedDimensionalGenericCache

Client runtime cache for Continuuuum **game dimensions**: share positional / optional velocity / aesthetic lemma state instead of cold-reloading systems like `TravelAgent`.

## Flow (dimension switch)

1. `SharedDimensionalGenericCache.CaptureFromScene(game, fromDim)`
2. `CopySharedPositional(fromDim, toDim)` — pose + velocity bridge
3. `DimensionParticleCleanup.Run` on outgoing roots
4. `DimensionMaterialCrossFader` drives `DimensionalShaderComponent` jobs (`_DimBlend`)
5. SG warm apply via `DimensionSwitchCache`
6. KeepAlive / AestheticOnly / ReplaceActor bindings
7. `DimensionalTravelAgentKeepAlive` — skip `RebuildCachedPlan` when goal unchanged
8. Restart particle emission on KeepAlive hosts

## Actor policy (`dimensional-actor-policy`)

| Value | Behavior |
|-------|----------|
| `keep-alive` (default) | Same GameObject; restore pose/velocity; no TravelAgent plan rebuild if goal stable |
| `aesthetic-only` | Same as KeepAlive for transform; aesthetics/paint overlay focus |
| `replace` | Disable old; spawn `replacePrefab`; seed pose/velocity from cache |

## Components

| Component | Role |
|-----------|------|
| `DimensionalLemmaBinding` | lemmaEntryId + instanceStableId + policy; registers with cache |
| `DimensionalLemmaPosition` | Authoritative world pose for positional lemmas |
| `DimensionalLemmaVelocityBridge` | Optional RB / RB2D linear+angular into the same slot |
| `DimensionalShaderComponent` | Explicit fade driver params (kind, blend prop, duration, curve, renderers, fallback) |
| `DimensionMaterialCrossFader` | Collects shader components and runs dual-slot fade |
| `DimensionParticleCleanup` | Stop/Clear; destroy `DimLocal*` orphans; restart emission |

## DimensionalShaderComponent fields

- `enabledForDimSwitch`, `materialKind`, `blendPropertyName` (default `_DimBlend`; sky → `_BlendWeight`)
- `durationSeconds`, `blendCurve`, `includeChildren`, `renderers`, `particleSystems`
- `useMaterialPropertyBlock`, `commitOnComplete`, `fallbackMode` (`HardCutAtHalf` / `AlphaDither` / `Skip`)
- `dissolvePropertyName`, `shaderGlobals`, `lemmaEntryId`, `allowLemmaOverride`
- `openCloseBtByDimension` — sparse `dimIndex → OpenCloseTopologyAsset` with `runtimeMilliseconds` (`-1` = default), `runOnEnter` / `runOnExit`. Fired by `DimensionSwitchCache` via `NotifyDimensionSwitch` / `BeginOpenCloseForDimension`; Locomotion registers `IDimensionalOpenCloseRunner`.

## Shaders

Under `Assets/Continuuuum/Dimensions/Shaders/`:

- `Continuuuum/Dimensions/CrossFadeMesh`
- `Continuuuum/Dimensions/CrossFadeParticle`
- `Continuuuum/Dimensions/CrossFadeWater`
- `Continuuuum/Dimensions/CrossFadeFire`
- `Continuuuum/Dimensions/CrossFadeSdfMax`

All expose `_DimBlend` (0 = A, 1 = B).
