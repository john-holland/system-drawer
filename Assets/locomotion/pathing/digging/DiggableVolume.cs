using System;
using System.Collections.Generic;
using SdfMax;
using SpatialVolumes;
using UnityEngine;

public enum DiggableVolumeKind
{
    Wall = 0,
    Soil = 1,
    Foundation = 2,
    Snow = 3
}

/// <summary>One clear or fill committed on a diggable volume, already clamped by granularity, limit, and size.</summary>
public struct DiggableIntegralReport
{
    public DiggableVolume volume;
    public Bounds box;
    public float step;
    public float cleared;
    public float filled;
    public bool wasClear;
}

public struct IntegralSample
{
    public Vector3 point;
    public bool inside;
}

/// <summary>Generic diggable volume for houses, prisons, and excavation sites.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Digging/Diggable Volume")]
public class DiggableVolume : MonoBehaviour
{
    public bool diggable = true;
    [Range(0f, 1f)] public float destructibility01 = 0.5f;
    [Range(0f, 1f)] public float tunnelStress01;
    public string materialClass = "masonry";
    public Bounds localBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
    public int floorIndex = 1;
    public DiggableVolumeKind volumeKind = DiggableVolumeKind.Wall;
    public SdfMaxCompositionAsset sdf;

    [Header("Integral space")]
    [Min(0.01f)] public float integralGranularity = 0.25f;
    [Min(0.01f)] public float integralLimit = 8f;
    [Min(0.05f)] public float integralSize = 4f;
    public float clearedIntegral;
    public float filledIntegral;
    [Min(0f)] public float iceBandThickness;
    public Bounds lastIntegralBox;
    public SpatialVolumeProvider spatialProvider;

    public static event Action<DiggableIntegralReport> IntegralCommitted;

    public Bounds WorldBounds
    {
        get
        {
            var b = localBounds;
            Vector3 c = transform.TransformPoint(b.center);
            return new Bounds(c, Vector3.Scale(b.size, transform.lossyScale));
        }
    }

    public int ApplyScoop(DigScoopSph sph, Vector3 contactWorld, float amount)
    {
        sph ??= new DigScoopSph();
        var node = sph.BuildSubtractNode(transform.InverseTransformPoint(contactWorld), amount);
        if (sdf == null)
            return 0;
        if (sdf.nodes == null)
            sdf.nodes = new List<SdfMaxNode>();
        Bounds box = IntegralBox(contactWorld, amount, out float step);
        List<IntegralSample> before = Capture(box, step);
        int child = sdf.nodes.Count;
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = node.primitiveType,
            localPosition = node.localPosition,
            radius = node.radius,
            sphereRadius = node.sphereRadius
        });
        int prevRoot = sdf.ResolveRootIndex();
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = prevRoot,
            childIndexB = child
        });
        sdf.rootNodeIndex = sdf.nodes.Count - 1;
        CommitIntegral(box, step, before, true);
        return 1;
    }

    /// <summary>Union a sphere of solid back into the volume and commit the filled integral.</summary>
    public int ApplyFill(Vector3 contactWorld, float amount)
    {
        if (sdf == null)
            return 0;
        if (sdf.nodes == null)
            sdf.nodes = new List<SdfMaxNode>();
        Bounds box = IntegralBox(contactWorld, amount, out float step);
        List<IntegralSample> before = Capture(box, step);
        int child = sdf.nodes.Count;
        float radius = Mathf.Max(0.05f, amount);
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            localPosition = transform.InverseTransformPoint(contactWorld),
            radius = radius,
            sphereRadius = radius
        });
        int prevRoot = sdf.ResolveRootIndex();
        sdf.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Min,
            childIndexA = prevRoot,
            childIndexB = child
        });
        sdf.rootNodeIndex = sdf.nodes.Count - 1;
        CommitIntegral(box, step, before, false);
        return 1;
    }

    Bounds IntegralBox(Vector3 contactWorld, float amount, out float step)
    {
        step = Mathf.Max(0.01f, integralGranularity);
        float reach = Mathf.Max(step, Mathf.Max(0.05f, amount) * 2f);
        float extent = Mathf.Min(Mathf.Max(0.05f, integralSize), reach);
        return new Bounds(contactWorld, Vector3.one * extent);
    }

    List<IntegralSample> Capture(Bounds box, float step)
    {
        var list = new List<IntegralSample>();
        int n = Mathf.Max(1, Mathf.CeilToInt(box.size.x / step));
        int budget = 4096;
        Vector3 origin = box.min;
        for (int iz = 0; iz < n && budget > 0; iz++)
        for (int iy = 0; iy < n && budget > 0; iy++)
        for (int ix = 0; ix < n && budget > 0; ix++)
        {
            budget--;
            Vector3 p = origin + new Vector3((ix + 0.5f) * step, (iy + 0.5f) * step, (iz + 0.5f) * step);
            if (!box.Contains(p))
                continue;
            list.Add(new IntegralSample { point = p, inside = InIceBand(p) || EvaluateInside(p) });
        }
        return list;
    }

    public bool InIceBand(Vector3 world)
    {
        if (iceBandThickness <= 0f)
            return false;
        Bounds wb = WorldBounds;
        return world.y >= wb.max.y - iceBandThickness && world.y <= wb.max.y + 0.001f && wb.Contains(world);
    }

    public bool ContainsSolid(Vector3 world)
    {
        if (InIceBand(world))
            return true;
        return EvaluateInside(world);
    }

    public void CommitIntegral(Bounds box, float step, List<IntegralSample> before, bool clear)
    {
        float addedClear = 0f;
        float addedFill = 0f;
        float quantum = step * step * step;
        float limit = Mathf.Max(quantum, integralLimit);
        if (before != null)
        {
            for (int i = 0; i < before.Count; i++)
            {
                if (addedClear + addedFill >= limit - 1e-5f)
                    break;
                IntegralSample sample = before[i];
                if (InIceBand(sample.point))
                    continue;
                bool after = EvaluateInside(sample.point);
                if (clear && sample.inside && !after)
                    addedClear += quantum;
                else if (!clear && !sample.inside && after)
                    addedFill += quantum;
            }
        }
        clearedIntegral += addedClear;
        filledIntegral += addedFill;
        lastIntegralBox = box;
        if (spatialProvider != null && spatialProvider.composition == sdf)
            SpatialVolumeCacheRegistry.Invalidate(spatialProvider);
        PatchCoincidentPathing(box, step);
        var report = new DiggableIntegralReport
        {
            volume = this,
            box = box,
            step = step,
            cleared = addedClear,
            filled = addedFill,
            wasClear = clear
        };
        IntegralCommitted?.Invoke(report);
    }

    void PatchCoincidentPathing(Bounds box, float step)
    {
        var solvers = UnityEngine.Object.FindObjectsByType<HierarchicalPathingSolver>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < solvers.Length; i++)
        {
            HierarchicalPathingSolver solver = solvers[i];
            if (solver == null || !solver.worldBounds.Intersects(box))
                continue;
            solver.PatchOccupancy(box, step, ContainsSolid);
        }
    }

    bool EvaluateInside(Vector3 world)
    {
        if (sdf == null || sdf.nodes == null || sdf.nodes.Count == 0)
            return false;
        var graph = new SdfMaxExpressionGraph(sdf, null, transform.localToWorldMatrix);
        var eval = new SdfMaxEvaluator(graph);
        return eval.IsInside(world, 0f);
    }
}
