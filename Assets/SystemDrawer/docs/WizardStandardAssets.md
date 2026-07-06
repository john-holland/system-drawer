# Wizard Standard Assets

One-click **Setup Standard Assets** on System Drawer service wizards creates versioned default assets under `Assets/SystemDrawer/StandardAssets/` and idempotent scene objects under `_StandardScene` (sibling of `_Wizards` on the hub).

## Rules

- **Idempotent**: existing assets and scene refs are reused; second click should only report "Skipped".
- **Undo-safe**: uses Unity Undo for all creates and assignments.
- **Bulk setup**: Facilitator Hub → **Setup Standard Assets (all applicable)** runs wizards in dependency order.

## Asset paths

| Domain | Path |
|--------|------|
| Feature Budget | `StandardAssets/FeatureBudget/DefaultFeatureBudgetProfile.asset` |
| USC | `StandardAssets/USC/DefaultUscBuildManifest.json` |
| Planetary | `StandardAssets/Planetary/LittlePrincePresetLibrary.asset` |
| Dream Cycle | `StandardAssets/DreamCycle/DefaultNeedAspectRegistry.asset` |
| Networking | `StandardAssets/Networking/DefaultNetworkSettings.asset` |
| Quest | `StandardAssets/Quest/DefaultQuestBehaviorTreeBundle.asset` |

## Per-wizard buttons

| Wizard | Inspector location | What setup does |
|--------|-------------------|-------------------|
| Calendar | `_Wizards` → Calendar Service Wizard | `NarrativeCalendar` GO + starter events |
| Narrative Prompt | `_Wizards` → Narrative Prompt Service Wizard | LSTM rig + calendar wiring |
| USC Build | `_Wizards` → USC Build Service Wizard | Default manifest JSON TextAsset |
| Planet | `_Wizards` → Planet Service Wizard | `PlanetSystem` + Little Prince preset |
| Feature Budget | SystemDrawer root → Feature Budget Runtime | Default profile + planet ratio sync |
| Weather | `_Wizards` → Weather Service Wizard | Full weather stack (Clear Day preset) |
| Spatial 4D | `_Wizards` → Spatial 4D Service Wizard | Orchestrator + 4D generator child |
| Quest | `_Wizards` → Quest Service Wizard | QuestRunner + QuestMapRenderer |
| Dream Cycle | `_Wizards` → Dream Cycle Service Wizard | Day/night runners + need registry |
| Network | `_Wizards` → Network Service Wizard | Client/server orchestrators + settings |
| Ragdoll | `_Wizards` → Ragdoll Service Wizard | Placeholder `RagdollRoot` only |

## Not applicable

- **Menu Ragdoll Service Wizard**: main-menu specific; assign manually.
- **BrainMessageService / SystemDrawerAnimator / AmbulatoryActorRegistrar**: need scene context; assign manually.

## Recommended workflow

1. **GameObject → System Drawer → System Drawer Hub** (or use Facilitator Hub).
2. **Ensure child _Wizards + bind references**.
3. **Setup Standard Assets (all applicable)**.
4. **Push registrations** before Play mode.

## Weather editor window

The Weather Service Wizard window **Auto-Setup** button uses the same `WeatherStandardAssets.SetupForWizard` path as the component wizard.
