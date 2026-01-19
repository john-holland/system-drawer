---
name: Weather System Implementation
overview: Implement a complete weather system for Unity with physics integration, following the architecture in weather.md. Update weather.md where necessary, and implement all subsystems (Meteorology, Wind, Precipitation, Water, River, Pond, Dam, Cloud, WeatherPhysicsManifold) with proper service update order and data flow.
todos:
  - id: update_weather_md
    content: "Update weather.md: fix typos (Percipitation→Precipitation, Forcast→Forecast), add Service Update Order section, add Data Flow diagram, clarify WeatherPhysicsManifold structure"
    status: completed
  - id: implement_weather_system
    content: "Implement WeatherSystem.cs: main controller, event collection, service update orchestration"
    status: completed
  - id: implement_weather_event
    content: "Implement WeatherEvent.cs: base event system for pressure changes, lightning, etc."
    status: completed
  - id: implement_meteorology
    content: "Implement Meteorology.cs: atmospheric conditions, weather event staging, cloud control"
    status: completed
    dependencies:
      - implement_weather_system
  - id: implement_wind
    content: "Implement Wind.cs: wind field generation, altitude-based wind, physics force application"
    status: completed
    dependencies:
      - implement_weather_system
  - id: implement_precipitation
    content: "Implement Precipitation.cs (rename from Percipitation.cs): rain/snow rendering, accumulation tracking"
    status: completed
    dependencies:
      - implement_weather_system
  - id: implement_water
    content: "Implement Water.cs: water body management, height maps, flow calculations"
    status: completed
    dependencies:
      - implement_weather_system
  - id: implement_river
    content: "Implement River.cs and RiverSpline.cs: river geometry, flow calculations, spline management"
    status: completed
    dependencies:
      - implement_water
  - id: implement_pond
    content: "Implement Pond.cs: water collection, lake effect, volume management"
    status: completed
    dependencies:
      - implement_water
  - id: implement_dam
    content: "Implement Dam.cs: water blocking, overflow calculations"
    status: completed
    dependencies:
      - implement_water
  - id: implement_cloud
    content: "Implement Cloud.cs: visual representation, pressure system integration, meteorology linking"
    status: completed
    dependencies:
      - implement_meteorology
      - implement_wind
  - id: implement_physics_manifold
    content: "Implement PhysicsManifold.cs: base manifold system for fluid/gas representation"
    status: completed
  - id: implement_weather_physics_manifold
    content: "Implement WeatherPhysicsManifold.cs: 3D matrix of weather data, spatial tree organization, shader access"
    status: completed
    dependencies:
      - implement_physics_manifold
  - id: implement_supporting_components
    content: "Implement supporting components: PhysicsProperties, RainProperties, Forecast (rename Forcast), etc."
    status: completed
  - id: unity_physics_integration
    content: "Add Unity physics integration: wind forces on Rigidbodies, tornado effects, OnWeatherForce interface"
    status: completed
    dependencies:
      - implement_wind
  - id: shader_integration
    content: Create shader library and expose WeatherPhysicsManifold data to shaders for water/cloud rendering
    status: completed
    dependencies:
      - implement_weather_physics_manifold
---

# Weather System Implementation Plan

## Overview

Implement a complete weather system that integrates with Unity's physics system, allowing weather effects to interact with game objects. The system will follow the architecture defined in `weather.md`, with updates to clarify implementation details.

## Phase 1: Architecture Documentation & Core Infrastructure

### 1.1 Update weather.md

**File**: `Assets/Weather/weather.md`

- Fix typo: "Percipitation" → "Precipitation" (throughout document)
- Fix typo: "Forcast" → "Forecast" 
- Add "Service Update Order" section documenting the execution sequence
- Add "Data Flow" section showing how data moves between systems
- Add "Component Relationships" diagram
- Clarify WeatherPhysicsManifold structure and access patterns
- Add "Integration Points" section for Unity physics interaction

### 1.2 Core Infrastructure Components

#### WeatherSystem.cs (Main Controller)

**File**: `Assets/Weather/WeatherSystem.cs`

**Responsibilities**:

- Collect all `WeatherEvent` objects in scene
- Execute service updates in correct order
- Manage global weather state
- Coordinate between subsystems

**Key Methods**:

- `Update()` - Main update loop
- `CollectWeatherEvents()` - Find all WeatherEvent components
- `ServiceUpdate()` - Execute subsystem updates in order
- `GetCurrentWeatherState()` - Return current weather parameters

**Service Update Order**:

1. Meteorology (stages weather events, controls cloud movements)
2. Wind (affects clouds, precipitation, physics objects)
3. Precipitation (rain rendering, phase changes)
4. Water (ponds, rivers, water level management)

