using UnityEngine;

/// <summary>Per-link rigidbody host with adjacency metadata for collision policy.</summary>
[RequireComponent(typeof(Rigidbody))]
public class RopeSegmentBody : MonoBehaviour
{
    public int logicalSegmentIndex = -1;
    public int ringSlotIndex = -1;
    public RopeSegmentBody neighborTowardHead;
    public RopeSegmentBody neighborTowardTail;
    public ConfigurableJoint jointToHead;
    public ConfigurableJoint jointToTail;

    Rigidbody _rb;
    Collider _collider;

    public Rigidbody Rigidbody => _rb != null ? _rb : (_rb = GetComponent<Rigidbody>());
    public Collider Collider => _collider != null ? _collider : (_collider = GetComponent<Collider>());

    public void Configure(RopeConfig config, int logicalIndex, int slotIndex)
    {
        logicalSegmentIndex = logicalIndex;
        ringSlotIndex = slotIndex;
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = config.segmentMassKg;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (_collider == null)
        {
            var cap = gameObject.AddComponent<CapsuleCollider>();
            cap.direction = 2;
            cap.radius = config.ropeRadiusM;
            cap.height = config.segmentLengthM * 0.95f;
            _collider = cap;
        }
    }

    public void SetSimulated(bool active)
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            return;
        _rb.isKinematic = !active;
        _rb.detectCollisions = active;
        gameObject.SetActive(active);
    }

    public Vector3 LinkAxisWorld()
    {
        return transform.forward;
    }
}
