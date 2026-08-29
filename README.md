# Locomotion & Weather System Project



A Unity project featuring advanced ragdoll physics, behavior trees, weather systems, narrative time management, and **local LSTM-based narrative tools** (prompt interpreter and calendar summarizer). The architecture is designed so you can **ship asset packs** and **partner with indies**: systems are service-wired, scriptable, and extensible without forking the core.



## Repository map



Sibling repos (assumed layout under `C:/Users/John/`):



| Repo | Role | README |

|------|------|--------|

| **Drawer 2** (this repo) | Unity game systems, System Drawer, narrative/4D/pathfinding | [README.md](README.md) |

| **continuuuum** | Library web UI + Flask API (upload, search, media parity) | [../continuuuum/README.md](../continuuuum/README.md) |

| **unified-semantic-compressor** | USC core: DB schema, compressors, ETL, CLI | [../unified-semantic-compressor/README.md](../unified-semantic-compressor/README.md) |



Unity ↔ Python bridge: [Scripts/CONTINUUUUM_AND_COMPRESSOR.md](Scripts/CONTINUUUUM_AND_COMPRESSOR.md)



## Services catalog



### Unity System Drawer



Scene services register on [`SystemDrawerService`](Assets/SystemDrawer/SystemDrawerService.cs). Scene hub: [`SystemDrawerFacilitator`](Assets/SystemDrawer/SystemDrawerFacilitator.cs).



| Service key | Wizard / component | Area | Purpose |

|-------------|-------------------|------|---------|

| `NarrativeCalendar` | [`CalendarServiceWizard`](Assets/SystemDrawer/CalendarServiceWizard.cs) | SystemDrawer | Narrative calendar asset registration |

| `NarrativeLSTMPrompt` | [`NarrativePromptServiceWizard`](Assets/SystemDrawer/NarrativePromptServiceWizard.cs) | SystemDrawer | Local LSTM prompt interpreter rig |

| `RagdollRoot` | [`RagdollServiceWizard`](Assets/SystemDrawer/RagdollServiceWizard.cs) | SystemDrawer | Player / ragdoll root transform |

| `WeatherSystem` | [`WeatherServiceWizardComponent`](Assets/SystemDrawer/WeatherServiceWizardComponent.cs) | SystemDrawer | Weather system GameObject (legacy key; see canonical keys below) |

| `weather.system` | [`WeatherServiceWizardComponent`](Assets/SystemDrawer/WeatherServiceWizardComponent.cs) | SystemDrawer | Canonical weather system GameObject |

| `weather.physicsManifold` | [`WeatherServiceWizardComponent`](Assets/SystemDrawer/WeatherServiceWizardComponent.cs) | SystemDrawer | [`WeatherPhysicsManifold`](Assets/Weather/WeatherPhysicsManifold.cs) scalar-field grid |

| `PlanetSystem` | [`PlanetServiceWizardComponent`](Assets/SystemDrawer/PlanetServiceWizardComponent.cs) | SystemDrawer | Planet host GameObject (legacy key) |

| `planet.body` | [`PlanetServiceWizardComponent`](Assets/SystemDrawer/PlanetServiceWizardComponent.cs) | Planetary | [`PlanetBody`](Assets/Planetary/Runtime/PlanetBody.cs) host |

| `planet.shellGrid` | [`PlanetServiceWizardComponent`](Assets/SystemDrawer/PlanetServiceWizardComponent.cs) | Planetary | [`PlanetShellManifoldGrid`](Assets/Planetary/Runtime/Bridges/PlanetShellManifoldGrid.cs) |

| `planet.physicalManifold` | [`PlanetServiceWizardComponent`](Assets/SystemDrawer/PlanetServiceWizardComponent.cs) | Planetary | Relativity / gameplay manifold overlay |

| `pathing.hierarchical` | [`SystemDrawerFacilitator`](Assets/SystemDrawer/SystemDrawerFacilitator.cs) | Pathfinding | [`HierarchicalPathingSolver`](Assets/HierarchicalPathFinding/HierarchicalPathingSolver.cs) |

| `animation.systemDrawerAnimator` | [`SystemDrawerAnimator`](Assets/SystemDrawer/SystemDrawerAnimator.cs) | Locomotion | Multi-layer animation coordinator |

| `Spatial4DOrchestrator` | [`Spatial4DServiceWizard`](Assets/BedogaGenerator/Spatial4DServiceWizard.cs) | BedogaGenerator | 4D spatial generator orchestrator |

