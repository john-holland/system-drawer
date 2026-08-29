using UnityEngine;

/// <summary>
/// Yaw/pitch/roll (or look-at) base so gallery audience sits on an angle, not only ortho PixelLight views.
/// </summary>
[AddComponentMenu("Locomotion/Civil/Angle Base 3D")]
public sealed class AngleBase3D : MonoBehaviour
{
    public float yawDeg;
    public float pitchDeg;
    public float rollDeg;
    public Transform lookAt;
    public bool useLookAt;
    public string galleryCellId;

    public Quaternion Orientation()
    {
        if (useLookAt && lookAt != null)
            return Quaternion.LookRotation((lookAt.position - transform.position).normalized, Vector3.up);
        return Quaternion.Euler(pitchDeg, yawDeg, rollDeg);
    }

    public void ApplyTo(Transform target)
    {
        if (target == null) return;
        target.position = transform.position;
        target.rotation = Orientation();
    }
}
