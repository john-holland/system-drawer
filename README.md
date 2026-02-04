# Locomotion & Weather System Project

A Unity project featuring advanced ragdoll physics, behavior trees, weather systems, narrative time management, and **local LSTM-based narrative tools** (prompt interpreter and calendar summarizer). The architecture is designed so you can **ship asset packs** and **partner with indies**: systems are service-wired, scriptable, and extensible without forking the core.

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

## Documentation

- [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md) - Required third-party assets
- [Scripts/README_NarrativeLSTM.md](Scripts/README_NarrativeLSTM.md) - Narrative LSTM: export → vocab → train (Python) → run in Unity (Barracuda)
- [.cursor/plans/](.cursor/plans/) - Development plans and architecture docs

## Asset packs & indie partnership

The project is structured so you can:

- **Ship asset packs** that plug into the same service keys (calendar, 4D, weather, narrative prompt) without replacing core code.
- **Partner with indies** by exposing wizards and drawers: they assign references from the System Drawer or create rigs (e.g. **Narrative Prompt Service Wizard** → Create LSTM prompt rig) and wire their own content.
- **Keep narrative and 4D local**: train LSTM on your own data, run prompt interpretation and “what’s going on” summarization entirely in-editor or at runtime with Barracuda.

## License

Custom code in this project is proprietary. Third-party assets maintain their own licenses (see `THIRD_PARTY_ASSETS.md`).
