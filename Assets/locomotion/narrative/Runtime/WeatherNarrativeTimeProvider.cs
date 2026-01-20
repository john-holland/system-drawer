using UnityEngine;
using Weather;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Narrative time provider that is intended to stay in-step with the WeatherSystem update cadence.
    /// MVP: uses Unity time but keeps an explicit reference to WeatherSystem so later we can drive time
    /// from weather simulation steps / pausable manifolds.
    /// </summary>
    public class WeatherNarrativeTimeProvider : MonoBehaviour, INarrativeTimeProvider
    {
        [Tooltip("Optional WeatherSystem reference (auto-found if null).")]
        public WeatherSystem weatherSystem;

        [Header("Start Date (UTC-like)")]
        public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);

        [Header("Rate")]
        [Tooltip("Narrative seconds that pass per 1 Unity second.")]
        public double narrativeSecondsPerUnitySecond = 60.0;

        private float startUnityTime;

        private void Awake()
        {
            if (weatherSystem == null)
                weatherSystem = FindObjectOfType<WeatherSystem>();
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

