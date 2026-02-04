using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keyed storage for primitive assets (Prompt, Image, Video, Sound) so every pipeline has traceable inputs.
/// Store under Assets/Generated/Primitives (or configurable). One store asset per project.
/// </summary>
public class PrimitiveAssetStore : ScriptableObject
{
    [Tooltip("Root folder for primitive assets (e.g. Assets/Generated/Primitives).")]
    public string primitivesFolder = "Assets/Generated/Primitives";

    [Tooltip("All entries. Newest last.")]
    public List<PrimitiveAssetEntry> entries = new List<PrimitiveAssetEntry>();

    /// <summary>Get entry by key (case-insensitive).</summary>
    public PrimitiveAssetEntry GetByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null) return null;
        var k = key.Trim();
        foreach (var e in entries)
            if (e != null && string.Equals(e.key, k, StringComparison.OrdinalIgnoreCase))
                return e;
        return null;
    }

    /// <summary>Add or replace entry with same key. Returns the entry.</summary>
    public PrimitiveAssetEntry AddOrReplace(PrimitiveAssetEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.key)) return null;
        if (entries == null) entries = new List<PrimitiveAssetEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && string.Equals(entries[i].key, entry.key, StringComparison.OrdinalIgnoreCase))
            {
                entries[i] = entry;
                return entry;
            }
        }
        entries.Add(entry);
        return entry;
    }

    /// <summary>Remove entry by key. Returns true if removed.</summary>
    public bool RemoveByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null) return false;
        var k = key.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && string.Equals(entries[i].key, k, StringComparison.OrdinalIgnoreCase))
            {
                entries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
