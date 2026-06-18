using System;

using System.Collections.Generic;



namespace Locomotion.Narrative

{

    /// <summary>Walks execution ledger newest-first, restoring snapshots and calling action undo hooks.</summary>

    public static class NarrativeRewindUndoWalker

    {

        public static void RewindToTime(

            float targetTime,

            NarrativeRuntimeState state,

            NarrativeExecutor executor,

            NarrativeExecutionContext ctx)

        {

            if (state == null)

                return;



            if (state.executionLedger != null && executor != null && ctx != null)

            {

                for (int i = state.executionLedger.Count - 1; i >= 0; i--)

                {

                    var entry = state.executionLedger[i];

                    if (entry.time <= targetTime)

                        break;

                    executor.TryUndoLedgerEntry(entry, ctx);

                }

            }



            TrimTriggeredEvents(state, targetTime);



            if (executor != null)

            {

                executor.PauseExecution();

                executor.SetRuntimeState(state);

            }



            if (state.executionLedger != null)

            {

                for (int i = state.executionLedger.Count - 1; i >= 0; i--)

                {

                    if (state.executionLedger[i].time > targetTime)

                        state.executionLedger.RemoveAt(i);

                }

            }

        }



        static void TrimTriggeredEvents(NarrativeRuntimeState state, float targetTime)

        {

            if (state.triggeredEventIds == null || state.executionLedger == null)

                return;



            var finishByEvent = new Dictionary<string, float>(StringComparer.Ordinal);

            for (int i = 0; i < state.executionLedger.Count; i++)

            {

                var e = state.executionLedger[i];

                if (string.IsNullOrEmpty(e.eventId))

                    continue;

                if (e.finishTime > 0f)

                    finishByEvent[e.eventId] = Math.Max(finishByEvent.TryGetValue(e.eventId, out float prev) ? prev : 0f, e.finishTime);

            }



            for (int i = state.triggeredEventIds.Count - 1; i >= 0; i--)

            {

                string eventId = state.triggeredEventIds[i];

                if (!finishByEvent.TryGetValue(eventId, out float finishTime) || finishTime > targetTime)

                    state.triggeredEventIds.RemoveAt(i);

            }

        }

    }

}


