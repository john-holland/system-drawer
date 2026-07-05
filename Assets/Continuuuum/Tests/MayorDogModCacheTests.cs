using System.IO;
using Continuuuum.Mods;
using NUnit.Framework;
using UnityEngine;

public class MayorDogModCacheTests
{
    string _origPersistent;

    [SetUp]
    public void SetUp()
    {
        _origPersistent = Application.persistentDataPath;
    }

    [Test]
    public void WriteRead_RoundTripsManifest()
    {
        var manifest = new MayorDogModManifest
        {
            schemaVersion = 1,
            cachedAt = "2026-01-01T00:00:00Z",
            userId = "tester",
            packages = new[] { new MayorDogModPackageDto { packageId = "pkg1", slug = "mod-a" } },
            lemmaOverrides = new[]
            {
                new MayorDogModOverrideDto { slotKey = "greet", overrideText = "Hi", priority = 0 },
            },
        };
        MayorDogModCache.Write(manifest);
        var loaded = MayorDogModCache.Read();
        Assert.NotNull(loaded);
        Assert.AreEqual("tester", loaded.userId);
        Assert.AreEqual(1, loaded.lemmaOverrides.Length);
        Assert.AreEqual("Hi", loaded.lemmaOverrides[0].overrideText);
    }

    [Test]
    public void Applicator_ResolvesModPlaceholder_ByPriority()
    {
        var manifest = new MayorDogModManifest
        {
            lemmaOverrides = new[]
            {
                new MayorDogModOverrideDto { slotKey = "x", overrideText = "low", priority = 0 },
                new MayorDogModOverrideDto { slotKey = "x", overrideText = "high", priority = 1 },
            },
        };
        MayorDogModApplicator.LoadFromManifest(manifest);
        var text = MayorDogModApplicator.ResolveModPlaceholders("Say {M:x} now");
        Assert.AreEqual("Say high now", text);
    }

    [Test]
    public void ApplyEpisodeOverrides_ReplacesSpan()
    {
        var manifest = new MayorDogModManifest
        {
            episodeOverrides = new[]
            {
                new MayorDogModOverrideDto
                {
                    draftEpisodeId = "ep1",
                    charStart = 6,
                    charEnd = 11,
                    overrideText = "brave",
                    priority = 0,
                },
            },
        };
        MayorDogModCache.SetCurrent(manifest);
        var outText = MayorDogModApplicator.ApplyEpisodeOverrides("hello world", "ep1");
        Assert.AreEqual("hello brave", outText);
    }
}
