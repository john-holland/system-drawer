# Time Traveling Unity Bear: Step-by-Step Build Guide

This document describes how to build what exists in this repository into a **Time Traveling Unity Bear**: a playable bear (ragdoll) that lives in narrative time and 4D space, triggers causality, uses tools and pathfinding, with weather and a wired system-drawer. File touchpoints are **mentions** (where to look), not modification instructions. Optional improvements are listed under **Nice to haves**.

---

## Vision

- **Bear** = ragdoll character driven by the locomotion stack (muscles, good sections, behavior trees, nervous system).
- **Time traveling** = narrative time is first-class: a clock, events in (x,y,z,t), causal links, and UI so the bear’s position and narrative time determine which events fire and which actions are valid.
- **Unity** = one coherent scene: SystemDrawer wizards wire calendar, 4D orchestrator, weather, ragdoll; the bear is the “player” position key for volume triggers.

---

## Phase 1: Foundation – Ragdoll and brain

1. **Ragdoll and muscles** – Use a RagdollSystem with muscle actuation, breakable sections, and `GetCurrentState()` for card matching. Designate one ragdoll as the bear (e.g. name it “Bear” in the hierarchy; bind it as `"player"` or `"bear"` for narrative).
   - See: [RagdollSystem.cs](Assets/locomotion/RagdollSystem.cs).

2. **Good sections and card solver** – Physics cards (GoodSection) with impulse stacks, connections, and feasibility. PhysicsCardSolver and TemporalGraph for pathfinding over cards.
   - See: [GoodSection.cs](Assets/locomotion/GoodSection.cs), [PhysicsCardSolver.cs](Assets/locomotion/PhysicsCardSolver.cs), [TemporalGraph.cs](Assets/locomotion/TemporalGraph.cs).

3. **Nervous system** – Impulse router, sensory input, brain, behavior tree, goal queue. Connect behavior tree to card solver and temporal graph so the bear chooses goals and executes card sequences.
   - See: [NervousSystem.cs](Assets/locomotion/NervousSystem.cs).

4. **Tool and traversability** – ToolTraversabilityPlanner, Consider component, tool cards. Optional: throw-only good sections and IK throw goal.
   - See: [ToolTraversabilityPlanner.cs](Assets/locomotion/ToolTraversabilityPlanner.cs).

**Deliverable:** A bear ragdoll that moves, uses tools, and follows behavior-tree goals via good sections.

---

## Phase 2: Narrative time and calendar

5. **Narrative clock and scheduler** – NarrativeClock drives `currentT` (narrative seconds). NarrativeScheduler evaluates calendar events; for events with `spatiotemporalVolume`, triggers when (position, t) is inside the volume.
   - See: [NarrativeClock.cs](Assets/locomotion/narrative/Runtime/NarrativeClock.cs), [NarrativeScheduler.cs](Assets/locomotion/narrative/Runtime/NarrativeScheduler.cs).

6. **Position keys** – Scheduler has `positionKeys` (e.g. `["player"]`). Resolve “player” to the bear’s GameObject via NarrativeBindings. Per-event `positionKeys` on NarrativeCalendarEvent override when needed.
   - See: [NarrativeScheduler.cs](Assets/locomotion/narrative/Runtime/NarrativeScheduler.cs), [NarrativeCalendarAsset.cs](Assets/locomotion/narrative/Runtime/NarrativeCalendarAsset.cs), [NarrativeBindings.cs](Assets/locomotion/narrative/Runtime/NarrativeBindings.cs).

7. **Calendar and executor** – NarrativeCalendarAsset with events and NarrativeCausalLink list. NarrativeExecutor runs actions (trees, play sound, change weather) when events fire.
   - See: [NarrativeCalendarAsset.cs](Assets/locomotion/narrative/Runtime/NarrativeCalendarAsset.cs), [NarrativeExecutor.cs](Assets/locomotion/narrative/Runtime/NarrativeExecutor.cs).

**Deliverable:** Narrative time advances; calendar events fire when the bear is in the right place at the right time.

---

## Phase 3: 4D placement and causality

8. **4D volumes and grid** – Events with optional `Bounds4` spatiotemporalVolume. NarrativeVolumeGrid4D built from volumes and causal order; occupancy and causal depth per cell. NarrativeVolumeQuery.Sample4D(position, t) available at runtime when the 4D generator is active.
   - See: [NarrativeVolumeGrid4D.cs](Assets/locomotion/narrative/Runtime/NarrativeVolumeGrid4D.cs), [NarrativeContingency.cs](Assets/locomotion/narrative/Runtime/NarrativeContingency.cs) (NarrativeVolumeQuery).

9. **Causal links and depth** – Causal links (fromEventId → toEventId) define the DAG; causal depth/order used when building the 4D grid. Causal overlay in editor on NarrativeCalendarAsset.
   - See: [NarrativeCalendarAsset.cs](Assets/locomotion/narrative/Runtime/NarrativeCalendarAsset.cs).

