using UnityEngine;

/// <summary>Drain clog packing — wet/dry SPH-ish accumulation preventing downflow.</summary>
[System.Serializable]
public sealed class ClogState
{
    [Range(0f, 1f)] public float clog01;
    [Range(0f, 1f)] public float wetPacking01;
    [Range(0f, 1f)] public float dryPacking01;
    public bool developerForceClog;

    public float EffectiveClog01()
    {
        if (developerForceClog) return 1f;
        return Mathf.Clamp01(Mathf.Max(clog01, Mathf.Max(wetPacking01, dryPacking01)));
    }

    public float OutflowMultiplier() => 1f - EffectiveClog01();

    public void AccumulateWet(float amount01)
    {
        wetPacking01 = Mathf.Clamp01(wetPacking01 + amount01);
        clog01 = Mathf.Clamp01(clog01 + amount01 * 0.5f);
    }

    public void AccumulateDry(float amount01)
    {
        dryPacking01 = Mathf.Clamp01(dryPacking01 + amount01);
        clog01 = Mathf.Clamp01(clog01 + amount01 * 0.7f);
    }

    public void Plunge(float clear01, float mixPerturbation01 = 0.3f)
    {
        float clear = Mathf.Clamp01(clear01);
        clog01 = Mathf.Clamp01(clog01 - clear);
        wetPacking01 = Mathf.Clamp01(wetPacking01 - clear * (0.5f + mixPerturbation01));
        dryPacking01 = Mathf.Clamp01(dryPacking01 - clear * 0.4f);
        developerForceClog = false;
    }

    public void SnakeClear(float clear01)
    {
        Plunge(clear01, 0.6f);
        dryPacking01 = Mathf.Clamp01(dryPacking01 - clear01);
    }
}
