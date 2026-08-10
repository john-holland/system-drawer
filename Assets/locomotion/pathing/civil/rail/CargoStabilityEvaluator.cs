using UnityEngine;

/// <summary>Live + bake-informed tip/slide risk for cargo and folded ambulatory sections.</summary>
public sealed class CargoStabilityEvaluator
{
    public float LastTipRisk01 { get; private set; }
    public float LastLashStable01 { get; private set; } = 1f;
    public bool LastIsStable { get; private set; } = true;

    public bool Evaluate(
        CargoStabilityMode mode,
        CargoLashProfile profile,
        CargoStabilityBakeAsset bake,
        Transform deckRoot,
        Vector3 worldCom,
        Vector3 accelWorld,
        float maxStrapNormalizedLoad01)
    {
        if (mode == CargoStabilityMode.ImpossibleKeepStable)
        {
            LastTipRisk01 = 0f;
            LastLashStable01 = 1f;
            LastIsStable = true;
            return true;
        }

        float tip = 0f;
        if (bake != null)
        {
            tip = Mathf.Max(tip, bake.prebakedTipRisk01);
            if (!bake.ComInsidePolygon(worldCom, deckRoot))
                tip = Mathf.Max(tip, 0.85f);
        }

        float lateral = new Vector2(accelWorld.x, accelWorld.z).magnitude;
        tip = Mathf.Clamp01(tip + lateral * 0.08f);
        tip = Mathf.Clamp01(tip + maxStrapNormalizedLoad01 * 0.25f);

        float threshold = profile != null ? profile.tipUnstable01 : 0.65f;
        if (mode == CargoStabilityMode.SoftLash && profile != null)
            threshold = profile.softLashTipBias01;

        LastTipRisk01 = tip;
        LastLashStable01 = Mathf.Clamp01(1f - tip);
        LastIsStable = tip < threshold;
        return LastIsStable;
    }
}
