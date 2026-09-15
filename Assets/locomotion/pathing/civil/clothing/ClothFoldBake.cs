using UnityEngine;

/// <summary>One fold material + cached mesh pieces for BT. No live Unity Cloth.</summary>
public static class ClothFoldBake
{
    public const string FoldShaderName = "Locomotion/SuperDeformoSkew";

    public static Mesh BakeBoltRect(ClothBoltSpec bolt, int divX = 8, int divZ = 12)
    {
        if (bolt == null) return null;
        int nx = Mathf.Max(1, divX);
        int nz = Mathf.Max(1, divZ);
        float w = Mathf.Max(0.01f, bolt.widthM);
        float l = Mathf.Max(0.01f, bolt.lengthM);
        var mesh = new Mesh { name = bolt.name + "_fold" };
        var verts = new Vector3[(nx + 1) * (nz + 1)];
        var uvs = new Vector2[verts.Length];
        var tris = new int[nx * nz * 6];
        int vi = 0;
        for (int z = 0; z <= nz; z++)
        {
            for (int x = 0; x <= nx; x++)
            {
                float u = x / (float)nx;
                float v = z / (float)nz;
                verts[vi] = new Vector3(u * w, 0f, v * l);
                uvs[vi] = new Vector2(u * bolt.weaveUv.x, v * bolt.weaveUv.y);
                vi++;
            }
        }
        int ti = 0;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                int i = z * (nx + 1) + x;
                tris[ti++] = i;
                tris[ti++] = i + nx + 1;
                tris[ti++] = i + 1;
                tris[ti++] = i + 1;
                tris[ti++] = i + nx + 1;
                tris[ti++] = i + nx + 2;
            }
        }
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh InvertWinding(Mesh source)
    {
        if (source == null) return null;
        var mesh = Object.Instantiate(source);
        mesh.name = source.name + "_invert";
        var tris = mesh.triangles;
        for (int i = 0; i + 2 < tris.Length; i += 3)
        {
            int t = tris[i];
            tris[i] = tris[i + 2];
            tris[i + 2] = t;
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    public static bool ApplyToStep(TayloringStep step, ClothBoltSpec bolt)
    {
        if (step == null || bolt == null) return false;
        step.bakedMesh = BakeBoltRect(bolt);
        step.foldCacheId = bolt.name + "_" + step.kind;
        step.bakeComplete = step.bakedMesh != null && step.bakedMesh.vertexCount > 0;
        return step.bakeComplete;
    }

    public static Material CreateFoldMaterial()
    {
        var shader = Shader.Find(FoldShaderName) ?? Shader.Find("Standard");
        if (shader == null) return null;
        return new Material(shader) { name = "ClothFoldBake" };
    }
}
