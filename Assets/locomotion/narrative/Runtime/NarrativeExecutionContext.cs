using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Runtime context passed through narrative evaluation/execution.
    /// </summary>
    public sealed class NarrativeExecutionContext
    {
        public NarrativeClock clock;
        public NarrativeBindings bindings;
        public object weatherSystem; // WeatherSystem via reflection to avoid compile-time dependency

        public NarrativeExecutionContext(NarrativeClock clock, NarrativeBindings bindings, object weatherSystem)
        {
            this.clock = clock;
            this.bindings = bindings;
            this.weatherSystem = weatherSystem;
        }

        public bool TryResolveObject(string key, out UnityEngine.Object obj)
        {
            if (bindings != null)
                return bindings.TryResolveObject(key, out obj);

            obj = null;
            return false;
        }

        public bool TryResolveGameObject(string key, out GameObject go)
        {
            if (bindings != null)
                return bindings.TryResolveGameObject(key, out go);

            go = null;
            return false;
        }

        public bool TryResolveAnimationClip(string key, out AnimationClip clip)
        {
            clip = null;
            var resolver = bindings?.GetClipResolver();
            if (resolver == null || string.IsNullOrWhiteSpace(key))
                return false;
            clip = resolver.ResolveClip(key.Trim());
            return clip != null;
        }

        public NarrativeDateTime Now => clock != null ? clock.Now : default;
    }
}

