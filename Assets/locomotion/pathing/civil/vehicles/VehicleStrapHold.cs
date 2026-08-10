using UnityEngine;

/// <summary>Hanging strap hold — rope physics for standing passengers (bus + train).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicles/Vehicle Strap Hold")]
public sealed class VehicleStrapHold : MonoBehaviour
{
    public Transform ceilingAnchor;
    public Transform handleAnchor;
    public RopeConfig ropeConfig = new RopeConfig();
    public RopeSystem rope;
    public string standupAnimationId = "strap_hold_standup";

    void Awake() => EnsureRope();

    public void EnsureRope()
    {
        if (ceilingAnchor == null) ceilingAnchor = transform;
        if (handleAnchor == null)
        {
            var h = transform.Find("handle");
            handleAnchor = h != null ? h : transform;
        }
        if (rope == null)
            rope = GetComponent<RopeSystem>() ?? gameObject.AddComponent<RopeSystem>();
    }
}
