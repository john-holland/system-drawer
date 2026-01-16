# Weather System

Let's implement a simple yet powerful weather system display for Unity.

We should integrate as seamlessly and closelessly as possible with the existing physics system, so you can throw a house around with a giant twister without too much effort.

## Unit Standards

For compatibility with standard weather forecast APIs and data formats (OpenWeatherMap, NOAA/NWS, METAR/TAF, ECMWF):

- **Temperature**: Celsius (°C) - Standard in most APIs (can convert from Kelvin: K - 273.15)
- **Pressure**: Hectopascals (hPa) / Millibars (mb) - Standard meteorological unit (1 hPa = 1 mb = 100 Pa)
- **Wind Speed**: Meters per second (m/s) - SI standard (can convert: 1 m/s ≈ 1.944 knots, 1 m/s ≈ 3.6 km/h)
- **Wind Direction**: Degrees (0-360°, where 0° = North, 90° = East, 180° = South, 270° = West)
- **Precipitation Rate**: Millimeters per hour (mm/h) - Standard metric unit
- **Humidity**: Percentage (0-100%) - Relative humidity
- **Visibility**: Meters (m) or Kilometers (km) - Standard metric
- **Cloud Cover**: Oktas (0-8) or Percentage (0-100%) - Oktas are standard in aviation weather
- **Dew Point**: Celsius (°C) - Standard meteorological unit

## Weather System

The weather system is the physics solver for the weather.

Collecting any `WeatherEvent` objects created during gameplay, the Weather System solves the current weather stage by running each sub system's service update:
 * meteorology
 * wind
 * precipitation
 * water
    * dam
    * river
 * cloud // after all that what do they look like!
 * // here we should have an endstate wind graph that will carry over in part or whole to the next frame

### Service Update Order

The `WeatherSystem` executes subsystem updates in a specific order to ensure proper data flow and dependencies:

1. **Meteorology** - Sets boundary conditions, stages weather events, controls cloud movements
2. **Wind** - Generates wind field vectors, affects clouds, precipitation, and physics objects
3. **Precipitation** - Rain/snow rendering, phase changes, accumulation tracking
4. **Water** - Water body management, height maps, flow calculations
   - **Dam** - Water blocking/overflow calculations (executed within Water update)
   - **River** - Flow calculations, spline management (executed within Water update)
5. **Cloud** - Visual representation, pressure system integration, meteorology linking
6. **WeatherPhysicsManifold** - Final state aggregation for shaders, spatial tree updates

This order ensures:
- Boundary conditions are set before field calculations
- Wind affects clouds and precipitation before they render
- Precipitation data is available for water systems
- Water systems update before visual representation
- Final manifold state is ready for shader access

### Data Flow

Data flows through the weather system in the following pattern:

```
WeatherEvent → WeatherSystem
    ↓
Meteorology (atmospheric conditions)
    ↓
Wind (force field generation)
    ↓
WeatherPhysicsManifold (advection, pressure, diffusion)
    ↓
Precipitation (uses manifold state)
    ↓
Water (receives precipitation, manages rivers/ponds/dams)
    ↓
Cloud (visual representation of manifold)
    ↓
WeatherPhysicsManifold (final state for shaders)
```

**Key Data Paths**:
- **Meteorology → Wind**: Temperature, pressure gradients drive wind patterns
- **Wind → Cloud**: Wind vectors move clouds via Semi-Lagrangian advection
- **Wind → Precipitation**: Wind drift affects rain/snow particle paths
- **Meteorology → Precipitation**: Humidity, temperature determine precipitation type
- **Precipitation → Water**: Accumulation feeds into rivers and ponds
- **Water → Wind**: Lake effect modifies local wind patterns
- **All → WeatherPhysicsManifold**: Aggregates all data for spatial queries and shader access

### Component Relationships

```
WeatherSystem (Main Controller)
├── Collects: WeatherEvent[]
├── Manages: Service update order
└── Coordinates:
    ├── Meteorology
    │   ├── Stages: WeatherEvent
    │   └── Controls: Cloud movement
    ├── Wind
    │   ├── Uses: PhysicsManifold
    │   └── Affects: Cloud, Precipitation, Physics objects
    ├── Precipitation
    │   ├── Uses: PhysicsManifold
    │   └── Feeds: Water system
    ├── Water
    │   ├── Manages: River, Pond, Dam
    │   └── Affects: Local Meteorology, Wind
    ├── Cloud
    │   ├── Uses: PhysicsManifold
    │   └── Receives: Meteorology, Wind data
    └── WeatherPhysicsManifold
        ├── Aggregates: All subsystem data
        ├── Uses: OctTree for spatial queries
        └── Provides: Shader access
```

