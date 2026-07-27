using System.Collections.Generic;
using UnityEngine;

/// <summary>Combat physics card: targets, defend wards, impact, prebake, proxy instruments.</summary>
[System.Serializable]
public class CombatCard : GoodSection
{
    [Header("Combat")]
    public CombatMode combatMode = CombatMode.Melee;
    public CombatMoveKind combatMoveKind = CombatMoveKind.Strike;
    public GameObject primaryTarget;
    public List<GameObject> secondaryTargets = new List<GameObject>();
    public List<DefendWard> defendWards = new List<DefendWard>();
    public CombatImpactSpec impact = new CombatImpactSpec();
    public CombatPrebakeRequirements prebake = new CombatPrebakeRequirements();
    public List<CombatLimbHealthRequirement> limbHealthRequirements = new List<CombatLimbHealthRequirement>();
    public CardInstrumentProxyOptions instrumentProxy = new CardInstrumentProxyOptions();

    [Tooltip("Optional attack behavior tree asset name / key.")]
    public string attackBehaviorKey = "combat.attack";
    [Tooltip("Optional defense ward behavior tree key.")]
    public string defenseBehaviorKey = "combat.defend";

    [Header("Slow-time / input")]
    public KeyCode hotkey = KeyCode.None;
    public string inputActionName;

    public CombatCard()
    {
        isCombatGoal = true;
        isHitGoal = false;
        isShootGoal = false;
        physicalPathingTag = "combat";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "combat";
    }

    public string CombatAnimationGroupTag => CombatAnimationGroup.ForMove(combatMoveKind);

    public bool MeetsCombatRequirements(GameObject actor, GameObject target, RagdollSystem actorRagdoll = null)
    {
        GameObject t = target != null ? target : primaryTarget;
        if (t == null && combatMoveKind != CombatMoveKind.Reload && combatMoveKind != CombatMoveKind.Aim)
            return false;
        if (actor != null)
        {
            var limbs = actor.GetComponent<LimbIntegrityState>();
            if (limbs != null && limbHealthRequirements != null)
            {
                for (int i = 0; i < limbHealthRequirements.Count; i++)
                    if (!limbs.MeetsRequirement(limbHealthRequirements[i]))
                        return false;
            }
        }
        return true;
    }

    public CombatDamageEvent BuildDamageEvent(GameObject attacker, GameObject target, Vector3 hit, Vector3 dir)
    {
        var imp = impact ?? new CombatImpactSpec();
        return new CombatDamageEvent
        {
            attacker = attacker,
            target = target,
            type = imp.damageType,
            worldHit = hit,
            direction = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward,
            depth01 = Mathf.Clamp01(imp.damage01),
            amount01 = imp.damage01,
            through = imp.throughOrStop,
            cutterProfileId = imp.cutterProfileId,
            cutProfileId = imp.cutProfileId,
            materialKind = imp.materialKind,
            healthMode = imp.healthMode,
            limbId = imp.primaryLimbBone,
            createWound = true,
            autoSuture = false
        };
    }

    public static CombatCard Generate(
        CombatMode mode,
        CombatMoveKind kind,
        GameObject target,
        RagdollState state = null)
    {
        var card = new CombatCard
        {
            combatMode = mode,
            combatMoveKind = kind,
            primaryTarget = target,
            sectionName = $"combat_{mode}_{kind}",
            description = $"{mode} {kind}",
            isCombatGoal = true,
            physicalPathingTag = $"combat_{kind.ToString().ToLowerInvariant()}",
            impact = DefaultImpact(kind),
            impulseStack = BuildImpulseStack(kind),
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 220f, maxTorque = 80f, maxVelocityChange = 3f },
            isHitGoal = kind == CombatMoveKind.Strike || kind == CombatMoveKind.Slash || kind == CombatMoveKind.Stab,
            isShootGoal = kind == CombatMoveKind.Fire || kind == CombatMoveKind.Suppress
        };
        if (kind == CombatMoveKind.Block || kind == CombatMoveKind.Parry)
        {
            card.defendWards.Add(new DefendWard
            {
                boneOrRegion = "Forearm_L",
                absorb01 = kind == CombatMoveKind.Parry ? 0.85f : 0.65f
            });
        }
        return card;
    }

    public static CombatImpactSpec DefaultImpact(CombatMoveKind kind)
    {
        var imp = new CombatImpactSpec();
        switch (kind)
        {
            case CombatMoveKind.Fire:
            case CombatMoveKind.Suppress:
                imp.damageType = CombatDamageType.Bullet;
                imp.damage01 = 0.45f;
                imp.throughOrStop = kind == CombatMoveKind.Suppress;
                break;
            case CombatMoveKind.Slash:
                imp.damageType = CombatDamageType.Slash;
                imp.damage01 = 0.4f;
                break;
            case CombatMoveKind.Stab:
                imp.damageType = CombatDamageType.Pierce;
                imp.damage01 = 0.5f;
                break;
            case CombatMoveKind.Throw:
                imp.damageType = CombatDamageType.Blunt;
                imp.damage01 = 0.35f;
                break;
            default:
                imp.damageType = CombatDamageType.Blunt;
                imp.damage01 = 0.25f;
                break;
        }
        return imp;
    }

    static List<ImpulseAction> BuildImpulseStack(CombatMoveKind kind)
    {
        string muscle = kind == CombatMoveKind.Fire ? "weapon.trigger" : "RightArm";
        return new List<ImpulseAction>
        {
            new ImpulseAction { muscleGroup = muscle, activation = kind == CombatMoveKind.Fire ? 1f : 0.7f }
        };
    }
}
