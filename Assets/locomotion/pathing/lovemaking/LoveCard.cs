using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Love physics card inheriting wrestling card structure: desires/requirements for love,
/// gentler impulses, consent gate, physicality for calendar pink→red tint.
/// </summary>
[System.Serializable]
public class LoveCard : WrestlingCard
{
    [Header("Love")]
    public LoveMakingMode loveMode = LoveMakingMode.Tender;
    public LoveMakingMoveKind loveMoveKind = LoveMakingMoveKind.Embrace;
    [Tooltip("Prefer intimate anim tag variants (.intimate).")]
    public bool intimateStyle;

    public List<LoveDesire> requiredDesires = new List<LoveDesire>();
    public List<LoveDesire> optionalDesires = new List<LoveDesire>();
    [Range(0f, 1f)] public float desireIntensity01 = 0.5f;
    [Tooltip("0 = tender/emotional … 1 = highly physical (calendar pink→red).")]
    [Range(0f, 1f)] public float physicality01 = 0.35f;
    public bool requiresConsent = true;
    public int maxParticipants = 2;
    public string preferredPartnerAspect;

    [Header("Kiss")]
    [Tooltip("0–1 kiss animation intensity (peck → making out). Default 0.35 (standard kiss).")]
    [Range(0f, 1f)] public float kissAnimationIntensity = LoveMakingAnimationGroup.DefaultKissIntensity;
    [Tooltip("Optional explicit animation key from {P:kiss|kiss-animation=...}. Empty = intensity band.")]
    public string kissAnimationKey;
    [Tooltip("Override jaw open for kiss; negative derives from kissAnimationIntensity.")]
    public float kissJawOpen01 = -1f;
    public string selfActorKey;
    public string partnerActorKey;
    public HeavyPettingIKAnimation heavyPettingIk;
    [Tooltip("When true, partner responded poorly to an unrequited kiss (visceral chemistry).")]
    public bool kissResponseNegative;

    public LoveCard()
    {
        isWrestlingGoal = false;
        isLoveMakingGoal = true;
        physicalPathingTag = "lovemaking";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "lovemaking";
        mode = WrestlingMode.Play;
        moveKind = WrestlingMoveKind.LockGrapple;
        sizeGate = WrestlingBodySizeGate.Permissive;
        dropHitBoneName = "Chest";
    }

    public string LoveAnimationGroupTag =>
        LoveMakingAnimationGroup.ForMove(loveMoveKind, intimateStyle, kissAnimationIntensity, kissAnimationKey);

    public bool MeetsLoveRequirements(GameObject actor, GameObject partner, RagdollSystem actorRagdoll = null)
    {
        if (partner == null && opponent == null)
            return false;
        GameObject p = partner != null ? partner : opponent;
        if (!MeetsWrestlingRequirements(actor, p, actorRagdoll))
            return false;
        if (requiresConsent)
        {
            var profile = p.GetComponent<RomanceProfile>();
            if (profile != null && !profile.AllowsIntimacyWith(actor))
                return false;
        }
        if (maxParticipants < 2)
            return false;
        return true;
    }

