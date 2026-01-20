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

        [Header("Debug")]
        public bool debugLogging = false;

        private readonly List<NarrativeCalendarEvent> scratch = new List<NarrativeCalendarEvent>(64);

        private void OnEnable()
        {
            if (autoFindReferences)
            {
                if (clock == null) clock = FindObjectOfType<NarrativeClock>();
                if (executor == null) executor = FindObjectOfType<NarrativeExecutor>();
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
            for (int i = 0; i < calendar.events.Count; i++)
            {
                var e = calendar.events[i];
                if (e == null) continue;
                if (e.startDateTime <= now)
                {
                    if (state.triggeredEventIds.Contains(e.id))
                        continue;
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

