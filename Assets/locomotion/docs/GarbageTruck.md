# Garbage Truck / TrashWarden

## GarbageTruckVehicleRagdoll

- Hopper `GarbageBag` with density + compaction + SPH-style particles
- Passenger seats; compression BT; lifter arm `VehicleInstrumentPhysicsProxy`
- `LiftBin` / `ShakeBinIntoHopper` gated by TrashWarden

## TrashWarden

Scores full `TrashBinRuntime` / `HouseBioRhythm.trashFill01`, releases trucks.

Predicates:

- `IsBinEmpty`
- `ShouldShakeOut` — required by `GarbageShakeIntoTruckNode`

## Continuuuum

`/garbage-bags` — credits-style New → Create; default slot always `random_garbage_bag`.

## FeatureBudget

`garbage_truck` (`FeatureBudgetIds.GarbageTruck`).
