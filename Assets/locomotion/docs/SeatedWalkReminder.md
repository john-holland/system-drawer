# Seated Walk Reminder

Optional bio-rhythm / BP–aware get-up-and-walk branch while seated.

## Policy (`SeatedWalkReminderPolicy`)

- `timerMinSeconds` / `timerMaxSeconds` — random range; set equal for stable expectation
- Timer **starts only after** `idleDebounceSeconds` of no player input
- Any input calls `NotifyPlayerInput()` → resets debounce and timer
- Optional `requireHypertensiveLoad` gates on BP / hypertensive_load from `LifeSystemsSheet`

## Spatial filter (`SpatialDescriptionFilter`)

Default allowed keys: `outside`, `sink`, `bathroom`. Override the list so developers can route walks elsewhere. `TryPickWaypoint` selects the first matching `SpatialTaggedPoint` from octree / SG search results.

## BT (`SeatedWalkReminderNode`)

Phases: Watch → Stand (end occupancy) → Walk (`PathfindingNode`) → Re-sit (`SitOnSurfaceNode` / periphery seat) → Done.

Wire `NotifyPlayerInput` / `ClearPlayerInput` from the player input buffer while the reminder is active.
