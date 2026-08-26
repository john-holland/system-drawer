using UnityEngine;

/// <summary>
/// Cylinder-section canvas for tattoos / curved whiteboards. UV wraps around the arc.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Canvas Curved Decal")]
[RequireComponent(typeof(PaintCanvas))]
public sealed class PaintCanvasCurvedDecal : MonoBehaviour
{
    [Min(0.02f)] public float radiusM = 0.25f;
    [Range(10f, 180f)] public float arcDeg = 90f;
    [Min(0.02f)] public float heightM = 0.2f;
    [Range(8, 64)] public int segments = 24;
    public bool rebuildOnEnable = true;

    MeshFilter _filter;
    MeshCollider _collider;

    void OnEnable()
    {
        var canvas = GetComponent<PaintCanvas>();
        if (canvas != null)
            canvas.surfaceKind = PaintCanvas.SurfaceKind.CurvedDecal;
        if (rebuildOnEnable)
            RebuildMesh();
    }

    public void RebuildMesh()
    {
        _filter = GetComponent<MeshFilter>();
        if (_filter == null)
            _filter = gameObject.AddComponent<MeshFilter>();
        if (GetComponent<MeshRenderer>() == null)
            gameObject.AddComponent<MeshRenderer>();
        _collider = GetComponent<MeshCollider>();
        if (_collider == null)
            _collider = gameObject.AddComponent<MeshCollider>();

        var mesh = BuildArcMesh();
        _filter.sharedMesh = mesh;
        _collider.sharedMesh = mesh;
    }

    Mesh BuildArcMesh()
    {
        int segs = Mathf.Clamp(segments, 8, 64);
        float halfArc = arcDeg * 0.5f * Mathf.Deg2Rad;
        var verts = new Vector3[(segs + 1) * 2];
        var uvs = new Vector2[verts.Length];
        var tris = new int[segs * 6];
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float a = Mathf.Lerp(-halfArc, halfArc, t);
            float x = Mathf.Sin(a) * radiusM;
            float z = Mathf.Cos(a) * radiusM;
            verts[i] = new Vector3(x, -heightM * 0.5f, z);
            verts[i + segs + 1] = new Vector3(x, heightM * 0.5f, z);
            uvs[i] = new Vector2(t, 0f);
            uvs[i + segs + 1] = new Vector2(t, 1f);
        }
        int ti = 0;
        for (int i = 0; i < segs; i++)
        {
            int b = i;
            int t = i + segs + 1;
            tris[ti++] = b;
            tris[ti++] = t;
            tris[ti++] = b + 1;
            tris[ti++] = t;
            tris[ti++] = t + 1;
            tris[ti++] = b + 1;
        }
        var mesh = new Mesh { name = "PaintCurvedDecal" };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public bool WorldToUv(Vector3 world, out Vector2 uv)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        float a = Mathf.Atan2(local.x, local.z);
        float halfArc = arcDeg * 0.5f * Mathf.Deg2Rad;
        uv = new Vector2(
            Mathf.InverseLerp(-halfArc, halfArc, a),
            Mathf.InverseLerp(-heightM * 0.5f, heightM * 0.5f, local.y));
        float radial = new Vector2(local.x, local.z).magnitude;
        bool onShell = Mathf.Abs(radial - radiusM) < radiusM * 0.35f;
        return onShell && uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
    }
}
