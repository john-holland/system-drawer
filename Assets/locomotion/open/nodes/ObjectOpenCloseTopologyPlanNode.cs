using System.Collections.Generic;
using Locomotion.Open.Nodes;
using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Generic BT plan node: authors step-by-step object open/close topology into persisted stop children.
    /// </summary>
    public sealed class ObjectOpenCloseTopologyPlanNode : BehaviorTreeNode
    {
        public OpenCloseTopologyAsset topology;
        public OpenCloseLemmaProperties lemmaOverrides;
        public OpenCloseCameraSequence cameraSequence;
        public Transform actor;
        [Tooltip("When true, reuse editor-baked Stop_* children at runtime instead of destroying them.")]
        public bool persistBakedSteps = true;

        readonly Stack<OpenCloseTopologyNode> _closeStack = new Stack<OpenCloseTopologyNode>();
        readonly List<OpenCloseAmbulateToStopNode> _stopNodes = new List<OpenCloseAmbulateToStopNode>();
        int _nodeIndex;
        List<OpenCloseTopologyNode> _flatNodes = new List<OpenCloseTopologyNode>();
        bool _built;
        CloseJointNode _activeClose;

        void Awake() => nodeType = NodeType.Sequence;

        /// <summary>Bake durable BT stop children from the topology asset (editor + runtime).</summary>
        public void BakeFromTopology()
        {
            _closeStack.Clear();
            _stopNodes.Clear();
            _flatNodes.Clear();
            _nodeIndex = 0;
            _activeClose = null;
            children.Clear();

            var result = OpenCloseTopologyBtBuilder.Bake(
                transform,
                topology,
                lemmaOverrides,
                actor,
                clearChildren: true);

            _flatNodes.AddRange(result.flatNodes);
            _stopNodes.AddRange(result.stopNodes);
            // Preserve bake stack order (last OnSequenceEnd visited closes first).
            var closeOrder = new List<OpenCloseTopologyNode>(result.closeStack);
            closeOrder.Reverse();
            for (int i = 0; i < closeOrder.Count; i++)
                _closeStack.Push(closeOrder[i]);

            children.AddRange(_stopNodes);
            _built = true;
        }

        /// <summary>Compatibility alias used by OpenCloseSequenceNode / compiler.</summary>
        public void RebuildFromTopology() => BakeFromTopology();

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            EnsureBuilt();
            if (_flatNodes.Count == 0)
                return BehaviorTreeStatus.Success;

            if (_activeClose != null)
            {
                var closeStatus = _activeClose.Execute(tree);
                if (closeStatus == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
                _activeClose = null;
                if (_nodeIndex >= _flatNodes.Count)
                    return DrainCloseStack(tree);
            }

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

            var autoClose = OpenCloseTopologyBtBuilder.ResolveAutoClose(node, lemmaOverrides, topology != null ? topology.defaultAutoCloseBt : AutoCloseBtMode.OnStopExit);
            if (autoClose == AutoCloseBtMode.AfterChildren)
            {
                _activeClose = OpenCloseTopologyBtBuilder.CreateCloseNode(transform, node);
                var closeStatus = _activeClose.Execute(tree);
                if (closeStatus == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
                _activeClose = null;
            }

            _nodeIndex++;
            if (_nodeIndex >= _flatNodes.Count)
                return DrainCloseStack(tree);
            return BehaviorTreeStatus.Running;
        }

        void EnsureBuilt()
        {
            if (_built)
                return;

            if (persistBakedSteps && transform.childCount > 0 && topology != null)
            {
                var existing = OpenCloseTopologyBtBuilder.CollectExisting(transform, topology);
                if (existing.stopNodes.Count > 0)
                {
                    _flatNodes.Clear();
                    _stopNodes.Clear();
                    _closeStack.Clear();
                    _flatNodes.AddRange(existing.flatNodes);
                    _stopNodes.AddRange(existing.stopNodes);
                    var closeOrder = new List<OpenCloseTopologyNode>(existing.closeStack);
                    closeOrder.Reverse();
                    for (int i = 0; i < closeOrder.Count; i++)
                        _closeStack.Push(closeOrder[i]);
                    children.Clear();
                    children.AddRange(_stopNodes);
                    _built = true;
                    return;
                }
            }

            BakeFromTopology();
        }

        BehaviorTreeStatus DrainCloseStack(BehaviorTree tree)
        {
            if (_closeStack.Count == 0)
            {
                cameraSequence?.RestoreCharacter();
                return BehaviorTreeStatus.Success;
            }

            var node = _closeStack.Pop();
            _activeClose = OpenCloseTopologyBtBuilder.CreateCloseNode(transform, node);
            var status = _activeClose.Execute(tree);
            if (status == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
            _activeClose = null;
            if (_closeStack.Count == 0)
            {
                cameraSequence?.RestoreCharacter();
                return BehaviorTreeStatus.Success;
            }
            return BehaviorTreeStatus.Running;
        }
    }
}
