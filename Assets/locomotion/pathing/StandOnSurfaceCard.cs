using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stand on a sit surface (chair, stack, books): feet plant on seat plane with CoG tow.
/// </summary>
[System.Serializable]
public class StandOnSurfaceCard : SitCard
{
    [Header("Stand-On")]
    public float plantStiffness = 0.95f;

    public StandOnSurfaceCard()
    {
        isStandOnSurfaceGoal = true;
        isSitGoal = false;
        occupancyMode = SurfaceOccupancyMode.StandOn;
        sectionName = "StandOnSurface";
        description = "Stand on surface with both feet planted and CoG over seat";
        isIsometric = true;
    }

    public static StandOnSurfaceCard Generate(SitSurfaceContact contact, RagdollState state)
    {
        var card = new StandOnSurfaceCard();
        card.BindSurface(contact);
        card.occupancyMode = SurfaceOccupancyMode.StandOn;
        card.isStandOnSurfaceGoal = true;
        card.isSitGoal = false;
        card.impulseStack = BuildPlantStack();
        card.requiredState = state?.CopyState();
        card.targetState = state?.CopyState();
        if (card.targetState != null && contact != null)
            card.targetState.rootPosition = contact.WorldPlanePoint + contact.WorldPlaneNormal * 0.95f;
        card.limits = new SectionLimits { maxForce = 700f, maxTorque = 140f, maxVelocityChange = 2.2f };
        return card;
    }

    public static List<ImpulseAction> BuildPlantStack()
    {
        return new List<ImpulseAction>
        {
            Make("left_ankle", 0.85f, Vector3.down),
            Make("right_ankle", 0.85f, Vector3.down),
            Make("left_knee", 0.55f, Vector3.zero),
            Make("right_knee", 0.55f, Vector3.zero),
            Make("left_hip", 0.5f, Vector3.zero),
            Make("right_hip", 0.5f, Vector3.zero),
            Make("abdomen", 0.4f, Vector3.up),
            Make("lumbar", 0.4f, Vector3.up)
        };
    }

    static ImpulseAction Make(string group, float activation, Vector3 dir)
    {
        return new ImpulseAction
        {
            muscleGroup = group,
            activation = activation,
            duration = 0.2f,
            forceDirection = dir,
            curve = AnimationCurve.Linear(0f, 1f, 1f, 1f)
        };
    }
}
