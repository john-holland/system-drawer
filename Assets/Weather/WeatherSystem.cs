using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Main weather system controller that orchestrates all weather subsystems.
    /// Collects WeatherEvent objects and executes service updates in the correct order.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Subsystem References")]
        [Tooltip("Meteorology subsystem")]
        public Meteorology meteorology;

        [Tooltip("Wind subsystem")]
        public Wind wind;

        [Tooltip("Precipitation subsystem")]
        public Precipitation precipitation;

        [Tooltip("Water subsystem")]
        public Water water;

        [Tooltip("Cloud subsystem")]
        public Cloud cloud;

        [Tooltip("WeatherPhysicsManifold subsystem")]
        public WeatherPhysicsManifold weatherPhysicsManifold;

        [Header("Configuration")]
        [Tooltip("Auto-find subsystems if not assigned")]
        public bool autoFindSubsystems = true;

        [Tooltip("Update rate (0 = every frame)")]
        public float updateInterval = 0f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool debugLogging = false;

        // Collected weather events
        private List<WeatherEvent> weatherEvents = new List<WeatherEvent>();

        // Update timing
        private float lastUpdateTime = 0f;

        // Current weather state
        private WeatherState currentWeatherState;

        /// <summary>
        /// Current weather state structure
        /// </summary>
        public struct WeatherState
        {
            public float temperature;
            public float pressure;
            public float humidity;
            public float windSpeed;
            public float windDirection;
            public float precipitationRate;
        }

        private void Awake()
        {
            if (autoFindSubsystems)
            {
                FindSubsystems();
            }
        }

        private void Start()
        {
            CollectWeatherEvents();
        }

        private void Update()
        {
            // Check if we should update this frame
            if (updateInterval > 0f)
            {
                if (Time.time - lastUpdateTime < updateInterval)
                    return;
            }

            // Collect events (in case new ones were added)
            CollectWeatherEvents();

            // Execute service update
            ServiceUpdate(Time.deltaTime);

            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Find all weather subsystems in the scene
        /// </summary>
        private void FindSubsystems()
        {
            if (meteorology == null)
                meteorology = FindObjectOfType<Meteorology>();

            if (wind == null)
                wind = FindObjectOfType<Wind>();

            if (precipitation == null)
                precipitation = FindObjectOfType<Precipitation>();

            if (water == null)
                water = FindObjectOfType<Water>();

            if (cloud == null)
                cloud = FindObjectOfType<Cloud>();

            if (weatherPhysicsManifold == null)
                weatherPhysicsManifold = FindObjectOfType<WeatherPhysicsManifold>();

            if (debugLogging)
            {
                Debug.Log($"[WeatherSystem] Found subsystems: " +
                    $"Meteorology={meteorology != null}, " +
                    $"Wind={wind != null}, " +
                    $"Precipitation={precipitation != null}, " +
                    $"Water={water != null}, " +
                    $"Cloud={cloud != null}, " +
                    $"WeatherPhysicsManifold={weatherPhysicsManifold != null}");
            }
        }

        /// <summary>
        /// Collect all WeatherEvent objects in the scene
        /// </summary>
        public void CollectWeatherEvents()
        {
            weatherEvents.Clear();
            weatherEvents.AddRange(FindObjectsOfType<WeatherEvent>());

            if (debugLogging)
            {
                Debug.Log($"[WeatherSystem] Collected {weatherEvents.Count} weather events");
            }
        }

        /// <summary>
        /// Execute service updates in the correct order
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Process weather events first
            ProcessWeatherEvents();

            // Service update order (as specified in weather.md):
            // 1. Meteorology (sets boundary conditions, stages weather events, controls cloud movements)
            if (meteorology != null)
            {
                meteorology.ServiceUpdate(deltaTime);
            }

            // 2. Wind (generates wind field vectors, affects clouds, precipitation, physics objects)
            if (wind != null)
            {
                wind.ServiceUpdate(deltaTime);
            }

            // 3. Precipitation (rain/snow rendering, phase changes, accumulation tracking)
            if (precipitation != null)
            {
                precipitation.ServiceUpdate(deltaTime);
            }

            // 4. Water (water body management, height maps, flow calculations)
            if (water != null)
            {
                water.ServiceUpdate(deltaTime);
            }

            // 5. Cloud (visual representation, pressure system integration, meteorology linking)
            if (cloud != null)
            {
                cloud.ServiceUpdate(deltaTime);
            }

            // 6. WeatherPhysicsManifold (final state aggregation for shaders, spatial tree updates)
            if (weatherPhysicsManifold != null)
            {
                weatherPhysicsManifold.ServiceUpdate(deltaTime);
            }

            // Update current weather state
            UpdateWeatherState();
        }

        /// <summary>
        /// Process all collected weather events
        /// </summary>
        private void ProcessWeatherEvents()
        {
            foreach (var weatherEvent in weatherEvents)
            {
                if (weatherEvent == null || !weatherEvent.isActiveAndEnabled)
                    continue;

                var eventData = weatherEvent.GetEventData();
                if (eventData.magnitude == 0f)
                    continue;

                // Apply event to affected systems
                ApplyWeatherEvent(eventData, weatherEvent);
            }
        }

        /// <summary>
        /// Apply a weather event to the appropriate subsystems
        /// </summary>
        private void ApplyWeatherEvent(WeatherEventData eventData, WeatherEvent weatherEvent)
        {
            // Check which systems this event affects
            bool affectsMeteorology = weatherEvent.AffectsSystem(AffectedSystem.Meteorology);
            bool affectsWind = weatherEvent.AffectsSystem(AffectedSystem.Wind);
            bool affectsPrecipitation = weatherEvent.AffectsSystem(AffectedSystem.Precipitation);
            bool affectsCloud = weatherEvent.AffectsSystem(AffectedSystem.Cloud);

            switch (eventData.eventType)
            {
                case WeatherEventType.PressureChange:
                    if (meteorology != null && affectsMeteorology)
                    {
                        if (eventData.isAdditive)
                            meteorology.pressure += eventData.magnitude;
                        else
                            meteorology.pressure *= eventData.magnitude;
                    }
                    break;

                case WeatherEventType.TemperatureChange:
                    if (meteorology != null && affectsMeteorology)
                    {
                        if (eventData.isAdditive)
                            meteorology.temperature += eventData.magnitude;
                        else
                            meteorology.temperature *= eventData.magnitude;
                    }
                    break;

                case WeatherEventType.WindGust:
                case WeatherEventType.Tornado:
                    if (wind != null && affectsWind)
                    {
                        wind.ApplyWeatherEvent(eventData);
                    }
                    break;

                case WeatherEventType.PrecipitationChange:
                    if (precipitation != null && affectsPrecipitation)
                    {
                        if (eventData.isAdditive)
                            precipitation.precipitationRate += eventData.magnitude;
                        else
                            precipitation.precipitationRate *= eventData.magnitude;
                    }
                    break;

                case WeatherEventType.HumidityChange:
                    if (meteorology != null && affectsMeteorology)
                    {
                        if (eventData.isAdditive)
                            meteorology.humidity += eventData.magnitude;
                        else
                            meteorology.humidity *= eventData.magnitude;
                        meteorology.humidity = Mathf.Clamp(meteorology.humidity, 0f, 100f);
                    }
                    break;

                case WeatherEventType.CloudFormation:
                    if (cloud != null && affectsCloud)
                    {
                        cloud.ApplyWeatherEvent(eventData);
                    }
                    break;
            }
        }

        /// <summary>
        /// Update current weather state from subsystems
        /// </summary>
        private void UpdateWeatherState()
        {
            currentWeatherState = new WeatherState
            {
                temperature = meteorology != null ? meteorology.temperature : 20f,
                pressure = meteorology != null ? meteorology.pressure : 1013.25f,
                humidity = meteorology != null ? meteorology.humidity : 50f,
                windSpeed = wind != null ? wind.speed : 0f,
                windDirection = wind != null ? wind.direction : 0f,
                precipitationRate = precipitation != null ? precipitation.precipitationRate : 0f
            };
        }

        /// <summary>
        /// Get current weather state
        /// </summary>
        public WeatherState GetCurrentWeatherState()
        {
            return currentWeatherState;
        }

        /// <summary>
        /// Get all active weather events
        /// </summary>
        public List<WeatherEvent> GetWeatherEvents()
        {
            return new List<WeatherEvent>(weatherEvents);
        }
    }
}
