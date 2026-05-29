using SdfMax;
using UnityEngine;

namespace SpatialVolumes
{
    public sealed class SpatialVolumeBuildContext
    {
        public Transform ProviderTransform;
        public VolumeBackend Backend;
        public MeshCollider MeshCollider;
        public SdfMaxCompositionAsset Composition;
        public SdfMaxSolverProfile Profile;
        public int MaxDepth = 8;
        public float MinLeafExtent = 0.1f;
        public int MaxTrianglesPerLeaf = 24;
        public bool SyncShape = true;
        public Matrix4x4 LastLocalToWorld;
        public int SurfaceMeshVersion;
        public SdfMaxBoneFieldContext BoneFieldContext;
    }
}
