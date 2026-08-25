# City Pixel Grid

Authoring + bake layer on top of [TrafficWarden.md](TrafficWarden.md): pixel-paint narrative hazard layers, stamp traffic/civil cards onto cells, bake MST caches, export `NarrativeCalendarEvent` volumes.

## Asset: `CityPixelGrid`

| Field | Role |
|-------|------|
| `worldOrigin`, `cellWorldSize`, `width`, `height` | World XZ placement; cell size from smallest `actorsForSizing` footprint |
| `frameGranularitySec`, `frameCount` | Narrative seconds per frame; Prev/Next in designer |
| `layers[]` | Roads / PowerLinesDown / Flood / Protest / Construction / TrafficBlock / **Cells, Walls, Doors, Yard, Support, TunnelStress, SurfaceMaterial** |
| `trafficEvents[]` | Planned city events → calendar export |
| `brushStamps[]` | Card/action placements per cell/frame |
| `catalog` | `CityPlaceableCatalog` — scalable shells (buildings, intersections, bus stops) |
| `bakedCaches[]` | MST nodes/edges + corridor marks per frame |

## Placeable chunks + best-fit

Scalable brushes (`Building`, `Intersection`, `SchoolBusStop`) materialize **one shell per connected chunk**, not per cell.

1. **`CityPlaceableChunker`** — 4-adjacent same-`typeKey` components; separator wall cells cut adjacency; incomplete separators warn and keep one chunk.
2. **Shared buildings** — different `typeKey`s may merge into one shell when a catalog candidate has `sharedBuildingCompatible` (+ `sg3dSharedBuildingCompatible` when SG3D composition is set) and tenants pass `allowedTenantTypeKeys`.
3. **`CityPlaceableBestFit`** — height-inclusive footprint score (`footprintCellsX/Z/Y` must cover need; minimize leftover volume). Stamp `candidateId` forces the shell.
4. **`FloorPlanIndexMap`** — floor/zone tenants, directory, attendant dialog ids (see [FloorPlanIndexMap.md](FloorPlanIndexMap.md)).

### Stamp fields (placeables)

| Field | Role |
|-------|------|
| `heightCells` | Story height; chunk height = max |
| `candidateId` | Forced catalog shell |
| `typeKey` | Tenant / placeable type |
| `floorIndex` / `zoneId` | Shared-shell paint into FloorPlanIndexMap |
| `floorPlanIndexMap` | Per-stamp map override |

## Designer

**Locomotion → City Pixel Grid Designer** · **Locomotion → Road Lanes Designer** embeds the same click-to-paint grid for highway sections (X = across lanes, Y = along the ribbon).

- Layer paint or **Brush Mode** (cards). **Select** brush click-drags cells (Shift add, Ctrl/Cmd toggle, Esc clear). **Undo / Redo** (Ctrl/Cmd+Z · Ctrl+Y) — a drag-paint is one step.
- **Add Prison Cell Layers** + **Cell** brush (diggable / destructible / tunnel stress)
- **Add House Street / Yard / Side Layers** (`EnsureHouseLayers`) + `DrivewayLot` / `GarageLot` brushes — see [HouseConstruction.md](HouseConstruction.md)
- **Export Prison Cell/Door/Wall Bounds4** for SG4D `PrisonCellVolume` / `DigContactCentroid`
- Prev / Next / Add Frame, granularity, **Resize Cell From Actors**
- Bake MST (this frame / all), Export Narrative Events
- Open Pixel Light / LadderLogic editors

### Brushes

| Brush | Card |
|-------|------|
| Police detail | `DispatchPoliceDetailCard` + ladder |
| One-way | `TAOneWayStreetCard` |
| Detour | `TADetourCard` |
| Stop sign | `TAStopSignCard` |
| Intersection | `TAIntersectionCard` (+ PixelLight / ladder) |
| School bus | `TASchoolBusStopCard` |
| Building | `TABuildingTypeCard` + Open Available Editors; height / candidate / floor / zone |
| Sign | `TASignCard` (Yield/Stop/SlowChildren/BlindDrive/…) |
| Select | Editor-only: click/drag cells; does not stamp |
| Building / Intersection / Placeable type separator | Cuts same-type adjacency (no spawn) |

**Locomotion → Ladder Logic Designer** edits `TrafficDetailLadderAsset` + light ladder timings.

## Runtime

- `CityPixelGridRuntime` — narrative clock → frame; hazard leases → `TrafficWarden.narrativeLeaseActive`; materialize signs + **chunked** placeables with `FloorPlanIndexMapHost`; prefer bake
- `TrafficWarden.cityGrid` + `preferCityGridBake` — use `bakedCaches` backbone when present, merge live TravelAgent demand

## Bake

Also see [RoadLanes.md](RoadLanes.md) for Highway / overpass / street-light / sidewalk / jersey brushes and stacked stamps.
