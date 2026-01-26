using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Optional curve-based nailbed mesh for the caboose digit.
    /// Generates a simple ribbon mesh along local +Z.
    /// </summary>
    [ExecuteAlways]
    public sealed class RagdollNailbed : MonoBehaviour
    {
        [Tooltip("Width profile along the nail (0..1 t). Output is multiplied by baseWidth.")]
        public AnimationCurve widthProfile = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

        public float length = 0.03f;
        public float baseWidth = 0.01f;
        public float thickness = 0.0015f;
        [Range(2, 64)] public int segments = 12;

        public Material material;

        private MeshFilter mf;
        private MeshRenderer mr;
        private Mesh mesh;
        private bool needsRebuild = false;

        private void OnEnable()
        {
            EnsureComponents();
            RebuildMesh();
        }

        private void OnValidate()
        {
            length = Mathf.Max(0.0001f, length);
            baseWidth = Mathf.Max(0.0001f, baseWidth);
            thickness = Mathf.Max(0f, thickness);
            segments = Mathf.Clamp(segments, 2, 128);
            EnsureComponents();
            // Mark for rebuild but don't rebuild during OnValidate
            needsRebuild = true;
        }

        private void Update()
        {
            // Rebuild mesh after OnValidate completes
            if (needsRebuild)
            {
                needsRebuild = false;
                RebuildMesh();
            }
        }

        private void EnsureComponents()
        {
            mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            if (material != null) mr.sharedMaterial = material;
        }

        public void RebuildMesh()
        {
            if (mf == null) EnsureComponents();
            if (mesh == null)
            {
                mesh = new Mesh { name = "NailbedMesh" };
                mesh.MarkDynamic();
            }

            int vCount = (segments + 1) * 4; // top+bottom, left+right
            var verts = new Vector3[vCount];
            var uvs = new Vector2[vCount];

            int triCount = segments * 6 * 2; // top and bottom ribbons (no sides)
            var tris = new int[triCount];

            int vi = 0;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float w = baseWidth * Mathf.Max(0f, widthProfile.Evaluate(t));
                float z = t * length;

                // top surface
                verts[vi + 0] = new Vector3(-w * 0.5f, thickness * 0.5f, z);
                verts[vi + 1] = new Vector3(+w * 0.5f, thickness * 0.5f, z);
                // bottom surface
                verts[vi + 2] = new Vector3(-w * 0.5f, -thickness * 0.5f, z);
                verts[vi + 3] = new Vector3(+w * 0.5f, -thickness * 0.5f, z);

                uvs[vi + 0] = new Vector2(0f, t);
                uvs[vi + 1] = new Vector2(1f, t);
                uvs[vi + 2] = new Vector2(0f, t);
                uvs[vi + 3] = new Vector2(1f, t);

                vi += 4;
            }

            int ti = 0;
            for (int i = 0; i < segments; i++)
            {
                int baseV = i * 4;
                int nextV = (i + 1) * 4;

                // top quad (baseV+0, baseV+1, nextV+0, nextV+1)
                tris[ti++] = baseV + 0;
                tris[ti++] = nextV + 0;
                tris[ti++] = nextV + 1;
                tris[ti++] = baseV + 0;
                tris[ti++] = nextV + 1;
                tris[ti++] = baseV + 1;

                // bottom quad (flip winding)
                tris[ti++] = baseV + 2;
                tris[ti++] = nextV + 3;
                tris[ti++] = nextV + 2;
                tris[ti++] = baseV + 2;
                tris[ti++] = baseV + 3;
                tris[ti++] = nextV + 3;
            }

            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }
    }
}
