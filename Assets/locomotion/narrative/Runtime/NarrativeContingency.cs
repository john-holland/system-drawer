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
}