- Dam (water blocking/overflow)
- River (flow calculations)

5. Cloud (visual representation, pressure systems)
6. WeatherPhysicsManifold (final state for shaders)

#### WeatherEvent.cs (Event System)

**File**: `Assets/Weather/WeatherEvent.cs`

**Responsibilities**:

- Base class for weather events (pressure changes, lightning, etc.)
- Can be additive or multiplicative
- Provides event data to WeatherSystem

**Key Properties**:

- `eventType` (enum: PressureChange, Lightning, TemperatureChange, etc.)
- `magnitude` (float)
- `duration` (float)
- `isAdditive` (bool)
- `affectsSystems` (list of system types)

## Phase 2: Core Weather Subsystems

### 2.1 Meteorology System

**File**: `Assets/Weather/Meteorology.cs`

**Responsibilities**:

- Control cloud movements and weather event staging
- Manage atmospheric conditions
- Stage storms or nice days

**Key Properties** (from weather.md):

- `temperature` (float, °C)
- `pressure` (float, hPa)
- `humidity` (float, 0-100%)
- `dewPoint` (float, °C)
- `cloudCover` (float, 0-8 oktas or 0-100%)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update atmospheric conditions
- `StageWeatherEvent(WeatherEventType type, float magnitude)` - Queue weather events
- `GetAtmosphericConditions()` - Return current conditions
- `CalculateDewPoint()` - Compute dew point from temp/humidity

**Integration**:

- Affects: Cloud movement, Precipitation probability, Wind patterns

### 2.2 Wind System

**File**: `Assets/Weather/Wind.cs`

**Responsibilities**:

- Generate wind field vectors
- Affect clouds, precipitation, and physics objects
- Support altitude-based wind variation

**Key Properties**:

- `speed` (float, m/s) - horizontal wind speed
- `direction` (float, 0-360°) - direction wind comes FROM
- `gustSpeed` (float, m/s) - peak wind speed
- `altitudeLevels` (Dictionary<float, WindData>) - wind by altitude (m AGL)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update wind field
- `GetWindAtPosition(Vector3 position, float altitude)` - Get wind vector at location
- `GetWindForce(Rigidbody rb)` - Calculate force to apply to physics object
- `GenerateWindField()` - Create/manage PhysicsManifold for wind

**Integration**:

- Uses: PhysicsManifold for wind field storage
- Affects: Cloud movement, Precipitation drift, Physics objects (via forces)

### 2.3 Precipitation System

**File**: `Assets/Weather/Precipitation.cs` (rename from Percipitation.cs)

**Responsibilities**:

- Render rain/snow/sleet/hail
- Manage precipitation phase changes
- Track accumulation

**Key Properties**:

- `precipitationRate` (float, mm/h)
- `intensity` (enum: Light, Moderate, Heavy)
- `type` (enum: Rain, Snow, Sleet, Hail)
- `accumulation` (float, mm) - total precipitation
- `linkToPhysicsManifold` (PhysicsManifold reference)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update precipitation
- `CalculateIntensity()` - Determine intensity from rate
- `UpdateAccumulation(float deltaTime)` - Track total precipitation
- `GetParticleSystemParameters()` - Return params for visual effects

**Integration**:

- Uses: PhysicsManifold for particle effects
- Affects: Water system (adds to ponds/rivers), Dam (overflow)

### 2.4 Water System

**File**: `Assets/Weather/Water.cs`

**Responsibilities**:

- Manage water bodies and height maps
- Prebake physics interactions
- Connect ponds, rivers, and rain

**Key Properties**:

- `waterLevel` (float, m or mm relative to reference)
- `flowRate` (float, m³/s) - for rivers
- `volume` (float, m³) - for ponds
- `waterModes` (enum flags: Wave, Spray, Rush, WhiteWater, Still, Rippling)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update water state
- `AddWater(float volume)` - Add water from precipitation
- `GetWaterLevelAt(Vector3 position)` - Get height map value
- `CalculateFlow()` - Compute flow rates

**Integration**:

- Receives: Precipitation accumulation
- Manages: Pond, River, Dam components
- Affects: Wind (lake effect), Local weather

## Phase 3: Water Subsystems

### 3.1 River System

**File**: `Assets/Weather/River.cs`

**Responsibilities**:

- Manage river geometry via RiverSplines
- Calculate flow rates and velocities
- Support procedural or placed splines
- Editor UI custom GUI for "press button, create box, manually refresh splines"

**Key Properties**:

