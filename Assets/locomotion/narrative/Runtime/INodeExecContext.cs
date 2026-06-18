using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    public interface INodeExecContext
    {
        NarrativeExecutionContext NarrativeContext { get; }
        NarrativeBindings Bindings { get; }
        float NarrativeTimeSeconds { get; }
        IReadOnlyList<GameObject> ResolvedObjects { get; }
    }

    public sealed class NodeExecContext : INodeExecContext
    {
        public NarrativeExecutionContext NarrativeContext { get; set; }
        public NarrativeBindings Bindings { get; set; }
        public float NarrativeTimeSeconds { get; set; }
        public IReadOnlyList<GameObject> ResolvedObjects { get; set; }
    }
}
