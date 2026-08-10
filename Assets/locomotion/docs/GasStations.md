# Gas Stations

Standalone fuel venues with store shelves, TA fuel/maintenance cards, and optional rail-parallel refill.

## Runtime

| Type | Role |
|------|------|
| `GasStationRuntime` | Anchors pumps, front desk, store, kitchen, bathrooms; `CompanyRegistration`; public/private + `governmentAssigned` |
| `GasStationBioRhythm` | Hours tick; peers TA; FacilitateCards → fuel / maintenance / store |
| `FuelPumpRuntime` | Road pads + optional `railSegmentId` for train refill |
| `GasStationShelfLemmaKeys` | `top-shelf`, `bottom-shelf`, `eye-level`; alcohol ⇒ `high_price` |
| `GasStationBootstrap` | Wired from `CivilInstitutionStub` when `CivilSystemKind.GasStation` |

Ownership seed: gov → `public_fuel_auth` (or `private_fuel_co`). Amenities reuse `CivilVenueAmenities` + `StoreBase` (`convenience_store`). Building slots pack `gas_station`: front_desk, store, kitchen, bathroom, pump_island.

## TA / train fuel

- Facilitate `TAVehicleFuelCard`, `TAMaintenanceCard`, store + `GasStationRailRefuelCard`.
- Trains: `TrainVehicleRagdoll.fuel01` / `fuelPort` / `fuelPortTopologyId` (`fuel01`); `TrainEngineerCard.fuel`; rail-parallel via pump `railSegmentId` match + `GasStationRailRefuelNode`.
- Sales debit shelf commodity (`fuel`) and credit `linkedTrainCompanyId` when set.

## Shelf lemmas

`FindShelfByLemma` maps vertical band; alcohol on top-shelf bumps price.

## FeatureBudget

Id: `gas_station` (`FeatureBudgetIds.GasStation`).
