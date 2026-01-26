# Locomotion & Weather System Project

A Unity project featuring advanced ragdoll physics, behavior trees, weather systems, and narrative time management.

## Project Structure

This project uses a **"Light Package" mode** where third-party assets are excluded from git. Only custom code and project structure are tracked.

### Core Systems

- **Locomotion System** (`Assets/locomotion/`) - Ragdoll physics, behavior trees, pathfinding
- **Weather System** (`Assets/Weather/`) - Weather simulation, terrain integration, portals
- **Narrative System** (`Assets/locomotion/narrative/`) - Time management, calendar, story execution
- **Hierarchical Pathfinding** (`Assets/HierarchicalPathFinding/`) - Spatial pathfinding system

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
- [.cursor/plans/](.cursor/plans/) - Development plans and architecture docs

## License

Custom code in this project is proprietary. Third-party assets maintain their own licenses (see `THIRD_PARTY_ASSETS.md`).
