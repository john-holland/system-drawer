using System.Collections.Generic;
using UnityEngine;

/// <summary>Routes card damage type to specialized family handlers.</summary>
public static class CombatDamageFamilyRouter
{
    public static CombatDamageResult ApplyForCard(CombatCard card, CombatDamageEvent evt)
    {
        if (evt == null) return new CombatDamageResult();
        if (card?.impact != null)
        {
            evt.type = card.impact.damageType;
            evt.through = card.impact.throughOrStop;
            evt.healthMode = card.impact.healthMode;
            evt.cutterProfileId = card.impact.cutterProfileId;
            evt.cutProfileId = card.impact.cutProfileId;
            evt.materialKind = card.impact.materialKind;
        }

        switch (evt.type)
        {
            case CombatDamageType.Bullet:
                return BulletDamageHandler.Apply(evt);
            case CombatDamageType.Slash:
                return SlashDamageHandler.Apply(evt);
            case CombatDamageType.Electric:
                return ElectricDamageHandler.Apply(evt);
            case CombatDamageType.Laser:
            case CombatDamageType.ContinuousCutter:
                return LaserCutterDamageHandler.Apply(evt);
            case CombatDamageType.Radiation:
                return RadiationDamageHandler.Apply(evt);
            case CombatDamageType.Explosion:
            case CombatDamageType.Gib:
                return ExplosionGibDamageHandler.Apply(evt);
            default:
                return CombatDamageApplier.Apply(evt);
        }
    }
}

public static class BulletDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null || evt.target == null) return new CombatDamageResult();
        // Simulated hit: ray from attacker through target
        if (evt.attacker != null)
        {
            Vector3 origin = evt.attacker.transform.position + Vector3.up * 1.2f;
            Vector3 dir = evt.direction.sqrMagnitude > 1e-6f
                ? evt.direction.normalized
                : (evt.target.transform.position - origin).normalized;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 120f))
            {
                evt.worldHit = hit.point;
                evt.direction = dir;
                if (evt.through)
                {
                    // Exit wound: second soft hit along ray
                    var exitEvt = Clone(evt);
                    exitEvt.worldHit = hit.point + dir * 0.12f;
                    exitEvt.amount01 *= 0.55f;
                    exitEvt.limbId = evt.limbId;
                    CombatDamageApplier.Apply(exitEvt);
                }
            }
        }
        evt.createWound = true;
        return CombatDamageApplier.Apply(evt);
    }

    static CombatDamageEvent Clone(CombatDamageEvent e) => new CombatDamageEvent
    {
        attacker = e.attacker,
        target = e.target,
        type = e.type,
        worldHit = e.worldHit,
        direction = e.direction,
        depth01 = e.depth01,
        amount01 = e.amount01,
        through = e.through,
        healthMode = e.healthMode,
        limbId = e.limbId,
        createWound = e.createWound,
        materialKind = e.materialKind
    };
}

public static class SlashDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null) return new CombatDamageResult();
        evt.type = CombatDamageType.Slash;
        evt.autoSuture = false; // knife game: leave open
        evt.createWound = true;
        var result = CombatDamageApplier.Apply(evt);
        // Bone/sinew guestimate already baked into WoundSiteRuntime spline
        return result;
    }
}

public static class ElectricDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null || evt.target == null) return new CombatDamageResult();
        evt.type = CombatDamageType.Electric;
        evt.smellSignature = "ozone_burn";
        // Jump points along rough bone chain
        var rd = evt.target.GetComponent<RagdollSystem>();
        string[] jumps = { "Hand_R", "Forearm_R", "UpperArm_R", "Chest", "Head" };
        CombatDamageResult last = new CombatDamageResult();
        for (int i = 0; i < jumps.Length; i++)
        {
            var jump = Clone(evt);
            jump.limbId = jumps[i];
            jump.amount01 = evt.amount01 * (0.35f / jumps.Length);
            jump.depth01 = 0.15f;
            if (rd != null)
            {
                var t = rd.GetBoneTransform(jumps[i]);
                if (t != null) jump.worldHit = t.position;
            }
            last = CombatDamageApplier.Apply(jump);
        }
        return last;
    }

    static CombatDamageEvent Clone(CombatDamageEvent e) => new CombatDamageEvent
    {
        attacker = e.attacker,
        target = e.target,
        type = CombatDamageType.Electric,
        worldHit = e.worldHit,
        direction = e.direction,
        amount01 = e.amount01,
        healthMode = e.healthMode,
        createWound = true,
        smellSignature = e.smellSignature
    };
}

public static class LaserCutterDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null) return new CombatDamageResult();
        // Depth from authored depth01: scrape→full
        evt.createWound = true;
        evt.autoSuture = false;
        if (evt.depth01 < 0.2f)
            evt.amount01 *= 0.35f; // scrape
        else if (evt.depth01 > 0.85f)
            evt.amount01 = Mathf.Max(evt.amount01, 0.7f); // chainsaw-class
        return CombatDamageApplier.Apply(evt);
    }
}

public static class RadiationDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null || evt.target == null) return new CombatDamageResult();
        evt.type = CombatDamageType.Radiation;
        evt.createWound = true;
        evt.smellSignature = "burned_flesh";
        // Texture-layer burn map approximated as wound with slow heal
        var result = CombatDamageApplier.Apply(evt);
        if (result.wound?.spec != null)
        {
            result.wound.spec.healDuration = 120f;
            result.wound.spec.swollenFade01 = 1f;
        }
        return result;
    }
}

public static class ExplosionGibDamageHandler
{
    public static CombatDamageResult Apply(CombatDamageEvent evt)
    {
        if (evt == null || evt.target == null) return new CombatDamageResult();
        evt.type = evt.type == CombatDamageType.Gib ? CombatDamageType.Gib : CombatDamageType.Explosion;
        evt.amount01 = Mathf.Max(evt.amount01, 0.6f);
        evt.createWound = true;
        evt.smellSignature = "burned_flesh";

        var limbs = evt.target.GetComponent<LimbIntegrityState>()
                    ?? evt.target.AddComponent<LimbIntegrityState>();
        limbs.EnsureDefaults();
        bool comical = evt.cutterProfileId != null &&
                       evt.cutterProfileId.IndexOf("comical", System.StringComparison.OrdinalIgnoreCase) >= 0;
        float force = comical ? 0.35f : 0.85f;

        // Detach a limb and stamp sdf cap key
        var entry = limbs.GetOrCreate(evt.limbId);
        entry.structural01 = Mathf.Max(0f, entry.structural01 - force);
        if (entry.structural01 <= 0.05f || evt.type == CombatDamageType.Gib)
            entry.detached = true;

        var result = CombatDamageApplier.Apply(evt);
        result.limbDetached = entry.detached;
        if (result.wound?.spec != null)
        {
            result.wound.spec.sdfCompositionKey =
                $"gib_cap_{evt.cutProfileId ?? "torn"}_{evt.materialKind}".ToLowerInvariant();
        }

        var rb = evt.target.GetComponentInChildren<Rigidbody>();
        if (rb != null)
            rb.AddExplosionForce(force * 400f, evt.worldHit, 2.5f, 0.5f, ForceMode.Impulse);
        return result;
    }
}
