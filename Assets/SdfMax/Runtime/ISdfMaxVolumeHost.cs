using UnityEngine;

namespace SdfMax
{
    /// <summary>Implemented by <see cref="SpatialVolumes.SpatialVolumeProvider"/> to avoid SdfMax → HierarchicalPathFinding references.</summary>
    public interface ISdfMaxVolumeHost
    {
        SdfMaxCompositionAsset Composition { get; }
        SdfMaxSolverProfile Profile { get; }
        Transform HostTransform { get; }
        bool SyncSDFTreeShape { get; }
        bool NotifyOnTransformChange { get; }
        int SurfaceMeshVersion { get; }
        SdfMaxBoneFieldContext BoneFieldContext { get; }
        void EnsureVolumeBuilt(bool force);
        void NotifyVolumeChanged();
        MeshCollider HostMeshCollider { get; }
    }
}
