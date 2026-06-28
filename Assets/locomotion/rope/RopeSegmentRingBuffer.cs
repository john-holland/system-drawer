using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixed-size ring of segment rigidbodies mapped to logical arc indices.
/// </summary>
public class RopeSegmentRingBuffer
{
    readonly RopeConfig _config;
    readonly RopeArcLengthState _arc;
    readonly Transform _root;
    readonly RopeSegmentBody[] _slots;
    readonly Dictionary<int, int> _logicalToSlot = new Dictionary<int, int>();
    int _headSlot;

    public RopeSegmentRingBuffer(RopeConfig config, RopeArcLengthState arc, Transform root, RopeSegmentBody segmentPrefab)
    {
        _config = config;
        _arc = arc;
        _root = root;
        _slots = new RopeSegmentBody[config.ringBufferSize];
        for (int s = 0; s < _slots.Length; s++)
        {
            RopeSegmentBody body = segmentPrefab != null
                ? Object.Instantiate(segmentPrefab, root)
                : CreateDefaultSegment(root, s);
            body.name = $"RopeSegment_slot{s}";
            body.Configure(config, -1, s);
            body.SetSimulated(false);
            _slots[s] = body;
        }
        ApplyAdjacencyCollisionIgnores();
    }

    static RopeSegmentBody CreateDefaultSegment(Transform root, int slot)
    {
        var go = new GameObject($"RopeSegment_slot{slot}");
        go.transform.SetParent(root, false);
        return go.AddComponent<RopeSegmentBody>();
    }

    public IReadOnlyList<RopeSegmentBody> Slots => _slots;

    public int HeadSlot => _headSlot;

    public int SlotForLogical(int logicalIndex)
    {
        if (!_logicalToSlot.TryGetValue(logicalIndex, out int slot))
            return -1;
        return slot;
    }

    public RopeSegmentBody GetBody(int logicalIndex)
    {
        int slot = SlotForLogical(logicalIndex);
        return slot >= 0 ? _slots[slot] : null;
    }

    public void RebuildActiveMapping(Transform headAnchor, Transform tailAnchor, Vector3 unwindDirection)
    {
        _logicalToSlot.Clear();
        int activeCount = _arc.ActiveSegmentCount;
        int ringSize = _slots.Length;
        int startLogical = _arc.ActiveHeadSegment;
        int endLogical = _arc.ActiveTailSegment;

        for (int i = 0; i < ringSize; i++)
        {
            int logical = startLogical + i;
            int slot = (_headSlot + i) % ringSize;
            RopeSegmentBody body = _slots[slot];

            if (logical > endLogical)
            {
                body.SetSimulated(false);
                body.logicalSegmentIndex = -1;
                continue;
            }

            body.logicalSegmentIndex = logical;
            body.ringSlotIndex = slot;
            body.SetSimulated(true);
            _logicalToSlot[logical] = slot;

            float arc = _arc.SegmentArcStart(logical);
            Vector3 pos;
            Quaternion rot;
            if (logical == startLogical && headAnchor != null)
            {
                pos = headAnchor.position + unwindDirection.normalized * (_config.segmentLengthM * 0.5f);
                rot = headAnchor.rotation;
            }
            else if (_arc.TryGetWoundPose(arc, out RopeWoundPose wound))
            {
                pos = wound.position;
                rot = wound.rotation;
            }
            else if (logical == endLogical && tailAnchor != null)
            {
                pos = tailAnchor.position;
                rot = tailAnchor.rotation;
            }
            else
            {
                float t = _arc.ArcToNormalized(arc);
                pos = headAnchor != null
                    ? headAnchor.position + unwindDirection.normalized * (arc - _arc.WoundLengthM)
                    : Vector3.down * t * _config.totalLengthM;
                rot = headAnchor != null ? headAnchor.rotation : Quaternion.identity;
            }

            if (body.Rigidbody.isKinematic || body.Rigidbody.linearVelocity.sqrMagnitude < 0.01f)
            {
                body.transform.SetPositionAndRotation(pos, rot);
            }
        }

        WireJoints();
        ApplyAdjacencyCollisionIgnores();
    }

