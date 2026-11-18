using SdfMax;
using UnityEngine;

/// <summary>
/// Bakes one soil DiggableVolume and one sealed skin per horizon.
/// Each horizon keeps its own SDF so it can lift and tear on its own.
/// </summary>
public static class HorizonContextSplitter
{
    public static int Bake(GroundCompositionSet set, Transform host)
    {
        if (set == null || host == null)
            return 0;
        set.EnsureDefaultHorizons();
        int baked = 0;
        float depth = 0f;
        for (int i = 0; i < set.horizons.Count; i++)
        {
            SoilHorizonLayer layer = set.horizons[i];
            if (layer == null)
                continue;
            var child = new GameObject("Horizon_" + layer.id);
            child.transform.SetParent(host, false);
            child.transform.localPosition = new Vector3(0f, -depth - layer.thicknessM * 0.5f, 0f);
            var volume = child.AddComponent<DiggableVolume>();
            volume.volumeKind = DiggableVolumeKind.Soil;
            volume.diggable = true;
            volume.materialClass = layer.compositionId;
            volume.destructibility01 = set.DestructibilityForHorizon(layer);
            volume.localBounds = new Bounds(Vector3.zero, new Vector3(2f, layer.thicknessM, 2f));
            volume.sdf = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            volume.sdf.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.Box,
                halfExtents = volume.localBounds.extents,
                localScale = Vector3.one
            });
            volume.sdf.rootNodeIndex = 0;
            var skin = child.AddComponent<SdfSealedClothSkin>();
            skin.fragments.Add(new SdfSealedClothFragment
            {
                id = layer.id.ToString(),
                centroid = child.transform.localPosition,
                curve = SdfSealedClothSkin.CurvedSection(Vector3.zero, Vector3.forward, layer.thicknessM),
                broken = false
            });
            depth += layer.thicknessM;
            baked++;
        }
        return baked;
    }
}
