using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One contact target for multi-actor heavy-petting / kiss accent IK.</summary>
[Serializable]
public sealed class HeavyPettingIKContactSpec
{
    public string sourceSectionOrBone = "LeftHand";
    public string targetActorKey;
    public string targetBoneOrLip = "LipMidpoint";
    [Range(0.1f, 1f)] public float stiffness = 0.55f;
    public float maxErrorMeters = 0.35f;
}

/// <summary>
/// Multi-actor heavy-petting / kiss accent training descriptor.
/// Not a LoveMakingMoveKind — Caress or Kiss may reference this asset.
/// </summary>
[CreateAssetMenu(fileName = "HeavyPettingIKAnimation", menuName = "Locomotion/Love Making/Heavy Petting IK Animation")]
public sealed class HeavyPettingIKAnimation : ScriptableObject
{
    public List<string> trainAgainstActorKeys = new List<string>();
    public List<GameObject> trainAgainstActors = new List<GameObject>();
    public List<HeavyPettingIKContactSpec> contacts = new List<HeavyPettingIKContactSpec>();
    public PhysicsIKTrainingRunAsset trainingRun;
    public PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.LoveHeavyPetting;
    public string[] ragdollSectionInclude;
    public string[] ragdollSectionExclude;
    [Range(0f, 1f)] public float minIntensity01;
    [Range(0f, 1f)] public float maxIntensity01 = 1f;
    public string animationKey;

    public bool MatchesIntensity(float intensity01)
    {
        float i = Mathf.Clamp01(intensity01);
        return i >= minIntensity01 - 1e-4f && i <= maxIntensity01 + 1e-4f;
    }
}
