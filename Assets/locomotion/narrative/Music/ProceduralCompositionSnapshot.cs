using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [Serializable]
    public sealed class ProceduralCompositionSnapshot
    {
        public string laneId;
        public List<MusicBehaviorNode> nodes = new List<MusicBehaviorNode>();
        public List<MusicCompositionOverlayEdge> baselineEdges = new List<MusicCompositionOverlayEdge>();

        public static ProceduralCompositionSnapshot FromPlan(MusicSectionPlan plan, string laneId)
        {
            var snap = new ProceduralCompositionSnapshot { laneId = laneId };
            if (plan?.sectionIdsUsed == null) return snap;

            int bar = 0;
            for (int i = 0; i < plan.sectionIdsUsed.Count; i++)
            {
                string id = plan.sectionIdsUsed[i];
                var node = new MusicBehaviorNode
                {
                    nodeId = $"{laneId}_{i}",
                    sectionId = id,
                    barStart = bar,
                    barEnd = bar + 4
                };
                bar += 4;
                snap.nodes.Add(node);
                if (i > 0)
                {
                    snap.baselineEdges.Add(new MusicCompositionOverlayEdge
                    {
                        fromNodeId = snap.nodes[i - 1].nodeId,
                        toNodeId = node.nodeId,
                        kind = MusicOverlayEdgeKind.Forward
                    });
                }
            }
            return snap;
        }

        public bool TryGetNode(string nodeId, out MusicBehaviorNode node)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeId == nodeId)
                {
                    node = nodes[i];
                    return true;
                }
            }
            node = null;
            return false;
        }
    }

    public sealed class MusicCompositionResetResult
    {
        public List<string> resetNodeIds = new List<string>();
        public List<MusicCompositionOverlayEdge> removedEdges = new List<MusicCompositionOverlayEdge>();
    }

    /// <summary>BFS-min reset along green overlay dependencies.</summary>
    public static class MusicCompositionReset
    {
        public static MusicCompositionResetResult Apply(
            MusicCompositionPlanAsset plan,
            IEnumerable<string> selectedNodeIds,
            bool downstreamOnly)
        {
            var result = new MusicCompositionResetResult();
            if (plan == null || selectedNodeIds == null || plan.proceduralSnapshot == null)
                return result;

            var seeds = new HashSet<string>();
            foreach (string id in selectedNodeIds)
                if (!string.IsNullOrEmpty(id)) seeds.Add(id);

            if (seeds.Count == 0) return result;

            var greenIncoming = BuildGreenIncoming(plan);
            var visitDepth = new Dictionary<string, int>();
            var queue = new Queue<(string id, int depth)>();

            foreach (string seed in seeds)
            {
                queue.Enqueue((seed, 0));
                if (!visitDepth.ContainsKey(seed) || 0 < visitDepth[seed])
                    visitDepth[seed] = 0;
            }

            while (queue.Count > 0)
            {
                var (id, depth) = queue.Dequeue();

                if (!greenIncoming.TryGetValue(id, out List<string> parents))
                    continue;

                for (int i = 0; i < parents.Count; i++)
                {
                    string p = parents[i];
                    int nextDepth = depth + 1;
                    if (downstreamOnly && !seeds.Contains(p))
                        continue;
                    if (!visitDepth.ContainsKey(p) || nextDepth < visitDepth[p])
                    {
                        visitDepth[p] = nextDepth;
                        queue.Enqueue((p, nextDepth));
                    }
                }
            }

            var resetOrder = new List<(string id, int depth)>();
            foreach (var kvp in visitDepth)
                resetOrder.Add((kvp.Key, kvp.Value));
            resetOrder.Sort((a, b) => a.depth.CompareTo(b.depth));

            for (int i = 0; i < resetOrder.Count; i++)
            {
                string nodeId = resetOrder[i].id;
                if (!plan.proceduralSnapshot.TryGetNode(nodeId, out MusicBehaviorNode procNode))
                    continue;

                MusicBehaviorNode live = plan.FindNode(nodeId);
                if (live == null)
                {
                    live = new MusicBehaviorNode { nodeId = nodeId };
                    plan.nodes.Add(live);
                }

                live.sectionId = procNode.sectionId;
                live.barStart = procNode.barStart;
                live.barEnd = procNode.barEnd;
                live.enterCut = procNode.enterCut;
                live.exitCut = procNode.exitCut;
                result.resetNodeIds.Add(nodeId);
            }

            for (int i = plan.overlayEdges.Count - 1; i >= 0; i--)
            {
                MusicCompositionOverlayEdge e = plan.overlayEdges[i];
                if (result.resetNodeIds.Contains(e.fromNodeId) || result.resetNodeIds.Contains(e.toNodeId))
                {
                    result.removedEdges.Add(e);
                    plan.overlayEdges.RemoveAt(i);
                }
            }

            return result;
        }

        static Dictionary<string, List<string>> BuildGreenIncoming(MusicCompositionPlanAsset plan)
        {
            var map = new Dictionary<string, List<string>>();
            for (int i = 0; i < plan.overlayEdges.Count; i++)
            {
                MusicCompositionOverlayEdge e = plan.overlayEdges[i];
                if (!map.TryGetValue(e.toNodeId, out List<string> list))
                {
                    list = new List<string>();
                    map[e.toNodeId] = list;
                }
                list.Add(e.fromNodeId);
            }
            return map;
        }
    }
}
