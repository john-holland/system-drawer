using System.Collections.Generic;
using UnityEngine;

/// <summary>Sidewalk composition: padded walkable ribbon + optional matting overlay.</summary>
public static class SidewalkMeshBaker
{
    public struct Result
    {
        public float walkableWidthM;
        public bool hasMatting;
        public Mesh mesh;
        public List<Vector3> polyline;
    }

    public static Result Bake(RoadLaneConfigAsset config, IList<Vector3> shoulderPolyline)
    {
        var r = new Result { polyline = new List<Vector3>() };
        if (config == null) return r;
        float width = Mathf.Max(0.1f, config.sidewalkWidthM);
        float pad = Mathf.Max(0f, config.sidewalkPaddingM);
        r.walkableWidthM = Mathf.Max(0.1f, width - 2f * pad);
        r.hasMatting = config.mattingWidth01 > 1e-4f;
        if (shoulderPolyline != null)
            r.polyline.AddRange(shoulderPolyline);
        r.mesh = BuildRibbon(r.polyline, r.walkableWidthM);
        return r;
    }

    public static Mesh BuildRibbon(IList<Vector3> polyline, float width)
    {
        var mesh = new Mesh { name = "SidewalkRibbon" };
        if (polyline == null || polyline.Count < 2) return mesh;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float half = width * 0.5f;
        for (int i = 0; i < polyline.Count; i++)
        {
            Vector3 p = polyline[i];
            Vector3 n = i + 1 < polyline.Count ? (polyline[i + 1] - p) : (p - polyline[i - 1]);
            n.y = 0f;
            if (n.sqrMagnitude < 1e-6f) n = Vector3.forward;
            Vector3 bin = Vector3.Cross(Vector3.up, n.normalized);
            verts.Add(p - bin * half);
            verts.Add(p + bin * half);
            if (i > 0)
            {
                int a = (i - 1) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
            }
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }
}
