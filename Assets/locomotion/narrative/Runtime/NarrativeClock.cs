using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Scene-level clock wrapper that exposes a consistent narrative date/time for scheduling and animation systems.
    /// </summary>
    public class NarrativeClock : MonoBehaviour
    {
        [Tooltip("Optional time provider. If null, a UnityNarrativeTimeProvider will be used if present, otherwise this clock returns a fixed start time.")]
        public MonoBehaviour timeProvider;

        [Tooltip("Fallback start date/time when no provider is available.")]
        public NarrativeDateTime fallbackStartDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);

        public NarrativeDateTime Now
        {
            get
            {
                var provider = timeProvider as INarrativeTimeProvider;
                if (provider != null)
                    return provider.GetNow();

                // Try auto-find a Unity provider in the scene.
                var unityProvider = FindAnyObjectByType<UnityNarrativeTimeProvider>();
                if (unityProvider != null)
                    return unityProvider.GetNow();

                return fallbackStartDateTime;
            }
        }
    }
}

