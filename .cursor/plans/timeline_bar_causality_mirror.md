# Timeline Bar, Causality, 4D Mirror, and System-Drawer Service

Plan: one-minute modulo timeline bar, causality recording, 4D tree mirror under orchestrator, plus a system-drawer service conglomerator with weak links and per-aspect service wizards; Assets cleanup with a misc directory.

---

## Part A: Timeline bar, recording, causality, 4D mirror (existing scope)

### A.1 Timeline bar: one minute, modulo refill

- **File:** `Assets/BedogaGenerator/Spatial4DInGameUI.cs`
- Bar represents **one minute** (0–60 narrative seconds). Value = `currentT % 60`; refills every 60s.
- Slider min/max 0–60; drive display with `currentT % 60`; label e.g. "Minute (0–60s)".
- Full timeline (date/time, Mark start/stop) still uses `currentT` in narrative seconds.

### A.2 Don't start until Mark start, or start with Causality

- **Orchestrator:** Add `autoStartWithCausality` (bool). When true, "recording" starts when `IsInsideNarrativeVolume(playerPosition, narrativeT)` becomes true.
- **In-game UI:** "Recording started" = set by "Mark start" or by first causality trigger when `autoStartWithCausality`. Bar does not advance until then (or stays at 0).

### A.3 Causality event collection

- **Orchestrator:** Add `collectCausalityEvents` (bool) and `causalityTriggersTripped` (list).
- **DTO:** e.g. `CausalityTriggerTrippedDto` with treeNodeId, sequenceId, gameTime, spatialNodeId/bounds4, position.
- Record when (position, t) enters a narrative volume; append to list. Logic in orchestrator or dedicated collector.

### A.4 Structure under Orchestrator (2D/3D, solvers, 4D mirror)

- **Under SpatialGenerator4DOrchestrator transform:** Enforce at least one 2D/3D generator, required solvers (Quad/Oct), and a **4D tree mirror** (readonly hierarchy mirroring `GetPlacedVolumes()`).
- **Mirror:** One node per placed volume; component with Bounds4, payload, copyable data, and "Open in tree editor" button where payload links to NarrativeTreeAsset. Sync on 4D change or "Refresh 4D mirror" button.

---

## Part B: System-drawer service conglomerator (weak links)

### B.1 Purpose

A single **system-drawer** service acts as a conglomerator that holds **weak links** to each library asset/system (narrative calendar, weather system, spatial 4D orchestrator, ragdoll systems, audio, etc.). Other code can query this service to discover available systems without requiring the whole package to be present—references are optional and resolved at runtime or in the editor.

### B.2 Design

- **Component or ScriptableObject:** e.g. `SystemDrawerService` or `ServiceConglomerator` that lives in a common/misc location.
- **Weak links:** Store references as `Object` (UnityEngine.Object), or by string key (e.g. "NarrativeCalendar", "WeatherSystem", "Spatial4DOrchestrator") with optional `Object` or scene/hierarchy lookup so that:
  - If a package/assembly is not included, the reference is simply null and the rest of the project still runs.
  - If included, the service can return the registered calendar, weather, 4D orchestrator, etc.
- **Registration:** Systems (or their wizards) register themselves with the conglomerator when present (e.g. on Enable or via editor menu "Register with System Drawer"). No hard dependency from the conglomerator to each package—use FindObjectOfType / asset DB / optional assembly reflection only when that assembly is loaded.
- **Location:** Place the conglomerator script and default asset under the new **misc** directory (see Part D) so it is a single, stable entry point.

### B.3 Usage

- Wizards and scene setup can ask the conglomerator "give me the NarrativeCalendar" or "give me the Spatial4DOrchestrator" and get null or the instance; no compile-time dependency on Locomotion.Narrative or BedogaGenerator from the conglomerator core if using string keys and runtime lookup.

---

## Part C: Service wizard per library aspect

### C.1 Goal

For each **library aspect** we have built (narrative calendar, narrative tree, weather event, weather service, ragdoll fitting, spatial 4D/orchestrator, IK training, audio/cache, etc.), provide a **service wizard** that:

- Can be **added standalone to GameObjects** (a component you add in the inspector).
- Exposes **buttons and slots** for auto-generation of working example objects and scene integrations.
- Reuses the existing editor windows and logic where they already exist; the wizard component is a thin inspector UI that opens the right window or runs the same "create example" / "integrate scene" logic.

### C.2 Library aspects and existing wizards

| Aspect | Existing entry point | Move / unify to |
|--------|----------------------|------------------|
| Narrative Calendar | `NarrativeCalendarWizardWindow` (Window menu) | Common asset: Calendar service wizard component + shared window |
| Narrative Tree | `NarrativeTreeEditorWindow`, `NarrativeTreeAssetEditor` | Tree service wizard component |
| Weather Event (narrative) | `WeatherEventWizardWindow` | Weather event wizard component |
| Weather Service (full) | `WeatherServiceWizard` (Weather package) | Weather service wizard component (in common) |
| Ragdoll Fitting | `RagdollFittingWizardWindow` | Ragdoll service wizard component |
| Spatial 4D / Orchestrator | `SpatialGenerator4DOrchestratorEditor` (Add 3D/4D, In-Game UI) | Spatial 4D service wizard component |
| IK Animation Training | `PhysicsIKTrainingWindow` | IK service wizard component |
| Narrative Calendar Timeline | `NarrativeCalendarTimelineWindow` | Can link from calendar wizard |
| Audio / Sound Cache | `SoundCacheGeneratorWindow` | Audio service wizard component |
| BedogaGenerator Octree | `OctreeInfoWindow` | Optional link from spatial wizard |

