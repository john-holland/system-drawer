# Weather / liquid integration

Drink and spill systems read and write the scene **`WeatherPhysicsManifold`** through **`LiquidWeatherManifoldBridge`**.

## Bridge API

| Method | Purpose |
|--------|---------|
| `ResolveManifold()` | Find assigned or scene `WeatherPhysicsManifold` |
| `SampleAt(worldPos)` | Read cell velocity, pressure, `WeatherMode` |
| `PaintWaterSphere(center, radius, velocity, pressurePa)` | Kalman-blend water cells at nozzle/sphere |
| `PaintSpillFootprint(hit, liters, spread)` | Floor spill footprint from ledger/spill pool |

Attach the bridge on the weather service root or reference it from:

- `LiquidConsumptionLedger.weatherBridge`
- `DrinkFlowModel.weatherBridge`
- `RollingSphereFloodSimulator.weatherBridge`
- `DrinkSpillSurfacePool.weatherBridge`

## Flow bake

1. Place `WeatherPhysicsManifold` in scene (cabin bounds ~4×3×6 m, cell resolution ~0.25 m).
2. Open **Window → Continuum → Drink Flow Bake**.
3. Assign `DrinkFlowModel`, manifold, and optional bridge.
4. Bake — each step paints water cells and records curves to `DrinkFlowBakeAsset`.
5. `DrinkStreamRenderer` samples live manifold at stream tip (falls back to baked curves).

## Rolling sphere flood

`RollingSphereFloodSimulator` spawns `WaterPhysicsApproximationSphere` instances from an `OpenEdgeLoopSpoutSimulator` rim. Active spheres call `PaintWaterSphere` each frame (LOD via `paintEveryNthSphere`).

With **`infinite-drain`**, spawn rate tracks flow even when vessel volume is refilled by the ledger.

## Airplane vs Fantasia

| Mode | Scene | Vessel | Closure |
|------|-------|--------|---------|
| Airplane | `airplane_drink_comedy.unity` | 0.2 L open cup | auto → mouth / stalled / spill-beat |
| Fantasia | `fantasia_drain_comedy.unity` | Endless bucket | `infinite-drain-beat` + timer |

## Related docs

- [airplane-drink-comedy.md](../../drink/docs/airplane-drink-comedy.md)
- [drink-lemma.md](../../drink/docs/drink-lemma.md)
