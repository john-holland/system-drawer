#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SkinnedMeshLoopSectionTests
{
    [Test]
    public void HashMesh_EqualThenMismatch()
    {
        var a = CreateCylinder(4, 8);
        var b = CreateCylinder(4, 8);
        Assert.AreEqual(SkinnedMeshLoopHasher.HashMesh(a), SkinnedMeshLoopHasher.HashMesh(b));
        var verts = b.vertices;
        verts[0] += Vector3.up * 0.01f;
        b.vertices = verts;
        Assert.AreNotEqual(SkinnedMeshLoopHasher.HashMesh(a), SkinnedMeshLoopHasher.HashMesh(b));
        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void MeshUpdated_DefaultFalse_MismatchFlipsTrue()
    {
        var mesh = CreateCylinder(3, 6);
        var go = new GameObject("LoopSection");
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.CaptureOriginals(mesh, null);
        var section = go.AddComponent<SkinnedMeshLoopSection>();
        section.sectionAsset = asset;
        section.RefreshMeshUpdated();
        Assert.IsFalse(section.meshUpdated);
        Assert.IsFalse(section.CanSetUseCached);

        var verts = mesh.vertices;
        verts[1] += Vector3.right * 0.05f;
        mesh.vertices = verts;
        section.RefreshMeshUpdated();
        Assert.IsTrue(section.meshUpdated);
        Assert.IsTrue(section.CanSetUseCached);
        Assert.IsFalse(section.CanApplyLoop);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void UseCached_RequiresMeshUpdated_AndWritesSavedCache()
    {
        var mesh = CreateCylinder(3, 6);
        var go = new GameObject("LoopSection");
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.CaptureOriginals(mesh, null);
        string originalSha = asset.originalMeshSha1;
        var section = go.AddComponent<SkinnedMeshLoopSection>();
        section.sectionAsset = asset;
        section.ApplyUseCachedSnapshot();
        Assert.IsFalse(section.useCached);

        var verts = mesh.vertices;
        verts[2] += Vector3.forward * 0.02f;
        mesh.vertices = verts;
        section.RefreshMeshUpdated();
        Assert.IsTrue(section.meshUpdated);
        section.ApplyUseCachedSnapshot();
        Assert.IsTrue(section.useCached);
        Assert.IsTrue(section.CanApplyLoop);
        Assert.AreEqual(originalSha, asset.originalMeshSha1);
        Assert.IsFalse(string.IsNullOrEmpty(asset.savedCacheMeshSha1));
        Assert.AreNotEqual(asset.originalMeshSha1, asset.savedCacheMeshSha1);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void Overwrite_ResetsFlagsAndOriginalHash()
    {
        var mesh = CreateCylinder(3, 6);
        var go = new GameObject("LoopSection");
        var smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.CaptureOriginals(mesh, null);
        var section = go.AddComponent<SkinnedMeshLoopSection>();
        section.sectionAsset = asset;
        var verts = mesh.vertices;
        verts[0] += Vector3.up * 0.2f;
        mesh.vertices = verts;
        section.RefreshMeshUpdated();
        section.ApplyUseCachedSnapshot();
        string liveSha = SkinnedMeshLoopHasher.HashMesh(mesh);
        section.OverwriteAndUpdateSavedCache();
        Assert.IsFalse(section.meshUpdated);
        Assert.IsFalse(section.useCached);
        Assert.AreEqual(liveSha, asset.originalMeshSha1);
        Assert.IsTrue(string.IsNullOrEmpty(asset.savedCacheMeshSha1));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void CycleDebounce_FreezesUntilMoveOrTimeout()
    {
        var d = new SkinnedMeshLoopCycleDebounce();
        d.Begin(Vector2.zero, 0);
        Assert.IsTrue(d.ShouldFreezeHover(new Vector2(1f, 1f), 0.1));
        Assert.IsFalse(d.ShouldFreezeHover(new Vector2(100f, 0f), 0.2));
        d.Begin(Vector2.zero, 10);
        Assert.IsFalse(d.ShouldFreezeHover(Vector2.zero, 10 + SkinnedMeshLoopCycleDebounce.TimeoutSeconds + 0.01));
    }

    [Test]
    public void ZoneHighlight_IncludesOneRingAndAveragesVertexColors()
    {
        var mesh = new Mesh();
        mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.colors = new[] { Color.red, Color.red, Color.red };
        var zone = new List<int>();
        SkinnedMeshLoopZoneHighlight.CollectZone(
            0, mesh.vertices, 0f, SkinnedMeshLoopEdgePath.BuildAdjacency(mesh), zone);
        Assert.AreEqual(3, zone.Count);
        Assert.IsTrue(zone.Contains(0));
        Assert.IsTrue(zone.Contains(1));
        Assert.IsTrue(zone.Contains(2));
        Color avg = SkinnedMeshLoopZoneHighlight.ZoneAverageAlbedo(
            zone, mesh.colors, null, null, Color.blue);
        Assert.AreEqual(1f, avg.r, 0.01f);
        Color complement = SkinnedMeshLoopZoneHighlight.ContrastComplement(Color.white);
        Assert.AreEqual(0f, complement.r, 0.01f);
        Assert.AreEqual(0.5f, SkinnedMeshLoopZoneHighlight.Blink01(0), 0.01f);
        Object.DestroyImmediate(mesh);
    }

    [Test]
    public void SplitBounds_CreateUnderMesh_AndCollectOverlapping()
    {
        var mesh = new Mesh { name = "boundsMesh" };
        mesh.vertices = new[]
        {
            Vector3.zero,
            Vector3.right * 2f,
            Vector3.up * 2f,
            new Vector3(0.1f, 0.1f, 0.1f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateBounds();
        var go = new GameObject("MeshForBounds");
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        try
        {
            var box = SkinnedMeshLoopSplitBounds.CreateUnderMesh(go.transform, mesh, "loop-a", "A");
            Assert.IsNotNull(box);
            Assert.AreEqual(go.transform, box.transform.parent);
            Assert.AreEqual("loop-a", box.loopId);
            Assert.AreEqual("A", box.loopName);
            Assert.AreEqual("SplitBounds_A", box.gameObject.name);
            Assert.AreSame(box, SkinnedMeshLoopSplitBounds.FindForLoop(go.transform, "loop-a"));
            Assert.AreSame(box, SkinnedMeshLoopSplitBounds.CreateUnderMesh(go.transform, mesh, "loop-a", "A"));

            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = Vector3.one;
            var hit = new List<int>();
            box.CollectOverlapping(mesh.vertices, go.transform.localToWorldMatrix, hit);
            Assert.IsTrue(hit.Contains(0));
            Assert.IsTrue(hit.Contains(3));
            Assert.IsFalse(hit.Contains(1));
            Assert.IsFalse(hit.Contains(2));
            Assert.IsTrue(box.ContainsWorldPoint(go.transform.TransformPoint(Vector3.zero)));
            Assert.IsFalse(box.ContainsWorldPoint(go.transform.TransformPoint(Vector3.right * 2f)));
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void SplitBounds_Associate_CombinedAndBespokeRemove()
    {
        var mesh = new Mesh { name = "boundsMesh2" };
        mesh.vertices = new[]
        {
            Vector3.zero,
            Vector3.right * 2f,
            Vector3.up * 2f,
            new Vector3(0.1f, 0.1f, 0.1f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateBounds();
        var go = new GameObject("MeshForBounds2");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        var loop = asset.AddLoop("Sleeve");
        var section = go.AddComponent<SkinnedMeshLoopSection>();
        section.sectionAsset = asset;
        try
        {
            var box = SkinnedMeshLoopSplitBounds.CreateUnderMesh(
                go.transform, mesh, loop.id, loop.displayName, asset, go);
            Assert.AreEqual("Sleeve", box.loopName);
            Assert.AreSame(asset, box.sectionAsset);
            Assert.AreSame(go, box.meshPrefab);

            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = Vector3.one;
            loop.splitBounds = box;
            loop.vertexIndices.Add(1);
            var combined = loop.CombinedVertexIndices(mesh.vertices, go.transform.localToWorldMatrix);
            Assert.IsTrue(combined.Contains(0));
            Assert.IsTrue(combined.Contains(3));
            Assert.IsTrue(combined.Contains(1));
            Assert.IsTrue(loop.RemoveBespokeVertexAt(0));
            Assert.AreEqual(0, loop.vertexIndices.Count);
            Assert.IsFalse(loop.RemoveBespokeVertexAt(0));

            section.SetSplitBounds(loop.id, box);
            Assert.AreSame(box, section.GetSplitBounds(loop.id));
        }
        finally
        {
            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void SplitBounds_ApplyOverlappingTriangles_UsesCurrentPose()
    {
        var mesh = new Mesh { name = "boundsTris" };
        mesh.vertices = new[]
        {
            Vector3.zero,
            new Vector3(0.1f, 0f, 0f),
            new Vector3(0f, 0.1f, 0f),
            Vector3.right * 2f,
            Vector3.right * 2f + Vector3.up * 0.1f,
            Vector3.right * 2f + Vector3.forward * 0.1f
        };
        mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
        mesh.RecalculateBounds();
        var go = new GameObject("MeshForBoundsTris");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        var loop = asset.AddLoop("Pocket");
        loop.assignedTriangles = new List<int> { 9 };
        loop.seedTriangle = 9;
        try
        {
            var box = SkinnedMeshLoopSplitBounds.CreateUnderMesh(
                go.transform, mesh, loop.id, loop.displayName, asset, go);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = Vector3.one;
            int n = box.ApplyOverlappingTriangles(
                loop, mesh.vertices, mesh.triangles, go.transform.localToWorldMatrix);
            Assert.AreEqual(1, n);
            Assert.AreEqual(1, loop.assignedTriangles.Count);
            Assert.AreEqual(0, loop.assignedTriangles[0]);
            Assert.AreEqual(0, loop.seedTriangle);

            box.transform.localPosition = Vector3.right * 2f;
            n = box.ApplyOverlappingTriangles(
                loop, mesh.vertices, mesh.triangles, go.transform.localToWorldMatrix);
            Assert.AreEqual(1, n);
            Assert.AreEqual(1, loop.assignedTriangles[0]);
            Assert.AreEqual(1, loop.seedTriangle);
        }
        finally
        {
            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void Renderer_ResolvesChildSkinnedMesh()
    {
        var root = new GameObject("LoopRoot");
        var child = new GameObject("MeshChild");
        child.transform.SetParent(root.transform);
        var smr = child.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = CreateCylinder(2, 4);
        var section = root.AddComponent<SkinnedMeshLoopSection>();
        try
        {
            Assert.AreSame(smr, section.Renderer);
        }
        finally
        {
            Object.DestroyImmediate(smr.sharedMesh);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CutSeam_TwoLoopsOnCylinder_ThreePieces_WithDuplicatedRims()
    {
        const int rings = 4;
        const int sides = 8;
        var mesh = CreateCylinder(rings, sides);
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.splitMode = SkinnedMeshLoopSplitMode.CutSeam;
        var loopA = asset.AddLoop("Ring1");
        loopA.vertexIndices = Ring(1, sides);
        var loopB = asset.AddLoop("Ring2");
        loopB.vertexIndices = Ring(2, sides);

        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        Assert.AreEqual(3, pieces.Count);
        int vertSum = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            Assert.IsNotNull(pieces[i].mesh);
            Assert.AreEqual(pieces[i].mesh.vertexCount, pieces[i].mesh.boneWeights.Length);
            vertSum += pieces[i].mesh.vertexCount;
            Object.DestroyImmediate(pieces[i].mesh);
        }
        Assert.AreEqual(mesh.vertexCount + sides * 2, vertSum);

        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void FloodInterior_SeedAndRemainder()
    {
        const int sides = 8;
        var mesh = CreateCylinder(4, sides);
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.splitMode = SkinnedMeshLoopSplitMode.FloodInterior;
        var loop = asset.AddLoop("Bottom");
        loop.vertexIndices = Ring(1, sides);
        loop.seedTriangle = 0;
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        Assert.AreEqual(2, pieces.Count);
        Assert.AreEqual("Piece_Bottom", pieces[0].name);
        Assert.AreEqual("Piece_Remainder", pieces[1].name);
        Assert.Greater(pieces[0].sourceTriangleIndices.Length, 0);
        Assert.Greater(pieces[1].sourceTriangleIndices.Length, 0);
        for (int i = 0; i < pieces.Count; i++)
            Object.DestroyImmediate(pieces[i].mesh);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void NamedAssign_TwoNamesAndRemainder()
    {
        var mesh = CreateCylinder(3, 6);
        int triCount = mesh.triangles.Length / 3;
        Assert.Greater(triCount, 6);
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.splitMode = SkinnedMeshLoopSplitMode.NamedAssign;
        var a = asset.AddLoop("A");
        a.assignedTriangles = new List<int> { 0, 1 };
        var b = asset.AddLoop("B");
        b.assignedTriangles = new List<int> { 2, 3 };
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        Assert.AreEqual(3, pieces.Count);
        Assert.AreEqual("Piece_A", pieces[0].name);
        Assert.AreEqual("Piece_B", pieces[1].name);
        Assert.AreEqual("Piece_Remainder", pieces[2].name);
        Assert.AreEqual(2, pieces[0].sourceTriangleIndices.Length);
        Assert.AreEqual(2, pieces[1].sourceTriangleIndices.Length);
        Assert.AreEqual(triCount - 4, pieces[2].sourceTriangleIndices.Length);
        for (int i = 0; i < pieces.Count; i++)
            Object.DestroyImmediate(pieces[i].mesh);
        Object.DestroyImmediate(mesh);
        Object.DestroyImmediate(asset);
    }

    [Test]
    public void AttachPieces_RootAndChildrenShareSectionAsset()
    {
        var mesh = CreateCylinder(3, 6);
        var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
        asset.splitMode = SkinnedMeshLoopSplitMode.NamedAssign;
        var loop = asset.AddLoop("A");
        loop.assignedTriangles = new List<int> { 0 };
        var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
        var root = new GameObject("SplitRoot");
        try
        {
            var section = root.AddComponent<SkinnedMeshLoopSection>();
            section.sectionAsset = asset;
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece == null || piece.mesh == null)
                    continue;
                var go = new GameObject(piece.name);
                go.transform.SetParent(root.transform, false);
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = piece.mesh;
                var tag = go.AddComponent<SkinnedMeshLoopSectionPiece>();
                tag.sectionAsset = asset;
                tag.loopIds = piece.loopIds;
                tag.splitMode = asset.splitMode;
            }
            Assert.AreSame(asset, section.sectionAsset);
            var tags = root.GetComponentsInChildren<SkinnedMeshLoopSectionPiece>();
            Assert.Greater(tags.Length, 0);
            for (int i = 0; i < tags.Length; i++)
                Assert.AreSame(asset, tags[i].sectionAsset);
        }
        finally
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i].mesh != null)
                    Object.DestroyImmediate(pieces[i].mesh);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void Renderer_ResolvesChildMeshRenderer()
    {
        var root = new GameObject("LoopRootMR");
        var child = new GameObject("MeshChild");
        child.transform.SetParent(root.transform);
        var mf = child.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateCylinder(2, 4);
        var mr = child.AddComponent<MeshRenderer>();
        var section = root.AddComponent<SkinnedMeshLoopSection>();
        try
        {
            Assert.AreSame(mr, section.Renderer);
            Assert.AreSame(mf.sharedMesh, section.SharedMesh);
            Assert.IsNull(section.SkinnedRenderer);
        }
        finally
        {
            Object.DestroyImmediate(mf.sharedMesh);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MaterialBreakout_TwoSubmeshes_TrianglesVerticesAndBoundaryLoops()
    {
        var mesh = CreateTwoSubmeshQuads();
        try
        {
            Assert.AreEqual(2, mesh.subMeshCount);
            var tris0 = SkinnedMeshLoopMaterialBreakout.TrianglesOfSubmesh(mesh, 0);
            var tris1 = SkinnedMeshLoopMaterialBreakout.TrianglesOfSubmesh(mesh, 1);
            Assert.AreEqual(2, tris0.Count);
            Assert.AreEqual(2, tris1.Count);
            Assert.AreEqual(4, SkinnedMeshLoopMaterialBreakout.VerticesOfSubmesh(mesh, 0).Count);
            var edges = SkinnedMeshLoopMaterialBreakout.BoundaryEdges(mesh, 0);
            Assert.Greater(edges.Count, 0);
            bool seam = false;
            for (int i = 0; i < edges.Count; i++)
            {
                int a = edges[i].Key;
                int b = edges[i].Value;
                if ((a == 1 && b == 2) || (a == 2 && b == 1))
                    seam = true;
            }
            Assert.IsTrue(seam);
            var loops = SkinnedMeshLoopMaterialBreakout.BoundaryLoops(mesh, 0);
            Assert.Greater(loops.Count, 0);
            Assert.GreaterOrEqual(loops[0].Count, 3);

            var go = new GameObject("MatBreak");
            var mr = go.AddComponent<MeshRenderer>();
            var asset = ScriptableObject.CreateInstance<SkinnedMeshLoopSectionAsset>();
            int n = SkinnedMeshLoopMaterialBreakout.ApplyToAsset(mesh, mr, asset, new[] { 0, 1 });
            Assert.AreEqual(2, n);
            Assert.AreEqual(SkinnedMeshLoopSplitMode.NamedAssign, asset.splitMode);
            Assert.AreEqual(2, asset.loops.Count);
            Assert.AreEqual(0, asset.loops[0].materialIndex);
            Assert.AreEqual(2, asset.loops[0].assignedTriangles.Count);

            var pieces = SkinnedMeshLoopSplitter.Split(mesh, asset);
            Assert.AreEqual(2, pieces.Count);
            Assert.AreEqual(0, pieces[0].sourceMaterialIndex);
            Assert.AreEqual(1, pieces[1].sourceMaterialIndex);
            for (int i = 0; i < pieces.Count; i++)
                Object.DestroyImmediate(pieces[i].mesh);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(asset);
        }
        finally
        {
            Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void MaterialBreakout_SplitSelected_OneMaterial()
    {
        var mesh = CreateTwoSubmeshQuads();
        var pieces = SkinnedMeshLoopMaterialBreakout.SplitSelected(mesh, null, new[] { 1 });
        try
        {
            Assert.AreEqual(1, pieces.Count);
            Assert.AreEqual(1, pieces[0].sourceMaterialIndex);
            Assert.AreEqual(2, pieces[0].sourceTriangleIndices.Length);
            Assert.IsNotNull(pieces[0].mesh);
        }
        finally
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i].mesh != null)
                    Object.DestroyImmediate(pieces[i].mesh);
            Object.DestroyImmediate(mesh);
        }
    }

    public static Mesh CreateTwoSubmeshQuads()
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

    public static Mesh CreateCylinder(int rings, int sides)
    {
        var verts = new Vector3[rings * sides];
        var uv = new Vector2[verts.Length];
        var norms = new Vector3[verts.Length];
        for (int r = 0; r < rings; r++)
        {
            float y = r / (float)(rings - 1);
            for (int s = 0; s < sides; s++)
            {
                float ang = s / (float)sides * Mathf.PI * 2f;
                int i = r * sides + s;
                verts[i] = new Vector3(Mathf.Cos(ang), y, Mathf.Sin(ang));
                uv[i] = new Vector2(s / (float)sides, y);
                norms[i] = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            }
        }
        var tris = new List<int>();
        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int n = (s + 1) % sides;
                int a = r * sides + s;
                int b = r * sides + n;
                int c = (r + 1) * sides + s;
                int d = (r + 1) * sides + n;
                tris.Add(a);
                tris.Add(c);
                tris.Add(b);
                tris.Add(b);
                tris.Add(c);
                tris.Add(d);
            }
        }
        var mesh = new Mesh { name = "LoopTestCylinder" };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.normals = norms;
        mesh.triangles = tris.ToArray();
        var bw = new BoneWeight[verts.Length];
        for (int i = 0; i < bw.Length; i++)
            bw[i].weight0 = 1f;
        mesh.boneWeights = bw;
        mesh.bindposes = new[] { Matrix4x4.identity };
        return mesh;
    }

    static List<int> Ring(int ring, int sides)
    {
        var list = new List<int>(sides);
        for (int s = 0; s < sides; s++)
            list.Add(ring * sides + s);
        return list;
    }
}
#endif
