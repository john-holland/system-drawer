using System;
using System.Collections.Generic;
using SdfMax;
using SpatialVolumes;
using UnityEngine;

public enum PlantRootBakeOutput
{
    DiggableVolume = 0,
    MeshOnly = 1
}

[Serializable]
public sealed class PlantRootGroundLayer
{
    public string layerId = "topsoil";
    [Min(0.01f)] public float heightM = 0.35f;
    public string materialClass = "loam";
}

/// <summary>
/// Builds a root SDF from plant tips, scoops a soil volume with <see cref="DigScoopSph"/>, and skins the root solid.
/// </summary>
public static class PlantRootSystemBake
{
    public static SdfMaxCompositionAsset BuildRootExpression(IList<PlantRootDef> roots)
    {
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.nodes = new List<SdfMaxNode>();
        if (roots == null) return asset;
        int splineCount = 0;
        for (int i = 0; i < roots.Count; i++)
        {
            var root = roots[i];
            if (root == null) continue;
            asset.nodes.Add(SplineRoot(root.localTip, root.radius));
            splineCount++;
        }
        if (splineCount == 0) return asset;
        if (splineCount == 1)
        {
            asset.rootNodeIndex = 0;
            return asset;
        }
        int acc = 0;
        for (int i = 1; i < splineCount; i++)
        {
            int min = asset.nodes.Count;
            asset.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Min,
                childIndexA = acc,
                childIndexB = i
            });
            acc = min;
        }
        asset.rootNodeIndex = acc;
        return asset;
    }

    public static Bounds ComputeSoilBounds(IList<PlantRootDef> roots, float margin, IList<PlantRootGroundLayer> layers)
    {
        bool any = false;
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (roots != null)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                if (!any)
                {
                    bounds = new Bounds(roots[i].localTip, Vector3.zero);
                    any = true;
                }
                else
                    bounds.Encapsulate(roots[i].localTip);
            }
        }
        if (!any)
            bounds = new Bounds(new Vector3(0f, -0.5f, 0f), Vector3.one * 0.4f);
        bounds.Encapsulate(Vector3.zero);
        bounds.Expand(Mathf.Max(0f, margin) * 2f);
        if (layers != null && layers.Count > 0)
        {
            float height = 0f;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] == null) continue;
                height += Mathf.Max(0.01f, layers[i].heightM);
            }
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            min.y = Mathf.Min(min.y, -height);
            max.y = Mathf.Max(max.y, 0f);
            bounds.SetMinMax(min, max);
        }
        return bounds;
    }

    public static SdfMaxCompositionAsset BuildSoilComposition(
        IList<PlantRootDef> roots,
        float margin,
        IList<PlantRootGroundLayer> layers)
    {
        var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
        asset.nodes = new List<SdfMaxNode>();
        Bounds bounds = ComputeSoilBounds(roots, margin, layers);
        if (layers == null || layers.Count == 0)
        {
            asset.nodes.Add(Box(bounds.center, bounds.extents));
            asset.rootNodeIndex = 0;
            return asset;
        }

        float y = 0f;
        int live = 0;
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null) continue;
            float h = Mathf.Max(0.01f, layer.heightM);
            float yTop = y;
            float yBot = y - h;
            if (live == 0)
                yTop = Mathf.Max(yTop, bounds.max.y);
            bool last = i == layers.Count - 1;
            if (last)
                yBot = Mathf.Min(yBot, bounds.min.y);
            int box = asset.nodes.Count;
            Vector3 center = new Vector3(bounds.center.x, (yTop + yBot) * 0.5f, bounds.center.z);
            Vector3 ext = new Vector3(bounds.extents.x, Mathf.Max(0.01f, (yTop - yBot) * 0.5f), bounds.extents.z);
            asset.nodes.Add(Box(center, ext));
            if (live == 0)
                asset.rootNodeIndex = box;
            else
            {
                int min = asset.nodes.Count;
                asset.nodes.Add(new SdfMaxNode
                {
                    op = SdfMaxOp.Min,
                    childIndexA = asset.rootNodeIndex,
                    childIndexB = box
                });
                asset.rootNodeIndex = min;
            }
            live++;
            y = yBot;
        }
        if (live == 0)
        {
            asset.nodes.Add(Box(bounds.center, bounds.extents));
            asset.rootNodeIndex = 0;
        }
        return asset;
    }

    public static void ConfigureSoilVolume(
        DiggableVolume volume,
        IList<PlantRootDef> roots,
        float margin,
        IList<PlantRootGroundLayer> layers,
        int particleCount,
        float scoopAmount)
    {
        if (volume == null) return;
        volume.diggable = true;
        volume.volumeKind = DiggableVolumeKind.Soil;
        volume.materialClass = TopMaterial(layers);
        volume.localBounds = ComputeSoilBounds(roots, margin, layers);
        volume.sdf = BuildSoilComposition(roots, margin, layers);
        ApplyScoops(volume, roots, particleCount, scoopAmount);
    }

    public static void ApplyScoops(DiggableVolume volume, IList<PlantRootDef> roots, int particleCount, float scoopAmount)
    {
        if (volume == null || volume.sdf == null || roots == null) return;
        var sph = new DigScoopSph();
        sph.SeedFill(Mathf.Max(1, particleCount), 0.5f);
        float amount = Mathf.Max(0.05f, scoopAmount);
        for (int i = 0; i < roots.Count; i++)
        {
            var root = roots[i];
            if (root == null) continue;
            sph.Scoop(null, root.localTip.magnitude, 0.1f, 20f);
            volume.ApplyScoop(sph, volume.transform.TransformPoint(root.localTip * 0.5f), amount);
            volume.ApplyScoop(sph, volume.transform.TransformPoint(root.localTip), amount);
        }
    }

    public static SdfMaxSkinnedMeshSurface BuildSkinnedHost(
        GameObject host,
        SdfMaxCompositionAsset rootExpression,
        IList<PlantRootDef> roots)
    {
        if (host == null) return null;
        var provider = host.GetComponent<SpatialVolumeProvider>() ?? host.AddComponent<SpatialVolumeProvider>();
        provider.backend = VolumeBackend.SdfMaxComposition;
        provider.composition = rootExpression;
        provider.renderMode = SdfMaxRenderMode.SkinnedMesh;
        provider.SyncRenderModeComponents();
        var skin = host.GetComponent<SdfMaxSkinnedMeshSurface>() ?? host.AddComponent<SdfMaxSkinnedMeshSurface>();
        skin.rootBone = host.transform;
        skin.bones = CreateTipBones(host.transform, roots);
        skin.RebuildSurfaceMesh();
        return skin;
    }

    public static string TopMaterial(IList<PlantRootGroundLayer> layers)
    {
        if (layers != null)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && !string.IsNullOrEmpty(layers[i].materialClass))
                    return layers[i].materialClass;
            }
        }
        return "loam";
    }

    static Transform[] CreateTipBones(Transform crown, IList<PlantRootDef> roots)
    {
        var list = new List<Transform>();
        if (roots == null) return list.ToArray();
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] == null) continue;
            var bone = new GameObject("root_" + i);
            bone.transform.SetParent(crown, false);
            bone.transform.localPosition = roots[i].localTip;
            list.Add(bone.transform);
        }
        return list.ToArray();
    }

    static SdfMaxNode SplineRoot(Vector3 tip, float radius)
    {
        return new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.SplineExtrusion,
            extrusionRadius = Mathf.Max(0.001f, radius),
            extrusionEnd = tip,
            extrusionPath = new List<Vector3> { Vector3.zero, tip }
        };
    }

    static SdfMaxNode Box(Vector3 center, Vector3 extents)
    {
        return new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Box,
            localPosition = center,
            halfExtents = new Vector3(
                Mathf.Max(0.01f, extents.x),
                Mathf.Max(0.01f, extents.y),
                Mathf.Max(0.01f, extents.z))
        };
    }
}
