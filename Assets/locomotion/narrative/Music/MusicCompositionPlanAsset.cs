using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [Serializable]
    public sealed class MusicBehaviorNode
    {
        public string nodeId;
        public string sectionId;
        public int barStart;
        public int barEnd;
        public MusicPointCutMode exitCut = MusicPointCutMode.None;
        public MusicPointCutMode enterCut = MusicPointCutMode.None;
    }

    [Serializable]
    public sealed class MusicCompositionOverlayEdge
    {
        public string fromNodeId;
        public string toNodeId;
        public MusicOverlayEdgeKind kind = MusicOverlayEdgeKind.Forward;
        public MusicStemRole lane;
    }

    [CreateAssetMenu(fileName = "MusicCompositionPlan", menuName = "Locomotion/Narrative/Music Composition Plan", order = 13)]
    public sealed class MusicCompositionPlanAsset : ScriptableObject
    {
        public string causalityFromLeaf;
        public string causalityToLeaf;
        public List<MusicBehaviorNode> nodes = new List<MusicBehaviorNode>();
        public List<MusicCompositionOverlayEdge> overlayEdges = new List<MusicCompositionOverlayEdge>();
        public ProceduralCompositionSnapshot proceduralSnapshot;

        public string ResolveOverlayNextSectionId(string fromNodeId, MusicStemRole lane)
        {
            if (string.IsNullOrEmpty(fromNodeId)) return null;
            for (int i = 0; i < overlayEdges.Count; i++)
            {
                MusicCompositionOverlayEdge e = overlayEdges[i];
                if (e.fromNodeId != fromNodeId || e.lane != lane) continue;
                if (e.kind != MusicOverlayEdgeKind.Forward) continue;
                MusicBehaviorNode node = FindNode(e.toNodeId);
                if (node != null && !string.IsNullOrEmpty(node.sectionId))
                    return node.sectionId;
            }
            return null;
        }

        public MusicBehaviorNode FindNode(string nodeId)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeId == nodeId) return nodes[i];
            }
            return null;
        }

        public bool TryGetOverlayEdge(string fromId, string toId, out MusicCompositionOverlayEdge edge)
        {
            for (int i = 0; i < overlayEdges.Count; i++)
            {
                if (overlayEdges[i].fromNodeId == fromId && overlayEdges[i].toNodeId == toId)
                {
                    edge = overlayEdges[i];
                    return true;
                }
            }
            edge = null;
            return false;
        }

        public void AddOrReplaceOverlayEdge(MusicCompositionOverlayEdge edge)
        {
            for (int i = overlayEdges.Count - 1; i >= 0; i--)
            {
                if (overlayEdges[i].fromNodeId == edge.fromNodeId &&
                    overlayEdges[i].toNodeId == edge.toNodeId &&
                    overlayEdges[i].lane == edge.lane)
                {
                    overlayEdges.RemoveAt(i);
                }
            }
            overlayEdges.Add(edge);
        }

        public MusicCompositionResetResult ResetSelected(IEnumerable<string> selectedNodeIds, bool downstreamOnly)
        {
            return MusicCompositionReset.Apply(this, selectedNodeIds, downstreamOnly);
        }
    }
}
