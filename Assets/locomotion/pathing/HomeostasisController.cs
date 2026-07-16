using UnityEngine;

/// <summary>
/// Pulls channels and organs toward healthy setpoints. Never spawns illness — only recovers.
/// </summary>
[AddComponentMenu("Locomotion/Life Systems Homeostasis")]
[RequireComponent(typeof(LifeSystemsSheet))]
public sealed class HomeostasisController : MonoBehaviour
{
    public LifeSystemsSheet sheet;
    public float channelPullPerSecond = 0.35f;
    public float organPullPerSecond = 0.2f;
    public float overhealDecayPerSecond = 0.05f;
    public float easyOverhealDecayPerSecond = 0.12f;

    void Awake()
    {
        if (sheet == null)
            sheet = GetComponent<LifeSystemsSheet>();
    }

    void Update()
    {
        if (sheet == null) return;
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        sheet.EnsureDefaults();
        float pull = channelPullPerSecond;
        if (sheet.difficulty == LifeSystemsDifficulty.Easy)
            pull *= 1.75f;

        var channels = LifeSystemsChannelCatalog.Channels;
        float errAcc = 0f;
        int n = 0;
        for (int i = 0; i < channels.Count; i++)
        {
            var def = channels[i];
            if (def.id == LifeSystemsChannelCatalog.HomeostasisError)
                continue;
            // Do not fight active illness channel locks — LifeSystemsServices freezes via effects;
            // here we always gently pull unless an illness effect marks the channel (checked externally).
            if (HasBlockingIllness(def.id))
                continue;
            float cur = sheet.Get01(def.id);
            float next = Mathf.MoveTowards(cur, def.setpoint01, pull * dt);
            sheet.Set01(def.id, next);
            errAcc += Mathf.Abs(next - def.setpoint01);
            n++;
        }
        if (n > 0)
            sheet.Set01(LifeSystemsChannelCatalog.HomeostasisError, errAcc / n);

        float organPull = organPullPerSecond;
        if (sheet.difficulty == LifeSystemsDifficulty.Easy)
            organPull *= 1.75f;
        float organTarget = sheet.difficulty == LifeSystemsDifficulty.Easy
            ? OrganCatalog.EasyHomeostasisSetpointRaw
            : OrganCatalog.HomeostasisSetpointRaw;
        float overhealDecay = sheet.difficulty == LifeSystemsDifficulty.Easy
            ? easyOverhealDecayPerSecond
            : overhealDecayPerSecond;

        sheet.organs.EnsureCatalogDefaults();
        for (int i = 0; i < sheet.organs.entries.Count; i++)
        {
            var e = sheet.organs.entries[i];
            if (e == null || HasBlockingOrganIllness(e.organId))
                continue;
            if (e.rawHealth > organTarget)
                e.rawHealth = Mathf.MoveTowards(e.rawHealth, organTarget, overhealDecay * dt);
            else
                e.rawHealth = Mathf.MoveTowards(e.rawHealth, organTarget, organPull * dt);
        }

        float lfSet = LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.LifeForce, out var lf)
            ? lf.setpoint01
            : 0.85f;
        sheet.lifeForce.TickTowardSetpoint(lfSet, pull * 0.5f, dt);
        sheet.TickRealtime(dt);
    }

    bool HasBlockingIllness(string channelId)
    {
        if (sheet.activeEffects == null) return false;
        double now = Time.unscaledTimeAsDouble;
        for (int i = 0; i < sheet.activeEffects.Count; i++)
        {
            var ae = sheet.activeEffects[i];
            if (ae?.spec == null) continue;
            if (ae.spec.source != LifeSystemsEffectSource.Illness) continue;
            if (!IsEffectActive(ae, now)) continue;
            var deltas = ae.spec.channelDeltas;
            if (deltas == null) continue;
            for (int d = 0; d < deltas.Count; d++)
            {
                if (deltas[d] != null &&
                    string.Equals(deltas[d].channelId, channelId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    bool HasBlockingOrganIllness(string organId)
    {
        if (sheet.activeEffects == null) return false;
        double now = Time.unscaledTimeAsDouble;
        for (int i = 0; i < sheet.activeEffects.Count; i++)
        {
            var ae = sheet.activeEffects[i];
            if (ae?.spec == null) continue;
            if (ae.spec.source != LifeSystemsEffectSource.Illness &&
                ae.spec.source != LifeSystemsEffectSource.Dev) continue;
            if (!IsEffectActive(ae, now)) continue;
            var deltas = ae.spec.organDeltas;
            if (deltas == null) continue;
            for (int d = 0; d < deltas.Count; d++)
            {
                if (deltas[d] != null &&
                    string.Equals(deltas[d].organId, organId, System.StringComparison.OrdinalIgnoreCase) &&
                    deltas[d].rawDelta < 0f)
                    return true;
            }
        }
        return false;
    }

    static bool IsEffectActive(LifeSystemsActiveEffect ae, double now)
    {
        if (ae.spec.durationSeconds <= 0f)
            return true;
        return now - ae.appliedUnscaledTime < ae.spec.durationSeconds;
    }
}
