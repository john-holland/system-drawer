using UnityEngine;

/// <summary>
/// Log₁₀ falloff when player speed exceeds developer max bounds (weather egg philosophy for actor LOD).
/// </summary>
[System.Serializable]
public sealed class CivilSpeedLodPolicy
{
    [Tooltip("Designer max player speed (m/s). Above this, LOD scale falls with log.")]
    public float developerMaxSpeedMps = 12f;

    [Tooltip("Log base for falloff (10 = log10).")]
    public float logFalloffBase = 10f;

    [Tooltip("Minimum LOD scale when overspeed (before FeatureBudget floor).")]
    [Range(0.05f, 1f)]
    public float lodFloor = 0.15f;

    /// <summary>
    /// When v &lt;= vmax → 1. When faster: clamp(1 / (1 + log_b(1 + v/vmax)), floor, 1).
    /// </summary>
    public float ComputeLodScale(float speedMps)
    {
        float vmax = Mathf.Max(0.01f, developerMaxSpeedMps);
        float v = Mathf.Max(0f, speedMps);
        if (v <= vmax)
            return 1f;
        float ratio = v / vmax;
        float logBase = Mathf.Max(2f, logFalloffBase);
        // log_b(x) = ln(x)/ln(b)
        float logTerm = Mathf.Log(1f + ratio) / Mathf.Log(logBase);
        float scale = 1f / (1f + logTerm);
        return Mathf.Clamp(scale, lodFloor, 1f);
    }
}
