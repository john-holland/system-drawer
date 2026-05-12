using System.Collections.Generic;
using System.Linq;
using Locomotion.Narrative;
using Locomotion.Narrative.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Narrative.Tests
{
    /// <summary>Tier A–C: built-in vocabulary, tokenizer parity, ORM ordering, spatial hints, literal parsing.</summary>
    public class VocabularyBuiltInNarrativeIntegrationTests
    {
        private static readonly string[] TokenizerParityCorpus =
        {
            "the player walks near the door if pause then forward",
            "volume at center between north and south",
            "count:integer pos:vector3 flag:boolean name:string"
        };

        [Test]
        public void TierA_Registry_AllEntries_ValidUrnsAndEnglish()
        {
            foreach (var d in VocabularyBuiltInRegistry.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(d.Id), d.Term);
                Assert.AreEqual("en", d.LanguageCode);
                Assert.IsTrue(VocabularyLanguageEncoding.IsBuiltInUrn(d.Id), d.Id);
            }
            var ids = VocabularyBuiltInRegistry.All.Select(x => x.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }

        [Test]
        public void TierA_TokenizerParity_MatchesNarrativeLSTMTokenizer()
        {
            foreach (var sentence in TokenizerParityCorpus)
            {
                var a = VocabularyBuiltInTokenizer.TokenizeText(sentence);
                var b = NarrativeLSTMTokenizer.TokenizeText(sentence);
                CollectionAssert.AreEqual(a, b, sentence);
            }
        }

        [Test]
        public void TierA_SynonymNil_ResolvesToNullDescriptor()
        {
            Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("nil", out var d));
            Assert.AreEqual("null", d.Term);
        }

        [Test]
        public void TierA_LiteralParsing_CoversEveryLiteralType()
        {
            var types = new HashSet<string>();
            foreach (var d in VocabularyBuiltInRegistry.All)
                if (d.Category == VocabularyBuiltInCategory.LiteralType)
                    types.Add(d.Term);

            string text = string.Join(" ", types.Select(t => $"x_{t}:{t}"));
            var parsed = NarrativeVocabularyBuiltInSemantics.ParseTypedLiterals(text);
            foreach (var lt in types)
                Assert.IsTrue(parsed.Exists(p => p.typeLemma == lt), $"missing literal {lt}");
        }

        [Test]
        public void TierB_BuiltInBinding_Precedes_SceneObjectRegistry_OnOverlap()
        {
            var go = new GameObject("reg_vol");
            var reg = go.AddComponent<SceneObjectRegistry>();
            reg.Register("volume", go, false, new List<string> { "volume" });

            var events = new List<InterpretedEvent>
            {
                new InterpretedEvent { title = "volume", tMin = 100f, tMax = 200f }
            };

            var builtIns = NarrativeBuiltInBindingHelper.BuildBuiltInBindings(events);
            var bindings = new List<InterpretedEventBinding>();
            OrmFillService.FillFromRegistry(events, reg, null, bindings, builtIns);

            Assert.AreEqual(1, bindings.Count);
            Assert.AreEqual(BindingStatus.BuiltInLexeme, bindings[0].status);
            Assert.IsTrue(VocabularyLanguageEncoding.IsBuiltInUrn(bindings[0].builtInEntryId));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TierB_Phrase_PlayerNearDoor_ResolvesBuiltInTokens_Or_Orphans()
        {
            Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("player", out _));
            Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("near", out _));
            Assert.IsTrue(VocabularyBuiltInLookup.TryResolvePhrase("door", out _));
        }

        [Test]
        public void TierC_SpatialGatewayHints_Pause_WidensTimeWindow()
        {
            var list = new List<InterpretedEvent>
            {
                new InterpretedEvent { title = "pause region", tMin = 1000f, tMax = 2000f }
            };
            float widthBefore = list[0].tMax - list[0].tMin;
            NarrativeVocabularyBuiltInSemantics.ApplySpatialGatewayHints(list, 86400f * 7f);
            float widthAfter = list[0].tMax - list[0].tMin;
            Assert.GreaterOrEqual(widthAfter, widthBefore - 1f);
        }

        [Test]
        public void TierC_CalendarBounds_FromInterpretedEvent_MatchesSpatialNarrative()
        {
            var ev = new InterpretedEvent
            {
                title = "forward slice",
                center = Vector3.one * 2f,
                size = Vector3.one,
                tMin = 10f,
                tMax = 90f
            };
            var ne = new NarrativeCalendarEvent
            {
                title = ev.title,
                spatiotemporalVolume = new Bounds4(ev.center, ev.size, ev.tMin, ev.tMax)
            };
            Assert.IsNotNull(ne.spatiotemporalVolume);
            Assert.AreEqual(10f, ne.spatiotemporalVolume.Value.tMin, 0.001f);
            Assert.AreEqual(90f, ne.spatiotemporalVolume.Value.tMax, 0.001f);
            Assert.AreEqual(ev.center.x, ne.spatiotemporalVolume.Value.center.x, 0.001f);
        }

        [Test]
        public void TierA_FloatLiteral_IsDistinctRow()
        {
            Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("float", out var df));
            Assert.IsTrue(VocabularyBuiltInLookup.TryGetByLemma("number", out var dn));
            Assert.AreEqual(VocabularyBuiltInCategory.LiteralType, df.Category);
            Assert.AreNotEqual(df.Id, dn.Id);
        }
    }
}