| `USCBuildService` | [`UscBuildServiceWizard`](Assets/SystemDrawer/UscBuildServiceWizard.cs) | SystemDrawer | USC build / export integration |

| `actor.brain` | [`BrainMessageService`](Assets/SystemDrawer/BrainMessageService.cs) | SystemDrawer | Actor brain message hub (key overridable) |

| `ContinuuuumNotifications` | [`ContinuuuumNotificationsService`](Assets/Continuuuum/ContinuuuumNotificationsService.cs) | Continuuuum | Continuuuum push / notification bridge |

| `network.clientOrchestrator` | [`ClientOrchestrator`](Assets/SystemDrawer/Networking/ClientOrchestrator.cs) | Networking | Scene client singleton (TCP/UDP, mode selection) |

| `network.serverOrchestrator` | [`ServerOrchestrator`](Assets/SystemDrawer/Networking/ServerOrchestrator.cs) | Networking | Game server / SP loopback host |

| `network.serverMode` | [`ServerOrchestrator`](Assets/SystemDrawer/Networking/ServerOrchestrator.cs) | Networking | Active `NetworkServerMode` registry object |

| `network.lobbyServer` | [`LobbyServerHost`](Assets/SystemDrawer/Networking/LobbyServerHost.cs) via server | Networking | Optional lobby registration/list TCP service |

| `menu.ragdoll` | [`MenuRagdollServiceWizard`](Assets/SystemDrawer/Networking/MenuRagdollServiceWizard.cs) | Networking | Main menu event tree (no Brain) |



**Networking modes:** `SinglePlayer` (local loopback server), `AuthoritativePeerToPeer` (ownership + causality tree streaming), `ClassicLockstep` (UDP decisions + audit). See [`NetworkingArchitecture.md`](Assets/SystemDrawer/docs/NetworkingArchitecture.md).



**Dedicated server CLI:** `-ds -m p2p -p 7777 --host-lobby --lobby-port 7780 --lobby-name "Session"` (also `--no-lobby`, `--bind-address`).



**Resolver:** [`SystemDrawerSceneServices`](Assets/SystemDrawer/SystemDrawerSceneServices.cs) + key constants in [`SystemDrawerServiceKeys`](Assets/SystemDrawer/SystemDrawerServiceKeys.cs). Assemblies that cannot reference SystemDrawer directly use [`SceneServiceLookup`](Assets/Weather/SceneServiceLookup.cs) (reflection bridge in Weather.Runtime).



### Python / server services



| Service | Repo | Entry point | Docs |

|---------|------|-------------|------|

| Continuuuum library server | continuuuum | `python serve_library.py` | [../continuuuum/README.md](../continuuuum/README.md) |

| USC CLI / compressors / ETL | unified-semantic-compressor | `python -m unified_semantic_archiver …` | [../unified-semantic-compressor/README.md](../unified-semantic-compressor/README.md) |

| Continuuuum API (episodes / thesaurus / screenplay) | Drawer 2 | `python -m continuuuum_api.server` | [Scripts/continuuuum_api/README.md](Scripts/continuuuum_api/README.md) |

| Video storage tool (legacy) | Drawer 2 | `Scripts/video_storage_tool/` | [Scripts/video_storage_tool/README.md](Scripts/video_storage_tool/README.md) — superseded by USC media parity |



## Project Structure



This project uses a **"Light Package" mode** where third-party assets are excluded from git. Only custom code and project structure are tracked.



### Core Systems



- **Locomotion System** (`Assets/locomotion/`) - Ragdoll physics, behavior trees, pathfinding

- **Weather System** (`Assets/Weather/`) - Weather simulation, terrain integration, portals

- **Narrative System** (`Assets/locomotion/narrative/`) - Time management, calendar, story execution

- **Narrative LSTM** (`Assets/locomotion/narrative/Inference/`) - Local prompt interpreter (natural language → events + 4D) and calendar summarizer (“what’s going on”)

- **Hierarchical Pathfinding** (`Assets/HierarchicalPathFinding/`) - Spatial pathfinding, physical-medium solvers, volume index

- **Planetary** (`Assets/Planetary/`) - Planet body, shell manifold grid, physics bridges, curved spacetime pathing

- **System Drawer** (`Assets/SystemDrawer/`) - Service wizards that register and wire systems (calendar, 4D, weather, **prompt**, **physics bridge**) for asset-pack and indie workflows



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



### Pathfinding & travel



