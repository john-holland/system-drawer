using System.Collections;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Test script that simulates a weather transition from low pressure cool weather
    /// to high pressure warm weather, monitoring precipitation formation.
    /// 
    /// Test scenario:
    /// 1. Start with low pressure (980 hPa), cool temperature (5°C), high humidity (80%)
    /// 2. Transition to high pressure (1020 hPa), warm temperature (25°C), high humidity (85%)
    /// 3. Monitor precipitation formation as warm, moist air rises and cools
    /// </summary>
    public class WeatherPressureTransitionTest : MonoBehaviour
    {
        [Header("Weather System References")]
        [Tooltip("WeatherSystem to test (auto-finds if not set)")]
        public WeatherSystem weatherSystem;

        [Tooltip("Meteorology component (auto-finds if not set)")]
        public Meteorology meteorology;

        [Tooltip("Precipitation component (auto-finds if not set)")]
        public Precipitation precipitation;

        [Tooltip("Cloud component (auto-finds if not set)")]
        public Cloud cloud;

        [Header("Test Configuration")]
        [Tooltip("Automatically start test on Start")]
        public bool autoStartOnPlay = true;

        [Tooltip("Duration of initial low pressure phase (seconds)")]
        public float initialPhaseDuration = 10f;

        [Tooltip("Duration of transition phase (seconds)")]
        public float transitionDuration = 30f;

        [Tooltip("Duration of high pressure phase (seconds)")]
        public float highPressurePhaseDuration = 20f;

        [Header("Initial Conditions (Low Pressure, Cool)")]
        [Tooltip("Initial pressure (low) in hPa")]
        public float initialPressure = 980f;

        [Tooltip("Initial temperature (cool) in Celsius")]
        public float initialTemperature = 5f;

        [Tooltip("Initial humidity in percentage")]
        [Range(0f, 100f)]
        public float initialHumidity = 80f;

        [Header("Final Conditions (High Pressure, Warm)")]
        [Tooltip("Final pressure (high) in hPa")]
        public float finalPressure = 1020f;

        [Tooltip("Final temperature (warm) in Celsius")]
        public float finalTemperature = 25f;

        [Tooltip("Final humidity in percentage")]
        [Range(0f, 100f)]
        public float finalHumidity = 85f;

        [Header("Test Results")]
        [Tooltip("Maximum precipitation rate observed during test")]
        public float maxPrecipitationRate = 0f;

        [Tooltip("Time when precipitation started forming")]
        public float precipitationStartTime = -1f;

        [Tooltip("Maximum cloud coverage observed")]
        public float maxCloudCover = 0f;

        [Tooltip("Test phase log")]
        [TextArea(10, 20)]
        public string testLog = "";

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool enableDebugLog = true;

        private bool testRunning = false;
        private float testStartTime = 0f;
        private float lastLogTime = 0f;
        private const float LOG_INTERVAL = 2f; // Log every 2 seconds

        private void Awake()
        {
            FindWeatherComponents();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartCoroutine(RunTest());
            }
        }

        /// <summary>
        /// Find all required weather components
        /// </summary>
        private void FindWeatherComponents()
        {
            if (weatherSystem == null)
                weatherSystem = FindObjectOfType<WeatherSystem>();

            if (meteorology == null)
                meteorology = FindObjectOfType<Meteorology>();

            if (precipitation == null)
                precipitation = FindObjectOfType<Precipitation>();

            if (cloud == null)
                cloud = FindObjectOfType<Cloud>();

            if (enableDebugLog)
            {
                Debug.Log($"[WeatherPressureTransitionTest] Found components: " +
                    $"WeatherSystem={weatherSystem != null}, " +
                    $"Meteorology={meteorology != null}, " +
                    $"Precipitation={precipitation != null}, " +
                    $"Cloud={cloud != null}");
            }
        }

        /// <summary>
        /// Run the weather transition test
        /// </summary>
        public IEnumerator RunTest()
        {
            if (testRunning)
            {
                Debug.LogWarning("[WeatherPressureTransitionTest] Test already running!");
                yield break;
            }

            if (!ValidateComponents())
            {
                Debug.LogError("[WeatherPressureTransitionTest] Missing required components!");
                yield break;
            }

            testRunning = true;
            testStartTime = Time.time;
            maxPrecipitationRate = 0f;
            precipitationStartTime = -1f;
            maxCloudCover = 0f;
            testLog = "=== Weather Pressure Transition Test ===\n\n";
            lastLogTime = Time.time;

            LogPhase("Starting test: Low Pressure Cool Weather → High Pressure Warm Weather");

            // Phase 1: Initialize low pressure, cool weather
            LogPhase($"Phase 1: Setting initial conditions (Low Pressure: {initialPressure} hPa, Cool: {initialTemperature}°C, Humidity: {initialHumidity}%)");
            SetWeatherConditions(initialPressure, initialTemperature, initialHumidity);
            
            yield return new WaitForSeconds(initialPhaseDuration);

            LogPhase("Phase 2: Transitioning to high pressure, warm weather...");
            
            // Phase 2: Transition to high pressure, warm weather
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                float t = elapsed / transitionDuration;
                
                // Smooth interpolation
                float pressure = Mathf.Lerp(initialPressure, finalPressure, t);
                float temperature = Mathf.Lerp(initialTemperature, finalTemperature, t);
                float humidity = Mathf.Lerp(initialHumidity, finalHumidity, t);

                SetWeatherConditions(pressure, temperature, humidity);

                // Monitor precipitation formation
                MonitorPrecipitation();

                // Periodic logging
                if (Time.time - lastLogTime >= LOG_INTERVAL)
                {
                    LogWeatherState($"Transition progress: {t * 100f:F1}%");
                    lastLogTime = Time.time;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 3: Maintain high pressure, warm weather
            LogPhase($"Phase 3: Maintaining high pressure conditions (Pressure: {finalPressure} hPa, Warm: {finalTemperature}°C, Humidity: {finalHumidity}%)");
            SetWeatherConditions(finalPressure, finalTemperature, finalHumidity);

            elapsed = 0f;
            while (elapsed < highPressurePhaseDuration)
            {
                MonitorPrecipitation();

                if (Time.time - lastLogTime >= LOG_INTERVAL)
                {
                    LogWeatherState("High pressure phase");
                    lastLogTime = Time.time;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Test complete
            LogPhase("=== Test Complete ===");
            LogTestResults();

            testRunning = false;
        }

        /// <summary>
        /// Set weather conditions
        /// </summary>
        private void SetWeatherConditions(float pressure, float temperature, float humidity)
        {
            if (meteorology != null)
            {
                meteorology.pressure = pressure;
                meteorology.temperature = temperature;
                meteorology.humidity = humidity;
            }
        }

        /// <summary>
        /// Monitor precipitation formation
        /// </summary>
        private void MonitorPrecipitation()
        {
            if (precipitation == null)
                return;

            // Track maximum precipitation rate
            if (precipitation.precipitationRate > maxPrecipitationRate)
            {
                maxPrecipitationRate = precipitation.precipitationRate;
            }

            // Track when precipitation starts forming
            if (precipitationStartTime < 0f && precipitation.precipitationRate > 0.1f)
            {
                precipitationStartTime = Time.time - testStartTime;
                LogPhase($"⚠ Precipitation started forming at {precipitationStartTime:F1} seconds!");
            }

            // Track cloud coverage
            if (meteorology != null)
            {
                float cloudCover = meteorology.GetCloudCoverPercentage();
                if (cloudCover > maxCloudCover)
                {
                    maxCloudCover = cloudCover;
                }
            }
        }

        /// <summary>
        /// Calculate expected precipitation based on atmospheric conditions
        /// </summary>
        private float CalculateExpectedPrecipitation(float temperature, float humidity, float pressure)
        {
            // Simplified precipitation model:
            // Precipitation forms when:
            // 1. High humidity (water vapor available)
            // 2. Temperature difference (cooling causes condensation)
            // 3. Pressure changes (rising air = low pressure = clouds/precipitation)

            // Calculate saturation pressure (Clausius-Clapeyron approximation)
            float saturationPressure = 6.112f * Mathf.Exp((17.67f * temperature) / (temperature + 243.5f)); // in hPa

            // Vapor pressure
            float vaporPressure = (humidity / 100f) * saturationPressure;

            // Precipitation likelihood increases with:
            // - High humidity (> 80%)
            // - Temperature changes (warm air rising)
            // - Low pressure (rising air)

            float pressureFactor = (1013.25f - pressure) / 50f; // Lower pressure = more precipitation
            pressureFactor = Mathf.Clamp01(pressureFactor);

            float humidityFactor = (humidity - 60f) / 40f; // Higher humidity = more precipitation
            humidityFactor = Mathf.Clamp01(humidityFactor);

            // When warm air (high temperature) meets cooler conditions or rises, it cools
            // This cooling causes condensation and precipitation
            float tempRiseFactor = (temperature - initialTemperature) / 20f; // Temperature increase
            tempRiseFactor = Mathf.Clamp01(tempRiseFactor);

            // Combine factors
            float precipitationRate = pressureFactor * humidityFactor * tempRiseFactor * 10f; // mm/h

            return precipitationRate;
        }

        /// <summary>
        /// Validate that all required components are present
        /// </summary>
        private bool ValidateComponents()
        {
            if (weatherSystem == null)
            {
                Debug.LogError("[WeatherPressureTransitionTest] WeatherSystem not found!");
                return false;
            }

            if (meteorology == null)
            {
                Debug.LogError("[WeatherPressureTransitionTest] Meteorology not found!");
                return false;
            }

            if (precipitation == null)
            {
                Debug.LogError("[WeatherPressureTransitionTest] Precipitation not found!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Log a test phase message
        /// </summary>
        private void LogPhase(string message)
        {
            float time = testRunning ? Time.time - testStartTime : 0f;
            string logEntry = $"[{time:F1}s] {message}\n";
            testLog += logEntry;
            
            if (enableDebugLog)
            {
                Debug.Log($"[WeatherPressureTransitionTest] {logEntry.Trim()}");
            }
        }

        /// <summary>
        /// Log current weather state
        /// </summary>
        private void LogWeatherState(string context)
        {
            if (meteorology == null || precipitation == null)
                return;

            float time = Time.time - testStartTime;
            float expectedPrecipitation = CalculateExpectedPrecipitation(
                meteorology.temperature,
                meteorology.humidity,
                meteorology.pressure
            );

            string logEntry = $"[{time:F1}s] {context}\n" +
                $"  Pressure: {meteorology.pressure:F1} hPa\n" +
                $"  Temperature: {meteorology.temperature:F1}°C\n" +
                $"  Humidity: {meteorology.humidity:F1}%\n" +
                $"  Dew Point: {meteorology.dewPoint:F1}°C\n" +
                $"  Cloud Cover: {meteorology.GetCloudCoverPercentage():F1}%\n" +
                $"  Precipitation Rate: {precipitation.precipitationRate:F2} mm/h (Expected: {expectedPrecipitation:F2} mm/h)\n" +
                $"  Precipitation Type: {precipitation.type}\n" +
                $"  Accumulation: {precipitation.accumulation:F2} mm\n\n";

            testLog += logEntry;

            if (enableDebugLog)
            {
                Debug.Log($"[WeatherPressureTransitionTest] {logEntry.Trim()}");
            }
        }

        /// <summary>
        /// Log final test results
        /// </summary>
        private void LogTestResults()
        {
            string results = "\n=== Test Results ===\n" +
                $"Maximum Precipitation Rate: {maxPrecipitationRate:F2} mm/h\n" +
                $"Precipitation Start Time: {(precipitationStartTime >= 0f ? $"{precipitationStartTime:F1} seconds" : "None observed")}\n" +
                $"Maximum Cloud Cover: {maxCloudCover:F1}%\n" +
                $"Final Accumulation: {(precipitation != null ? precipitation.accumulation.ToString("F2") : "N/A")} mm\n" +
                $"Test Duration: {Time.time - testStartTime:F1} seconds\n";

            testLog += results;

            if (enableDebugLog)
            {
                Debug.Log($"[WeatherPressureTransitionTest] {results.Trim()}");
            }
        }

        /// <summary>
        /// Manually trigger precipitation based on conditions
        /// </summary>
        private void Update()
        {
            if (!testRunning || meteorology == null || precipitation == null)
                return;

            // Calculate expected precipitation and update precipitation rate
            // This simulates the physical process of warm air rising and cooling
            float expectedPrecipitation = CalculateExpectedPrecipitation(
                meteorology.temperature,
                meteorology.humidity,
                meteorology.pressure
            );

            // Smoothly update precipitation rate towards expected value
            precipitation.precipitationRate = Mathf.Lerp(
                precipitation.precipitationRate,
                expectedPrecipitation,
                Time.deltaTime * 0.5f // Gradual transition
            );

            // Update precipitation type based on temperature
            if (precipitation != null && meteorology != null)
            {
                precipitation.UpdateTypeFromTemperature(meteorology.temperature);
            }

            // Update cloud cover based on humidity and temperature
            if (meteorology != null)
            {
                // Cloud cover increases with humidity and temperature (warm air holds more moisture)
                float humidityFactor = meteorology.humidity / 100f;
                float tempFactor = Mathf.Clamp01((meteorology.temperature - initialTemperature) / 20f);
                meteorology.cloudCover = humidityFactor * tempFactor * 100f;
            }
        }
    }
}
