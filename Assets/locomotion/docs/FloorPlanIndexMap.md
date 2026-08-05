# FloorPlanIndexMap

Floor/zone index for city placeable buildings (especially shared skyscraper shells). Drives SG3D zone prompts, building directory maps, and attendant dialog tree ids.

## Asset

| Field | Role |
|-------|------|
| `mapId` / `buildingStableId` | Stable identity for UI + narrative |
| `floors[]` | Per-floor label, zone cells, directory entries, `attendantDialogTreeId` |
| `sharedSpaces[]` | Corridors / elevators (`tenantTypeKey` often `shared`) |

## Runtime host

`FloorPlanIndexMapHost` on the spawned shell:

- `TryGetAttendantDialog(floorIndex, zoneId)` → narrative / lemma dialogue-set id
- `GetDirectory()` → flat list for building-map UI
- `SendSg3dZonePrompts()` → `OnCityPixelFloorPlanReady` / `OnCityPixelSg3dFloorZone` messages

## Resolution order

1. Stamp / chunk `floorPlanIndexMap` override  
2. Catalog candidate `defaultFloorPlanIndexMap`  
3. Auto-build from chunk stamps (`floorIndex`, `zoneId`, `typeKey`) via `FloorPlanIndexMap.BuildFromChunk`

See also [CityPixelGrid.md](CityPixelGrid.md) (shared buildings + placeable catalog).
