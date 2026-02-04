#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

namespace Locomotion.Narrative.EditorTools
{
    /// <summary>Result of comparing one baseline event to current (or null for added/deleted).</summary>
    public enum InterpretedEventDiffKind
    {
        Same,
        PropertyChange,
        Deleted,
        Added
    }

    /// <summary>One row in the diff: baseline (or null), current (or null), and kind.</summary>
    public struct InterpretedEventDiffEntry
    {
        public InterpretedEvent? Baseline;
        public InterpretedEvent? Current;
        public InterpretedEventDiffKind Kind;
    }

    /// <summary>Semantic diff of two interpreted-event lists. Index-based matching; float tolerance for equality.</summary>
    public static class InterpretedEventDiff
    {
        public const float FloatTolerance = 1e-4f;

        public static bool EventEquals(in InterpretedEvent a, in InterpretedEvent b)
        {
            return string.Equals(a.title, b.title, StringComparison.Ordinal)
                   && Mathf.Abs(a.startSeconds - b.startSeconds) < FloatTolerance
                   && Mathf.Abs(a.durationSeconds - b.durationSeconds) < FloatTolerance
                   && Vector3.SqrMagnitude(a.center - b.center) < FloatTolerance * FloatTolerance
                   && Vector3.SqrMagnitude(a.size - b.size) < FloatTolerance * FloatTolerance
                   && Mathf.Abs(a.tMin - b.tMin) < FloatTolerance
                   && Mathf.Abs(a.tMax - b.tMax) < FloatTolerance;
        }

        public static bool HasPropertyChange(in InterpretedEvent baseline, in InterpretedEvent current)
        {
            return !string.Equals(baseline.title, current.title, StringComparison.Ordinal)
                   || Mathf.Abs(baseline.startSeconds - current.startSeconds) >= FloatTolerance
                   || Mathf.Abs(baseline.durationSeconds - current.durationSeconds) >= FloatTolerance
                   || Vector3.SqrMagnitude(baseline.center - current.center) >= FloatTolerance * FloatTolerance
                   || Vector3.SqrMagnitude(baseline.size - current.size) >= FloatTolerance * FloatTolerance
                   || Mathf.Abs(baseline.tMin - current.tMin) >= FloatTolerance
                   || Mathf.Abs(baseline.tMax - current.tMax) >= FloatTolerance;
        }

        /// <summary>Run diff: index-based matching. Extra baseline = Deleted, extra current = Added; same index compared for Same vs PropertyChange.</summary>
        public static List<InterpretedEventDiffEntry> Run(IReadOnlyList<InterpretedEvent> baseline, IReadOnlyList<InterpretedEvent> current)
        {
            var result = new List<InterpretedEventDiffEntry>();
            int bCount = baseline?.Count ?? 0;
            int cCount = current?.Count ?? 0;
            int max = Mathf.Max(bCount, cCount);

            for (int i = 0; i < max; i++)
            {
                bool hasBaseline = i < bCount;
                bool hasCurrent = i < cCount;

                if (hasBaseline && hasCurrent)
                {
                    var b = baseline[i];
                    var c = current[i];
                    if (EventEquals(b, c))
                        result.Add(new InterpretedEventDiffEntry { Baseline = b, Current = c, Kind = InterpretedEventDiffKind.Same });
                    else
                        result.Add(new InterpretedEventDiffEntry { Baseline = b, Current = c, Kind = InterpretedEventDiffKind.PropertyChange });
                }
                else if (hasBaseline)
                    result.Add(new InterpretedEventDiffEntry { Baseline = baseline[i], Current = null, Kind = InterpretedEventDiffKind.Deleted });
                else
                    result.Add(new InterpretedEventDiffEntry { Baseline = null, Current = current[i], Kind = InterpretedEventDiffKind.Added });
            }

            return result;
        }
    }
}
#endif
