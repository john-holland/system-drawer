# Vehicle Repair Center & Police Station

## VehicleRepairCenter (`CivilSystemKind.CarRepair`)

| Type | Role |
|------|------|
| `VehicleRepairCenterBioRhythm` | Open cron, bay occupancy, commodity demand |
| `VehicleRepairCenterRuntime` | Amenities, company pecking, store/retail, kitchen, bays |
| `TAMaintenanceCard` | Bay service / inspect / repair → `VehicleRagdoll.integrity01` |
| `VehicleRepairCenterBootstrap` | Auto on `CivilInstitutionStub` CarRepair wake |

## PoliceStation

| Type | Role |
|------|------|
| `PoliceStationBuildingRagdoll` | Layout refs (hall, desks, meeting, offices, interrogation, holding, repair bay) |
| `PoliceStationBioRhythm` | Holding/desk/alert facility channels |
| `PoliceDispatchBioRhythm` | `DispatchBioRhythm` `serviceId=police`; CopCards; ViolenceTelecomHint ingest |
| Cards | `PoliceCard`, `CopCard`, `CopDetailCard`, `CopPullOverCard`, `CopLightsCard`, `CopRequestWeaponCard`, `PoliceInterrogateCard`, `PoliceJailCivilianCard`, `TrafficJusticeCard`, `DispatchPoliceDetailCard` (traffic ladder) |
| Traffic detail | `TrafficDetailLadderAsset` + `PoliceDispatchBioRhythm.trafficDetailLadder`; cruiser `DispatchToDetail` → see [TrafficWarden.md](TrafficWarden.md) |
| `PoliceCuffItem` / `SpikeTrapItem` | Inventory cuffs (keyId) + deployable spike hazard |
| `PoliceCarVehicleRagdoll` | PixelLights, weapons chest telecom policy, aim/takedown stubs |
| Repair bay | Reuses `TAMaintenanceCard` |

### Weapons chest

- `requiresTelecomForWeapons=true` → `TryOpenWeaponsChest` mints code via dispatch; `ConfirmWeaponCode` unlocks
- `false` or chest disabled → unlocked fulfill

### Continuuuum inventory size

- `VehicleRagdoll.totalInteriorSize` (+ computed sum of section capacities)
- Inspector **Update size for Continuuuum** PUTs `/api/civil/vehicle-inventory/<id>`
- SQLite table `vehicle_inventory` write-through (plus in-memory cache)

## Stretch

Full interrogate constraints, live jail wrestling trees, weapon proxy polish, vehicle takedown crash physics.
