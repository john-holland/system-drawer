using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers room / nested inventory contexts by path convention:
/// bedroom2 contains bedroom2_dresser2 (parent hierarchy + InventoryManager context).
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Inventory Binder")]
public sealed class HouseInventoryBinder : MonoBehaviour
{
    public Transform roomsRoot;
    public readonly Dictionary<string, GameObject> contexts = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (roomsRoot == null) roomsRoot = transform;
        Rebuild();
    }

    public void Rebuild()
    {
        contexts.Clear();
        if (roomsRoot == null) return;
        var transforms = roomsRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var t = transforms[i];
            if (t == null || t == roomsRoot) continue;
            string path = BuildPath(t);
            contexts[path] = t.gameObject;
            contexts[t.name] = t.gameObject;
        }
    }

    string BuildPath(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        while (cur != null && cur != roomsRoot)
        {
            parts.Add(cur.name);
            cur = cur.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    public bool TryGetContext(string nameOrPath, out GameObject go) =>
        contexts.TryGetValue(nameOrPath, out go);

    /// <summary>True when child path is nested under parent room (bedroom2_dresser2 under bedroom2).</summary>
    public static bool IsNestedInventoryName(string parentRoom, string childName)
    {
        if (string.IsNullOrEmpty(parentRoom) || string.IsNullOrEmpty(childName)) return false;
        return childName.StartsWith(parentRoom + "_", System.StringComparison.OrdinalIgnoreCase)
               || childName.StartsWith(parentRoom + "/", System.StringComparison.OrdinalIgnoreCase);
    }
}