### Integration Points

**Unity Physics Integration**:
- **Wind Forces**: `Wind.GetWindForce(Rigidbody)` applies forces to physics objects
- **OnWeatherForce Interface**: Objects implementing `IOnWeatherForce` receive custom force handling
- **Tornado Effects**: `WeatherEvent` with vortex type can create tornado-like wind patterns
- **Physical Weather Objects**: `PhysicalWeatherObject` components define material interactions and blocking

**Shader Integration**:
- **WeatherPhysicsManifold**: Exposes data via `GetShaderParameters()` for texture/buffer access
- **Compute Shaders**: Optional GPU acceleration for manifold updates (LBM method)
- **Water Rendering**: River and pond shaders read from manifold for flow visualization
- **Cloud Rendering**: Cloud shaders use manifold pressure/velocity data for volume effects

**Spatial Queries**:
- **OctTree**: Efficient blocking shape queries for `PhysicalWeatherObject` components
- **Material Lookup**: Per-cell material properties from blocking objects
- **Interior Spaces**: Separate manifold tracking for enclosed spaces

## Numerical Integration & Physics Methods

For real-time weather simulation, we employ state-of-the-art numerical integration methods adapted from operational meteorology and computational fluid dynamics. These methods ensure stable, accurate frame-by-frame state calculation for the `WeatherPhysicsManifold`.

### Integration Methods

#### Semi-Implicit Euler (Primary Method)
- **Use**: Default for `WeatherPhysicsManifold` velocity updates
- **Pros**: Fast, stable, energy-conserving for Hamiltonian systems
- **Cons**: Lower accuracy than RK4, but sufficient for real-time
- **Application**: Velocity field updates, pressure gradients, temperature diffusion

#### RK4 (Runge-Kutta 4th Order)
- **Use**: High-accuracy paths when needed (optional upgrade)
- **Pros**: High accuracy, stable for smooth flows, standard in operational weather models
- **Cons**: 4 evaluations per step (more expensive)
- **Application**: Critical wind field calculations, precise particle tracking

#### Verlet Integration
- **Use**: Precipitation particle physics (raindrops, snowflakes)
- **Pros**: Energy-conserving, symplectic, good for long-term stability
- **Cons**: Less accurate for rapidly changing forces
- **Application**: Individual precipitation particle tracking

#### Position-Based Dynamics (PBD)
- **Use**: Water surface simulation, cloud volume deformation
- **Pros**: Stable, controllable, good for fluids
- **Cons**: Less physically accurate, but visually convincing
- **Application**: `Water.cs` surface effects, `Cloud.cs` volume rendering

### Meteorological Techniques

#### Semi-Lagrangian Advection
- **What**: Track fluid parcels backward in time, interpolate from grid
- **Why**: Stable for large time steps, standard in operational National Weather Prediction (NWP) models // todo: double check NWP
- **Application**: Wind field advection in `Wind.cs`, cloud movement in `Cloud.cs`
- **Implementation**: Backtrace particles through velocity field, sample previous grid state

#### Operator Splitting
- **What**: Split advection, pressure, and diffusion into separate steps
- **Why**: Stable, allows different methods per physical process
- **Application**: `WeatherSystem.ServiceUpdate()` structure:
  1. Advection (semi-Lagrangian)
  2. Pressure projection (pressure-Poisson solver)
  3. Diffusion/viscosity (implicit)
  4. External forces (wind, gravity)

#### Finite Volume Method (FVM)
- **What**: Conservative discretization of conservation laws
- **Why**: Preserves mass/momentum, standard in CFD
- **Application**: Water flow in `River.cs` and `Pond.cs`, mass conservation

#### Lattice Boltzmann Method (LBM) - Optional GPU Acceleration
- **What**: Particle-based fluid dynamics on a lattice
- **Why**: GPU-friendly, handles complex boundaries well, can run at 60fps on modern GPUs
- **Application**: Compute shader implementation for `WeatherPhysicsManifold` updates
- **Note**: Consider for high-resolution simulations requiring GPU acceleration

### Frame-End State Calculation

The `WeatherSystem.ServiceUpdate()` follows operator splitting to determine final lattice state:

