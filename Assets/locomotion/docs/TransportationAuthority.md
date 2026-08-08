# Transportation Authority, Bus Station, Repair Center

## Hub bridge

| Service id | Type |
|------------|------|
| `transportation_authority` | `TransportationAuthorityBioRhythm` |
| `mission_control` | `MissionControlBioRhythm` |
| `air_traffic_control` | `AirTrafficControlBioRhythm` |
| `traffic_warden` | existing `TrafficDispatchBioRhythm` |

Cross-dispatch via `CentralDispatchHub.RequestCrossDispatch`. Mission Control / ATC enqueue TA kinds (`ta_reroute`, `ta_halt`, `ta_recall`, `ta_hours`, …). TA `FacilitateCards` maps kinds to `TATransitCards`.

## Bus station

- `CivilSystemKind.BusDepot` / `TransitHub` → `BusStationBootstrap`
- `BusStationRuntime` + `BusStationBioRhythm` + optional nested `VehicleRepairCenterRuntime` + public/private cafeteria
- SG pack: `BusStationSgPackSettings` / `BusStationSgGenerator` (lemma fragment `pack=3d,placement=uniform,pad=0.35`)
- Building slots: platform, waiting, bay, telecom, cafeteria, bathroom, parking, trash

## Commuter cards

`CommuterWaitCard` (`check_time`, `impatiently`), `CommuterFindSeatCard` (`scans`, `find` + seat-ahead / grab-bar IK + `SeatedPelvisPoseCache`), board / stow / stop-request / complaint / exit.

## Bus vehicle

`BusVehicleRagdoll`: seating, talk rules, bathroom, baggage (`TSAGroundCrewCard`), fuel, repair location, telecom/webtop, stop buttons, cabin music (DAC host), sound design.

## Ownership

`CompanyRegistration.parentCompanyId`: `government` → `public_transit_auth` | `private_transit_auth` → kitchen / fuel / repair (may be conglomerate-owned via repair center fields).

## Schedules (SQL + website)

Tables: `transit_authority_vehicle_schedule`, `transit_building_schedule`  
UI: `/transit` — API `/api/transit/vehicle-schedules`, `/api/transit/building-schedules`, `/api/transit/routes`
