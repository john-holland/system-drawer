using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Narrative time provider driven by Unity time (scaled or unscaled).
    /// </summary>
    public class UnityNarrativeTimeProvider : MonoBehaviour, INarrativeTimeProvider
    {
        [Header("Start Date (UTC-like)")]
        public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);

        [Header("Rate")]
        [Tooltip("Narrative seconds that pass per 1 Unity second.")]
        public double narrativeSecondsPerUnitySecond = 60.0;

        [Tooltip("If true, uses Time.unscaledTime; otherwise uses Time.time.")]
        public bool useUnscaledTime = false;

        private float startUnityTime;

        private void OnEnable()
        {
            startUnityTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        public NarrativeDateTime GetNow()
        {
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            double elapsed = Mathf.Max(0f, t - startUnityTime);
            double narrativeSeconds = elapsed * narrativeSecondsPerUnitySecond;
            return startDateTime.AddSeconds(narrativeSeconds);
        }
    }
}

