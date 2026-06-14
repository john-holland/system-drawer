#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SpatialVolumes;
using Weather;

/// <summary>Discovery table for physics bridge components in the open scenes.</summary>
public static class PhysicsBridgeRegistry
{
    public enum BridgeCoordinateFrame
    {
        Unspecified,
        RoadFrenet,
        SphericalPlanet,
        GridAdvection,
        RelativityShell,
        VehicleLocal,
        SdfMeshLocal,
        BoneLocal,
        Spatial4DGateway
    }

    public struct BridgeRow
    {
        public UnityEngine.Object source;
        public string sourceTypeName;
        public BridgeCoordinateFrame fromFrame;
        public WeatherPhysicsManifold targetManifold;
        public string lastStampLabel;
        public bool active;
    }

    public static IReadOnlyList<BridgeRow> DiscoverActiveBridges()
    {
        var rows = new List<BridgeRow>();
        DiscoverType<Roads.RoadPhysicsManifoldBridge>(rows, BridgeCoordinateFrame.RoadFrenet, "RoadPhysicsManifoldBridge");
        DiscoverType<Planetary.Bridges.PlanetPhysicsManifoldBridge>(rows, BridgeCoordinateFrame.SphericalPlanet, "PlanetPhysicsManifoldBridge");
        DiscoverType<Planetary.Bridges.PlanetShellManifoldGrid>(rows, BridgeCoordinateFrame.SphericalPlanet, "PlanetShellManifoldGrid");
        DiscoverMonoByName(rows, "LavaPhysicsManifold", BridgeCoordinateFrame.GridAdvection);
        DiscoverMonoByName(rows, "PhysicalManifold", BridgeCoordinateFrame.RelativityShell);
        DiscoverMonoByName(rows, "PlanetSpacetimeEnvelope", BridgeCoordinateFrame.RelativityShell);
        DiscoverMonoByName(rows, "VehiclePhysicsManifoldSlot", BridgeCoordinateFrame.VehicleLocal);
        DiscoverType<SpatialVolumeProvider>(rows, BridgeCoordinateFrame.SdfMeshLocal, "SpatialVolumeProvider");
        DiscoverMonoByName(rows, "Spatial4DMirrorNode", BridgeCoordinateFrame.Spatial4DGateway);
        DiscoverRagdollJointVentures(rows);
        return rows;
    }

    static void DiscoverType<T>(List<BridgeRow> rows, BridgeCoordinateFrame frame, string label) where T : UnityEngine.Object
    {
        var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            T src = found[i];
            if (src == null)
                continue;
            rows.Add(BuildRow(src, label, frame, ResolveManifold(src)));
        }
    }

    static void DiscoverMonoByName(List<BridgeRow> rows, string typeName, BridgeCoordinateFrame frame)
    {
        foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb.GetType().Name != typeName)
                continue;
            rows.Add(BuildRow(mb, typeName, frame, ResolveManifold(mb)));
        }
    }

    static void DiscoverRagdollJointVentures(List<BridgeRow> rows)
    {
        foreach (RagdollSystem rs in UnityEngine.Object.FindObjectsByType<RagdollSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rs == null)
                continue;
            int jointCount = rs.GetComponentsInChildren<ConfigurableJoint>(true).Length;
            if (jointCount == 0)
                continue;
            rows.Add(new BridgeRow
            {
                source = rs,
                sourceTypeName = $"Ragdoll joints ({jointCount})",
                fromFrame = BridgeCoordinateFrame.BoneLocal,
                targetManifold = null,
                lastStampLabel = "read-only",
                active = rs.isActiveAndEnabled
            });
        }
    }

    static BridgeRow BuildRow(UnityEngine.Object src, string typeName, BridgeCoordinateFrame frame, WeatherPhysicsManifold manifold)
    {
        return new BridgeRow
        {
            source = src,
            sourceTypeName = typeName,
            fromFrame = frame,
            targetManifold = manifold,
            lastStampLabel = EditorApplication.timeSinceStartup > 0 ? "scene" : "—",
            active = src is Behaviour b ? b.isActiveAndEnabled : true
        };
    }

    static WeatherPhysicsManifold ResolveManifold(UnityEngine.Object src)
    {
        FieldInfo f = src.GetType().GetField("manifold", BindingFlags.Instance | BindingFlags.Public);
        if (f != null && f.GetValue(src) is WeatherPhysicsManifold m)
            return m;
        PropertyInfo p = src.GetType().GetProperty("manifold", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.GetValue(src) is WeatherPhysicsManifold m2)
            return m2;
        return null;
    }

    public static bool ValidateRow(in BridgeRow row, out string message)
    {
        if (row.source == null)
        {
            message = "Missing source.";
            return false;
        }

        if (row.fromFrame == BridgeCoordinateFrame.BoneLocal)
        {
            message = "Ragdoll venture (read-only).";
            return true;
        }

        if (row.targetManifold == null)
        {
            message = "No WeatherPhysicsManifold assigned.";
            return false;
        }

        message = "OK";
        return true;
    }
}
#endif