```
1. Meteorology (sets boundary conditions)
2. Wind (generates force field)
3. Advect all fields (velocity, temperature, pressure) - Semi-Lagrangian
4. Apply pressure gradients - Implicit pressure projection
5. Apply diffusion (temperature, moisture) - Implicit viscosity
6. Precipitation (uses manifold state)
7. Water (uses manifold + precipitation)
8. Cloud (visual representation of manifold)
```

Final state is stored in `WeatherPhysicsManifold`, ready for shader access and next frame's initial conditions.

## Physical Weather Objects & Spatial Blocking

For local weather effects (e.g., dew on aluminum shed, interior weather phenomena), we use a physical octree with blocking shapes to create microclimates and material-specific interactions.

### Physical Octree Blocking System

The `WeatherPhysicsManifold` uses an **OctTree** (similar to `SGOctTree` from BedogaGenerator) to manage spatial blocking and material interactions:

- **Blocking Shapes**: Colliders or custom meshes define solid geometry
- **Material Properties**: Each blocking shape has material type (aluminum, wood, glass, etc.)
- **Local Weather Effects**: Weather calculations respect blocking geometry
  - Wind shadowing behind objects
  - Dew formation on cold surfaces (aluminum sheds)
  - Precipitation accumulation on surfaces
  - Temperature gradients around objects
  - Pressure variations in enclosed spaces

**Key Features**:
- Efficient spatial queries via OctTree
- Material-specific weather interactions
- Procedural microclimate generation
- GPU-friendly blocking data structure

### PhysicalWeatherObject Component

A component that can be attached to any GameObject (e.g., a shed, building, tree) to define how it interacts with weather systems.

**Key Properties**:
- **Material Type**: Enum (Aluminum, Wood, Glass, Concrete, Metal, Fabric, etc.)
- **Thermal Properties**:
  - **Thermal Conductivity**: W/(m·K) - how quickly heat transfers
  - **Specific Heat**: J/(kg·K) - heat capacity
  - **Emissivity**: 0-1 - radiation properties
- **Surface Properties**:
  - **Roughness**: 0-1 - affects water accumulation, dew formation
  - **Absorptivity**: 0-1 - how much solar/thermal energy is absorbed
  - **Water Permeability**: 0-1 - how water penetrates (0 = waterproof, 1 = fully permeable)
- **Interior Weather**:
  - **HasInterior**: bool - whether object has enclosed space
  - **InteriorVolume**: m³ - volume of interior space
  - **Ventilation**: 0-1 - air exchange rate with exterior
  - **InteriorTemperature**: °C - separate temperature tracking
  - **InteriorHumidity**: % - separate humidity tracking
- **Blocking Shape**:
  - **UseCollider**: bool - use attached Collider as blocking shape
  - **CustomMesh**: Mesh - optional custom blocking mesh
  - **BlockingMode**: Enum (Full, Partial, WindOnly) - what weather effects are blocked

**Key Methods**:
- `GetMaterialType()` - Return material type
- `GetThermalProperties()` - Return thermal data
- `CalculateDewPoint()` - Compute dew formation based on surface temp/humidity
- `UpdateInteriorWeather(float deltaTime, WeatherPhysicsManifold manifold)` - Update interior conditions
- `GetBlockingBounds()` - Return OctTree-compatible bounds
- `GetWeatherInteraction(Vector3 position, WeatherType type)` - Get local weather modification

**Procedural Weather Phenomena**:
- **Dew Formation**: On cold surfaces (aluminum sheds) when humidity > dew point
- **Frost**: On surfaces below freezing with high humidity
- **Condensation**: On interior surfaces when interior humidity > exterior
- **Wind Shadowing**: Reduced wind speed behind blocking objects
- **Temperature Gradients**: Warmer/cooler zones around objects based on material
- **Precipitation Accumulation**: Water pooling on surfaces based on roughness
- **Interior Weather**: Separate atmospheric conditions inside enclosed spaces
  - Temperature lag (warmer in day, cooler at night)
  - Humidity accumulation from interior sources
  - Pressure equalization with exterior
  - Wind effects through ventilation

**Example Use Case**: A shed in a field
- Material: Aluminum (high thermal conductivity, low emissivity)
- HasInterior: true
- InteriorVolume: 10 m³
- Ventilation: 0.1 (small gaps)
- Result:
  - Dew forms on exterior aluminum surfaces at night
  - Interior temperature tracks exterior with lag
  - Interior humidity can accumulate if ventilation is low
  - Wind creates pressure differences between interior/exterior
  - Precipitation can pool on roof if roughness > 0

