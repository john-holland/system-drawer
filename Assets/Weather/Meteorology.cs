using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Meteorology system that controls cloud movements, stages weather events,
    /// and manages atmospheric conditions (temperature, pressure, humidity, etc.)
    /// </summary>
    public class Meteorology : MonoBehaviour
    {
        [Header("Atmospheric Conditions")]
        [Tooltip("Temperature in Celsius")]
        public float temperature = 20f;

        [Tooltip("Pressure in hectopascals (hPa)")]
        public float pressure = 1013.25f; // Standard sea level pressure

        [Tooltip("Humidity percentage (0-100)")]
        [Range(0f, 100f)]
        public float humidity = 50f;

        [Tooltip("Dew point in Celsius (calculated or set manually)")]
        public float dewPoint = 10f;

        [Tooltip("Cloud cover in oktas (0-8) or percentage (0-100)")]
        [Range(0f, 100f)]
        public float cloudCover = 50f;

        [Header("Configuration")]
        [Tooltip("Auto-calculate dew point from temperature and humidity")]
        public bool autoCalculateDewPoint = true;

        [Tooltip("Cloud cover unit (true = oktas 0-8, false = percentage 0-100)")]
        public bool useOktas = false;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Atmospheric influence area radius in meters")]
        public float areaOfEffect = 50f;

        [Header("Weather Event Staging")]
        [Tooltip("Queue of weather events to stage")]
        private Queue<WeatherEventData> stagedEvents = new Queue<WeatherEventData>();

        [Tooltip("Current active weather events")]
        private List<WeatherEventData> activeEvents = new List<WeatherEventData>();

        /// <summary>
        /// Service update called by WeatherSystem
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Update dew point if auto-calculating
            if (autoCalculateDewPoint)
            {
                dewPoint = CalculateDewPoint(temperature, humidity);
            }

            // Process staged events
            ProcessStagedEvents();

            // Update active events
            UpdateActiveEvents(deltaTime);
        }

        /// <summary>
        /// Calculate dew point from temperature and humidity using Magnus formula
        /// </summary>
        public float CalculateDewPoint(float temp, float relHumidity)
        {
            // Magnus formula approximation
            // Td = (b * γ(T, RH)) / (a - γ(T, RH))
            // where γ(T, RH) = (a * T) / (b + T) + ln(RH/100)
            const float a = 17.27f;
            const float b = 237.7f;

            float gamma = (a * temp) / (b + temp) + Mathf.Log(relHumidity / 100f);
            float dewPoint = (b * gamma) / (a - gamma);

            return dewPoint;
        }

        /// <summary>
        /// Stage a weather event
        /// </summary>
        public void StageWeatherEvent(WeatherEventType type, float magnitude)
        {
            var eventData = new WeatherEventData
            {
                eventType = type,
                magnitude = magnitude,
                isAdditive = true,
                position = transform.position
            };

            stagedEvents.Enqueue(eventData);
        }

        /// <summary>
        /// Get current atmospheric conditions
        /// </summary>
        public AtmosphericConditions GetAtmosphericConditions()
        {
            return new AtmosphericConditions
            {
                temperature = this.temperature,
                pressure = this.pressure,
                humidity = this.humidity,
                dewPoint = this.dewPoint,
                cloudCover = this.cloudCover,
                useOktas = this.useOktas
            };
        }

        /// <summary>
        /// Process staged weather events
        /// </summary>
        private void ProcessStagedEvents()
        {
            while (stagedEvents.Count > 0)
            {
                var eventData = stagedEvents.Dequeue();
                activeEvents.Add(eventData);

                // Apply event immediately
                ApplyWeatherEvent(eventData);
            }
        }

        /// <summary>
        /// Update active events and remove expired ones
        /// </summary>
        private void UpdateActiveEvents(float deltaTime)
        {
            // Note: Event duration is handled by WeatherEvent component
            // This is for events staged directly through Meteorology
            activeEvents.RemoveAll(e => e.magnitude == 0f);
        }

        /// <summary>
        /// Apply a weather event to atmospheric conditions
        /// </summary>
        private void ApplyWeatherEvent(WeatherEventData eventData)
        {
            switch (eventData.eventType)
            {
                case WeatherEventType.PressureChange:
                    if (eventData.isAdditive)
                        pressure += eventData.magnitude;
                    else
                        pressure *= eventData.magnitude;
                    break;

                case WeatherEventType.TemperatureChange:
                    if (eventData.isAdditive)
                        temperature += eventData.magnitude;
                    else
                        temperature *= eventData.magnitude;
                    break;

                case WeatherEventType.HumidityChange:
                    if (eventData.isAdditive)
                        humidity += eventData.magnitude;
                    else
                        humidity *= eventData.magnitude;
                    humidity = Mathf.Clamp(humidity, 0f, 100f);
                    break;
            }
        }

        /// <summary>
        /// Get cloud cover in oktas (0-8)
        /// </summary>
        public float GetCloudCoverOktas()
        {
            if (useOktas)
                return Mathf.Clamp(cloudCover, 0f, 8f);
            else
                return (cloudCover / 100f) * 8f;
        }

        /// <summary>
        /// Get cloud cover as percentage (0-100)
        /// </summary>
        public float GetCloudCoverPercentage()
        {
            if (useOktas)
                return (cloudCover / 8f) * 100f;
            else
                return cloudCover;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;

            // Color based on pressure (low = blue, high = red)
            // Normalize pressure: 980-1040 hPa range
            float pressureNormalized = Mathf.InverseLerp(980f, 1040f, pressure);
            Color pressureColor = Color.Lerp(Color.blue, Color.red, pressureNormalized);

            // Temperature indicator (cold = cyan, hot = red)
            float tempNormalized = Mathf.InverseLerp(-10f, 40f, temperature);
            Color tempColor = Color.Lerp(Color.cyan, Color.red, tempNormalized);

            // Combine colors
            Color baseColor = Color.Lerp(pressureColor, tempColor, 0.5f);
            baseColor.a = 0.7f;

            // Sphere showing atmospheric influence area (reusing WeatherEvent style)
            Gizmos.color = baseColor;
            Gizmos.DrawWireSphere(position, areaOfEffect);

            // Cloud cover visualization (opacity/transparency)
            float cloudCoverPercent = GetCloudCoverPercentage();
            Color cloudColor = Color.white;
            cloudColor.a = cloudCoverPercent / 100f * 0.5f;
            Gizmos.color = cloudColor;
            Gizmos.DrawSphere(position, areaOfEffect * 0.9f);

            // Pressure indicator (inner ring)
            Gizmos.color = pressureColor;
            Gizmos.DrawWireSphere(position, areaOfEffect * 0.7f);

            // Temperature indicator (outer ring)
            Gizmos.color = tempColor;
            Gizmos.DrawWireSphere(position, areaOfEffect * 1.1f);
        }
    }

    /// <summary>
    /// Atmospheric conditions data structure
    /// </summary>
    public struct AtmosphericConditions
    {
        public float temperature;
        public float pressure;
        public float humidity;
        public float dewPoint;
        public float cloudCover;
        public bool useOktas;
    }
}
