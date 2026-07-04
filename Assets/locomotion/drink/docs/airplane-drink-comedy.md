# Airplane drink comedy

Demo scene and beat wiring for in-flight turbulence + partial raise + spill closure.

## Quick setup

1. **GameObject → Locomotion → Airplane Drink Comedy Scene** — creates `Assets/Scenes/airplane_drink_comedy.unity`.
2. **GameObject → Locomotion → Airplane Coffee Cup** — saves `Assets/locomotion/drink/Prefabs/AirplaneCoffeeCup.prefab`.
3. Assign ragdoll / `Consider` targets as needed; press Play with `AnimationPlaybackPolicyContext` script text below.

## Beat script

```
{P:beat1} She tries to {drink} the {coffee} almost to her {mouth} while {turbulence} hits.
{P:beat2} {stalled} — the cup hovers, shaking.
{P:beat3} {drink} again — {spilled} everywhere.
{P:beat4} {empty-handed} on the {tray}.
```

## Lemma props on `drink`

| Key | Value |
|-----|-------|
| `drink-efficacy` | `0.25` |
| `sip-count` | `3` |
| `train-for-perfect-drink` | `false` |
| `closure-mode` | `auto` |

## Actor hierarchy

- `Consider`, `NervousSystem`, `AnimationPlaybackPolicyContext`
- `LiquidConsumptionLedger`, `LemmaConsumptionClosure`, `CabinTurbulenceDriver`
- `DrinkFromVesselNode` → cup prefab reference
- Scene `WeatherService` with `WeatherPhysicsManifold` + `LiquidWeatherManifoldBridge`
- Cabin `DrinkSpillSurfacePool` for tray stains

## Closure matrix

| Beat | Lemmas | Closure | Liquid outcome |
|------|--------|---------|----------------|
| 1 | drink, almost, mouth, turbulence | `mouth` (auto) | Low mouth delivery, some spill |
| 2 | stalled, turbulence | `stalled` | Raise only; no vessel debit |
| 3 | drink, spilled | `spill-beat` | High spill on tray/manifold |
| 4 | empty-handed | phrase consumed | Placement beat (tray) |

## Fantasia stub

**GameObject → Locomotion → Fantasia Drain Comedy Scene** creates `Assets/Scenes/fantasia_drain_comedy.unity` with `infinite-drain: true` on the bucket flow model and rolling-sphere flood painting the weather manifold.

See [weather-liquid-integration.md](../../liquid/docs/weather-liquid-integration.md) for manifold bake workflow.
