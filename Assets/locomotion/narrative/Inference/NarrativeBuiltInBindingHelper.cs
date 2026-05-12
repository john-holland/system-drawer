using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Maps interpreted event titles to Continuum built-in URNs before SceneObjectRegistry resolution.
    /// </summary>
    public static class NarrativeBuiltInBindingHelper
    {
        public static Dictionary<int, InterpretedEventBinding> BuildBuiltInBindings(IList<InterpretedEvent> events)
        {
            var map = new Dictionary<int, InterpretedEventBinding>();
            if (events == null) return map;
            for (int i = 0; i < events.Count; i++)
            {
                string title = events[i].title;
                if (VocabularyBuiltInLookup.TryResolvePhrase(title, out var d))
                    map[i] = InterpretedEventBinding.BuiltIn(i, title, d.Id, d.Category);
            }
            return map;
        }
    }
}
