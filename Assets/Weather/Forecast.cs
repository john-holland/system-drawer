using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Forecast entry for a specific time
    /// </summary>
    [System.Serializable]
    public class ForecastEntry
    {
        [Tooltip("Time for this forecast entry (hours from now)")]
        public float timeHours;

        [Tooltip("Temperature in Celsius")]
        public float temperature;

        [Tooltip("Pressure in hPa")]
        public float pressure;

        [Tooltip("Humidity percentage (0-100)")]
        [Range(0f, 100f)]
        public float humidity;

        [Tooltip("Wind speed in m/s")]
        public float windSpeed;

        [Tooltip("Wind direction in degrees (0-360)")]
        [Range(0f, 360f)]
        public float windDirection;

        [Tooltip("Precipitation rate in mm/h")]
        public float precipitationRate;

        [Tooltip("Weather events at this time")]
        public List<WeatherEventData> weatherEvents = new List<WeatherEventData>();
    }

    /// <summary>
    /// Weather forecast data structure.
    /// Contains a timeline of predicted weather events and conditions.
    /// </summary>
    public class Forecast : MonoBehaviour
    {
        [Header("Forecast Timeline")]
        [Tooltip("List of forecast entries (timeline)")]
        public List<ForecastEntry> forecastEntries = new List<ForecastEntry>();

        [Header("Configuration")]
        [Tooltip("Forecast duration in hours")]
        public float forecastDurationHours = 24f;

        [Tooltip("Time step between forecast entries (hours)")]
        public float timeStepHours = 1f;

        [Tooltip("Auto-generate forecast on start")]
        public bool autoGenerate = false;

        /// <summary>
        /// Get forecast entry at a specific time
        /// </summary>
        public ForecastEntry GetForecastAtTime(float timeHours)
        {
            if (forecastEntries.Count == 0)
                return null;

            // Find closest entry
            ForecastEntry closest = forecastEntries[0];
            float closestDistance = Mathf.Abs(closest.timeHours - timeHours);

            foreach (var entry in forecastEntries)
            {
                float distance = Mathf.Abs(entry.timeHours - timeHours);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = entry;
                }
            }

            return closest;
        }

        /// <summary>
        /// Get forecast entry for current time
        /// </summary>
        public ForecastEntry GetCurrentForecast()
        {
            return GetForecastAtTime(0f);
        }

        /// <summary>
        /// Generate a forecast timeline
        /// </summary>
        public void GenerateForecast()
        {
            forecastEntries.Clear();

            int entryCount = Mathf.CeilToInt(forecastDurationHours / timeStepHours);
            for (int i = 0; i < entryCount; i++)
            {
                float timeHours = i * timeStepHours;
                ForecastEntry entry = new ForecastEntry
                {
                    timeHours = timeHours,
                    temperature = 20f, // Would be generated from weather model
                    pressure = 1013.25f,
                    humidity = 50f,
                    windSpeed = 5f,
                    windDirection = 0f,
                    precipitationRate = 0f,
                    weatherEvents = new List<WeatherEventData>()
                };

                forecastEntries.Add(entry);
            }
        }

        private void Start()
        {
            if (autoGenerate)
            {
                GenerateForecast();
            }
        }
    }
}