- Hierarchical spatial pathfinding with [`HierarchicalPathingSolver`](Assets/HierarchicalPathFinding/HierarchicalPathingSolver.cs)

- Physical-medium routing via [`PhysicalPathingSolverRegistry`](Assets/HierarchicalPathFinding/PhysicalPathingSolverRegistry.cs) and [`PhysicalMediumVolumeIndex`](Assets/HierarchicalPathFinding/PhysicalMediumVolumeIndex.cs) (medium + shell altitude band)

- Multi-modal travel planning ([`TravelAgent`](Assets/locomotion/travel/TravelAgent.cs), [`CompositeMultiModalPathNode`](Assets/locomotion/nodes/CompositeMultiModalPathNode.cs))

- Reverse-leg arc budget ([`TravelPathReverseLimits`](Assets/locomotion/travel/TravelPathReverseLimits.cs), [`TravelPathKinematicsProfile`](Assets/locomotion/travel/TravelPathKinematicsProfile.cs)) with playback ([`ReversePlaybackController`](Assets/locomotion/travel/ReversePlaybackController.cs))

- Integration with behavior trees and card-based movement



### Planetary physics & weather manifold



- Planet-centric shell grid ([`PlanetShellManifoldGrid`](Assets/Planetary/Runtime/Bridges/PlanetShellManifoldGrid.cs)) with pole caps, longitude wrap, and altitude bands

- Flat-grid adapter ([`PlanetShellToWeatherManifoldAdapter`](Assets/Planetary/Runtime/Bridges/PlanetShellToWeatherManifoldAdapter.cs)) stamping into [`WeatherPhysicsManifold`](Assets/Weather/WeatherPhysicsManifold.cs)

- Composition / surface bridges ([`PlanetPhysicsManifoldBridge`](Assets/Planetary/Runtime/Bridges/PlanetPhysicsManifoldBridge.cs), [`RoadPhysicsManifoldBridge`](Assets/Roads/Runtime/RoadPhysicsManifoldBridge.cs))

- Canonical spatiotemporal field charts ([`CanonicalSpatiotemporalField`](Assets/Planetary/Runtime/Field/CanonicalSpatiotemporalField.cs))

- Great-circle shell pathing ([`PlanetShellPathingSolver`](Assets/Planetary/Runtime/Pathing/PlanetShellPathingSolver.cs), [`CurvedSpacetimeSd2PathingSolver`](Assets/Planetary/Runtime/Pathing/CurvedSpacetimeSd2PathingSolver.cs))

- Scene service resolution replaces ad-hoc `FindObject` lookups (see Services catalog above)



## Tools & Utilities



- **Ragdoll Fitting Wizard** - Auto-configure ragdoll components

- **Weather Service Wizard** - Setup weather systems

- **Physics Bridge Editor** (`Window/System Drawer/Physics/Physics Bridge Editor`) - Discover road/planet/ragdoll bridges, shell grid overlay, unresolved service keys ([`PhysicsBridgeEditorWindow`](Assets/SystemDrawer/Editor/PhysicsBridgeEditorWindow.cs))

- **System Drawer Facilitator** - Push / validate scene service registrations ([`SystemDrawerFacilitatorHubWindow`](Assets/SystemDrawer/Editor/SystemDrawerFacilitatorHubWindow.cs))

- **Third-Party Asset Validator** - Check required assets

- **Animation Behavior Tree Generator** - Convert animations to behavior trees



## Documentation index



When adding a new `.md` under `Assets/` or `Scripts/`, append it here (and to sibling READMEs if cross-repo).



### Project root



- [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md) — Required third-party assets



### Unity ↔ Python bridge



- [Scripts/CONTINUUUUM_AND_COMPRESSOR.md](Scripts/CONTINUUUUM_AND_COMPRESSOR.md) — Continuuuum + USC integration from Unity



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

- [Assets/locomotion/docs/SkinnedMeshLoopSection.md](Assets/locomotion/docs/SkinnedMeshLoopSection.md) — Skinned mesh loop picker, multi-loop split, prefab export



**Travel pathing & reverse playback (source)**



| Topic | Entry points |

|-------|----------------|

| Travel agent & path preview | [`TravelAgent.cs`](Assets/locomotion/travel/TravelAgent.cs) |

| Reverse arc budget | [`TravelPathReverseLimits.cs`](Assets/locomotion/travel/TravelPathReverseLimits.cs), [`TravelPathKinematicsProfile.cs`](Assets/locomotion/travel/TravelPathKinematicsProfile.cs) |

