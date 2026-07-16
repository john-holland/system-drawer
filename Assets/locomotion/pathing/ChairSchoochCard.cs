using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Non-rotating chair schooch: same limb sequence as rotate, plus lift body while hands
/// hold the chair as tool-use; translate chair and re-seat / re-plant.
/// </summary>
[System.Serializable]
public class ChairSchoochCard : SitCard
{
    [Header("Chair Schooch")]
    public Vector3 scootWorldDelta = new Vector3(0f, 0f, 0.25f);
    public float sequenceStepSeconds = 0.12f;
    public float liftActivation = 0.85f;

    public ChairSchoochCard()
    {
        isChairSchoochGoal = true;
        isSitGoal = true;
        pleaseHold = true;
        sectionName = "ChairSchooch";
        description = "Schooch chair: lift body, hold chair as tool, translate, re-seat";
    }

    public static ChairSchoochCard Generate(SitSurfaceContact contact, Vector3 scootDelta, RagdollState state, SurfaceOccupancyMode mode = SurfaceOccupancyMode.Sit)
    {
        var card = new ChairSchoochCard
        {
            scootWorldDelta = scootDelta,
            occupancyMode = mode,
            isSitGoal = mode == SurfaceOccupancyMode.Sit,
            isStandOnSurfaceGoal = mode == SurfaceOccupancyMode.StandOn
        };
        card.BindSurface(contact);
        card.occupancyMode = mode;
        card.impulseStack = BuildSchoochSequence(card.sequenceStepSeconds, card.liftActivation, mode);
        card.requiredState = state?.CopyState();
        card.targetState = state?.CopyState();
        card.limits = new SectionLimits { maxForce = 700f, maxTorque = 160f, maxVelocityChange = 3.2f };
        return card;
    }

    public static List<ImpulseAction> BuildSchoochSequence(float step, float lift, SurfaceOccupancyMode mode)
    {
        float t = Mathf.Max(0.05f, step);
        var list = new List<ImpulseAction>();

        // Hold chair as tool-use (hands)
        list.Add(Timed("left_shoulder", 0.7f, Vector3.down, t));
        list.Add(Timed("right_shoulder", 0.7f, Vector3.down, t));
        list.Add(Timed("left_elbow", 0.6f, Vector3.down, t));
        list.Add(Timed("right_elbow", 0.6f, Vector3.down, t));

        // Lift body (hips/abs for sit; ankles/hips for stand-on)
        if (mode == SurfaceOccupancyMode.StandOn)
        {
            list.Add(Timed("left_ankle", lift, Vector3.up, t));
            list.Add(Timed("right_ankle", lift, Vector3.up, t));
            list.Add(Timed("left_hip", lift * 0.8f, Vector3.up, t));
            list.Add(Timed("right_hip", lift * 0.8f, Vector3.up, t));
        }
        else
        {
            list.Add(Timed("abdomen", lift, Vector3.up, t));
            list.Add(Timed("left_hip", lift * 0.9f, Vector3.up, t));
            list.Add(Timed("right_hip", lift * 0.9f, Vector3.up, t));
            list.Add(Timed("lumbar", lift * 0.7f, Vector3.up, t));
        }

        // Same rotate limb sequence for foot/thigh clearance while scooting
        list.AddRange(ChairRotateCard.BuildRotateSequence(t, mode));
        return list;
    }

    /// <summary>Translate chair host and return true if moved.</summary>
    public bool ApplyChairTranslate()
    {
        if (surfaceContact == null || surfaceContact.host == null)
            return false;
        Rigidbody rb = surfaceContact.hostBody ?? surfaceContact.host.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(rb.position + scootWorldDelta);
            return true;
        }
        surfaceContact.host.position += scootWorldDelta;
        return true;
    }

    static ImpulseAction Timed(string group, float activation, Vector3 dir, float duration)
    {
        return new ImpulseAction
        {
            muscleGroup = group,
            activation = activation,
            duration = duration,
            forceDirection = dir,
            curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };
    }
}
