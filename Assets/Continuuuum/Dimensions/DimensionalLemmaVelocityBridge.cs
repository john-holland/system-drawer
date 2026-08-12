using UnityEngine;

/// <summary>Optional velocity bridge into DimensionalPositionalSlot for KeepAlive motion continuity.</summary>
[AddComponentMenu("Continuuuum/Dimensions/Dimensional Lemma Velocity Bridge")]
public sealed class DimensionalLemmaVelocityBridge : MonoBehaviour
{
    public bool captureAngular = true;
    Rigidbody _rb;
    Rigidbody2D _rb2;

    void Awake()
    {
        EnsureBodies();
    }

    void EnsureBodies()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_rb2 == null)
            _rb2 = GetComponent<Rigidbody2D>();
    }

    public void WriteTo(DimensionalPositionalSlot slot)
    {
        if (slot == null)
            return;
        EnsureBodies();
        if (_rb != null)
        {
            slot.hasVelocity = true;
            slot.linearVelocity = _rb.linearVelocity;
            slot.angularVelocity = captureAngular ? _rb.angularVelocity : Vector3.zero;
            return;
        }
        if (_rb2 != null)
        {
            slot.hasVelocity = true;
            var v = _rb2.linearVelocity;
            slot.linearVelocity = new Vector3(v.x, v.y, 0f);
            slot.angularVelocity = captureAngular ? new Vector3(0f, 0f, _rb2.angularVelocity) : Vector3.zero;
        }
    }

    public void ApplyFrom(DimensionalPositionalSlot slot)
    {
        if (slot == null || !slot.hasVelocity)
            return;
        EnsureBodies();
        if (_rb != null)
        {
            _rb.linearVelocity = slot.linearVelocity;
            if (captureAngular)
                _rb.angularVelocity = slot.angularVelocity;
            return;
        }
        if (_rb2 != null)
        {
            _rb2.linearVelocity = new Vector2(slot.linearVelocity.x, slot.linearVelocity.y);
            if (captureAngular)
                _rb2.angularVelocity = slot.angularVelocity.z;
        }
    }
}
