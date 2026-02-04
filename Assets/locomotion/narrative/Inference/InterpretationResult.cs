using System;
using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>Result of interpreting a prompt asset: events, bindings, generation requests. Stored keyed by asset.</summary>
    [Serializable]
    public class InterpretationResult
    {
        public List<InterpretedEvent> events = new List<InterpretedEvent>();
        public List<InterpretedEventBinding> bindings = new List<InterpretedEventBinding>();
        public List<GenerationRequest> generationRequests = new List<GenerationRequest>();

        public void Clear()
        {
            events.Clear();
            bindings.Clear();
            generationRequests.Clear();
        }
    }
}
