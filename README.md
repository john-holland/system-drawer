# Locomotion & Weather System Project

A Unity project featuring advanced ragdoll physics, behavior trees, weather systems, narrative time management, and **local LSTM-based narrative tools** (prompt interpreter and calendar summarizer). The architecture is designed so you can **ship asset packs** and **partner with indies**: systems are service-wired, scriptable, and extensible without forking the core.

## Repository map

Sibling repos (assumed layout under `C:/Users/John/`):

| Repo | Role | README |
|------|------|--------|
| **Drawer 2** (this repo) | Unity game systems, System Drawer, narrative/4D/pathfinding | [README.md](README.md) |
| **continuum** | Library web UI + Flask API (upload, search, media parity) | [../continuum/README.md](../continuum/README.md) |
| **unified-semantic-compressor** | USC core: DB schema, compressors, ETL, CLI | [../unified-semantic-compressor/README.md](../unified-semantic-compressor/README.md) |

Unity ↔ Python bridge: [Scripts/CONTINUUM_AND_COMPRESSOR.md](Scripts/CONTINUUM_AND_COMPRESSOR.md)

## Services catalog

### Unity System Drawer

Scene services register on [`SystemDrawerService`](Assets/SystemDrawer/SystemDrawerService.cs). Scene hub: [`SystemDrawerFacilitator`](Assets/SystemDrawer/SystemDrawerFacilitator.cs).

| Service key | Wizard / component | Area | Purpose |
|-------------|-------------------|------|---------|
| `NarrativeCalendar` | [`CalendarServiceWizard`](Assets/SystemDrawer/CalendarServiceWizard.cs) | SystemDrawer | Narrative calendar asset registration |
| `NarrativeLSTMPrompt` | [`NarrativePromptServiceWizard`](Assets/SystemDrawer/NarrativePromptServiceWizard.cs) | SystemDrawer | Local LSTM prompt interpreter rig |
| `RagdollRoot` | [`RagdollServiceWizard`](Assets/SystemDrawer/RagdollServiceWizard.cs) | SystemDrawer | Player / ragdoll root transform |
| `WeatherSystem` | [`WeatherServiceWizardComponent`](Assets/SystemDrawer/WeatherServiceWizardComponent.cs) | SystemDrawer | Weather system GameObject |
| `Spatial4DOrchestrator` | [`Spatial4DServiceWizard`](Assets/BedogaGenerator/Spatial4DServiceWizard.cs) | BedogaGenerator | 4D spatial generator orchestrator |
| `USCBuildService` | [`UscBuildServiceWizard`](Assets/SystemDrawer/UscBuildServiceWizard.cs) | SystemDrawer | USC build / export integration |
| `actor.brain` | [`BrainMessageService`](Assets/SystemDrawer/BrainMessageService.cs) | SystemDrawer | Actor brain message hub (key overridable) |
| `ContinuumNotifications` | [`ContinuumNotificationsService`](Assets/Continuum/ContinuumNotificationsService.cs) | Continuum | Continuum push / notification bridge |

### Python / server services

| Service | Repo | Entry point | Docs |
|---------|------|-------------|------|
| Continuum library server | continuum | `python serve_library.py` | [../continuum/README.md](../continuum/README.md) |
| USC CLI / compressors / ETL | unified-semantic-compressor | `python -m unified_semantic_archiver …` | [../unified-semantic-compressor/README.md](../unified-semantic-compressor/README.md) |
| Continuum API (episodes / thesaurus / screenplay) | Drawer 2 | `python -m continuum_api.server` | [Scripts/continuum_api/README.md](Scripts/continuum_api/README.md) |
| Video storage tool (legacy) | Drawer 2 | `Scripts/video_storage_tool/` | [Scripts/video_storage_tool/README.md](Scripts/video_storage_tool/README.md) — superseded by USC media parity |

## Project Structure

This project uses a **"Light Package" mode** where third-party assets are excluded from git. Only custom code and project structure are tracked.

### Core Systems

