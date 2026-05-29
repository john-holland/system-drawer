using SdfMax;
using UnityEngine;

namespace SpatialVolumes
{
    [DisallowMultipleComponent]
    public sealed class SdfMaxSolverController : MonoBehaviour
    {
        public SpatialVolumeProvider volumeProvider;
        public SdfMaxCompositionAsset composition;
        public SdfMaxSolverProfile profile;
        public MeshCollider sourceMeshCollider;
        public MeshFilter sourceMesh;

        [Tooltip("Mirrors SpatialVolumeProvider.SyncSDFTreeShape when provider is assigned.")]
        public bool SyncSDFTreeShape = true;

        void Awake()
        {
            EnsureProvider();
        }

        void OnValidate()
        {
            EnsureProvider();
            SyncToProvider();
        }

        public void EnsureProvider()
        {
            if (volumeProvider == null)
                volumeProvider = GetComponent<SpatialVolumeProvider>();
            if (volumeProvider == null)
                volumeProvider = gameObject.AddComponent<SpatialVolumeProvider>();
        }

        public void SyncToProvider()
        {
            if (volumeProvider == null)
                return;
            volumeProvider.backend = VolumeBackend.SdfMaxComposition;
            if (composition != null)
                volumeProvider.composition = composition;
            if (profile != null)
                volumeProvider.profile = profile;
            if (sourceMeshCollider != null)
                volumeProvider.meshCollider = sourceMeshCollider;
            volumeProvider.SyncSDFTreeShape = SyncSDFTreeShape;
        }

        public void RebuildIfDirty(bool force = false)
        {
            EnsureProvider();
            SyncToProvider();
            volumeProvider.RebuildIfDirty(force);
        }
    }
}
