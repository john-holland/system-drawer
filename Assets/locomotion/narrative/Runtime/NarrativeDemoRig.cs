using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Convenience component for quickly wiring a narrative stack in a scene.
    /// </summary>
    public class NarrativeDemoRig : MonoBehaviour
    {
        public NarrativeCalendarAsset calendar;

        [Header("Auto-wire")]
        public bool autoCreateIfMissing = true;

        private void Reset()
        {
            AutoWire();
        }

        private void Awake()
        {
            if (autoCreateIfMissing)
                AutoWire();
        }

        private void AutoWire()
        {
            if (GetComponent<NarrativeBindings>() == null)
                gameObject.AddComponent<NarrativeBindings>();

            if (GetComponent<NarrativeClock>() == null)
                gameObject.AddComponent<NarrativeClock>();

            if (GetComponent<UnityNarrativeTimeProvider>() == null)
                gameObject.AddComponent<UnityNarrativeTimeProvider>();

            if (GetComponent<NarrativeExecutor>() == null)
                gameObject.AddComponent<NarrativeExecutor>();

            if (GetComponent<NarrativeScheduler>() == null)
                gameObject.AddComponent<NarrativeScheduler>();

            // Push calendar ref into scheduler if present
            var scheduler = GetComponent<NarrativeScheduler>();
            if (scheduler != null)
                scheduler.calendar = calendar;
        }
    }
}

