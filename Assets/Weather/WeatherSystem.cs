using System.Collections.Generic;
using UnityEngine;
using Weather.Executor;

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

        [Header("Egg LOD")]
        [Tooltip("Delegate advection to player egg zones via WeatherExecutorService.")]
        public bool useEggLodManifolds = true;

        public WeatherExecutorService weatherExecutor;

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

            if (weatherExecutor == null)
                weatherExecutor = GetComponent<WeatherExecutorService>() ?? FindAnyObjectByType<WeatherExecutorService>();
            if (weatherExecutor != null && weatherExecutor.weatherSystem == null)
                weatherExecutor.weatherSystem = this;
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

            // Refresh registry-backed events only when empty (scene-placed events self-register).
            if (weatherEvents.Count == 0)
                CollectWeatherEvents();

            if (useEggLodManifolds && weatherExecutor != null)
            {
                weatherExecutor.TickClient(Time.deltaTime);
            }
            else
            {
                ServiceUpdate(Time.deltaTime);
            }

            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Find all weather subsystems in the scene
        /// </summary>
        private void FindSubsystems()
        {
            if (meteorology == null)
                meteorology = FindFirstObjectByType<Meteorology>();

            if (wind == null)
                wind = FindFirstObjectByType<Wind>();

            if (precipitation == null)
                precipitation = FindFirstObjectByType<Precipitation>();

            if (water == null)
                water = FindFirstObjectByType<Water>();

            if (cloud == null)
                cloud = FindFirstObjectByType<Cloud>();

            if (weatherPhysicsManifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherPhysicsManifold);

            if (weatherExecutor == null)
                weatherExecutor = GetComponent<WeatherExecutorService>() ?? FindAnyObjectByType<WeatherExecutorService>();

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
            using (PerfTrace.Scope("CollectWeatherEvents"))
            {
                weatherEvents.Clear();
                WeatherEventRegistry.CopyTo(weatherEvents);
                if (weatherEvents.Count == 0)
                    weatherEvents.AddRange(FindObjectsByType<WeatherEvent>(FindObjectsSortMode.None));

                if (debugLogging)
                {
                    Debug.Log($"[WeatherSystem] Collected {weatherEvents.Count} weather events");
                }
            }
        }

        public void RegisterWeatherEvent(WeatherEvent weatherEvent)
        {
            if (weatherEvent == null || weatherEvents.Contains(weatherEvent))
                return;
            weatherEvents.Add(weatherEvent);
        }

        /// <summary>
        /// Execute service updates in the correct order
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            ServiceUpdateSubsystems(deltaTime, skipManifold: false);
        }

        public void ServiceUpdateSubsystems(float deltaTime, bool skipManifold)
        {
            using (PerfTrace.Scope("WeatherSystem.ServiceUpdate"))
            {
                ProcessWeatherEvents();

                if (meteorology != null)
                    meteorology.ServiceUpdate(deltaTime);

                if (wind != null)
                    wind.ServiceUpdate(deltaTime);

                if (precipitation != null)
                    precipitation.ServiceUpdate(deltaTime);

                if (water != null)
                    water.ServiceUpdate(deltaTime);

                if (cloud != null)
                    cloud.ServiceUpdate(deltaTime);

                if (!skipManifold && weatherPhysicsManifold != null)
                    weatherPhysicsManifold.ServiceUpdate(deltaTime);

                UpdateWeatherState();
            }
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

        public bool TryQueryWeatherAt(Vector3 world, out ManifoldCellData data)
        {
            if (weatherExecutor != null && weatherExecutor.TryQueryWeatherAt(world, out data))
                return true;

            if (weatherPhysicsManifold != null)
            {
                data = weatherPhysicsManifold.GetDataAtPosition(world);
                return true;
            }

            data = default;
            return false;
        }
    }
}
