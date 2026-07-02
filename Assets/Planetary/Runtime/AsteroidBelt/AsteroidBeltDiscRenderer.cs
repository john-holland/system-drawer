using UnityEngine;

namespace Planetary.AsteroidBelt
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class AsteroidBeltDiscRenderer : MonoBehaviour
    {
        public AsteroidBeltStatisticalManifold manifold;
        public Material discMaterial;
        public int sectorCount = 128;
        public int radialSegments = 4;

        MeshFilter _filter;
        MeshRenderer _renderer;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            _filter = GetComponent<MeshFilter>();
            _renderer = GetComponent<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
            if (manifold == null)
                manifold = GetComponent<AsteroidBeltStatisticalManifold>();
            RebuildMesh();
        }

        public void RebuildMesh()
        {
            if (manifold == null || _filter == null)
                return;
            _filter.sharedMesh = BuildAnnulusMesh(
                manifold.innerRadiusM,
                manifold.outerRadiusM,
                sectorCount,
                radialSegments);
        }

        public void SetOpacity(float opacity, float density)
        {
            if (_renderer == null)
                return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_Opacity", opacity);
            _mpb.SetFloat("_MeanDensity", density);
            if (manifold != null)
                _mpb.SetFloat("_DensityVariance", manifold.densityVariance);
            _renderer.SetPropertyBlock(_mpb);
            if (discMaterial != null)
                _renderer.sharedMaterial = discMaterial;
        }

        static Mesh BuildAnnulusMesh(float innerR, float outerR, int sectors, int radialSegs)
        {
            int vertCount = (sectors + 1) * (radialSegs + 1);
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var tris = new int[sectors * radialSegs * 6];
            int vi = 0;
            for (int r = 0; r <= radialSegs; r++)
            {
                float t = r / (float)radialSegs;
                float radius = Mathf.Lerp(innerR, outerR, t);
                for (int s = 0; s <= sectors; s++)
                {
                    float ang = s / (float)sectors * Mathf.PI * 2f;
                    verts[vi] = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                    uvs[vi] = new Vector2(s / (float)sectors, t);
                    vi++;
                }
            }
            int ti = 0;
            for (int r = 0; r < radialSegs; r++)
            for (int s = 0; s < sectors; s++)
            {
                int a = r * (sectors + 1) + s;
                int b = a + sectors + 1;
                tris[ti++] = a;
                tris[ti++] = b;
                tris[ti++] = a + 1;
                tris[ti++] = a + 1;
                tris[ti++] = b;
                tris[ti++] = b + 1;
            }
            var mesh = new Mesh { name = "AsteroidBeltDisc" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
