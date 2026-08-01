# Kitchen, Restaurant, ChefCard, ThreatWarden

Sim hospitality stack (non-Toast): local menu/orders API, retinue pecking order, ChefCards, kitchen BioRhythm, ThreatWarden escalation.

## Continuuuum API

- Schema: `Scripts/continuuuum_restaurant_schema.sql`
- Routes: `/api/restaurant/*` + page `/restaurants`
- Order statuses: `queued|prep|plating|served|cancelled` (no payment)
- Retinue: persona, role, pecking_order, duty_cron, waypoint_group
- Retinue treemap: `GET /api/restaurant/{id}/retinue/treemap` — D3 hierarchy **derived** from pecking_order + manager role tokens (no stored parent/org-tree columns); ThreatWarden still sorts flat pecking
- ChefCard graph: `GET /api/restaurant/{id}/chef-card-graph` (d3 nodes/links)

## Unity types

| Type | Role |
|------|------|
| `KitchenBioRhythmService` | Venue heat/stress/service/cleanliness proxies |
| `ChefCard` / `ConsiderChefCards` | Duty checklist + `GoalType.Cooking` |
| `CookDutyNode` / `CookDutyBehaviorNode` | BT → `ChefActivitySolvers` |
| `ChefActivitySolvers` | filet/pour/dispense/cut/stir/heat/season/wash verbs |
| Recipe / meal / tray / taste | See [RecipeBehaviorTree.md](RecipeBehaviorTree.md) |
| Tear-down / dishwashing Hanoi | See [KitchenTearDownDishwashing.md](KitchenTearDownDishwashing.md) |
| `ChefMaterialEvolutionCard` | Phase → smell descriptors |
| `PanOilSmokeTracker` | Oil/smoke on sear/broil |
| `CardHistoryManager` | Snapshot ring buffer (default 5000); no live card refs |
| `RestaurantVenueRuntime` | Open/close + retinue waypoints + hygiene |
| `ThreatWarden` | Alert/threat levels per agency |
| `ThreatCard` / `JusticeCard` | Escalation + shut-off heat |
| `ThreatDialogBranch` | Pecking-order telecom suggestions |

## Lemmas

```
{P:chef|op=activity|activity=sear|mode=Line}
{P:chef|op=wash}
{P:taste|notes=sour,spicy|intensity=0.5}
{P:threat|op=raise|lemma=on-edge|agency=kitchen}
{P:threat|op=clear|alert=all-clear}
{P:have|op=putaway|item=flour|context=van-interior}
```

Alertness lemmas: `on-edge`, `all-clear`, `under-attack`, `potential-intruders`.

## Editors

- **Window → System Drawer → Active Cards** — active pool + history
- **Window → System Drawer → Cards/Chef**

## Smoke-detector call chain

1. Sensor BT → `ThreatWarden.RaiseThreat(SmokeDetectorBattery)`
2. `ThreatCard` assigned to nearest retinue by pecking order under context owner
3. `ThreatDialogBranch` telecom → building_maintenance / fire_department contacts
4. Parallel: water pour / extinguisher / grain; `JusticeCard.ShutOffHeat` if burn gate ok
5. `CardHistoryManager` snapshots Threat/Justice cards
