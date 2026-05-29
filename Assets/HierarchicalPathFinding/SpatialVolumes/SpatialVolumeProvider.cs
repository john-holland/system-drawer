using System;
using System.Collections.Generic;
using SdfMax;
using UnityEngine;

namespace SpatialVolumes
{
    [DisallowMultipleComponent]
    public sealed class SpatialVolumeProvider : MonoBehaviour, ISpatialVolumeQuery, ISdfMaxVolumeHost
    {
        public VolumeBackend backend = VolumeBackend.SdfMaxComposition;
        public MeshCollider meshCollider;
        public SdfMaxCompositionAsset composition;
        public SdfMaxSolverProfile profile;

        [Header("Render")]
        public SdfMaxRenderMode renderMode = SdfMaxRenderMode.None;

        [Header("Tree build")]
        public int maxDepth = 8;
        public float minLeafExtent = 0.1f;
        public int maxTrianglesPerLeaf = 24;

        [Header("Runtime shape sync")]
        public bool SyncSDFTreeShape = true;
        public bool notifyOnTransformChange = true;

        public int SurfaceMeshVersion => _surfaceMeshVersion;

        public static event Action<SpatialVolumeProvider> Changed;

        Matrix4x4 _lastLocalToWorld;
        int _surfaceMeshVersion;
        readonly List<SpatialVolumeLeaf> _scratchLeaves = new List<SpatialVolumeLeaf>(64);

        SdfMaxCompositionAsset ISdfMaxVolumeHost.Composition => composition;
        SdfMaxSolverProfile ISdfMaxVolumeHost.Profile => profile;
        Transform ISdfMaxVolumeHost.HostTransform => transform;
        bool ISdfMaxVolumeHost.SyncSDFTreeShape => SyncSDFTreeShape;
        bool ISdfMaxVolumeHost.NotifyOnTransformChange => notifyOnTransformChange;
        int ISdfMaxVolumeHost.SurfaceMeshVersion => _surfaceMeshVersion;
        MeshCollider ISdfMaxVolumeHost.HostMeshCollider => meshCollider;

        SdfMaxBoneFieldContext ISdfMaxVolumeHost.BoneFieldContext
        {
            get
            {
                var skinned = GetComponent<SdfMaxSkinnedMeshSurface>();
                return skinned != null ? skinned.BoneContext : null;
            }
        }

        void ISdfMaxVolumeHost.EnsureVolumeBuilt(bool force) => RebuildIfDirty(force);
        void ISdfMaxVolumeHost.NotifyVolumeChanged() => NotifyChanged();

        void Awake() => SyncRenderComponents();
        public void OnEnable() => NotifyChanged();
        public void OnDisable() => NotifyChanged();

        void OnValidate()
        {
            SyncColliderReference();
            WarnIfBothSurfaceRenderers();
            SyncRenderComponents();
            RefreshSurfaceMeshVersion();
            NotifyChanged();
        }

        void WarnIfBothSurfaceRenderers()
        {
            var a = GetComponent<SdfMaxMeshSurface>();
            var b = GetComponent<SdfMaxSkinnedMeshSurface>();
            if (a != null && a.enabled && b != null && b.enabled)
                Debug.LogWarning($"{name}: SdfMaxMeshSurface and SdfMaxSkinnedMeshSurface are both enabled; only one should be active.", this);
        }

        void Update()
        {
            if (!SyncSDFTreeShape || !notifyOnTransformChange)
                return;

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                OnTransformSyncDirty();
            }
        }

        void OnTransformSyncDirty()
        {
            Matrix4x4 current = transform.localToWorldMatrix;
            if (_lastLocalToWorld == current)
                return;
            _lastLocalToWorld = current;

            if (backend == VolumeBackend.MeshConvexTree)
                SpatialVolumeCacheRegistry.Invalidate(this);

            NotifyChanged();
        }

