using System;
using System.Collections.Generic;
using Locomotion.Narrative;

namespace Locomotion.Liquid
{
    /// <summary>Resolves partially-raise-amount from lemmas almost+mouth/lips and stalled.</summary>
    public static class LiquidPartialRaiseResolver
    {
        public const int DefaultAdjacencyChars = 12;

        public static float Resolve(
            DrinkLemmaProperties props,
            IReadOnlyList<PromptSegment> segments,
            IReadOnlyList<LocalizationClauseBindingRecord> bindings,
            string activeScriptText,
            int adjacencyChars = DefaultAdjacencyChars)
        {
            float explicitAmount = props.partiallyRaiseAmount;
            if (explicitAmount < 1f - 1e-4f && explicitAmount > 0f)
                return UnityEngine.Mathf.Clamp01(explicitAmount);

            if (ContainsLemma(activeScriptText, bindings, "stalled"))
                return UnityEngine.Mathf.Min(1f, props.partialRaiseDefaultWhenStalled);

            if (ContainsLemma(activeScriptText, bindings, "almost") &&
                (ContainsLemma(activeScriptText, bindings, "mouth") || ContainsLemma(activeScriptText, bindings, "lips")))
                return UnityEngine.Mathf.Min(1f, props.partialRaiseDefaultWhenStalled);

            if (ContainsLemma(activeScriptText, bindings, "endless"))
                return 1f;

            return 1f;
        }

        public static bool ShouldSuppressDispense(DrinkLemmaProperties props, string activeScriptText,
            IReadOnlyList<LocalizationClauseBindingRecord> bindings)
        {
            if (props.closureMode == DrinkClosureMode.Stalled)
                return true;
            if (ContainsLemma(activeScriptText, bindings, "stalled"))
                return true;
            float raise = Resolve(props, null, bindings, activeScriptText);
            return raise < 1f - 1e-4f && !ContainsLemma(activeScriptText, bindings, "drink");
        }

        static bool ContainsLemma(string script, IReadOnlyList<LocalizationClauseBindingRecord> bindings, string term)
        {
            if (!string.IsNullOrEmpty(script) &&
                script.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (bindings == null)
                return false;
            foreach (var b in bindings)
            {
                if (b == null)
                    continue;
                if (!string.IsNullOrEmpty(b.selectionText) &&
                    b.selectionText.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
