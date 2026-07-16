using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Free-hang fort/stack balance when feet do not reach the ground while sitting.
/// Arms brace/grasp support; legs act as counterweights; abs/back co-contract for CoG.
/// </summary>
[System.Serializable]
public class SitBalanceCard : SitCard
{
    [Header("Free-hang Balance")]
    public bool requireFeetMissGround = true;
    public float braceActivation = 0.7f;

    public SitBalanceCard()
    {
        isSitGoal = true;
        occupancyMode = SurfaceOccupancyMode.Sit;
        sectionName = "SitBalance";
        description = "Balance fort/stack with arms, legs, and abdominals when feet miss ground";
    }

    public static SitBalanceCard Generate(SitSurfaceContact contact, RagdollState state)
    {
        var card = new SitBalanceCard();
        card.BindSurface(contact);
        card.impulseStack = BuildBalanceStack(0.7f);
        card.requiredState = state?.CopyState();
        card.targetState = state?.CopyState();
        card.limits = new SectionLimits { maxForce = 600f, maxTorque = 120f, maxVelocityChange = 2.5f };
        return card;
    }

    public static List<ImpulseAction> BuildBalanceStack(float activation)
    {
        float a = Mathf.Clamp01(activation);
        return new List<ImpulseAction>
        {
            Make("abdomen", a, Vector3.up),
            Make("lumbar", a * 0.9f, Vector3.up),
            Make("left_thigh", a * 0.55f, Vector3.right),
            Make("right_thigh", a * 0.55f, Vector3.left),
            Make("left_hip", a * 0.45f, Vector3.forward),
            Make("right_hip", a * 0.45f, Vector3.forward),
            Make("left_shoulder", a * 0.75f, Vector3.down),
            Make("right_shoulder", a * 0.75f, Vector3.down),
            Make("left_elbow", a * 0.5f, Vector3.down),
            Make("right_elbow", a * 0.5f, Vector3.down)
        };
    }

    static ImpulseAction Make(string group, float activation, Vector3 dir)
    {
        return new ImpulseAction
        {
            muscleGroup = group,
            activation = Mathf.Clamp01(activation),
            duration = 0.15f,
            forceDirection = dir,
            curve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f)
        };
    }
}