    public static LoveCard Generate(
        LoveMakingMode mode,
        LoveMakingMoveKind kind,
        GameObject partner,
        RagdollState state,
        bool intimateStyle = false,
        float kissAnimationIntensity = -1f,
        string kissAnimationKey = null)
    {
        float kissI = kissAnimationIntensity >= 0f
            ? Mathf.Clamp01(kissAnimationIntensity)
            : LoveMakingAnimationGroup.DefaultKissIntensity;
        var card = new LoveCard
        {
            loveMode = mode,
            loveMoveKind = kind,
            intimateStyle = intimateStyle,
            opponent = partner,
            sectionName = $"lovemaking_{mode}_{kind}",
            description = $"{mode} {kind}",
            isLoveMakingGoal = true,
            isWrestlingGoal = false,
            physicalPathingTag = $"lovemaking_{kind.ToString().ToLowerInvariant()}",
            physicality01 = DefaultPhysicality(kind),
            desireIntensity01 = DefaultDesireIntensity(kind),
            requiredDesires = DefaultRequiredDesires(kind),
            optionalDesires = DefaultOptionalDesires(kind),
            requiredLimbBones = DefaultLoveLimbs(kind),
            optionalLimbBones = new List<string> { "Head", "Chest" },
            impulseStack = BuildLoveImpulseStack(kind),
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 120f, maxTorque = 40f, maxVelocityChange = 1.2f },
            dropHitBoneName = kind == LoveMakingMoveKind.Kiss ? "Head" : "Chest",
            moveKind = WrestlingMoveKind.LockGrapple,
            mode = WrestlingMode.Play,
            kissAnimationIntensity = kissI,
            kissAnimationKey = kissAnimationKey
        };
        return card;
    }

    public void ApplyKissLemma(LoveMakingKissLemmaProperties props)
    {
        if (!string.IsNullOrEmpty(props.lemmaHint) && props.kissAnimationIntensity < 0f)
            kissAnimationIntensity = LoveMakingAnimationGroup.DefaultIntensityForLemma(props.lemmaHint);
        if (props.kissAnimationIntensity >= 0f)
            kissAnimationIntensity = Mathf.Clamp01(props.kissAnimationIntensity);
        if (!string.IsNullOrEmpty(props.kissAnimation))
            kissAnimationKey = props.kissAnimation;
    }

    public static float DefaultPhysicality(LoveMakingMoveKind kind)
    {
        switch (kind)
        {
            case LoveMakingMoveKind.Approach:
            case LoveMakingMoveKind.Part:
                return 0.15f;
            case LoveMakingMoveKind.Nuzzle:
            case LoveMakingMoveKind.Hold:
                return 0.35f;
            case LoveMakingMoveKind.Kiss:
            case LoveMakingMoveKind.Embrace:
                return 0.55f;
            case LoveMakingMoveKind.Caress:
            case LoveMakingMoveKind.DanceClose:
                return 0.7f;
            default:
                return 0.4f;
        }
    }

    public static float DefaultDesireIntensity(LoveMakingMoveKind kind) =>
        Mathf.Clamp01(DefaultPhysicality(kind) + 0.15f);

    public static List<LoveDesire> DefaultRequiredDesires(LoveMakingMoveKind kind)
    {
        switch (kind)
        {
            case LoveMakingMoveKind.Kiss:
                return new List<LoveDesire> { LoveDesire.Affection, LoveDesire.Closeness };
            case LoveMakingMoveKind.Caress:
                return new List<LoveDesire> { LoveDesire.Pleasure, LoveDesire.Comfort };
            case LoveMakingMoveKind.Hold:
            case LoveMakingMoveKind.Embrace:
                return new List<LoveDesire> { LoveDesire.Closeness };
            default:
                return new List<LoveDesire> { LoveDesire.Affection };
        }
    }

    public static List<LoveDesire> DefaultOptionalDesires(LoveMakingMoveKind kind)
    {
        if (kind == LoveMakingMoveKind.DanceClose || kind == LoveMakingMoveKind.Caress)
            return new List<LoveDesire> { LoveDesire.Play, LoveDesire.Trust };
        return new List<LoveDesire> { LoveDesire.Trust };
    }

    public static List<string> DefaultLoveLimbs(LoveMakingMoveKind kind)
    {
        switch (kind)
        {
            case LoveMakingMoveKind.Kiss:
            case LoveMakingMoveKind.Nuzzle:
                return new List<string> { "Head", "Chest" };
            case LoveMakingMoveKind.Caress:
                return new List<string> { "LeftHand", "RightHand" };
            case LoveMakingMoveKind.Embrace:
            case LoveMakingMoveKind.Hold:
            case LoveMakingMoveKind.DanceClose:
                return new List<string> { "LeftHand", "RightHand", "Chest" };
            default:
                return new List<string> { "Hips", "Chest" };
        }
    }

    public static List<ImpulseAction> BuildLoveImpulseStack(LoveMakingMoveKind kind)
    {
        float t = 0.18f;
        var list = new List<ImpulseAction>();
        switch (kind)
        {
            case LoveMakingMoveKind.Approach:
                list.Add(LoveImpulse("abdomen", 0.35f, Vector3.forward, t));
                break;
            case LoveMakingMoveKind.Embrace:
            case LoveMakingMoveKind.Hold:
                list.Add(LoveImpulse("left_shoulder", 0.4f, Vector3.forward, t));
                list.Add(LoveImpulse("right_shoulder", 0.4f, Vector3.forward, t));
                list.Add(LoveImpulse("abdomen", 0.3f, Vector3.forward, t));
                break;
            case LoveMakingMoveKind.Kiss:
                list.Add(LoveImpulse("neck", 0.35f, Vector3.forward, t));
                list.Add(LoveImpulse("abdomen", 0.25f, Vector3.up, t));
                break;
            case LoveMakingMoveKind.Caress:
                list.Add(LoveImpulse("left_shoulder", 0.3f, Vector3.right, t));
                list.Add(LoveImpulse("right_shoulder", 0.3f, Vector3.left, t));
                break;
            default:
                list.Add(LoveImpulse("abdomen", 0.25f, Vector3.forward, t));
                break;
        }
        return list;
    }

    static ImpulseAction LoveImpulse(string muscle, float activation, Vector3 dir, float duration)
    {
        return new ImpulseAction
        {
            muscleGroup = muscle,
            activation = activation,
            duration = duration,
            forceDirection = dir.normalized,
            curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };
    }
}
