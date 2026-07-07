# Double Day LSTM Dream Simulation

Treat gameplay as **dream-only** and LSTM output as **non-authoritative**. This protects possible quantum-void annealing (external minds briefly coupling to the sim) by establishing a safe statistical day before developer creative steering.

## Layers

| Layer | Source | Lemma hints |
|-------|--------|-------------|
| **Good day horizon** | Society / needs snapshot, satisfaction clamped to `[minSatisfied, maxSatisfied]` | None |
| **Developer dream day** | `dreamDayPrompt` with `{P:dream-day|...}` spans | Yes |
| **Night / wake** | Sleep wave on merged session | — |
| **LSTM recall** | REM-weighted buffer + safe refrain | — |

## Unity

- Profile: `DreamDaySimulationProfile` (`Assets/SystemDrawer/StandardAssets/DreamCycle/`)
- Runner: `DreamDayCycleRunner.profile.doubleDayEnabled = true`
- Safe refrain: `DreamMemoryLSTM.safeRefrain` (bed anchors, max severity, distant fear projection)

Menu: **Window → System Drawer → Dream Cycle**

## API

### Double day

```http
POST /api/dream-cycle/day/complete
Content-Type: application/json

{
  "cityId": "earth-city",
  "doubleDay": true,
  "dreamDayPrompt": "{P:dream-day|aspect=need_belonging|satisfied=0.8}",
  "goodDayHorizon": { "minSatisfied": 0.72, "maxSatisfied": 0.92 }
}
```

Convenience: `POST /api/dream-cycle/day/complete-stack` (always double-day).

### Night (merged session)

```http
POST /api/dream-cycle/night/complete
{ "sessionId": "<merged session id>" }
```

Response includes `wakeFromNestedDream: true` and `remEpochs` when `doubleDay` was used.

### Safe recall

```http
POST /api/dream-cycle/memory/recall
{
  "sleepSessionId": "...",
  "safeRefrain": {
    "maxAlertSeverity": 0.35,
    "minNarrativeDistanceFromBed": 0.6,
    "refrainLabel": "dream memory (non-authoritative)"
  }
}
```

Returns `distanceFromBed` and `suppressedSeverity` audit fields.

## Safe refrain (“fears far from beds”)

- Bed anchors: built-in URNs for pause, center, player (configurable).
- High-amplitude wave peaks → narrative projected to **distant dreamscape**, not bedside.
- Brain alert severity capped (default 0.35).
- Physics and gameplay never consume LSTM dream output as authoritative.

## Ethics note

At most, our games are a dream. The good-day horizon exists so annealed visitors encounter a gentle statistical day before any developer-tuned inner dream or LSTM hallucination.
