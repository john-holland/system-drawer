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
