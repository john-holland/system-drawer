#if UNITY_EDITOR
using NUnit.Framework;

public sealed class LemmaBuildChatSessionTests
{
    const string AccompaniedAssistant = @"
Tier 0 is sufficient: accompanied links subject and object without C#.

```json lemma-mechanism-descriptor
{
  ""lemma"": ""accompanied"",
  ""posTag"": ""verb"",
  ""mechanicalRole"": ""ConnectorConjunction"",
  ""outputTier"": 0,
  ""functionalDescription"": ""Links subject to accompanying object."",
  ""mechanismPrompt"": """",
  ""synonyms"": [""escorted""],
  ""compositionChildren"": [
    { ""entryId"": ""urn:unity:continuuuum:builtin:v1:/en/noun/player"", ""sortOrder"": 0 },
    { ""entryId"": ""urn:unity:continuuuum:builtin:v1:/en/noun/object"", ""sortOrder"": 1 }
  ],
  ""properties"": [
    { ""propertyKey"": ""causality-tree"", ""propertyValue"": ""accompaniment"" }
  ]
}
```
";

    [Test]
    public void TryParseLastDescriptor_ParsesAccompaniedFence()
    {
        var session = new LemmaBuildChatSession();
        session.AppendAssistant(AccompaniedAssistant);

        Assert.IsTrue(session.TryParseLastDescriptor(out var descriptor));
        Assert.AreEqual("accompanied", descriptor.lemma);
        Assert.AreEqual("verb", descriptor.posTag);
        Assert.AreEqual("ConnectorConjunction", descriptor.mechanicalRole);
        Assert.AreEqual(0, descriptor.outputTier);
        Assert.AreEqual(2, descriptor.compositionChildren.Length);
    }

    [Test]
    public void DescriptorParser_ParsesFenceTag()
    {
        Assert.IsTrue(LemmaBuildDescriptorParser.TryParseFromAssistantText(AccompaniedAssistant, out var descriptor));
        Assert.IsTrue(LemmaBuildDescriptorParser.HasRequiredFields(descriptor));
    }

    [Test]
    public void BuiltinValidator_WarnsOnSubjectAlias()
    {
        var children = new[]
        {
            new LemmaCompositionChildPutDto { entryId = "subject", sortOrder = 0 }
        };
        var warnings = LemmaBuildBuiltinValidator.ValidateCompositionEntryIds(children);
        Assert.IsNotEmpty(warnings);
        StringAssert.Contains("player", warnings[0]);
    }

    [Test]
    public void SessionPaths_Slugify_NormalizesLemma()
    {
        Assert.AreEqual("coffee-cup", LemmaBuildSessionPaths.Slugify("Coffee Cup"));
        Assert.AreEqual("default", LemmaBuildSessionPaths.Slugify(""));
    }
}
#endif
