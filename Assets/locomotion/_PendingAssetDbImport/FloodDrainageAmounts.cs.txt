using UnityEngine;

/// <summary>Standing-volume and sump-flow math for SPH / flood drainage (liters).</summary>
public static class FloodDrainageAmounts
{
    public const float SpawnRatePerLiterPerSecond = 400f;

    public static float ApplyDrain(ref float standingLiters, float requestLiters)
    {
        float taken = Mathf.Min(Mathf.Max(0f, standingLiters), Mathf.Max(0f, requestLiters));
        standingLiters -= taken;
        return taken;
    }

    public static float SpawnRateFromLitersPerSecond(float litersPerSecond) =>
        Mathf.Max(0f, litersPerSecond) * SpawnRatePerLiterPerSecond;

    public static float HeightFromLiters(float standingLiters, float pitAreaM2)
    {
        float area = Mathf.Max(0.01f, pitAreaM2);
        return Mathf.Max(0f, standingLiters) * 0.001f / area;
    }

    public static float SumpFlowLitersPerSecond(
        float standingLiters,
        float minActivationLiters,
        float maxFlowLitersPerSecond,
        bool powerOn)
    {
        if (!powerOn || standingLiters < minActivationLiters)
            return 0f;
        return Mathf.Min(standingLiters, Mathf.Max(0f, maxFlowLitersPerSecond));
    }
}
