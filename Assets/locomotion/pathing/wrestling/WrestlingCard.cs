using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wrestling physics card: limb requirements, body-size gates, move/mode, and branch metadata.
/// </summary>
[System.Serializable]
public class WrestlingCard : GoodSection
{
    [Header("Wrestling")]
    public WrestlingMode mode = WrestlingMode.Play;
    public WrestlingMoveKind moveKind = WrestlingMoveKind.LockGrapple;
    [Tooltip("Prefer kayfabe / pro animation tag variants (.pro).")]
    public bool professionalStyle;

    public List<string> requiredLimbBones = new List<string>();
    public List<string> optionalLimbBones = new List<string>();
    public WrestlingBodySizeGate sizeGate = WrestlingBodySizeGate.Permissive;

    [Tooltip("DropOn strike / contact bone on opponent.")]
    public string dropHitBoneName = "Chest";

    [Tooltip("Procedural Counter facing offset (degrees).")]
    public float counterAngleDeg;

    [Tooltip("Optional bespoke counter animation group tag.")]
    public string bespokeCounterAnimTag;

    public WrestlingMoveKind liftBranch = WrestlingMoveKind.Throw;
    public WrestlingMoveKind throwBranch = WrestlingMoveKind.DropOn;

    [Header("Slow-time / input")]
    public KeyCode hotkey = KeyCode.None;
    public string inputActionName;
    public Transform aimAnchorOverride;

    [System.NonSerialized] public GameObject opponent;

    public WrestlingCard()
    {
        isWrestlingGoal = true;
        physicalPathingTag = "wrestling";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "wrestling";
    }

    public string AnimationGroupTag =>
        !string.IsNullOrEmpty(bespokeCounterAnimTag) && moveKind == WrestlingMoveKind.Counter
            ? bespokeCounterAnimTag
            : WrestlingAnimationGroup.ForMove(moveKind, professionalStyle);

    public bool MeetsWrestlingRequirements(GameObject actor, GameObject opp, RagdollSystem actorRagdoll = null)
    {
        if (opp == null && opponent == null)
            return false;
        GameObject o = opp != null ? opp : opponent;
        if (sizeGate != null && !sizeGate.Passes(actor, o, out _))
            return false;
        if (actorRagdoll == null && actor != null)
            actorRagdoll = actor.GetComponent<RagdollSystem>();
        if (requiredLimbBones == null || requiredLimbBones.Count == 0)
            return true;
        if (actorRagdoll == null)
            return true;
        for (int i = 0; i < requiredLimbBones.Count; i++)
        {
            string bone = requiredLimbBones[i];
            if (string.IsNullOrEmpty(bone)) continue;
            if (actorRagdoll.GetBoneTransform(bone) == null)
                return false;
        }
        return true;
    }

    public int CountOptionalLimbsPresent(RagdollSystem actorRagdoll)
    {
        if (actorRagdoll == null || optionalLimbBones == null) return 0;
        int n = 0;
        for (int i = 0; i < optionalLimbBones.Count; i++)
        {
            if (!string.IsNullOrEmpty(optionalLimbBones[i]) &&
                actorRagdoll.GetBoneTransform(optionalLimbBones[i]) != null)
                n++;
        }
        return n;
    }

    public Vector3 ResolveAimAnchorWorld(GameObject opp)
    {
        if (aimAnchorOverride != null)
            return aimAnchorOverride.position;
        GameObject o = opp != null ? opp : opponent;
        if (o == null) return Vector3.zero;
        if (!string.IsNullOrEmpty(dropHitBoneName))
        {
            var rd = o.GetComponent<RagdollSystem>();
            Transform t = rd != null ? rd.GetBoneTransform(dropHitBoneName) : null;
            if (t != null) return t.position;
        }
        if (requiredLimbBones != null && requiredLimbBones.Count > 0)
        {
            var rd = o.GetComponent<RagdollSystem>();
            if (rd != null)
            {
                Transform t = rd.GetBoneTransform(requiredLimbBones[0]);
                if (t != null) return t.position;
            }
        }
        return o.transform.position;
    }

    public static WrestlingCard GenerateLunge(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Play, WrestlingMoveKind.LungeShootIn, opponent, state, pro);

    public static WrestlingCard GenerateLock(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Play, WrestlingMoveKind.LockGrapple, opponent, state, pro);

    public static WrestlingCard GenerateThrow(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Play, WrestlingMoveKind.Throw, opponent, state, pro);

    public static WrestlingCard GenerateLift(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Play, WrestlingMoveKind.Lift, opponent, state, pro);

    public static WrestlingCard GenerateDropOn(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Pin, WrestlingMoveKind.DropOn, opponent, state, pro);

    public static WrestlingCard GenerateCounter(GameObject opponent, RagdollState state, bool pro = false) =>
        Generate(WrestlingMode.Play, WrestlingMoveKind.Counter, opponent, state, pro);

