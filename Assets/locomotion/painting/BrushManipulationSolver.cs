using UnityEngine;

/// <summary>
/// IK-style solver: maps brush tip/shaft targets to hand goals while keeping paint off the player body.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Brush Manipulation Solver")]
public sealed class BrushManipulationSolver : MonoBehaviour
{
    public RagdollSystem ragdoll;
    public HairBodyCapsuleBinder bodyBinder;
    public Transform brushTip;
    public Transform brushFerrule;
    public Transform brushShaft;
    public Transform canvasPlane;
    public Transform rightHandGoal;
    public Transform leftHandGoal;
    public bool dominantRight = true;
    [Range(0.01f, 0.5f)] public float keepOutPaddingM = 0.08f;
    [Range(0f, 90f)] public float maxFerruleRotateDeg = 55f;
    [Range(0f, 0.3f)] public float tipRetractM = 0.06f;

    HairCapsuleBuffer _capsules;

    public Quaternion RightHandRotation { get; private set; } = Quaternion.identity;
    public Quaternion LeftHandRotation { get; private set; } = Quaternion.identity;
    public Vector3 RightHandPosition { get; private set; }
    public Vector3 LeftHandPosition { get; private set; }
    public bool PaintWouldTouchPlayer { get; private set; }

    void Awake()
    {
        if (ragdoll == null)
            ragdoll = GetComponentInParent<RagdollSystem>();
        if (bodyBinder == null)
            bodyBinder = GetComponent<HairBodyCapsuleBinder>();
        _capsules = new HairCapsuleBuffer();
    }

    void LateUpdate()
    {
        Solve(Time.deltaTime);
    }

    public void Solve(float dt)
    {
        if (brushFerrule == null && brushTip == null) return;
        Transform ferrule = brushFerrule != null ? brushFerrule : brushTip;
        Transform tip = brushTip != null ? brushTip : ferrule;

        _capsules.Clear();
        bodyBinder?.Bind(_capsules);

        Vector3 tipPos = tip.position;
        Vector3 shaftDir = brushShaft != null
            ? brushShaft.forward
            : (tip.position - ferrule.position).normalized;
        if (shaftDir.sqrMagnitude < 1e-6f)
            shaftDir = Vector3.forward;

        // Keep bristles on canvas side of wrist plane when possible
        if (canvasPlane != null)
        {
            Plane canvas = new Plane(canvasPlane.forward, canvasPlane.position);
            float side = canvas.GetSide(tipPos) ? 1f : -1f;
            if (side < 0f)
                tipPos = canvas.ClosestPointOnPlane(tipPos) - canvasPlane.forward * 0.002f;
        }

        PaintWouldTouchPlayer = false;
        if (IntersectsBody(tipPos, keepOutPaddingM))
        {
            PaintWouldTouchPlayer = true;
            // Prefer rotating around ferrule
            Vector3 fromFerrule = tipPos - ferrule.position;
            Vector3 away = AwayFromNearestCapsule(tipPos);
            Quaternion swing = Quaternion.FromToRotation(fromFerrule.normalized, Vector3.Slerp(fromFerrule.normalized, away, 0.65f).normalized);
            swing = Quaternion.RotateTowards(Quaternion.identity, swing, maxFerruleRotateDeg);
            tipPos = ferrule.position + swing * fromFerrule;
            tipPos -= away.normalized * tipRetractM;
            if (brushShaft != null)
                brushShaft.rotation = swing * brushShaft.rotation;
        }

        if (brushTip != null)
            brushTip.position = tipPos;

        Transform handBone = ragdoll != null
            ? ragdoll.GetBoneTransform(dominantRight ? "RightHand" : "LeftHand")
            : null;

        Vector3 handPos = ferrule.position;
        Quaternion handRot = Quaternion.LookRotation(shaftDir, canvasPlane != null ? canvasPlane.up : Vector3.up);
        if (dominantRight)
        {
            RightHandPosition = handPos;
            RightHandRotation = handRot;
            if (rightHandGoal != null)
                rightHandGoal.SetPositionAndRotation(handPos, handRot);
        }
        else
        {
            LeftHandPosition = handPos;
            LeftHandRotation = handRot;
            if (leftHandGoal != null)
                leftHandGoal.SetPositionAndRotation(handPos, handRot);
        }

        // Soft pull hand bone goal if present (does not override full ABT)
        if (handBone != null && Application.isPlaying)
        {
            // Goals only — physics cards / ABT consume RightHandPosition
        }
    }

    bool IntersectsBody(Vector3 point, float pad)
    {
        if (_capsules == null) return false;
        var slots = _capsules.Slots;
        int n = Mathf.Min(_capsules.Count, slots.Length);
        for (int i = 0; i < n; i++)
        {
            Vector4 c = slots[i];
            if (c.w <= 1e-5f) continue;
            float r = c.w + pad;
            if ((point - new Vector3(c.x, c.y, c.z)).sqrMagnitude <= r * r)
                return true;
        }
        return false;
    }

    Vector3 AwayFromNearestCapsule(Vector3 point)
    {
        float best = float.MaxValue;
        Vector3 away = Vector3.up;
        var slots = _capsules.Slots;
        int n = Mathf.Min(_capsules.Count, slots.Length);
        for (int i = 0; i < n; i++)
        {
            Vector4 c = slots[i];
            if (c.w <= 1e-5f) continue;
            Vector3 d = point - new Vector3(c.x, c.y, c.z);
            float m = d.magnitude;
            if (m < best && m > 1e-5f)
            {
                best = m;
                away = d / m;
            }
        }
        return away;
    }
}
