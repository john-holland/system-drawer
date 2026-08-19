# Prison venue, warden, justice travel, digging

Civil kind `Prison` (`CivilSystemKind.Prison`) wakes through `CivilInstitutionStub` → `PrisonBootstrap`, same path as police.

## Bootstrap and slots

`PrisonBootstrap.Ensure()` adds:

- `PrisonBuildingRagdoll` — cells, yard, cafeteria, clinic, farm, library, chambers, parole, warden office
- `PrisonBioRhythm` + `PrisonDispatchBioRhythm` (`serviceId = "corrections"`)
- `AuthWarden` + `KeycardLock` zone tiers
- `PrisonWarden` + `PrisonRetinueClient`

`BuildingRequirementSpec.DefaultSlotsFor("prison")` includes cells, armory, guard post, yard, weights, nursery, farm, rehab gate, library, nurse/ER/OR, meeting/group/interrogation, cafeteria, parole board, warden office.

`PrisonerStatus` drives `PrisonerScheduleFactory` slots. `PrisonerSwitcherooCatalog` applies SC4-style appearance packs on the same cell footprint.

## Warden diamond

`Locomotion/Prison Warden Power Diamond` draws limits (dialog / physical / outing / parole). Selected `JusticeRehabilitationTravelAgent` step:

- **Red** — over that warden’s upper limit
- **Blue** — CivilianPaperDoll in-paint
- **Dotted white** — predicted procedural placement (also in Scene view via `TravelAgentSceneHandles`)

Over-limit scores recommend **restraint** (`WrestlingCard` on `PrisonGuardCard`); otherwise **remuneration**.

## Justice travel

`JusticeRehabilitationTravelAgent` steps: arrest → holding → trial → bail → sentencing → intake → custody → parole / rehab / outing. `PrebakeCalendar` writes `NarrativeCalendarEvent` rows (RNG/fixed duration / optional `spatiotemporalVolume`).

`JusticeSeatCard` + `TAVehicleJusticeTransportCard` assign Guard vs Prisoner seats on `BusVehicleRagdoll.seatAnchors`.

## Digging stubs

`DiggingCard` (`stopAmbulation` default true), `ConsiderDiggingCards`, `DiggingTopologyAsset` / `TopologicalDigSolver`, `DiggingBehaviorTreeNode`, `DigActionQueue`.

SPH / tip-minimum / SdfMax subtract / tunnel support+collapse / heightmap portals: `DigScoopSph`, `TunnelSupportSimulation`. Feature budget id `digging`.

## PixelLight

Designer **Add Prison Cell Layers** paints Cells, Walls, Doors, Yard, Support, TunnelStress, SurfaceMaterial. **Cell** brush stamps diggable/destructible walls and tunnel stress. **Export Prison Cell/Door/Wall Bounds4** clusters into SG4D volumes.

SG nodes: `PrisonCellDoorNode`, `PrisonWallNode`, `TunnelCollapseNode`, `TunnelSupportNode`.

## Retinue API

`POST /api/society/cities/<cityId>/prisons/<stableId>/retinue` with `action: request | sync | merge`. Unity `PrisonRetinueClient` applies `PersonaRequestBundle` (`govAgencyId`, `contractorId`) through `LifeSystemsGovGloveBias`.
