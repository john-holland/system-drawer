using UnityEngine;

/// <summary>Wind/unwind rates and spool attachment for arc-length state.</summary>
public class RopeWindingController
{
    readonly RopeConfig _config;
    readonly RopeArcLengthState _arc;
    readonly RopeSegmentRingBuffer _ring;

    float _windRateMps;

    public RopeWindingController(RopeConfig config, RopeArcLengthState arc, RopeSegmentRingBuffer ring)
    {
        _config = config;
        _arc = arc;
        _ring = ring;
    }

    public float WindRateMps => _windRateMps;

    public void SetRate(float signedRateMps)
    {
        _windRateMps = Mathf.Clamp(signedRateMps, -_config.maxUnwindRateMps, _config.maxWindRateMps);
    }

    public void Tick(float dt, Transform spool, Transform headAnchor, Vector3 unwindDirection)
    {
        if (Mathf.Abs(_windRateMps) < 1e-5f)
            return;

        float delta = _windRateMps * dt;
        if (_windRateMps > 0f)
        {
            float before = _arc.WoundLengthM;
            _arc.Wind(delta, _config.ringBufferSize);
            if (_arc.WoundLengthM > before + 1e-4f)
                _ring.CaptureTailToWound(_arc);
        }
        else
        {
            float before = _arc.WoundLengthM;
            _arc.Unwind(-delta, _config.ringBufferSize);
            if (_arc.WoundLengthM < before - 1e-4f)
                _ring.ActivateHeadFromSpool(spool, _arc);
        }

        _ring.RebuildActiveMapping(headAnchor, spool, unwindDirection);
    }
}
