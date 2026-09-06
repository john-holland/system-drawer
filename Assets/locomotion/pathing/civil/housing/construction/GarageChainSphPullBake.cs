using UnityEngine;

/// <summary>
/// Prebaked SPH-style pull field along a chain. Bake once; runtime only interpolates bins.
/// Door bend samples this field — it does not re-run SPH.
/// </summary>
[System.Serializable]
public sealed class GarageChainSphPullField
{
    public float[] tensionN = System.Array.Empty<float>();
    public Vector3[] bendDir = System.Array.Empty<Vector3>();
    public float[] wrap01 = System.Array.Empty<float>();
    public float lengthM;
    public float binSizeM = 0.1f;
    public string bakeHash = "";
    public int bakeCount;

    public int BinCount => tensionN != null ? tensionN.Length : 0;

    public void Bake(GarageChainSpec spec)
    {
        if (spec == null) return;
        string hash = spec.BakeHash();
        if (bakeCount > 0 && bakeHash == hash && BinCount > 0)
            return;

        lengthM = Mathf.Max(0.2f, spec.totalLengthM);
        binSizeM = Mathf.Max(0.02f, spec.sphArcBinM);
        int n = Mathf.Max(2, Mathf.CeilToInt(lengthM / binSizeM));
        tensionN = new float[n];
        bendDir = new Vector3[n];
        wrap01 = new float[n];

        float pitchR = Mathf.Max(0.02f, spec.pitchRadiusM);
        float wrapArc = Mathf.PI * pitchR;
        float wrapStart = Mathf.Clamp01(0.5f - wrapArc / (2f * lengthM));
        float wrapEnd = Mathf.Clamp01(0.5f + wrapArc / (2f * lengthM));
        var steel = spec.steel ?? GarageSteelLimits.DefaultSteel();
        float baseT = steel.YieldTensionN(spec.selectedKind);

        // One SPH neighborhood pass along the polyline (particles die into bins).
        int particles = Mathf.Max(n * 3, 24);
        var accT = new float[n];
        var accB = new Vector3[n];
        var accW = new float[n];
        var accC = new int[n];
        for (int p = 0; p < particles; p++)
        {
            float s01 = particles <= 1 ? 0f : p / (float)(particles - 1);
            int bin = Mathf.Clamp(Mathf.FloorToInt(s01 * n), 0, n - 1);
            float w = s01 >= wrapStart && s01 <= wrapEnd
                ? 1f
                : Mathf.Clamp01(1f - Mathf.Abs(s01 - 0.5f) * 1.4f);
            Vector3 bend = s01 >= wrapStart && s01 <= wrapEnd
                ? Quaternion.AngleAxis(s01 * 360f, Vector3.right) * Vector3.forward
                : Vector3.up;
            accT[bin] += baseT * (0.55f + 0.45f * w);
            accB[bin] += bend;
            accW[bin] += w;
            accC[bin]++;
        }

        for (int i = 0; i < n; i++)
        {
            int c = Mathf.Max(1, accC[i]);
            tensionN[i] = accT[i] / c;
            bendDir[i] = accB[i].sqrMagnitude > 1e-8f ? (accB[i] / c).normalized : Vector3.up;
            wrap01[i] = accW[i] / c;
        }

        bakeHash = hash;
        bakeCount++;
    }

    public float SampleTension(float s01) => SampleScalar(tensionN, s01);
    public float SampleWrap(float s01) => SampleScalar(wrap01, s01);

    public Vector3 SampleBend(float s01)
    {
        if (bendDir == null || bendDir.Length == 0)
            return Vector3.up;
        float t = Mathf.Clamp01(s01) * (bendDir.Length - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(t), 0, bendDir.Length - 2);
        return Vector3.Slerp(bendDir[i], bendDir[i + 1], t - i).normalized;
    }

    static float SampleScalar(float[] bins, float s01)
    {
        if (bins == null || bins.Length == 0)
            return 0f;
        float t = Mathf.Clamp01(s01) * (bins.Length - 1);
        int i = Mathf.Clamp(Mathf.FloorToInt(t), 0, bins.Length - 2);
        return Mathf.Lerp(bins[i], bins[i + 1], t - i);
    }
}
