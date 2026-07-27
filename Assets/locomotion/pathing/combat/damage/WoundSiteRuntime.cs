using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authorable wound site: spline cut, close/heal, suture poles, SDF stamp hooks.</summary>
[Serializable]
public sealed class WoundSiteSpec
{
    public string id = Guid.NewGuid().ToString("N");
    public CombatDamageType sourceType = CombatDamageType.Slash;
    public string limbId = "Chest";
    public List<Vector3> splineLocal = new List<Vector3>();
    [Range(0f, 1f)] public float closeAmount;
    [Tooltip("Parabola coefficient aligned with spline for layered cut blend.")]
    public float parabolaCoefficient = 0.5f;
    [Tooltip("0 and 1 are infinite poles; in-between = rip risk.")]
    [Range(0f, 1f)] public float stitchHoldPotential = 0.5f;
    public float healStartTime;
    public float healDuration = 30f;
    public bool showHealedFillet;
    [Range(0f, 1f)] public float swollenFade01 = 1f;
    public string sdfCompositionKey;
    public string smellSignature;
    public bool open;
}

/// <summary>Runtime wound instance.</summary>
[Serializable]
public sealed class WoundSite
{
    public WoundSiteSpec spec = new WoundSiteSpec();
    public bool sutured;
    public float openedAt;

    public bool IsFullyClosed => spec != null && spec.closeAmount >= 0.999f;
    public bool IsHealedFilletVisible => IsFullyClosed && spec != null && spec.showHealedFillet;
}

/// <summary>Hosts wound sites; drives close/heal and optional SDF stamp keys.</summary>
[AddComponentMenu("Locomotion/Combat/Wound Site Runtime")]
public sealed class WoundSiteRuntime : MonoBehaviour
{
    public List<WoundSite> wounds = new List<WoundSite>();
    public Material layeredCutMaterial;
    public bool enableSdfStampHooks = true;

    public WoundSite OpenFromDamage(CombatDamageEvent evt, float amount01, bool autoSuture)
    {
        var site = new WoundSite
        {
            openedAt = Time.time,
            sutured = autoSuture,
            spec = new WoundSiteSpec
            {
                sourceType = evt.type,
                limbId = evt.limbId,
                closeAmount = autoSuture ? 0.15f : 0f,
                open = !autoSuture,
                smellSignature = evt.smellSignature,
                healStartTime = Time.time,
                parabolaCoefficient = 0.35f + amount01 * 0.4f,
                stitchHoldPotential = autoSuture ? 0.55f : 0f,
                sdfCompositionKey = enableSdfStampHooks
                    ? $"wound_{evt.type}_{evt.limbId}".ToLowerInvariant()
                    : null
            }
        };
        // Guestimate a short surface spline from hit + direction
        Vector3 local = transform.InverseTransformPoint(evt.worldHit);
        Vector3 dir = transform.InverseTransformDirection(evt.direction.normalized);
        site.spec.splineLocal.Add(local - dir * 0.05f);
        site.spec.splineLocal.Add(local);
        site.spec.splineLocal.Add(local + dir * (0.05f + amount01 * 0.1f));
        wounds.Add(site);
        TickHeal(0f);
        return site;
    }

    public void TickHeal(float dt)
    {
        if (wounds == null) return;
        for (int i = 0; i < wounds.Count; i++)
        {
            var w = wounds[i];
            if (w?.spec == null) continue;
            if (w.IsFullyClosed)
            {
                w.spec.showHealedFillet = true;
                float age = Time.time - w.spec.healStartTime;
                float t = w.spec.healDuration > 1e-3f ? age / w.spec.healDuration : 1f;
                w.spec.swollenFade01 = Mathf.Clamp01(1f - t);
            }
            else if (w.sutured)
            {
                w.spec.closeAmount = Mathf.MoveTowards(w.spec.closeAmount, 1f, dt * 0.05f);
            }
        }
    }

    void Update() => TickHeal(Time.deltaTime);

    public static float EffectiveRipRisk(float stitchHoldPotential)
    {
        // 0 and 1 are infinite poles (no rip / instant rip continuum endpoints)
        if (stitchHoldPotential <= 0f || stitchHoldPotential >= 1f) return 0f;
        return stitchHoldPotential;
    }
}
