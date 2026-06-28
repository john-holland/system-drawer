using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum RopeOverlapFlags : uint
{
    None = 0,
    SelfCross = 1 << 0,
    Tangle = 1 << 1,
    External = 1 << 2
}

public struct RopeOverlapEntry
{
    public int segmentA;
    public int segmentB;
    public float arcA;
    public float arcB;
    public Vector3 contactPoint;
    public Vector3 normal;
    public float penetration;
    public RopeOverlapFlags flags;
}

/// <summary>Incremental index of non-adjacent segment overlaps for cord solving.</summary>
public class RopeOverlapIndex
{
    readonly List<RopeOverlapEntry> _entries = new List<RopeOverlapEntry>(32);
    readonly Dictionary<long, int> _pairToIndex = new Dictionary<long, int>();

    public IReadOnlyList<RopeOverlapEntry> Entries => _entries;

    static long PairKey(int a, int b)
    {
        if (a > b)
            (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    public void Clear() => _entries.Clear();
    public void ClearPairMap() => _pairToIndex.Clear();

    public void RegisterCollision(RopeSegmentBody a, RopeSegmentBody b, ContactPoint contact, RopeArcLengthState arc)
    {
        RegisterCollision(a, b, contact.point, contact.normal, contact.separation, arc);
    }

    public void RegisterCollision(
        RopeSegmentBody a,
        RopeSegmentBody b,
        Vector3 contactPoint,
        Vector3 normal,
        float separation,
        RopeArcLengthState arc)
    {
        if (a == null || b == null)
            return;
        int la = a.logicalSegmentIndex;
        int lb = b.logicalSegmentIndex;
        if (la < 0 || lb < 0)
            return;
        if (Mathf.Abs(la - lb) <= 1)
            return;

        float arcA = arc.SegmentArcStart(la);
        float arcB = arc.SegmentArcStart(lb);
        var flags = RopeOverlapFlags.SelfCross;
        if (arcA > arcB && Vector3.Dot(normal, (a.transform.position - b.transform.position).normalized) > 0f)
            flags |= RopeOverlapFlags.Tangle;

        var entry = new RopeOverlapEntry
        {
            segmentA = la,
            segmentB = lb,
            arcA = arcA,
            arcB = arcB,
            contactPoint = contactPoint,
            normal = normal,
            penetration = separation < 0f ? -separation : 0.01f,
            flags = flags
        };

        long key = PairKey(la, lb);
        if (_pairToIndex.TryGetValue(key, out int idx))
            _entries[idx] = entry;
        else
        {
            _pairToIndex[key] = _entries.Count;
            _entries.Add(entry);
        }
    }

    public void RegisterExternal(RopeSegmentBody seg, ContactPoint contact, RopeArcLengthState arc)
    {
        RegisterExternal(seg, contact.point, contact.normal, contact.separation, arc);
    }

    public void RegisterExternal(
        RopeSegmentBody seg,
        Vector3 contactPoint,
        Vector3 normal,
        float separation,
        RopeArcLengthState arc)
    {
        if (seg == null || seg.logicalSegmentIndex < 0)
            return;
        var entry = new RopeOverlapEntry
        {
            segmentA = seg.logicalSegmentIndex,
            segmentB = -1,
            arcA = arc.SegmentArcStart(seg.logicalSegmentIndex),
            contactPoint = contactPoint,
            normal = normal,
            penetration = separation < 0f ? -separation : 0.01f,
            flags = RopeOverlapFlags.External
        };
        _entries.Add(entry);
    }

    public void InvalidateLogicalRange(int fromLogical, int toLogical)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            RopeOverlapEntry e = _entries[i];
            if ((e.segmentA >= fromLogical && e.segmentA <= toLogical) ||
                (e.segmentB >= fromLogical && e.segmentB <= toLogical))
            {
                _entries.RemoveAt(i);
            }
        }
        _pairToIndex.Clear();
        for (int i = 0; i < _entries.Count; i++)
        {
            RopeOverlapEntry e = _entries[i];
            if (e.segmentB >= 0)
                _pairToIndex[PairKey(e.segmentA, e.segmentB)] = i;
        }
    }

    public bool HasTangle => _entries.Exists(e => (e.flags & RopeOverlapFlags.Tangle) != 0);
}
