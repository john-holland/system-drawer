using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IceSectionCurvature
{
    public float[] coefficients = Array.Empty<float>();
    public Vector2 lensing = new Vector2(0.5f, 0.5f);
    public float granularity = 8f;
    public float sphFlowFactor;

    public static int DegreeCount(float granularity)
    {
        int n = Mathf.Clamp(Mathf.RoundToInt(granularity), 1, 16);
        return n;
    }

    public static IceSectionCurvature ForGranularity(float granularity, float sphFlow)
    {
        int n = DegreeCount(granularity);
        var c = new float[n];
        c[0] = 0.02f;
        if (n > 2)
            c[2] = 0.01f;
        return new IceSectionCurvature
        {
            coefficients = c,
            granularity = granularity,
            sphFlowFactor = sphFlow
        };
    }

    public static float Kappa(float[] c, float sd)
    {
        if (c == null || c.Length == 0)
            return 0f;
        float k = 0f;
        float p = 1f;
        for (int d = 0; d < c.Length; d++)
        {
            k += c[d] * p;
            p *= sd;
        }
        return k;
    }

    public static float Rendered(float angular, float sphFlowFactor, float[] c, float sd)
    {
        return angular + sphFlowFactor * Kappa(c, sd);
    }
}

[Serializable]
public sealed class SnowQuadPixel
{
    public Vector3 localPoint;
    public Vector3 curve;
    public bool shattered;
}

[Serializable]
public sealed class IcePlaneSection
{
    public string id;
    public Vector3 origin;
    public Vector3 normal = Vector3.up;
    public float stress;
    public bool broken;
    public IceSectionCurvature curvature = new IceSectionCurvature();
    public List<SnowQuadPixel> pixels = new List<SnowQuadPixel>();
}

public static class SdfSealedSkinConvexPlaneSplitter
{
    public static List<IcePlaneSection> Split(SdfSealedClothSkin skin, float granularity, float sphFlow)
    {
        var list = new List<IcePlaneSection>();
        if (skin == null || skin.fragments == null)
            return list;
        for (int i = 0; i < skin.fragments.Count; i++)
        {
            SdfSealedClothFragment fragment = skin.fragments[i];
            if (fragment == null)
                continue;
            Vector3 origin = fragment.centroid;
            Vector3 tangent = Vector3.forward;
            if (fragment.curve != null && fragment.curve.Length > 1)
                tangent = (fragment.curve[fragment.curve.Length - 1] - fragment.curve[0]).normalized;
            var section = new IcePlaneSection
            {
                id = string.IsNullOrEmpty(fragment.id) ? "ice_" + i : fragment.id,
                origin = origin,
                normal = Vector3.up,
                curvature = IceSectionCurvature.ForGranularity(granularity, sphFlow)
            };
            int pixels = IceSectionCurvature.DegreeCount(granularity);
            for (int p = 0; p < pixels; p++)
            {
                float u = pixels == 1 ? 0.5f : p / (float)(pixels - 1);
                Vector3 point = origin + tangent * u;
                Vector3 bow = Vector3.zero;
                if (fragment.curve != null && fragment.curve.Length > 1)
                    bow = fragment.curve[Mathf.Min(fragment.curve.Length - 1, 1)] - origin;
                section.pixels.Add(new SnowQuadPixel { localPoint = point, curve = bow, shattered = false });
            }
            list.Add(section);
        }
        return list;
    }
}

/// <summary>One continuous shell until stress opens a new layer.</summary>
public sealed class IceShellState
{
    public int layers = 1;
    public float breakStress = 0.55f;
    public List<IcePlaneSection> sections = new List<IcePlaneSection>();

    public bool ApplyStress(float stress)
    {
        if (stress <= breakStress)
            return false;
        layers++;
        for (int i = 0; i < sections.Count; i++)
        {
            IcePlaneSection section = sections[i];
            if (section == null)
                continue;
            section.broken = true;
            section.stress = stress;
            if (section.pixels == null)
                continue;
            for (int p = 0; p < section.pixels.Count; p++)
                section.pixels[p].shattered = true;
        }
        return true;
    }
}
