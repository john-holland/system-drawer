using UnityEngine;

namespace Locomotion.Narrative
{
    public class NarrativeClock : MonoBehaviour
    {
        [Tooltip("Optional time provider. If null, a UnityNarrativeTimeProvider will be used if present, otherwise this clock returns a fixed start time.")]
        public MonoBehaviour timeProvider;

        [Tooltip("Fallback start date/time when no provider is available.")]
        public NarrativeDateTime fallbackStartDateTime = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);

        public event System.Action<NarrativeDateTime> OnTimeJump;

        public NarrativeDateTime Now
        {
            get
            {
                var provider = timeProvider as INarrativeTimeProvider;
                if (provider != null)
                    return provider.GetNow();

                var unityProvider = FindAnyObjectByType<UnityNarrativeTimeProvider>();
                if (unityProvider != null)
                    return unityProvider.GetNow();

                return fallbackStartDateTime;
            }
        }

        public float SimulationSeconds => NarrativeCalendarMath.DateTimeToSeconds(Now);

        public void JumpTo(NarrativeDateTime target)
        {
            var unityProvider = timeProvider as UnityNarrativeTimeProvider;
            if (unityProvider == null)
                unityProvider = FindAnyObjectByType<UnityNarrativeTimeProvider>();
            unityProvider?.SetSimulationTime(target);
            OnTimeJump?.Invoke(target);
        }

        public void SetSimulationTime(float narrativeSeconds) =>
            JumpTo(NarrativeCalendarMath.SecondsToNarrativeDateTime(narrativeSeconds));
    }
}
