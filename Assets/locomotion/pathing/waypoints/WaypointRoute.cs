using System;
using System.Collections.Generic;
using UnityEngine;

public enum TravelGuidanceMode
{
    /// <summary>PC: guide line + optional gambit/stunt target select.</summary>
    PlayerGuide,
    /// <summary>NPC: full TravelAgent + risk pipeline with feature coefficients.</summary>
    NpcFull
}

public enum WaypointVisualMode
{
    Mesh,
    SdfMax
}

/// <summary>0–1 gates for TravelAgent / Stuntman / Safety Warden features.</summary>
[Serializable]
public sealed class TravelFeatureCoefficients
{
    [Range(0f, 1f)] public float stuntman = 1f;
    [Range(0f, 1f)] public float safetyWarden = 1f;
    [Range(0f, 1f)] public float multibody = 1f;
    [Range(0f, 1f)] public float gambitAutoCommit = 1f;
    [Range(0f, 1f)] public float vehicleLegs = 1f;

    public bool AllowStuntman => stuntman >= 0.5f;
    public bool AllowSafetyWarden => safetyWarden >= 0.5f;
    public bool AllowMultibody => multibody >= 0.5f;
    public bool AllowGambitAutoCommit => gambitAutoCommit >= 0.5f;
    public bool AllowVehicleLegs => vehicleLegs >= 0.5f;
}

[Serializable]
public sealed class WaypointMarker
{
    public string id = Guid.NewGuid().ToString("N");
    public string name = "WP";
    public Vector3 worldPosition;
    public GameObject targetActorOrObject;
    public string formationId = "triangle";
    public bool attackMark;
    public WaypointVisualMode visualMode = WaypointVisualMode.Mesh;
    public string animationBtKey = "waypoint.idle";

    public string EffectiveAnimKey =>
        attackMark ? "waypoint.attack" : (string.IsNullOrEmpty(animationBtKey) ? "waypoint.idle" : animationBtKey);
}

[Serializable]
public sealed class WaypointRoute
{
    public string routeId = Guid.NewGuid().ToString("N");
    public List<WaypointMarker> markers = new List<WaypointMarker>();
    public int activeIndex;
    public string defaultFormationId = "triangle";

    public int Count => markers?.Count ?? 0;
    public WaypointMarker Active =>
        markers != null && activeIndex >= 0 && activeIndex < markers.Count ? markers[activeIndex] : null;

    public WaypointMarker Add(Vector3 world, string name = null, string formationId = null)
    {
        if (markers == null) markers = new List<WaypointMarker>();
        var m = new WaypointMarker
        {
            worldPosition = world,
            name = string.IsNullOrEmpty(name) ? $"WP{markers.Count + 1}" : name,
            formationId = string.IsNullOrEmpty(formationId) ? defaultFormationId : formationId
        };
        markers.Add(m);
        activeIndex = markers.Count - 1;
        return m;
    }

    public bool RemoveAt(int index)
    {
        if (markers == null || index < 0 || index >= markers.Count) return false;
        markers.RemoveAt(index);
        activeIndex = Mathf.Clamp(activeIndex, 0, Mathf.Max(0, markers.Count - 1));
        return true;
    }

    public void Clear()
    {
        markers?.Clear();
        activeIndex = 0;
    }

    public void CycleFormationNext(IList<string> catalogIds)
    {
        var m = Active;
        if (m == null || catalogIds == null || catalogIds.Count == 0) return;
        int i = IndexOfId(catalogIds, m.formationId);
        m.formationId = catalogIds[(i + 1) % catalogIds.Count];
    }

    public void CycleFormationPrev(IList<string> catalogIds)
    {
        var m = Active;
        if (m == null || catalogIds == null || catalogIds.Count == 0) return;
        int i = IndexOfId(catalogIds, m.formationId);
        i = (i - 1 + catalogIds.Count) % catalogIds.Count;
        m.formationId = catalogIds[i];
    }

    static int IndexOfId(IList<string> ids, string id)
    {
        for (int i = 0; i < ids.Count; i++)
            if (string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    public List<Vector3> Polyline()
    {
        var list = new List<Vector3>();
        if (markers == null) return list;
        for (int i = 0; i < markers.Count; i++)
            if (markers[i] != null) list.Add(markers[i].worldPosition);
        return list;
    }
}
