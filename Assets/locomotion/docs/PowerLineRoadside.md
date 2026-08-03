# Power Line Roadside Decorator

`PowerLineRoadsideDecorator` samples a road spline (duck-typed `RoadSpline3D.BuildSamples` / `GetSampleAtDistance`), places `UtilityPoleAssembly` on the shoulder with occupancy sublimation, and spans black cables via `PowerLineSpan`.

## Defaults

- Ground-merge junctions when sublimation fails (pretend underground, rise on next pole)
- Warnings logged when optimal connection fails
- Tension lemmas: `unbreakable`, `faulty-standoff` (`PowerLineTensionLemma`)

## Pole assembly

Wood shaft (dev texture or hair-flux tint fallback), crossarm, insulators, transformer, fuse, copper ground, eye plate, guy anchor + marker sheath, secondary rack, climb steps (trigger colliders for IK/pathing).