### Integration with WeatherPhysicsManifold

The OctTree blocking system integrates with `WeatherPhysicsManifold`:

1. **Spatial Queries**: OctTree efficiently finds blocking objects for any lattice cell
2. **Material Lookup**: Each blocking shape provides material properties
3. **Local Modifications**: Weather calculations respect blocking and material properties
4. **Interior Tracking**: Separate manifold data for interior spaces of `PhysicalWeatherObject`s

## Core Weather Subsystems

### Meteorology

Controls the cloud movements and stages weather events to happen so you can stage a storm, or a nice day with interesting wind all the same.

**Key Parameters**:
- **Temperature**: °C (Celsius)
- **Pressure**: hPa (Hectopascals / Millibars)
- **Humidity**: % (0-100)
- **Dew Point**: °C
- **Cloud Cover**: Oktas (0-8) or %

### Wind

Wind system that affects clouds, precipitation, and physics objects.

**Key Parameters**:
- **Speed**: m/s (meters per second) - horizontal wind speed
- **Direction**: degrees (0-360°, meteorological convention: direction wind comes FROM)
- **Gust Speed**: m/s (peak wind speed)
- **Altitude Levels**: Can vary by height (m above ground level)

**Integration**:
- Uses Semi-Lagrangian advection for wind field updates
- Affects: Cloud movement, Precipitation drift, Physics objects (via forces)
- Respects blocking shapes from `PhysicalWeatherObject` components

### Precipitation

Rain rendering and phase change events that might lower pressure or effect change over time.

**Key Parameters**:
- **Precipitation Rate**: mm/h (millimeters per hour)
- **Intensity**: Light (< 2.5 mm/h), Moderate (2.5-7.6 mm/h), Heavy (> 7.6 mm/h)
- **Type**: Rain, Snow, Sleet, Hail
- **Accumulation**: mm (total precipitation)

**Integration**:
- Uses Verlet integration for particle physics
- Affects: Water system (adds to ponds/rivers), Dam (overflow), PhysicalWeatherObject surfaces (accumulation)

### Water

Water system is for prebaking water effects and connecting ponds (may be arbitrarily large), rivers, and rains.

**Key Parameters**:
- **Water Level**: m (meters) or mm relative to reference
- **Flow Rate**: m³/s (cubic meters per second) for rivers
- **Volume**: m³ (cubic meters) for ponds

**Integration**:
- Uses Finite Volume Method for flow calculations
- Receives: Precipitation accumulation
- Manages: Pond, River, Dam components
- Affects: Wind (lake effect), Local weather

### River

Rivers contain RiverSplines and are mostly about having a way to geometrically organize a natural order.
River splines can be placed or generated procedurally.

**Key Parameters**:
- **Flow Rate**: m³/s (cubic meters per second)
- **Velocity**: m/s (meters per second)
- **Width**: m (meters)
- **Depth**: m (meters)

**Integration**:
- Uses Finite Volume Method for flow calculations
- Receives: Water from Precipitation, upstream Rivers
- Affects: Wind (local effects), Pond (feeds into)

### Pond/Lake

Ponds collect water and effect precipitation systems. They can have additional settings like artificial lake effect etc.

**Key Parameters**:
- **Water Level**: m (meters)
- **Volume**: m³ (cubic meters)
- **Surface Area**: m² (square meters)
- **Temperature**: °C (can affect local weather)

**Integration**:
- Receives: Water from Precipitation, Rivers
- Affects: Local Meteorology (temperature, humidity), Wind

### Dam

A planar or mesh prevention of water.

**Key Parameters**:
- **Height**: m (meters)
- **Water Level**: m (meters) above/below dam
- **Flow Rate**: m³/s (if water overflows)

**Integration**:
- Blocks: Water from Rivers, Ponds
- Affects: Downstream Rivers, Ponds

### Cloud

A cloud is a pressure system and is compatible with the PhysicsManifold - this should allow for some really nice, Myth-like explosive smoke, and geysers!!

**Key Parameters**:
- **Altitude**: m (meters) - base and top altitude
- **Pressure**: hPa (hectopascals)
- **Coverage**: Oktas (0-8) or %
- **Type**: Cumulus, Stratus, Cirrus, etc.
- **Density**: kg/m³ (affects visual rendering)

