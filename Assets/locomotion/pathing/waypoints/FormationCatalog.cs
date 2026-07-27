using System.Collections.Generic;
using UnityEngine;

/// <summary>Named formation catalog over TravelFormationAsset entries.</summary>
[CreateAssetMenu(fileName = "FormationCatalog", menuName = "Locomotion/Travel/Formation Catalog", order = 51)]
public sealed class FormationCatalog : ScriptableObject
{
    [System.Serializable]
    public sealed class Entry
    {
        public string id = "triangle";
        public string displayName = "Triangle";
        public TravelFormationAsset asset;
    }

    public List<Entry> entries = new List<Entry>
    {
        new Entry { id = "triangle", displayName = "Triangle" },
        new Entry { id = "pineapple", displayName = "Pineapple" },
        new Entry { id = "divide_and_conquer", displayName = "Divide and Conquer" }
    };

    public IList<string> Ids
    {
        get
        {
            var ids = new List<string>();
            if (entries == null) return ids;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && !string.IsNullOrEmpty(entries[i].id))
                    ids.Add(entries[i].id);
            return ids;
        }
    }

    public bool TryGet(string id, out TravelFormationAsset asset)
    {
        asset = null;
        if (entries == null || string.IsNullOrEmpty(id)) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null) continue;
            if (string.Equals(e.id, id, System.StringComparison.OrdinalIgnoreCase))
            {
                asset = e.asset;
                return asset != null;
            }
        }
        return false;
    }

    public string NormalizeId(string id)
    {
        if (TryGet(id, out _)) return id;
        return entries != null && entries.Count > 0 && entries[0] != null ? entries[0].id : "triangle";
    }
}
