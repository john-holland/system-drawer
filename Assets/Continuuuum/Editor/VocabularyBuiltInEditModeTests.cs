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
    public void VocabularyBuiltInRegistry_Includes_RoadLaneAndPhoneWireLemmas()
    {
        string[] terms =
        {
            "road-lane", "sidewalk", "phone-pole", "street-wire", "hanging-shoes",
            "jersey-barrier", "emergency-bar", "street-light", "traffic-signal"
        };
        foreach (var term in terms)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.AreEqual("phone-pole", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "phone", "pole" }));
        Assert.AreEqual("hanging-shoes", BuiltInSynonyms.CanonicalizeToken("hanging_shoes"));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_PenInkLemmas()
    {
        string[] nouns = { "pen", "quill", "nib", "ink", "cap", "paint", "towel", "whiteboard" };
        foreach (var term in nouns)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(
            VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", "write")));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(
            VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", "dip")));
        Assert.AreEqual("cap-open", BuiltInSynonyms.CanonicalizeToken("cap_open"));
        Assert.AreEqual("single-layer-mix", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "single", "layer", "mix" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_UniversityLemmas()
    {
        string[] nouns = { "campus", "curriculum", "headmaster", "dean", "dorm", "course-load", "age-bracket", "teacher", "assistant" };
        foreach (var term in nouns)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(
            VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", "enroll")));
        Assert.AreEqual("course-load", BuiltInSynonyms.CanonicalizeToken("course_load"));
        Assert.AreEqual("age-bracket", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "age", "bracket" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_ScribeLemmas()
    {
        string[] nouns = { "scribe-set", "page", "anchor", "format", "pecking-order", "scribe" };
        foreach (var term in nouns)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.AreEqual("scribe-set", BuiltInSynonyms.CanonicalizeToken("scribe_set"));
        Assert.AreEqual("pecking-order", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "pecking", "order" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_RelationshipLemmas()
    {
        string[] nouns = { "stage", "consent", "doctrine", "subjects", "affection", "romance" };
        foreach (var term in nouns)
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.AreEqual("stage", BuiltInSynonyms.CanonicalizeToken("relationship_stage"));
        Assert.AreEqual("stage", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "relationship", "stage" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_GenevaConventionLemmas()
    {
        foreach (var term in new[] { LegalLemmaPropertyKeys.GenevaConventions, LegalLemmaPropertyKeys.Torture, LegalLemmaPropertyKeys.RespectsGenevaConventions })
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", term);
            Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(id), term);
        }
        Assert.AreEqual("geneva-conventions", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "geneva", "conventions" }));
        Assert.AreEqual("respects-geneva-conventions", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "respects", "geneva", "conventions" }));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_AnnounceRightsReturnedLemmas()
    {
        string announce = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", LegalLemmaPropertyKeys.Announce);
        string returned = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", LegalLemmaPropertyKeys.Returned);
        string rightsReturned = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", LegalLemmaPropertyKeys.RightsReturned);
        string announceRights = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", LegalLemmaPropertyKeys.AnnounceRightsReturned);
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(announce));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(returned));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(rightsReturned));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(announceRights));
        Assert.AreEqual("rights-returned", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "rights", "returned" }));
        Assert.AreEqual("announce-rights-returned", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "announce", "rights", "returned" }));
        Assert.AreEqual("announce-rights-returned", BuiltInSynonyms.CanonicalizeToken("announce_rights_returned"));
        Assert.AreEqual("announce-rights-returned", BuiltInSynonyms.CanonicalizeToken("AnnounceRightsReturned"));
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("announce rights returned", out var hit));
        Assert.AreEqual("announce-rights-returned", hit.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("constitution rights returned", out var evt));
        Assert.AreEqual("announce-rights-returned", evt.Term);
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_StructuralChatOpenCloseLemmas()
    {
        Assert.AreEqual(VocabularyBuiltInIds.EnChat, VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "chat"));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(VocabularyBuiltInIds.EnChat));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(VocabularyBuiltInIds.EnOpenChat));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(VocabularyBuiltInIds.EnCloseChat));
        string wordBank = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "word-bank");
        string send = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", "send");
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(wordBank));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(send));
        var chat = VocabularyBuiltInRegistry.TryGetById(VocabularyBuiltInIds.EnChat).Value;
        Assert.IsTrue(System.Linq.Enumerable.Contains(chat.Tags, "structural-chat"));
        Assert.IsTrue(System.Linq.Enumerable.Contains(chat.Tags, "open-close"));
    }

    [Test]
    public void BuiltInSynonyms_OpenCloseChatPhrases_MapToCanonicalLemma()
    {
        Assert.AreEqual("open-chat", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "open", "chat" }));
        Assert.AreEqual("close-chat", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "close", "the", "chat" }));
        Assert.AreEqual("chat", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "chat", "window" }));
        Assert.AreEqual("word-bank", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "word", "bank" }));
    }

    [Test]
    public void BuiltInSynonyms_GameSessionPhrases_MapToCanonicalLemma()
    {
        Assert.AreEqual("game-session", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "game", "session" }));
        Assert.AreEqual("local-save", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "local", "save" }));
        Assert.AreEqual("save-server-to-local", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "save", "server", "to", "local" }));
        Assert.AreEqual("local-server", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "local", "server" }));
        Assert.AreEqual("local-save", BuiltInSynonyms.CanonicalizeToken("local_save"));
        Assert.AreEqual("game-session", BuiltInSynonyms.CanonicalizeToken("game_session"));
    }

    [Test]
    public void VocabularyBuiltInRegistry_Includes_VoteQueueLemmas()
    {
        foreach (var term in VoteLemmaPropertyKeys.LemmaPlaceholders)
            Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma(term, out _), term);
        string queued = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "verb", "queued");
        string home = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "home-address");
        string randomly = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "adv", "randomly");
        string ifSo = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "adv", "if-so");
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(queued));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(home));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(randomly));
        Assert.IsNotNull(VocabularyBuiltInRegistry.TryGetById(ifSo));
        Assert.AreEqual("home-address", BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "home", "address" }));
        Assert.IsNull(BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(new[] { "if", "so" }));
        Assert.AreEqual("if-so", BuiltInSynonyms.CanonicalizeToken("if_so"));
        Assert.AreEqual("home-address", BuiltInSynonyms.CanonicalizeToken("homeAddress"));
    }

    [Test]
    public void AdverbIfPostfix_GreedyIfSo_OnHappilyAndRandomly()
    {
        var happily = AdverbIfPostfix.Apply(new[] { "write", "happily", "if", "so" });
        CollectionAssert.AreEqual(new[] { "write", "happily-if-so" }, happily);
        var queued = AdverbIfPostfix.ApplyToText(VoteLemmaPropertyKeys.DefaultInpaintPrompt);
        CollectionAssert.AreEqual(new[] { "queued", "by", "address", "or", "randomly-if-so" }, queued);
        Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("happily-if-so", out var composed));
        Assert.AreEqual("happily-if-so", composed.Term);
        Assert.AreEqual("adverb", composed.PosTag);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("write happily, if so", out var write));
        Assert.AreEqual("write", write.Term);
        Assert.IsFalse(VocabularyBuiltInLookup.TryGetByLemma("write-happily-if-so", out _));
    }

    [Test]
    public void If_OperatorPositions_PrefixInfixPostfixCircumfix()
    {
        Assert.AreEqual(IfOperatorPosition.Prefix, IfPredicate.Classify(new[] { "if", "no", "home", "address" }, 0));
        Assert.AreEqual(IfOperatorPosition.Prefix, IfPredicate.Classify(new[] { "if", "so" }, 0));
        Assert.AreEqual(IfOperatorPosition.Prefix, IfPredicate.Classify(new[] { "or", "if", "queued" }, 1));
        Assert.AreEqual(IfOperatorPosition.Infix, IfPredicate.Classify(new[] { "happily", "if", "queued" }, 1));
        Assert.AreEqual(IfOperatorPosition.Infix, IfPredicate.Classify(new[] { "queued", "by", "address", "if", "home", "address" }, 3));
        Assert.AreEqual(IfOperatorPosition.Infix, IfPredicate.Classify(new[] { "randomly", "if", "no", "property" }, 1));
        Assert.AreEqual(IfOperatorPosition.Postfix, IfPredicate.Classify(new[] { "happily", "if", "so" }, 1));
        Assert.AreEqual(IfOperatorPosition.Postfix, IfPredicate.Classify(new[] { "queued", "if", "so" }, 1));
        Assert.AreEqual(IfOperatorPosition.Postfix, IfPredicate.Classify(new[] { "queued", "if" }, 1));
        Assert.AreEqual(IfOperatorPosition.Circumfix, IfPredicate.Classify(new[] { "if", "pause", "then", "forward" }, 0));
        Assert.AreEqual(IfOperatorPosition.Circumfix, IfPredicate.Classify(new[] { "write", "if", "pause", "then", "forward" }, 1));
        Assert.AreEqual(IfOperatorPosition.Circumfix, IfPredicate.Classify(new[] { "if", "so", "then", "queue" }, 0));
        Assert.AreEqual(IfOperatorPosition.Circumfix, IfPredicate.Classify(new[] { "happily", "if", "so", "then", "write" }, 1));

        Assert.IsTrue(AdverbIfPostfix.IsPrefixIf(new[] { "if", "no", "home", "address" }, 0));
        Assert.IsFalse(AdverbIfPostfix.IsPrefixIf(new[] { "happily", "if", "queued" }, 1));
        Assert.IsFalse(AdverbIfPostfix.IsPrefixIf(new[] { "happily", "if", "so" }, 1));

        CollectionAssert.AreEqual(
            new[] { "if", "so" },
            AdverbIfPostfix.ApplyToText("if so"));
        CollectionAssert.AreEqual(
            new[] { "if", "no", "home", "address", "property" },
            AdverbIfPostfix.ApplyToText("if no home address property"));
        CollectionAssert.AreEqual(
            new[] { "if", "pause", "then", "forward" },
            AdverbIfPostfix.ApplyToText("if pause then forward"));
        CollectionAssert.AreEqual(
            new[] { "write", "happily", "if", "queued" },
            AdverbIfPostfix.Apply(new[] { "write", "happily", "if", "queued" }));
        CollectionAssert.AreEqual(
            new[] { "queued", "by", "address", "if", "home", "address" },
            AdverbIfPostfix.ApplyToText("queued by address if home address"));
        CollectionAssert.AreEqual(
            new[] { "queued", "by", "address", "or", "randomly", "if", "no", "home", "address", "property" },
            AdverbIfPostfix.ApplyToText("queued by address, or randomly if no home address property"));
        CollectionAssert.AreEqual(
            new[] { "write", "happily-if-so" },
            AdverbIfPostfix.Apply(new[] { "write", "happily", "if", "so" }));
        CollectionAssert.AreEqual(
            new[] { "happily", "if", "so", "then", "write" },
            AdverbIfPostfix.Apply(new[] { "happily", "if", "so", "then", "write" }));

        var prefixHits = IfPredicate.FindAllInText("if no home address property");
        Assert.AreEqual(1, prefixHits.Length);
        Assert.AreEqual(IfOperatorPosition.Prefix, prefixHits[0].Position);
        var infixHits = IfPredicate.FindAllInText("queued by address if home address");
        Assert.AreEqual(1, infixHits.Length);
        Assert.AreEqual(IfOperatorPosition.Infix, infixHits[0].Position);
        var postfixHits = IfPredicate.FindAllInText("write happily, if so");
        Assert.AreEqual(1, postfixHits.Length);
        Assert.AreEqual(IfOperatorPosition.Postfix, postfixHits[0].Position);
        Assert.IsTrue(postfixHits[0].Composed);
        var circumHits = IfPredicate.FindAllInText("if pause then forward");
        Assert.AreEqual(1, circumHits.Length);
        Assert.AreEqual(IfOperatorPosition.Circumfix, circumHits[0].Position);

        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("if", out var bare));
        Assert.AreEqual("if", bare.Term);
        Assert.AreEqual("conjunction", bare.PosTag);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("if so", out var ifSoPrefix));
        Assert.AreEqual("if", ifSoPrefix.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("if no home address property", out var ifNo));
        Assert.AreEqual("if", ifNo.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("if pause then forward", out var ifPause));
        Assert.AreEqual("if", ifPause.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("if so, queue randomly", out var ifSoQueue));
        Assert.AreEqual("if", ifSoQueue.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("queued by address if home address", out var queued));
        Assert.AreEqual("queued", queued.Term);
        Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemmaExact("if-so", out var ifSoLemma));
        Assert.AreEqual("if-so", ifSoLemma.Term);
        Assert.AreEqual("adverb", ifSoLemma.PosTag);
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
