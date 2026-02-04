using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Scene-side bindings that map stable string keys (stored in Narrative assets) to actual Unity objects.
    /// This supports the hybrid authoring model: assets stay scene-agnostic; scenes provide bindings.
    /// </summary>
    public class NarrativeBindings : MonoBehaviour
    {
        [Serializable]
        public class BindingEntry
        {
            public string key;
            public UnityEngine.Object value;
        }

        public List<BindingEntry> bindings = new List<BindingEntry>();

        [Tooltip("Optional: resolve animation clip by key (e.g. generator key + '_clip'). Assign an AssetLoader or other IAnimationClipResolver.")]
        public MonoBehaviour clipResolver;

        private readonly Dictionary<string, UnityEngine.Object> index = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);

        private void OnEnable()
        {
            RebuildIndex();
        }

        public void RebuildIndex()
        {
            index.Clear();
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b == null) continue;
                if (string.IsNullOrWhiteSpace(b.key)) continue;
                if (b.value == null) continue;
                index[b.key.Trim()] = b.value;
            }
        }

        public bool TryResolveObject(string key, out UnityEngine.Object obj)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                obj = null;
                return false;
            }

            if (index.Count != bindings.Count)
                RebuildIndex();

            return index.TryGetValue(key.Trim(), out obj);
        }

        public bool TryResolveGameObject(string key, out GameObject go)
        {
            if (TryResolveObject(key, out var obj))
            {
                if (obj is GameObject g)
                {
                    go = g;
                    return true;
                }

                if (obj is Component c)
                {
                    go = c.gameObject;
                    return true;
                }
            }

            go = null;
            return false;
        }

        public IAnimationClipResolver GetClipResolver()
        {
            return clipResolver != null ? clipResolver as IAnimationClipResolver : null;
        }
    }
}

