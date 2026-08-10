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

`cooking` | `train` | `bus` | `computer` | `silo` | `railmaintenance` | `generic`

Building seeds: `train_station`, `bus_depot`, `grain_silo`, `rail_maintenance_depot` (+ existing restaurant for kitchens).

### Train / silo / depot bridges

| Kind | Runtime | CivilSystemKind | BT ops |
|------|---------|-----------------|--------|
| Train | `TrainStationRuntime` | TrainStation | couple, swap car, unload bay, limb work |
| Silo | `GrainSiloStubRuntime` | GrainSilo | load/unload bulk ↔ bay |
| RailMaintenance | `RailMaintenanceDepotStub` | RailMaintenanceDepot | pull/replace car, lash inspect |

See [`TrainCars.md`](TrainCars.md) for consist, fold, lash, and `ITrainStationOps` cards.
