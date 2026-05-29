using UnityEngine;

namespace SdfMax
{
    /// <summary>
    /// 1A: Static MeshFilter/MeshRenderer surface derived from SDF composition on an <see cref="ISdfMaxVolumeHost"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class SdfMaxMeshSurface : MonoBehaviour
    {
        public Material material;
        public Material[] materials;
        public float meshRebuildDebounceSeconds = 0.1f;

        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        ISdfMaxVolumeHost _host;
        bool _meshDirty = true;
        float _lastMeshRebuildRequest = -999f;
        int _lastSurfaceVersion = int.MinValue;
        int _lastVertexCount;

        void Awake() => EnsureComponents();

        void OnEnable()
        {
            EnsureComponents();
            _meshDirty = true;
        }

        void OnValidate()
        {
            EnsureComponents();
            ApplyMaterials();
            _meshDirty = true;
        }

        void Update()
        {
            _host = _host ?? GetComponent<ISdfMaxVolumeHost>();
            if (_host == null)
                return;

            int ver = _host.SurfaceMeshVersion;
            if (ver != _lastSurfaceVersion)
            {
                _lastSurfaceVersion = ver;
                MarkMeshDirty();
            }

            if (!_meshDirty)
                return;
            if (Time.time - _lastMeshRebuildRequest < meshRebuildDebounceSeconds)
                return;
            RebuildSurfaceMesh();
        }

        public void MarkMeshDirty()
        {
            _meshDirty = true;
            _lastMeshRebuildRequest = Time.time;
        }

        public void RebuildSurfaceMesh()
        {
            EnsureComponents();
            _host = GetComponent<ISdfMaxVolumeHost>();
            if (_host == null || _host.Composition == null)
                return;

            var profile = _host.Profile;
            if (profile != null && !profile.generateSurfaceMesh)
            {
                _meshDirty = false;
                return;
            }

            var graph = new SdfMaxExpressionGraph(
                _host.Composition,
                profile,
                _host.HostTransform.localToWorldMatrix);
            var eval = new SdfMaxEvaluator(graph);
            Bounds worldBounds = eval.WorldBounds;
            Bounds localBounds = WorldBoundsToLocal(worldBounds, _host.HostTransform);

            int buildVer = SdfMaxSurfaceMesher.ComputeSurfaceMeshVersion(profile, _host.Composition);
            var data = SdfMaxSurfaceMesher.Build(
                eval,
                localBounds,
                _host.HostTransform.localToWorldMatrix,
                profile != null ? profile.surfaceIsoLevel : 0f,
                profile != null ? profile.surfaceGridRes : 32,
                buildVer,
                profile == null || profile.recalculateNormals);

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "SdfMaxSurfaceMesh" };
                _mesh.MarkDynamic();
            }

            if (!data.IsValid)
            {
                _mesh.Clear();
                _lastVertexCount = 0;
            }
            else
            {
                data.ApplyToMesh(_mesh, profile != null && profile.recalculateNormals);
                _lastVertexCount = data.Vertices.Length;
            }

            _meshFilter.sharedMesh = _mesh;

            if (profile != null && profile.generateColliderMesh)
                ApplyColliderMesh(profile);

            _host.EnsureVolumeBuilt(true);
            _meshDirty = false;
        }

        void ApplyColliderMesh(SdfMaxSolverProfile profile)
        {
            var mc = _host.HostMeshCollider;
            if (mc == null)
                mc = GetComponent<MeshCollider>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = _mesh;
            mc.convex = profile.convexCollider;
        }

        static Bounds WorldBoundsToLocal(Bounds world, Transform t)
        {
            Vector3 c = t.InverseTransformPoint(world.center);
            Vector3 ext = t.InverseTransformVector(world.extents);
            return new Bounds(c, ext * 2f);
        }

        void EnsureComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            ApplyMaterials();
        }

        void ApplyMaterials()
        {
            if (_meshRenderer == null)
                return;
            if (materials != null && materials.Length > 0)
                _meshRenderer.sharedMaterials = materials;
            else if (material != null)
                _meshRenderer.sharedMaterial = material;
        }

        public int LastVertexCount => _lastVertexCount;
    }
}
