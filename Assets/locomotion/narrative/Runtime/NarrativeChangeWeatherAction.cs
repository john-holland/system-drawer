using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to change weather conditions.
    /// </summary>
    [Serializable]
    public class NarrativeChangeWeatherAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the WeatherSystem GameObject")]
        public string weatherSystemKey = "weather";

        [Tooltip("Weather event type")]
        public WeatherEventType weatherType = WeatherEventType.TemperatureChange;

        [Tooltip("Event magnitude/intensity")]
        public float intensity = 1f;

        [Tooltip("Event duration (0 = permanent)")]
        public float duration = 0f;

        [Tooltip("Is the change additive (true) or multiplicative (false)")]
        public bool isAdditive = true;

        [Tooltip("Which systems this event affects")]
        public AffectedSystem affectedSystems = AffectedSystem.Meteorology;

        [Tooltip("Wind direction in degrees (0-360, meteorological: direction wind comes from). Used for WindGust when set.")]
        [Range(0f, 360f)]
        public float windDirectionDegrees = 0f;

        [Tooltip("Weather event GameObject (optional, creates new if null)")]
        public GameObject weatherEventPrefab;

        [NonSerialized]
        private GameObject createdWeatherEvent;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            // Resolve WeatherSystem
            GameObject weatherSystemGo = null;
            if (!string.IsNullOrEmpty(weatherSystemKey))
            {
                if (!ctx.TryResolveGameObject(weatherSystemKey, out weatherSystemGo))
                {
                    weatherSystemGo = null;
                }
            }

            if (weatherSystemGo == null)
            {
                // Try to find WeatherSystem in scene
                var weatherSystemType = System.Type.GetType("WeatherSystem, Weather.Runtime");
                if (weatherSystemType == null)
                {
                    weatherSystemType = System.Type.GetType("WeatherSystem, Assembly-CSharp");
                }

                if (weatherSystemType != null)
                {
                    var weatherSystem = UnityEngine.Object.FindAnyObjectByType(weatherSystemType);
                    if (weatherSystem != null)
                    {
                        weatherSystemGo = (weatherSystem as MonoBehaviour)?.gameObject;
                    }
                }
            }

            if (weatherSystemGo == null)
            {
                Debug.LogWarning("[NarrativeChangeWeatherAction] Could not find WeatherSystem");
                return BehaviorTreeStatus.Failure;
            }

            // Create or use weather event
            GameObject weatherEventObj = weatherEventPrefab;
            if (weatherEventObj == null)
            {
                // Create new weather event GameObject
                weatherEventObj = new GameObject("NarrativeWeatherEvent_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
                weatherEventObj.transform.SetParent(weatherSystemGo.transform);

                // Add WeatherEvent component
                var weatherEventType = System.Type.GetType("WeatherEvent, Weather.Runtime");
                if (weatherEventType == null)
                {
                    weatherEventType = System.Type.GetType("WeatherEvent, Assembly-CSharp");
                }

                if (weatherEventType != null)
                {
                    var weatherEvent = weatherEventObj.AddComponent(weatherEventType);
                    
                    // Set event properties using reflection
                    SetProperty(weatherEvent, "eventType", Convert.ToInt32(weatherType));
                    SetProperty(weatherEvent, "magnitude", intensity);
                    SetProperty(weatherEvent, "isAdditive", isAdditive);
                    SetProperty(weatherEvent, "affectedSystems", Convert.ToInt32(affectedSystems));

                    if (duration > 0f)
                    {
                        SetProperty(weatherEvent, "duration", duration);
                    }

                    if (weatherType == WeatherEventType.WindGust)
                    {
                        Vector3 dir = new Vector3(
                            Mathf.Sin(windDirectionDegrees * Mathf.Deg2Rad),
                            0f,
                            -Mathf.Cos(windDirectionDegrees * Mathf.Deg2Rad));
                        SetProperty(weatherEvent, "vectorData", dir);
                    }

                    createdWeatherEvent = weatherEventObj;
                }
                else
                {
                    Debug.LogError("[NarrativeChangeWeatherAction] Could not find WeatherEvent type");
                    UnityEngine.Object.Destroy(weatherEventObj);
                    return BehaviorTreeStatus.Failure;
                }
            }
            else
            {
                // Use existing prefab
                weatherEventObj = UnityEngine.Object.Instantiate(weatherEventPrefab, weatherSystemGo.transform);
                
                // Get WeatherEvent type
                var weatherEventType = System.Type.GetType("WeatherEvent, Weather.Runtime");
                if (weatherEventType == null)
                {
                    weatherEventType = System.Type.GetType("WeatherEvent, Assembly-CSharp");
                }
                
                // Update properties
                if (weatherEventType != null)
                {
                    var weatherEvent = weatherEventObj.GetComponent(weatherEventType);
                    if (weatherEvent != null)
                    {
                        SetProperty(weatherEvent, "eventType", Convert.ToInt32(weatherType));
                        SetProperty(weatherEvent, "magnitude", intensity);
                        SetProperty(weatherEvent, "isAdditive", isAdditive);
                        SetProperty(weatherEvent, "affectedSystems", Convert.ToInt32(affectedSystems));
                        if (weatherType == WeatherEventType.WindGust)
                        {
                            Vector3 dir = new Vector3(
                                Mathf.Sin(windDirectionDegrees * Mathf.Deg2Rad),
                                0f,
                                -Mathf.Cos(windDirectionDegrees * Mathf.Deg2Rad));
                            SetProperty(weatherEvent, "vectorData", dir);
                        }
                    }
                }
            }

            // If duration is set, schedule destruction
            if (duration > 0f && createdWeatherEvent != null)
            {
                // Schedule destruction (would need a coroutine or timer)
                // For now, just mark it
            }

            return BehaviorTreeStatus.Success;
        }

        private void SetProperty(object obj, string propertyName, object value)
        {
            if (obj == null) return;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName, 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                }
            }
        }
    }

    // Weather event types (from Weather system)
    public enum WeatherEventType
    {
        PressureChange,
        TemperatureChange,
        WindGust,
        Tornado,
        PrecipitationChange,
        HumidityChange,
        CloudFormation
    }

    // Affected systems (from Weather system)
    [Flags]
    public enum AffectedSystem
    {
        None = 0,
        Meteorology = 1,
        Wind = 2,
        Precipitation = 4,
        Cloud = 8,
        Water = 16
    }
}
