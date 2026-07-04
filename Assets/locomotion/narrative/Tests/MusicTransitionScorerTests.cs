using Locomotion.Narrative.Music;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Narrative.Tests
{
    public sealed class MusicTransitionScorerTests
    {
        [Test]
        public void Score_SameSection_IsZeroKeyCost()
        {
            var section = ScriptableObject.CreateInstance<MusicSectionAsset>();
            section.canonicalKey = "C";
            var scorer = new TransitionScorer();
            var bank = new ModulationSavingsBank();
            float cost = scorer.Score(section, section, null, null, bank);
            Assert.Less(cost, 0.01f);
        }

        [Test]
        public void PickBestNext_AvoidsRecentSection()
        {
            var lib = ScriptableObject.CreateInstance<MusicSectionLibrary>();
            var a = ScriptableObject.CreateInstance<MusicSectionAsset>();
            a.sectionId = "a";
            a.stemRole = MusicStemRole.Background;
            a.canonicalKey = "C";
            var b = ScriptableObject.CreateInstance<MusicSectionAsset>();
            b.sectionId = "b";
            b.stemRole = MusicStemRole.Background;
            b.canonicalKey = "G";
            lib.sections.Add(a);
            lib.sections.Add(b);

            var graph = new MusicSectionGraph();
            graph.AddNode(a);
            graph.AddNode(b);
            graph.AddEdge("a", "b", 1f);

            var scorer = new TransitionScorer();
            var bank = new ModulationSavingsBank();
            MusicSectionAsset pick = graph.PickBestNext(a, lib.sections, new[] { "b" }, null, null, scorer, bank);
            Assert.AreEqual("a", pick?.StableId);
        }
    }
}
