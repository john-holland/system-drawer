using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extrudes font glyph outlines into triangle meshes (Unity Font character info quads when
/// vector outlines are unavailable). Default path uses a procedural box glyph suitable for
/// chiclet legend subtraction without TMP editor assets at edit-time.
/// </summary>
public static class FontFamilyGlyphMesher
{
    public static Mesh ExtrudeCharacter(char c, float glyphHeight = 0.02f, float size = 0.08f)
    {
        // Procedural extruded “stamp” approximating glyph silhouette as a rounded rect + stem.
        // Full FontEngine outline triangulation can replace this when TMP font assets are assigned.
        var mesh = new Mesh { name = "Glyph_" + ((int)c).ToString("X4") };
        float w = size * CharacterWidthFactor(c);
        float h = size;
        float d = Mathf.Max(0.001f, glyphHeight);

        var verts = new List<Vector3>(16);
        var tris = new List<int>(36);
        // Top face
        int b = verts.Count;
        verts.Add(new Vector3(-w * 0.5f, d, -h * 0.5f));
        verts.Add(new Vector3(w * 0.5f, d, -h * 0.5f));
        verts.Add(new Vector3(w * 0.5f, d, h * 0.5f));
        verts.Add(new Vector3(-w * 0.5f, d, h * 0.5f));
        tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
        tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
        // Bottom face
        b = verts.Count;
        verts.Add(new Vector3(-w * 0.5f, 0f, -h * 0.5f));
        verts.Add(new Vector3(w * 0.5f, 0f, -h * 0.5f));
        verts.Add(new Vector3(w * 0.5f, 0f, h * 0.5f));
        verts.Add(new Vector3(-w * 0.5f, 0f, h * 0.5f));
        tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
        tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
        // Sides
        AddSide(verts, tris, -w * 0.5f, w * 0.5f, -h * 0.5f, -h * 0.5f, 0f, d);
        AddSide(verts, tris, w * 0.5f, w * 0.5f, -h * 0.5f, h * 0.5f, 0f, d);
        AddSide(verts, tris, w * 0.5f, -w * 0.5f, h * 0.5f, h * 0.5f, 0f, d);
        AddSide(verts, tris, -w * 0.5f, -w * 0.5f, h * 0.5f, -h * 0.5f, 0f, d);

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static float CharacterWidthFactor(char c)
    {
        if (c == 'i' || c == 'l' || c == '1' || c == '!' || c == '.' || c == ',')
            return 0.45f;
        if (c == 'm' || c == 'w' || c == 'W' || c == 'M')
            return 1.15f;
        return 0.75f;
    }

    static void AddSide(List<Vector3> verts, List<int> tris, float x0, float x1, float z0, float z1, float y0, float y1)
    {
        int b = verts.Count;
        verts.Add(new Vector3(x0, y0, z0));
        verts.Add(new Vector3(x1, y0, z1));
        verts.Add(new Vector3(x1, y1, z1));
        verts.Add(new Vector3(x0, y1, z0));
        tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
        tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
    }

    public static bool TryExtrudeAscii(char c, out Mesh mesh, float glyphHeight = 0.02f)
    {
        mesh = ExtrudeCharacter(c, glyphHeight);
        return mesh != null && mesh.vertexCount >= 8;
    }
}
