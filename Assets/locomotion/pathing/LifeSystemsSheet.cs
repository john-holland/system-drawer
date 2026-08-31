using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-actor life-systems sheet: channels, organs, life force, bio rhythms.</summary>
[AddComponentMenu("Locomotion/Life Systems Sheet")]
public sealed class LifeSystemsSheet : MonoBehaviour
{
    public LifeSystemsDifficulty difficulty = LifeSystemsDifficulty.Normal;

    [SerializeField] List<ChannelValue> channelValues = new List<ChannelValue>();
    public OrganHealthState organs = new OrganHealthState();
    public LifeForceChannel lifeForce = new LifeForceChannel();
    public BioRhythmClock bioRhythm = new BioRhythmClock();
    public List<LifeSystemsActiveEffect> activeEffects = new List<LifeSystemsActiveEffect>();

    [Serializable]
    public sealed class ChannelValue
    {
        public string channelId;
        public float value01;
        public float clinicalValue;
    }

    readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    bool _indexed;
    bool _ensuringDefaults;

    void Awake()
    {
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        if (_ensuringDefaults)
            return;
        _ensuringDefaults = true;
        try
        {
            if (channelValues == null)
                channelValues = new List<ChannelValue>();
            if (organs == null)
                organs = new OrganHealthState();
            if (lifeForce == null)
                lifeForce = new LifeForceChannel();
            if (bioRhythm == null)
                bioRhythm = new BioRhythmClock();
            if (activeEffects == null)
                activeEffects = new List<LifeSystemsActiveEffect>();

            var channels = LifeSystemsChannelCatalog.Channels;
            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < channelValues.Count; i++)
            {
                if (channelValues[i] != null && !string.IsNullOrEmpty(channelValues[i].channelId))
                    have.Add(channelValues[i].channelId);
            }
            for (int i = 0; i < channels.Count; i++)
            {
                var def = channels[i];
                if (have.Contains(def.id))
                    continue;
                float v01 = def.setpoint01;
                channelValues.Add(new ChannelValue
                {
                    channelId = def.id,
                    value01 = v01,
                    clinicalValue = LifeSystemsChannelCatalog.ClinicalFrom01(def, v01)
                });
            }

            organs.EnsureCatalogDefaults();
            _indexed = false;
            RebuildIndex();
            // Write LifeForce directly — must not call Set01 (that would re-enter EnsureDefaults).
            Apply01(LifeSystemsChannelCatalog.LifeForce, lifeForce.lifeForce01);
        }
        finally
        {
            _ensuringDefaults = false;
        }
    }

    void RebuildIndex()
    {
        _index.Clear();
        for (int i = 0; i < channelValues.Count; i++)
        {
            if (channelValues[i] != null && !string.IsNullOrEmpty(channelValues[i].channelId))
                _index[channelValues[i].channelId] = i;
        }
        _indexed = true;
    }

    bool TryGetChannel(string id, out ChannelValue cv)
    {
        cv = null;
        if (!_indexed) RebuildIndex();
        if (!_index.TryGetValue(id ?? "", out int i) || i < 0 || i >= channelValues.Count)
            return false;
        cv = channelValues[i];
        return cv != null;
    }

    public float Get01(string channelId)
    {
        if (!TryGetChannel(channelId, out var cv))
            return LifeSystemsChannelCatalog.TryGet(channelId, out var d) ? d.setpoint01 : 0.5f;
        return cv.value01;
    }

    public float GetClinical(string channelId)
    {
        if (!TryGetChannel(channelId, out var cv))
        {
            if (LifeSystemsChannelCatalog.TryGet(channelId, out var d))
                return d.clinicalDefault;
            return 0f;
        }
        return cv.clinicalValue;
    }

    public void Set01(string channelId, float value01)
    {
        if (!_ensuringDefaults)
            EnsureDefaults();
        Apply01(channelId, value01);
    }

    void Apply01(string channelId, float value01)
    {
        if (!TryGetChannel(channelId, out var cv))
            return;
        cv.value01 = value01;
        if (LifeSystemsChannelCatalog.TryGet(channelId, out var def))
            cv.clinicalValue = LifeSystemsChannelCatalog.ClinicalFrom01(def, value01);
        if (string.Equals(channelId, LifeSystemsChannelCatalog.LifeForce, StringComparison.OrdinalIgnoreCase))
            lifeForce.lifeForce01 = value01;
        if (string.Equals(channelId, LifeSystemsChannelCatalog.BioRhythmAmplitude, StringComparison.OrdinalIgnoreCase))
            bioRhythm.amplitude01 = Mathf.Clamp01(value01);
    }

    public void SetClinical(string channelId, float clinical)
    {
        if (!LifeSystemsChannelCatalog.TryGet(channelId, out var def))
            return;
        Set01(channelId, LifeSystemsChannelCatalog.ClinicalTo01(def, clinical));
        if (TryGetChannel(channelId, out var cv))
            cv.clinicalValue = clinical;
    }

    public void Adjust01(string channelId, float delta01)
    {
        Set01(channelId, Get01(channelId) + delta01);
    }

    public void AdjustClinical(string channelId, float deltaClinical)
    {
        SetClinical(channelId, GetClinical(channelId) + deltaClinical);
    }

    public float GetRegional01(string channelId, BodyPartLifeModifier part)
    {
        float systemic = Get01(channelId);
        if (part == null)
            return systemic;
        return part.ApplyToChannel(channelId, systemic);
    }

    public float HeartRateBpm
    {
        get => GetClinical(LifeSystemsChannelCatalog.HeartRate);
        set => SetClinical(LifeSystemsChannelCatalog.HeartRate, value);
    }

    public float BloodPressureSys
    {
        get => GetClinical(LifeSystemsChannelCatalog.BloodPressureSys);
        set => SetClinical(LifeSystemsChannelCatalog.BloodPressureSys, value);
    }

    public float BloodPressureDia
    {
        get => GetClinical(LifeSystemsChannelCatalog.BloodPressureDia);
        set => SetClinical(LifeSystemsChannelCatalog.BloodPressureDia, value);
    }

    public void TickRealtime(float dt)
    {
        EnsureDefaults();
        bioRhythm.Tick(dt);
        float mod = bioRhythm.Modulation01();
        // Light realtime urgency: HR drifts with bio rhythm + adrenaline.
        float adrenaline = Get01(LifeSystemsChannelCatalog.Adrenaline);
        float hr01 = Get01(LifeSystemsChannelCatalog.HeartRate);
        float targetHr01 = hr01;
        if (LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.HeartRate, out var hrDef))
            targetHr01 = hrDef.setpoint01 + mod + adrenaline * 0.12f;
        Set01(LifeSystemsChannelCatalog.HeartRate, Mathf.Lerp(hr01, targetHr01, 1f - Mathf.Exp(-dt * 2f)));

        float sys = Get01(LifeSystemsChannelCatalog.BloodPressureSys);
        float dia = Get01(LifeSystemsChannelCatalog.BloodPressureDia);
        if (LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.BloodPressureSys, out var sysDef) &&
            LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.BloodPressureDia, out var diaDef))
        {
            Set01(LifeSystemsChannelCatalog.BloodPressureSys,
                Mathf.Lerp(sys, sysDef.setpoint01 + adrenaline * 0.08f + mod * 0.5f, 1f - Mathf.Exp(-dt * 1.5f)));
            Set01(LifeSystemsChannelCatalog.BloodPressureDia,
                Mathf.Lerp(dia, diaDef.setpoint01 + adrenaline * 0.05f, 1f - Mathf.Exp(-dt * 1.5f)));
        }

        float sysMm = GetClinical(LifeSystemsChannelCatalog.BloodPressureSys);
        float diaMm = GetClinical(LifeSystemsChannelCatalog.BloodPressureDia);
        float load = 0f;
        if (sysMm > 130f) load += (sysMm - 130f) / 90f;
        if (diaMm > 80f) load += (diaMm - 80f) / 60f;
        Set01(LifeSystemsChannelCatalog.HypertensiveLoad, Mathf.Clamp01(load * 0.5f));

        Set01(LifeSystemsChannelCatalog.LifeForce, lifeForce.lifeForce01);
        Set01(LifeSystemsChannelCatalog.BioRhythmAmplitude, bioRhythm.amplitude01);

        // Soft channel modulation from bio rhythm — do not fight illness locks.
        if (!ChannelBlockedByIllness(LifeSystemsChannelCatalog.ClearThought))
            AdjustTransient(LifeSystemsChannelCatalog.ClearThought, mod);
        if (!ChannelBlockedByIllness(LifeSystemsChannelCatalog.Attention))
            AdjustTransient(LifeSystemsChannelCatalog.Attention, mod);
        if (!ChannelBlockedByIllness(LifeSystemsChannelCatalog.Immune))
            AdjustTransient(LifeSystemsChannelCatalog.Immune, mod * 0.5f);
    }

    public bool ChannelBlockedByIllness(string channelId)
    {
        if (activeEffects == null || string.IsNullOrEmpty(channelId))
            return false;
        double now = Time.unscaledTimeAsDouble;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var ae = activeEffects[i];
            if (ae?.spec == null) continue;
            if (ae.spec.source != LifeSystemsEffectSource.Illness) continue;
            if (!IsEffectActive(ae, now)) continue;
            var deltas = ae.spec.channelDeltas;
            if (deltas == null) continue;
            for (int d = 0; d < deltas.Count; d++)
            {
                if (deltas[d] != null &&
                    string.Equals(deltas[d].channelId, channelId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    static bool IsEffectActive(LifeSystemsActiveEffect ae, double now)
    {
        if (ae.spec.durationSeconds <= 0f)
            return true;
        if (ae.appliedUnscaledTime < 0)
            return true;
        return now - ae.appliedUnscaledTime < ae.spec.durationSeconds;
    }

    void AdjustTransient(string channelId, float delta)
    {
        if (!LifeSystemsChannelCatalog.TryGet(channelId, out var def))
            return;
        float v = Get01(channelId);
        Set01(channelId, Mathf.Clamp(v + delta, def.softBandMin01 - 0.2f, def.softBandMax01 + 0.2f));
    }
}
