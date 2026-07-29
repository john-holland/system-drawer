using System;
using UnityEngine;

/// <summary>
/// Scans physics colliders near the scalp and writes nearest-fit capsules into slots 6–9.
/// </summary>
[AddComponentMenu("Locomotion/Hair/Collider Primitive Scanner")]
public sealed class HairColliderPrimitiveScanner : MonoBehaviour
{
    public Transform scalpRoot;
    public HairPlumeConfig config;
    public Collider[] ignoreColliders;
    public int maxResults = 16;

    readonly Collider[] _overlap = new Collider[32];
    readonly Candidate[] _candidates = new Candidate[32];

    struct Candidate
    {
        public Vector3 center;
        public float radius;
        public float score;
    }

    public void ScanAndWrite(HairCapsuleBuffer buffer)
    {
        if (buffer == null || scalpRoot == null) return;
        buffer.ClearDynamicSlots();

        float scanR = config != null ? config.dynamicScanRadiusM : 0.55f;
        LayerMask mask = config != null ? config.dynamicScanMask : ~0;
        int hitCount = Physics.OverlapSphereNonAlloc(scalpRoot.position, scanR, _overlap, mask, QueryTriggerInteraction.Ignore);
        int candCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            var col = _overlap[i];
            if (col == null || ShouldIgnore(col)) continue;
            if (!HairCapsuleBuffer.TryFitColliderCapsule(col, out Vector3 center, out float radius))
                continue;

            float dist = Vector3.Distance(center, scalpRoot.position);
            float score = dist - radius * 0.25f;
            if (candCount < _candidates.Length)
            {
                _candidates[candCount++] = new Candidate { center = center, radius = radius, score = score };
            }
        }

        Array.Sort(_candidates, 0, candCount, Comparer.Instance);

        int write = Mathf.Min(HairCapsuleBuffer.DynamicSlots, candCount);
        for (int i = 0; i < write; i++)
            buffer.SetDynamicSlot(i, _candidates[i].center, _candidates[i].radius);
    }

    bool ShouldIgnore(Collider col)
    {
        if (col.transform == transform || col.transform.IsChildOf(transform))
            return true;
        if (ignoreColliders == null) return false;
        for (int i = 0; i < ignoreColliders.Length; i++)
        {
            if (ignoreColliders[i] == col) return true;
        }
        return false;
    }

    sealed class Comparer : System.Collections.Generic.IComparer<Candidate>
    {
        public static readonly Comparer Instance = new Comparer();
        public int Compare(Candidate a, Candidate b) => a.score.CompareTo(b.score);
    }
}
