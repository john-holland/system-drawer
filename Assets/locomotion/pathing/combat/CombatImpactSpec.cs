using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Bone/region coverage that absorbs matching damage types.</summary>
[Serializable]
public sealed class DefendWard
{
    public string id = Guid.NewGuid().ToString("N");
    public string boneOrRegion = "Forearm_L";
    [Range(0f, 180f)] public float blockConeDeg = 90f;
    public Vector3 localForward = Vector3.forward;
    [Range(0f, 1f)] public float absorb01 = 0.7f;
    public float staminaCost01 = 0.1f;
    public List<CombatDamageType> absorbs = new List<CombatDamageType>
    {
        CombatDamageType.Blunt, CombatDamageType.Slash, CombatDamageType.Pierce
    };

    public bool Absorbs(CombatDamageType type)
    {
        if (absorbs == null) return false;
        for (int i = 0; i < absorbs.Count; i++)
            if (absorbs[i] == type) return true;
        return false;
    }
}

/// <summary>Ragdoll impact + damage payload on a CombatCard.</summary>
[Serializable]
public sealed class CombatImpactSpec
{
    public CombatDamageType damageType = CombatDamageType.Blunt;
    [Range(0f, 1f)] public float damage01 = 0.25f;
    public string primaryLimbBone = "Chest";
    public List<string> secondaryLimbBones = new List<string>();
    public float impulseMagnitude = 80f;
    public bool throughOrStop;
    public DamageHealthMode healthMode = DamageHealthMode.PerLimb;
    public string cutterProfileId;
    public string cutProfileId;
    public CombatMaterialKind materialKind = CombatMaterialKind.Human;
}

/// <summary>Prebake / destructive environment requirements before card can run.</summary>
[Serializable]
public sealed class CombatPrebakeRequirements
{
    public bool requiresDestructiveEnv;
    public List<string> envTags = new List<string>();
    public bool dirtyNavBakeOnCommit;
}

/// <summary>Minimum limb integrity required to execute the card.</summary>
[Serializable]
public sealed class CombatLimbHealthRequirement
{
    public string limbId = "RightArm";
    [Range(0f, 1f)] public float minIntegrity01 = 0.2f;
}
