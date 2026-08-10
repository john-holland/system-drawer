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

    [Test]
    public void BuiltInSynonyms_MultiWord_FirstAndThirdPerson_MapToCanonicalLemma()
    {
        Assert.AreEqual("first-person", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "first", "person" }));
        Assert.AreEqual("third-person", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "Third", "Person" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_PlayerPerspectiveLemmas()
    {
        string fp = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "first-person");
        string tp = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "third-person");
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(fp));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(tp));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_CivilAndLifeSystemLemmas()
    {
        string factory = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "factory");
        string transitHub = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "transit-hub");
        string gasStation = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "gas-station");
        string lifeForce = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "life-force");
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(factory));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(transitHub));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(gasStation));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(lifeForce));
    }

    [Test]
    public void JsonExport_MatchesRegistryCount()
    {
        var path = VocabularyBuiltInJsonExporter.Export();
        Assert.IsTrue(System.IO.File.Exists(path), path);
        var json = System.IO.File.ReadAllText(path);
        var idCount = 0;
        var idx = 0;
        while ((idx = json.IndexOf("\"id\":", idx, System.StringComparison.Ordinal)) >= 0)
        {
            idCount++;
            idx += 5;
        }
        Assert.AreEqual(VocabularyBuiltInRegistry.Count, idCount);
    }

    [Test]
    public void NsmSemanticPrimes_AtLeast65TaggedPrime()
    {
        int primeTagged = VocabularyBuiltInRegistry.All.Count(d =>
            d.Tags != null && d.Tags.Any(t => string.Equals(t, "prime", System.StringComparison.OrdinalIgnoreCase)));
        Assert.GreaterOrEqual(primeTagged, 65, "Expected at least 65 prime-tagged builtins");
    }

    [Test]
    public void NsmSemanticPrimes_SemanticPrimeCategory_AtLeast56()
    {
        int sem = VocabularyBuiltInRegistry.All.Count(d => d.Category == VocabularyBuiltInCategory.SemanticPrime);
        Assert.GreaterOrEqual(sem, 56);
    }

    [Test]
    public void NsmSemanticPrimes_OverlapTerms_CarryPrimeTag()
    {
        string[] overlaps = { "this", "move", "when", "here", "near", "inside", "not", "because", "if" };
        foreach (var term in overlaps)
        {
            var hit = VocabularyBuiltInRegistry.All.FirstOrDefault(d =>
                string.Equals(d.Term, term, System.StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(string.IsNullOrEmpty(hit.Id), "missing overlap term " + term);
            Assert.IsTrue(
                hit.Tags != null && hit.Tags.Any(t => string.Equals(t, "prime", System.StringComparison.OrdinalIgnoreCase)),
                "overlap term missing prime tag: " + term);
        }
    }
}