- **Locomotion System** (`Assets/locomotion/`) - Ragdoll physics, behavior trees, pathfinding
- **Weather System** (`Assets/Weather/`) - Weather simulation, terrain integration, portals
- **Narrative System** (`Assets/locomotion/narrative/`) - Time management, calendar, story execution
- **Narrative LSTM** (`Assets/locomotion/narrative/Inference/`) - Local prompt interpreter (natural language → events + 4D) and calendar summarizer (“what’s going on”)
- **Hierarchical Pathfinding** (`Assets/HierarchicalPathFinding/`) - Spatial pathfinding system
- **System Drawer** (`Assets/SystemDrawer/`) - Service wizards that register and wire systems (calendar, 4D, weather, **prompt**) for asset-pack and indie workflows

## Getting Started

### Prerequisites

- Unity 2021.3 or later
- Git

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd "Drawer 2"
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Add project from disk
   - Select the project folder

3. **Import Third-Party Assets**
   - See [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md) for a complete list
   - Open Unity Asset Store (Window > Asset Store)
   - Search for and import each required asset
   - Or use the validator: **Tools > Validate Third-Party Assets**

4. **Verify Setup**
   - Open **Tools > Validate Third-Party Assets** to check all assets are imported
   - Open a test scene to verify everything works

## Development Workflow

### Light Package Mode

This project excludes third-party assets from git to:
- Reduce repository size
- Avoid licensing conflicts
- Keep the repository focused on custom code

**What's tracked:**
- All custom C# scripts
- Project settings and structure
- Custom prefabs and scenes
- Assembly definitions

**What's excluded:**
- Third-party asset packages (see `THIRD_PARTY_ASSETS.md`)
- Library and build artifacts (see `.gitignore`)

### Testing

- **Python (Scripts):** From repo root: `cd Scripts && python -m pytest video_storage_tool/tests -v -m "not slow"`. Some tests are skipped without optional fixtures; three reconstitute roundtrip tests require **ffmpeg** on PATH (e.g. `brew install ffmpeg`). Fix or skip when ffmpeg is missing for a green run.

### Adding New Third-Party Assets

If you add a new third-party asset:
1. Add it to `.gitignore`
2. Document it in `THIRD_PARTY_ASSETS.md`
3. Update `ThirdPartyAssetValidator.cs` if needed

## Key Features

### Ragdoll System
- Full body physics with auto-creation of body parts
- Radial limits for joint constraints
- Physics card system for movement
- Behavior tree integration

### Weather System
- Dynamic weather simulation
- Terrain integration (height maps and mesh terrains)
- Portal-based rain effects
- Global illumination control

### Narrative System
- Calendar-based time management
- Behavior tree execution
- Event scheduling
- Time-based weather integration
- **Narrative LSTM**: train on your project’s calendar/4D data; run a local “ChatGPT-like” prompt interpreter (natural language → narrative events + 4D spatial entries) and a summarizer (calendar → short “what’s going on” text). No cloud required; ONNX + Barracuda in-editor and at runtime.

### Pathfinding
- Hierarchical spatial pathfinding
- Integration with behavior trees
- Card-based movement system

## Tools & Utilities

- **Ragdoll Fitting Wizard** - Auto-configure ragdoll components
- **Weather Service Wizard** - Setup weather systems
- **Third-Party Asset Validator** - Check required assets
- **Animation Behavior Tree Generator** - Convert animations to behavior trees

## Documentation index

When adding a new `.md` under `Assets/` or `Scripts/`, append it here (and to sibling READMEs if cross-repo).

### Project root

- [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md) — Required third-party assets

### Unity ↔ Python bridge

- [Scripts/CONTINUUM_AND_COMPRESSOR.md](Scripts/CONTINUUM_AND_COMPRESSOR.md) — Continuum + USC integration from Unity

### Narrative / LSTM / episodic

