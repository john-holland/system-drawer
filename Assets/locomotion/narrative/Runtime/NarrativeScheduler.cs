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
        [Tooltip("When an event has spatiotemporalVolume, these keys are used to resolve player/listener positions from bindings for region check. Event triggers when any key resolves to a GameObject inside the volume. Event can override with its own positionKeys.")]
        public List<string> positionKeys = new List<string> { "player" };

        [Header("BioRhythm")]
        [Tooltip("Forwards to NarrativeCalendarAsset.showBioRhythmEvents when calendar is assigned.")]
        public bool showBioRhythmEvents;

        [Header("Debug")]
        public bool debugLogging = false;

        private readonly List<NarrativeCalendarEvent> scratch = new List<NarrativeCalendarEvent>(64);

        private void OnValidate()
        {
            if (calendar != null)
                calendar.showBioRhythmEvents = showBioRhythmEvents;
        }

        private void OnEnable()
        {
            if (autoFindReferences)
            {
                if (clock == null) clock = FindAnyObjectByType<NarrativeClock>();
                if (executor == null) executor = FindAnyObjectByType<NarrativeExecutor>();
            }

            if (calendar != null)
                showBioRhythmEvents = calendar.showBioRhythmEvents;

            if (applyPastEventsOnEnable)
                ApplyEventsUpToNow();
        }

        private void Update()
        {
            if (calendar != null)
                calendar.showBioRhythmEvents = showBioRhythmEvents;

            if (calendar == null || clock == null || executor == null)
                return;

            if (executor.GetRuntimeState().isExecuting)
                return;

            ApplyEventsUpToNow();
        }

        /// <summary>
        /// Applies (triggers) all events whose start time is <= now and not yet triggered.
        /// Live playback starts only the earliest pending event.
        /// </summary>
        public void ApplyEventsUpToNow()
        {
            if (calendar == null || clock == null || executor == null)
                return;

            NarrativeDateTime now = clock.Now;
            CollectPending(now, scratch);
            if (scratch.Count == 0)
                return;

            scratch.Sort((a, b) => a.startDateTime.CompareTo(b.startDateTime));
            var evt = scratch[0];
            if (debugLogging)
                Debug.Log($"[NarrativeScheduler] Triggering '{evt.title}' at {now}");

            executor.StartEvent(evt);
        }

        /// <summary>
        /// Jumps the clock and applies every untriggered event at or before the target, in start order.
        /// This does not run from Update.
        /// </summary>
        public int FastForwardTo(NarrativeDateTime target)
        {
            if (calendar == null || executor == null)
                return 0;
            if (clock != null)
                clock.JumpTo(target);
            CollectPending(target, scratch);
            scratch.Sort((a, b) => a.startDateTime.CompareTo(b.startDateTime));
            int applied = 0;
            for (int i = 0; i < scratch.Count; i++)
            {
                if (executor.ExecuteEventImmediate(scratch[i]))
                    applied++;
            }
            return applied;
        }

        /// <summary>Drops triggered ids whose events start after the target, then jumps the clock.</summary>
        public int ClearTriggeredAfter(NarrativeDateTime target)
        {
            if (calendar == null || executor == null)
                return 0;
            NarrativeRuntimeState state = executor.GetRuntimeState();
            if (state.triggeredEventIds == null)
                return 0;
            int removed = 0;
            for (int i = state.triggeredEventIds.Count - 1; i >= 0; i--)
            {
                string id = state.triggeredEventIds[i];
                NarrativeCalendarEvent evt = FindEvent(id);
                if (evt != null && evt.startDateTime > target)
                {
                    state.triggeredEventIds.RemoveAt(i);
                    removed++;
                }
            }
            if (clock != null)
                clock.JumpTo(target);
            return removed;
        }

        void CollectPending(NarrativeDateTime now, List<NarrativeCalendarEvent> into)
        {
            into.Clear();
            if (calendar == null || calendar.events == null || executor == null)
                return;
            NarrativeRuntimeState state = executor.GetRuntimeState();
            float tNow = NarrativeCalendarMath.DateTimeToSeconds(now);
            for (int i = 0; i < calendar.events.Count; i++)
            {
                NarrativeCalendarEvent e = calendar.events[i];
                if (e == null)
                    continue;
                if (state.triggeredEventIds != null && state.triggeredEventIds.Contains(e.id))
                    continue;

                if (e.spatiotemporalVolume.HasValue)
                {
                    var vol = e.spatiotemporalVolume.Value;
                    if (!NarrativeVolumeQuery.IsEventActiveAt(vol.tMin, vol.tMax, tNow))
                        continue;
                    var keysToUse = (e.positionKeys != null && e.positionKeys.Count > 0) ? e.positionKeys : positionKeys;
                    if (executor.bindings == null || keysToUse == null || keysToUse.Count == 0)
                        continue;
                    bool anyInVolume = false;
                    for (int k = 0; k < keysToUse.Count; k++)
                    {
                        string key = keysToUse[k];
                        if (string.IsNullOrWhiteSpace(key))
                            continue;
                        if (executor.bindings.TryResolveGameObject(key.Trim(), out GameObject go) && go != null
                            && vol.Contains(go.transform.position, tNow))
                        {
                            anyInVolume = true;
                            break;
                        }
                    }
                    if (!anyInVolume)
                        continue;
                    into.Add(e);
                }
                else if (e.startDateTime <= now)
                {
                    into.Add(e);
                }
            }
        }

        NarrativeCalendarEvent FindEvent(string id)
        {
            if (calendar == null || calendar.events == null || string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < calendar.events.Count; i++)
            {
                NarrativeCalendarEvent evt = calendar.events[i];
                if (evt != null && evt.id == id)
                    return evt;
            }
            return null;
        }
    }
}