**Integration**:
- Uses Semi-Lagrangian advection for movement
- Uses Position-Based Dynamics for volume deformation
- Receives: Data from Meteorology, Wind
- Visual: Particle effects, shader-based rendering

## WeatherPhysicsManifold

Like the Wind data structure, a 3D matrix of velocities and mode coords in an oct tree for access from the weather shaders. Enhanced with physical blocking and material interactions.

### Structure

**Data Per Cell**:
- **Velocity**: Vector3 (m/s) - 3D velocity vector
- **Pressure**: float (hPa) - atmospheric pressure
- **Temperature**: float (°C) - air temperature
- **Density**: float (kg/m³) - air density
- **Mode**: Enum (Water, Rain, Cloud, Wind, Air) - material type per cell
- **Material Properties**: Optional material data from blocking objects

**Spatial Organization**:
- **OctTree Structure**: Hierarchical spatial partitioning for efficient queries
- **Cell Resolution**: Configurable grid resolution (e.g., 1m, 5m, 10m per cell)
- **World Bounds**: Defines the spatial extent of the simulation
- **Blocking Integration**: OctTree stores references to `PhysicalWeatherObject` blocking shapes
- **Material Lookup**: Per-cell material properties cached from blocking objects
- **Interior Spaces**: Separate manifold instances for enclosed spaces (tracked by `PhysicalWeatherObject`)

### Access Patterns

**Position-Based Queries**:
- `GetDataAtPosition(Vector3 position)` - Returns manifold data at world position
- `SetDataAtPosition(Vector3 position, ManifoldData data)` - Sets data at position
- `GetVelocityAtPosition(Vector3 position)` - Fast velocity lookup
- `GetPressureAtPosition(Vector3 position)` - Pressure at position
- `GetTemperatureAtPosition(Vector3 position)` - Temperature at position

**Spatial Queries**:
- `QueryBounds(Bounds bounds)` - Returns all cells within bounds
- `QuerySphere(Vector3 center, float radius)` - Returns cells in sphere
- `FindBlockingObjects(Vector3 position)` - Finds blocking shapes at position
- `GetMaterialAtPosition(Vector3 position)` - Returns material properties

**Shader Access**:
- `GetShaderParameters()` - Returns structured data for shader access
- **Texture Format**: 3D texture with RGBA channels (velocity.xyz, pressure.w)
- **Buffer Format**: Structured buffer with full cell data
- **Compute Shader**: Direct GPU updates via compute shader dispatch

**Update Scheme** (per frame, using operator splitting):
1. **Advection**: Semi-Lagrangian (stable, large timesteps)
   - Backtrace particles through velocity field
   - Interpolate from previous grid state
2. **Pressure Projection**: Implicit pressure-Poisson solver (conserves mass)
   - Solves for pressure field
   - Projects velocity to be divergence-free
3. **Diffusion**: Implicit viscosity (stable)
   - Temperature diffusion
   - Moisture diffusion
4. **Forces**: External forces (wind, gravity, blocking objects)
   - Apply wind forces from `Wind` system
   - Apply gravity
   - Apply forces from blocking objects
5. **Material Interactions**: Apply material-specific effects (dew, condensation, etc.)
   - Query blocking objects for material properties
   - Apply thermal effects
   - Apply surface interactions

**GPU Acceleration** (optional):
- **Compute Shaders**: Parallel updates for all cells
- **Lattice Boltzmann Method**: GPU-friendly fluid dynamics
- **Texture Reads**: Direct shader access via 3D textures
- **Structured Buffers**: High-performance data access

**Integration**:
- **Aggregates**: Wind, Cloud, Precipitation, Water data
- **Provides**: Data to shaders for rendering
- **Uses**: OctTree for efficient blocking queries
- **Supports**: Interior weather tracking for `PhysicalWeatherObject`s
- **Updates**: Called last in service update order to capture final state

## Data Integration

These units align with common weather data sources:
- **OpenWeatherMap API**: Uses metric units (Celsius, hPa, m/s, mm)
- **NOAA/NWS**: Mix of metric and imperial (convert as needed)
- **METAR/TAF**: Aviation format (Celsius, hPa, m/s or knots, meters)
- **ECMWF**: European (metric/SI units)
- **Weather.gov**: US National Weather Service format

All systems should provide conversion utilities for display purposes (e.g., °F for temperature, mph for wind speed, inches for precipitation) while maintaining internal storage in standard units.
