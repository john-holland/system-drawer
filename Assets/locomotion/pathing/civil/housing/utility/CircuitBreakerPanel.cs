using System.Collections.Generic;
using UnityEngine;

/// <summary>100 A service panel. Branches grow on the first panel; Spatial Generator clones a second when load would exceed 100 A.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Circuit Breaker Panel")]
public sealed class CircuitBreakerPanel : MonoBehaviour
{
    public const float DefaultAmpacityAmps = 100f;
    public const float Volts = 240f;

    public float ampacityAmps = DefaultAmpacityAmps;
    public bool feedOn = true;
    public HousePowerBus powerBus;
    public readonly List<float> branchAmps = new List<float>();

    public float SumBranchAmps()
    {
        float s = 0f;
        for (int i = 0; i < branchAmps.Count; i++)
            s += Mathf.Max(0f, branchAmps[i]);
        return s;
    }

    public float Load01()
    {
        float cap = Mathf.Max(1f, ampacityAmps);
        return Mathf.Clamp01(SumBranchAmps() / cap);
    }

    public static int RequiredPanelCount(float summedBranchAmps, float ampacityAmps = DefaultAmpacityAmps)
    {
        float cap = Mathf.Max(1f, ampacityAmps);
        float load = Mathf.Max(0f, summedBranchAmps);
        if (load <= cap)
            return 1;
        return Mathf.Max(2, Mathf.CeilToInt(load / cap));
    }

    public static float MaxDrawKwForAmpacity(float ampacityAmps = DefaultAmpacityAmps) =>
        ampacityAmps * Volts / 1000f;

    public void ResetBreakers() => feedOn = true;

    public void SetFeed(bool on)
    {
        feedOn = on;
        if (powerBus != null && !on)
            powerBus.charge01 = 0f;
    }
}
