# Kitchen Tear-down + Dishwashing

## Season pan (`ChefSeasonPanCard`)

Modes:

- **InOven** — park seasoned pan in oven
- **OnStove** — park on stove
- **WipeOilAfterClean** — CleanStation then oil wipe (default)

Resets `PanOilSmokeTracker` smoke/oil; bumps kitchen cleanliness.

## Dishwashing (Towers of Hanoi)

Zones (soil gradient): **Dirty → Sink → Dishwasher → Dry**. Optional **Compost** (scraps only; `enableCompostZone=false` by default).

Rules:

- Only move the top dish of a zone stack
- Legal moves are forward along the soil rank (Sink→Dry shortcut allowed for drying-rack preference)
- Never put dirtier onto cleaner illegally

Layout heuristic: configure zone anchors **nearest → furthest from trash** along the kitchen lateral axis for highest clean-dish throughput (`DishWashingStation.SortZonesNearestTrash`).

Cards: `DishwashingCard` via `ConsiderDishwashingCards`. Bio-rhythm: `DishWashingStationBioRhythm` (dirty backlog, sink load, washer cycle, dry ready, throughput).

## Sink + IK

- `SinkSpringNozzleFixture` — spring tip + rinse liters; flow sentiment `stalled` / `almost` / `endless` / `flowing`
- Scrub: timing and/or flood-proxy rinse (not full SPH)
- `DishIkTrainingCatalog` — sponge pick/put, spray, scrub, place dishwasher / drying rack
- `DishwashBehaviorNode` — BT pick → scrub → place

## Tear-down branch

`KitchenTearDownBranch` after meal: seed Dirty zone, emit season-pan + dishwashing cards (`KitchenTearDownSettings` on recipe/meal assets).
