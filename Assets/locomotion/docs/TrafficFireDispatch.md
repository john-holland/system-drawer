# Traffic Lights, Fire Station & Dispatch

Shared dispatch spine, generic music-synced PixelLights (also used as stop-light heads), and FireStation BuildingRagdoll package.

## Dispatch

| Type | Role |
|------|------|
| `DispatchBioRhythm` | Company/gov, pecking, cron, routes, queue/alert channels |
| `CentralDispatchHub` | Subscribe services; cross-dispatch (police↔EMS↔fire, floating hub) |
| `Dispatch*Card` | Route / pickup / load / unload / passenger / confirm |

## PixelLight (generic)

Path: `pathing/civil/lights/`

- `PixelLightRig` — timed grid patterns, `syncMode = Free | BeatQuantized | CompositionPhase`
- `PixelLightOptic` — bread-pan mesh, transparent top, metallic sides, interior light + lenticular sliders
- `PixelLightTimedDesignerWindow` — Locomotion menu; **Create default prefab**
- Music: `BeatQuantizedActionBinder` + `MusicAmbianceSchedule` (no second clock)

Consumers: firetruck bars, stage/club fixtures, **traffic signal heads**.

## Traffic / stop lights

- `TrafficLightController` — ladder RR + side-street sensor call → drives `PixelLightRig` R/Y/G heads
- `TrafficLightPoleDecorator` — mounts heads on pole
- `LaneSensorVolume` — occupancy input
- Lemmas: `StreetLightLemmaPropertyKeys` (`changed-to`, `red`, `green`, `yellow`) + `StreetLightLemmaResolver`

## Fire station

- `CivilSystemKind.FireStation` + `FireStationBuildingRagdoll`
- `FirehouseBioRhythm` : `DispatchBioRhythm` — shift cron, water reserve, station siren
- `FireWarden` — water demand totals, truck release, hospital cross-dispatch, spray-stop stub
- Fireman cards (assemble, sirens, pullout, drive, body carry, door destroy, call-in)
- `FireHydrant` + rope hose spindle on `FireTruckVehicleRagdoll` (rear steer, dual seats, PixelLight mounts)
- `FiremanHomeBinding` — off-shift HouseBio / PersonaDay homes

## Continuuuum

- `/vehicle-inventory` + `GET/PUT /api/civil/vehicle-inventory`
- Persona-day catalog includes `FireStation`
- Discovery tokens: `dispatch`, `fire-station`, `traffic-light`, `vehicle-inventory`, `pixel-light`

## Related

- [TrafficWarden.md](TrafficWarden.md) — city MST enqueue, police traffic detail, avoid-cop, flow volumes

## Stretch

Live SPH master stream, full breach IK sets, full SG4D spray alignment, rail TravelLeg package.
