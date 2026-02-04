#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unit tests for AssetDependencyGraph: Register, GetDependencies, GetFullAssetKeys.
/// </summary>
public class AssetDependencyGraphTests
{
    [Test]
    public void GetDependencies_EmptyGraph_ReturnsNull()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        Assert.IsNull(graph.GetDependencies("shader_key"));
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void Register_ThenGetDependencies_ReturnsSameList()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        var deps = new List<string> { "key_albedo", "key_normal" };
        graph.Register("shader_key", deps);
        var outDeps = graph.GetDependencies("shader_key");
        Assert.IsNotNull(outDeps);
        Assert.AreEqual(2, outDeps.Count);
        Assert.That(outDeps, Does.Contain("key_albedo"));
        Assert.That(outDeps, Does.Contain("key_normal"));
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void Register_WithNullDependencyKeys_StoresEmptyList()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        graph.Register("key", null);
        var outDeps = graph.GetDependencies("key");
        Assert.IsNotNull(outDeps);
        Assert.AreEqual(0, outDeps.Count);
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void Register_EmptyKey_DoesNotAddEntry()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        graph.Register("", new List<string> { "a" });
        Assert.IsNull(graph.GetDependencies(""));
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void Register_UpdateExistingKey_ReplacesDependencies()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        graph.Register("k", new List<string> { "old1" });
        graph.Register("k", new List<string> { "new1", "new2" });
        var outDeps = graph.GetDependencies("k");
        Assert.AreEqual(2, outDeps.Count);
        Assert.That(outDeps, Does.Contain("new1"));
        Assert.That(outDeps, Does.Contain("new2"));
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void GetDependencies_IsCaseInsensitive()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        graph.Register("ShaderKey", new List<string> { "d1" });
        Assert.IsNotNull(graph.GetDependencies("shaderkey"));
        Assert.AreEqual(1, graph.GetDependencies("shaderkey").Count);
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void GetFullAssetKeys_ExistingKey_ReturnsTrueAndKeys()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        graph.Register("mat", new List<string> { "tex1" });
        bool found = graph.GetFullAssetKeys("mat", out string shaderKey, out List<string> textureKeys);
        Assert.IsTrue(found);
        Assert.AreEqual("mat", shaderKey);
        Assert.IsNotNull(textureKeys);
        Assert.AreEqual(1, textureKeys.Count);
        Assert.AreEqual("tex1", textureKeys[0]);
        Object.DestroyImmediate(graph);
    }

    [Test]
    public void GetFullAssetKeys_MissingKey_ReturnsFalse()
    {
        var graph = ScriptableObject.CreateInstance<AssetDependencyGraph>();
        bool found = graph.GetFullAssetKeys("missing", out string shaderKey, out List<string> textureKeys);
        Assert.IsFalse(found);
        Assert.IsNull(textureKeys);
        Object.DestroyImmediate(graph);
    }
}
#endif
