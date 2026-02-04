using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores shader/material key → list of texture/asset dependency keys for the ORM graph.
/// Resolution: load shader by key, for each dependency key load texture, build Material and optionally cache.
/// </summary>
[CreateAssetMenu(fileName = "AssetDependencyGraph", menuName = "Generated/Asset Dependency Graph", order = 52)]
public class AssetDependencyGraph : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string assetKey;
        public List<string> dependencyKeys = new List<string>();
    }

    [Tooltip("Asset key → dependency keys (e.g. shader key → albedo, normal map keys).")]
    public List<Entry> entries = new List<Entry>();

    /// <summary>Register or update (key, dependencyKeys).</summary>
    public void Register(string key, List<string> dependencyKeys)
    {
        if (string.IsNullOrEmpty(key)) return;
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].assetKey, key, StringComparison.OrdinalIgnoreCase))
            {
                entries[i].dependencyKeys = dependencyKeys != null ? new List<string>(dependencyKeys) : new List<string>();
                return;
            }
        }
        entries.Add(new Entry { assetKey = key, dependencyKeys = dependencyKeys != null ? new List<string>(dependencyKeys) : new List<string>() });
    }

    /// <summary>Get dependency keys for an asset key, or null if not found.</summary>
    public List<string> GetDependencies(string key)
    {
        if (entries == null || string.IsNullOrEmpty(key)) return null;
        foreach (var e in entries)
            if (string.Equals(e.assetKey, key, StringComparison.OrdinalIgnoreCase))
                return e.dependencyKeys != null ? new List<string>(e.dependencyKeys) : new List<string>();
        return null;
    }

    /// <summary>Resolve full asset: returns shader path/ref and list of texture paths/refs. Caller builds Material. Returns true if key found.</summary>
    public bool GetFullAssetKeys(string key, out string shaderOrMaterialKey, out List<string> textureKeys)
    {
        shaderOrMaterialKey = key;
        textureKeys = GetDependencies(key);
        return textureKeys != null;
    }
}