| Reverse animation playback | [`ReversePlaybackController.cs`](Assets/locomotion/travel/ReversePlaybackController.cs), [`SystemDrawerAnimator.cs`](Assets/SystemDrawer/SystemDrawerAnimator.cs), [`AnimationBehaviorTreeNode.cs`](Assets/locomotion/AnimationBehaviorTreeNode.cs) |

| Leg execution & activation | [`TravelLegSequenceNode.cs`](Assets/locomotion/nodes/TravelLegSequenceNode.cs), [`ApplyTravelLegAnimationNode.cs`](Assets/locomotion/nodes/travel/ApplyTravelLegAnimationNode.cs) |

| Planner nodes | [`CompositeMultiModalPathNode.cs`](Assets/locomotion/nodes/CompositeMultiModalPathNode.cs), [`PathfindingNode.cs`](Assets/locomotion/nodes/PathfindingNode.cs) |

| Walking cards in reverse | [`PhysicsCardSolver.cs`](Assets/locomotion/PhysicsCardSolver.cs) (`GenerateWalkingCard`) |

| Tests | [`ReversePlaybackControllerTests.cs`](Assets/locomotion/Tests/ReversePlaybackControllerTests.cs), [`AnimationBehaviorTreeReverseTests.cs`](Assets/locomotion/Tests/AnimationBehaviorTreeReverseTests.cs), [`TravelPathReverseLimitsTests.cs`](Assets/locomotion/Tests/TravelPathReverseLimitsTests.cs) |



### Hierarchical pathfinding / physical medium



**Physical pathing (source)**



| Topic | Entry points |

|-------|----------------|

| Solver registry | [`PhysicalPathingSolverRegistry.cs`](Assets/HierarchicalPathFinding/PhysicalPathingSolverRegistry.cs) |

| Medium + altitude resolution | [`PhysicalMediumVolumeIndex.cs`](Assets/HierarchicalPathFinding/PhysicalMediumVolumeIndex.cs), [`PhysicalMediumVolume.cs`](Assets/HierarchicalPathFinding/PhysicalMediumVolume.cs) |

| Hierarchical coordinator | [`HierarchicalPathingSolver.cs`](Assets/HierarchicalPathFinding/HierarchicalPathingSolver.cs) |



### Weather



- [Assets/Weather/weather.md](Assets/Weather/weather.md) — Weather system overview

- [Assets/Weather/cloud_lighting_integration.md](Assets/Weather/cloud_lighting_integration.md) — Cloud lighting integration



### System Drawer



- [Assets/SystemDrawer/docs/MemorySwizzleView.md](Assets/SystemDrawer/docs/MemorySwizzleView.md) — Memory Swizzle treemap profiler (WinDirStat-style)

- [Assets/SystemDrawer/docs/PerfTraceView.md](Assets/SystemDrawer/docs/PerfTraceView.md) — PerfTrace scoped timing overlay



**Scene services & physics bridge (source)**



| Topic | Entry points |

|-------|----------------|

| Canonical keys | [`SystemDrawerServiceKeys.cs`](Assets/SystemDrawer/SystemDrawerServiceKeys.cs) |

| Resolver | [`SystemDrawerSceneServices.cs`](Assets/SystemDrawer/SystemDrawerSceneServices.cs) |

| Cross-assembly lookup | [`SceneServiceLookup.cs`](Assets/Weather/SceneServiceLookup.cs) |

| Facilitator push / validate | [`SystemDrawerFacilitator.cs`](Assets/SystemDrawer/SystemDrawerFacilitator.cs), [`SystemDrawerFacilitatorHubWindow.cs`](Assets/SystemDrawer/Editor/SystemDrawerFacilitatorHubWindow.cs) |

| Planet / weather wizards | [`PlanetServiceWizardComponent.cs`](Assets/SystemDrawer/PlanetServiceWizardComponent.cs), [`WeatherServiceWizardComponent.cs`](Assets/SystemDrawer/WeatherServiceWizardComponent.cs) |

| Physics Bridge Editor | [`PhysicsBridgeEditorWindow.cs`](Assets/SystemDrawer/Editor/PhysicsBridgeEditorWindow.cs), [`PhysicsBridgeRegistry.cs`](Assets/SystemDrawer/Editor/PhysicsBridgeRegistry.cs), [`PhysicsBridgeEditorWindow.ShellGridPanels.cs`](Assets/SystemDrawer/Editor/PhysicsBridgeEditorWindow.ShellGridPanels.cs) |

