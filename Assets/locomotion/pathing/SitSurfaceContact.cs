using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Occupiable surface contact: plane + polygon (or AABB fallback) on a host transform/rigidbody.
/// Works for chair seats, book tops, wall ledges, stacked chairs.
/// </summary>
[Serializable]
public sealed class SitSurfaceContact
{
    public Transform host;
    public Rigidbody hostBody;
    public Vector3 localPlanePoint = Vector3.zero;
    public Vector3 localPlaneNormal = Vector3.up;
    public List<Vector3> localPolygon = new List<Vector3>();
    public float halfExtentX = 0.25f;
    public float halfExtentZ = 0.25f;

    public Vector3 WorldPlanePoint =>
        host != null ? host.TransformPoint(localPlanePoint) : localPlanePoint;

    public Vector3 WorldPlaneNormal
    {
        get
        {
            Vector3 n = host != null ? host.TransformDirection(localPlaneNormal) : localPlaneNormal;
            return n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.up;
        }
    }

    public Quaternion WorldSeatRotation =>
        host != null
            ? Quaternion.LookRotation(
                Vector3.ProjectOnPlane(host.forward, WorldPlaneNormal).normalized.sqrMagnitude > 1e-6f
                    ? Vector3.ProjectOnPlane(host.forward, WorldPlaneNormal).normalized
                    : Vector3.Cross(WorldPlaneNormal, Vector3.right).normalized,
                WorldPlaneNormal)
            : Quaternion.identity;

    public static SitSurfaceContact FromWorldPlane(Transform host, Vector3 worldPoint, Vector3 worldNormal, float halfX = 0.25f, float halfZ = 0.25f)
    {
        var c = new SitSurfaceContact
        {
            host = host,
            hostBody = host != null ? host.GetComponentInParent<Rigidbody>() : null,
            halfExtentX = halfX,
            halfExtentZ = halfZ
        };
        if (host != null)
        {
            c.localPlanePoint = host.InverseTransformPoint(worldPoint);
            c.localPlaneNormal = host.InverseTransformDirection(worldNormal.normalized);
        }
        else
        {
            c.localPlanePoint = worldPoint;
            c.localPlaneNormal = worldNormal.normalized;
        }
        c.EnsureDefaultPolygon();
        return c;
    }

    public void EnsureDefaultPolygon()
    {
        if (localPolygon != null && localPolygon.Count >= 3)
            return;
        localPolygon = new List<Vector3>
        {
            localPlanePoint + new Vector3(-halfExtentX, 0f, -halfExtentZ),
            localPlanePoint + new Vector3(halfExtentX, 0f, -halfExtentZ),
            localPlanePoint + new Vector3(halfExtentX, 0f, halfExtentZ),
            localPlanePoint + new Vector3(-halfExtentX, 0f, halfExtentZ)
        };
    }

    public List<Vector3> GetWorldPolygon()
    {
        EnsureDefaultPolygon();
        var world = new List<Vector3>(localPolygon.Count);
        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector3 lp = localPolygon[i];
            world.Add(host != null ? host.TransformPoint(lp) : lp);
        }
        return world;
    }

    /// <summary>Project a world point onto the contact plane.</summary>
    public Vector3 ProjectOntoPlane(Vector3 worldPoint)
    {
        Vector3 p = WorldPlanePoint;
        Vector3 n = WorldPlaneNormal;
        return worldPoint - n * Vector3.Dot(worldPoint - p, n);
    }

    /// <summary>
    /// Returns signed distance of projected CoG from polygon center, and whether projection is inside polygon (XZ in plane space).
    /// </summary>
    public bool TryProjectCog(Vector3 worldCog, out Vector3 projected, out float tipRisk01)
    {
        projected = ProjectOntoPlane(worldCog);
        var poly = GetWorldPolygon();
        if (poly.Count < 3)
        {
            tipRisk01 = 1f;
            return false;
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < poly.Count; i++)
            center += poly[i];
        center /= poly.Count;

        bool inside = PointInPolygonProjected(projected, poly, WorldPlaneNormal);
        float maxR = 0.01f;
        for (int i = 0; i < poly.Count; i++)
            maxR = Mathf.Max(maxR, Vector3.Distance(center, poly[i]));
        float dist = Vector3.Distance(projected, center);
        tipRisk01 = inside
            ? Mathf.Clamp01(dist / maxR)
            : Mathf.Clamp01(1f + (dist - maxR) / maxR);
        return inside;
    }

    static bool PointInPolygonProjected(Vector3 point, List<Vector3> poly, Vector3 normal)
    {
        // Build orthonormal basis on plane and do 2D winding.
        Vector3 n = normal.normalized;
        Vector3 t = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 b = Vector3.Cross(n, t);
        float px = Vector3.Dot(point, t);
        float py = Vector3.Dot(point, b);
        bool inside = false;
        int j = poly.Count - 1;
        for (int i = 0; i < poly.Count; i++)
        {
            float xi = Vector3.Dot(poly[i], t);
            float yi = Vector3.Dot(poly[i], b);
            float xj = Vector3.Dot(poly[j], t);
            float yj = Vector3.Dot(poly[j], b);
            bool intersect = ((yi > py) != (yj > py)) &&
                             (px < (xj - xi) * (py - yi) / Mathf.Max(1e-6f, yj - yi) + xi);
            if (intersect)
                inside = !inside;
            j = i;
        }
        return inside;
    }
}
