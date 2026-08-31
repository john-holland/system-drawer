using UnityEngine;

/// <summary>Basement standing water + prebake emit into a rolling-sphere flood (duck-typed).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Basement Flood Cache")]
public sealed class HouseBasementFloodCache : MonoBehaviour
{
    public BuildingWaterShutoff shutoff;
    public WaterHeaterRuntime heater;
    public UtilityBioRhythm utilityBio;
    public float standingLiters;
    public float pitAreaM2 = 4f;
    public float lastDrainedLiters;
    public float lastDrainedLitersPerSecond;
    public float lastEmittedLiters;
    public bool lastPrebakeAttempted;
    public Object floodSimulator;
    public Transform emitOrigin;

    public void DrainFromFlow(float litersPerSecond, float dt = 1f)
    {
        float taken = FloodDrainageAmounts.ApplyDrain(ref standingLiters, Mathf.Max(0f, litersPerSecond) * Mathf.Max(0f, dt));
        lastDrainedLiters = taken;
        lastDrainedLitersPerSecond = dt > 0f ? taken / dt : 0f;
        InvokeFloodDrain(taken);
        SyncBio();
    }

    public float DrainAmount(float liters)
    {
        float taken = FloodDrainageAmounts.ApplyDrain(ref standingLiters, liters);
        lastDrainedLiters = taken;
        lastDrainedLitersPerSecond = 0f;
        InvokeFloodDrain(taken);
        SyncBio();
        return taken;
    }

    public void Prebake()
    {
        lastPrebakeAttempted = true;
        float emit = 0f;
        if (shutoff != null && !shutoff.open)
            emit += 12f;
        if (heater != null && heater.leak01 > 0.01f)
            emit += heater.leak01 * 40f;
        if (utilityBio != null && utilityBio.sewerBackup01 > 0.2f)
            emit += utilityBio.sewerBackup01 * 30f;
        standingLiters += emit;
        lastEmittedLiters = emit;
        if (emit > 0.01f)
            InvokeFloodEmit(emit);
        SyncBio();
    }

    void SyncBio()
    {
        if (utilityBio == null) return;
        utilityBio.standingLiters = standingLiters;
        utilityBio.flood01 = Mathf.Clamp01(standingLiters / 200f);
    }

    void InvokeFloodEmit(float liters)
    {
        if (floodSimulator == null) return;
        var m = floodSimulator.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) });
        m?.Invoke(floodSimulator, new object[] { liters });
    }

    void InvokeFloodDrain(float liters)
    {
        if (floodSimulator == null || liters <= 0f) return;
        var drainAmount = floodSimulator.GetType().GetMethod("DrainAmount", new[] { typeof(float) });
        if (drainAmount != null)
        {
            drainAmount.Invoke(floodSimulator, new object[] { liters });
            return;
        }
        var drainFlow = floodSimulator.GetType().GetMethod("DrainFromFlow", new[] { typeof(float) });
        drainFlow?.Invoke(floodSimulator, new object[] { liters });
    }
}
