using UnityEngine;

/// <summary>Forwards segment collision events to RopeSystem overlap index.</summary>
public class RopeSegmentCollisionRelay : MonoBehaviour
{
    RopeSystem _system;
    RopeSegmentBody _body;

    public void Initialize(RopeSystem system, RopeSegmentBody body)
    {
        _system = system;
        _body = body;
    }

    void OnCollisionEnter(Collision collision) => Forward(collision);
    void OnCollisionStay(Collision collision) => Forward(collision);

    void Forward(Collision collision)
    {
        if (_system == null || _body == null || collision.contactCount == 0)
            return;
        var otherBody = collision.collider.GetComponentInParent<RopeSegmentBody>();
        if (otherBody != null && otherBody != _body)
            _system.RegisterSegmentPairContact(_body, otherBody, collision.GetContact(0));
        else
            _system.RegisterExternalContact(_body, collision.GetContact(0));
    }
}