- [Scripts/README_NarrativeLSTM.md](Scripts/README_NarrativeLSTM.md) — Export → vocab → train (Python) → run in Unity (Barracuda)
- [Scripts/EPISODIC_CONTENT_GENERATOR.md](Scripts/EPISODIC_CONTENT_GENERATOR.md) — Episodic content pipeline: episodes, USC assets, scripts → Spatial 4D
- [Scripts/EPISODIC_PORT_BROKER_AND_CAVE.md](Scripts/EPISODIC_PORT_BROKER_AND_CAVE.md) — Episodic port broker and Cave integration
- [Assets/Documentation/PROMPT_GENERATOR_FLOW.md](Assets/Documentation/PROMPT_GENERATOR_FLOW.md) — Prompt generator flow

### Spatial volumes / SDF Max

- [Assets/SdfMax/docs/SdfMaxArchitecture.md](Assets/SdfMax/docs/SdfMaxArchitecture.md) — SDF Max theory and architecture
- [Assets/SdfMax/docs/SdfMaxComposition.md](Assets/SdfMax/docs/SdfMaxComposition.md) — Composition components and editor
- [Assets/SdfMax/docs/SdfMaxSurfaceMesh.md](Assets/SdfMax/docs/SdfMaxSurfaceMesh.md) — Static and skinned surface meshes
- [Assets/HierarchicalPathFinding/docs/SpatialVolumeProvider.md](Assets/HierarchicalPathFinding/docs/SpatialVolumeProvider.md) — Unified volume API

### 4D / Bedoga

- [Assets/BedogaGenerator/SpatialGenerator4D_Setup.md](Assets/BedogaGenerator/SpatialGenerator4D_Setup.md) — SpatialGenerator4D setup guide

### Locomotion / travel / ragdoll

- [Assets/locomotion/docs/PlayerRagdollControllerSetup.md](Assets/locomotion/docs/PlayerRagdollControllerSetup.md) — Player ragdoll controller setup
- [Assets/locomotion/docs/TravelFormation.md](Assets/locomotion/docs/TravelFormation.md) — Travel formation
- [Assets/locomotion/docs/PromptPlayerIdleThirdPerson.md](Assets/locomotion/docs/PromptPlayerIdleThirdPerson.md) — Third-person idle prompt

### Weather

- [Assets/Weather/weather.md](Assets/Weather/weather.md) — Weather system overview
- [Assets/Weather/cloud_lighting_integration.md](Assets/Weather/cloud_lighting_integration.md) — Cloud lighting integration

### USC build (Unity)

- [Assets/Documentation/USC_BUILD_MODES.md](Assets/Documentation/USC_BUILD_MODES.md) — USC build modes

### Theory / one-offs

- [Assets/Documentation/UnifyingTheoryMath.md](Assets/Documentation/UnifyingTheoryMath.md) — Unifying theory math
- [Assets/Documentation/TimeTravelingBearBuildSteps.md](Assets/Documentation/TimeTravelingBearBuildSteps.md) — Time Traveling Bear build steps
- [Assets/TrebleEnhancement.md](Assets/TrebleEnhancement.md) — Treble enhancement

### Python APIs

- [Scripts/continuum_api/README.md](Scripts/continuum_api/README.md) — Episode script, thesaurus, screenplay API
- [Scripts/video_storage_tool/README.md](Scripts/video_storage_tool/README.md) — Legacy video storage (superseded by USC media parity)

### Sibling repositories

- [continuum documentation index](../continuum/README.md#documentation-index)
- [unified-semantic-compressor documentation index](../unified-semantic-compressor/README.md#documentation-index)

## Asset packs & indie partnership

The project is structured so you can:

- **Ship asset packs** that plug into the same service keys (calendar, 4D, weather, narrative prompt) without replacing core code.
- **Partner with indies** by exposing wizards and drawers: they assign references from the System Drawer or create rigs (e.g. **Narrative Prompt Service Wizard** → Create LSTM prompt rig) and wire their own content.
- **Keep narrative and 4D local**: train LSTM on your own data, run prompt interpretation and “what’s going on” summarization entirely in-editor or at runtime with Barracuda.

## License

Custom code in this project is proprietary. Third-party assets maintain their own licenses (see `THIRD_PARTY_ASSETS.md`).
