using System;
using UnityEngine;

public struct RopeSegmentTensionSample
{
    public int logicalIndex;
    public float tensionN;
    public float strain;
    public float twistRad;
}

public class RopeSnapEvent
{
    public int segmentIndex;
    public float arcM;
    public Vector3 worldPoint;
}

/// <summary>Per-segment tensile strength, total load, and snap detection.</summary>
public class RopeTensileModel
{
    readonly RopeConfig _config;
    readonly RopeArcLengthState _arc;
    readonly RopeSegmentRingBuffer _ring;

    float _maxTensionN;
    RopeSnapEvent _pendingSnap;

    public event Action<RopeSnapEvent> Snapped;

    public RopeTensileModel(RopeConfig config, RopeArcLengthState arc, RopeSegmentRingBuffer ring)
    {
        _config = config;
        _arc = arc;
        _ring = ring;
    }

    public float MaxTensionN => _maxTensionN;
    public float TotalBreakTensionN => ComputeTotalBreak();
    public float NormalizedLoad => TotalBreakTensionN > 1e-3f ? _maxTensionN / TotalBreakTensionN : 0f;
    public bool HasPendingSnap => _pendingSnap != null;

    float ComputeTotalBreak()
    {
        if (_config.totalStrengthPolicy == RopeTotalStrengthPolicy.SumSegments)
            return _config.breakTensionN * _arc.SegmentCount;
        return _config.breakTensionN;
    }

    public void SampleAfterPhysics()
    {
        _maxTensionN = 0f;
        _pendingSnap = null;
        Quaternion prevRot = Quaternion.identity;
        bool hasPrev = false;

        for (int logical = _arc.ActiveHeadSegment; logical <= _arc.ActiveTailSegment; logical++)
        {
            RopeSegmentBody body = _ring.GetBody(logical);
            if (body == null || !body.gameObject.activeSelf)
                continue;

            float tension = EstimateTension(body);
            _maxTensionN = Mathf.Max(_maxTensionN, tension);

            if (tension >= _config.breakTensionN)
            {
                _pendingSnap = new RopeSnapEvent
                {
                    segmentIndex = logical,
                    arcM = _arc.SegmentArcStart(logical),
                    worldPoint = body.transform.position
                };
                Snapped?.Invoke(_pendingSnap);
                break;
            }

            if (hasPrev)
            {
                Quaternion dq = body.transform.rotation * Quaternion.Inverse(prevRot);
                dq.ToAngleAxis(out float angle, out _);
                // twist accumulation handled in cache write via body index
            }
            prevRot = body.transform.rotation;
            hasPrev = true;
        }
    }

    static float EstimateTension(RopeSegmentBody body)
    {
        float mag = 0f;
        if (body.jointToHead != null)
            mag = Mathf.Max(mag, body.jointToHead.currentForce.magnitude);
        if (body.jointToTail != null)
            mag = Mathf.Max(mag, body.jointToTail.currentForce.magnitude);
        if (mag < 1e-3f)
            mag = body.Rigidbody.linearVelocity.magnitude * body.Rigidbody.mass * 10f;
        return mag;
    }

    public RopeSegmentTensionSample GetSample(int logicalIndex)
    {
        RopeSegmentBody body = _ring.GetBody(logicalIndex);
        float tension = body != null ? EstimateTension(body) : 0f;
        float strain = _config.yieldTensionN > 1e-3f ? tension / _config.yieldTensionN : 0f;
        return new RopeSegmentTensionSample
        {
            logicalIndex = logicalIndex,
            tensionN = tension,
            strain = strain,
            twistRad = body != null ? body.transform.rotation.eulerAngles.z * Mathf.Deg2Rad : 0f
        };
    }

    public void ClearPendingSnap() => _pendingSnap = null;
}
