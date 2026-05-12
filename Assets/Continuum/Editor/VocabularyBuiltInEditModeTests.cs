using System.Linq;
using NUnit.Framework;

public class VocabularyBuiltInEditModeTests
{
    [Test]
    public void AllBuiltInUrns_Unique()
    {
        var ids = VocabularyBuiltInRegistry.All.Select(d => d.Id).ToList();
        var distinct = ids.Distinct().Count();
        Assert.AreEqual(ids.Count, distinct, "Duplicate URN in registry");
    }

    [Test]
    public void EveryDescriptor_HasLanguageCode_And_PrefixedId()
    {
        foreach (var d in VocabularyBuiltInRegistry.All)
        {
            Assert.IsFalse(string.IsNullOrEmpty(d.LanguageCode), d.Id);
            Assert.AreEqual("en", d.LanguageCode);
            Assert.IsTrue(VocabularyLanguageEncoding.IsBuiltInUrn(d.Id), d.Id);
            StringAssert.Contains("/en/", d.Id);
        }
    }

    [Test]
    public void FormatBuiltInUrn_Matches_Registry_The()
    {
        string u = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "det", "the");
        Assert.AreEqual(VocabularyBuiltInIds.EnThe, u);
    }

    [Test]
    public void Registry_Includes_LiteralType_And_SpatialGateway()
    {
        var lit = VocabularyBuiltInRegistry.All.Count(d => d.Category == VocabularyBuiltInCategory.LiteralType);
        var gw = VocabularyBuiltInRegistry.All.Count(d => d.Category == VocabularyBuiltInCategory.SpatialGateway);
        Assert.GreaterOrEqual(lit, 10);
        Assert.GreaterOrEqual(gw, 6);
    }

    [Test]
    public void MergeApiEnrichment_ById_AddsDefinition_WithoutReplacingTerm()
    {
        var local = VocabularyBuiltInRegistrar.RegisterLocal();
        var api = new[]
        {
            new VocabularyApiThesaurusEntry
            {
                Id = VocabularyBuiltInIds.EnThe,
                Term = "WRONG", // should not replace built-in term
                PosTag = "WRONG",
                Definition = "def from API",
                Version = "1.1"
            }
        };
        var merged = VocabularyBuiltInRegistrar.MergeApiEnrichment(local, api);
        var e = merged[VocabularyBuiltInIds.EnThe];
        Assert.AreEqual("def from API", e.Definition);
        Assert.AreEqual("1.1", e.Version);
        Assert.AreEqual("the", e.Term);
        Assert.AreEqual("determiner", e.PosTag);
    }

    [Test]
    public void MergeApiEnrichment_ByTermPosLanguage_AddsDefinition()
    {
        var local = VocabularyBuiltInRegistrar.RegisterLocal();
        var api = new[]
        {
            new VocabularyApiThesaurusEntry
            {
                Id = null,
                Term = "if",
                PosTag = "conjunction",
                LanguageCode = "en",
                Definition = "conditional"
            }
        };
        var merged = VocabularyBuiltInRegistrar.MergeApiEnrichment(local, api);
        var ifId = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "conj", "if");
        Assert.IsTrue(merged.TryGetValue(ifId, out var e));
        Assert.AreEqual("conditional", e.Definition);
    }

    [Test]
    public void TryResolveLanguageId_UsesCallback()
    {
        bool ok = VocabularyLanguageEncoding.TryResolveLanguageId(
            c => c == "en" ? "lang-en-1" : null,
            "en",
            out var id);
        Assert.IsTrue(ok);
        Assert.AreEqual("lang-en-1", id);
    }
}
