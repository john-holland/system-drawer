using UnityEngine;

/// <summary>
/// Fixed 10-slot capsule buffer for HairPlume.shader (xyz = center, w = radius).
/// Slots 0–5 body, 6–9 dynamic collider fits.
/// </summary>
public sealed class HairCapsuleBuffer
{
    public const int SlotCount = HairPlumeConfig.CapsuleSlotCount;
    public const int BodySlots = HairPlumeConfig.BodyCapsuleSlots;
    public const int DynamicSlots = HairPlumeConfig.DynamicCapsuleSlots;

    public enum BodySlot
    {
        Head = 0,
        ChestShoulders = 1,
        LeftArm = 2,
        RightArm = 3,
        LeftKnee = 4,
        RightKnee = 5
    }

    readonly Vector4[] _slots = new Vector4[SlotCount];
    int _count;

    static readonly int[] CapsuleIds =
    {
        Shader.PropertyToID("_Capsule0"),
        Shader.PropertyToID("_Capsule1"),
        Shader.PropertyToID("_Capsule2"),
        Shader.PropertyToID("_Capsule3"),
        Shader.PropertyToID("_Capsule4"),
        Shader.PropertyToID("_Capsule5"),
        Shader.PropertyToID("_Capsule6"),
        Shader.PropertyToID("_Capsule7"),
        Shader.PropertyToID("_Capsule8"),
        Shader.PropertyToID("_Capsule9")
    };

    static readonly int CapsuleCountId = Shader.PropertyToID("_CapsuleCount");

    public int Count => _count;
    public Vector4[] Slots => _slots;

    public void Clear()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots[i] = Vector4.zero;
        _count = 0;
    }

    public void SetSlot(int index, Vector3 center, float radius)
    {
        if (index < 0 || index >= SlotCount) return;
        _slots[index] = new Vector4(center.x, center.y, center.z, Mathf.Max(0f, radius));
        _count = Mathf.Max(_count, index + 1);
    }

    public void SetSlot(BodySlot slot, Vector3 center, float radius) =>
        SetSlot((int)slot, center, radius);

    /// <summary>Pack capsule from two endpoints (midpoint + radius covering half-length).</summary>
    public void SetCapsuleFromSegment(int index, Vector3 a, Vector3 b, float radius)
    {
        Vector3 mid = (a + b) * 0.5f;
        float halfLen = Vector3.Distance(a, b) * 0.5f;
        SetSlot(index, mid, Mathf.Max(radius, halfLen * 0.35f + radius * 0.5f));
    }

    public void ClearDynamicSlots()
    {
        for (int i = BodySlots; i < SlotCount; i++)
            _slots[i] = Vector4.zero;
        if (_count > BodySlots)
            _count = BodySlots;
    }

    public void SetDynamicSlot(int dynamicIndex, Vector3 center, float radius)
    {
        if (dynamicIndex < 0 || dynamicIndex >= DynamicSlots) return;
        SetSlot(BodySlots + dynamicIndex, center, radius);
    }

    public void BindToMaterial(Material mat)
    {
        if (mat == null) return;
        mat.SetFloat(CapsuleCountId, _count);
        for (int i = 0; i < SlotCount; i++)
            mat.SetVector(CapsuleIds[i], _slots[i]);
    }

    public static bool TryFitColliderCapsule(Collider col, out Vector3 center, out float radius)
    {
        center = Vector3.zero;
        radius = 0f;
        if (col == null || !col.enabled) return false;

        if (col is CapsuleCollider cap)
        {
            Vector3 axis = cap.direction == 0 ? Vector3.right : (cap.direction == 1 ? Vector3.up : Vector3.forward);
            float half = Mathf.Max(0f, cap.height * 0.5f - cap.radius);
            Vector3 a = cap.transform.TransformPoint(cap.center + axis * half);
            Vector3 b = cap.transform.TransformPoint(cap.center - axis * half);
            center = (a + b) * 0.5f;
            Vector3 lossy = cap.transform.lossyScale;
            float s = Mathf.Max(lossy.x, Mathf.Max(lossy.y, lossy.z));
            radius = cap.radius * s;
            return radius > 1e-5f;
        }

        if (col is SphereCollider sphere)
        {
            center = sphere.transform.TransformPoint(sphere.center);
            Vector3 lossy = sphere.transform.lossyScale;
            float s = Mathf.Max(lossy.x, Mathf.Max(lossy.y, lossy.z));
            radius = sphere.radius * s;
            return radius > 1e-5f;
        }

        Bounds bnds = col.bounds;
        center = bnds.center;
        Vector3 e = bnds.extents;
        radius = Mathf.Max(e.x, Mathf.Max(e.y, e.z)) * 0.5f + Mathf.Min(e.x, Mathf.Min(e.y, e.z)) * 0.25f;
        return radius > 1e-5f;
    }
}
