# Sanitation Facilities

Factory-subclass waste plant: sorting, poop-quifer fertilizer flow, recycling transfer, road care crews.

## Runtime

| Type | Role |
|------|------|
| `FactoryRuntime` / `FactoryBioRhythm` | Gate + line base |
| `SanitationFacilityRuntime` | Loading, pallets, sorting stations, poop-quifer, recycling, trucks, crew depot |
| `SanitationFacilityBioRhythm` | Peers TA; pickup / sort / maintenance / road-work cards |
| `SanitationPoopQuifer` | Inflow→digest→outflow → fertilizer commodity |
| `SanitationSortingStation` | Conveyor BT; actor/machine bag cut; downflow stages |
| `SanitationRecyclingTransfer` | TransferBulk-style stock |
| `RoadCareCrewRuntime` | Retinue RTS CallToArms; pickup waypoints → house trash → facility trash |

Ownership: `government` → `public_sanitation_auth` (or private). Continuuuum `/garbage-bags` facility section stores company id + IPv6 city prefix.

Building slots pack `sanitation_facility`: gate, line, loading, sorting, poop_quifer, recycle_bay, crew_depot, bathroom, trash.

## FeatureBudget

`sanitation_facility` (`FeatureBudgetIds.SanitationFacility`).
