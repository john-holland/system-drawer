using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dish pit: inventory + zone stacks with Towers-of-Hanoi soil rules.
/// Zone order nearest→furthest from trash for highest clean-dish throughput.
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Dish Washing Station")]
public sealed class DishWashingStation : MonoBehaviour
{
    public DishWashingStationConfig config;
    public Transform trashAnchor;
    public Vector3 kitchenLateral = Vector3.right;
    public InventoryManager inventory;
    public int pendingDirtySeed = 2;
    public readonly List<DishZoneBinding> runtimeZones = new List<DishZoneBinding>();

    void Awake()
    {
        EnsureZones();
    }

    public void EnsureZones()
    {
        runtimeZones.Clear();
        if (config != null)
        {
            config.EnsureStandardZones();
            for (int i = 0; i < config.zones.Count; i++)
            {
                if (config.zones[i] == null) continue;
                if (!config.enableCompostZone && config.zones[i].kind == DishZoneKind.Compost)
                    continue;
                runtimeZones.Add(config.zones[i]);
            }
        }
        else
        {
            runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dirty });
            runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Sink });
            runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dishwasher });
            runtimeZones.Add(new DishZoneBinding { kind = DishZoneKind.Dry });
        }
        SortZonesNearestTrash();
    }

    /// <summary>Sort zone anchors along trash→lateral axis (Dirty nearest).</summary>
    public void SortZonesNearestTrash()
    {
        Vector3 origin = trashAnchor != null ? trashAnchor.position : transform.position;
        Vector3 axis = kitchenLateral.sqrMagnitude > 0.0001f ? kitchenLateral.normalized : transform.right;
        runtimeZones.Sort((a, b) =>
        {
            float da = Project(a, origin, axis);
            float db = Project(b, origin, axis);
            int soil = SoilRank(a.kind).CompareTo(SoilRank(b.kind));
            if (a.anchor != null && b.anchor != null && Mathf.Abs(da - db) > 0.01f)
                return da.CompareTo(db);
            return soil;
        });
    }

    static float Project(DishZoneBinding z, Vector3 origin, Vector3 axis)
    {
        if (z?.anchor == null) return SoilRank(z != null ? z.kind : DishZoneKind.Dirty);
        return Vector3.Dot(z.anchor.position - origin, axis);
    }

    public static int SoilRank(DishZoneKind kind)
    {
        switch (kind)
        {
            case DishZoneKind.Compost: return -1;
            case DishZoneKind.Dirty: return 0;
            case DishZoneKind.Sink: return 1;
            case DishZoneKind.Dishwasher: return 2;
            case DishZoneKind.Dry: return 3;
            default: return 0;
        }
    }

    public DishZoneBinding GetZone(DishZoneKind kind)
    {
        for (int i = 0; i < runtimeZones.Count; i++)
            if (runtimeZones[i] != null && runtimeZones[i].kind == kind)
                return runtimeZones[i];
        return null;
    }

    public int SeedDirtyFromService(int count)
    {
        var dirty = GetZone(DishZoneKind.Dirty);
        if (dirty == null) return 0;
        int n = Mathf.Max(0, count);
        for (int i = 0; i < n; i++)
            dirty.stack.Add($"dish_{Guid.NewGuid().ToString("N").Substring(0, 6)}");
        GetComponent<DishWashingStationBioRhythm>()?.NotifyDirtySeeded(n);
        return n;
    }

    public bool TryPeekTop(DishZoneKind kind, out string dishId)
    {
        dishId = null;
        var z = GetZone(kind);
        if (z == null || z.stack.Count == 0) return false;
        dishId = z.stack[z.stack.Count - 1];
        return true;
    }

    /// <summary>Legal forward Hanoi move along soil gradient (or compost scrap before sink).</summary>
    public static bool IsLegalMove(DishZoneKind from, DishZoneKind to, bool compostEnabled)
    {
        if (from == to) return false;
        if (to == DishZoneKind.Compost)
            return compostEnabled && from == DishZoneKind.Dirty;
        if (from == DishZoneKind.Compost) return false;
        int a = SoilRank(from);
        int b = SoilRank(to);
        // Forward wash steps only (allow +1 or Dirty→Sink→…); never place dirtier onto cleaner wrongly
        return b == a + 1 || (from == DishZoneKind.Sink && to == DishZoneKind.Dry);
    }

    public bool TryMove(string dishId, DishZoneKind from, DishZoneKind to, out string error)
    {
        error = null;
        bool compost = config != null && config.enableCompostZone;
        if (!IsLegalMove(from, to, compost))
        {
            error = $"illegal Hanoi move {from}→{to}";
            return false;
        }
        var src = GetZone(from);
        var dst = GetZone(to);
        if (src == null || dst == null)
        {
            error = "zone missing";
            return false;
        }
        if (src.stack.Count == 0)
        {
            error = "empty source";
            return false;
        }
        string top = src.stack[src.stack.Count - 1];
        if (!string.IsNullOrEmpty(dishId) && !string.Equals(top, dishId, StringComparison.Ordinal))
        {
            error = "not top of stack";
            return false;
        }
        src.stack.RemoveAt(src.stack.Count - 1);
        dst.stack.Add(top);
        GetComponent<DishWashingStationBioRhythm>()?.NotifyMove(from, to);
        if (inventory != null)
        {
            inventory.NoteScriptMention(top);
            var item = inventory.FindByName(top) ?? new InventoryItem { id = top, name = top };
            item.contextGameObject = dst.anchor != null ? dst.anchor.gameObject : gameObject;
            item.contextPath = to.ToString();
            inventory.UpsertLocal(item);
        }
        return true;
    }

    public int Count(DishZoneKind kind)
    {
        var z = GetZone(kind);
        return z != null ? z.stack.Count : 0;
    }
}
