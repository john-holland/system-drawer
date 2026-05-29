using UnityEngine;

namespace SdfMax
{
    /// <summary>
    /// 1B: SkinnedMeshRenderer surface from SDF composition; bone motion drives GPU skinning and bone-aware SDF queries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SdfMaxSkinnedMeshSurface : MonoBehaviour
    {
        public Transform rootBone;
        public Transform[] bones = System.Array.Empty<Transform>();
        public SdfMaxSkinBindingAsset bindingAsset;
        public Material material;
        public Material[] materials;
        public bool generateBindPoseCollider;
        public float meshRebuildDebounceSeconds = 0.1f;

        SkinnedMeshRenderer _skinnedRenderer;
        Mesh _mesh;
        ISdfMaxVolumeHost _host;
        SdfMaxBoneFieldContext _boneContext = new SdfMaxBoneFieldContext();
        bool _meshDirty = true;
        float _lastMeshRebuildRequest = -999f;
        int _lastSurfaceVersion = int.MinValue;

        public SdfMaxBoneFieldContext BoneContext => _boneContext;

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

            if (_host.SyncSDFTreeShape && _host.NotifyOnTransformChange)
                PollBoneTransforms();

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

        void PollBoneTransforms()
        {
            bool any = false;
            if (rootBone != null && rootBone.hasChanged)
            {
                rootBone.hasChanged = false;
                any = true;
            }
            if (bones != null)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] != null && bones[i].hasChanged)
                    {
                        bones[i].hasChanged = false;
                        any = true;
                    }
                }
            }
            if (any)
                _host.NotifyVolumeChanged();
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

            ResolveBones();
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
            Bounds localBounds = WorldBoundsToLocal(eval.WorldBounds, _host.HostTransform);

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
                _mesh = new Mesh { name = "SdfMaxSkinnedSurfaceMesh" };
                _mesh.MarkDynamic();
            }

            if (!data.IsValid)
            {
                _mesh.Clear();
                _skinnedRenderer.sharedMesh = _mesh;
                _meshDirty = false;
                return;
            }

            ConvertVerticesToRootBoneSpace(data.Vertices, _host.HostTransform, rootBone);
            data.ApplyToMesh(_mesh, profile != null && profile.recalculateNormals);

            Matrix4x4[] bindposes;
            BoneWeight[] weights;
            if (bindingAsset != null && bindingAsset.boneWeights != null &&
                bindingAsset.boneWeights.Length == data.Vertices.Length)
            {
                bindposes = bindingAsset.bindposes;
                weights = bindingAsset.boneWeights;
                _boneContext.Bones = bindingAsset.bones;
            }
            else
            {
                SdfMaxSkinWeightBinder.GenerateWeights(data.Vertices, rootBone, bones, out bindposes, out weights);
                _boneContext.Bones = bones;
            }

            _mesh.bindposes = bindposes;
            _mesh.boneWeights = weights;
            _boneContext.RootBone = rootBone;
            _boneContext.Bindposes = bindposes;
            _boneContext.BindWeights = weights;
            _boneContext.BindVertices = _mesh.vertices;
            _boneContext.BindRootLocalToWorld = rootBone.localToWorldMatrix;

            _skinnedRenderer.sharedMesh = _mesh;
            _skinnedRenderer.rootBone = rootBone;
            _skinnedRenderer.bones = bones;

            if (generateBindPoseCollider)
                ApplyBindPoseCollider(profile);

            _host.EnsureVolumeBuilt(true);
            _meshDirty = false;
        }

        public void RegenerateSkinWeights()
        {
            if (_mesh == null || _mesh.vertexCount == 0)
            {
                RebuildSurfaceMesh();
                return;
            }
            ResolveBones();
            var verts = _mesh.vertices;
            SdfMaxSkinWeightBinder.GenerateWeights(verts, rootBone, bones, out var bindposes, out var weights);
            _mesh.bindposes = bindposes;
            _mesh.boneWeights = weights;
            _boneContext.Bindposes = bindposes;
            _boneContext.BindWeights = weights;
            _boneContext.BindVertices = verts;
        }

        void ResolveBones()
        {
            if (rootBone == null)
                rootBone = transform;
            if (bones == null || bones.Length == 0)
            {
                var found = GetComponentsInChildren<Transform>(true);
                if (found.Length > 1)
                {
                    bones = new Transform[found.Length - 1];
                    int j = 0;
                    for (int i = 0; i < found.Length; i++)
                    {
                        if (found[i] == transform)
                            continue;
                        bones[j++] = found[i];
                    }
                    if (j < bones.Length)
                        System.Array.Resize(ref bones, j);
                }
                else
                    bones = new[] { rootBone };
            }
        }

        void ApplyBindPoseCollider(SdfMaxSolverProfile profile)
        {
            var mc = _host.HostMeshCollider ?? GetComponent<MeshCollider>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = _mesh;
            mc.convex = profile != null && profile.convexCollider;
        }

        static void ConvertVerticesToRootBoneSpace(Vector3[] vertices, Transform host, Transform rootBone)
        {
            if (vertices == null || host == null || rootBone == null)
                return;
            Matrix4x4 hostToRoot = rootBone.worldToLocalMatrix * host.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = hostToRoot.MultiplyPoint3x4(vertices[i]);
        }

        static Bounds WorldBoundsToLocal(Bounds world, Transform t)
        {
            Vector3 c = t.InverseTransformPoint(world.center);
            Vector3 ext = t.InverseTransformVector(world.extents);
            return new Bounds(c, ext * 2f);
        }

        void EnsureComponents()
        {
            _skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
            if (_skinnedRenderer == null)
                _skinnedRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            ApplyMaterials();
        }

        void ApplyMaterials()
        {
            if (_skinnedRenderer == null)
                return;
            if (materials != null && materials.Length > 0)
                _skinnedRenderer.sharedMaterials = materials;
            else if (material != null)
                _skinnedRenderer.sharedMaterial = material;
        }
    }
}
