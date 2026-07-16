using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotating-chair swivel as tool-use: thigh push, abs/back balance, foot forward,
/// back thigh advance, outer/inner thighs dodge caster legs. Tow chain keeps CoG over seat.
/// </summary>
[System.Serializable]
public class ChairRotateCard : SitCard
{
    [Header("Chair Rotate")]
    public float yawDegrees = 45f;
    public float sequenceStepSeconds = 0.12f;
    public Bounds casterLocalBounds = new Bounds(Vector3.zero, new Vector3(0.45f, 0.15f, 0.45f));

    public ChairRotateCard()
    {
        isChairRotateGoal = true;
        isSitGoal = true;
        pleaseHold = true;
        sectionName = "ChairRotate";
        description = "Rotate swivel chair under occupant with thigh/abs/foot caster-dodge sequence";
    }

    public static ChairRotateCard Generate(SitSurfaceContact contact, float yawDegrees, RagdollState state, SurfaceOccupancyMode mode = SurfaceOccupancyMode.Sit)
    {
        var card = new ChairRotateCard
        {
            yawDegrees = yawDegrees,
            occupancyMode = mode,
            isSitGoal = mode == SurfaceOccupancyMode.Sit,
            isStandOnSurfaceGoal = mode == SurfaceOccupancyMode.StandOn
        };
        card.BindSurface(contact);
        card.occupancyMode = mode;
        card.impulseStack = BuildRotateSequence(card.sequenceStepSeconds, mode);
        card.requiredState = state?.CopyState();
        card.targetState = state?.CopyState();
        card.limits = new SectionLimits { maxForce = 650f, maxTorque = 160f, maxVelocityChange = 3f };
        return card;
    }

    /// <summary>
    /// 1) top thigh impulse 2) abs+back 3) foot forward 4) back thigh 5) outer/inner thighs dodge casters.
    /// </summary>
    public static List<ImpulseAction> BuildRotateSequence(float step, SurfaceOccupancyMode mode)
    {
        float t = Mathf.Max(0.05f, step);
        var list = new List<ImpulseAction>
        {
            // 1. Impulse to top thigh (push swivel)
            Timed("left_thigh", 0.9f, Vector3.forward, t),
            // 2. Contract abs + back to balance
            Timed("abdomen", 0.85f, Vector3.up, t),
            Timed("lumbar", 0.8f, Vector3.up, t),
            Timed("torso", 0.55f, Vector3.up, t),
            // 3. Bring foot forward
            Timed("left_ankle", 0.7f, Vector3.forward, t),
            Timed("left_foot", 0.65f, Vector3.forward, t),
            // 4. Contract back thigh to bring foot forward
            Timed("right_thigh", 0.8f, Vector3.forward, t),
            Timed("right_hip", 0.6f, Vector3.forward, t),
            // 5. Outer/inner thighs dodge caster legs
            Timed("left_thigh", 0.75f, Vector3.right, t),
            Timed("right_thigh", 0.75f, Vector3.left, t)
        };

        if (mode == SurfaceOccupancyMode.StandOn)
        {
            list.Insert(0, Timed("left_ankle", 0.5f, Vector3.down, t * 0.5f));
            list.Insert(1, Timed("right_ankle", 0.5f, Vector3.down, t * 0.5f));
        }
        return list;
    }

    /// <summary>Apply yaw impulse to chair host rigidbody if present.</summary>
    public void ApplyChairYawImpulse()
    {
        if (surfaceContact == null)
            return;
        Rigidbody rb = surfaceContact.hostBody;
        if (rb == null && surfaceContact.host != null)
            rb = surfaceContact.host.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.isKinematic)
            return;
        Vector3 torque = surfaceContact.WorldPlaneNormal * (yawDegrees * Mathf.Deg2Rad * rb.mass * 2f);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    /// <summary>True when a world foot position clears the caster AABB (host local).</summary>
    public bool FootClearsCasters(Vector3 worldFoot)
    {
        if (surfaceContact == null || surfaceContact.host == null)
            return true;
        Vector3 local = surfaceContact.host.InverseTransformPoint(worldFoot);
        return !casterLocalBounds.Contains(local);
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