    public static WrestlingCard Generate(
        WrestlingMode mode,
        WrestlingMoveKind kind,
        GameObject opponent,
        RagdollState state,
        bool professionalStyle = false,
        WrestlingBodySizeGate gate = null)
    {
        var card = new WrestlingCard
        {
            mode = mode,
            moveKind = kind,
            professionalStyle = professionalStyle,
            opponent = opponent,
            sizeGate = gate ?? DefaultGateFor(kind),
            sectionName = $"wrestling_{mode}_{kind}",
            description = $"{mode} {kind}",
            isWrestlingGoal = true,
            physicalPathingTag = $"wrestling_{kind.ToString().ToLowerInvariant()}",
            requiredLimbBones = DefaultRequiredLimbs(kind),
            optionalLimbBones = DefaultOptionalLimbs(kind),
            impulseStack = BuildImpulseStack(kind),
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 800f, maxTorque = 200f, maxVelocityChange = 4f }
        };
        return card;
    }

    public static WrestlingBodySizeGate DefaultGateFor(WrestlingMoveKind kind)
    {
        // Throw/Lift/DropOn need a non-trivial opponent mass (blocks "suplex baby" by default).
        if (kind == WrestlingMoveKind.Lift || kind == WrestlingMoveKind.Throw || kind == WrestlingMoveKind.DropOn)
        {
            return new WrestlingBodySizeGate
            {
                minOpponentMass = 25f,
                maxOpponentMass = 5000f,
                minOpponentExtentMagnitude = 0.4f
            };
        }
        return WrestlingBodySizeGate.Permissive;
    }

    public static List<string> DefaultRequiredLimbs(WrestlingMoveKind kind)
    {
        switch (kind)
        {
            case WrestlingMoveKind.LungeShootIn:
                return new List<string> { "Hips", "LeftFoot", "RightFoot" };
            case WrestlingMoveKind.Pull:
            case WrestlingMoveKind.Push:
            case WrestlingMoveKind.LockGrapple:
            case WrestlingMoveKind.Pry:
                return new List<string> { "LeftHand", "RightHand" };
            case WrestlingMoveKind.Block:
            case WrestlingMoveKind.Deflect:
                return new List<string> { "LeftForeArm", "RightForeArm" };
            case WrestlingMoveKind.Lift:
            case WrestlingMoveKind.Throw:
            case WrestlingMoveKind.DropOn:
                return new List<string> { "LeftHand", "RightHand", "Hips" };
            case WrestlingMoveKind.Counter:
                return new List<string> { "Hips", "Chest" };
            default:
                return new List<string> { "LeftHand", "RightHand" };
        }
    }

    public static List<string> DefaultOptionalLimbs(WrestlingMoveKind kind)
    {
        if (kind == WrestlingMoveKind.Throw || kind == WrestlingMoveKind.Lift)
            return new List<string> { "Head", "Chest" };
        return new List<string>();
    }

    public static List<ImpulseAction> BuildImpulseStack(WrestlingMoveKind kind)
    {
        float t = 0.12f;
        var list = new List<ImpulseAction>();
        switch (kind)
        {
            case WrestlingMoveKind.LungeShootIn:
                list.Add(Timed("left_hip", 0.9f, Vector3.forward, t));
                list.Add(Timed("right_hip", 0.9f, Vector3.forward, t));
                list.Add(Timed("abdomen", 0.7f, Vector3.forward, t));
                break;
            case WrestlingMoveKind.Pull:
                list.Add(Timed("left_shoulder", 0.85f, Vector3.back, t));
                list.Add(Timed("right_shoulder", 0.85f, Vector3.back, t));
                break;
            case WrestlingMoveKind.Push:
                list.Add(Timed("left_shoulder", 0.85f, Vector3.forward, t));
                list.Add(Timed("right_shoulder", 0.85f, Vector3.forward, t));
                break;
            case WrestlingMoveKind.LockGrapple:
                list.Add(Timed("left_shoulder", 0.8f, Vector3.down, t));
                list.Add(Timed("right_shoulder", 0.8f, Vector3.down, t));
                list.Add(Timed("abdomen", 0.6f, Vector3.forward, t));
                break;
            case WrestlingMoveKind.Pry:
                list.Add(Timed("left_elbow", 0.75f, Vector3.up, t));
                list.Add(Timed("right_elbow", 0.75f, Vector3.up, t));
                break;
            case WrestlingMoveKind.Block:
            case WrestlingMoveKind.Deflect:
                list.Add(Timed("left_shoulder", 0.7f, Vector3.up, t));
                list.Add(Timed("right_shoulder", 0.7f, Vector3.up, t));
                break;
            case WrestlingMoveKind.Lift:
                list.Add(Timed("left_hip", 0.95f, Vector3.up, t));
                list.Add(Timed("right_hip", 0.95f, Vector3.up, t));
                list.Add(Timed("abdomen", 0.9f, Vector3.up, t));
                list.Add(Timed("left_shoulder", 0.8f, Vector3.up, t));
                list.Add(Timed("right_shoulder", 0.8f, Vector3.up, t));
                break;
            case WrestlingMoveKind.Throw:
                list.Add(Timed("abdomen", 0.95f, Vector3.forward, t));
                list.Add(Timed("left_shoulder", 0.9f, new Vector3(0.2f, 0.5f, 0.8f), t));
                list.Add(Timed("right_shoulder", 0.9f, new Vector3(-0.2f, 0.5f, 0.8f), t));
                list.Add(Timed("left_hip", 0.85f, Vector3.up, t));
                list.Add(Timed("right_hip", 0.85f, Vector3.up, t));
                break;
            case WrestlingMoveKind.DropOn:
                list.Add(Timed("abdomen", 0.95f, Vector3.down, t));
                list.Add(Timed("lumbar", 0.85f, Vector3.down, t));
                list.Add(Timed("left_hip", 0.8f, Vector3.down, t));
                list.Add(Timed("right_hip", 0.8f, Vector3.down, t));
                break;
            case WrestlingMoveKind.Counter:
                list.Add(Timed("abdomen", 0.8f, Vector3.back, t));
                list.Add(Timed("left_hip", 0.75f, Vector3.right, t));
                list.Add(Timed("right_hip", 0.75f, Vector3.left, t));
                break;
        }
        return list;
    }

    static ImpulseAction Timed(string muscle, float activation, Vector3 dir, float duration)
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
