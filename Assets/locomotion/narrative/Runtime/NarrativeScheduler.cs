using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Watches a NarrativeCalendarAsset and triggers events when the clock reaches their start time.
    /// MVP: triggers events sequentially through a NarrativeExecutor.
    /// </summary>
    public class NarrativeScheduler : MonoBehaviour
    {
        public NarrativeCalendarAsset calendar;
        public NarrativeClock clock;
        public NarrativeExecutor executor;

        [Header("Behavior")]
        public bool autoFindReferences = true;
        public bool applyPastEventsOnEnable = true;
        [Tooltip("When an event has spatiotemporalVolume, this key is used to resolve player/listener position from bindings for region check.")]
        public string playerPositionKey = "player";

        [Header("Debug")]
        public bool debugLogging = false;

        private readonly List<NarrativeCalendarEvent> scratch = new List<NarrativeCalendarEvent>(64);

        private void OnEnable()
        {
            if (autoFindReferences)
            {
                if (clock == null) clock = FindAnyObjectByType<NarrativeClock>();
                if (executor == null) executor = FindAnyObjectByType<NarrativeExecutor>();
            }

            if (applyPastEventsOnEnable)
                ApplyEventsUpToNow();
        }

        private void Update()
        {
            if (calendar == null || clock == null || executor == null)
                return;

            if (executor.GetRuntimeState().isExecuting)
                return;

            ApplyEventsUpToNow();
        }

        /// <summary>
        /// Applies (triggers) all events whose start time is <= now and not yet triggered.
        /// </summary>
        public void ApplyEventsUpToNow()
        {
            if (calendar == null || clock == null || executor == null)
                return;

            NarrativeDateTime now = clock.Now;
            NarrativeRuntimeState state = executor.GetRuntimeState();

            scratch.Clear();
            float tNow = NarrativeCalendarMath.DateTimeToSeconds(now);
            for (int i = 0; i < calendar.events.Count; i++)
            {
                var e = calendar.events[i];
                if (e == null) continue;
                if (state.triggeredEventIds.Contains(e.id))
                    continue;

                if (e.spatiotemporalVolume.HasValue)
                {
                    var vol = e.spatiotemporalVolume.Value;
                    if (!NarrativeVolumeQuery.IsEventActiveAt(vol.tMin, vol.tMax, tNow))
                        continue;
                    if (executor.bindings == null || string.IsNullOrEmpty(playerPositionKey)
                        || !executor.bindings.TryResolveGameObject(playerPositionKey, out GameObject go) || go == null)
                        continue;
                    if (!vol.Contains(go.transform.position, tNow))
                        continue;
                    scratch.Add(e);
                }
                else if (e.startDateTime <= now)
                {
                    scratch.Add(e);
                }
            }

            if (scratch.Count == 0)
                return;

            // Sort by start time (stable deterministic execution).
            scratch.Sort((a, b) => a.startDateTime.CompareTo(b.startDateTime));

            // Start the earliest pending event.
            var evt = scratch[0];
            if (debugLogging)
                Debug.Log($"[NarrativeScheduler] Triggering '{evt.title}' at {now}");

            executor.StartEvent(evt);
        }
    }
}

