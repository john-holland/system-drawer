# Drink lemma

Built-in verb: `urn:lemma:en:verb:drink`

## Property keys

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `drink-animation-ref` | String | — | `DrinkAnimationReference` asset path |
| `auto-middle-mouth-jaw` | Bool | true | Auto middle mouth / jaw anchor |
| `jaw-tilt-animation-audit-insert` | Bool | false | Jaw tilt audit window |
| `hold-without-return` | Bool | false | Skip return cards after hold |
| `put-without-release` | Bool | false | Skip release after put |
| `nozzle-loop-enabled` | Bool | false | Continuous pour loop clip |
| `liquid-simulation-enabled` | Bool | true | Local liquid sim |
| `place-nozzle-on-mouth` | Bool | false | IK nozzle on mouth |
| `drink-efficacy` | Float 0–1 | 0.7 | Mouth delivery fraction |
| `sip-count` | Int ≥ 1 | 1 | Sips to imbibe over |
| `total-volume-liters` | Float | 0 | Volume target (L); UI shows US fl oz |
| `partially-raise-amount` | Float 0–1 | 1 | Fraction of full raise toward mouth |
| `partial-raise-default-when-stalled` | Float 0–1 | 0.65 | Default when `almost`+`mouth` or `stalled` |
| `train-for-perfect-drink` | Bool | false | Score zero-spill training |
| `max-spill-liters-tolerance` | Float | 0.05 | Spill cap when training |
| `closure-mode` | String | auto | See closure modes below |
| `mouth-volume-liters-target` | Float | 0 | Mouth delivery target (0 = sip math) |
| `infinite-drain` | Bool | false | Fantasia: vessel never depletes |
| `infinite-drain-closure-seconds` | Float | 0 | Close infinite-drain beat after N seconds |

### Closure modes

| Value | When beat closes |
|-------|------------------|
| `auto` | Infer from lemmas (`stalled`, `spilled`, `endless`) |
| `mouth` | Mouth volume or sip count reached |
| `empty-vessel` | Vessel volume ≈ 0 |
| `stalled` | Partial raise done, dispense suppressed |
| `spill-beat` | Spill exceeds threshold |
| `infinite-drain-beat` | Timer or spill/mouth target (Fantasia) |

## Comedy lemmas

Built-in registry terms used by partial raise and closure: `almost`, `lips`, `mouth`, `coffee`, `turbulence`, `tray`, `stalled`, `spilled`, `empty-handed`, `endless`.

## Prefab setup

1. Add `DrinkVesselComponent` + optional `interiorMeshCollider`.
2. Open cup: `OpenEdgeLoopSpoutSimulator` on rim (no nozzle required).
3. Closed pour: child `DrinkNozzleComponent` with `tip` transform.
4. Add `DrinkLiquidContent`, `DrinkFlowModel`, optional `DrinkVesselInteriorLoopFinder`.
5. On actor: `DrinkMouthAnchor`, `DrinkMouthJawAligner`, `LiquidConsumptionLedger`, `LemmaConsumptionClosure`.
6. Assign `DrinkAnimationReference` in Lemma Properties window.

## Bake workflow

1. Place scene `WeatherPhysicsManifold` (see [weather-liquid-integration.md](../../liquid/docs/weather-liquid-integration.md)).
2. Open **Window → Continuum → Drink Flow Bake**.
3. Assign `DrinkFlowModel` and manifold bridge; bake to `DrinkFlowBakeAsset`.
4. Wire asset on `DrinkStreamRenderer`.

## BT

Use `DrinkFromVesselNode` with vessel GameObject. Resolves properties via `AnimationPlaybackPolicyContext.GetDrinkProperties()`. Skips beats when `IsPhraseConsumed` is true.

Partial raise from `LiquidPartialRaiseResolver`; closure from `LemmaConsumptionClosure.TryClose`.

See [airplane-drink-comedy.md](airplane-drink-comedy.md) for the full demo scene.
