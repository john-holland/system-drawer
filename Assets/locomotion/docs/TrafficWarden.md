# Traffic Warden, Police Detail, Avoid-Cop

City-scoped traffic coordinator: MST over cached TravelAgent A* corridors, car enqueue, street-light policy, police traffic detail via dispatch, soft avoid-cop pathing, and Bounds4 volume BT grafts.

## Types

| Type | Role |
|------|------|
| `TrafficWarden` | City hub; corridor MST, enqueue, lights, avoid sources, `cityGrid` reserved for Phase 2 |
| `TrafficCorridorGraph` | Snap waypoints → undirected demand graph |
| `TrafficMstBuilder` | Kruskal MST backbone |
| `TrafficCarEnqueue` | Release cars along backbone when lights proceed |
| `TrafficWardenStateMachine` | `NormalLadder` / `CongestedHold` / `EmergencyPreempt` / `PoliceDetailActive` / `NarrativeLease` |
| `TrafficDispatchBioRhythm` | Hub `serviceId = traffic_warden` |
| `TrafficDetailLadderAsset` | Scriptable ladder steps for traffic detail |
| `DispatchPoliceDetailCard` | Ladder-driven detail + expands to CopLights / PullOver / etc. |
| `TrafficFlowVolumeTrigger` | Bounds4 / trigger → graft `GoalType.TravelAgent` flow goals |
| `TrafficWardenBootstrap` | Ensures hub + warden + traffic bio |

## Flow

1. Sample `TravelAgentRegistry` CachedPlans → corridor graph → MST.
2. Enqueue cars (parking seed / explicit) → goals on backbone endpoints; gate on nearby `TrafficLightController.MainProceed` / `SideProceed`.
3. Congestion / narrative lease → hold AllRed; police detail → preempt green + `RequestCrossDispatch(traffic_warden → police, kind=traffic_detail)`.
4. `PoliceDispatchBioRhythm.FacilitateCards` emits ladder cards for `traffic_detail`.
5. Cruiser `DispatchToDetail` registers avoid source; TravelAgents soft-cost A* near cops unless `ignoreTrafficAvoidance`.

## Avoid-cop

- `TravelAgent.ignoreTrafficAvoidance`, `avoidActors`, `avoidRadius`, `avoidCostMultiplier`
- `PlannerHints.avoidPoints` + `HierarchicalPathingSolver.SetSoftAvoid`
- OctreeLeaves pathing multiplies leaf-center edge cost near avoid points

## City Pixel Grid

See [CityPixelGrid.md](CityPixelGrid.md): assign `TrafficWarden.cityGrid` (+ optional `CityPixelGridRuntime`). When `preferCityGridBake` and `bakedCaches` exist for the active frame, enqueue uses the baked MST backbone.

## Related

- [TrafficFireDispatch.md](TrafficFireDispatch.md) — lights, fire, dispatch spine
- [PoliceVehicleRepair.md](PoliceVehicleRepair.md) — police station / cruiser
