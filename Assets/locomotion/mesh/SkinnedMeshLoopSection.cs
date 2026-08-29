using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds a <see cref="SkinnedMeshLoopSectionAsset"/> to a SkinnedMeshRenderer or MeshRenderer.
/// When the live mesh/textures diverge, <see cref="meshUpdated"/> is true until overwrite or useCached.
/// </summary>
[AddComponentMenu("Locomotion/Mesh/Skinned Mesh Loop Section")]
[ExecuteAlways]
public sealed class SkinnedMeshLoopSection : MonoBehaviour
{
    public SkinnedMeshLoopSectionAsset sectionAsset;
    public bool meshUpdated;
    [HideInInspector] public bool useCached;
    public List<LoopSplitBoundsBinding> splitBoundsBindings = new List<LoopSplitBoundsBinding>();

    bool _loggedSkip;

    public Renderer Renderer => SkinnedMeshLoopRendererUtil.Resolve(this);

    public SkinnedMeshRenderer SkinnedRenderer => Renderer as SkinnedMeshRenderer;

    public Mesh SharedMesh => SkinnedMeshLoopRendererUtil.SharedMesh(Renderer);

    public bool CanApplyLoop
    {
        get
        {
            if (sectionAsset == null)
                return false;
            if (!meshUpdated)
                return true;
            return useCached;
        }
    }

    public bool CanSetUseCached => meshUpdated;

    void OnValidate()
    {
        if (!meshUpdated)
            useCached = false;
        RefreshMeshUpdated();
    }

    public void RefreshMeshUpdated()
    {
        var rend = Renderer;
        Mesh mesh = SharedMesh;
        if (sectionAsset == null || rend == null || mesh == null)
        {
            meshUpdated = false;
            return;
        }
        var liveTex = SkinnedMeshLoopHasher.CollectTextures(rend);
        meshUpdated = !sectionAsset.LiveMatchesOriginal(mesh, liveTex);
        if (!meshUpdated)
            useCached = false;
    }

    public void ApplyUseCachedSnapshot()
    {
        if (!meshUpdated)
        {
            useCached = false;
            return;
        }
        useCached = true;
        var rend = Renderer;
        Mesh mesh = SharedMesh;
        if (sectionAsset == null || rend == null || mesh == null)
            return;
        sectionAsset.SnapshotSavedCache(mesh, SkinnedMeshLoopHasher.CollectTextures(rend));
    }

    public void OverwriteAndUpdateSavedCache()
    {
        var rend = Renderer;
        Mesh mesh = SharedMesh;
        if (sectionAsset == null || rend == null || mesh == null || !meshUpdated || !useCached)
            return;
        sectionAsset.OverwriteOriginalsFromCacheOrLive(mesh, SkinnedMeshLoopHasher.CollectTextures(rend));
        meshUpdated = false;
        useCached = false;
        _loggedSkip = false;
    }

    public bool TryGetWorkingMesh(out Mesh mesh)
    {
        mesh = null;
        var rend = Renderer;
        if (rend == null)
            return false;
        if (meshUpdated && !useCached)
        {
            if (!_loggedSkip)
            {
                Debug.LogWarning(
                    "[SkinnedMeshLoopSection] meshUpdated and not useCached; skip applying loop / split.",
                    this);
                _loggedSkip = true;
            }
            return false;
        }
        if (useCached && sectionAsset != null && sectionAsset.savedCacheMesh != null)
        {
            mesh = sectionAsset.savedCacheMesh;
            return true;
        }
        mesh = SharedMesh;
        return mesh != null;
    }

    public SkinnedMeshLoopSplitBounds GetSplitBounds(string loopId)
    {
        if (splitBoundsBindings == null || string.IsNullOrEmpty(loopId))
            return null;
        for (int i = 0; i < splitBoundsBindings.Count; i++)
        {
            var b = splitBoundsBindings[i];
            if (b != null && b.loopId == loopId)
                return b.bounds;
        }
        return null;
    }

    public void SetSplitBounds(string loopId, SkinnedMeshLoopSplitBounds bounds)
    {
        if (string.IsNullOrEmpty(loopId))
            return;
        if (splitBoundsBindings == null)
            splitBoundsBindings = new List<LoopSplitBoundsBinding>();
        for (int i = 0; i < splitBoundsBindings.Count; i++)
        {
            var b = splitBoundsBindings[i];
            if (b == null || b.loopId != loopId)
                continue;
            b.bounds = bounds;
            return;
        }
        splitBoundsBindings.Add(new LoopSplitBoundsBinding { loopId = loopId, bounds = bounds });
    }
}

[Serializable]
public sealed class LoopSplitBoundsBinding
{
    public string loopId;
    public SkinnedMeshLoopSplitBounds bounds;
}
