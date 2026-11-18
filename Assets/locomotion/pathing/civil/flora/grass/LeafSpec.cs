using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Gaussian fit of a Kármán vortex street used to prebake leaf bend.</summary>
[CreateAssetMenu(fileName = "KarmanVortexWindFit", menuName = "Locomotion/Civil/Karman Vortex Wind Fit")]
public sealed class KarmanVortexWindFit : ScriptableObject
{
    [Min(0.01f)] public float spacing = 0.35f;
    [Min(0.001f)] public float sigma = 0.12f;
    public float amplitude = 0.4f;
    public float[] bakedBend = Array.Empty<float>();

    public float Sample(float along)
    {
        float period = Mathf.Max(0.01f, spacing);
        float x = along - Mathf.Round(along / period) * period;
        float s = Mathf.Max(0.001f, sigma);
        return amplitude * Mathf.Exp(-(x * x) / (2f * s * s));
    }

    public void Prebake(int samples)
    {
        int n = Mathf.Max(2, samples);
        bakedBend = new float[n];
        for (int i = 0; i < n; i++)
        {
            float along = i / (float)(n - 1);
            bakedBend[i] = Sample(along);
        }
    }
}

/// <summary>Leaf gradients, water response, and the ribbon or SDF used by a skinned blade.</summary>
[CreateAssetMenu(fileName = "LeafSpec", menuName = "Locomotion/Civil/Leaf Spec")]
public sealed class LeafSpec : ScriptableObject
{
    public Gradient albedo = new Gradient();
    public Texture2D overwater;
    public Texture2D underwater;
    public AnimationCurve waterResponse = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve underwaterResponse = AnimationCurve.Linear(0f, 0.2f, 1f, 0.8f);
    [Range(0f, 1f)] public float averageWaterRequirement01 = 0.45f;
    public List<Vector3> centerCurve = new List<Vector3> { Vector3.zero, new Vector3(0f, 0.5f, 0.02f), new Vector3(0f, 1f, 0f) };
    public bool useRibbonSides;
    public List<Vector3> ribbonLeft = new List<Vector3>();
    public List<Vector3> ribbonRight = new List<Vector3>();
    public Mesh sdfMesh;
    public KarmanVortexWindFit windFit;

    public Color AlbedoAt(float along01)
    {
        if (albedo == null)
            return new Color(0.3f, 0.55f, 0.22f, 1f);
        return albedo.Evaluate(Mathf.Clamp01(along01));
    }

    public float WaterResponse(float moisture01, bool submerged)
    {
        AnimationCurve curve = submerged ? underwaterResponse : waterResponse;
        float u = Mathf.Clamp01(moisture01);
        if (curve == null || curve.length == 0)
            return u;
        return curve.Evaluate(u);
    }

    public Texture2D TextureFor(bool submerged)
    {
        return submerged && underwater != null ? underwater : overwater;
    }
}
