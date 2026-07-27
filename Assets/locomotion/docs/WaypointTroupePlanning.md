# Waypoint troupe planning

In-game waypoint list + group guidance for PCs (guide line) and NPCs (full TravelAgent with feature coefficients).

## Input

[`WaypointPlannerInput`](../pathing/waypoints/WaypointPlannerInput.cs) (default **F** add under crosshair; **G** remove last; **[ ]** cycle formation).

## Route + guidance

| Type | Role |
|------|------|
| `WaypointRoute` / `WaypointMarker` | Ordered stops; per-WP `formationId`; attack marks |
| `WaypointGuidanceService` | PC `LineRenderer`; NPC `TravelAgent.previewGoalWorld` + rebuild |
| `TravelFeatureCoefficients` | 0–1 gates for Stuntman / SafetyWarden / multibody / gambit / vehicle |
| `FormationCatalog` | triangle / pineapple / divide_and_conquer → `TravelFormationAsset` |
| `WaypointMarkerRuntime` | Mesh or SDF Max stamp key + `waypoint.idle` / `waypoint.attack` |
| `WaypointSpatialProjector` | Pins into 2D/3D/4D SpatialGenerator hosts |

Formation offset uses per-waypoint tangents via [`TravelFormationPathOffset`](../travel/TravelFormationPathOffset.cs) + [`TravelFormationTangentOffset`](../travel/TravelFormationTangentOffset.cs).

## Combat facilitator

[`CombatRulesFacilitatorService`](../pathing/troupe/CombatRulesFacilitatorService.cs): teams, `TroupeParameters` (orders authority, vehicle/rideable, coeffs, call-to-arms range = dialog/comms range unless dialog overrides).  
[`CombatCommuniqueNarrativeAction`](../pathing/troupe/CombatCommuniqueNarrativeAction.cs): voice / handheld / phone / webtop + optional dialogue.

## Lemmas

- `{P:waypoint|name=A|x=1|y=2|z=3}` or `v=(1,2,3)`
- `{P:waypoint|from=A|to=B|formation=triangle}`
- `{P:formation|id=pineapple}`

See also [InventoryLoadouts.md](InventoryLoadouts.md).
