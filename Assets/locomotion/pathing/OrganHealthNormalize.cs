using UnityEngine;

/// <summary>Maps unbounded organ raw health into presentation [0,1] and qualitative labels.</summary>
public static class OrganHealthNormalize
{
    public const float EasyCriticalFloor = 0.15f;
    public const float GreatThreshold = 0.95f;
    public const float GoodThreshold = 0.75f;
    public const float FairThreshold = 0.5f;
    public const float PoorThreshold = 0.35f;

    /// <summary>
    /// Soft clamp: identity on [0,1]; overheal (&gt;1) saturates at 1; values &lt; 0 approach 0.
    /// Raw remains available separately for hardcore / effect math.
    /// </summary>
    public static float SoftClamp01(float raw)
    {
        if (float.IsNaN(raw) || float.IsInfinity(raw))
            return 0f;
        if (raw >= 1f)
            return 1f;
        if (raw >= 0f)
            return raw;
        // raw < 0 → approach 0 as |raw| grows
        return 1f / (1f - raw);
    }

    public static float EasyNormalize(float raw)
    {
        return Mathf.Max(EasyCriticalFloor, SoftClamp01(raw));
    }

    public static float Normalize(float raw, LifeSystemsDifficulty difficulty)
    {
        return difficulty == LifeSystemsDifficulty.Easy
            ? EasyNormalize(raw)
            : SoftClamp01(raw);
    }

    public static string Label(float normalized01)
    {
        if (normalized01 >= GreatThreshold) return "Great";
        if (normalized01 >= GoodThreshold) return "Good";
        if (normalized01 >= FairThreshold) return "Fair";
        if (normalized01 >= PoorThreshold) return "Poor";
        return "Critical";
    }
}
