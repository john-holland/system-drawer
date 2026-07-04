using System.Collections.Generic;
using Locomotion.Narrative.Music;
using NUnit.Framework;
using UnityEngine;

namespace Locomotion.Narrative.Tests
{
    public sealed class MusicCompositionResetTests
    {
        [Test]
        public void ResetSelected_RestoresProceduralNode_AndRemovesGreenEdges()
        {
            var plan = ScriptableObject.CreateInstance<MusicCompositionPlanAsset>();
            plan.proceduralSnapshot = new ProceduralCompositionSnapshot
            {
                nodes = new List<MusicBehaviorNode>
                {
                    new MusicBehaviorNode { nodeId = "a", sectionId = "proc_a", barStart = 0, barEnd = 4 },
                    new MusicBehaviorNode { nodeId = "b", sectionId = "proc_b", barStart = 4, barEnd = 8 }
                }
            };

            plan.nodes.Add(new MusicBehaviorNode { nodeId = "b", sectionId = "edited_b", barStart = 4, barEnd = 8 });
            plan.overlayEdges.Add(new MusicCompositionOverlayEdge
            {
                fromNodeId = "a",
                toNodeId = "b",
                kind = MusicOverlayEdgeKind.Forward
            });

            MusicCompositionResetResult result = plan.ResetSelected(new[] { "b" }, downstreamOnly: false);

            Assert.Contains("b", result.resetNodeIds);
            Assert.AreEqual("proc_b", plan.FindNode("b").sectionId);
            Assert.AreEqual(0, plan.overlayEdges.Count);
        }

        [Test]
        public void OverlayEdge_ReplacesProceduralTransition()
        {
            var plan = ScriptableObject.CreateInstance<MusicCompositionPlanAsset>();
            plan.nodes.Add(new MusicBehaviorNode { nodeId = "n1", sectionId = "s2" });
            plan.AddOrReplaceOverlayEdge(new MusicCompositionOverlayEdge
            {
                fromNodeId = "n0",
                toNodeId = "n1",
                kind = MusicOverlayEdgeKind.Forward,
                lane = MusicStemRole.Background
            });

            string next = plan.ResolveOverlayNextSectionId("n0", MusicStemRole.Background);
            Assert.AreEqual("s2", next);
        }
    }
}
