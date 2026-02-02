# Scheduler position keys: single key to set (with per-event override)

## Current behavior

- **NarrativeScheduler**: `public string playerPositionKey = "player"`. For events with `spatiotemporalVolume`, the scheduler resolves that one key and checks if that GameObject's position is inside the volume. Only that one entity is considered.
- **Actions** (RunBehaviorTreeAction, NarrativeSitAction, etc.) each have their own keys (actorKey, targetKey) and resolve via `ctx.TryResolveGameObject(key, ...)` at execution time. The scheduler does **not** currently pull a key from the event or from any action for the region check.

## Desired behavior

1. **Set of keys on the scheduler** (default includes "player"): trigger when **any** of those keys resolves to a GameObject inside the event's spatiotemporal volume.
2. **Pull the key from the event**: allow the **event** to optionally specify which keys to use for its own region check. If the event defines position keys, use those for that event; otherwise use the scheduler's default set. So we "pull the key from the action" (the event being scheduled) when the event specifies it.

## Implementation

### 1. NarrativeScheduler

- Replace `public string playerPositionKey = "player"` with:
  - `public List<string> positionKeys = new List<string> { "player" };`
- In the spatiotemporal branch: resolve "which keys to use" as:
  - **If** the event has a non-null, non-empty `positionKeys` (see below), use `event.positionKeys`.
  - **Else** use `scheduler.positionKeys`.
- Then: for the chosen list, loop over keys; if any key resolves to a GameObject and that GameObject's position is inside the volume at tNow, add the event to scratch (and break). Same behavior when the chosen list is null/empty: no one triggers that volume event.

### 2. NarrativeCalendarEvent (per-event override)

- Add an optional field on [NarrativeCalendarAsset.cs](Assets/locomotion/narrative/Runtime/NarrativeCalendarAsset.cs) (on `NarrativeCalendarEvent`):
  - `[Tooltip("When set and non-empty, these keys are used for spatiotemporal region check instead of the scheduler's positionKeys. Empty or null = use scheduler default.")]
   public List<string> positionKeys;`
- Serialization: `List<string>` is serializable; leave null or empty by default so existing events keep using the scheduler's default.
- When the scheduler evaluates a volume-based event, use `e.positionKeys != null && e.positionKeys.Count > 0 ? e.positionKeys : scheduler.positionKeys` as the list to try.

### 3. Summary

- **Scheduler**: `positionKeys` (List&lt;string&gt;, default `{ "player" }`); region check = "any of (event.positionKeys ?? scheduler.positionKeys) resolves and is inside volume".
- **Event**: optional `positionKeys`; when set and non-empty, that event's region check uses the event's keys instead of the scheduler's. So we pull the key(s) from the event (the "action" being scheduled) when the event defines them.
- **NarrativeExecutor / actions**: no change. Actions continue to use their own single keys (actorKey, targetKey) for resolution at execution time; this change only affects **who can trigger** a volume-based event (scheduler + optional per-event keys).

## Optional: InsideNarrativeVolumeCondition

- [NarrativeContingency.cs](Assets/locomotion/narrative/Runtime/NarrativeContingency.cs) has `InsideNarrativeVolumeCondition.positionKey = "player"` (single key per condition). Left as-is in this plan; could later add a "positionKeys" set and "any in volume" semantics if desired.
