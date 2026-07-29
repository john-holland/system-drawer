using UnityEngine;

/// <summary>
/// Collision-enabled carry: grip frame rails only; fail if trajectory would smudge wet paint.
/// Developer opt-in via collisionEnabledCarryMode.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Carry Keep Out Solver")]
public sealed class PaintCarryKeepOutSolver : MonoBehaviour
{
    public PaintCanvas canvas;
    public Transform leftFrameGrip;
    public Transform rightFrameGrip;
    public Transform leftHandGoal;
    public Transform rightHandGoal;
    public RagdollSystem ragdoll;
    [Tooltip("Developer discretion — enable collision-aware carry training / runtime.")]
    public bool collisionEnabledCarryMode;
    public float frameInset = 0.02f;

    public bool WouldSmudge { get; private set; }
    public Vector3 LeftGrip { get; private set; }
    public Vector3 RightGrip { get; private set; }

    void LateUpdate()
    {
        if (!collisionEnabledCarryMode || canvas == null) return;
        Solve();
    }

    public void Solve()
    {
        Bounds b = canvas.canvasRenderer != null
            ? canvas.canvasRenderer.bounds
            : new Bounds(canvas.transform.position, Vector3.one * 0.5f);

        Vector3 right = canvas.transform.right;
        Vector3 up = canvas.transform.up;
        LeftGrip = b.center - right * (b.extents.x + frameInset) + up * frameInset;
        RightGrip = b.center + right * (b.extents.x + frameInset) + up * frameInset;

        if (leftFrameGrip != null) LeftGrip = leftFrameGrip.position;
        if (rightFrameGrip != null) RightGrip = rightFrameGrip.position;

        if (leftHandGoal != null)
            leftHandGoal.position = LeftGrip;
        if (rightHandGoal != null)
            rightHandGoal.position = RightGrip;

        WouldSmudge = false;
        if (ragdoll != null)
        {
            var lh = ragdoll.GetBoneTransform("LeftHand");
            var rh = ragdoll.GetBoneTransform("RightHand");
            WouldSmudge |= HandTouchesWetPaint(lh);
            WouldSmudge |= HandTouchesWetPaint(rh);
        }
    }

    bool HandTouchesWetPaint(Transform hand)
    {
        if (hand == null || canvas == null) return false;
        if (!canvas.WorldToCanvasUv(hand.position, out Vector2 uv))
            return false;
        // Near frame edges is OK
        if (uv.x < 0.08f || uv.x > 0.92f || uv.y < 0.08f || uv.y > 0.92f)
            return false;
        var visc = canvas.Viscosity;
        if (visc == null) return false;
        visc.SampleUv(uv, out Color c);
        var wet = canvas.layerStack != null ? canvas.layerStack.TopWetLayer() : null;
        float dryLock = canvas.layerStack != null ? canvas.layerStack.smudgeDryLock : 0.85f;
        return c.b > 0.05f && (wet == null || wet.dry01 < dryLock);
    }
}