    void WireJoints()
    {
        var activeBodies = new List<RopeSegmentBody>();
        for (int logical = _arc.ActiveHeadSegment; logical <= _arc.ActiveTailSegment; logical++)
        {
            RopeSegmentBody b = GetBody(logical);
            if (b != null && b.gameObject.activeSelf)
                activeBodies.Add(b);
        }

        for (int i = 0; i < activeBodies.Count; i++)
        {
            RopeSegmentBody body = activeBodies[i];
            body.neighborTowardHead = i > 0 ? activeBodies[i - 1] : null;
            body.neighborTowardTail = i < activeBodies.Count - 1 ? activeBodies[i + 1] : null;
            EnsureJoint(body, body.neighborTowardHead, ref body.jointToHead);
            EnsureJoint(body, body.neighborTowardTail, ref body.jointToTail);
        }
    }

    static void EnsureJoint(RopeSegmentBody body, RopeSegmentBody connected, ref ConfigurableJoint joint)
    {
        if (connected == null)
        {
            if (joint != null)
                joint.connectedBody = null;
            return;
        }

        if (joint == null)
        {
            joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = true;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
        }

        joint.connectedBody = connected.Rigidbody;
    }

    public void ApplyJointParameters(float spring, float damper)
    {
        foreach (RopeSegmentBody body in _slots)
        {
            ApplySpring(body.jointToHead, spring, damper);
            ApplySpring(body.jointToTail, spring, damper);
        }
    }

    static void ApplySpring(ConfigurableJoint joint, float spring, float damper)
    {
        if (joint == null)
            return;
        var drive = new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = spring * 2f
        };
        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;
    }

    public void CaptureTailToWound(RopeArcLengthState arc)
    {
        int tailLogical = arc.ActiveTailSegment;
        RopeSegmentBody tail = GetBody(tailLogical);
        if (tail == null)
            return;
        float arcM = arc.SegmentArcStart(tailLogical);
        arc.StoreWoundPose(arcM, tail.transform.position, tail.transform.rotation);
        tail.SetSimulated(false);
        _logicalToSlot.Remove(tailLogical);
        _headSlot = (_headSlot + 1) % _slots.Length;
    }

    public void ActivateHeadFromSpool(Transform spool, RopeArcLengthState arc)
    {
        int headLogical = arc.ActiveHeadSegment;
        int slot = _headSlot;
        RopeSegmentBody body = _slots[slot];
        body.logicalSegmentIndex = headLogical;
        body.SetSimulated(true);
        _logicalToSlot[headLogical] = slot;

        float arcM = arc.SegmentArcStart(headLogical);
        if (arc.TryGetWoundPose(arcM, out RopeWoundPose wound))
            body.transform.SetPositionAndRotation(wound.position, wound.rotation);
        else if (spool != null)
            body.transform.SetPositionAndRotation(spool.position, spool.rotation);
    }

    void ApplyAdjacencyCollisionIgnores()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            for (int j = i + 1; j < _slots.Length; j++)
            {
                RopeSegmentBody a = _slots[i];
                RopeSegmentBody b = _slots[j];
                if (a == null || b == null || a.Collider == null || b.Collider == null)
                    continue;
                int la = a.logicalSegmentIndex;
                int lb = b.logicalSegmentIndex;
                bool adjacent = la >= 0 && lb >= 0 && Mathf.Abs(la - lb) <= 1;
                Physics.IgnoreCollision(a.Collider, b.Collider, adjacent);
            }
        }
    }

    public void InvalidateLogicalRange(int fromLogical, int toLogical)
    {
        for (int l = fromLogical; l <= toLogical; l++)
            _logicalToSlot.Remove(l);
    }
}
