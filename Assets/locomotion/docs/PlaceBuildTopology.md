# Place / Build Topology

`PlaceBuildTopologyAsset` + `PlaceBuildTopologyPlanNode`:

Find grabbable (chair/box/book synonyms) → place → occupy Sit/StandOn. Optional turn-in-chair and place-close modes.

Builder: `PlaceBuildTopologyBtBuilder.FindGrabbable` / `BuildStepIds`.

Used after jump-press exhausts attempts, or when TravelAgent needs a seat/stand bridge.
