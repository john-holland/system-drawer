using UnityEngine;

/// <summary>Insulation-style pleat repeat along a fold spline, baked into the fold cache.</summary>
public static class ClothPleatBaker
{
    public static int PleatCount(float pathLengthM, float pleatSpacingM)
    {
        float spacing = Mathf.Max(0.02f, pleatSpacingM);
        return Mathf.Max(2, Mathf.RoundToInt(Mathf.Max(0.02f, pathLengthM) / spacing));
    }

    public static float PathLength(ClothSplinePath path)
    {
        if (path?.controlPoints == null || path.controlPoints.Count < 2) return 0f;
        float d = 0f;
        for (int i = 1; i < path.controlPoints.Count; i++)
            d += Vector3.Distance(path.controlPoints[i - 1], path.controlPoints[i]);
        return d;
    }

    public static InsulationBattBaker.Result BakePleats(ClothSplinePath fold, float spacingM = 0.08f)
    {
        int layers = Mathf.Clamp(PleatCount(PathLength(fold), spacingM), 2, 4);
        return InsulationBattBaker.BakeSlot(null, layers, false);
    }
}
