using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene index of <see cref="ParkingZoneVolume"/> for terminal placement search.</summary>
public static class ParkingZoneIndex
{
    struct Entry
    {
        public ParkingZoneVolume zone;
        public Bounds bounds;
    }

    static readonly List<Entry> _entries = new List<Entry>();
    static bool _dirty = true;

    static ParkingZoneIndex()
    {
        ParkingZoneVolume.Changed += _ => _dirty = true;
    }

    public static void RebuildIfDirty()
    {
        if (!_dirty)
            return;
        _dirty = false;
        _entries.Clear();
        var zones = Object.FindObjectsByType<ParkingZoneVolume>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            ParkingZoneVolume z = zones[i];
            if (z == null || !z.isActiveAndEnabled)
                continue;
            _entries.Add(new Entry { zone = z, bounds = z.GetWorldBounds() });
        }
    }

    public static List<ParkingZoneVolume> QueryNear(Vector3 world, float radius, TravelLegMode? legFilter = null)
    {
        RebuildIfDirty();
        float r2 = radius * radius;
        var result = new List<ParkingZoneVolume>();
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry e = _entries[i];
            if (e.zone == null)
                continue;
            Vector3 closest = e.bounds.ClosestPoint(world);
            if ((closest - world).sqrMagnitude > r2 && !e.bounds.Contains(world))
                continue;
            if (legFilter.HasValue && !e.zone.AllowsLeg(legFilter.Value))
                continue;
            result.Add(e.zone);
        }
        return result;
    }

    public static List<ParkingZoneVolume> QueryNearGoal(Vector3 goalHint, float radius, TravelLegMode? legFilter = null)
    {
        return QueryNear(goalHint, radius, legFilter);
    }
}
