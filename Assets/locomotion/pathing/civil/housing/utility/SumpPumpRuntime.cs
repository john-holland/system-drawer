using UnityEngine;

/// <summary>
/// Basement pit pump. Stays off below <see cref="minActivationLiters"/> (no trickle)
/// and clamps drain to <see cref="maxFlowLitersPerSecond"/>. Discharge goes to sewer, not the water main.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sump Pump")]
public sealed class SumpPumpRuntime : MonoBehaviour
{
    public HouseBasementFloodCache floodCache;
    public HousePowerBus powerBus;
    public CircuitBreakerPanel panel;
    public SewerGraph sewer;
    public SewerNode sewerTap;
    public UtilityBioRhythm utilityBio;
    public float minActivationLiters = 20f;
    public float minActivationHeightM;
    public float maxFlowLitersPerSecond = 8f;
    public float lastDrainedLitersPerSecond;
    public bool lastOn;

    public bool PowerOn =>
        (panel == null || panel.feedOn) &&
        (powerBus == null || powerBus.charge01 > 0.01f);

    public void Tick(float dt)
    {
        if (floodCache == null)
            floodCache = GetComponentInParent<HouseBasementFloodCache>();
        float standing = floodCache != null ? floodCache.standingLiters : (utilityBio != null ? utilityBio.standingLiters : 0f);
        if (minActivationHeightM > 0f && floodCache != null)
        {
            float height = FloodDrainageAmounts.HeightFromLiters(standing, floodCache.pitAreaM2);
            if (height < minActivationHeightM)
            {
                lastOn = false;
                lastDrainedLitersPerSecond = 0f;
                return;
            }
        }

        float flow = FloodDrainageAmounts.SumpFlowLitersPerSecond(
            standing, minActivationLiters, maxFlowLitersPerSecond, PowerOn);
        lastOn = flow > 0f;
        lastDrainedLitersPerSecond = flow;
        if (flow <= 0f || dt <= 0f)
            return;

        if (floodCache != null)
            floodCache.DrainFromFlow(flow, dt);
        else if (utilityBio != null)
            FloodDrainageAmounts.ApplyDrain(ref utilityBio.standingLiters, flow * dt);

        if (sewer != null && sewerTap != null)
            sewer.TransmitWaterIn(sewerTap, flow * dt);
        if (powerBus != null)
            powerBus.charge01 = Mathf.Max(0f, powerBus.charge01 - 0.002f * dt);
        if (utilityBio != null)
            utilityBio.sumpOn = lastOn;
    }

    public void Prime()
    {
        if (floodCache != null && floodCache.standingLiters < minActivationLiters)
            floodCache.standingLiters = minActivationLiters;
    }

    public void Clear()
    {
        lastOn = false;
        lastDrainedLitersPerSecond = 0f;
        if (floodCache != null)
            floodCache.DrainAmount(floodCache.standingLiters);
    }
}
