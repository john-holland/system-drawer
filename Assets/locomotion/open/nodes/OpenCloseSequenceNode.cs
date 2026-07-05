using System.Collections.Generic;
using Locomotion.Open.Nodes;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Hierarchical orchestrator for open/close topology beats.</summary>
    public sealed class OpenCloseSequenceNode : BehaviorTreeNode
    {
        public OpenCloseTopologyAsset topology;
        public OpenCloseLemmaProperties lemmaOverrides;
        public OpenCloseCameraSequence cameraSequence;
        public Transform actor;

        readonly Stack<OpenCloseTopologyNode> _closeStack = new Stack<OpenCloseTopologyNode>();
        readonly List<OpenCloseAmbulateToStopNode> _stopNodes = new List<OpenCloseAmbulateToStopNode>();
        int _nodeIndex;
        List<OpenCloseTopologyNode> _flatNodes = new List<OpenCloseTopologyNode>();
        bool _built;

        void Awake() => nodeType = NodeType.Sequence;

        public void RebuildFromTopology()
        {
            ClearBuiltNodes();
            _flatNodes.Clear();
            _closeStack.Clear();
            _stopNodes.Clear();
            _nodeIndex = 0;

            if (topology?.root == null)
            {
                _built = true;
                return;
            }

            foreach (var n in topology.EnumerateDepthFirst())
            {
                if (!n.enabledInGameplay)
                    continue;
                if (topology.linearOnly && !n.enabledInGameplay)
                    continue;
                _flatNodes.Add(n);
                if (n.autoCloseBt == AutoCloseBtMode.OnSequenceEnd)
                    _closeStack.Push(n);
                _stopNodes.Add(CreateAmbulateNode(n));
            }

            children.Clear();
            children.AddRange(_stopNodes);
            _built = true;
        }

        void ClearBuiltNodes()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(c.gameObject);
                else
                    DestroyImmediate(c.gameObject);
            }
        }

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (!_built)
                RebuildFromTopology();
            if (_flatNodes.Count == 0)
                return BehaviorTreeStatus.Success;

            if (_nodeIndex >= _flatNodes.Count)
                return DrainCloseStack(tree);

            var node = _flatNodes[_nodeIndex];
            var amb = _stopNodes[_nodeIndex];
            cameraSequence?.FocusStop(node, lemmaOverrides);

            var status = amb.Execute(tree);
            if (status == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
            if (status == BehaviorTreeStatus.Failure)
                return BehaviorTreeStatus.Failure;

            if (node.autoCloseBt == AutoCloseBtMode.AfterChildren)
            {
                var closeStatus = RunClose(tree, node);
                if (closeStatus == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
            }

            _nodeIndex++;
            if (_nodeIndex >= _flatNodes.Count)
                return DrainCloseStack(tree);
            return BehaviorTreeStatus.Running;
        }

        OpenCloseAmbulateToStopNode CreateAmbulateNode(OpenCloseTopologyNode node)
        {
            var go = new GameObject($"Stop_{node.nodeId}");
            go.transform.SetParent(transform, false);
            var amb = go.AddComponent<OpenCloseAmbulateToStopNode>();
            amb.approachAnchor = node.hasApproachAnchor ? node.approachAnchorWorld : (node.target != null ? node.target.transform.position : Vector3.zero);
            amb.arrivalBlendCoefficient = lemmaOverrides.arrivalBlendCoefficient > 0f ? lemmaOverrides.arrivalBlendCoefficient : node.arrivalBlendCoefficient;
            amb.reachRadiusMeters = lemmaOverrides.reachRadiusMeters > 0f ? lemmaOverrides.reachRadiusMeters : node.reachRadiusMeters;
            amb.requireFacingTarget = node.requireFacingTarget;
            amb.handlePoint = node.target != null ? node.target.transform : null;

            if (node.jointKind == OpenCloseJointKind.LatchOnly)
            {
                var unlock = go.AddComponent<UnlockLatchNode>();
                unlock.latch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
                amb.children.Add(unlock);
            }
            else
            {
                var open = go.AddComponent<OpenJointNode>();
                open.driver = node.target != null ? node.target.GetComponent<OpenableJointDriver>() : null;
                open.profile = node.beatProfile;
                amb.children.Add(open);
            }

            if (node.autoCloseBt == AutoCloseBtMode.OnStopExit)
            {
                var exitGo = new GameObject("ExitTrigger");
                exitGo.transform.SetParent(go.transform, false);
                var exit = exitGo.AddComponent<OpenCloseExitTriggerNode>();
                exit.stopCenter = node.target != null ? node.target.transform : transform;
                exit.actor = actor != null ? actor : transform;
                var close = exitGo.AddComponent<CloseJointNode>();
                close.driver = node.target != null ? node.target.GetComponent<OpenableJointDriver>() : null;
                close.profile = node.beatProfile;
                close.relatch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
                amb.children.Add(exit);
                amb.children.Add(close);
            }

            return amb;
        }

        BehaviorTreeStatus RunClose(BehaviorTree tree, OpenCloseTopologyNode node)
        {
            var closeGo = new GameObject($"Close_{node.nodeId}");
            closeGo.transform.SetParent(transform, false);
            var close = closeGo.AddComponent<CloseJointNode>();
            close.driver = node.target != null ? node.target.GetComponent<OpenableJointDriver>() : null;
            close.profile = node.beatProfile;
            close.relatch = node.target != null ? node.target.GetComponent<OpenableLatch>() : null;
            return close.Execute(tree);
        }

        BehaviorTreeStatus DrainCloseStack(BehaviorTree tree)
        {
            if (_closeStack.Count == 0)
            {
                cameraSequence?.RestoreCharacter();
                return BehaviorTreeStatus.Success;
            }
            var node = _closeStack.Pop();
            return RunClose(tree, node);
        }
    }
}
