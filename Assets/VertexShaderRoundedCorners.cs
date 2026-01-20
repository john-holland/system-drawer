using System;
using System.Collections.Generic;
using UnityEngine;

public class VertexShaderRoundedCorners : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Optional source polyline (local-space). If empty, uses this object's MeshFilter mesh vertices projected to XZ.")]
    public List<Vector3> polylineLocal = new List<Vector3>();

    [Header("Rounding")]
    [Tooltip("Corners within this angle (degrees) of 90° will be rounded.")]
    [Range(0f, 45f)]
    public float ninetyDegreeTolerance = 12f;

    [Tooltip("Radius for rounding (in the same units as the polyline).")]
    public float radius = 0.15f;

    [Tooltip("Segments used to approximate the quarter-circle.")]
    [Range(1, 24)]
    public int segmentsPerCorner = 6;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color inputColor = new Color(1f, 0.5f, 0.1f, 0.9f);
    public Color outputColor = new Color(0.2f, 0.8f, 1f, 0.95f);

    [NonSerialized] public List<Vector3> roundedLocal = new List<Vector3>(256);

    private void OnValidate()
    {
        // Keep values sane.
        radius = Mathf.Max(0f, radius);
        segmentsPerCorner = Mathf.Clamp(segmentsPerCorner, 1, 64);
    }

    private void Update()
    {
        // MVP: recompute every frame for easy iteration in-editor/play.
        // If you want, we can switch this to compute-on-demand + caching.
        var input = GetInputPolylineLocal();
        roundedLocal = RoundNinetyDegreeCornersLoopXZ(input, radius, ninetyDegreeTolerance, segmentsPerCorner);
    }

    private List<Vector3> GetInputPolylineLocal()
    {
        if (polylineLocal != null && polylineLocal.Count >= 3)
            return polylineLocal;

        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var verts = mf.sharedMesh.vertices;
            // Best-effort: use mesh vertices as a loop in their existing order (often not a boundary loop).
            // For real boundary extraction we'd need mesh topology, but this keeps the component usable now.
            var list = new List<Vector3>(verts.Length);
            for (int i = 0; i < verts.Length; i++)
                list.Add(verts[i]);
            return list;
        }

        return polylineLocal ?? new List<Vector3>();
    }

    /// <summary>
    /// One-pass loop rounding on the XZ plane. For each vertex, if the interior angle is ~90°,
    /// replace the corner with a small arc that is stitched seamlessly into the path.
    /// </summary>
    public static List<Vector3> RoundNinetyDegreeCornersLoopXZ(
        List<Vector3> loopLocal,
        float radius,
        float ninetyDegreeTolerance,
        int segmentsPerCorner)
    {
        var output = new List<Vector3>(loopLocal != null ? loopLocal.Count * (segmentsPerCorner + 1) : 0);
        if (loopLocal == null || loopLocal.Count < 3)
            return output;

        int n = loopLocal.Count;
        float r = Mathf.Max(0f, radius);
        float tol = Mathf.Clamp(ninetyDegreeTolerance, 0f, 45f);
        int seg = Mathf.Clamp(segmentsPerCorner, 1, 64);

        // One pass over vertices; stitch end by wrapping indices (closed loop).
        for (int i = 0; i < n; i++)
        {
            Vector3 prev = loopLocal[(i - 1 + n) % n];
            Vector3 curr = loopLocal[i];
            Vector3 next = loopLocal[(i + 1) % n];

            Vector2 a = new Vector2(prev.x, prev.z);
            Vector2 b = new Vector2(curr.x, curr.z);
            Vector2 c = new Vector2(next.x, next.z);

            Vector2 v1 = (a - b);
            Vector2 v2 = (c - b);
            float len1 = v1.magnitude;
            float len2 = v2.magnitude;
            if (len1 < 1e-5f || len2 < 1e-5f)
                continue;

            Vector2 d1 = v1 / len1; // from corner to prev
            Vector2 d2 = v2 / len2; // from corner to next

            // Interior angle at b between the two segments (0..180).
            float angle = Vector2.Angle(d1, d2);
            bool isNearRightAngle = Mathf.Abs(angle - 90f) <= tol;

            if (!isNearRightAngle || r <= 0f)
            {
                // Keep original corner (stitch by not duplicating if last added is same-ish).
                AddIfNotTooClose(output, curr);
                continue;
            }

            // Compute tangent points along each edge (offset from corner by r).
            // For a 90° corner this is a good approximation; for other angles we'd use r / tan(theta/2).
            float tDist = Mathf.Min(r, len1 * 0.5f, len2 * 0.5f);
            Vector2 p1 = b + d1 * tDist; // toward prev edge
            Vector2 p2 = b + d2 * tDist; // toward next edge

            // Arc center is at intersection of lines perpendicular to edges through p1/p2.
            // For a 90° corner, this is equivalent to offsetting each edge inward by r.
            // We can compute center as b + bisector * (r / sin(theta/2)) but for ~90° keep it stable:
            Vector2 bis = (d1 + d2).normalized;
            if (bis.sqrMagnitude < 1e-6f)
            {
                AddIfNotTooClose(output, curr);
                continue;
            }

            float theta2 = Mathf.Deg2Rad * (angle * 0.5f);
            float centerDist = tDist / Mathf.Max(0.001f, Mathf.Sin(theta2)); // general-ish
            Vector2 center = b + bis * centerDist;

            float startAng = Mathf.Atan2(p1.y - center.y, p1.x - center.x);
            float endAng = Mathf.Atan2(p2.y - center.y, p2.x - center.x);

            // Ensure we sweep the shorter direction (the inside of the corner)
            float delta = Mathf.DeltaAngle(startAng * Mathf.Rad2Deg, endAng * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            // Add tangent start then arc points then tangent end.
            AddIfNotTooClose(output, new Vector3(p1.x, curr.y, p1.y));
            for (int s = 1; s < seg; s++)
            {
                float t = s / (float)seg;
                float ang = startAng + delta * t;
                Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * tDist;
                output.Add(new Vector3(p.x, curr.y, p.y));
            }
            AddIfNotTooClose(output, new Vector3(p2.x, curr.y, p2.y));
        }

        // Stitch end: ensure loop doesn't duplicate the first point at the end.
        if (output.Count >= 2 && (output[0] - output[^1]).sqrMagnitude < 1e-8f)
            output.RemoveAt(output.Count - 1);

        return output;
    }

    private static void AddIfNotTooClose(List<Vector3> pts, Vector3 p)
    {
        if (pts.Count == 0)
        {
            pts.Add(p);
            return;
        }

        if ((pts[^1] - p).sqrMagnitude > 1e-8f)
            pts.Add(p);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        var input = GetInputPolylineLocal();
        DrawLoop(input, inputColor);
        DrawLoop(roundedLocal, outputColor);
    }

    private void DrawLoop(List<Vector3> loop, Color color)
    {
        if (loop == null || loop.Count < 2)
            return;

        Gizmos.color = color;
        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = transform.TransformPoint(loop[i]);
            Vector3 b = transform.TransformPoint(loop[(i + 1) % loop.Count]);
            Gizmos.DrawLine(a, b);
        }
    }
}