- `flowRate` (float, m³/s)
- `velocity` (float, m/s)
- `width` (float, m)
- `depth` (float, m)
- `riverSplines` (List<RiverSpline>)
- `materials` (Material[]) - for art
- `windEffectOverride` (optional Wind override)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update river flow
- `AddWater(float volume)` - Add water from precipitation/upstream
- `CalculateFlowRate()` - Compute flow from depth/width/velocity
- `GetSplineAtDistance(float distance)` - Get spline data
- Support Octtree with super fast sphere physics data for shader coord access and procedural water flow shaders
    - could prebake this with timed conical measures with combo of RNG release and measured pace of physical flow

**Integration**:

- Uses: RiverSpline for geometry
- Receives: Water from Precipitation, upstream Rivers
- Affects: Wind (local effects), Pond (feeds into)

#### RiverSpline.cs

**File**: `Assets/Weather/RiverSpline.cs`

**Responsibilities**:

- Define river path geometry
- Support spline painting for shader fitting
- Segmentation representated by nested gameobjects for by hand tweaking
    - editor GUI should manage this

**Key Properties**:

- `splinePoints` (List<Vector3>)
- `width` (float, m)
- `depth` (float, m)
- `flowDirection` (Vector3)

**Key Methods**:

- `GetPositionAtDistance(float distance)` - Get world position
- `GetFlowDirectionAtDistance(float distance)` - Get flow vector
- `GetWidthAtDistance(float distance)` - Get width

### 3.2 Pond/Lake System

**File**: `Assets/Weather/Pond.cs`

**Responsibilities**:

- Collect water from precipitation and rivers
- Manage lake effect on local weather
- Track volume and surface area

**Key Properties**:

- `waterLevel` (float, m)
- `volume` (float, m³)
- `surfaceArea` (float, m²)
- `temperature` (float, °C) - affects local weather
- `lakeEffectEnabled` (bool)
- `naturalEntropy` (float) - for procedural effects

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update pond state
- `AddWater(float volume)` - Add from precipitation/rivers
- `CalculateSurfaceArea()` - Compute from geometry
- `GetLakeEffect()` - Return local weather modification

**Integration**:

- Receives: Water from Precipitation, Rivers
- Affects: Local Meteorology (temperature, humidity), Wind

### 3.3 Dam System

**File**: `Assets/Weather/Dam.cs`

**Responsibilities**:

- Block water with configurable plane/mesh
- Calculate overflow rates
- Manage water level above/below dam

**Key Properties**:

- `height` (float, m)
- `waterLevel` (float, m) - above/below dam
- `flowRate` (float, m³/s) - if overflowing
- `damPlane` (Plane or Mesh) - blocking geometry

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update dam state
- `CheckOverflow()` - Determine if water exceeds dam height
- `CalculateOverflowRate()` - Compute flow if overflowing
- `GetWaterLevelDifference()` - Get level above/below dam

**Integration**:

- Blocks: Water from Rivers, Ponds
- Affects: Downstream Rivers, Ponds

## Phase 4: Visual & Physics Integration

### 4.1 Cloud System

**File**: `Assets/Weather/Cloud.cs`

**Responsibilities**:

- Visual representation of clouds
- Pressure system integration
- Compatible with PhysicsManifold for effects

**Key Properties**:

- `altitude` (Vector2, m) - base and top altitude
- `pressure` (float, hPa)
- `coverage` (float, 0-8 oktas or 0-100%)
- `type` (enum: Cumulus, Stratus, Cirrus, etc.)
- `density` (float, kg/m³)
- `isManagedByMeteorology` (bool)
- `physicsManifold` (PhysicsManifold reference)

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update cloud state
- `UpdateFromMeteorology(Meteorology meteorology)` - Apply meteorology data
- `GetVisualParameters()` - Return rendering params
- `ApplyWind(Wind wind)` - Move cloud based on wind

**Integration**:

- Uses: PhysicsManifold for pressure/velocity data
- Receives: Data from Meteorology, Wind
- Visual: Particle effects, shader-based rendering

### 4.2 WeatherPhysicsManifold

**File**: `Assets/Weather/WeatherPhysicsManifold.cs`

**Responsibilities**:

- 3D matrix of velocities and mode coordinates
- Includes water, rain, cloud, and wind data
- Accessible from weather shaders
- Organized in spatial tree (QuadTree/OctTree)

**Key Properties**:

- `velocity` (Vector3, m/s) - 3D vector per cell
- `pressure` (float, hPa) - per cell
- `temperature` (float, °C) - per cell
- `density` (float, kg/m³) - air density per cell
- `mode` (enum) - Water, Rain, Cloud, Wind, Air
- `spatialTree` (QuadTree/OctTree) - for efficient access

