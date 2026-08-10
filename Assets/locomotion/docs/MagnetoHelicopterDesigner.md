# Magneto / Helicopter Designer

Menu: **Locomotion → Magneto / Helicopter Designer**.

## Tabs

| Tab | Role |
|-----|------|
| Overview | Craft identity + **Save all** + **Save prefab** (full craft hierarchy en totale) |
| Property Config | Authoritative `MagnetoLiftParams` — **Add / Remove / Duplicate magneto**, id popup + index slider |
| Requirements | Edit mins without mutating props; **Apply** writes minimums; efficacy banner when props are below last Apply |
| Turning / Flapping | Tail rotor, yaw, winglet/flap open-close topology |
| Cabin | Doors, gear, bathroom, kitchen, grab/standup bars |
| Instruments | Proxy surface ids |
| PixelLight | Per **view × scope** settings bags + square paint; open timed/airport editors |
| Placement | Multi grid slots — scrollable **accordion** list via `PixelLightMultiSlotCatalog` |
| Cockpit GPS | Portal id, HUD mode, TravelAgent bake cache |

## Runtime

- `HelicopterVehicleRagdoll` — magnetos, cabin, telecom, `PilotGpsHudWebtop`, `UnityRenderPortal`, `pixelLightCatalog`
- `HelicopterDirectionSolver` + `HelicopterTravelRouteMerger` on Fly/Land legs
- Landing: `RoadLot` → `ParkingLot` → `HelipadAnywhereBounds`
- Biplane path: `AirplaneVehicleRagdoll.magnetos` + shared `PixelLightMultiSlotCatalog`

## Related

- [`PixelLightMultiSlot.md`](PixelLightMultiSlot.md) — view×scope persistence, multi slots, Feature Budget
- [`PilotGpsHud.md`](PilotGpsHud.md) — portal RT overlay
- [`RoadLots.md`](RoadLots.md) — graded pads, walls, grass
- [`FeatureBudget.md`](../../SystemDrawer/docs/FeatureBudget.md) — `pixel_light` feature id
