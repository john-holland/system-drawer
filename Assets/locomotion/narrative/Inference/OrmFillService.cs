using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative.Serialization;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Resolve interpreted event titles/phrases against SceneObjectRegistry and produce bindings (matched key or status).
    /// </summary>
    public static class OrmFillService
    {
        /// <summary>
        /// For each event title (and optionally phrase), try to resolve against registry. Fills outBindings with Matched, UnderstoodNoOrmMatch, or MarkedGenerate.
        /// </summary>
        public static void FillFromRegistry(
            IList<InterpretedEvent> events,
            SceneObjectRegistry registry,
            HashSet<int> markedGenerateEventIndices,
            List<InterpretedEventBinding> outBindings)
        {
            outBindings?.Clear();
            if (outBindings == null) return;

            if (registry == null)
            {
                for (int i = 0; i < (events?.Count ?? 0); i++)
                    outBindings.Add(InterpretedEventBinding.NoMatch(i, events[i].title));
                return;
            }

            for (int i = 0; i < (events?.Count ?? 0); i++)
            {
                var ev = events[i];
                string phrase = (ev.title ?? "").Trim();
                if (markedGenerateEventIndices != null && markedGenerateEventIndices.Contains(i))
                {
                    outBindings.Add(InterpretedEventBinding.Generate(i, phrase));
                    continue;
                }
                string resolvedKey = registry.ResolveKey(phrase);
                if (!string.IsNullOrEmpty(resolvedKey))
                    outBindings.Add(InterpretedEventBinding.Matched(i, phrase, resolvedKey));
                else
                {
                    if (!string.IsNullOrEmpty(phrase))
                    {
                        var words = NarrativeLSTMTokenizer.TokenizeText(phrase);
                        for (int w = words.Length; w >= 1; w--)
                        {
                            string sub = string.Join(" ", words, 0, w);
                            resolvedKey = registry.ResolveKey(sub);
                            if (!string.IsNullOrEmpty(resolvedKey))
                            {
                                outBindings.Add(InterpretedEventBinding.Matched(i, phrase, resolvedKey));
                                break;
                            }
                        }
                        if (outBindings.Count <= i)
                            outBindings.Add(InterpretedEventBinding.NoMatch(i, phrase));
                    }
                    else
                        outBindings.Add(InterpretedEventBinding.NoMatch(i, phrase));
                }
            }
        }
    }
}
