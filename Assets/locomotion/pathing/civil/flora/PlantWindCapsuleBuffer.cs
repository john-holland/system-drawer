using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlantWindCapsuleIntent
{
    PassThrough = 0,
    Fall = 1,
    Lodge = 2
}

public struct PlantWindCapsule
{
    public int id;
    public Vector3 a;
    public Vector3 b;
    public float radius;
    public float extraSize;
    public PlantWindCapsuleIntent intent;
    public Collider sourceCollider;
    public bool outsideDebris;

    public float TotalRadius => Mathf.Max(0f, radius) + Mathf.Max(0f, extraSize);

    public bool IsShaderInclusion => id != 0 && (sourceCollider == null || outsideDebris);
}

/// <summary>
/// Fixed ring of canopy pass-through capsules. The oldest branch slot drops when the ring is full.
/// Outside debris claims a free slot and resizes it. The tree's own colliders are not included.
/// </summary>
public sealed class PlantWindCapsuleBuffer
{
    public const int SlotCount = 8;

    readonly PlantWindCapsule[] _slots = new PlantWindCapsule[SlotCount];
    int _count;
    int _head;
    int _serial;

    static readonly int CountId = Shader.PropertyToID("_PlantCapCount");
    static readonly int OmitId = Shader.PropertyToID("_OmitBend");
    static readonly int CapAId = Shader.PropertyToID("_PlantCapA");
    static readonly int CapBId = Shader.PropertyToID("_PlantCapB");

    public int Count => _count;

    public int ShaderInclusionCount
    {
        get
        {
        int n = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].IsShaderInclusion)
                n++;
        }
        return n;
        }
    }

    public int Push(PlantWindCapsule capsule)
    {
        if (capsule.sourceCollider != null)
            return 0;
        int guard = 0;
        while (guard < SlotCount && _slots[_head].outsideDebris)
        {
            _head = (_head + 1) % SlotCount;
            guard++;
        }
        if (_slots[_head].outsideDebris)
            return 0;
        if (capsule.id == 0)
            capsule.id = ++_serial;
        _slots[_head] = capsule;
        _head = (_head + 1) % SlotCount;
        if (_count < SlotCount)
            _count++;
        return capsule.id;
    }

    public bool Contains(int id)
    {
        return TryGet(id, out _);
    }

    public bool TryGet(int id, out PlantWindCapsule capsule)
    {
        capsule = default;
        if (id == 0)
            return false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].id != id)
                continue;
            capsule = _slots[i];
            return true;
        }
        return false;
    }

    public bool SetIntent(int id, PlantWindCapsuleIntent intent)
    {
        if (id == 0)
            return false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].id != id)
                continue;
            PlantWindCapsule capsule = _slots[i];
            capsule.intent = intent;
            _slots[i] = capsule;
            return true;
        }
        return false;
    }

    public void Bind(Material material, bool omitBend)
    {
        if (material == null)
            return;
        var capA = new Vector4[SlotCount];
        var capB = new Vector4[SlotCount];
        int included = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            PlantWindCapsule capsule = _slots[i];
            if (!capsule.IsShaderInclusion)
                continue;
            capA[included] = new Vector4(capsule.a.x, capsule.a.y, capsule.a.z, Mathf.Max(0f, capsule.radius));
            capB[included] = new Vector4(capsule.b.x, capsule.b.y, capsule.b.z, Mathf.Max(0f, capsule.extraSize));
            included++;
        }
        material.SetFloat(CountId, included);
        material.SetFloat(OmitId, omitBend ? 1f : 0f);
        material.SetVectorArray(CapAId, capA);
        material.SetVectorArray(CapBId, capB);
    }

    public int ClaimOutsideDebris(Collider debris, Transform treeRoot, float extraSize)
    {
        if (!IsOutsideDebris(debris, treeRoot))
            return 0;
        if (!TryFit(debris, out Vector3 a, out Vector3 b, out float radius))
            return 0;
        return ClaimFitted(debris, a, b, radius, Mathf.Max(0f, extraSize));
    }

    public int ScanAttachmentStress(Collider actorBody, Transform treeRoot, bool climbingOrAttached, float stress01, float extraSize)
    {
        if (!climbingOrAttached || stress01 <= 1e-4f)
            return 0;
        float stressed = Mathf.Max(0f, extraSize) * (1f + Mathf.Clamp01(stress01));
        return ClaimOutsideDebris(actorBody, treeRoot, stressed);
    }

    int ClaimFitted(Collider debris, Vector3 a, Vector3 b, float radius, float extraSize)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].sourceCollider != debris)
                continue;
            PlantWindCapsule sized = _slots[i];
            sized.a = a;
            sized.b = b;
            sized.radius = radius;
            sized.extraSize = extraSize;
            sized.outsideDebris = true;
            _slots[i] = sized;
            return sized.id;
        }

        int free = -1;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].id == 0)
            {
                free = i;
                break;
            }
        }
        if (free < 0)
            return 0;

        var capsule = new PlantWindCapsule
        {
            id = ++_serial,
            a = a,
            b = b,
            radius = radius,
            extraSize = extraSize,
            intent = PlantWindCapsuleIntent.PassThrough,
            sourceCollider = debris,
            outsideDebris = true
        };
        _slots[free] = capsule;
        return capsule.id;
    }

    public static bool IsOutsideDebris(Collider debris, Transform treeRoot)
    {
        if (debris == null)
            return false;
        if (treeRoot == null)
            return true;
        Transform t = debris.transform;
        return t != treeRoot && !t.IsChildOf(treeRoot);
    }

    public static bool TryFit(Collider debris, out Vector3 a, out Vector3 b, out float radius)
    {
        a = b = Vector3.zero;
        radius = 0f;
        if (debris == null || !debris.enabled)
            return false;
        if (debris is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float scale = MaxScale(sphere.transform.lossyScale);
            a = b = center;
            radius = sphere.radius * scale;
            return radius > 1e-5f;
        }
        if (debris is CapsuleCollider cap)
        {
            Vector3 axis = cap.direction == 0 ? Vector3.right : (cap.direction == 1 ? Vector3.up : Vector3.forward);
            float half = Mathf.Max(0f, cap.height * 0.5f - cap.radius);
            float scale = MaxScale(cap.transform.lossyScale);
            a = cap.transform.TransformPoint(cap.center + axis * half);
            b = cap.transform.TransformPoint(cap.center - axis * half);
            radius = cap.radius * scale;
            return radius > 1e-5f;
        }
        Bounds bounds = debris.bounds;
        Vector3 ext = bounds.extents;
        int axisIndex = ext.x >= ext.y && ext.x >= ext.z ? 0 : (ext.y >= ext.z ? 1 : 2);
        Vector3 dir = axisIndex == 0 ? Vector3.right : (axisIndex == 1 ? Vector3.up : Vector3.forward);
        float halfLen = axisIndex == 0 ? ext.x : (axisIndex == 1 ? ext.y : ext.z);
        a = bounds.center - dir * halfLen;
        b = bounds.center + dir * halfLen;
        radius = axisIndex == 0 ? Mathf.Max(ext.y, ext.z) : (axisIndex == 1 ? Mathf.Max(ext.x, ext.z) : Mathf.Max(ext.x, ext.y));
        return radius > 1e-5f;
    }

    static float MaxScale(Vector3 lossy)
    {
        return Mathf.Max(lossy.x, Mathf.Max(lossy.y, lossy.z));
    }

    int SlotIndex(int age)
    {
        int start = (_head - _count + SlotCount * 2) % SlotCount;
        return (start + age) % SlotCount;
    }
}

