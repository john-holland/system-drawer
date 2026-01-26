using UnityEngine;
using System;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Narrative time provider that is intended to stay in-step with the WeatherSystem update cadence.
    /// MVP: uses Unity time but keeps an explicit reference to WeatherSystem so later we can drive time
    /// from weather simulation steps / pausable manifolds.
    /// Uses reflection to avoid compile-time dependency on Weather.Runtime.
    /// </summary>
    public class WeatherNarrativeTimeProvider : MonoBehaviour, INarrativeTimeProvider
    {
        [Tooltip("Optional WeatherSystem GameObject (auto-found if null, uses reflection to avoid compile-time dependency).")]
        public GameObject weatherSystemObject;
        
        private object weatherSystemComponent; // WeatherSystem via reflection
        private Type weatherSystemType;

        [Header("Start Date (UTC-like)")]
        public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);

        [Header("Rate")]
        [Tooltip("Narrative seconds that pass per 1 Unity second.")]
        public double narrativeSecondsPerUnitySecond = 60.0;

        private float startUnityTime;

        private void Awake()
        {
            // Use reflection to find WeatherSystem
            weatherSystemType = Type.GetType("Weather.WeatherSystem, Weather.Runtime");
            if (weatherSystemType != null)
            {
                if (weatherSystemObject == null)
                {
                    MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
                    foreach (var mb in allMonoBehaviours)
                    {
                        if (weatherSystemType.IsAssignableFrom(mb.GetType()))
                        {
                            weatherSystemObject = mb.gameObject;
                            weatherSystemComponent = mb;
                            break;
                        }
                    }
                }
                else
                {
                    weatherSystemComponent = weatherSystemObject.GetComponent(weatherSystemType);
                }
            }
        }

        private void OnEnable()
        {
            startUnityTime = Time.time;
        }

        public NarrativeDateTime GetNow()
        {
            // If later we expose a discrete simulation clock from WeatherSystem, use it here.
            float elapsed = Mathf.Max(0f, Time.time - startUnityTime);
            return startDateTime.AddSeconds(elapsed * narrativeSecondsPerUnitySecond);
        }
    }
}

