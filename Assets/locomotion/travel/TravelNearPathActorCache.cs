using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Broad-phase cache of dynamic colliders overlapping the union bounds of active travel polylines for a solve.
/// </summary>
public static class TravelNearPathActorCache
{
    public struct DynamicActorEntry
    {
        public Vector3 center;
        public float radiusXZ;
        public int layer;
    }

    static readonly List<DynamicActorEntry> s_entries = new List<DynamicActorEntry>(64);
    static readonly Collider[] s_overlapBuffer = new Collider[256];

    /// <summary>
    /// Repopulates entries from physics overlap. Call once per multibody solve with union bounds of all considered paths.
    /// </summary>
    public static IReadOnlyList<DynamicActorEntry> Rebuild(Bounds queryBounds, LayerMask mask)
    {
        s_entries.Clear();
        Vector3 half = queryBounds.extents;
        if (half.sqrMagnitude < 1e-8f)
            half = Vector3.one * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(
            queryBounds.center,
            half,
            s_overlapBuffer,
            Quaternion.identity,
            mask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = s_overlapBuffer[i];
            if (c == null)
                continue;
            Bounds b = c.bounds;
            float rxz = Mathf.Max(b.extents.x, b.extents.z);
            s_entries.Add(new DynamicActorEntry
            {
                center = b.center,
                radiusXZ = Mathf.Max(0.05f, rxz),
                layer = c.gameObject.layer
            });
        }

        return s_entries;
    }
}