### C.3 Implementation approach

- **Common assets directory:** Create a shared location (e.g. under `Assets/Misc/ServiceWizards/` or `Assets/Common/ServiceWizards/`) for:
  - One **MonoBehaviour (or ScriptableObject) per aspect** that acts as the "service wizard" for that aspect: e.g. `CalendarServiceWizard`, `WeatherServiceWizardComponent`, `Spatial4DServiceWizard`, `RagdollServiceWizard`. Each can be added to any GameObject.
  - In the inspector, each shows:
    - Slots (object fields) for the main asset/component it configures (e.g. calendar, orchestrator, weather system).
    - Buttons: "Open Calendar Wizard", "Create example calendar", "Create scene integration", etc., which call into the existing window or creation logic.
- **Move existing wizards into common assets:** Relocate or duplicate the editor window scripts and their "create example" / "integrate" logic so they live under the common/misc area (or keep them in their packages but have the common wizard component reference them via the conglomerator or by menu). Prefer **moving** editor windows and shared helpers into a common Editor folder (e.g. `Assets/Misc/Editor/ServiceWizards/`) so one place owns "Calendar Wizard", "Weather Service Wizard", etc., and each package (Locomotion, Weather, BedogaGenerator) can remain minimal with runtime-only code where possible.
- **Standalone on GameObjects:** Each wizard component is just a small script: inspector UI with slots + buttons that open the appropriate EditorWindow or run CreateExample(). No need to duplicate the full window UI—the component can invoke `NarrativeCalendarWizardWindow.ShowWindow(calendar)` or equivalent.

### C.4 Conglomerator integration

- Each service wizard (when present) can register its primary asset with the system-drawer conglomerator (e.g. "NarrativeCalendar" → this calendar). The conglomerator then becomes the single place to "find the calendar" or "find the 4D orchestrator" for other tools and wizards.

---

## Part D: Clean up base-level Assets directory; misc directory with service wizard

### D.1 Base-level cleanup

- **Audit root `Assets/`:** Identify loose scripts and assets at the top level (e.g. `BoundsPlacer.cs`, `Camera.cs`, `FirstPersonController.cs`, `ModulatingSoundComponent.cs`, `DrawerTopInteractionBehaviourScript.cs`, etc.) that are not part of a feature package.
- **Misc directory:** Create **`Assets/Misc/`** and move or organize:
  - **Service conglomerator:** e.g. `Assets/Misc/SystemDrawer/` or `Assets/Misc/ServiceConglomerator/` for the system-drawer service script and default asset.
  - **Service wizards:** e.g. `Assets/Misc/ServiceWizards/` (runtime components) and `Assets/Misc/Editor/ServiceWizards/` (editor windows and shared wizard logic moved from Locomotion, Weather, BedogaGenerator as needed).
  - Other one-off or shared scripts that don’t belong in a specific package can move under `Assets/Misc/Scripts/` or similar so the root has only high-level folders (BedogaGenerator, locomotion, Weather, HierarchicalPathFinding, Misc, Scenes, etc.).

### D.2 Resulting structure (target)

- **Assets/**
  - **Misc/**
    - **SystemDrawer/** (or ServiceConglomerator) — conglomerator script + default asset
    - **ServiceWizards/** — wizard components (Calendar, Weather, Spatial4D, Ragdoll, etc.)
    - **Editor/**
      - **ServiceWizards/** — editor windows and shared "create example" logic (moved from packages)
    - (optional) **Scripts/** — other shared/one-off scripts moved from root
  - **BedogaGenerator/** — unchanged except possibly editor wizard code moved to Misc
  - **locomotion/** — unchanged except wizard windows moved to Misc or linked from Misc
  - **Weather/** — unchanged except WeatherServiceWizard moved/copied to Misc or linked
  - **HierarchicalPathFinding/**, **LevelGenerator/**, **Scenes/**, etc.
- **Assets/** root: no (or few) loose .cs files; mostly folders and meta.

### D.3 Order of work

1. Create `Assets/Misc/` and subfolders (SystemDrawer, ServiceWizards, Editor/ServiceWizards).
2. Add system-drawer conglomerator script and optional default asset; implement weak-link registry.
3. Add one "service wizard" component per aspect (starting with calendar, weather, spatial 4D, ragdoll) that can be added to a GameObject and shows slots + buttons; wire buttons to existing windows or create-example logic.
4. Move or copy existing wizard editor windows into `Assets/Misc/Editor/ServiceWizards/` (or keep in package and reference from Misc) so that the Misc directory is the canonical place for "service wizard" entry points.
5. Move loose root-level scripts into `Assets/Misc/Scripts/` (or appropriate package) and fix any references; leave Assets root clean.

---

## Implementation order (combined plan)

1. **Part A (existing):** Timeline bar 1-min modulo → recording started (Mark start / autoStartWithCausality) → causality DTO + list → orchestrator structure + 4D mirror.
2. **Part D.1–D.2:** Create `Assets/Misc/` and subfolders; add SystemDrawer and ServiceWizards layout.
3. **Part B:** Implement system-drawer conglomerator with weak links; place under Misc.
4. **Part C:** Implement one service wizard component per aspect (calendar, weather, spatial 4D, ragdoll, etc.) under Misc; wire to existing windows and create-example logic; optionally move editor windows into Misc/Editor/ServiceWizards.
5. **Part D.3:** Move loose root-level assets into Misc (or correct package); clean up Assets root.
