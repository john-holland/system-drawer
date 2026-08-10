# Train Cars, Rail, Stations, Cargo Stability

## Composition

| Type | Role |
|------|------|
| `TrainCarVehicleRagdoll` | Car with limbs, containment bays, lash, coupling |
| `TrainCarAmbulationLimb` | Crane / dig / loader — OpenClose topology id + fold state |
| `TrainCarContainmentBay` | Nested vehicles or bulk commodity |
| `TrainConsistRuntime` | Ordered cars + formation group |
| `TrainCouplingRuntime` | Front/rear couple/decouple |

Close semantics: limb → **refold**; contained vehicle → **park** (`TrainCarCloseMode`).

## BT nodes

- `TrainCarUnfoldPlanNode` — unfold limb / bay ramp
- `TrainCarClosePlanNode` — RefoldLimb | ParkContainedVehicle | Both
- `TrainCarFoldFailureBranchNode` — Selector; Failure → default branch + `train_fold_failed`

## Resultants (fluent + lemmas)

```csharp
car.Resultants.Vehicles().Parked().OfKind("truck")
car.Resultants.Limbs().Unfolded().OfRole(TrainCarLimbRole.Crane)
car.Resultants.All().Stable()
```

Lemma keys: `TrainCarLemmaPropertyKeys` (+ LocalizationPropertySpecCatalog train specs). Binder: `TrainCarLemmaBinder.ApplyToken("impossible_keep_stable")`.

## Cargo lash

- `CargoLashProfile` / `CargoLashRuntime` — FixedJoints + optional `RopeSystem`
- `CargoStabilityEvaluator` + `CargoStabilityBakeAsset` — prebake + live tip risk
- Modes: `Nominal` | `SoftLash` | `ImpossibleKeepStable` (never tips; infinite pin)

## Rail travel

- `TravelLegMode.Rail` — CompositeMultiModalPathNode uses drive-style waypoint chain
- `MultiModalSegment.railSegmentId` / `consistId`
- `TravelAgent.consistId`, `railSegmentId`, `trainConsist`
- Linked snake: `TravelAgentMultibodySettings.enableLinkedSegmentSnake` → `TravelMultibodyPathAdjuster.ApplyLinkedSegmentSnakeXZ`

Feature Budget: `train_rail`, `cargo_lash`.

## Stations / silos / depots

| StationKind | Runtime | CivilSystemKind |
|-------------|---------|-----------------|
| Train | `TrainStationRuntime` | TrainStation |
| Silo | `GrainSiloStubRuntime` | GrainSilo |
| RailMaintenance | `RailMaintenanceDepotStub` | RailMaintenanceDepot |

Ops via `ITrainStationOps` / cards: couple, swap car, unload bay, limb work, silo load/unload, depot replace, lash inspect.

`StationHierarchyNode.TryBridge` registers civil venues and ensures stub components.
