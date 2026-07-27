using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LimbIntegrityEntry
{
    public string limbId = "Chest";
    [Range(0f, 1f)] public float health01 = 1f;
    [Range(0f, 1f)] public float structural01 = 1f;
    public bool detached;
}

/// <summary>Per-limb HP / structural integrity next to LifeSystems.</summary>
[AddComponentMenu("Locomotion/Combat/Limb Integrity")]
public sealed class LimbIntegrityState : MonoBehaviour
{
    public List<LimbIntegrityEntry> limbs = new List<LimbIntegrityEntry>();
    [Range(0f, 1f)] public float overallHealth01 = 1f;

    void Awake() => EnsureDefaults();

    public void EnsureDefaults()
    {
        if (limbs == null) limbs = new List<LimbIntegrityEntry>();
        if (limbs.Count == 0)
        {
            limbs.Add(new LimbIntegrityEntry { limbId = "Head" });
            limbs.Add(new LimbIntegrityEntry { limbId = "Chest" });
            limbs.Add(new LimbIntegrityEntry { limbId = "Abdomen" });
            limbs.Add(new LimbIntegrityEntry { limbId = "LeftArm" });
            limbs.Add(new LimbIntegrityEntry { limbId = "RightArm" });
            limbs.Add(new LimbIntegrityEntry { limbId = "LeftLeg" });
            limbs.Add(new LimbIntegrityEntry { limbId = "RightLeg" });
        }
    }

    public LimbIntegrityEntry GetOrCreate(string limbId)
    {
        EnsureDefaults();
        if (string.IsNullOrEmpty(limbId)) limbId = "Chest";
        for (int i = 0; i < limbs.Count; i++)
            if (limbs[i] != null && string.Equals(limbs[i].limbId, limbId, StringComparison.OrdinalIgnoreCase))
                return limbs[i];
        var e = new LimbIntegrityEntry { limbId = limbId };
        limbs.Add(e);
        return e;
    }

    public float GetHealth01(string limbId) => GetOrCreate(limbId).health01;

    public void ApplyDamage(string limbId, float amount01, DamageHealthMode mode, out bool detachedNow)
    {
        detachedNow = false;
        amount01 = Mathf.Max(0f, amount01);
        if (mode == DamageHealthMode.Overall)
        {
            overallHealth01 = Mathf.Clamp01(overallHealth01 - amount01);
            return;
        }
        var e = GetOrCreate(limbId);
        e.health01 = Mathf.Clamp01(e.health01 - amount01);
        e.structural01 = Mathf.Clamp01(e.structural01 - amount01 * 0.75f);
        if (e.structural01 <= 0.02f && !e.detached)
        {
            e.detached = true;
            detachedNow = true;
        }
    }

    public bool MeetsRequirement(CombatLimbHealthRequirement req)
    {
        if (req == null) return true;
        return GetHealth01(req.limbId) >= req.minIntegrity01 && !GetOrCreate(req.limbId).detached;
    }
}
