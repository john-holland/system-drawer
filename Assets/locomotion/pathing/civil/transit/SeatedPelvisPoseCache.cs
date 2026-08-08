using UnityEngine;

/// <summary>Caches last-good pelvis→seat local pose for seated plant IK on transit vehicles.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Transit/Seated Pelvis Pose Cache")]
public sealed class SeatedPelvisPoseCache : MonoBehaviour
{
    public Transform seatAnchor;
    public Vector3 pelvisLocalPosition;
    public Quaternion pelvisLocalRotation = Quaternion.identity;
    public bool hasPose;
    public float cachedAtTime;

    public void Capture(Transform pelvis, Transform seat)
    {
        if (pelvis == null || seat == null) return;
        seatAnchor = seat;
        pelvisLocalPosition = seat.InverseTransformPoint(pelvis.position);
        pelvisLocalRotation = Quaternion.Inverse(seat.rotation) * pelvis.rotation;
        hasPose = true;
        cachedAtTime = Time.time;
    }

    public bool TryGetWorldPose(out Vector3 worldPos, out Quaternion worldRot)
    {
        worldPos = Vector3.zero;
        worldRot = Quaternion.identity;
        if (!hasPose || seatAnchor == null) return false;
        worldPos = seatAnchor.TransformPoint(pelvisLocalPosition);
        worldRot = seatAnchor.rotation * pelvisLocalRotation;
        return true;
    }

    public void Clear()
    {
        hasPose = false;
        seatAnchor = null;
    }
}
