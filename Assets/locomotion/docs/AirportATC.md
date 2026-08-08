# Airport + ATC / TSA + Road Repair

## Hub bridge

| Service id | Type |
|------------|------|
| `airport` | `AirPortBioRhythm` |
| `air_traffic_control` | `AirTrafficControlBioRhythm` (expanded FacilitateCards) |
| `transportation_authority` | `TransportationAuthorityBioRhythm` (shared transit spine) |

Cross-dispatch via `CentralDispatchHub`. Airport is symbiotic with ATC + TA; do not fork a second ATC.

## Airport composition

- `CivilSystemKind.Airport` → `AirportBootstrap`
- `AirportRuntime` + `AirPortBioRhythm` + `AirportBuildingRagdoll` + `AuthWarden` + `PersonaShiftManager`
- Fleet: `AirplaneVehicleRagdoll`, `AirportExtensionGate` (jetway), optional ground vehicles
- Galley kitchens parent to airport kitchen company (`LinkGalleyToAirportKitchen`)
- Building slots: runway, taxiway, terminal, security, gate, apron, hangar, cafeteria, spa, bathroom, telecom, parking, trash
- SG pack: `AirportRunwaySgPackSettings` / `AirportRunwaySgGenerator` (lemma fragment `scale=large,strips=14,diagonal=45`)

## AuthWarden

Event bus (not a Keycard rewrite): `OnAuthGranted` / `OnAuthDenied` / `OnAuthRevoked` for meeting rooms, offices, TSA checkpoints, gate desks. Zones carry `AuthAccessTier`.

## Cards

- ATC / Pilot: `ATCPilotCards` — takeoff, all-clear, report, holding, landing, cruise, ferry, park, gate, maneuvers; compose `VehicleActorCard` + `WaypointCard`
- TSA / airport: `TSAAirportCards` — checkpoint, check-in, announcements, attendant service, seat tray, patrol (thresholds from `terrorLevel01`), baggage, gate desk, bridge
- Dispatch kinds: `AirportDispatchKinds`

## Persona shifts

`PersonaShiftManager` auto-installs from `PersonaDayManager` for Airport / BusDepot venues. Open/close cron wakes matching retinue personas.

## Schedules (SQL + website)

Tables: `airport_airplane_schedule`, `airport_staff_hours`  
UI: `/airplanes`, `/staff-hours`  
API: `/api/airport/airplane-schedules`, `/api/airport/staff-hours`

## PixelLight + road repair

- Editor: **Locomotion → Airport Pixel Light Designer** — apron/runway/taxiway/terminal Custom layers, lane disable + detour/signage stamps
- `RoadsideDecorStamp` — GameObject or MeshRenderer decal
- `RoadDeformationRepairWindow` — inclusive `startDate` / `endDate` (+ optional crew cron); past end clears damage features and may stamp `RoadRepairDecal`
- `RoadRepairDecal` — paint-transfer / SDF Max patch memory after geometry reset

## Lemmas

`AirportLemmaPropertyKeys`: boarding-party-number-N, runway describe/align, baggage load/unload, cage/animal peer.
