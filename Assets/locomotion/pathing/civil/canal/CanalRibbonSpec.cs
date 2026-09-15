using System;
using UnityEngine;

/// <summary>Canal centerline ribbon: depth, walls, authored water velocity bake (optional live sim).</summary>
[CreateAssetMenu(fileName = "CanalRibbon", menuName = "Locomotion/Civil/Canal Ribbon")]
public sealed class CanalRibbonSpec : ScriptableObject
{
    [Header("Channel")]
    public float lengthM = 120f;
    public float widthM = 12f;
    public float depthM = 4.5f;
    public float wallAngleDeg = 15f;
    public string wallMaterial = "fabric_concrete";
    public float cementInnerCurveM = 0.4f;
    public float cementOuterCurveM = 0.8f;
    public float girderSpacingM = 6f;
    public bool fabricConcreteFill = true;

    [Header("Water bake")]
    public float authoredSpeedMps = 0.6f;
    public float authoredVelocityMps = 0.6f;
    public float bakeBinM = 4f;
    public bool bakeNotLiveSim = true;

    [Header("Ground")]
    public string[] groundCompositionWhitelist = { "clay", "silt", "lined_concrete" };

    public string BakeHash() =>
        lengthM.ToString("0.###") + "|" + depthM.ToString("0.###") + "|"
        + authoredSpeedMps.ToString("0.###") + "|" + authoredVelocityMps.ToString("0.###")
        + "|" + bakeBinM.ToString("0.###");
}

[Serializable]
public sealed class CanalWaterBakeField
{
    public float[] speedMps = Array.Empty<float>();
    public Vector3[] velocity = Array.Empty<Vector3>();
    public float lengthM;
    public float binSizeM = 4f;
    public string bakeHash = "";
    public int bakeCount;

    public int BinCount => speedMps != null ? speedMps.Length : 0;

    public void Bake(CanalRibbonSpec spec) => Bake(spec, false);

    public void Bake(CanalRibbonSpec spec, bool force)
    {
        if (spec == null) return;
        string hash = spec.BakeHash();
        if (!force && bakeCount > 0 && bakeHash == hash && BinCount > 0)
            return;
        lengthM = Mathf.Max(8f, spec.lengthM);
        binSizeM = Mathf.Max(0.5f, spec.bakeBinM);
        int n = Mathf.Max(2, Mathf.CeilToInt(lengthM / binSizeM));
        speedMps = new float[n];
        velocity = new Vector3[n];
        float s = Mathf.Max(0f, spec.authoredSpeedMps);
        float v = Mathf.Max(0f, spec.authoredVelocityMps);
        for (int i = 0; i < n; i++)
        {
            float t = n <= 1 ? 0f : i / (float)(n - 1);
            float taper = 1f - 0.15f * Mathf.Abs(t * 2f - 1f);
            speedMps[i] = s * taper;
            velocity[i] = Vector3.forward * (v * taper);
        }
        bakeHash = hash;
        bakeCount++;
    }

    public float SampleSpeed(float arc01)
    {
        if (BinCount == 0) return 0f;
        float x = Mathf.Clamp01(arc01) * (BinCount - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(x), 0, BinCount - 2);
        return Mathf.Lerp(speedMps[i], speedMps[i + 1], x - i);
    }
}
