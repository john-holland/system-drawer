#if UNITY_EDITOR
using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// Unit tests for GeneratedResultEntry: assetDependencyKeys and constructor.
/// </summary>
public class GeneratedResultEntryTests
{
    [Test]
    public void Constructor_InitializesAssetDependencyKeysAsEmpty()
    {
        var entry = new GeneratedResultEntry("prompt", "path", null, "model");
        Assert.IsNotNull(entry.assetDependencyKeys);
        Assert.AreEqual(0, entry.assetDependencyKeys.Count);
    }

    [Test]
    public void AssetDependencyKeys_CanAddAndRetain()
    {
        var entry = new GeneratedResultEntry();
        entry.assetDependencyKeys.Add("key_albedo");
        entry.assetDependencyKeys.Add("key_normal");
        Assert.AreEqual(2, entry.assetDependencyKeys.Count);
        Assert.That(entry.assetDependencyKeys, Does.Contain("key_albedo"));
        Assert.That(entry.assetDependencyKeys, Does.Contain("key_normal"));
    }

    [Test]
    public void TimestampString_ZeroTicks_ReturnsEmpty()
    {
        var entry = new GeneratedResultEntry();
        entry.timestampTicks = 0;
        Assert.AreEqual("", entry.TimestampString);
    }

    [Test]
    public void Constructor_SetsPromptAndPath()
    {
        var entry = new GeneratedResultEntry("icy surface", "Assets/out.shader", null, "stub");
        Assert.AreEqual("icy surface", entry.prompt);
        Assert.AreEqual("Assets/out.shader", entry.generatedAssetPath);
        Assert.AreEqual("stub", entry.modelUsed);
        Assert.AreNotEqual(0, entry.timestampTicks);
    }
}
#endif
