# Station Placards + Hierarchy

Hierarchical personnel-functionality stations (cooking, train, bus, computer desk) under company/gov buildings, bound to SG4D causality leaves.

## Continuuuum

| Endpoint | Role |
|----------|------|
| `GET/PUT /api/stations?cityId=` | List / replace placards |
| `GET /api/stations/assemblage` | Force-graph nodes/links |
| `GET /api/stations/treemap` | Squarified hierarchy JSON |
| `PUT /api/stations/level-stats` | Unity editor upload |
| Page | `/stations` (Treemap / Graph / Placards) |

Schema: `Scripts/continuuuum_stations_schema.sql` — placards, commodities, assignments, level_stats.

## Unity

| Type | Role |
|------|------|
| `StationHierarchyNode` | Scene placard: kind, parent, leaf id, config |
| `StationConfig` | Commodities + assignments + kitchen/computer/vehicle refs |
| `StationRegistry` | Enumerate hierarchy + build upload DTO |
| `SpatialGenerator4DOrchestrator.EnumerateStationHierarchy` | Bind nearest SG4D leaf ids |
| Cooking bridge | Registers `CivilVenueNode` Kitchen on PersonaDayManager |
| Computer bridge | Resolves `ComputerPeripheryStation` |

Editor: **Window → System Drawer → Stations** — list + **Upload level stats**.

## Kinds

`cooking` | `train` | `bus` | `computer` | `generic`

Building seeds: `train_station`, `bus_depot` (+ existing restaurant for kitchens).
