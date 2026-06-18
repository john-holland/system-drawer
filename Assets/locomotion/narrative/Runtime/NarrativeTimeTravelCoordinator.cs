using System;
using Planetary.TimeTravel;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum RewindAuthorityHint
    {
        Local,
        ServerAuthoritative,
        HostPeer
    }

    /// <summary>Unified narrative + weather rewind entry point.</summary>
    [AddComponentMenu("Locomotion/Narrative/Time Travel Coordinator")]
    public sealed class NarrativeTimeTravelCoordinator : MonoBehaviour
    {
        public NarrativeClock clock;
        public NarrativeExecutor executor;
        public NarrativeScheduler scheduler;
        public PlanetaryWeatherTimeTravelSystem weatherTimeTravel;

        public event Action<float, RewindAuthorityHint> RewindRequested;

        NarrativeExecutionContext _ctx;

        void Awake()
        {
            if (clock == null) clock = FindAnyObjectByType<NarrativeClock>();
            if (executor == null) executor = FindAnyObjectByType<NarrativeExecutor>();
            if (scheduler == null) scheduler = FindAnyObjectByType<NarrativeScheduler>();
            if (weatherTimeTravel == null) weatherTimeTravel = FindAnyObjectByType<PlanetaryWeatherTimeTravelSystem>();
        }

        public void RequestRewind(NarrativeDateTime targetTime, RewindAuthorityHint authorityHint = RewindAuthorityHint.Local)
        {
            float seconds = NarrativeCalendarMath.DateTimeToSeconds(targetTime);
            if (authorityHint == RewindAuthorityHint.Local)
            {
                ApplyRewindLocal(BuildCheckpointForTime(seconds, "local"));
                return;
            }
            RewindRequested?.Invoke(seconds, authorityHint);
        }

        public NarrativeTimeTravelCheckpoint BuildCheckpointForTime(float targetTime, string authorityClientId)
        {
            var checkpoint = new NarrativeTimeTravelCheckpoint
            {
                narrativeTime = targetTime,
                authorityClientId = authorityClientId
            };
            if (weatherTimeTravel != null)
            {
                var frame = weatherTimeTravel.CaptureCurrentPublic();
                checkpoint.weatherFrameJson = WeatherTimeTravelFrameSerializer.ToJson(frame);
            }
            if (executor != null)
            {
                var state = executor.GetRuntimeState();
                checkpoint.triggeredEventIds = new System.Collections.Generic.List<string>(state.triggeredEventIds);
                checkpoint.activeEventId = state.activeEventId;
                checkpoint.nodeStack = new System.Collections.Generic.List<string>(state.nodeStack);
                checkpoint.childIndexStack = new System.Collections.Generic.List<int>(state.childIndexStack);
                checkpoint.executionLedger = new System.Collections.Generic.List<NarrativeExecutionLedgerEntry>(state.executionLedger);
            }
            return checkpoint;
        }

        public void MergeCheckpoint(NarrativeTimeTravelCheckpoint incoming)
        {
            if (incoming?.executionLedger == null || executor == null)
                return;
            executor.GetRuntimeState().executionLedger.AddRange(incoming.executionLedger);
        }

        public void ApplyRewindLocal(NarrativeTimeTravelCheckpoint checkpoint)
        {
            if (checkpoint == null)
                return;

            PathReplacementGate.LockUntilCausalityDepth(int.MaxValue);
            try
            {
                clock?.JumpTo(NarrativeCalendarMath.SecondsToNarrativeDateTime(checkpoint.narrativeTime));

                if (weatherTimeTravel != null && !string.IsNullOrEmpty(checkpoint.weatherFrameJson))
                    weatherTimeTravel.ApplyFramePublic(WeatherTimeTravelFrameSerializer.FromJson(checkpoint.weatherFrameJson));
                else
                    weatherTimeTravel?.OnCalendarJump(checkpoint.narrativeTime);

                if (executor != null)
                {
                    var state = executor.GetRuntimeState();
                    if (checkpoint.triggeredEventIds != null)
                    {
                        state.triggeredEventIds.Clear();
                        state.triggeredEventIds.AddRange(checkpoint.triggeredEventIds);
                    }
                    state.activeEventId = checkpoint.activeEventId;
                    if (checkpoint.nodeStack != null)
                    {
                        state.nodeStack.Clear();
                        state.nodeStack.AddRange(checkpoint.nodeStack);
                    }
                    if (checkpoint.childIndexStack != null)
                    {
                        state.childIndexStack.Clear();
                        state.childIndexStack.AddRange(checkpoint.childIndexStack);
                    }
                    if (checkpoint.executionLedger != null)
                        state.executionLedger = new System.Collections.Generic.List<NarrativeExecutionLedgerEntry>(checkpoint.executionLedger);

                    _ctx ??= new NarrativeExecutionContext(clock, executor.bindings, null);
                    NarrativeRewindUndoWalker.RewindToTime(checkpoint.narrativeTime, state, executor, _ctx);
                }

                scheduler?.ApplyEventsUpToNow();
            }
            finally
            {
                PathReplacementGate.Unlock();
            }
        }
    }
}