public static class PlantWindClearance
{
    public static bool PetalOmitted(SdfSealedClothFragment fragment, float tear01)
    {
        if (fragment != null && fragment.broken)
            return true;
        return tear01 > 1e-4f && fragment != null;
    }

    public static void CollectFruit(FlowerDevStep step, List<Vector3> into)
    {
        if (step?.pieces == null || into == null)
            return;
        bool fruiting = step.kind == FlowerDevStepKind.FruitSet || step.kind == FlowerDevStepKind.SeedMaturity;
        for (int i = 0; i < step.pieces.Count; i++)
        {
            FlowerOrganPiece piece = step.pieces[i];
            if (piece == null)
                continue;
            if (piece.kind == FlowerOrganKind.Fruit || (fruiting && piece.kind == FlowerOrganKind.Ovary))
                into.Add(piece.centroidLocal);
        }
    }

    public static Vector3[] ClearPetalCurve(
        Vector3[] curve,
        Vector3 normal,
        IList<Vector3> fruit,
        float fruitRadius,
        IList<BranchPathBake> liveBranches,
        float woodRadius)
    {
        if (curve == null || curve.Length == 0)
            return Array.Empty<Vector3>();
        Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
        var cleared = (Vector3[])curve.Clone();
        float fruitR = Mathf.Max(0f, fruitRadius);
        float woodR = Mathf.Max(0f, woodRadius);
        for (int i = 0; i < cleared.Length; i++)
        {
            for (int step = 0; step < 8; step++)
            {
                if (!Hits(cleared[i], fruit, fruitR, liveBranches, woodR))
                    break;
                float push = Mathf.Max(0.01f, Mathf.Max(fruitR, woodR));
                cleared[i] += n * push;
            }
        }
        return cleared;
    }

    public static bool Intersects(
        Vector3[] curve,
        IList<Vector3> fruit,
        float fruitRadius,
        IList<BranchPathBake> liveBranches,
        float woodRadius)
    {
        if (curve == null)
            return false;
        for (int i = 0; i < curve.Length; i++)
        {
            if (Hits(curve[i], fruit, fruitRadius, liveBranches, woodRadius))
                return true;
        }
        return false;
    }

    static bool Hits(Vector3 point, IList<Vector3> fruit, float fruitRadius, IList<BranchPathBake> liveBranches, float woodRadius)
    {
        if (fruit != null)
        {
            float r2 = fruitRadius * fruitRadius;
            for (int i = 0; i < fruit.Count; i++)
            {
                if ((point - fruit[i]).sqrMagnitude <= r2)
                    return true;
            }
        }
        if (liveBranches == null)
            return false;
        for (int b = 0; b < liveBranches.Count; b++)
        {
            BranchPathBake bake = liveBranches[b];
            if (bake == null || bake.broken || bake.positions == null || bake.positions.Length == 0)
                continue;
            if (bake.positions.Length == 1)
            {
                if ((point - bake.positions[0]).sqrMagnitude <= woodRadius * woodRadius)
                    return true;
                continue;
            }
            for (int i = 1; i < bake.positions.Length; i++)
            {
                if (DistanceToSegment(point, bake.positions[i - 1], bake.positions[i]) <= woodRadius)
                    return true;
            }
        }
        return false;
    }

    static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        float t = denom > 1e-8f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / denom) : 0f;
        return Vector3.Distance(point, a + ab * t);
    }
}
