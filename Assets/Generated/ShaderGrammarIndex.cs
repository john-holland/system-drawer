using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps linguistic terms (e.g. adjectives: icy, wet, dry, hot, cold) to shader property names or slot ids
/// so the LM prompt and dependency list stay consistent.
/// </summary>
[CreateAssetMenu(fileName = "ShaderGrammarIndex", menuName = "Generated/Shader Grammar Index", order = 50)]
public class ShaderGrammarIndex : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Term (e.g. icy, wet, dry, hot, cold).")]
        public string term = "";
        [Tooltip("Optional role tag (e.g. adjective, material) for filtering.")]
        public string role = "";
        [Tooltip("Shader property name (e.g. _Wetness, _IceTint) or slot id (e.g. albedo, normal, specular).")]
        public string shaderPropertyOrSlot = "";
    }

    [Tooltip("List of term → property/slot mappings.")]
    public List<Entry> entries = new List<Entry>();

    /// <summary>Get all entries; optionally filter by role.</summary>
    public IEnumerable<Entry> GetEntries(string roleFilter = null)
    {
        if (entries == null) yield break;
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.term)) continue;
            if (!string.IsNullOrEmpty(roleFilter) && !string.Equals(e.role, roleFilter, StringComparison.OrdinalIgnoreCase))
                continue;
            yield return e;
        }
    }

    /// <summary>Build a short "allowed properties / slots" string for the LM prompt.</summary>
    public string ToPromptSpec(int maxEntries = 20)
    {
        var list = new List<string>();
        foreach (var e in GetEntries())
        {
            if (list.Count >= maxEntries) break;
            var line = string.IsNullOrEmpty(e.role)
                ? string.Format("{0} -> {1}", e.term, e.shaderPropertyOrSlot)
                : string.Format("{0} ({1}) -> {2}", e.term, e.role, e.shaderPropertyOrSlot);
            list.Add(line);
        }
        if (list.Count == 0) return "";
        return "Allowed terms -> properties/slots:\n" + string.Join("\n", list);
    }
}
