using System;
using UnityEngine;

[Serializable]
public struct RopeWoundPose
{
    public Vector3 position;
    public Quaternion rotation;
    public bool valid;
}

/// <summary>
/// Arc-length bookkeeping: wound span, active window, and per-bin wound pose cache.
/// </summary>
public class RopeArcLengthState
{
    readonly RopeWoundPose[] _woundBins;
    readonly float _binSize;
    readonly float _totalLength;
    readonly int _segmentCount;

    float _woundLengthM;
    int _activeHeadSegment;
    int _activeTailSegment;

    public RopeArcLengthState(RopeConfig config)
    {
        _totalLength = Mathf.Max(0.1f, config.totalLengthM);
        _binSize = Mathf.Max(0.01f, config.arcBinSizeM);
        _segmentCount = config.SegmentCount;
        _woundBins = new RopeWoundPose[config.ArcBinCount];
        _woundLengthM = 0f;
        _activeHeadSegment = 0;
        _activeTailSegment = Mathf.Min(config.ringBufferSize - 1, _segmentCount - 1);
    }

    public float TotalLength => _totalLength;
    public float WoundLengthM => _woundLengthM;
    public float ActiveLengthM => Mathf.Max(0f, _totalLength - _woundLengthM);
    public int SegmentCount => _segmentCount;
    public int ActiveHeadSegment => _activeHeadSegment;
    public int ActiveTailSegment => _activeTailSegment;
    public int ActiveSegmentCount => Mathf.Max(0, _activeTailSegment - _activeHeadSegment + 1);

    public float SegmentArcStart(int segmentIndex)
    {
        return segmentIndex * SegmentLength();
    }

    public float SegmentLength()
    {
        return _totalLength / Mathf.Max(1, _segmentCount);
    }

    public int ArcToBin(float arcM)
    {
        return Mathf.Clamp(Mathf.FloorToInt(arcM / _binSize), 0, _woundBins.Length - 1);
    }

    public float ArcToNormalized(float arcM)
    {
        return Mathf.Clamp01(arcM / _totalLength);
    }

    public void StoreWoundPose(float arcM, Vector3 position, Quaternion rotation)
    {
        int bin = ArcToBin(arcM);
        _woundBins[bin] = new RopeWoundPose { position = position, rotation = rotation, valid = true };
    }

    public bool TryGetWoundPose(float arcM, out RopeWoundPose pose)
    {
        int bin = ArcToBin(arcM);
        pose = _woundBins[bin];
        return pose.valid;
    }

    public void Wind(float deltaM, int maxActiveSegments)
    {
        if (deltaM <= 0f)
            return;
        float segLen = SegmentLength();
        _woundLengthM = Mathf.Min(_totalLength, _woundLengthM + deltaM);
        int woundSegments = Mathf.FloorToInt(_woundLengthM / segLen);
        _activeHeadSegment = Mathf.Clamp(woundSegments, 0, _segmentCount - 1);
        int maxTail = Mathf.Min(_segmentCount - 1, _activeHeadSegment + maxActiveSegments - 1);
        _activeTailSegment = Mathf.Max(_activeHeadSegment, maxTail);
    }

    public void Unwind(float deltaM, int maxActiveSegments)
    {
        if (deltaM <= 0f)
            return;
        _woundLengthM = Mathf.Max(0f, _woundLengthM - deltaM);
        float segLen = SegmentLength();
        int woundSegments = Mathf.FloorToInt(_woundLengthM / segLen);
        _activeHeadSegment = Mathf.Clamp(woundSegments, 0, _segmentCount - 1);
        int maxTail = Mathf.Min(_segmentCount - 1, _activeHeadSegment + maxActiveSegments - 1);
        _activeTailSegment = Mathf.Max(_activeHeadSegment, maxTail);
    }

    public bool IsSegmentActive(int logicalSegmentIndex)
    {
        return logicalSegmentIndex >= _activeHeadSegment && logicalSegmentIndex <= _activeTailSegment;
    }

    public void ResetWound(float woundM = 0f)
    {
        _woundLengthM = Mathf.Clamp(woundM, 0f, _totalLength);
        for (int i = 0; i < _woundBins.Length; i++)
            _woundBins[i] = default;
    }
}
