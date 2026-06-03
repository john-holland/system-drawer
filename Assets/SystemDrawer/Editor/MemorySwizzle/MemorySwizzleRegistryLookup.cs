using System.Collections.Generic;
using UnityEngine;

/// <summary>Maps GameObject instance IDs to SceneObjectRegistry keys.</summary>
public static class MemorySwizzleRegistryLookup
{
    public static Dictionary<int, string> BuildInstanceIdToKeyMap()
    {
        var map = new Dictionary<int, string>();
        var registries = Resources.FindObjectsOfTypeAll<SceneObjectRegistry>();
        for (int r = 0; r < registries.Length; r++)
        {
            var reg = registries[r];
            if (reg == null)
                continue;
            AddEntries(reg.cloneable, map);
            AddEntries(reg.references, map);
        }
        return map;
    }

    static void AddEntries(List<SceneObjectEntry> entries, Dictionary<int, string> map)
    {
        if (entries == null)
            return;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e?.reference == null || string.IsNullOrEmpty(e.key))
                continue;
            int id = e.reference.GetInstanceID();
            if (!map.ContainsKey(id))
                map[id] = e.key.Trim();
        }
    }
}
