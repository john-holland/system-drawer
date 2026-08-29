#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SkinnedMeshLoopSplitBuilderTests
{
    [Test]
    public void AttachPieces_RootAndChildrenSharePickerAsset()
    {
        var mesh = new Mesh { name = "tiny" };
        mesh.vertices = new[]
        {
            Vector3.zero, Vector3.right, Vector3.up,
            Vector3.right + Vector3.up
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.splitMode = SkinnedMeshLoopSplitMode.NamedAssign;
        var loop = asset.AddLoop("A");
        loop.assignedTriangles = new List<int> { 0 };
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        var root = new GameObject("BuilderRoot");
        try
        {
            SkinnedMeshLoopSplitBuilder.AttachPieces(root, null, asset, pieces, null);
            var section = root.GetComponent<SkinnedMeshLoopSection>();
            Assert.IsNotNull(section);
            Assert.AreSame(asset, section.sectionAsset);
            var tags = root.GetComponentsInChildren<SkinnedMeshLoopSectionPiece>();
            Assert.Greater(tags.Length, 0);
            for (int i = 0; i < tags.Length; i++)
            {
                Assert.AreSame(asset, tags[i].sectionAsset);
                Assert.AreEqual(asset.splitMode, tags[i].splitMode);
            }
        }
        finally
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].mesh != null)
                    Object.DestroyImmediate(pieces[i].mesh);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void AttachPieces_MeshRendererSource_CreatesMeshFilterChildren()
    {
        var mesh = TwoSubmeshQuads();
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        SkinnedMeshLoopMaterialBreakout.ApplyToAsset(mesh, null, asset, new[] { 0, 1 });
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        var root = new GameObject("MrRoot");
        var child = new GameObject("Src");
        child.transform.SetParent(root.transform);
        var mf = child.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = child.AddComponent<MeshRenderer>();
        try
        {
            SkinnedMeshLoopSplitBuilder.AttachPieces(root, mr, asset, pieces, mr.sharedMaterials);
            var filters = root.GetComponentsInChildren<MeshFilter>();
            Assert.Greater(filters.Length, 1);
            Assert.IsNull(root.GetComponentInChildren<SkinnedMeshRenderer>());
            Assert.Greater(root.GetComponentsInChildren<MeshRenderer>().Length, 1);
        }
        finally
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].mesh != null)
                    Object.DestroyImmediate(pieces[i].mesh);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(asset);
        }
    }

    static Mesh TwoSubmeshQuads()
    {
        var mesh = new Mesh { name = "TwoSubmeshQuads" };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(2f, 0f, 0f),
            new Vector3(2f, 1f, 0f)
        };
        mesh.subMeshCount = 2;
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.SetTriangles(new[] { 1, 4, 5, 1, 5, 2 }, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif
