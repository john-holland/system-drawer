# Dream day lemma grammar

Use `{P:dream-day|...}` spans to steer per-need 2D spatial generator slots during the **developer dream day** (inner layer of the double-day stack).

> **Note:** `{P:dream-day}` applies to the inner developer dream day only. The outer **good day horizon** is statistical (society snapshot + satisfaction floors) with no lemma hints. See [`DreamDoubleDay.md`](../../SystemDrawer/docs/DreamDoubleDay.md).

## Syntax

```
{P:dream-day|aspect=need_belonging|spatial2d-slot=need_belonging|satisfied=0.8}
```

## Parameters

| Key | Description |
|-----|-------------|
| `aspect` | Need aspect id: `need_physiological`, `need_safety`, `need_belonging`, `need_esteem`, `need_self_actualization` |
| `spatial2d-slot` | Target `SpatialGenerator` slot id (TwoDimensional mode) |
| `satisfied` | Optional 0–1 hint for day satisfaction before sleep collapse |

## API

- `POST /api/dream-cycle/lemma/compile` — parse spans from prompt text
- `POST /api/dream-cycle/day/complete` — run city day with optional `dayPrompt`

## Unity

`DreamDayCycleRunner` reads compiled hints and assigns seeds to `NeedAspectSpatialSlot` generators.
