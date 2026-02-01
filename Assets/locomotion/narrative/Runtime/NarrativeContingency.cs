using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Comparison operators for narrative value comparisons (local copy to avoid Runtime dependency).
    /// </summary>
    public enum ComparisonOperator
    {
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        Equal,
        NotEqual
    }

    public enum NarrativeLogicalOperator
    {
        All,
        Any
    }

    [Serializable]
    public class NarrativeContingency
    {
        [Tooltip("If false, the owning node/action is treated as disabled.")]
        public bool enabled = true;

        public NarrativeLogicalOperator op = NarrativeLogicalOperator.All;

        [SerializeReference]
        public List<NarrativeCondition> conditions = new List<NarrativeCondition>();

        public bool Evaluate(NarrativeExecutionContext ctx)
        {
            if (!enabled)
                return false;

            if (conditions == null || conditions.Count == 0)
                return true;

            if (op == NarrativeLogicalOperator.All)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    var c = conditions[i];
                    if (c == null) continue;
                    if (!c.Evaluate(ctx))
                        return false;
                }
                return true;
            }

            // Any
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                if (c.Evaluate(ctx))
                    return true;
            }
            return false;
        }
    }

    [Serializable]
    public abstract class NarrativeCondition
    {
        public abstract bool Evaluate(NarrativeExecutionContext ctx);
    }

    /// <summary>
    /// Reflection-based condition: compare a component property/field against a value.
    /// This is the core of the GUI contingency builder MVP.
    /// </summary>
    [Serializable]
    public class ComponentMemberCondition : NarrativeCondition
    {
        [Tooltip("Key resolved via NarrativeBindings (GameObject or Component).")]
        public string targetKey;

        [Tooltip("Assembly-qualified name or full name for the component type (optional; if empty, searches all components).")]
        public string componentTypeName;

        [Tooltip("Property or field name.")]
        public string memberName;

        public ComparisonOperator comparison = ComparisonOperator.Equal;
        public NarrativeValue compareTo;

        public override bool Evaluate(NarrativeExecutionContext ctx)
        {
            if (ctx == null)
                return false;

            if (!ctx.TryResolveGameObject(targetKey, out var go) || go == null)
                return false;

            object memberValue = NarrativeReflection.TryGetMemberValue(go, componentTypeName, memberName);
            return NarrativeReflection.Compare(memberValue, comparison, compareTo);
        }
    }

    /// <summary>
    /// Condition: is the resolved position inside a narrative volume at current narrative time?
    /// Uses the 4D query API (stub in Phase 1; implemented in Phase 4-5).
    /// </summary>
    [Serializable]
    public class InsideNarrativeVolumeCondition : NarrativeCondition
    {
        [Tooltip("Key resolved via NarrativeBindings to a GameObject; its transform position is used. If empty, uses first bound GameObject or (0,0,0).")]
        public string positionKey = "player";

        public override bool Evaluate(NarrativeExecutionContext ctx)
        {
            if (ctx == null)
                return false;

            Vector3 position = Vector3.zero;
            if (!string.IsNullOrEmpty(positionKey) && ctx.TryResolveGameObject(positionKey, out var go) && go != null)
                position = go.transform.position;

            float t = ctx.clock != null ? NarrativeCalendarMath.DateTimeToSeconds(ctx.clock.Now) : 0f;
            return NarrativeVolumeQuery.IsInsideNarrativeVolume(position, t);
        }
    }

    /// <summary>
    /// 4D query API. Set by SpatialGenerator4D or 4D grid in Phase 4-5.
    /// </summary>
    public static class NarrativeVolumeQuery
    {
        public static System.Func<Vector3, float, bool> IsInsideNarrativeVolumeImpl;
        /// <summary>When set (e.g. by SpatialGenerator4D), uses schedule padding for active window. Otherwise nominal tStart..tEnd.</summary>
        public static System.Func<float, float, float, bool> IsEventActiveAtImpl;
        /// <summary>When set (e.g. by NarrativeVolumeGrid4D), returns occupancy and causal depth at (position, t).</summary>
        public static System.Func<Vector3, float, (float occupancy, float causalDepth)> Sample4DImpl;

        public static bool IsInsideNarrativeVolume(Vector3 position, float t)
        {
            if (IsInsideNarrativeVolumeImpl != null)
                return IsInsideNarrativeVolumeImpl(position, t);
            return false;
        }

        /// <summary>True if t is inside the event's active window (optionally with schedule padding from 4D generator).</summary>
        public static bool IsEventActiveAt(float tStart, float tEnd, float t)
        {
            if (IsEventActiveAtImpl != null)
                return IsEventActiveAtImpl(tStart, tEnd, t);
            return t >= tStart && t <= tEnd;
        }

        /// <summary>Sample 4D grid at (position, t). Returns occupancy (0..1) and causal depth. Uses Sample4DImpl when set.</summary>
        public static bool Sample4D(Vector3 position, float t, out float occupancy, out float causalDepth)
        {
            occupancy = 0f;
            causalDepth = 0f;
            if (Sample4DImpl != null)
            {
                var result = Sample4DImpl(position, t);
                occupancy = result.occupancy;
                causalDepth = result.causalDepth;
                return true;
            }
            return false;
        }
    }
}