| Tests | [`SystemDrawerSceneServicesTests.cs`](Assets/SystemDrawer/Tests/SystemDrawerSceneServicesTests.cs) |



### Planetary



- [Assets/Planetary/docs/PlanetaryArchitecture.md](Assets/Planetary/docs/PlanetaryArchitecture.md) — Planet meshes, SDF planar features, relativity pathing, spaceship integration



**Shell manifold & physics bridge (source)**



| Topic | Entry points |

|-------|----------------|

| Shell grid (lat/lon/altitude) | [`PlanetShellManifoldGrid.cs`](Assets/Planetary/Runtime/Bridges/PlanetShellManifoldGrid.cs) (`ShellCellId`) |

| Weather manifold adapter | [`PlanetShellToWeatherManifoldAdapter.cs`](Assets/Planetary/Runtime/Bridges/PlanetShellToWeatherManifoldAdapter.cs) |

| Surface stamp bridge | [`PlanetPhysicsManifoldBridge.cs`](Assets/Planetary/Runtime/Bridges/PlanetPhysicsManifoldBridge.cs) |

| Planet host / rebake | [`PlanetBody.cs`](Assets/Planetary/Runtime/PlanetBody.cs), [`PlanetInteriorPhysicsUpdater.cs`](Assets/Planetary/Runtime/Tectonics/PlanetInteriorPhysicsUpdater.cs) |

| Time-travel manifold restore | [`PlanetaryWeatherTimeTravelSystem.cs`](Assets/Planetary/Runtime/TimeTravel/PlanetaryWeatherTimeTravelSystem.cs) |

| Canonical field charts | [`CanonicalSpatiotemporalField.cs`](Assets/Planetary/Runtime/Field/CanonicalSpatiotemporalField.cs) |

| Lava / emission manifold | [`LavaPhysicsManifold.cs`](Assets/Planetary/Runtime/Lava/LavaPhysicsManifold.cs) |

| Great-circle pathing | [`PlanetShellPathingSolver.cs`](Assets/Planetary/Runtime/Pathing/PlanetShellPathingSolver.cs), [`CurvedSpacetimeSd2PathingSolver.cs`](Assets/Planetary/Runtime/Pathing/CurvedSpacetimeSd2PathingSolver.cs), [`PlanetPathingBackend.cs`](Assets/Planetary/Runtime/Pathing/PlanetPathingBackend.cs) |

| Spherical coordinates | [`SphericalCoordinates.cs`](Assets/Planetary/Runtime/SphericalCoordinates.cs) |

| Tests | [`PlanetShellManifoldGridTests.cs`](Assets/Planetary/Tests/PlanetShellManifoldGridTests.cs) |



### USC build (Unity)



- [Assets/Documentation/USC_BUILD_MODES.md](Assets/Documentation/USC_BUILD_MODES.md) — USC build modes



### Theory / one-offs



- [Assets/Documentation/UnifyingTheoryMath.md](Assets/Documentation/UnifyingTheoryMath.md) — Unifying theory math

- [Assets/Documentation/TimeTravelingBearBuildSteps.md](Assets/Documentation/TimeTravelingBearBuildSteps.md) — Time Traveling Bear build steps

- [Assets/TrebleEnhancement.md](Assets/TrebleEnhancement.md) — Treble enhancement



### Python APIs



- [Scripts/continuuuum_api/README.md](Scripts/continuuuum_api/README.md) — Episode script, thesaurus, screenplay API

- [Scripts/video_storage_tool/README.md](Scripts/video_storage_tool/README.md) — Legacy video storage (superseded by USC media parity)



### Sibling repositories



- [continuuuum documentation index](../continuuuum/README.md#documentation-index)

- [unified-semantic-compressor documentation index](../unified-semantic-compressor/README.md#documentation-index)



## Asset packs & indie partnership



The project is structured so you can:



- **Ship asset packs** that plug into the same service keys (calendar, 4D, weather, narrative prompt) without replacing core code.

- **Partner with indies** by exposing wizards and drawers: they assign references from the System Drawer or create rigs (e.g. **Narrative Prompt Service Wizard** → Create LSTM prompt rig) and wire their own content.

- **Keep narrative and 4D local**: train LSTM on your own data, run prompt interpretation and “what’s going on” summarization entirely in-editor or at runtime with Barracuda.



## License



Custom code in this project is proprietary. Third-party assets maintain their own licenses (see `THIRD_PARTY_ASSETS.md`).

