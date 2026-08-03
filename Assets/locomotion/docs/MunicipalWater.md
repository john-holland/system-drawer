# Municipal Water

Ubiquitous city water supply for toilets, sinks, and showers.

## Service

`MunicipalWaterService` (`civil.municipalWater`) auto-creates if missing. Channels: `supplyPressure01`, `hotSupply01`, `coldSupply01`, `sewerCapacity01`. Scale via `MunicipalWaterLemmaBias` (drought / boost / off tokens).

## Fixtures

| Component | Role |
|-----------|------|
| `FixturePlumbingNode` | Cold/hot/drain branches, clog, overflow/blow flags |
| `BuildingPlumbingGroup` | Group id; optional flush→hot cross-talk |
| `ToiletFixture` | Flush liters + overflow jet |
| `SinkFixture` / `ShowerFixture` | Tap/valve; blow-off **off** by default |

Cross-talk flags (default **off**): `sinkGetsHotWhenToiletFlushed`, `showerGetsHotWhenToiletFlushed`.

## Overflow

- Toilet overflow jet **on** by default → `ToiletOverflowJetDriver`
- Layers: ceiling → roof → sky spout
- Sink/shower pressure-gauge blow **off** unless enabled

## Plumber cards

`ClogToiletCard`, `PlungeToiletCard`, `SnakeToiletCard` + `GoalType.Plumbing` / `ConsiderPlumbingCards`.

## Continuuuum

`GET/PUT /api/civil/municipal-water`
