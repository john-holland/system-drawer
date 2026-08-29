using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored vertex loops plus original/cached mesh and texture hashes.</summary>
[CreateAssetMenu(menuName = "Locomotion/Mesh/Skinned Mesh Loop Section", fileName = "SkinnedMeshLoopSection")]
public sealed class SkinnedMeshLoopSectionAsset : ScriptableObject
{
    [Serializable]
    public sealed class LoopSection
    {
        public string id;
        public string displayName;
        public List<int> vertexIndices = new List<int>();
        public int seedTriangle = -1;
        public List<int> assignedTriangles = new List<int>();
        public string boneName;
        public string blendShapeNote;
        public int submeshIndex = -1;
        public int materialIndex = -1;
        [NonSerialized] public SkinnedMeshLoopSplitBounds splitBounds;
        public bool bespokeVertsExpanded = true;

        /// <summary>Bounds overlap plus bespoke vertexIndices, de-duplicated, overlap first.</summary>
        public List<int> CombinedVertexIndices(Vector3[] meshLocalVerts, Matrix4x4 meshLocalToWorld)
        {
            var list = new List<int>();
            var seen = new HashSet<int>();
            if (splitBounds != null && meshLocalVerts != null)
            {
                splitBounds.CollectOverlapping(meshLocalVerts, meshLocalToWorld, list);
                for (int i = 0; i < list.Count; i++)
                    seen.Add(list[i]);
            }
            if (vertexIndices == null)
                return list;
            for (int i = 0; i < vertexIndices.Count; i++)
            {
                int v = vertexIndices[i];
                if (seen.Add(v))
                    list.Add(v);
            }
            return list;
        }

        public bool RemoveBespokeVertexAt(int index)
        {
            if (vertexIndices == null || index < 0 || index >= vertexIndices.Count)
                return false;
            vertexIndices.RemoveAt(index);
            return true;
        }
    }

    public Mesh originalMesh;
    public Texture2D[] originalTextures = Array.Empty<Texture2D>();
    public string originalMeshSha1 = "";
    public string[] originalTextureSha1s = Array.Empty<string>();

    public Mesh savedCacheMesh;
    public Texture2D[] savedCacheTextures = Array.Empty<Texture2D>();
    public string savedCacheMeshSha1 = "";
    public string[] savedCacheTextureSha1s = Array.Empty<string>();

    public List<LoopSection> loops = new List<LoopSection>();
    public SkinnedMeshLoopSplitMode splitMode = SkinnedMeshLoopSplitMode.CutSeam;
    public float zoneRadius = 0.05f;
    public int lastPickedIndex = -1;
    public List<int> breakoutMaterialIndices = new List<int>();

    public LoopSection GetLoop(string id)
    {
        if (loops == null || string.IsNullOrEmpty(id))
            return null;
        for (int i = 0; i < loops.Count; i++)
            if (loops[i] != null && loops[i].id == id)
                return loops[i];
        return null;
    }

    public LoopSection AddLoop(string displayName = null)
    {
        if (loops == null)
            loops = new List<LoopSection>();
        var loop = new LoopSection
        {
            id = Guid.NewGuid().ToString("N"),
            displayName = string.IsNullOrEmpty(displayName) ? "Loop " + (loops.Count + 1) : displayName
        };
        loops.Add(loop);
        return loop;
    }

    public void CaptureOriginals(Mesh mesh, Texture2D[] textures)
    {
        originalMesh = mesh;
        originalMeshSha1 = SkinnedMeshLoopHasher.HashMesh(mesh);
        originalTextures = textures != null ? (Texture2D[])textures.Clone() : Array.Empty<Texture2D>();
        originalTextureSha1s = HashAll(originalTextures);
    }

    public void SnapshotSavedCache(Mesh mesh, Texture2D[] textures)
    {
        savedCacheMesh = mesh;
        savedCacheMeshSha1 = SkinnedMeshLoopHasher.HashMesh(mesh);
        savedCacheTextures = textures != null ? (Texture2D[])textures.Clone() : Array.Empty<Texture2D>();
        savedCacheTextureSha1s = HashAll(savedCacheTextures);
    }

    public void OverwriteOriginalsFromCacheOrLive(Mesh mesh, Texture2D[] textures)
    {
        CaptureOriginals(mesh, textures);
        savedCacheMesh = null;
        savedCacheTextures = Array.Empty<Texture2D>();
        savedCacheMeshSha1 = "";
        savedCacheTextureSha1s = Array.Empty<string>();
    }

    public bool LiveMatchesOriginal(Mesh liveMesh, Texture2D[] liveTextures)
    {
        if (string.IsNullOrEmpty(originalMeshSha1))
            return true;
        if (SkinnedMeshLoopHasher.HashMesh(liveMesh) != originalMeshSha1)
            return false;
        return SkinnedMeshLoopHasher.TexturesMatch(liveTextures, originalTextureSha1s);
    }

    static string[] HashAll(Texture2D[] textures)
    {
        if (textures == null || textures.Length == 0)
            return Array.Empty<string>();
        var sha = new string[textures.Length];
        for (int i = 0; i < textures.Length; i++)
            sha[i] = SkinnedMeshLoopHasher.HashTexture(textures[i]);
        return sha;
    }
}
