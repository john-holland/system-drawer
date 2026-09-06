using NUnit.Framework;
using UnityEngine;

public sealed class CustomRadialSideAndHostTests
{
    [Test]
    public void RecognizeAndResize_FromOpenQuad()
    {
        var mesh = new Mesh { name = "Quad" };
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var poses = CustomRadialSideRecognizer.Recognize(mesh);
        Assert.Greater(poses.Count, 0);
        var asset = ScriptableObject.CreateInstance<CustomRadialSideAsset>();
        CustomRadialSideRecognizer.AutoResize(asset, mesh);
        Assert.IsFalse(string.IsNullOrEmpty(asset.lastRecognizeHash));
        Assert.Greater(asset.jointMiddle.size.sqrMagnitude, 0f);
        var verts = asset.JointMiddleVertexIndices(mesh, Matrix4x4.identity);
        Assert.Greater(verts.Count, 0);
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(mesh);
    }

    [Test]
    public void CreateAnchorObjects_ParentsStartPost()
    {
        var go = new GameObject("host");
        try
        {
            var host = go.AddComponent<RadialBuildHost>();
            host.CreateAnchorObjects();
            Assert.IsNotNull(host.centerPost);
            Assert.IsNotNull(host.StartPostAnchor);
            Assert.IsNotNull(host.StartPostBounds);
            Assert.AreEqual(host.centerPost.transform, host.StartPostAnchor.parent);
            host.CreateAnchorObjects();
            Assert.AreEqual(1, CountNamed(host.centerPost.transform, RadialBuildHost.StartPostAnchorName));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PreviewLabels_OnlyMatchingWhenStartPostSet()
    {
        var go = new GameObject("host");
        try
        {
            var host = go.AddComponent<RadialBuildHost>();
            host.pieceSize = Vector3.one;
            host.spec = new RadialBuildSpec { joinKind = RadialJoinKind.Natural };
            host.CreateAnchorObjects();
            host.StartPostAnchor.position = new Vector3(10f, 0f, 10f);
            host.RefreshSolved();
            Assert.AreEqual(0, host.PreviewLabels().Length);

            float radius = RadialSlotMath.NaturalRadius(Vector3.one, 4, 360f, 0f);
            Vector3 slot0 = RadialSlotMath.PolarSlot(host.centerPost.transform.position, Vector3.up, radius, 0, 4, 0f, 360f);
            host.StartPostAnchor.position = slot0;
            host.RefreshSolved();
            Assert.Greater(host.PreviewLabels().Length, 0);
            for (int i = 0; i < host.solvedConfigs.Count; i++)
                Assert.IsTrue(host.solvedConfigs[i].matchesStartPostAnchor);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static int CountNamed(Transform t, string name)
    {
        int n = 0;
        for (int i = 0; i < t.childCount; i++)
            if (t.GetChild(i).name == name) n++;
        return n;
    }
}