**Key Methods**:

- `ServiceUpdate(float deltaTime)` - Update manifold state
- `GetDataAtPosition(Vector3 position)` - Get manifold data
- `SetDataAtPosition(Vector3 position, ManifoldData data)` - Set data
- `GetShaderParameters()` - Return data for shader access

**Integration**:

- Aggregates: Wind, Cloud, Precipitation, Water data
- Provides: Data to shaders for rendering
- Uses: Spatial tree for efficient queries

### 4.3 PhysicsManifold (Base)

**File**: `Assets/Weather/PhysicsManifold.cs`

**Responsibilities**:

- Generic connector for physics system
- Fluid/gaseous manifold representation
- Force-directed graph or spatial tree

**Key Properties**:

- `material` (enum or Material reference)
- `state` (enum: Gas, Liquid, Solid, Plasma)
- `pressure` (float, Pa or hPa)
- `temperature` (float, °C)
- `velocity` (Vector3, m/s)
- `spatialTree` (QuadTree/OctTree)

**Key Methods**:

- `GetStateAtPosition(Vector3 position)` - Get material/state
- `GetPressureAtPosition(Vector3 position)` - Get pressure
- `GetVelocityAtPosition(Vector3 position)` - Get velocity
- `ApplyForce(Vector3 position, Vector3 force)` - Apply physics force

## Phase 5: Supporting Components

### 5.1 PhysicsProperties.cs

**File**: `Assets/Weather/PhysicsProperties.cs`

**Responsibilities**:

- Define material properties for physics calculations
- Air, water, cloud density, viscosity, etc.

**Key Properties**:

- `density` (float, kg/m³)
- `viscosity` (float, Pa·s)
- `specificHeat` (float, J/(kg·K))
- `thermalConductivity` (float, W/(m·K))

### 5.2 RainProperties.cs

**File**: `Assets/Weather/RainProperties.cs`

**Responsibilities**:

- Properties specific to rain rendering
- Particle system parameters

**Key Properties**:

- `dropletSize` (float, mm)
- `fallSpeed` (float, m/s)
- `windDrift` (float) - how much wind affects rain

### 5.3 WaterPhysicsApproximationSphere.cs

**File**: `Assets/Weather/WaterPhysicsApproximationSphere.cs`

**Responsibilities**:

- Approximate water physics with spheres
- For performance when full simulation isn't needed
- Binning with level of detail (LOD) access or modulating appopriately could be very helpful
    - limits on bin sizes for physics response

### 5.4 WeatherLandMass.cs

**File**: `Assets/Weather/WeatherLandMass.cs`

**Responsibilities**:

- Define land geometry for weather calculations
- Height maps, terrain data

### 5.5 Forecast.cs (rename from Forcast.cs)

**File**: `Assets/Weather/Forecast.cs`

**Responsibilities**:

- Weather forecast data structure
- Timeline of predicted weather events

## Phase 6: Unity Integration

### 6.1 Physics Integration

- Add `OnWeatherForce` interface for objects affected by wind
- Apply wind forces to Rigidbodies
- Support for tornado effects on physics objects
    - perhaps with a WeatherEvent that introduces a vortex of Vector3 impulses of wind?
    - it's possible twisters could occur naturally in our physics wind engine with the WeatherSystem solver... :3
    - it'll be interesting to see which cloud formations we can get to render naturally...!

### 6.2 Shader Integration

- Create shader library for weather effects
- Expose WeatherPhysicsManifold data to shaders
- Support for water rendering (rivers, ponds)
- Cloud rendering shaders

### 6.3 Editor Tools

- Inspector customizations for weather components
- Visualization gizmos for wind fields, water levels
- Forecast timeline editor

## Implementation Order

1. **Core Infrastructure**: WeatherSystem, WeatherEvent
2. **Atmospheric Systems**: Meteorology, Wind
3. **Precipitation**: Precipitation system
4. **Water Management**: Water, River, Pond, Dam
5. **Visual Systems**: Cloud, WeatherPhysicsManifold
6. **Physics Integration**: PhysicsManifold, Unity physics hooks
7. **Supporting Components**: Helper classes, properties
8. **Polish**: Editor tools, shaders, optimization

## Technical Considerations

- **Performance**: Use spatial trees (QuadTree/OctTree) for efficient queries
- **Units**: Maintain standard units internally, provide conversion utilities
- **Update Order**: Critical - must follow service update order
- **Data Flow**: Clear interfaces between systems
    - generic data side of house open for things like "dirt in the air", or "spells!" etc
- **Extensibility**: Allow custom weather events and effects