# PixelLight Multi-Slot + View×Scope Settings

## Per view × scope settings

Helicopter (and airplane/airport catalogs) store **independent** `PixelLightViewScopeSettings` keyed by:

`view (Top/Front/Back/Left/Right/Bottom) | scope (Airframe/Magneto) | magnetoIndex`

Changing View or Scope loads that bag’s properties (grid, pattern, colors, brush, paint frame). Edits do **not** overwrite other view/scope bags. Switching views keeps each bag’s fields; use **Pull mount → this view+scope bag** only when you intentionally want the live mount to seed the current bag. Prefer a **distinct pattern asset per bag** if paint frames must differ (shared `PixelLightPatternAsset` references share paint data).

Host asset: `PixelLightMultiSlotCatalog` (`Create → Locomotion/Civil/Pixel Light Multi Slot Catalog`).

Assigned on:

- `HelicopterVehicleRagdoll.pixelLightCatalog`
- `MagnetoHelicopterConfigurationAsset.pixelLightCatalog`
- `AirplaneVehicleRagdoll.pixelLightCatalog`
- Airport Pixel Light Designer catalog field

## Multi grid slots

`PixelLightMultiSlotCatalog.gridSlots` lists `PixelLightGridSlotEntry` rows (heli `HelicoptorGridSlotGameObject` and/or `PixelLightGridMountGameObject`).

**Placement** tab (heli) and **Airport Pixel Light Designer** show a **scrollable accordion** of slots (`PixelLightGridSlotAccordionDrawer`).

## Feature Budget

| Id | Display | Notes |
|----|---------|--------|
| `pixel_light` | PixelLight / Grid Slots | Perf scopes: `PixelLight`, `PixelLightRig`, `PixelLightOptic`, `PixelLightGridMount` |

`maxRecommendedSlots` on the catalog warns when slot count exceeds the soft cap (default 16). Auto granularity can reduce aesthetic PixelLight work via Feature Budget like other civil features.

See [`FeatureBudget.md`](../../SystemDrawer/docs/FeatureBudget.md).

## Designer flow

1. Create/assign `PixelLightMultiSlotCatalog`.
2. Placement → add/sync slots; accordion edit cells/contents.
3. PixelLight tab → pick View + Scope → edit bag → **Apply this view+scope to selected mount**.
4. Quick brush row: **On / Delete / Grid Slot / Fill / Chase / Clear**. **Grid Slot** click places a `HelicoptorGridSlot` at that cell (overlay **G**). **Delete** on a **G** cell prompts *Do you want to delete this grid slot?* (Yes/No) and removes the scene slot + catalog entry.
5. **Frame scrubber** — IntSlider + Time (ms) slider + tick bar to hand-scrub pattern frames; optional live preview on the selected mount (pauses rig playback).
6. Save all on Overview persists catalog + craft config.
