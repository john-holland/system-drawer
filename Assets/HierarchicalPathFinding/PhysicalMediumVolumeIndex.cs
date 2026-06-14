using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Weather;
/// <summary>
/// Runtime index of <see cref="PhysicalMediumVolume"/> for planner medium resolution.
/// On overlapping volumes, prefers smallest volume (highest priority).
/// </summary>
public static class PhysicalMediumVolumeIndex
{
    struct Entry
    {
        public PhysicalMediumVolume volume;
        public Bounds bounds;
        public float volumeSize;
        public PhysicalPathingMedium medium;
    }

    static readonly List<Entry> _entries = new List<Entry>();
    static bool _dirty = true;

    static PhysicalMediumVolumeIndex()
    {
        PhysicalMediumVolume.Changed += _ => _dirty = true;
    }

    public static void RebuildIfDirty()
    {
        if (!_dirty)
            return;
        _dirty = false;
        _entries.Clear();
        var volumes = Object.FindObjectsByType<PhysicalMediumVolume>(FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++)
        {
            PhysicalMediumVolume v = volumes[i];
            if (v == null || !v.isActiveAndEnabled)
                continue;
            Bounds b = v.GetWorldBounds();
            _entries.Add(new Entry
            {
                volume = v,
                bounds = b,
                volumeSize = b.size.x * b.size.y * b.size.z,
                medium = v.medium
            });
        }
    }

    public static bool TryResolveMedium(Vector3 world, out PhysicalPathingMedium medium)
    {
        RebuildIfDirty();
        medium = PhysicalPathingMedium.Unspecified;
        float bestSize = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry e = _entries[i];
            if (!e.bounds.Contains(world))
                continue;
            if (e.volumeSize >= bestSize)
                continue;
            bestSize = e.volumeSize;
            medium = e.medium;
            found = true;
        }

        return found;
    }

    public static PhysicalPathingMedium ResolveSegmentMedium(IReadOnlyList<Vector3> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
            return PhysicalPathingMedium.Unspecified;

        Vector3 mid = waypoints[waypoints.Count / 2];
        return TryResolveMedium(mid, out PhysicalPathingMedium m) ? m : PhysicalPathingMedium.Unspecified;
    }

    /// <summary>When a planet shell grid is registered, resolve altitude band from shell cell indexing.</summary>
    public static bool TryResolveAltitudeBand(Vector3 world, out int altitudeBand)
    {
        altitudeBand = 0;
        Component shellGrid = null;
        if (!SceneServiceLookup.TryResolve("planet.shellGrid", out shellGrid) || shellGrid == null)
            return false;

        MethodInfo tryWorld = shellGrid.GetType().GetMethod("TryWorldToCell");
        if (tryWorld == null)
            return false;

        object[] args = { world, null };
        if (!(bool)tryWorld.Invoke(shellGrid, args))
            return false;

        object cellId = args[1];
        if (cellId == null)
            return false;

        FieldInfo bandField = cellId.GetType().GetField("AltitudeBand");
        if (bandField == null)
            return false;

        altitudeBand = (int)bandField.GetValue(cellId);
        return true;
    }

    public static void Invalidate() => _dirty = true;
}