10. **Timeline bar and causality recording** – In-game UI: one-minute modulo timeline bar (0–60s), “Mark start” / “Mark stop”. Orchestrator: autoStartWithCausality, collectCausalityEvents, causalityTriggersTripped (CausalityTriggerTrippedDto). When the bear enters a narrative volume at (position, t), it is recorded; bar can wait for first causality trigger.
    - See: [SpatialGenerator4DOrchestrator.cs](Assets/BedogaGenerator/SpatialGenerator4DOrchestrator.cs), [Spatial4DInGameUI.cs](Assets/BedogaGenerator/Spatial4DInGameUI.cs), [Spatial4DExpressionsDto.cs](Assets/BedogaGenerator/Spatial4DExpressionsDto.cs).

11. **4D mirror under orchestrator** – Under SpatialGenerator4DOrchestrator: 2D/3D generators, solvers, and 4D tree mirror (one node per placed volume, Bounds4 + payload, “Open in tree editor”). Use “Refresh 4D mirror” in the orchestrator editor to sync.
    - See: [SpatialGenerator4DOrchestrator.cs](Assets/BedogaGenerator/SpatialGenerator4DOrchestrator.cs), [Spatial4DMirrorNode.cs](Assets/BedogaGenerator/Spatial4DMirrorNode.cs), [SpatialGenerator4DOrchestratorEditor.cs](Assets/BedogaGenerator/Editor/SpatialGenerator4DOrchestratorEditor.cs).

**Deliverable:** Events live in (x,y,z,t); causality is visible and recordable; bear’s (position, t) drives triggers and recording.

---

## Phase 4: System drawer and wizards

12. **SystemDrawerService** – Single conglomerator with weak links by string key (e.g. NarrativeCalendar, WeatherSystem, Spatial4DOrchestrator, RagdollRoot, player). Systems register on Enable or via “Register with System Drawer”.
    - See: [SystemDrawerService.cs](Assets/SystemDrawer/SystemDrawerService.cs).

13. **Service wizards** – One wizard component per aspect: Calendar, Narrative Tree, Weather Event, Weather Service, Ragdoll Fitting, Spatial 4D Orchestrator, IK Training, Narrative LSTM Prompt. Each exposes slots and buttons to create examples and wire references from System Drawer.
    - See: [CalendarServiceWizard.cs](Assets/SystemDrawer/CalendarServiceWizard.cs), [RagdollServiceWizard.cs](Assets/SystemDrawer/RagdollServiceWizard.cs), [Spatial4DServiceWizard.cs](Assets/SystemDrawer/Spatial4DServiceWizard.cs), and other wizards under [SystemDrawer](Assets/SystemDrawer/).

14. **Scene wiring** – In the Time Traveling Bear scene: one SystemDrawerService; calendar, 4D orchestrator, weather, and bear ragdoll (and optional LSTM) registered. Add RagdollServiceWizard on the bear with `ragdollRoot` set and `alsoRegisterAsPlayerKey` = `"player"` so the drawer provides the bear for “player”. Add NarrativeBindings with a binding key `"player"` → Bear GameObject so the scheduler and executor resolve position keys to the bear.
    - See: [NarrativeBindings.cs](Assets/locomotion/narrative/Runtime/NarrativeBindings.cs), [RagdollServiceWizard.cs](Assets/SystemDrawer/RagdollServiceWizard.cs).

**Deliverable:** One scene with all systems discoverable and wired via System Drawer; bear is the player entity for narrative.

---

## Phase 5: Weather and world

15. **Weather stack** – WeatherSystem, Meteorology, Wind, Precipitation, Water, Cloud, WeatherPhysicsManifold and related. Service update order and data flow as in weather.md. Optional: wind forces on Rigidbodies so the bear can be affected by wind.
    - See: [WeatherSystem.cs](Assets/Weather/WeatherSystem.cs) and other scripts under [Weather](Assets/Weather/).

16. **Weather–narrative link** – NarrativeChangeWeatherAction (and weather event wizard) can create WeatherEvent with wind direction / pressure. Calendar events can change weather at (position, t) or by time only.
    - See: [NarrativeChangeWeatherAction.cs](Assets/locomotion/narrative/Runtime/NarrativeChangeWeatherAction.cs), [weather_event_wizard_window.md](.cursor/plans/weather_event_wizard_window.md).

17. **World and pathfinding** – HierarchicalPathingSolver for walk paths; NarrativePathfindingCoverage optional for 4D coverage visualization. Bear uses pathfinding and ToolTraversabilityPlanner for walk + tool bridges; planner already uses (queryPosition, queryT) to filter sections.
    - See: [HierarchicalPathingSolver](Assets/HierarchicalPathFinding/), [ToolTraversabilityPlanner.cs](Assets/locomotion/ToolTraversabilityPlanner.cs), [NarrativePathfindingCoverage.cs](Assets/BedogaGenerator/NarrativePathfindingCoverage.cs).

**Deliverable:** World has weather and terrain; bear pathfinds and uses tools; weather can be driven by narrative.

