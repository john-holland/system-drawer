using UnityEngine;

/// <summary>
/// Scene gizmo: hairline ring + bright green ribbon along the hair-part split spline.
/// </summary>
[AddComponentMenu("Locomotion/Hair/Hair Line Part Gizmo")]
[ExecuteAlways]
public sealed class HairLinePartGizmo : MonoBehaviour
{
    public HairPlumeConfig config;
    public Transform scalpRoot;
    public bool drawHairline = true;
    public bool drawPartRibbon = true;
    public bool drawCenterPate = true;
    public Color hairlineColor = new Color(0.3f, 0.85f, 1f, 0.85f);
    public Color partRibbonColor = new Color(0.15f, 1f, 0.2f, 1f);
    public Color pateColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Min(8)] public int hairlineSegments = 48;

    void OnDrawGizmos()
    {
        if (config == null) return;
        Transform root = scalpRoot != null ? scalpRoot : transform;

        if (drawCenterPate)
        {
            Gizmos.color = pateColor;
            Vector3 pate = HairLineSampler.CenterPateWorld(root, config);
            Gizmos.DrawSphere(pate, 0.008f);
        }

        if (drawHairline)
            DrawHairline(root);

        if (drawPartRibbon && config.hairPartSpline != null && config.hairPartSpline.enabled)
            DrawPartRibbon(root);
    }

    void DrawHairline(Transform root)
    {
        Gizmos.color = hairlineColor;
        int n = Mathf.Max(8, hairlineSegments);
        Vector3 prev = HairLineSampler.EmergenceRingPoint(root, config, 0f);
        for (int i = 1; i <= n; i++)
        {
            float u = i / (float)n;
            Vector3 p = HairLineSampler.EmergenceRingPoint(root, config, u);
            Gizmos.DrawLine(prev, p);
            // Short emergence tick toward pate-averaged direction
            Vector3 dir = HairLineSampler.EmergenceDirection(root, config, u - 1f / n);
            Gizmos.DrawLine(prev, prev + dir * 0.025f);
            prev = p;
        }
    }

    void DrawPartRibbon(Transform root)
    {
        var part = config.hairPartSpline;
        part.EnsureDefaults();
        int n = Mathf.Max(4, part.sampleCount);
        float half = Mathf.Max(0.001f, part.gizmoRibbonHalfWidthM);
        Color c = partRibbonColor;
        c.a = 1f;
        Gizmos.color = c;

        Vector3 prevC = part.EvaluateWorld(root, 0f);
        Vector3 prevT = root.TransformDirection(part.TangentLocal(0f));
        Vector3 up = root.up;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 center = part.EvaluateWorld(root, t);
            Vector3 tangent = root.TransformDirection(part.TangentLocal(t));
            if (tangent.sqrMagnitude < 1e-8f) tangent = prevT;
            tangent.Normalize();

            Vector3 side = Vector3.Cross(up, tangent);
            if (side.sqrMagnitude < 1e-8f)
                side = Vector3.Cross(root.right, tangent);
            side.Normalize();

            Vector3 prevSide = Vector3.Cross(up, prevT.normalized);
            if (prevSide.sqrMagnitude < 1e-8f)
                prevSide = side;
            prevSide.Normalize();

            Vector3 a0 = prevC - prevSide * half;
            Vector3 a1 = prevC + prevSide * half;
            Vector3 b0 = center - side * half;
            Vector3 b1 = center + side * half;

            // Bright green ribbon: outline + cross ribs
            Gizmos.DrawLine(a0, b0);
            Gizmos.DrawLine(a1, b1);
            Gizmos.DrawLine(a0, a1);
            Gizmos.DrawLine(b0, b1);
            Gizmos.DrawLine(a0, b1);
            Gizmos.DrawLine(a1, b0);
            // Center spine
            Gizmos.DrawLine(prevC, center);

            prevC = center;
            prevT = tangent;
        }
    }
}
