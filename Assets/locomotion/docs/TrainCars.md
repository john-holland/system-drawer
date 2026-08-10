# Train Vehicles, Rail Track, Stations, Dispatch

## Composition (replace model)

| Type | Role |
|------|------|
| `TrainVehicleRagdoll` | Authoritative craft — nested `cars`, limbs, bays, lash, coupling, cabin, planar aisles |
| `TrainCarAmbulationLimb` | Crane / dig / loader / plow — OpenClose topology + fold state |
| `TrainCarContainmentBay` | Nested vehicles or bulk commodity |
| `TrainCouplingRuntime` | Front/rear couple → rebuilds head car list |
| `RailTrackStructure` | Mesh/SDF track part catalog + `railSegmentId` |
| `PlanarSplinePathLocomotion` | Aisle / door bridge / porch walk ribbons |

Close semantics: limb → **refold**; contained vehicle → **park** (`TrainCarCloseMode`).

## Dispatch / station

- `TrainStationBioRhythm` — public/private parking, checkpoints, TSA attendant + TA cards
- `TrainDispatchMissionControlBioRhythm` — engineer/dispatch start/stop/speed/plow/justice/follow/turnstile/yard
- Cards in `TrainDispatchCards.cs` compose `TAVehicle*Card` where applicable

## Travel

- `TravelLegMode.Rail` → `RailTrackFollowPlanNode` samples track then waypoints; snake via `CopySnakeWorldPositions`
- Seat tickets: Continuuuum `/train-seats` → `TrainSeatTicketConfig.ApplyTo`

## Holds

Shared `VehicleGrabHold` (cylinder) + `VehicleStrapHold` (rope) on train and bus.

Feature Budget: `train_rail`, `rail_track`, `train_dispatch`, `planar_spline_path`, `cargo_lash`.
