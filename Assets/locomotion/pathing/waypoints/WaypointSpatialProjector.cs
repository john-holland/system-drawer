using System.Collections.Generic;
using UnityEngine;

/// <summary>Projects waypoint markers into 2D/3D/4D SpatialGenerator hosts for map/UI pieces.</summary>
[AddComponentMenu("Locomotion/Waypoints/Spatial Projector")]
public sealed class WaypointSpatialProjector : MonoBehaviour
{
    public WaypointRoute route;
    public UnityEngine.Object spatialGenerator2D;
    public UnityEngine.Object spatialGenerator3D;
    public UnityEngine.Object spatialGenerator4D;
    public string mapLayerKey = "waypoints";
    public bool projectAttackMarks = true;

    readonly List<Vector3> _lastProjected = new List<Vector3>();

    public IReadOnlyList<Vector3> LastProjected => _lastProjected;

    public void Project(WaypointRoute r = null)
    {
        route = r ?? route;
        _lastProjected.Clear();
        if (route?.markers == null) return;
        for (int i = 0; i < route.markers.Count; i++)
        {
            var m = route.markers[i];
            if (m == null) continue;
            if (m.attackMark && !projectAttackMarks) continue;
            _lastProjected.Add(m.worldPosition);
            EmitToHost(spatialGenerator2D, m, "2d");
            EmitToHost(spatialGenerator3D, m, "3d");
            EmitToHost(spatialGenerator4D, m, "4d");
        }
    }

    void EmitToHost(Object host, WaypointMarker m, string mode)
    {
        if (host == null || m == null) return;
        // Soft contract: hosts may implement ProjectWaypoint(string layer, Vector3, string id, bool attack)
        var mb = host as MonoBehaviour;
        if (mb != null)
        {
            mb.SendMessage(
                "ProjectWaypoint",
                new object[] { mapLayerKey, m.worldPosition, m.name, m.attackMark, mode },
                SendMessageOptions.DontRequireReceiver);
        }
        // Also try SpatialGenerator slot on TravelAgent-style Object refs
        var go = host as GameObject;
        if (go != null)
        {
            go.SendMessage(
                "ProjectWaypoint",
                new object[] { mapLayerKey, m.worldPosition, m.name, m.attackMark, mode },
                SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>Flat list suitable for R6-style 2D map pins.</summary>
    public List<(string name, Vector2 xz, bool attack)> ToMapPins()
    {
        var list = new List<(string, Vector2, bool)>();
        if (route?.markers == null) return list;
        for (int i = 0; i < route.markers.Count; i++)
        {
            var m = route.markers[i];
            if (m == null) continue;
            list.Add((m.name, new Vector2(m.worldPosition.x, m.worldPosition.z), m.attackMark));
        }
        return list;
    }
}
