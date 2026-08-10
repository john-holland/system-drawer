using UnityEngine;

/// <summary>Cylinder physics grab bar for standing / seat standup support (bus + train).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicles/Vehicle Grab Hold")]
public sealed class VehicleGrabHold : MonoBehaviour
{
    public float radius = 0.03f;
    public float height = 1.1f;
    public bool kinematic = true;
    public string standupAnimationId = "seat_bar_standup";
    public CapsuleCollider capsule;

    void Awake() => EnsureCollider();

    public void EnsureCollider()
    {
        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
        capsule.radius = radius;
        capsule.height = height;
        capsule.direction = 1;
        var rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = kinematic;
        rb.useGravity = false;
    }
}
