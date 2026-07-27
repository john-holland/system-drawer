using System.Collections.Generic;
using UnityEngine;

/// <summary>Applies combat damage: mask → limb/overall → LifeSystems → wound → smell.</summary>
public static class CombatDamageApplier
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        var result = new CombatDamageResult();
        if (evt == null || evt.target == null)
            return result;

        float remaining = evt.amount01;
        var mask = evt.target.GetComponentInChildren<DamageMask>();
        if (mask != null)
        {
            remaining = mask.Absorb(evt);
            result.clothingAbsorbed01 = evt.amount01 - remaining;
        }

        if (remaining <= 1e-4f)
        {
            result.fullyBlockedByMask = true;
            return result;
        }

        // Ward absorb on defender
        var wards = evt.target.GetComponent<DefendWardRuntime>();
        if (wards != null)
        {
            float absorbed = wards.TryAbsorb(evt, remaining);
            remaining -= absorbed;
            result.wardAbsorbed01 = absorbed;
        }

        if (remaining <= 1e-4f)
        {
            result.fullyBlockedByWard = true;
            return result;
        }

        var limbs = evt.target.GetComponent<LimbIntegrityState>()
                    ?? evt.target.AddComponent<LimbIntegrityState>();
        limbs.EnsureDefaults();
        limbs.ApplyDamage(evt.limbId, remaining, evt.healthMode, out bool detached);
        result.limbDetached = detached;
        result.appliedToActor01 = remaining;

        // LifeSystems / organ trauma
        var sheet = LifeSystemsServices.Instance != null
            ? LifeSystemsServices.Instance.GetOrCreate(evt.target)
            : evt.target.GetComponent<LifeSystemsSheet>();
        if (sheet == null)
            sheet = evt.target.AddComponent<LifeSystemsSheet>();
        sheet.EnsureDefaults();
        sheet.Adjust01(LifeSystemsChannelCatalog.Fatigue, remaining * 0.15f);
        sheet.Adjust01(LifeSystemsChannelCatalog.Adrenaline, remaining * 0.2f);
        sheet.Adjust01(LifeSystemsChannelCatalog.Morale, -remaining * 0.1f);
        ApplyOrganTrauma(sheet, evt.limbId, remaining);

        if (evt.createWound)
        {
            var woundHost = evt.target.GetComponent<WoundSiteRuntime>()
                            ?? evt.target.AddComponent<WoundSiteRuntime>();
            result.wound = woundHost.OpenFromDamage(evt, remaining, autoSuture: evt.autoSuture);
        }

        if (!string.IsNullOrEmpty(evt.smellSignature) ||
            evt.type == CombatDamageType.Heat ||
            evt.type == CombatDamageType.Explosion)
        {
            EmitSmell(evt.target, evt);
            result.smellEmitted = true;
        }

        result.ok = true;
        return result;
    }

    static void ApplyOrganTrauma(LifeSystemsSheet sheet, string limbId, float amount01)
    {
        if (sheet?.organs == null || amount01 <= 1e-4f) return;
        var region = GuessRegion(limbId);
        var part = BodyPartLifeModifier.FindHost(sheet, region);
        float traumaScale = part != null ? Mathf.Max(0f, part.organTraumaMultiplier) : 1f;
        float rawDelta = -amount01 * traumaScale;
        var difficulty = LifeSystemsDifficulty.Normal;
        var organs = OrganCatalog.Organs;
        for (int i = 0; i < organs.Count; i++)
        {
            var def = organs[i];
            if (def == null || def.hostRegion != region) continue;
            sheet.organs.ApplyRawDelta(def.id, rawDelta, difficulty);
        }
    }

    static OrganHostRegion GuessRegion(string limbId)
    {
        if (string.IsNullOrEmpty(limbId)) return OrganHostRegion.Torso;
        if (limbId.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return OrganHostRegion.Head;
        if (limbId.IndexOf("abdomen", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return OrganHostRegion.Abdomen;
        if (limbId.IndexOf("neck", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return OrganHostRegion.NeckTorso;
        return OrganHostRegion.Torso;
    }

    static void EmitSmell(GameObject target, CombatDamageEvent evt)
    {
        var emitter = target.GetComponent<Locomotion.Senses.SmellEmitter>()
                      ?? target.AddComponent<Locomotion.Senses.SmellEmitter>();
        string sig = evt.smellSignature;
        if (string.IsNullOrEmpty(sig))
        {
            if (evt.type == CombatDamageType.Heat || evt.type == CombatDamageType.Explosion)
                sig = "burned_flesh";
            else
                sig = "blood";
        }
        emitter.signature = sig;
        emitter.intensity = Mathf.Clamp01(evt.amount01 + 0.2f);
        emitter.radius = 2f + evt.amount01 * 4f;
    }
}

public sealed class CombatDamageResult
{
    public bool ok;
    public bool fullyBlockedByMask;
    public bool fullyBlockedByWard;
    public float clothingAbsorbed01;
    public float wardAbsorbed01;
    public float appliedToActor01;
    public bool limbDetached;
    public bool smellEmitted;
    public WoundSite wound;
}
