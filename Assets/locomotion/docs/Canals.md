# Canals, rivers, and dams

Canal ribbon is a road-lanes derivative: City Pixel Grid X = across channel, Y = along the centerline. **Locomotion → Canal Lanes Designer** authors depth, wall angle/material, cement inner/outer curve, girder spacing, fabric-concrete fill, and **baked** water speed/velocity bins (optional live [`WeatherPhysicsManifold`](../../../Weather/WeatherPhysicsManifold.cs) / [`River`](../../../Weather/River.cs) / [`Dam`](../../../Weather/Dam.cs)).

`CanalRibbonSpec` (`Create → Locomotion/Civil/Canal Ribbon`) + `CanalLockSpec`. Flood authoring uses `CityPixelLayerKind.Flood`.

## Ops

`CanalBioRhythm` (`DispatchBioRhythm`): hours cron, staff pecking, lock-engine commodities (food / gas / wood-for-steam), cafeteria `StoreBase`, PA `PixelLightGridMountGameObject`. `StationShiftSchedule` can replace hours with 24h / 18h banana-stand.

Lock-engine gearbox uses **Locomotion → Gearbox Designer**.

## Travel

`CannalTravelAgent` + **Locomotion → Cannal Travel Agent**. Steps: Approach, Lock, Transit, Exit. Power diamond: **red** limits, **blue** optimal, **dashed white** actual on Speed / Size / Justice / Threat. Threat halo from `ThreatWarden`. Boat/heli/car-off-ramp escape is `TravelAgentCard.preferFlee`, not a second solver.

Lock open/close: `CanalLockOpenCloseBt` in Open.Runtime (approach → fill/drain → gate → transit → close).

## Space canals

`CanalDispatcher` / `SpaceCannalDispatcher` + `SpaceCannalBioRhythm` (hours, fuel / GER / food / wood-for-steam). Gate requires a live `GerPowerBus` (`HousingBuildingRagdoll.gerBus`, next to `HousePowerBus`). Dark bus stamps `CityPixelLayerKind.PowerLinesDown` via `CityUtilityGridSeeder.SeedGerFromHouses`.

Slip-stream: `SdfSpatiotemporalVolume` + `CurvedSpacetimeSd2PathingSolver`. Landing zones and commodities on the dispatcher.

`SpaceCannalTravelAgent` + **Locomotion → Space Cannal Travel Agent**. Diamond Speed / Size / Justice / Threat. Gate dark ⇒ over-limit.

Housing: `SpaceCannalHousing` paints `CityPixelLayerKind.Apartment` and reuses `HousingBuildingRagdoll` + `MonarchicVenueRuntime` / `MonarchCard`. Shifts: `StationShiftSchedule` on canal and mill bios.
