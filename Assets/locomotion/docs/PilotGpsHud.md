# Pilot GPS HUD + UnityRenderPortal

GPS map content renders to a Unity **RenderTexture**. The webtop publishes **`portalBounds2`** anchors; `UnityRenderPortal` sizes a quad/RawImage **over** the webtop panel. Frames are **not** streamed via `cctvFrame`.

## Modes

| Mode | Source |
|------|--------|
| BakedRoute | `PilotGpsRouteBakeCache` from `TravelAgent.CachedPlan` (Travel Pathing Editor → Pilot GPS bake) |
| RealtimeIsometric | Ortho nadir camera under craft |

## Placement

Designer Placement → **Place telecom + GPS webtop** on `HelicoptorGridSlotGameObject`.

## Bridge

Webtop → Unity action `portalBounds2` (see [`WEBTOP_BRIDGE.md`](../../../Scripts/telecom/docs/WEBTOP_BRIDGE.md)). JS: `collectAndPublishPortalBounds2()` for `[data-unity-portal]` / `.unity-render-portal`.
