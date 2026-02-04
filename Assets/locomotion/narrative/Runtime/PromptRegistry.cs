using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Registry of prompt assets by key and synonyms (ORM-style). Resolve by key or synonym to get NarrativePromptAsset.
    /// </summary>
    public class PromptRegistry : ScriptableObject
    {
        [Tooltip("Registered prompt assets. Keys and synonyms are built into lookup at runtime.")]
        public List<NarrativePromptAsset> prompts = new List<NarrativePromptAsset>();

        private Dictionary<string, NarrativePromptAsset> _keyToPrompt;
        private bool _dirty = true;

        private void BuildLookups()
        {
            if (!_dirty) return;
            _keyToPrompt = new Dictionary<string, NarrativePromptAsset>(StringComparer.OrdinalIgnoreCase);
            if (prompts == null) return;
            foreach (var p in prompts)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.key)) continue;
                var k = p.key.Trim();
                _keyToPrompt[k] = p;
                if (p.synonyms != null)
                {
                    foreach (var s in p.synonyms)
                    {
                        var sym = (s ?? "").Trim();
                        if (!string.IsNullOrEmpty(sym))
                            _keyToPrompt[sym] = p;
                    }
                }
            }
            _dirty = false;
        }

        /// <summary>Resolve by key or synonym. Returns the prompt asset or null.</summary>
        public NarrativePromptAsset Resolve(string keyOrSynonym)
        {
            if (string.IsNullOrWhiteSpace(keyOrSynonym)) return null;
            BuildLookups();
            var key = keyOrSynonym.Trim();
            return _keyToPrompt != null && _keyToPrompt.TryGetValue(key, out var p) ? p : null;
        }

        /// <summary>Register an asset. Adds to list and invalidates lookups. If key already exists, replaces.</summary>
        public void Register(NarrativePromptAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.key)) return;
            if (prompts == null) prompts = new List<NarrativePromptAsset>();
            var k = asset.key.Trim();
            for (int i = 0; i < prompts.Count; i++)
            {
                if (prompts[i] != null && string.Equals(prompts[i].key, k, StringComparison.OrdinalIgnoreCase))
                {
                    prompts[i] = asset;
                    _dirty = true;
                    return;
                }
            }
            prompts.Add(asset);
            _dirty = true;
        }

        /// <summary>Remove asset by key. Returns true if removed.</summary>
        public bool RemoveByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || prompts == null) return false;
            var k = key.Trim();
            for (int i = 0; i < prompts.Count; i++)
            {
                if (prompts[i] != null && string.Equals(prompts[i].key, k, StringComparison.OrdinalIgnoreCase))
                {
                    prompts.RemoveAt(i);
                    _dirty = true;
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _dirty = true;
        }
#endif
    }
}
