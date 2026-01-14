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
 
### Meteorology

Controls the cloud movements and stages weather events to happen so you can stage a storm, or a nice day with interesting wind all the same.

**Key Parameters**:
- **Temperature**: °C (Celsius)
- **Pressure**: hPa (Hectopascals / Millibars)
- **Humidity**: % (0-100)
- **Dew Point**: °C
- **Cloud Cover**: Oktas (0-8) or %

### Precipitation

Rain rendering and phase change events that might lower pressure or effect change over time.

**Key Parameters**:
- **Precipitation Rate**: mm/h (millimeters per hour)
- **Intensity**: Light (< 2.5 mm/h), Moderate (2.5-7.6 mm/h), Heavy (> 7.6 mm/h)
- **Type**: Rain, Snow, Sleet, Hail
- **Accumulation**: mm (total precipitation)

### Water

Water system is for prebaking water effects and connecting ponds (may be arbitrarily large), rivers, and rains.

**Key Parameters**:
- **Water Level**: m (meters) or mm relative to reference
- **Flow Rate**: m³/s (cubic meters per second) for rivers
- **Volume**: m³ (cubic meters) for ponds

### River

Rivers contain RiverSplines and are mostly about having a way to geometrically organize a natural order.
River splines can be placed or generated procedurally.

**Key Parameters**:
- **Flow Rate**: m³/s (cubic meters per second)
- **Velocity**: m/s (meters per second)
- **Width**: m (meters)
- **Depth**: m (meters)

### Pond/Lake

Ponds collect water and effect precipitation systems. They can have additional settings like artificial lake effect etc.

**Key Parameters**:
- **Water Level**: m (meters)
- **Volume**: m³ (cubic meters)
- **Surface Area**: m² (square meters)
- **Temperature**: °C (can affect local weather)

### Dam

A planar or mesh prevention of water.

**Key Parameters**:
- **Height**: m (meters)
- **Water Level**: m (meters) above/below dam
- **Flow Rate**: m³/s (if water overflows)

### Wind

Wind system that affects clouds, precipitation, and physics objects.

**Key Parameters**:
- **Speed**: m/s (meters per second) - horizontal wind speed
- **Direction**: degrees (0-360°, meteorological convention: direction wind comes FROM)
- **Gust Speed**: m/s (peak wind speed)
- **Altitude Levels**: Can vary by height (m above ground level)

### Cloud

A cloud is a pressure system and is compatible with the PhysicsManifold - this should allow for some really nice, Myth-like explosive smoke, and geysers!!

**Key Parameters**:
- **Altitude**: m (meters) - base and top altitude
- **Pressure**: hPa (hectopascals)
- **Coverage**: Oktas (0-8) or %
- **Type**: Cumulus, Stratus, Cirrus, etc.
- **Density**: kg/m³ (affects visual rendering)

### WeatherPhysicsManifold

Like the Wind data structure, a 3D matrix of velocities and mode coords in a quad or oct tree for access from the weather shaders.

**Key Parameters**:
- **Velocity**: m/s (meters per second) - 3D vector
- **Pressure**: hPa (hectopascals)
- **Temperature**: °C (Celsius)
- **Density**: kg/m³ (air density)

## Data Integration

These units align with common weather data sources:
- **OpenWeatherMap API**: Uses metric units (Celsius, hPa, m/s, mm)
- **NOAA/NWS**: Mix of metric and imperial (convert as needed)
- **METAR/TAF**: Aviation format (Celsius, hPa, m/s or knots, meters)
- **ECMWF**: European (metric/SI units)
- **Weather.gov**: US National Weather Service format

All systems should provide conversion utilities for display purposes (e.g., °F for temperature, mph for wind speed, inches for precipitation) while maintaining internal storage in standard units.