# Airport / ATC

Bootstrap: `AirportBootstrap.Ensure()` on a `CivilInstitutionStub` (kind Airport) creates hub peers — `AirportRuntime`, `AirPortBioRhythm`, `AirportBuildingRagdoll`, `AirTrafficControlBioRhythm`, TA, `AuthWarden`, persona shift.

## Dispatch

- Kinds: [`AirportDispatchKinds`](../pathing/civil/airport/AirportDispatchKinds.cs)
- ATC FacilitateCards: takeoff / landing / holding / disaster (`TSADisasterCard` → nearest ATC)
- Airport FacilitateCards: TSA checkpoint/patrol/baggage/gate + takeoff/checklist/disaster
- Landing queue: `AirTrafficControlBioRhythm.EnqueueLanding` / `TryClaimLandingSlot`
- Destination: `SelectDestinationAtc` — default peer id for routine, nearest for disaster/potty

## Airplane designer cross-link

See [`AirplaneDesigner.md`](AirplaneDesigner.md) for plane power bus, checklist, webtop USC video, landing-queue/refuel merge on TravelAgent aircraft routes, and ATC dispatcher dialogue catalog.

## Related

- [`ATCPilotCards.cs`](../pathing/civil/airport/ATCPilotCards.cs) / [`TSAAirportCards.cs`](../pathing/civil/airport/TSAAirportCards.cs)
- [`AirplaneOpsCards.cs`](../pathing/civil/airport/AirplaneOpsCards.cs) — checklist, takeoff, disaster
- PixelLight apron mounts: Airport PixelLight Designer + `PixelLightGridMountGameObject`
