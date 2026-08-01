# PersonaDayManager + Civil System LOD Lattice

Wakes civil retinues from Spatial4D causality context, applies gov-glove persona + biorhythm bias, and LOD-gates sims via FeatureBudget + velocity log falloff.

## Pipeline

1. Narrative clock `t` + player speed
2. Venues ordered by developer kind priority + `developerPriority`
3. Causality: `NarrativeVolumeQuery.Sample4D` ≥ `minCausalDepth`
4. Hours: `CronDue.IsActiveSchedule(hoursCron)` (mask form `* 11-22 * * *`)
5. LOD: `min(speedLogScale, FeatureBudget civil/society/pathing/narrative)` → FullSim / Proxy / Ghost / Culled
6. Wake: kitchen → `RestaurantVenueRuntime.SetOpen`; others → activate retinue + `CivilVenueBioRhythmService`
7. Persona: `LifeSystemsGovGloveBias` from `/api/persona-day/request` bundle
8. Travel: `CombatRulesFacilitatorService.CallToArms` / `WaypointGuidanceService.DriveAgentsTowardActive`
9. Rate-limited skips still increment **would-have-been** counters

## Speed LOD

When `v > developerMaxSpeedMps`:

```
lodScale = clamp(1 / (1 + log_b(1 + v/vmax)), lodFloor, 1)
```

FeatureBudget Auto floor still wins when granularity is lower.

## FeatureBudget

- Id: `civil_systems` (`FeatureBudgetIds.CivilSystems`)
- Consumer: `CivilSystemsFeatureBudgetConsumer` scales `PersonaDayManager.tickIntervalSeconds`

## Continuuuum

| Endpoint | Role |
|----------|------|
| `GET/POST /api/persona-day/request` | Persona + biorhythm seed multiplex |
| `GET /api/persona-day/venues` | Lattice catalog |
| `GET/PUT /api/persona-day/settings` | Civil LOD settings |
| Settings → **Civil LOD** | Kind priority, speed bounds, caps |

## Spatial wake

`SpatialGenerator.EnumeratePlacedInstances` / `CollectPlacedGameObjects` + `PlacedInstancesChanged`.  
`SpatialPersonaWakeBridge` (Bedoga) ingests into `SpatialRetinueWakeSource`.

## Types

`Assets/locomotion/pathing/persona/` — manager, lattice, LOD, cron, would-have-been, wake source, venue bio.
