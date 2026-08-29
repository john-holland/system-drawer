using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>SHA1 of mesh topology/attributes and texture bytes for loop-section cache checks.</summary>
public static class SkinnedMeshLoopHasher
{
    public static string HashMesh(Mesh mesh)
    {
        if (mesh == null)
            return "";
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            WriteVec3(w, mesh.vertices);
            WriteVec3(w, mesh.normals);
            WriteVec2(w, mesh.uv);
            int[] tris = mesh.triangles;
            w.Write(tris != null ? tris.Length : 0);
            if (tris != null)
            {
                for (int i = 0; i < tris.Length; i++)
                    w.Write(tris[i]);
            }
            w.Write(mesh.blendShapeCount);
            return HashBytes(ms.ToArray());
        }
    }

    public static string HashTexture(Texture2D tex)
    {
        if (tex == null)
            return "";
        if (tex.isReadable)
        {
            try
            {
                return HashBytes(tex.GetRawTextureData());
            }
            catch (Exception)
            {
                // fall through
                Debug.LogError("Failed to get raw texture data for " + tex.name);
            }
        }
        return HashBytes(System.Text.Encoding.UTF8.GetBytes(tex.name + ":" + tex.width + "x" + tex.height));
    }

    public static string HashBytes(byte[] data)
    {
        if (data == null || data.Length == 0)
            return "";
        using (var sha = SHA1.Create())
        {
            byte[] hash = sha.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static Texture2D[] CollectTextures(Renderer renderer)
    {
        if (renderer == null)
            return Array.Empty<Texture2D>();
        var mats = renderer.sharedMaterials;
        if (mats == null || mats.Length == 0)
            return Array.Empty<Texture2D>();
        var list = new System.Collections.Generic.List<Texture2D>();
        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            if (mat == null)
                continue;
            var tex = mat.mainTexture as Texture2D;
            if (tex != null && !list.Contains(tex))
                list.Add(tex);
        }
        return list.ToArray();
    }

    public static bool TexturesMatch(Texture2D[] live, string[] originalSha1s)
    {
        int n = originalSha1s != null ? originalSha1s.Length : 0;
        int m = live != null ? live.Length : 0;
        if (n != m)
            return n == 0 && m == 0;
        for (int i = 0; i < n; i++)
        {
            if (HashTexture(live[i]) != originalSha1s[i])
                return false;
        }
        return true;
    }

    static void WriteVec3(BinaryWriter w, Vector3[] arr)
    {
        w.Write(arr != null ? arr.Length : 0);
        if (arr == null)
            return;
        for (int i = 0; i < arr.Length; i++)
        {
            w.Write(arr[i].x);
            w.Write(arr[i].y);
            w.Write(arr[i].z);
        }
    }

    static void WriteVec2(BinaryWriter w, Vector2[] arr)
    {
        w.Write(arr != null ? arr.Length : 0);
        if (arr == null)
            return;
        for (int i = 0; i < arr.Length; i++)
        {
            w.Write(arr[i].x);
            w.Write(arr[i].y);
        }
    }
}
