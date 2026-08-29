using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies split meshes / materials / textures and builds a prefab that still references the picker asset.
/// </summary>
public static class SkinnedMeshLoopSplitBuilder
{
    public const string DefaultFolder = "Assets/locomotion/Prefabs/SkinnedLoopPieces";

    public static GameObject AttachPieces(
        GameObject root,
        SkinnedMeshRenderer boneSource,
        SkinnedMeshLoopSectionAsset asset,
        IReadOnlyList<SkinnedMeshLoopSplitPiece> pieces,
        Material[] materials)
    {
        return AttachPieces(root, (Renderer)boneSource, asset, pieces, materials);
    }

    public static GameObject AttachPieces(
        GameObject root,
        Renderer source,
        SkinnedMeshLoopSectionAsset asset,
        IReadOnlyList<SkinnedMeshLoopSplitPiece> pieces,
        Material[] materials)
    {
        if (root == null)
            return null;
        var loop = root.GetComponent<SkinnedMeshLoopSection>();
        if (loop == null)
            loop = root.AddComponent<SkinnedMeshLoopSection>();
        loop.sectionAsset = asset;

        var skinned = source as SkinnedMeshRenderer;
        Transform parent = source != null ? source.transform : root.transform;
        Vector3 localPos = Vector3.zero;
        Quaternion localRot = Quaternion.identity;
        Vector3 localScale = Vector3.one;
        if (source != null)
        {
            localPos = source.transform.localPosition;
            localRot = source.transform.localRotation;
            localScale = source.transform.localScale;
            if (source.transform.parent != null)
                parent = source.transform.parent;
        }

        if (pieces == null)
            return root;
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            if (piece == null || piece.mesh == null)
                continue;
            var go = new GameObject(string.IsNullOrEmpty(piece.name) ? "Piece_" + i : piece.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            Material[] pieceMats = MaterialsForPiece(piece, materials, source);
            if (skinned != null)
            {
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = piece.mesh;
                smr.bones = skinned.bones;
                smr.rootBone = skinned.rootBone;
                if (pieceMats != null)
                    smr.sharedMaterials = pieceMats;
            }
            else
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = piece.mesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (pieceMats != null)
                    mr.sharedMaterials = pieceMats;
            }
            var tag = go.AddComponent<SkinnedMeshLoopSectionPiece>();
            tag.sectionAsset = asset;
            tag.loopIds = piece.loopIds;
            tag.splitMode = asset != null ? asset.splitMode : SkinnedMeshLoopSplitMode.CutSeam;
        }
        return root;
    }

    static Material[] MaterialsForPiece(SkinnedMeshLoopSplitPiece piece, Material[] copied, Renderer source)
    {
        Material[] src = copied;
        if (src == null && source != null)
            src = source.sharedMaterials;
        if (src == null || src.Length == 0)
            return null;
        int idx = piece != null ? piece.sourceMaterialIndex : -1;
        if (idx >= 0 && idx < src.Length)
            return new[] { src[idx] };
        return src;
    }

    public static string SavePrefab(
        Renderer source,
        SkinnedMeshLoopSection section,
        IReadOnlyList<SkinnedMeshLoopSplitPiece> pieces,
        string prefabPath,
        string textureFolder)
    {
        return SavePrefab(source, section != null ? section.sectionAsset : null, pieces, prefabPath, textureFolder);
    }

    public static string SavePrefab(
        SkinnedMeshRenderer source,
        SkinnedMeshLoopSectionAsset asset,
        IReadOnlyList<SkinnedMeshLoopSplitPiece> pieces,
        string prefabPath,
        string textureFolder)
    {
        return SavePrefab((Renderer)source, asset, pieces, prefabPath, textureFolder);
    }

    public static string SavePrefab(
        Renderer source,
        SkinnedMeshLoopSectionAsset asset,
        IReadOnlyList<SkinnedMeshLoopSplitPiece> pieces,
        string prefabPath,
        string textureFolder)
    {
        if (source == null || pieces == null || pieces.Count == 0)
            return null;

        GameObject sourceRoot = source.transform.root.gameObject;
        var clone = Object.Instantiate(sourceRoot);
        clone.name = sourceRoot.name + "_LoopSplit";

        var matched = FindMatchingRenderer(clone, source);
        if (matched == null)
            matched = clone.GetComponentInChildren<Renderer>();
        if (matched != null)
            matched.enabled = false;

        Material[] mats = CopyMaterialsAndTextures(source, textureFolder);
        AttachPieces(clone, matched, asset, pieces, mats);

        if (string.IsNullOrEmpty(prefabPath))
        {
            EnsureFolder(DefaultFolder);
            prefabPath = DefaultFolder + "/" + clone.name + ".prefab";
        }
        string dir = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir))
            EnsureFolder(dir);
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null || pieces[i].mesh == null)
                continue;
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(
                (dir ?? DefaultFolder) + "/" + pieces[i].mesh.name + ".asset");
            AssetDatabase.CreateAsset(pieces[i].mesh, meshPath);
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(clone, prefabPath, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefabPath;
    }

    public static Material[] CopyMaterialsAndTextures(Renderer source, string folder)
    {
        if (source == null)
            return null;
        var srcMats = source.sharedMaterials;
        if (srcMats == null || srcMats.Length == 0)
            return null;
        if (!string.IsNullOrEmpty(folder))
            EnsureFolder(folder);
        var copied = new Material[srcMats.Length];
        for (int i = 0; i < srcMats.Length; i++)
        {
            var src = srcMats[i];
            if (src == null)
                continue;
            var mat = new Material(src);
            mat.name = src.name + "_LoopPiece";
            if (!string.IsNullOrEmpty(folder))
                CopyMaterialTextures(src, mat, folder);
            if (!string.IsNullOrEmpty(folder))
            {
                string matPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + mat.name + ".mat");
                AssetDatabase.CreateAsset(mat, matPath);
                copied[i] = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
            else
                copied[i] = mat;
        }
        return copied;
    }

    static void CopyMaterialTextures(Material src, Material dst, string folder)
    {
        if (src == null || dst == null || string.IsNullOrEmpty(folder))
            return;
        string[] names = src.GetTexturePropertyNames();
        if (names == null)
            return;
        for (int i = 0; i < names.Length; i++)
        {
            string prop = names[i];
            var tex = src.GetTexture(prop) as Texture2D;
            if (tex == null)
                continue;
            var readable = MakeReadable(tex);
            if (readable == null)
                continue;
            string pngPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + tex.name + "_" + SanitizeFileToken(prop) + ".png");
            File.WriteAllBytes(pngPath, readable.EncodeToPNG());
            Object.DestroyImmediate(readable);
            AssetDatabase.ImportAsset(pngPath);
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (imported != null)
                dst.SetTexture(prop, imported);
        }
    }

    static string SanitizeFileToken(string prop)
    {
        if (string.IsNullOrEmpty(prop))
            return "tex";
        return prop.Replace("_", "").Replace(" ", "");
    }

    public static Texture2D MakeReadable(Texture2D src)
    {
        if (src == null)
            return null;
        if (src.isReadable)
        {
            var clone = Object.Instantiate(src);
            return clone;
        }
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    public static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder))
            return;
        assetFolder = assetFolder.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;
        string[] parts = assetFolder.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static Renderer FindMatchingRenderer(GameObject cloneRoot, Renderer source)
    {
        if (cloneRoot == null || source == null)
            return null;
        string path = GetPath(source.transform);
        if (!string.IsNullOrEmpty(path))
        {
            var t = cloneRoot.transform.Find(path);
            if (t != null)
            {
                var r = t.GetComponent<Renderer>();
                if (r != null)
                    return r;
            }
        }
        var all = cloneRoot.GetComponentsInChildren<Renderer>(true);
        return all != null && all.Length > 0 ? all[0] : null;
    }

    static string GetPath(Transform t)
    {
        if (t.parent == null)
            return "";
        var parts = new List<string>();
        while (t.parent != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
