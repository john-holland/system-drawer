using UnityEngine;

namespace SdfMax
{
    public sealed class SdfMaxSurfaceMeshData
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uvs;
        public int[] Triangles;
        public Bounds LocalBounds;
        public int BuildVersion;

        public bool IsValid => Vertices != null && Vertices.Length > 0 && Triangles != null && Triangles.Length >= 3;

        public void ApplyToMesh(Mesh mesh, bool recalculateNormals)
        {
            if (mesh == null || !IsValid)
                return;
            mesh.Clear();
            mesh.vertices = Vertices;
            mesh.triangles = Triangles;
            if (Uvs != null && Uvs.Length == Vertices.Length)
                mesh.uv = Uvs;
            if (Normals != null && Normals.Length == Vertices.Length && !recalculateNormals)
                mesh.normals = Normals;
            else if (recalculateNormals)
                mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
