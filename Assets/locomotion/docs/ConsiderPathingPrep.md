# Consider Pathing Prep

`ConsiderPathingPrep.EnrichPlan` runs during `TravelAgent.RebuildCachedPlan`:

- Scans nearby `OpenableJointDriver` (by type name) and inserts ToolBridge open segments.
- If a `ComputerPeripheryStation` exists, appends walk-to-approach + Sit or StandOn cards.

Generic Consider remains the card source; prep only layers plan segments.