        public SpatialVolumeBuildContext CreateBuildContext()
        {
            return new SpatialVolumeBuildContext
            {
                ProviderTransform = transform,
                Backend = backend,
                MeshCollider = meshCollider,
                Composition = composition,
                Profile = profile,
                MaxDepth = maxDepth,
                MinLeafExtent = minLeafExtent,
                MaxTrianglesPerLeaf = maxTrianglesPerLeaf,
                SyncShape = SyncSDFTreeShape,
                LastLocalToWorld = transform.localToWorldMatrix,
                SurfaceMeshVersion = _surfaceMeshVersion,
                BoneFieldContext = ((ISdfMaxVolumeHost)this).BoneFieldContext
            };
        }

        public void RebuildIfDirty(bool force = false)
        {
            RefreshSurfaceMeshVersion();
            SpatialVolumeCacheRegistry.EnsureBuilt(this, force);
        }

        public void RefreshSurfaceMeshVersion()
        {
            _surfaceMeshVersion = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(profile, composition);
        }

        void SyncRenderComponents()
        {
            var staticMesh = GetComponent<SdfMaxMeshSurface>();
            var skinnedMesh = GetComponent<SdfMaxSkinnedMeshSurface>();

            switch (renderMode)
            {
                case SdfMaxRenderMode.StaticMesh:
                    if (skinnedMesh != null && skinnedMesh.enabled)
                        skinnedMesh.enabled = false;
                    if (staticMesh == null)
                        staticMesh = gameObject.AddComponent<SdfMaxMeshSurface>();
                    staticMesh.enabled = true;
                    break;
                case SdfMaxRenderMode.SkinnedMesh:
                    if (staticMesh != null && staticMesh.enabled)
                        staticMesh.enabled = false;
                    if (skinnedMesh == null)
                        skinnedMesh = gameObject.AddComponent<SdfMaxSkinnedMeshSurface>();
                    skinnedMesh.enabled = true;
                    break;
                default:
                    if (staticMesh != null)
                        staticMesh.enabled = false;
                    if (skinnedMesh != null)
                        skinnedMesh.enabled = false;
                    break;
            }
        }

        public bool TrySample(Vector3 worldPos, float t, out float fieldValue, out bool inside)
        {
            fieldValue = 1000f;
            inside = false;
            if (!SpatialVolumeCacheRegistry.TryGetBackend(this, out var be) || be == null)
                return false;
            fieldValue = be.Sample(worldPos, t);
            inside = be.IsInside(worldPos, t);
            return true;
        }

        public bool SearchLeaves(Bounds worldBounds, float t, List<SpatialVolumeLeaf> results)
        {
            if (results == null)
                return false;
            results.Clear();
            if (!SpatialVolumeCacheRegistry.TryGetBackend(this, out var be) || be == null)
                return false;
            be.CollectLeaves(worldBounds, t, results);
            return results.Count > 0;
        }

        public Bounds GetWorldBounds()
        {
            if (SpatialVolumeCacheRegistry.TryGetBackend(this, out var be) && be != null)
                return be.WorldBounds;
            return new Bounds(transform.position, Vector3.one);
        }

        public void ExportVolumeBounds(List<SpatialVolumeBounds4> outVolumes, float tMin, float tMax)
        {
            if (outVolumes == null)
                return;
            if (!SpatialVolumeCacheRegistry.TryGetBackend(this, out var be) || be == null)
                return;
            be.ExportVolumeBounds(outVolumes, tMin, tMax, transform.localToWorldMatrix);
        }

        void SyncColliderReference()
        {
            if (meshCollider == null)
                meshCollider = GetComponent<MeshCollider>();
        }

        public void NotifyChanged()
        {
            if (SyncSDFTreeShape)
                SpatialVolumeCacheRegistry.Invalidate(this);
            RefreshSurfaceMeshVersion();
            var meshSurface = GetComponent<SdfMaxMeshSurface>();
            if (meshSurface != null && meshSurface.enabled)
                meshSurface.MarkMeshDirty();
            var skinned = GetComponent<SdfMaxSkinnedMeshSurface>();
            if (skinned != null && skinned.enabled)
                skinned.MarkMeshDirty();
            Changed?.Invoke(this);
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Pick Backend")]
        void AutoPickBackend()
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null && meshCollider.convex && composition == null)
                backend = VolumeBackend.MeshConvexTree;
            else
                backend = VolumeBackend.SdfMaxComposition;
        }
#endif
    }
}
