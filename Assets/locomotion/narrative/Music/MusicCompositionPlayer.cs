using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [Serializable]
    public sealed class MusicCompositionStateMachine
    {
        public string machineId;
        public MusicStemRole lane;
        public List<MusicBehaviorNode> nodes = new List<MusicBehaviorNode>();
        public List<MusicCompositionOverlayEdge> proceduralEdges = new List<MusicCompositionOverlayEdge>();
        public List<MusicCompositionOverlayEdge> overlayEdges = new List<MusicCompositionOverlayEdge>();

        public MusicCompositionOverlayEdge ResolveTransition(string fromNodeId)
        {
            for (int i = 0; i < overlayEdges.Count; i++)
            {
                if (overlayEdges[i].fromNodeId == fromNodeId && overlayEdges[i].kind == MusicOverlayEdgeKind.Forward)
                    return overlayEdges[i];
            }
            for (int i = 0; i < proceduralEdges.Count; i++)
            {
                if (proceduralEdges[i].fromNodeId == fromNodeId)
                    return proceduralEdges[i];
            }
            return null;
        }
    }

    /// <summary>IEnumerator drivers for stem lanes with suspend/release point cuts.</summary>
    public sealed class MusicCompositionPlayer
    {
        readonly MusicSectionLibrary _library;
        readonly HashSet<string> _suspendPool = new HashSet<string>();
        readonly HashSet<string> _released = new HashSet<string>();

        public MusicCompositionPlayer(MusicSectionLibrary library)
        {
            _library = library;
        }

        public IEnumerator PlayMachine(MusicCompositionStateMachine machine, MusicPlaybackMixer mixer, float bpm)
        {
            if (machine == null || machine.nodes.Count == 0)
                yield break;

            string currentId = machine.nodes[0].nodeId;
            int safety = 256;

            while (!string.IsNullOrEmpty(currentId) && safety-- > 0)
            {
                MusicBehaviorNode node = FindNode(machine, currentId);
                if (node == null) yield break;
                if (_released.Contains(node.nodeId)) yield break;

                if (node.enterCut == MusicPointCutMode.SuspendForReturn && !_suspendPool.Contains(node.nodeId))
                    _suspendPool.Add(node.nodeId);

                yield return PlayNodeBars(node, mixer, bpm);

                if (node.exitCut == MusicPointCutMode.Release)
                {
                    _released.Add(node.nodeId);
                    _suspendPool.Remove(node.nodeId);
                    yield break;
                }

                if (node.exitCut == MusicPointCutMode.SuspendForReturn)
                {
                    _suspendPool.Add(node.nodeId);
                    yield return WaitForReturn(machine, node.nodeId);
                }

                MusicCompositionOverlayEdge edge = machine.ResolveTransition(node.nodeId);
                currentId = edge?.toNodeId;
            }
        }

        IEnumerator PlayNodeBars(MusicBehaviorNode node, MusicPlaybackMixer mixer, float bpm)
        {
            if (!_library.TryGet(node.sectionId, out MusicSectionAsset section))
                yield break;

            var slot = new MusicStemSlot
            {
                role = MusicStemRole.Background,
                sectionId = node.sectionId,
                clip = section.loopClip,
                volume = 0.7f
            };
            mixer.CrossfadeToSlots(new[] { slot });

            float barSec = 60f / Mathf.Max(section.bpm, 1f) * section.beatsPerBar;
            int bars = Mathf.Max(1, node.barEnd - node.barStart);
            float wait = barSec * bars;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                mixer.Tick(Time.deltaTime, bpm);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator WaitForReturn(MusicCompositionStateMachine machine, string suspendedId)
        {
            while (_suspendPool.Contains(suspendedId))
            {
                MusicCompositionOverlayEdge ret = FindReturnEdge(machine, suspendedId);
                if (ret != null)
                {
                    _suspendPool.Remove(suspendedId);
                    yield return null;
                    break;
                }
                yield return null;
            }
        }

        static MusicCompositionOverlayEdge FindReturnEdge(MusicCompositionStateMachine machine, string toNodeId)
        {
            for (int i = 0; i < machine.overlayEdges.Count; i++)
            {
                MusicCompositionOverlayEdge e = machine.overlayEdges[i];
                if (e.toNodeId == toNodeId && e.kind == MusicOverlayEdgeKind.Return)
                    return e;
            }
            return null;
        }

        static MusicBehaviorNode FindNode(MusicCompositionStateMachine machine, string nodeId)
        {
            for (int i = 0; i < machine.nodes.Count; i++)
            {
                if (machine.nodes[i].nodeId == nodeId) return machine.nodes[i];
            }
            return null;
        }
    }
}