---

## Phase 6: Causality-aware bear (unifying theory)

18. **Feasibility filter** – When building plans or choosing cards, use (queryPosition, queryT) in ToolTraversabilityPlanner.FindPlan; sections are filtered by GoodSection.EnablesTraversabilityAt(position, t). Optionally, callers can filter availableSections by NarrativeVolumeQuery.Sample4D(bearPosition, currentT) (e.g. require occupancy or causal depth threshold) before calling FindPlan.
    - See: [ToolTraversabilityPlanner.cs](Assets/locomotion/ToolTraversabilityPlanner.cs), [GoodSection.cs](Assets/locomotion/GoodSection.cs) (IsTraversabilityValidAt, useValidInVolume), [UnifyingTheoryMath.md](Assets/Documentation/UnifyingTheoryMath.md).

19. **Cost or graph modulation (optional)** – Extend TemporalGraph/pathfinding so edge cost or node visibility depends on (position, t) and causal depth (e.g. narrative term in cost). Unifying theory doc outlines formulas.
    - See: [TemporalGraph.cs](Assets/locomotion/TemporalGraph.cs), [UnifyingTheoryMath.md](Assets/Documentation/UnifyingTheoryMath.md).

20. **Time-aware goals** – BehaviorTreeGoal has optional validAfterNarrativeTime, validBeforeNarrativeTime, and requireMinCausalDepth. When evaluating or enqueueing goals, gate by narrative clock and (when requireMinCausalDepth > 0) by NarrativeVolumeQuery.Sample4D(agentPosition, t).causalDepth so the bear’s high-level behavior aligns with the timeline and causality.
    - See: [BehaviorTreeGoal.cs](Assets/locomotion/BehaviorTreeGoal.cs). Integration point: the code that enqueues or selects goals (e.g. in NervousSystem or behavior tree nodes) should check these fields when NarrativeClock and NarrativeVolumeQuery are available.

**Deliverable:** Bear’s movement and tool use respect narrative causality; optional cost/heuristic for time-aware pathfinding.

---

## Phase 7: Polish and optional extensions

21. **LSTM narrative (optional)** – Narrative LSTM: prompt interpreter (natural language → events/4D), calendar summarizer, export/training scripts. Wire via NarrativePromptServiceWizard and System Drawer.
    - See: [NarrativeLSTMPromptInterpreter.cs](Assets/locomotion/narrative/Inference/NarrativeLSTMPromptInterpreter.cs), [NarrativePromptServiceWizard](Assets/SystemDrawer/NarrativePromptServiceWizard.cs).

22. **Networking (optional)** – For multiple bears or multiplayer: determinism, server time authority for narrative clock, spatial interest, card state sync. Not required for single-player time-traveling bear.
    - See: [networking_architecture.md](.cursor/plans/networking_architecture.md).

23. **Bear identity and UX** – Name the ragdoll “Bear” in the hierarchy; ensure one NarrativeClock and one NarrativeScheduler in the scene; use the timeline bar and causality list in Spatial4DInGameUI. Optional: “time travel” affordance (e.g. jump to time or rewind) beyond current play-forward narrative time.
    - See: [Spatial4DInGameUI.cs](Assets/BedogaGenerator/Spatial4DInGameUI.cs), [NarrativeClock.cs](Assets/locomotion/narrative/Runtime/NarrativeClock.cs).

---

## Nice to haves (tooling or system changes)

- **Causal order from links** – Derive `causalOrder` (depth per volume) automatically from NarrativeCalendarAsset.causalLinks (topological order of event DAG) when building the 4D grid.
- **Narrative term in pathfinding** – Add an optional narrative cost term to TemporalGraph.FindPath (e.g. delegate or overload that takes (position, t) and returns extra cost from NarrativeVolumeQuery.Sample4D) so paths prefer narrative-valid regions.
- **NarrativeBindings from drawer** – Option to resolve bindings from SystemDrawerService when a key is missing (e.g. “player” from drawer so one registration fills both drawer and narrative).
- **Heuristic for time–causality A\*** – Admissible heuristic for (state, time, position) space when narrative cost is included in pathfinding.
- **Multi-agent causality** – When multiple actors share the narrative grid, rules or tooling for one-time events (e.g. one ladder placement) to avoid conflicts.
- **Inverse use** – Use actor graph structure (required sections to reach a goal) to suggest or validate causal links in the calendar.
- **Smooth causal depth** – Interpolate causal depth (and occupancy) in the 4D grid for smoother cost or visualization.
- **Time jump / rewind** – NarrativeClock API or UI to set current time (e.g. for “time travel” gameplay) with optional event replay or reset of triggered state.

---

## Outcome

After following these steps you have: a bear (ragdoll) in a scene with narrative time, 4D-placed events, causal links, timeline/causality UI, weather, pathfinding and tools, and optional LSTM. The bear is the “player” for position keys; moving it through space and advancing narrative time makes it the time-traveling actor that triggers and is constrained by causality.
