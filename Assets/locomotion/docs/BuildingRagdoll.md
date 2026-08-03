# BuildingRagdoll

Structural integrity for civil buildings: health channels, material impulse memory, damaged-object queue → CivicCard repair.

## Naming

| Type | Role |
|------|------|
| **BuildingRagdoll** | Shipping spine — integrity, impulse memory, bio, damage queue |
| **BuildingBeast** | Stub only — reserved for later fiction; does not tick sim |

## Pipeline

1. Destructible impact → `ImpulseMaterialMemory.NotifyImpulse`
2. Wood/metal tau → bend/dent memory → `BuildingHealthState`
3. Damage above threshold → `DamagedObjectQueue`
4. `ConsiderCivicCards` → `CivicCard` repair / inspect / secure
5. `CivicRepairTopology` open/close repair zones + retinue

## Continuuuum

| Endpoint | Role |
|----------|------|
| `GET/POST /api/civil/damaged-objects` | Queue |
| `POST /api/civil/damaged-objects/<id>/resolve` | Complete repair |
| `GET/PUT /api/civil/building-health/<id>` | Health snapshot |
| `POST /api/civil/store/prebake-shelves` | Shelf fill fallback |
| `GET /api/civil/meta` | Discovery (`buildingBeast: stub_only`) |

## Editor

`Window → System Drawer → Civil → Building Requirements`
