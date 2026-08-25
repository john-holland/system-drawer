# Vehicle Instrument Physics Proxy

Retarget local vehicle instruments onto remote vehicle control surfaces (e.g. ejector-seat jets → fire-truck ladder thrust).

## Types

| Type | Role |
|------|------|
| `IVehicleControlSurface` | Apply normalized impulse |
| `VehicleManifoldControlSurface` | Rigidbody force/torque |
| `VehicleInstrumentBinding` | localSurfaceId → remote vehicle + surface |
| `VehicleInstrumentPhysicsProxy` | Resolve + `RouteCard` |
| `ProxiedDrivingPhysicsCardSolver` | Filters cards that lack proxy bindings |

## Flow

1. Author `VehicleInstrumentMap` on the seat (local ids `jet.pitch`, `jet.yaw`).
2. Bind those ids to a remote `VehicleActor` surface.
3. Drive cards that pass the source map are routed with `proxy.RouteCard(card, dt)`.

`VehicleInteriorManifold.TryGetPathToInstrument` resolves instrument transforms by instance id or proxy binding origins (straight segment v1).

## Video steering BT

`TravelLegDriveNode` calls `ProxiedDrivingPhysicsCardSolver.TryRouteFirstApplicable` when a proxy is present, otherwise `RouteCard` on steer/throttle/brake stubs from `PhysicalPathingGoodSectionStubs`. Vehicle video takes (Intel YOLO26 + road-center spline) bake this chain via `VehicleSteeringBtBaker` after `SeedVehicleVelocityNode`. See [IkWebcamVideoInterpretation.md](IkWebcamVideoInterpretation.md).

## Cabin camera vs chase-cam

Chase-cam Vehicle takes treat YOLO26 bboxes as the ego subject on the road spline. Cabin (`cabinCamera`) takes never do that: YOLO is **traffic through glass only**. Ego chassis velocity comes from windshield **polar VO** (`CabinPolarVelocity` → `SeedVehicleVelocityNode` / `DimensionalLemmaVelocityBridge`). Occupant pose maps onto the same instrument proxy:

- Hands → `CreateDriveSteerStub` (`vehicle_steering`)
- **Infer shoulder shifts**: residual lean vs polar accel → throttle / brake stubs
- Feet (accelerator / brake / clutch) override inferred pedals

`CabinPoseInstrumentSolver.TryRoute` still uses `VehicleInstrumentPhysicsProxy.RouteCard`. No new input stack.

## Combat / Love / Wrestling cards

`CardInstrumentProxyOptions` on Combat, Love, and Wrestling cards can route fire/commit through `VehicleInstrumentPhysicsProxy.TryFirePulse` (map slot + ~22 N / 5 lbf safety lock). See [CombatTopology.md](CombatTopology.md).
