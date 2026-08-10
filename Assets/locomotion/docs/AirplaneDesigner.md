# Airplane Designer

Menu: **Locomotion → Airplane Designer**.

## Tabs

| Tab | Role |
|-----|------|
| Overview | Identity, fuel, ATC destination, landing-queue/refuel flags, **Save all** / Pull / Create asset |
| Construction | Wing/tail params, tip cache recompute, nose + landing-gear topology ids |
| Aero | Fuselage ellipsoid + conical nozzle, jet list, affine weather bridge apply |
| Power | Batteries, system draw table + shed priorities, seat outlet/seatback counts, chorus↔PA music |
| PixelLight | Mount list + Ensure; opens Airport PixelLight Designer (6-view closest-first) |
| Checklist | `TSAChecklistCard` template edit / regenerate defaults |
| ATC Dialogue | `AtcDispatcherDialogueCatalog` kind → dialogue-set map |

## Runtime

- `AirplaneConfigurationAsset` — ApplyTo / CaptureFrom ↔ `AirplaneVehicleRagdoll`
- `AirplaneWingSurfaceParams` — tip cache + `FlyingCardConfig.wingAspectRatio`
- `AirplanePowerBus` + `AirplaneBioRhythm` — draw sum, battery drain, progressive shed/restore
- `AirplaneCabinMusicSystem` — Chorus / PA / SeatAux / PilotTelecom
- `AirplaneWeatherAeroBridge` — ellipsoid affine → lift/drag/thrust slots
- `WebtopUscVideoPlayer` — open → play → close topology sequence
- Cards: `TSAChecklistCard`, `TSATakeoffCard`, `TSALandingCard` gear override, `TSADisasterCard` nearest ATC
- Route: `AircraftTravelRouteMerger` inserts `AircraftLandingQueueNode` + `AircraftRefuelNode` on Fly/Land/Park
- ATC: `EnqueueLanding` / `TryClaimLandingSlot`, `SelectDestinationAtc` default vs nearest (disaster/potty)

## Related

- [`MagnetoHelicopterDesigner.md`](MagnetoHelicopterDesigner.md) — shared PixelLight mounts / biplane magnetos
- [`PixelLightMultiSlot.md`](PixelLightMultiSlot.md) — view×scope catalog
- [`AirportATC.md`](AirportATC.md) — ATC / TSA bootstrap
- Feature Budget: reuse `pixel_light` for nav mounts
