# Sewer Graph / Drainage

`SewerGraph` connects buildings and street drains to sanitation poop-quifers.

- Inflow: water + gas taps (capacity via `MunicipalWaterService`). Sump discharge uses `SewerFlowKind.WaterIn` (`SewerGraph.TransmitWaterIn`) so it never returns to the potable `WaterGraph`.
- Shared outflow pipe: poo + soapy water (`SewerFlowKind.PooOut` / `SoapyWaterOut`)
- City seed: `CityPixelGrid.SeedWaterAndSewerFromGrid` / street-name pecking — see [HouseUtility.md](HouseUtility.md)
- `SewerBuildingTap` on houses/buildings links `FixturePlumbingNode` flush/use
- `RoadsideDrainRuntime` for street runoff
- Empty lots: `DryWellRuntime` / `BioswaleRuntime` + `HeightMapInteriorShaderBuffer`

`MunicipalWaterService.PublishToSewerGraph` publishes sewer capacity into graph tick.

## FeatureBudget

`sewer_graph` (`FeatureBudgetIds.SewerGraph`).
